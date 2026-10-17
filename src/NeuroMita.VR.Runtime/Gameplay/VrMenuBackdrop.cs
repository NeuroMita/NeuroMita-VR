using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal sealed class VrMenuBackdrop : IDisposable
    {
        private sealed class Layer
        {
            public Image Source;
            public bool Enabled;
            public SpriteRenderer Renderer;
        }

        private readonly List<Layer> layers = new();
        private readonly GameObject root;
        private readonly Sprite solid;
        private bool disposed;

        public VrMenuBackdrop(Canvas canvas, Transform parent)
        {
            root = VrObject.Create("VR menu scenery");
            root.transform.SetParent(parent, false);
            var texture = Texture2D.whiteTexture;
            solid = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.one * 0.5f, 100f);
            foreach (var source in canvas.GetComponentsInChildren<Image>(true))
            {
                var view = VrObject.Create(source.name, typeof(SpriteRenderer));
                view.transform.SetParent(root.transform, false);
                view.layer = source.gameObject.layer;
                var renderer = view.GetComponent<SpriteRenderer>();
                renderer.sortingOrder = canvas.sortingOrder;
                layers.Add(new Layer { Source = source, Enabled = source.enabled, Renderer = renderer });
                source.enabled = false;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (disposed) return;
            foreach (var layer in layers)
            {
                if (layer.Source == null)
                {
                    layer.Renderer.enabled = false;
                    continue;
                }
                var source = layer.Source;
                source.enabled = false;
                var rect = source.rectTransform;
                var sprite = source.sprite != null ? source.sprite : solid;
                var scale = rect.lossyScale;
                layer.Renderer.sprite = sprite;
                layer.Renderer.color = source.color;
                layer.Renderer.enabled = layer.Enabled && source.gameObject.activeInHierarchy;
                layer.Renderer.transform.SetPositionAndRotation(rect.TransformPoint(rect.rect.center), rect.rotation);
                layer.Renderer.transform.localScale = new Vector3(
                    rect.rect.width * Mathf.Abs(scale.x) / Mathf.Max(0.0001f, sprite.bounds.size.x),
                    rect.rect.height * Mathf.Abs(scale.y) / Mathf.Max(0.0001f, sprite.bounds.size.y), 1f);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var layer in layers)
                if (layer.Source != null) layer.Source.enabled = layer.Enabled;
            layers.Clear();
            if (root != null) root.SetActive(false);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(solid);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(solid);
            }
        }
    }
}
