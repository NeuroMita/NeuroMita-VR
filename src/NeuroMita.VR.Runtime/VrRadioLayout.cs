using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    internal sealed class VrRadioLayout : System.IDisposable
    {
        private sealed class Saved
        {
            internal VisualElement Element;
            internal StyleLength Width, Height, MinWidth;
        }
        private readonly List<Saved> saved = new();
        internal VrRadioLayout(VisualElement root)
        {
            Resize(root?.Q<Button>("volume-button"), 48f, 48f);
            Resize(root?.Q<VisualElement>(className: "radio-volume-shell"), 120f, 36f);
        }
        private void Resize(VisualElement element, float width, float height)
        {
            if (element == null) return;
            var style = element.style;
            saved.Add(new Saved { Element = element, Width = style.width, Height = style.height, MinWidth = style.minWidth });
            style.width = width; style.minWidth = width; style.height = height;
        }
        public void Dispose()
        {
            foreach (var entry in saved)
            {
                if (entry.Element?.panel == null) continue;
                var style = entry.Element.style;
                style.width = entry.Width; style.minWidth = entry.MinWidth; style.height = entry.Height;
            }
            saved.Clear();
        }
    }
}
