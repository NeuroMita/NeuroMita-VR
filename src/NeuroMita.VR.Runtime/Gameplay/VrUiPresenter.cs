using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10010)]
#endif
    public sealed partial class VrUiPresenter : MonoBehaviour
    {
        private sealed class Panel
        {
            public Canvas canvas;
            public RectTransform rect;
            public RenderMode mode;
            public Camera camera;
            public Vector3 position, scale;
            public Quaternion rotation;
            public Vector2 min, max, pivot, size, anchored;
            public CanvasScaler scaler;
            public bool scalerEnabled;
            public bool wasActive;
            public float worldScale;
            public Vector3 worldPosition;
            public Quaternion worldRotation;
            public bool backdrop;
            public VrMenuBackdrop backdropView;
            public System.IDisposable photoExclusion;
        }

        private readonly RaycastResult emptyHit = new RaycastResult();
        private readonly List<Panel> panels = new List<Panel>();
#if NEUROMITA_VR_IL2CPP
        private readonly Il2CppSystem.Collections.Generic.List<RaycastResult> hits = new();
#else
        private readonly List<RaycastResult> hits = new();
#endif
        private readonly List<GraphicRaycaster> raycasters = new List<GraphicRaycaster>();
        private sealed class ChatMask
        {
            public CanvasGroup Group;
            public float Alpha;
            public bool Interactable, BlocksRaycasts, Created;
        }
        private readonly Dictionary<Transform, ChatMask> chatMasks = new();
        private Canvas pauseCanvas;
        private VrPlayerRig rig;
        private PointerEventData pointer;
        private EventSystem eventSystem;
        private GameObject hover, press, drag;
        private LineRenderer laser;
        private Material laserMaterial;
        private bool lastHeld;
        private Vector3 pressWorld;
        private readonly VrConsolePresentation consolePresentation = new();
        internal bool IsPointingAtToolkit => hover != null && hover.GetComponentInParent<VrToolkitInputRelay>() != null;
        private bool waitForPointerRelease;
        private bool pointerLeft, pointerPreferenceSet;
        private Vector2 screenPosition;
        private int pointerFrame = -1;
        public Vector2 PointerPosition { get { EnsurePointerUpdated(); return screenPosition; } }
        public bool HasUiTarget { get { EnsurePointerUpdated(); return hover != null; } }

        public bool IsPointingAt(Canvas canvas)
        {
            EnsurePointerUpdated();
            return canvas != null && hover != null && hover.transform.IsChildOf(canvas.transform);
        }

        private void EnsurePointerUpdated()
        {
            if (pointerFrame == Time.frameCount || rig == null || rig.Eye == null || !VrRuntime.IsActive) return;
            pointerFrame = Time.frameCount;
            RefreshPauseVisibility(FindPauseCanvas());
            rig.RefreshTrackingPose();
            UpdatePointer();
        }

        private Canvas FindPauseCanvas()
            => panels.Find(p => p.canvas != null && p.canvas.isActiveAndEnabled && p.canvas.GetComponent<InterfaceFastMenu>() != null)?.canvas;

        public static bool CanRaycastDuringPause(Canvas canvas, Canvas pause)
            => canvas != null && (pause == null || canvas == pause || canvas.transform.IsChildOf(pause.transform));

        public void RefreshPauseVisibility(Canvas pause)
        {
            if (pause != null && !pause.isActiveAndEnabled) pause = null;
            if (pauseCanvas != pause) CancelInput();
            pauseCanvas = pause;
            RefreshPauseBackdrop(pause);
            if (pause == null)
            {
                foreach (var mask in chatMasks.Values)
                {
                    if (mask.Group == null) continue;
                    mask.Group.alpha = mask.Alpha;
                    mask.Group.interactable = mask.Interactable;
                    mask.Group.blocksRaycasts = mask.BlocksRaycasts;
                    if (mask.Created)
                    {
                        if (Application.isPlaying) Destroy(mask.Group);
                        else DestroyImmediate(mask.Group);
                    }
                }
                chatMasks.Clear();
                return;
            }
            foreach (var chat in FindObjectsByType<MitaAI.Interface.InterfaceRoot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var composer = chat.InputFieldGO != null ? chat.InputFieldGO : chat.GetComponent<MitaAI.InputControl>()?.inputFieldObject;
                var view = composer != null ? composer.transform : chat.transform;
                if (!view.gameObject.activeInHierarchy || pause.transform.IsChildOf(view)) continue;
                if (!chatMasks.TryGetValue(view, out var mask))
                {
                    var group = view.GetComponent<CanvasGroup>();
                    bool created = group == null;
                    if (created) group = view.gameObject.AddComponent<CanvasGroup>();
                    mask = new ChatMask { Group = group, Alpha = group.alpha, Interactable = group.interactable,
                        BlocksRaycasts = group.blocksRaycasts, Created = created };
                    chatMasks.Add(view, mask);
                }
                if (mask.Group == null) continue;
                mask.Group.alpha = 0f;
                mask.Group.interactable = false;
                mask.Group.blocksRaycasts = false;
            }
        }

        public void Bind(VrPlayerRig playerRig)
        {
            CancelInput();
            rig = playerRig;
            pointerFrame = -1;
            foreach (var panel in panels)
                if (panel.canvas != null)
                {
                    panel.canvas.worldCamera = rig != null ? rig.Eye : null;
                    Place(panel);
                }
            if (rig == null)
            {
                screenPosition = Vector2.zero;
                if (laser != null) laser.enabled = false;
                return;
            }
            if (laser == null)
            {
                var root = VrObject.Create("VR pointer");
                root.transform.SetParent(transform, false);
                laser = root.AddComponent<LineRenderer>();
                laser.positionCount = 2;
                laser.useWorldSpace = true;
                laser.startWidth = 0.008f;
                laser.endWidth = 0.005f;
                laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                laser.receiveShadows = false;
                var template = VrAssets.Load<Material>("NeuroMitaVrPointer");
                laserMaterial = template != null ? new Material(template) : new Material(Shader.Find("Unlit/Color"));
                laserMaterial.color = Color.cyan;
                laser.sharedMaterial = laserMaterial;
            }
        }

        public void RegisterWorldCanvas(Canvas canvas)
        {
            if (canvas == null) return;
            if (rig != null) canvas.worldCamera = rig.Eye;
            foreach (var raycaster in canvas.GetComponentsInChildren<GraphicRaycaster>(true))
                if (!raycasters.Contains(raycaster)) raycasters.Add(raycaster);
        }

        public void RefreshCanvases()
        {
            if (rig == null) return;
            panels.RemoveAll(p =>
            {
                if (p.canvas != null) return false;
                p.backdropView?.Dispose();
                p.photoExclusion?.Dispose();
                return true;
            });
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
                AdaptCanvas(canvas);
            foreach (var canvas in canvases)
                if (canvas.GetComponent<NeuroMita.Menu.MenuUIRoot>() != null && !panels.Exists(p => p.canvas == canvas))
                    PresentCanvas(canvas);
            raycasters.Clear();
            raycasters.AddRange(FindObjectsByType<GraphicRaycaster>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        public void AdaptCanvas(Canvas canvas)
        {
                if (rig == null || canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace || panels.Exists(p => p.canvas == canvas)) return;
                var rect = canvas.GetComponent<RectTransform>();
                if (rect == null) return;
                var scaler = canvas.GetComponent<CanvasScaler>();
                var panel = new Panel
                {
                    canvas = canvas, rect = rect, mode = canvas.renderMode, camera = canvas.worldCamera,
                    position = rect.localPosition, rotation = rect.localRotation, scale = rect.localScale,
                    min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta,
                    anchored = rect.anchoredPosition, scaler = scaler, scalerEnabled = scaler != null && scaler.enabled,
                    backdrop = IsMenuBackdrop(canvas)
                };
                Vector2 resolution = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    ? scaler.referenceResolution : rect.rect.size;
                if (resolution.x < 100f || resolution.y < 100f) resolution = new Vector2(1920f, 1080f);
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = rig.Eye;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                rect.sizeDelta = resolution;
                panel.worldScale = 1.8f / resolution.x;
                ApplyScale(panel);
                panels.Add(panel);
                panel.photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
                Place(panel);
                if (panel.backdrop) panel.backdropView = new VrMenuBackdrop(canvas, transform);
        }

        public void PresentPauseMenu(Canvas canvas) => PresentCanvas(canvas);

        public void PresentCanvas(Canvas canvas)
        {
            if (rig == null || canvas == null) return;
            if (!panels.Exists(p => p.canvas == canvas))
            {
                var rect = canvas.GetComponent<RectTransform>();
                if (rect == null) return;
                var scaler = canvas.GetComponent<CanvasScaler>();
                var panel = new Panel
                {
                    canvas = canvas, rect = rect, mode = canvas.renderMode, camera = canvas.worldCamera,
                    position = rect.localPosition, rotation = rect.localRotation, scale = rect.localScale,
                    min = rect.anchorMin, max = rect.anchorMax, pivot = rect.pivot, size = rect.sizeDelta,
                    anchored = rect.anchoredPosition, scaler = scaler, scalerEnabled = scaler != null && scaler.enabled,
                    backdrop = IsMenuBackdrop(canvas)
                };
                Vector2 resolution = rect.rect.size;
                if (canvas.renderMode != RenderMode.WorldSpace || resolution.x < 100f || resolution.y < 100f)
                    resolution = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                        ? scaler.referenceResolution : resolution;
                if (resolution.x < 100f || resolution.y < 100f) resolution = new Vector2(1920f, 1080f);
                if (scaler != null) scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                rect.sizeDelta = resolution;
                panel.worldScale = 1.8f / resolution.x;
                ApplyScale(panel);
                panels.Add(panel);
                panel.photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            }
            var target = panels.Find(p => p.canvas == canvas);
            canvas.worldCamera = rig.Eye;
            Place(target);
            foreach (var raycaster in canvas.GetComponentsInChildren<GraphicRaycaster>(true))
                if (!raycasters.Contains(raycaster)) raycasters.Add(raycaster);
        }

        public void Recenter()
        {
            foreach (var panel in panels) if (panel.canvas != null) Place(panel);
        }

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void Place(Panel panel)
        {
            if (rig == null || rig.Eye == null) return;
            var eye = rig.Eye.transform;
            panel.worldRotation = Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
            float distance = PanelDistance(panel);
            panel.worldPosition = eye.position + panel.worldRotation * new Vector3(0f, -0.05f + (VrRuntime.Settings?.VrUiHeight ?? 0) * 0.01f, distance);
            panel.rect.SetPositionAndRotation(panel.worldPosition, panel.worldRotation);
        }

        private int layoutDistance = -1, layoutHeight = int.MinValue, layoutFollow = -1;

        private bool IsMenuBackdrop(Canvas canvas)
        {
            return rig?.Player == null &&
                NeuroMita.Menu.MenuSceneResolver.ResolvePrimaryRoot(canvas.gameObject.scene) != null &&
                canvas.GetComponentsInChildren<GraphicRaycaster>(true).Length == 0 &&
                canvas.GetComponentsInChildren<Image>(true).Length > 0;
        }

        private static float PanelDistance(Panel panel)
        {
            float distance = (VrRuntime.Settings?.VrUiDistance ?? 150) * 0.01f;
            if (VrRuntime.Rig?.Player == null && !panel.backdrop) distance = Mathf.Min(distance, 1.1f);
            return panel.backdrop ? Mathf.Max(3.5f, distance + 2f) :
                distance - Mathf.Clamp(panel.canvas.sortingOrder, -100, 100) * 0.0001f;
        }

        private static void ApplyScale(Panel panel)
        {
            float desired = panel.worldScale * (panel.backdrop ? 4f : 1f) * ((VrRuntime.Settings?.VrUiScale ?? 100) * 0.01f);
            Vector3 parentScale = panel.rect.parent != null ? panel.rect.parent.lossyScale : Vector3.one;
            panel.rect.localScale = new Vector3(desired / Mathf.Max(0.000001f, Mathf.Abs(parentScale.x)),
                desired / Mathf.Max(0.000001f, Mathf.Abs(parentScale.y)),
                desired / Mathf.Max(0.000001f, Mathf.Abs(parentScale.z)));
        }

        private void LateUpdate()
        {
            if (rig == null || rig.Eye == null) return;
            RefreshPauseVisibility(FindPauseCanvas());
            consolePresentation.Refresh();
            var settings = VrRuntime.Settings;
            int distanceSetting = settings?.VrUiDistance ?? 150;
            int heightSetting = settings?.VrUiHeight ?? 0;
            int followSetting = settings?.VrUiFollow ?? 1;
            bool layoutChanged = layoutDistance != distanceSetting || layoutHeight != heightSetting || layoutFollow != followSetting;
            layoutDistance = distanceSetting;
            layoutHeight = heightSetting;
            layoutFollow = followSetting;
            foreach (var panel in panels)
            {
                if (panel.canvas == null) continue;
                bool active = panel.canvas.isActiveAndEnabled;
                if (active && (!panel.wasActive || layoutChanged)) Place(panel);
                panel.wasActive = active;
                if (active)
                {
                    ApplyScale(panel);
                    int follow = VrRuntime.Settings?.VrUiFollow ?? 1;
                    bool console = VrConsolePresentation.IsOpen && (panel.canvas.GetComponentInChildren<ConsoleInterface>(true) != null ||
                        panel.canvas.GetComponentInParent<ConsoleInterface>() != null);
                    if (panel.backdrop || console) follow = 0;
                    if (follow != 2 && panel.canvas.GetComponent<NeuroMita.Menu.MenuUIRoot>() != null)
                        follow = 0;
                    var eye = rig.Eye.transform;
                    float distance = PanelDistance(panel);
                    var facing = follow == 2 ? eye.rotation : Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
                    var desired = eye.position + facing * new Vector3(0f, -0.05f + (VrRuntime.Settings?.VrUiHeight ?? 0) * 0.01f, distance);
                    if (follow == 2)
                    {
                        panel.worldRotation = facing;
                        panel.worldPosition = desired;
                    }
                    else if (follow == 1 && (Mathf.Abs(Mathf.DeltaAngle(panel.worldRotation.eulerAngles.y, eye.eulerAngles.y)) > 45f ||
                        Vector3.Distance(panel.worldPosition, desired) > 0.5f))
                    {
                        float blend = 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime);
                        panel.worldRotation = Quaternion.Slerp(panel.worldRotation, facing, blend);
                        panel.worldPosition = Vector3.Lerp(panel.worldPosition, desired, blend);
                    }
                    panel.rect.SetPositionAndRotation(panel.worldPosition, panel.worldRotation);
                    panel.backdropView?.Refresh();
                }
            }
            RefreshPauseBackdrop(pauseCanvas);
            EnsurePointerUpdated();
        }

        private void UpdatePointer()
        {
            if (VrQteInput.CapturesActions || VrDeathInput.BlocksPointer) { CancelInput(); return; }
            bool leftPointer = VrRuntime.Settings?.VrPointerLeft == true;
            if (!pointerPreferenceSet || pointerLeft != leftPointer)
            {
                CancelInput();
                pointerLeft = leftPointer;
                pointerPreferenceSet = true;
            }
            if (!rig.HeadTracked || !rig.PointerHandTracked)
            {
                Release();
                waitForPointerRelease = true;
                if (laser != null) laser.enabled = false;
                return;
            }
            if (waitForPointerRelease)
            {
                if (VrRuntime.Input.PointerHeld) return;
                waitForPointerRelease = false;
            }
            if (EventSystem.current == null) { Release(); return; }
            if (eventSystem != EventSystem.current)
            {
                CancelInput();
                eventSystem = EventSystem.current;
                pointer = new PointerEventData(eventSystem) { pointerId = -100, button = PointerEventData.InputButton.Left };
                if (VrRuntime.Input.PointerHeld) return;
            }
            var ray = rig.PointerRay;
            Vector3 end = ray.GetPoint(3f);
            float nearest = float.PositiveInfinity;
            RaycastResult chosen = emptyHit;
            Vector2 chosenPosition = screenPosition;
            foreach (var raycaster in raycasters)
            {
                if (raycaster == null || !raycaster.isActiveAndEnabled) continue;
                var canvas = raycaster.GetComponent<Canvas>();
                if (!canvas.isActiveAndEnabled || canvas.renderMode != RenderMode.WorldSpace) continue;
                if (!VrDeathInput.IsVisible && !CanRaycastDuringPause(canvas, pauseCanvas)) continue;
                var rect = canvas.GetComponent<RectTransform>();
                if (!TryProject(ray, rect, out var world, out float distance)) continue;
                if (distance > 10f) continue;
                var eventCamera = canvas.worldCamera != null ? canvas.worldCamera : rig.Eye;
                Vector2 pixels = eventCamera.WorldToScreenPoint(world);
                pointer.position = pixels;
                hits.Clear();
                raycaster.Raycast(pointer, hits);
                if (hits.Count == 0)
                {
                    if (canvas.GetComponent<MitaAI.RadialMenu>() != null)
                    {
                        if (distance < nearest)
                        {
                            nearest = distance;
                            chosen = emptyHit;
                            chosenPosition = pixels;
                            end = world;
                        }
                    }
                    continue;
                }
                if (VrDeathInput.IsVisible)
                    for (int i = hits.Count - 1; i >= 0; i--)
                        if (!VrDeathInput.AllowsTarget(hits[i].gameObject)) hits.RemoveAt(i);
                if (hits.Count == 0 || distance >= nearest) continue;
                nearest = distance;
                chosen = hits[0];
                chosenPosition = pixels;
                end = world;
            }
            pointer.delta = chosenPosition - screenPosition;
            screenPosition = chosenPosition;
            pointer.position = chosenPosition;
            pointer.pointerCurrentRaycast = chosen;
            VrInventoryDrag.Update(pointer);
            var target = chosen.gameObject;
            var scroll = target != null ? VrUiDispatch.GetEventHandler<IScrollHandler>(target) : null;
            float scrollAxis = VrRuntime.Input.Turn.y;
            if (scroll != null && Mathf.Abs(scrollAxis) > 0.2f)
            {
                pointer.scrollDelta = new Vector2(0f, scrollAxis * Time.unscaledDeltaTime * 20f);
                VrUiDispatch.Execute(scroll, pointer, ExecuteEvents.scrollHandler);
            }
            if (hover != target)
            {
                if (hover != null) VrUiDispatch.ExecuteHierarchy(hover, pointer, ExecuteEvents.pointerExitHandler);
                hover = target;
                pointer.pointerEnter = hover;
                if (hover != null) VrUiDispatch.ExecuteHierarchy(hover, pointer, ExecuteEvents.pointerEnterHandler);
                if (hover != null) SendUiHaptic(0.12f, 0.025f);
            }
            if (hover != null && pointer.delta.sqrMagnitude > 0f)
                VrUiDispatch.ExecuteHierarchy(hover, pointer, ExecuteEvents.pointerMoveHandler);
            bool primary = VrRuntime.Input.PointerPrimaryHeld;
            bool secondary = VrRuntime.Input.PointerSecondaryHeld;
            bool held = rig.HeadTracked && rig.PointerHandTracked && (primary || secondary);
            if (held && !lastHeld) VrInventoryDrag.PointerDown(target);
            if (held && !lastHeld && target != null)
            {
                pointer.button = secondary && !primary ? PointerEventData.InputButton.Right : PointerEventData.InputButton.Left;
                pressWorld = end;
                pointer.pressPosition = pointer.position;
                pointer.pointerPressRaycast = chosen;
                pointer.eligibleForClick = true;
                SendUiHaptic(0.22f, 0.035f);
                VrRuntime.Keyboard?.PreserveSelection();
                press = VrUiDispatch.ExecuteHierarchy(target, pointer, ExecuteEvents.pointerDownHandler);
                if (press == null) press = VrUiDispatch.GetEventHandler<IPointerClickHandler>(target);
                pointer.pointerPress = press;
                pointer.rawPointerPress = target;
                if (VrRuntime.Keyboard?.Owns(press) != true) eventSystem.SetSelectedGameObject(press, pointer);
                drag = pointer.button == PointerEventData.InputButton.Left ? VrUiDispatch.GetEventHandler<IDragHandler>(target) : null;
                pointer.pointerDrag = drag;
                if (drag != null) VrUiDispatch.Execute(drag, pointer, ExecuteEvents.initializePotentialDrag);
                VrDiagnostics.Mark("UI press", "target=" + target.name + ", handler=" + press?.name + ", drag=" + drag?.name + ", position=" + end);
            }
            if (held && drag != null && pointer.delta.sqrMagnitude > 0f)
            {
                if (!pointer.dragging && (end - pressWorld).sqrMagnitude > 0.000225f)
                {
                    pointer.dragging = true;
                    pointer.eligibleForClick = false;
                    VrUiDispatch.Execute(drag, pointer, ExecuteEvents.beginDragHandler);
                }
                if (pointer.dragging) VrUiDispatch.Execute(drag, pointer, ExecuteEvents.dragHandler);
            }
            if (!held && lastHeld) ReleasePress(target);
            lastHeld = held;
            if (laser != null)
            {
                bool showPointer = NeuroMita.Settings.GameSettingsService.Instance?.ShowCrosshair ?? true;
                laser.enabled = rig.PointerHandTracked && showPointer;
                laser.SetPosition(0, ray.origin);
                laser.SetPosition(1, end);
                ShowHitMarker(target != null && showPointer, end, ray, held);
            }
        }

        public static bool TryProject(Ray ray, RectTransform rect, out Vector3 point, out float distance)
        {
            point = default;
            distance = 0f;
            if (rect == null || !new Plane(rect.forward, rect.position).Raycast(ray, out distance) || distance <= 0f) return false;
            point = ray.GetPoint(distance);
            Vector3 local = rect.InverseTransformPoint(point);
            return rect.rect.Contains(new Vector2(local.x, local.y));
        }

        private static void SendUiHaptic(float amplitude, float duration) => VrHaptics.Ui(amplitude, duration);

        private void ReleasePress(GameObject target)
        {
            if (pointer == null) return;
            if (press != null) VrDiagnostics.Mark("UI release", "handler=" + press.name + ", target=" + target?.name + ", eligible=" + pointer.eligibleForClick + ", dragging=" + pointer.dragging);
            if (press != null) VrUiDispatch.Execute(press, pointer, ExecuteEvents.pointerUpHandler);
            if (pointer.dragging && drag != null)
            {
                if (target != null) VrUiDispatch.ExecuteHierarchy(target, pointer, ExecuteEvents.dropHandler);
                VrUiDispatch.Execute(drag, pointer, ExecuteEvents.endDragHandler);
            }
            else if (pointer.eligibleForClick && press != null && press == VrUiDispatch.GetEventHandler<IPointerClickHandler>(target))
                VrUiDispatch.Execute(press, pointer, ExecuteEvents.pointerClickHandler);
            pointer.pointerPress = null;
            pointer.pointerDrag = null;
            pointer.dragging = false;
            pointer.eligibleForClick = false;
            press = drag = null;
        }

        private void Release()
        {
            if (pointer != null) pointer.eligibleForClick = false;
            ReleasePress(null);
            VrInventoryDrag.Cancel();
            if (hover != null && pointer != null) VrUiDispatch.ExecuteHierarchy(hover, pointer, ExecuteEvents.pointerExitHandler);
            hover = null;
            ShowHitMarker(false, default, default, false);
            lastHeld = false;
        }

        public void CancelInput()
        {
            if (pointer != null) pointer.eligibleForClick = false;
            Release();
            waitForPointerRelease = true;
            if (laser != null) laser.enabled = false;
            ShowHitMarker(false, default, default, false);
        }

        public void Restore()
        {
            RefreshPauseVisibility(null);
            Release();
            foreach (var panel in panels)
            {
                panel.backdropView?.Dispose();
                panel.photoExclusion?.Dispose();
                if (panel.canvas == null) continue;
                panel.canvas.renderMode = panel.mode;
                panel.canvas.worldCamera = panel.camera;
                panel.rect.anchorMin = panel.min;
                panel.rect.anchorMax = panel.max;
                panel.rect.pivot = panel.pivot;
                panel.rect.sizeDelta = panel.size;
                panel.rect.anchoredPosition = panel.anchored;
                panel.rect.localPosition = panel.position;
                panel.rect.localRotation = panel.rotation;
                panel.rect.localScale = panel.scale;
                if (panel.scaler != null) panel.scaler.enabled = panel.scalerEnabled;
            }
            panels.Clear();
            ReleasePauseBackdrop();
            ReleaseHitMarker();
            consolePresentation.Dispose();
            if (laserMaterial != null)
            {
                if (Application.isPlaying) Destroy(laserMaterial);
                else DestroyImmediate(laserMaterial);
                laserMaterial = null;
            }
        }

        private void OnDisable() => RefreshPauseVisibility(null);
        private void OnDestroy() => Restore();
    }
}
