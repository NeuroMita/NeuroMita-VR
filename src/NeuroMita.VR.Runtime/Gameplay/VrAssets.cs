using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrAssets
    {
#if NEUROMITA_VR_IL2CPP
        private static AssetBundle bundle;
        private static bool attempted;
#endif
        public static void Validate()
        {
            if (Load<Mesh>("NeuroMitaVrLeftHand") == null || Load<Mesh>("NeuroMitaVrRightHand") == null ||
                Load<Material>("NeuroMitaVrPointer") == null || Load<Material>("NeuroMitaVrComfort") == null)
                throw new System.InvalidOperationException("VR mod bundle is missing required meshes/materials. Install the complete matching package.");
        }
        public static T Load<T>(string name) where T : UnityEngine.Object
        {
#if NEUROMITA_VR_IL2CPP
            if (!attempted)
            {
                attempted = true;
                var directory = System.IO.Path.GetDirectoryName(typeof(VrAssets).Assembly.Location);
                bundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(directory, "neuromita-vr-assets"));
                if (bundle == null) Debug.LogError("[NeuroMita VR] Required mod asset bundle is missing or incompatible.");
            }
            if (bundle != null)
            {
                var asset = bundle.LoadAsset(name, Il2CppInterop.Runtime.Il2CppType.Of<T>());
                if (asset != null) return asset.Cast<T>();
            }
#endif
            return Resources.Load<T>(name);
        }
    }
}