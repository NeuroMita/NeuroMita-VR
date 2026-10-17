using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    public sealed partial class VrUiPresenter
    {
        private Canvas hitMarker;
        private Texture2D hitTexture;
        private Material hitMaterial;
        private System.IDisposable hitExclusion;

        private void ShowHitMarker(bool visible, Vector3 point, Ray ray, bool held)
        {
            if (!visible)
            {
                if (hitMarker != null) hitMarker.gameObject.SetActive(false);
                return;
            }
            if (hitMarker == null)
            {
                var root = VrObject.Create("VR pointer hit", typeof(RectTransform), typeof(Canvas), typeof(RawImage));
                root.layer = LayerMask.NameToLayer("UI");
                root.transform.SetParent(transform, false);
                hitMarker = root.GetComponent<Canvas>();
                hitMarker.renderMode = RenderMode.WorldSpace;
                hitMarker.overrideSorting = true;
                hitMarker.sortingOrder = 32760;
                root.GetComponent<RectTransform>().sizeDelta = Vector2.one * 0.02f;
                var image = root.GetComponent<RawImage>();
                image.raycastTarget = false;
                hitTexture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                hitTexture.name = "VR cursor ring";
                var pixels = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color>(32 * 32);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        float radius = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f));
                        pixels[y * 32 + x] = radius > 16f ? Color.clear : radius > 12f ? Color.black : Color.white;
                    }
                hitTexture.SetPixels(pixels);
                hitTexture.Apply();
                image.texture = hitTexture;
                var shader = Shader.Find("UI/Default");
                if (shader != null)
                {
                    hitMaterial = new Material(shader);
                    hitMaterial.SetInt("unity_GUIZTestMode", (int)UnityEngine.Rendering.CompareFunction.Always);
                    hitMaterial.renderQueue = 4990;
                    image.material = hitMaterial;
                }
                hitExclusion?.Dispose();
                hitExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(hitMarker));
            }
            hitMarker.gameObject.SetActive(true);
            hitMarker.worldCamera = rig.Eye;
            hitMarker.transform.SetPositionAndRotation(point - ray.direction * 0.015f,
                Quaternion.LookRotation(ray.direction, Vector3.up));
            hitMarker.GetComponent<RawImage>().color = held ? new Color(0.2f, 1f, 1f, 1f) : Color.white;
        }

        private void ReleaseHitMarker()
        {
            hitExclusion?.Dispose(); hitExclusion = null;
            if (hitMarker != null) Destroy(hitMarker.gameObject);
            if (hitTexture != null) Destroy(hitTexture);
            if (hitMaterial != null) Destroy(hitMaterial);
            hitMarker = null; hitTexture = null; hitMaterial = null;
        }
    }
}
