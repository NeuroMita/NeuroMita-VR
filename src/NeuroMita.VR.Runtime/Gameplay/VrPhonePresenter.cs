using NeuroMita.Menu;
using NeuroMita.Player;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrPhonePresenter : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform panel;
        private RawImage preview;
        private Text controlsHint;
        private Text sessionStatus;
        private SelfieHudView legacyHud;
        private VrUiSurface legacyHudSurface;
        private RenderTexture previewTexture;
        private PlayerSelfieController controller;
        private int revision = -1;
        private float nextPreview;
        private System.IDisposable photoExclusion;
        private readonly System.Collections.Generic.List<Text> labels = new();

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            var current = rig?.Player != null ? rig.Player.GetComponent<PlayerSelfieController>() : null;
            if (VrRuntime.Input?.IsActive == true)
                ApplyFramingInput(current, VrRuntime.Input.Turn, Time.unscaledDeltaTime);
            Refresh(current, rig, VrRuntime.Presenter);
        }

        public static bool ApplyFramingInput(PlayerSelfieController current, Vector2 turn, float deltaTime)
        {
            if (current == null || !current.CanAdjustFraming || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                return false;
            float step = Mathf.Min(deltaTime, 0.1f);
            float deadzone = (VrRuntime.Settings?.VrTurnDeadzone ?? 15) * 0.01f;
            if (turn.magnitude <= deadzone) turn = Vector2.zero;
            else turn = Vector2.ClampMagnitude(turn, 1f);
            return current.TryAdjustFraming(Vector2.zero,
                current.WristRollUnlocked ? turn * (90f * step) : Vector2.zero,
                current.WristRollUnlocked ? 0f : turn.x * (0.25f * step),
                current.WristRollUnlocked ? 0f : -turn.y * (40f * step));
        }

        public void Refresh(PlayerSelfieController current, VrPlayerRig rig, VrUiPresenter pointer)
        {
            bool visible = current != null && current.IsActive && rig?.Eye != null &&
                AppServices.GetOrNull<InputManager>()?.CurrentState == InputManager.InputState.Gameplay;
            if (!visible)
            {
                if (canvas != null) canvas.gameObject.SetActive(false);
                controller = null;
                RestoreLegacyHud();
                return;
            }
            bool opening = controller != current || canvas == null || !canvas.gameObject.activeSelf;
            controller = current;
            EnsureCanvas(pointer);
            canvas.gameObject.SetActive(true);
            canvas.worldCamera = rig.Eye;
            if (opening)
            {
                var eye = rig.Eye.transform;
                var facing = Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
                panel.SetPositionAndRotation(eye.position + facing * new Vector3(0.45f, -0.25f, 1.1f), facing);
            }
            if (revision != LocalizationManager.CurrentRevision) RefreshLabels();
            RefreshLegacyHud(AppServices.GetOrNull<UIManager>()?.CurrentGameUIRoot?.SelfieHud, rig.Eye, pointer);
            controlsHint.text = LocalizationManager.GetTableString(!current.CanAdjustFraming ? "vr.phone.walkControls" :
                current.WristRollUnlocked ? "vr.phone.wristControls" : "vr.phone.framingControls");
            if (Time.unscaledTime >= nextPreview && current.ActiveSessionCamera != null)
            {
                nextPreview = Time.unscaledTime + 0.1f;
                var lens = current.ActiveSessionCamera;
                var target = lens.targetTexture;
                var stereo = lens.stereoTargetEye;
                bool canvasEnabled = canvas.enabled;
                try
                {
                    canvas.enabled = false;
                    lens.targetTexture = previewTexture;
                    lens.stereoTargetEye = StereoTargetEyeMask.None;
                    lens.Render();
                }
                finally
                {
                    canvas.enabled = canvasEnabled;
                    lens.targetTexture = target;
                    lens.stereoTargetEye = stereo;
                }
            }
        }

        private void RefreshLegacyHud(SelfieHudView hud, Camera eye, VrUiPresenter pointer)
        {
            if (hud != legacyHud)
            {
                RestoreLegacyHud();
                legacyHud = hud;
                if (hud != null && hud.GetComponent<RectTransform>() is { } view)
                {
                    legacyHudSurface = new VrUiSurface(view, eye, pointer);
                    legacyHudSurface.Canvas.enabled = false;
                }
            }
            bool hintsVisible = legacyHud == null || legacyHud.CanvasGroup == null || legacyHud.CanvasGroup.alpha > 0f;
            controlsHint.enabled = hintsVisible;
            if (sessionStatus == null) return;
            sessionStatus.text = hintsVisible && legacyHud != null
                ? (legacyHud.ModeText != null ? legacyHud.ModeText.text : "") +
                    (legacyHud.ToastText != null && legacyHud.ToastText.gameObject.activeSelf ? "\n" + legacyHud.ToastText.text : "")
                : "";
        }

        private void RestoreLegacyHud()
        {
            legacyHudSurface?.Dispose();
            legacyHudSurface = null;
            legacyHud = null;
        }
        private void EnsureCanvas(VrUiPresenter pointer)
        {
            if (canvas != null) return;
            var root = VrObject.Create("VR phone controls", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(720f, 780f);
            panel.localScale = Vector3.one * 0.001f;
            root.GetComponent<Image>().color = new Color(0.07f, 0.035f, 0.1f, 0.96f);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 600;
            photoExclusion = VrHostLease.Wrap(PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            var screen = VrObject.Create("Phone preview", typeof(RectTransform), typeof(RawImage));
            screen.transform.SetParent(panel, false);
            var screenRect = screen.GetComponent<RectTransform>();
            screenRect.sizeDelta = new Vector2(672f, 300f);
            screenRect.anchoredPosition = new Vector2(0f, 150f);
            preview = screen.GetComponent<RawImage>();
            preview.raycastTarget = false;
            previewTexture = new RenderTexture(672, 300, 24);
            previewTexture.Create();
            preview.texture = previewTexture;
            for (int i = 0; i < 9; i++)
            {
                var action = (PlayerSelfieController.SessionAction)i;
                var button = VrObject.Create(action.ToString(), typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(panel, false);
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(216f, 72f);
                rect.anchoredPosition = new Vector2((i % 3 - 1) * 228f, -55f - (i / 3) * 82f);
                button.GetComponent<Image>().color = new Color(0.3f, 0.12f, 0.32f);
                button.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };
                button.GetComponent<Button>().onClick.AddListener(VrUiEvents.Callback(() => controller?.TryPerformAction(action)));
                var text = VrObject.Create("Label", typeof(RectTransform), typeof(Text));
                text.transform.SetParent(button.transform, false);
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = new Vector2(-12f, -8f);
                var label = text.GetComponent<Text>();
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                labels.Add(label);
            }
            var hint = VrObject.Create("Controller help", typeof(RectTransform), typeof(Text));
            hint.transform.SetParent(panel, false);
            var hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(672f, 64f);
            hintRect.anchoredPosition = new Vector2(0f, -295f);
            controlsHint = hint.GetComponent<Text>();
            controlsHint.fontSize = 20;
            controlsHint.alignment = TextAnchor.MiddleCenter;
            controlsHint.color = new Color(0.9f, 0.82f, 0.95f);
            controlsHint.raycastTarget = false;
            var status = VrObject.Create("Session status", typeof(RectTransform), typeof(Text));
            status.transform.SetParent(panel, false);
            var statusRect = status.GetComponent<RectTransform>();
            statusRect.sizeDelta = new Vector2(672f, 80f);
            statusRect.anchoredPosition = new Vector2(0f, -355f);
            sessionStatus = status.GetComponent<Text>();
            sessionStatus.fontSize = 20;
            sessionStatus.alignment = TextAnchor.MiddleCenter;
            sessionStatus.color = new Color(0.9f, 0.82f, 0.95f);
            sessionStatus.raycastTarget = false;
            RefreshLabels();
            pointer?.RegisterWorldCanvas(canvas);
        }

        private void RefreshLabels()
        {
            revision = LocalizationManager.CurrentRevision;
            controlsHint.font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (sessionStatus != null) sessionStatus.font = controlsHint.font;
            for (int i = 0; i < labels.Count; i++)
            {
                var action = (PlayerSelfieController.SessionAction)i;
                labels[i].font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labels[i].text = LocalizationManager.GetTableString("vr.phone." + action) ?? action.ToString();
            }
        }

        private void OnDisable()
        {
            RestoreLegacyHud();
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            RestoreLegacyHud();
            photoExclusion?.Dispose();
            if (canvas != null)
            {
                if (Application.isPlaying) Destroy(canvas.gameObject);
                else DestroyImmediate(canvas.gameObject);
            }
            if (previewTexture != null)
            {
                previewTexture.Release();
                if (Application.isPlaying) Destroy(previewTexture);
                else DestroyImmediate(previewTexture);
            }
        }
    }
}
