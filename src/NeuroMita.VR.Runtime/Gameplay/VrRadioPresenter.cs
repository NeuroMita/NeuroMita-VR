using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrRadioPresenter : MonoBehaviour
    {
        private MusicListUI view;
        private UIDocument document;
        private PanelSettings originalSettings, settings;
        private RenderTexture texture;
        private Canvas canvas;
        private VisualElement backdrop;
        private StyleColor originalBackdrop;
        private VrRadioLayout volumeLayout;
        private VrToolkitInputRelay relay;
        private System.IDisposable photoExclusion;
        private float nextDiscovery;

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            if (rig?.Eye == null) { Release(); return; }
            if (Time.unscaledTime >= nextDiscovery)
            {
                nextDiscovery = Time.unscaledTime + 0.25f;
                MusicListUI current = null;
                foreach (var candidate in FindObjectsByType<MusicListUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (candidate.isActiveAndEnabled) { current = candidate; break; }
                if (current != view) { Release(); if (current != null) Bind(current, rig.Eye, VrRuntime.Presenter); }
            }
            if (view == null || !view.isActiveAndEnabled) { Release(); return; }
            if (document.panelSettings != settings) document.panelSettings = settings;
            canvas.worldCamera = rig.Eye;
        }

        private void Bind(MusicListUI source, Camera eye, VrUiPresenter pointer)
        {
            view = source;
            document = source.GetComponent<UIDocument>();
            if (document == null || document.panelSettings == null) { Release(); return; }
            originalSettings = document.panelSettings;
            settings = Instantiate(originalSettings);
            settings.name = "VR radio panel settings";
            texture = new RenderTexture(1280, 900, 24, RenderTextureFormat.ARGB32);
            texture.Create();
            settings.renderMode = PanelRenderMode.ScreenSpaceOverlay;
            settings.targetTexture = texture;
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            settings.clearDepthStencil = true;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;
            document.panelSettings = settings;
            backdrop = document.rootVisualElement.Q<VisualElement>(className: "radio-panel-surface");
            if (backdrop != null)
            {
                originalBackdrop = backdrop.style.backgroundColor;
                backdrop.style.backgroundColor = new Color(0.16f, 0.06f, 0.22f, 1f);
            }
            volumeLayout = new VrRadioLayout(document.rootVisualElement);
            var root = VrObject.Create("VR radio controls", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(RawImage));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = eye;
            canvas.sortingOrder = 650;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280f, 900f);
            rect.localScale = Vector3.one * (1.375f / 1280f);
            var head = eye.transform;
            Vector3 anchor = VrHostViews.RadioAnchor(source) != null ? VrHostViews.RadioAnchor(source).position : head.position + head.forward;
            var facing = Quaternion.LookRotation((anchor - head.position).sqrMagnitude > 0.001f ? anchor - head.position : head.forward, Vector3.up);
            Vector3 center = anchor + (head.position - anchor).normalized * 0.45f + Vector3.up * 0.18f;
            Vector3 direction = center - head.position;
            if (direction.magnitude > 1.8f || direction.magnitude < 0.65f || Vector3.Angle(head.forward, direction) > 55f)
            {
                var upright = Quaternion.Euler(0f, head.eulerAngles.y, 0f);
                center = head.position + upright * new Vector3(0.25f, -0.12f, 1.15f);
                direction = center - head.position;
            }
            VrPanelPlacement.Place(rect, eye, center, 1.375f);
            root.GetComponent<RawImage>().texture = texture;
            relay = root.AddComponent<VrToolkitInputRelay>();
            relay.Bind(document, rect);
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            pointer?.RegisterWorldCanvas(canvas);
            VrDiagnostics.Mark("radio panel bound", "position=" + rect.position + ", document=" + document.name + ", panel=" + (document.rootVisualElement?.panel != null));
        }

        private void Release()
        {
            volumeLayout?.Dispose(); volumeLayout = null;
            if (relay != null) relay.Cancel();
            if (backdrop != null) backdrop.style.backgroundColor = originalBackdrop;
            backdrop = null;
            if (document != null && document.panelSettings == settings) document.panelSettings = originalSettings;
            photoExclusion?.Dispose();
            photoExclusion = null;
            if (canvas != null) { canvas.gameObject.SetActive(false); DisposeObject(canvas.gameObject); }
            if (settings != null) DisposeObject(settings);
            if (texture != null) { texture.Release(); DisposeObject(texture); }
            canvas = null; settings = null; texture = null; document = null; originalSettings = null; view = null; relay = null;
        }

        private static void DisposeObject(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
