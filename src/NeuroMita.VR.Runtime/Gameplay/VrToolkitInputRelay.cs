using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    public sealed partial class VrToolkitInputRelay : MonoBehaviour
#if !NEUROMITA_VR_IL2CPP
        , IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IBeginDragHandler, IDragHandler, IScrollHandler
#endif
    {
        private UIDocument document;
        private RectTransform rect;
        private Vector2 position;
        private bool pressed;
        private bool ownsPointer;

        public void Bind(UIDocument target, RectTransform surface) { document = target; rect = surface; }

        public static Vector2 ToPanelPosition(Rect bounds, Vector2 local, Vector2 panelSize)
            => new Vector2((local.x - bounds.xMin) / bounds.width * panelSize.x,
                (bounds.yMax - local.y) / bounds.height * panelSize.y);

        private bool Project(PointerEventData data)
        {
            if (document?.rootVisualElement?.panel == null || rect == null) return false;
            var camera = data.pressEventCamera != null ? data.pressEventCamera : data.enterEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, camera, out var local)) return false;
            Vector2 size = document.rootVisualElement.layout.size;
            if (size.x <= 0f || size.y <= 0f || float.IsNaN(size.x) || float.IsNaN(size.y)) return false;
            position = ToPanelPosition(rect.rect, local, size);
            return true;
        }

        private void Send(EventType type, Vector2 delta = default, bool cancel = false)
        {
            var root = document?.rootVisualElement;
            if (root?.panel == null) return;
#if NEUROMITA_VR_IL2CPP
            var target = root.panel.Cast<BaseVisualElementPanel>().Pick(position) ?? root;
#else
            var target = root.panel.Pick(position) ?? root;
#endif
#if NEUROMITA_VR_IL2CPP
            var handler = target.Cast<IEventHandler>();
#else
            IEventHandler handler = target;
#endif
            var input = new Event { type = type, mousePosition = position, button = 0, clickCount = 1, delta = delta };
            switch (type)
            {
                case EventType.MouseDown:
                    { var evt = PointerDownEvent.GetPooled(input); try { evt.target = handler; target.SendEvent(evt); } finally { evt.Dispose(); } }
                    break;
                case EventType.MouseUp:
                    if (cancel)
                    {
                        { var evt = PointerCancelEvent.GetPooled(input); try { evt.target = handler; target.SendEvent(evt); } finally { evt.Dispose(); } }
                    }
                    else
                    {
                        { var evt = PointerUpEvent.GetPooled(input); try { evt.target = handler; target.SendEvent(evt); } finally { evt.Dispose(); } }
                    }
                    break;
                case EventType.ScrollWheel:
                    { var evt = WheelEvent.GetPooled(input); try { evt.target = handler; target.SendEvent(evt); } finally { evt.Dispose(); } }
                    break;
                default:
                    { var evt = PointerMoveEvent.GetPooled(input); try { evt.target = handler; target.SendEvent(evt); } finally { evt.Dispose(); } }
                    break;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !Project(data)) return;
            pressed = true;
            ownsPointer = true;
            Send(EventType.MouseDown);
        }
        public void OnPointerUp(PointerEventData data)
        {
            if (!pressed) return;
            if ((!data.eligibleForClick && !data.dragging) || !Project(data)) { Cancel(); return; }
            pressed = false;
            Send(EventType.MouseUp);
            ownsPointer = false;
        }
        public void OnPointerEnter(PointerEventData data) { if (Project(data)) Send(EventType.MouseMove); }
        public void OnPointerMove(PointerEventData data) { if (Project(data)) Send(EventType.MouseMove); }
        public void OnPointerExit(PointerEventData data) { if (pressed) return; position = new Vector2(-10000f, -10000f); Send(EventType.MouseMove); }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data) { if (pressed && Project(data)) Send(EventType.MouseDrag); }
        public void OnScroll(PointerEventData data) { if (Project(data)) Send(EventType.ScrollWheel, -data.scrollDelta * 18f); }
        public void Cancel()
        {
            if (!ownsPointer) return;
            if (pressed) { pressed = false; Send(EventType.MouseUp, default, true); }
            document?.rootVisualElement?.panel?.ReleasePointer(0);
            ownsPointer = false;
        }
        private void OnDisable() => Cancel();
    }
}
