using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    internal sealed class VrToolkitWorldPanel : IDisposable
    {
        private bool disposed;
        private readonly UIDocument document;
        private readonly PanelSettings original, settings;
        private readonly RenderTexture texture;
        private readonly VrToolkitInputRelay relay;
        private readonly IDisposable photoExclusion;
        public Canvas Canvas { get; private set; }

        public VrToolkitWorldPanel(UIDocument source, Transform parent, Camera eye, VrUiPresenter pointer, string title = "YouTube browser")
        {
            if (source == null || source.panelSettings == null) throw new ArgumentException("Toolkit document has no PanelSettings");
            document = source;
            original = source.panelSettings;
            settings = UnityEngine.Object.Instantiate(original);
            settings.name = "VR " + title + " panel settings";
            texture = new RenderTexture(1600, 1000, 24, RenderTextureFormat.ARGB32);
            texture.name = "VR " + title + " UI";
            try
            {
                if (!texture.Create()) throw new InvalidOperationException(title + " RenderTexture creation failed");
                settings.renderMode = PanelRenderMode.ScreenSpaceOverlay;
                settings.targetTexture = texture;
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = 1f;
                settings.clearDepthStencil = true;
                settings.clearColor = true;
                settings.colorClearValue = Color.clear;
                source.panelSettings = settings;
                var root = VrObject.Create("VR " + title, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(RawImage));
                Canvas = root.GetComponent<Canvas>();
                root.transform.SetParent(parent, false);
                Canvas.renderMode = RenderMode.WorldSpace;
                Canvas.worldCamera = eye;
                Canvas.sortingOrder = 640;
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1600, 1000);
                rect.localScale = Vector3.one * (1.4f / 1600f);
                var facing = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
                VrPanelPlacement.Place(rect, eye, eye.transform.position + facing * new Vector3(-0.4f, -0.1f, 1.3f), 1.4f);
                root.GetComponent<RawImage>().texture = texture;
                relay = root.AddComponent<VrToolkitInputRelay>();
                relay.Bind(document, rect);
                photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(Canvas));
                pointer?.RegisterWorldCanvas(Canvas);
            }
            catch { Dispose(); throw; }
        }

        public void Refresh(Camera eye)
        {
            if (Canvas == null || document == null) return;
            Canvas.worldCamera = eye;
            if (document.panelSettings != settings) document.panelSettings = settings;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            relay?.Cancel();
            if (document != null && document.panelSettings == settings) document.panelSettings = original;
            photoExclusion?.Dispose();
            if (Canvas != null)
            {
                Canvas.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(Canvas.gameObject);
                Canvas = null;
            }
            if (texture != null) { texture.Release(); UnityEngine.Object.Destroy(texture); }
            if (settings != null) UnityEngine.Object.Destroy(settings);
        }
    }
}
