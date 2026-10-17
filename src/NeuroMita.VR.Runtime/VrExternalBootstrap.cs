using System;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrExternalBootstrap : MonoBehaviour
    {
        private static VrExternalBootstrap pending;
        private bool cancelled;
        public VrExternalBootstrap(IntPtr pointer) : base(pointer) { }
        public static void Schedule()
        {
            if (pending != null) return;
            var root = VrObject.Create("NeuroMita external VR bootstrap");
            DontDestroyOnLoad(root);
            pending = root.AddComponent<VrExternalBootstrap>();
        }
        public static void Cancel()
        {
            if (pending == null) return;
            pending.cancelled = true;
            Destroy(pending.gameObject);
            pending = null;
        }
        private void Update()
        {
            if (cancelled || Time.realtimeSinceStartup < 5f) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool menuReady = NeuroMita.Menu.MenuSceneResolver.ResolvePrimaryRoot(scene) != null;
            bool playerReady = AppServices.GetOrNull<GlobalReferences>()?.PlayerActor != null;
            if ((!menuReady && !playerReady) || SceneService.IsTransitionInProgress) return;
            cancelled = true;
            pending = null;
            try { VrRuntime.StartVr(); }
            catch (Exception exception) { Debug.LogError(exception.ToString()); }
            Destroy(gameObject);
        }
        private void OnDestroy() { if (pending == this) pending = null; }
    }
}