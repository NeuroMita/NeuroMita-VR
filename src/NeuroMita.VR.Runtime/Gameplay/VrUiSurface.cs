using System;
using NeuroMita.Modding;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal sealed class VrUiSurface : IDisposable
    {
        public RectTransform View { get; }
        public Canvas Canvas { get; }
        public RectTransform Rect => Canvas != null ? Canvas.GetComponent<RectTransform>() : null;
        private readonly Transform parent;
        private readonly int sibling;
        private readonly Vector2 min, max, pivot, size, anchored;
        private readonly Vector3 position, scale;
        private readonly Quaternion rotation;
        private readonly IDisposable ownership;
        private readonly IDisposable photoExclusion;
#if NEUROMITA_VR_IL2CPP
        private readonly Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> corners = new(4);
#else
        private readonly Vector3[] corners = new Vector3[4];
#endif
        private bool disposed;

        public VrUiSurface(RectTransform view, Camera eye, VrUiPresenter pointer)
        {
            View = view;
            parent = view.parent;
            sibling = view.GetSiblingIndex();
            min = view.anchorMin; max = view.anchorMax; pivot = view.pivot;
            size = view.sizeDelta; anchored = view.anchoredPosition;
            position = view.localPosition; scale = view.localScale; rotation = view.localRotation;
            Vector2 resolvedSize = view.rect.size;
            ownership = VrHostLease.Wrap(ExternalUiLayout.Acquire(view));
            var wrapper = VrObject.Create("VR surface: " + view.name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            wrapper.layer = view.gameObject.layer;
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetSiblingIndex(sibling);
            Canvas = wrapper.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.WorldSpace;
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(Canvas));
            Canvas.worldCamera = eye;
            Canvas.overrideSorting = true;
            Canvas.sortingOrder = 500;
            Rect.anchorMin = Rect.anchorMax = Rect.pivot = Vector2.one * 0.5f;
            view.SetParent(wrapper.transform, false);
            view.anchorMin = view.anchorMax = Vector2.one * 0.5f;
            view.sizeDelta = resolvedSize;
            view.localRotation = Quaternion.identity;
            view.localScale = Vector3.one;
            RefreshGeometry();
            pointer?.RegisterWorldCanvas(Canvas);
        }

        public void RefreshGeometry()
        {
            if (View == null || Canvas == null) return;
            Bounds bounds = default;
            bool found = false;
            foreach (var graphic in View.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.rectTransform.rect.width <= 0f || graphic.rectTransform.rect.height <= 0f) continue;
                graphic.rectTransform.GetWorldCorners(corners);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 local = View.InverseTransformPoint(corners[i]);
                    if (!found) { bounds = new Bounds(local, Vector3.zero); found = true; }
                    else bounds.Encapsulate(local);
                }
            }
            if (!found) bounds = new Bounds(View.rect.center, View.rect.size);
            Rect.sizeDelta = new Vector2(Mathf.Max(16f, bounds.size.x + 16f), Mathf.Max(16f, bounds.size.y + 16f));
            View.localPosition = -bounds.center;
        }

        public void Place(Vector3 worldPosition, Quaternion worldRotation, float widthMeters, Camera eye)
        {
            if (Canvas == null || View == null) return;
            Canvas.worldCamera = eye;
            float desiredScale = widthMeters / Mathf.Max(1f, Rect.rect.width);
            Vector3 parentScale = parent != null ? parent.lossyScale : Vector3.one;
            Rect.localScale = new Vector3(desiredScale / Mathf.Max(0.000001f, Mathf.Abs(parentScale.x)),
                desiredScale / Mathf.Max(0.000001f, Mathf.Abs(parentScale.y)),
                desiredScale / Mathf.Max(0.000001f, Mathf.Abs(parentScale.z)));
            Rect.SetPositionAndRotation(worldPosition, worldRotation);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            photoExclusion?.Dispose();
            if (Canvas != null)
            {
                Canvas.enabled = false;
                Canvas.transform.SetParent(null, false);
            }
            if (View != null)
            {
                View.SetParent(parent, false);
                View.SetSiblingIndex(sibling);
                View.anchorMin = min; View.anchorMax = max; View.pivot = pivot;
                View.sizeDelta = size; View.anchoredPosition = anchored;
                View.localPosition = position; View.localRotation = rotation; View.localScale = scale;
            }
            ownership.Dispose();
            if (Canvas != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(Canvas.gameObject);
                else UnityEngine.Object.DestroyImmediate(Canvas.gameObject);
            }
        }
    }
}
