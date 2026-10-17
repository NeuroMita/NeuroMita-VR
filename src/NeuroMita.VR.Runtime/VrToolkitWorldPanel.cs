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
        private Material depthMaterial;
        private readonly RenderTexture texture;
        private readonly VrToolkitInputRelay relay;
        private readonly IDisposable photoExclusion;
        public Canvas Canvas { get; private set; }

        public VrToolkitWorldPanel(UIDocument source, Transform parent, Camera eye, VrUiPresenter pointer, string title = "YouTube browser", int pixelWidth = 1600, int pixelHeight = 1000, float width = 1.4f, float pixelScale = 1f)
        {
            if (source == null || source.panelSettings == null) throw new ArgumentException("Toolkit document has no PanelSettings");
            document = source;
            original = source.panelSettings;
            settings = UnityEngine.Object.Instantiate(original);
            settings.name = "VR " + title + " panel settings";
            texture = new RenderTexture(pixelWidth, pixelHeight, 24, RenderTextureFormat.ARGB32);
            texture.name = "VR " + title + " UI";
            try
            {
                if (!texture.Create()) throw new InvalidOperationException(title + " RenderTexture creation failed");
                settings.renderMode = PanelRenderMode.ScreenSpaceOverlay;
                settings.targetTexture = texture;
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                settings.scale = pixelScale;
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
                rect.sizeDelta = new Vector2(pixelWidth, pixelHeight);
                rect.localScale = Vector3.one * (width / pixelWidth);
                var facing = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
                VrPanelPlacement.Place(rect, eye, eye.transform.position + facing * new Vector3(-0.4f, -0.1f, 1.3f), width);
                root.GetComponent<RawImage>().texture = texture;
                relay = root.AddComponent<VrToolkitInputRelay>();
                relay.Bind(document, rect);
                photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(Canvas));
                pointer?.RegisterWorldCanvas(Canvas);
            }
            catch { Dispose(); throw; }
        }

        internal void EnableDepthRendering()
        {
            if (Canvas == null) return;
            var shader = Shader.Find("UI/Default");
            if (shader == null) throw new InvalidOperationException("UI/Default is unavailable for TV depth rendering");
            depthMaterial = new Material(shader);
            depthMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            depthMaterial.renderQueue = 3000;
            Canvas.GetComponent<RawImage>().material = depthMaterial;
            Canvas.sortingOrder = 0;
        }

        internal void MountOnDocument(Camera eye)
        {
            if (Canvas == null || document == null) return;
            var rect = Canvas.GetComponent<RectTransform>();
            if (rect.parent != document.transform) rect.SetParent(document.transform, false);
            float pixelsPerUnit = Mathf.Max(1f, original.referenceSpritePixelsPerUnit);
            Vector2 size = document.worldSpaceSize / pixelsPerUnit;
            if (size.x <= 0.01f || size.y <= 0.01f) size = new Vector2(1.6f, 0.9f);
            rect.localPosition = new Vector3(0f, 0f, -0.01f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(size.x / rect.sizeDelta.x, size.y / rect.sizeDelta.y, 1f);
            Vector3 towardEye = eye.transform.position - rect.position;
            if (Vector3.Dot(rect.forward, towardEye) > 0f) rect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rect.position += towardEye.normalized * 0.005f;
        }

        internal void Place(Camera eye, Vector3 center, float width)
        {
            if (Canvas != null) VrPanelPlacement.Place(Canvas.GetComponent<RectTransform>(), eye, center, width);
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
            if (depthMaterial != null) UnityEngine.Object.Destroy(depthMaterial);
        }
    }
}
