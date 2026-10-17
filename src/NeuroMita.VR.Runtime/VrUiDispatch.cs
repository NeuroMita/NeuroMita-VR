using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Il2CppInterop.Runtime.InteropTypes;

namespace NeuroMita.VR
{
    internal static class VrUiDispatch
    {
        private static bool Supports<T>() => typeof(T) == typeof(IPointerDownHandler) || typeof(T) == typeof(IPointerUpHandler) ||
            typeof(T) == typeof(IPointerEnterHandler) || typeof(T) == typeof(IPointerExitHandler) || typeof(T) == typeof(IPointerMoveHandler) ||
            typeof(T) == typeof(IBeginDragHandler) || typeof(T) == typeof(IDragHandler) || typeof(T) == typeof(IScrollHandler);

        public static bool Execute<T>(GameObject target, BaseEventData data, ExecuteEvents.EventFunction<T> handler) where T : Il2CppObjectBase
        {
            if (target == null) return false;
            var relay = Supports<T>() ? target.GetComponent<VrToolkitInputRelay>() : null;
            if (relay == null) return ExecuteEvents.Execute(target, data, handler);
            var pointer = data.Cast<PointerEventData>();
            if (typeof(T) == typeof(IPointerDownHandler)) relay.OnPointerDown(pointer);
            else if (typeof(T) == typeof(IPointerUpHandler)) relay.OnPointerUp(pointer);
            else if (typeof(T) == typeof(IPointerEnterHandler)) relay.OnPointerEnter(pointer);
            else if (typeof(T) == typeof(IPointerExitHandler)) relay.OnPointerExit(pointer);
            else if (typeof(T) == typeof(IPointerMoveHandler)) relay.OnPointerMove(pointer);
            else if (typeof(T) == typeof(IBeginDragHandler)) relay.OnBeginDrag(pointer);
            else if (typeof(T) == typeof(IDragHandler)) relay.OnDrag(pointer);
            else if (typeof(T) == typeof(IScrollHandler)) relay.OnScroll(pointer);
            return true;
        }

        public static GameObject ExecuteHierarchy<T>(GameObject target, BaseEventData data, ExecuteEvents.EventFunction<T> handler) where T : Il2CppObjectBase
        {
            if (target == null) return null;
            for (var current = target.transform; current != null; current = current.parent)
                if (Execute(current.gameObject, data, handler)) return current.gameObject;
            return null;
        }

        public static GameObject GetEventHandler<T>(GameObject target) where T : Il2CppObjectBase
        {
            if (target != null && Supports<T>())
                for (var current = target.transform; current != null; current = current.parent)
                {
                    if (current.GetComponent<VrToolkitInputRelay>() != null) return current.gameObject;
                    if (ExecuteEvents.CanHandleEvent<T>(current.gameObject)) return current.gameObject;
                }
            return ExecuteEvents.GetEventHandler<T>(target);
        }
    }
}
