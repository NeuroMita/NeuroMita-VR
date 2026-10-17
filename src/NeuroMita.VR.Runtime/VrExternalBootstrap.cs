using System;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrExternalBootstrap : MonoBehaviour
    {
        private bool requested;
        public VrExternalBootstrap(IntPtr pointer) : base(pointer) { }
        public static void Schedule()
        {
            var root = VrObject.Create("NeuroMita external VR bootstrap");
            DontDestroyOnLoad(root);
            root.AddComponent<VrExternalBootstrap>();
        }
        private void Update()
        {
            if (requested || Time.realtimeSinceStartup < 5f) return;
            requested = true;
            try { VrRuntime.StartVr(); }
            catch (Exception exception) { Debug.LogError(exception.ToString()); }
            Destroy(gameObject);
        }
    }
}