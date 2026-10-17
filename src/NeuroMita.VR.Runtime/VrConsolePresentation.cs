using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal sealed class VrConsolePresentation : System.IDisposable
    {
        private readonly Dictionary<Image, Color> backgrounds = new();
        private ConsoleInterface owner;
        private float nextDiscovery;
        internal static bool IsOpen => AppServices.GetOrNull<UIManager>()?.FocusedType?.FullName == typeof(ConsoleInterface).FullName;

        internal void Refresh()
        {
            if (!IsOpen) { Dispose(); return; }
            if (Time.unscaledTime >= nextDiscovery || owner == null)
            {
                nextDiscovery = Time.unscaledTime + 0.5f;
                owner = Object.FindFirstObjectByType<ConsoleInterface>();
                var rootImage = owner != null ? owner.GetComponent<Image>() : null;
                if (rootImage != null && !backgrounds.ContainsKey(rootImage)) backgrounds.Add(rootImage, rootImage.color);
                var panel = owner != null ? owner.transform.Find("MainPanel") : null;
                if (panel != null)
                {
                    var main = panel.GetComponent<RectTransform>();
                    float area = main != null ? main.rect.width * main.rect.height : 1f;
                    foreach (var image in panel.GetComponentsInChildren<Image>(true))
                    {
                        if (image == null || image.GetComponent<Selectable>() != null) continue;
                        var rect = image.GetComponent<RectTransform>();
                        Color color = image.color;
                        bool largeDark = rect != null && rect.rect.width * rect.rect.height > area * 0.2f &&
                            Mathf.Max(color.r, Mathf.Max(color.g, color.b)) < 0.25f;
                        if ((image.transform == panel || largeDark) && !backgrounds.ContainsKey(image)) backgrounds.Add(image, color);
                    }
                }
            }
            foreach (var image in backgrounds)
                if (image.Key != null)
                {
                    Color color = image.Key.color;
                    color.a = Mathf.Min(image.Value.a, 0.1f);
                    image.Key.color = color;
                }
        }

        public void Dispose()
        {
            foreach (var image in backgrounds) if (image.Key != null) image.Key.color = image.Value;
            backgrounds.Clear();
            owner = null;
        }
    }
}
