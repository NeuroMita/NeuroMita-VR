using System.Collections.Generic;
using NeuroMita.Player;
using NeuroMita.Menu;
using NeuroMita.World.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrContextActionsPresenter : MonoBehaviour
    {
        private sealed class ActionRow
        {
            public Button Button;
            public Text Label;
            public ActionType Type;
            public GameObject Target;
            public bool ItemAction;
        }

        private sealed class ActionPanel
        {
            public Canvas Canvas;
            public RectTransform Rect;
            public readonly List<ActionRow> Rows = new();
            public System.IDisposable PhotoExclusion;
            public int Count;
        }

        private readonly List<WorldInteractionTarget.ScreenHintInfo> authoredHints = new();
        private readonly List<LookHintPresenter> suppressed = new();
        private ActionPanel handPanel, worldPanel;
        private VrUiSurface itemStatus;
        private ItemStatusController statusOwner;
        private PlayerActor player;
        private GameObject worldTarget;
        private Vector3 worldAnchor;
        private float refreshAt;

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            if (rig?.Eye == null) { Hide(); return; }
            player = rig.Player;
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.25f;
                foreach (var legacy in FindObjectsByType<LookHintPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (legacy.enabled && !suppressed.Contains(legacy))
                    {
                        suppressed.Add(legacy);
                        legacy.enabled = false;
                    }
                var status = FindFirstObjectByType<ItemStatusController>(FindObjectsInactive.Include);
                if (status != statusOwner)
                {
                    itemStatus?.Dispose();
                    statusOwner = status;
                    itemStatus = VrHostViews.ItemStatusRoot(status) != null ? new VrUiSurface(VrHostViews.ItemStatusRoot(status), rig.Eye, VrRuntime.Presenter) : null;
                }
                itemStatus?.RefreshGeometry();
            }

            bool gameplay = player != null && Time.timeScale > 0f
                && AppServices.GetOrNull<InputManager>()?.CurrentState == InputManager.InputState.Gameplay;
            var inventory = AppServices.GetOrNull<UIManager>()?.CurrentGameUIRoot?.GetComponentInChildren<InventoryFullUI>(true);
            if (!gameplay || (inventory != null && inventory.isInventoryOpen)) { Hide(); return; }

            if (handPanel?.Canvas == null) { Release(handPanel); handPanel = CreatePanel("VR held item actions"); }
            if (worldPanel?.Canvas == null) { Release(worldPanel); worldPanel = CreatePanel("VR world actions"); }
            handPanel.Count = worldPanel.Count = 0;
            var hints = VrHostViews.ItemHints(player.ItemActions);
            var target = player.Interaction?.HoveredInteractable?.TryCast<WorldInteractionTarget>();
            var nextTarget = target != null ? target.gameObject : player.Interaction?.CurrentTargetRaw;
            if (nextTarget == null && hints != null)
                foreach (var hint in hints)
                    if (hint.valid && hint.targetObject != null && hint.availability != ActionAvailability.Hidden)
                    {
                        nextTarget = hint.targetObject;
                        break;
                    }
            if (worldTarget != nextTarget)
            {
                worldTarget = nextTarget;
                worldAnchor = player.Interaction != null ? player.Interaction.CurrentHitPoint : Vector3.zero;
                if (nextTarget != null && nextTarget != player.Interaction?.CurrentTargetRaw
                    && nextTarget != target?.gameObject)
                {
                    var renderer = nextTarget.GetComponentInChildren<Renderer>();
                    worldAnchor = renderer != null ? renderer.bounds.ClosestPoint(rig.Eye.transform.position) : nextTarget.transform.position;
                }
            }
            authoredHints.Clear();
            if (target != null)
            {
                VrHostViews.CollectHints(target, authoredHints);
                foreach (var hint in authoredHints)
                {
                    if (HideTvHint(hint.ActionType, target.gameObject)) continue;
                    AddRow(worldPanel, hint.Description, hint.ActionType, target.gameObject, hint.Blocked);
                }
            }
            if (hints != null)
                foreach (var hint in hints)
                {
                    if (!hint.valid || hint.availability == ActionAvailability.Hidden
                        || (target != null && target.HasAuthoredAction(hint.actionType))
                        || HideTvHint(hint.actionType, hint.targetObject)) continue;
                    AddRow(hint.targetObject != null ? worldPanel : handPanel, hint.description,
                        hint.actionType, hint.targetObject, hint.availability != ActionAvailability.AvailableNow, true);
                }

            var eye = rig.Eye.transform;
            Vector3 handAnchor = rig.RightHandTracked && rig.RightHand != null
                ? rig.RightHand.position + Vector3.up * 0.10f
                : eye.position + eye.rotation * new Vector3(0.28f, -0.25f, 0.65f);
            bool leftPointer = VrRuntime.Settings?.VrPointerLeft == true;
            var offHand = leftPointer ? rig.RightHand : rig.LeftHand;
            bool offHandTracked = leftPointer ? rig.RightHandTracked : rig.LeftHandTracked;
            Vector3 actionAnchor = offHandTracked && offHand != null
                ? offHand.position + Vector3.up * 0.22f
                : eye.position + eye.rotation * new Vector3(-0.25f, -0.25f, 0.65f);
            Place(handPanel, actionAnchor, 0.34f, rig.Eye);
            Vector3 towardEye = (eye.position - worldAnchor).normalized;
            Place(worldPanel, worldAnchor + towardEye * 0.08f, 0.46f, rig.Eye);
            if (itemStatus != null)
            {
                itemStatus.Canvas.enabled = true;
                Quaternion facing = Facing(handAnchor, eye);
                itemStatus.Place(handAnchor + facing * new Vector3(-0.22f, 0f, 0f), facing, 0.07f, rig.Eye);
            }
        }

        public Ray ResolveInteractionRay(Ray pointerRay, VrUiPresenter pointer)
        {
            var canvas = worldPanel?.Canvas;
            if (worldTarget == null || !worldTarget.activeInHierarchy || worldPanel == null || canvas == null
                || worldPanel.Count == 0 || !canvas.gameObject.activeInHierarchy
                || pointer == null || !pointer.IsPointingAt(canvas))
                return pointerRay;
            Vector3 direction = worldAnchor - pointerRay.origin;
            return direction.sqrMagnitude > 0.0001f ? new Ray(pointerRay.origin, direction.normalized) : pointerRay;
        }

        private bool HideTvHint(ActionType type, GameObject target) =>
            type == ActionType.Use && target != null && target.GetComponentInParent<TVDeviceController>() != null
            && VrHostViews.HideTvHud(player);

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void AddRow(ActionPanel panel, string description, ActionType type, GameObject target, bool blocked, bool itemAction = false)
        {
            if (string.IsNullOrWhiteSpace(description)) return;
            if (panel.Count == panel.Rows.Count)
            {
                var buttonRoot = VrObject.Create("Context action", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonRoot.transform.SetParent(panel.Rect, false);
                var button = buttonRoot.GetComponent<Button>();
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var labelRoot = VrObject.Create("Label", typeof(RectTransform), typeof(Text));
                labelRoot.transform.SetParent(buttonRoot.transform, false);
                var labelRect = labelRoot.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = new Vector2(-24f, -8f);
                var label = labelRoot.GetComponent<Text>();
                label.fontSize = 40;
                label.alignment = TextAnchor.MiddleCenter;
                label.raycastTarget = false;
                var row = new ActionRow { Button = button, Label = label };
                button.onClick.AddListener(VrUiEvents.Callback(() => VrHostViews.InvokeContextAction(player, row.Type, row.Target, row.ItemAction)));
                panel.Rows.Add(row);
            }
            var current = panel.Rows[panel.Count++];
            current.Type = type;
            current.Target = target;
            current.ItemAction = itemAction;
            current.Button.interactable = !blocked;
            current.Button.GetComponent<Image>().color = blocked ? new Color(0.2f, 0.13f, 0.18f, 0.95f) : new Color(0.42f, 0.08f, 0.25f, 0.95f);
            current.Label.font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            current.Label.color = blocked ? Color.gray : Color.white;
            current.Label.text = description;
        }

        private static Quaternion Facing(Vector3 position, Transform eye) =>
            (position - eye.position).sqrMagnitude > 0.0001f ? Quaternion.LookRotation(position - eye.position, eye.up) : eye.rotation;

        private static void Place(ActionPanel panel, Vector3 position, float width, Camera eye)
        {
            panel.Canvas.gameObject.SetActive(panel.Count > 0);
            panel.Canvas.worldCamera = eye;
            panel.Rect.sizeDelta = new Vector2(600f, Mathf.Max(1, panel.Count) * 82f);
            panel.Rect.localScale = Vector3.one * (width / 600f);
            panel.Rect.SetPositionAndRotation(position, Facing(position, eye.transform));
            for (int i = 0; i < panel.Rows.Count; i++)
            {
                var row = panel.Rows[i];
                row.Button.gameObject.SetActive(i < panel.Count);
                var rect = row.Button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(600f, 74f);
                rect.anchoredPosition = new Vector2(0f, (panel.Count - 1) * 41f - i * 82f);
            }
        }

        private static ActionPanel CreatePanel(string name)
        {
            var root = VrObject.Create(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 620;
            VrRuntime.Presenter?.RegisterWorldCanvas(canvas);
            return new ActionPanel
            {
                Canvas = canvas,
                Rect = root.GetComponent<RectTransform>(),
                PhotoExclusion = VrHostLease.Wrap(PlayerSelfieController.ExcludeCanvasFromPhotos(canvas))
            };
        }

        private void Hide()
        {
            if (handPanel?.Canvas != null) handPanel.Canvas.gameObject.SetActive(false);
            if (worldPanel?.Canvas != null) worldPanel.Canvas.gameObject.SetActive(false);
            if (itemStatus?.Canvas != null) itemStatus.Canvas.enabled = false;
        }

        private void OnDisable()
        {
            Hide();
            foreach (var legacy in suppressed) if (legacy != null) legacy.enabled = true;
            suppressed.Clear();
            itemStatus?.Dispose();
            itemStatus = null;
            statusOwner = null;
        }

        private void OnDestroy()
        {
            Release(handPanel);
            Release(worldPanel);
        }

        private static void Release(ActionPanel panel)
        {
            panel?.PhotoExclusion?.Dispose();
            if (panel?.Canvas != null)
            {
                if (Application.isPlaying) Destroy(panel.Canvas.gameObject);
                else DestroyImmediate(panel.Canvas.gameObject);
            }
        }
    }
}