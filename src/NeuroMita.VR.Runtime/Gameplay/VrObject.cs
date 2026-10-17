using System;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrObject
    {
        public static GameObject Create(string name = "GameObject", params Type[] components)
        {
            var root = new GameObject(name);
            foreach (var component in components)
            {
#if NEUROMITA_VR_IL2CPP
                root.AddComponent(Il2CppInterop.Runtime.Il2CppType.From(component));
#else
                root.AddComponent(component);
#endif
            }
            return root;
        }
    }

    internal static class VrHostLease
    {
#if NEUROMITA_VR_IL2CPP
        private sealed class Lease : IDisposable
        {
            private Il2CppSystem.IDisposable value;
            public Lease(Il2CppSystem.IDisposable source) => value = source;
            public void Dispose()
            {
                var previous = value;
                value = null;
                previous?.Dispose();
            }
        }
        public static IDisposable Wrap(Il2CppSystem.IDisposable value) => value != null ? new Lease(value) : null;
#else
        public static IDisposable Wrap(IDisposable value) => value;
#endif
    }

    internal static class VrFrameHooks
    {
#if NEUROMITA_VR_IL2CPP
        private static Action callbacks;
        private static readonly Action<Camera> pump = _ => callbacks?.Invoke();
#endif
        public static event Action BeforeRender
        {
            add
            {
#if NEUROMITA_VR_IL2CPP
                if (callbacks == null) VrHostEvents.Camera(pump, true, true);
                callbacks += value;
#else
                Application.onBeforeRender += new UnityEngine.Events.UnityAction(value.Invoke);
#endif
            }
            remove
            {
#if NEUROMITA_VR_IL2CPP
                callbacks -= value;
                if (callbacks == null) VrHostEvents.Camera(pump, true, false);
#else
                Application.onBeforeRender -= new UnityEngine.Events.UnityAction(value.Invoke);
#endif
            }
        }
    }

#if NEUROMITA_VR_IL2CPP
    internal static class VrNativeCallbackCache<T> where T : Il2CppSystem.Delegate
    {
        private static readonly System.Collections.Generic.Dictionary<Delegate, T> callbacks = new();
        public static T Get(Delegate handler)
        {
            if (!callbacks.TryGetValue(handler, out var callback))
                callbacks[handler] = callback = Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<T>(handler);
            return callback;
        }
        public static void Release(Delegate handler) => callbacks.Remove(handler);
    }
#endif
}