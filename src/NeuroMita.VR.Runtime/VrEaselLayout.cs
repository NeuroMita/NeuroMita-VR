using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    internal sealed class VrEaselLayout : System.IDisposable
    {
        private sealed class Placement
        {
            internal VisualElement Element;
            internal StyleLength Left, Right, Top, Bottom, Width;
            internal Placement(VisualElement element)
            {
                Element = element;
                var style = element.style;
                Left = style.left; Right = style.right; Top = style.top; Bottom = style.bottom; Width = style.width;
            }
            internal void Restore()
            {
                if (Element?.panel == null) return;
                var style = Element.style;
                style.left = Left; style.right = Right; style.top = Top; style.bottom = Bottom; style.width = Width;
            }
        }
        private readonly Placement header, tools;
        private readonly EaselInteractionUI owner;
        private static VrEaselLayout current;
        internal static bool TryApply(EaselInteractionUI view)
        {
            if (!VrRuntime.IsActive || current?.owner != view) return false;
            float width = view._document?.rootVisualElement?.contentRect.width ?? 0f;
            if (width > 24f) current.Refresh(width);
            return true;
        }
        internal VrEaselLayout(EaselInteractionUI view)
        {
            owner = view;
            current = this;
            if (view._headerElement != null) header = new Placement(view._headerElement);
            if (view._toolboxElement != null) tools = new Placement(view._toolboxElement);
        }
        internal void Refresh(float panelWidth)
        {
            if (float.IsNaN(panelWidth) || float.IsInfinity(panelWidth) || panelWidth <= 24f) return;
            if (header != null)
            {
                var style = header.Element.style;
                style.left = 12f; style.right = StyleKeyword.Auto; style.top = 12f;
                style.width = panelWidth - 24f;
            }
            if (tools != null)
            {
                var style = tools.Element.style;
                style.left = 12f; style.right = StyleKeyword.Auto; style.top = 120f;
                style.bottom = 12f; style.width = panelWidth - 24f;
            }
        }
        public void Dispose() { if (current == this) current = null; header?.Restore(); tools?.Restore(); }
    }
}
