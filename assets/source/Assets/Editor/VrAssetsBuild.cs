using System.IO;
using UnityEditor;
using UnityEngine;

namespace NeuroMita.VR.Editor
{
    public static class VrAssetsBuild
    {
        [MenuItem("NeuroMita VR/Build Windows AssetBundle")]
        public static string Build()
        {
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../prebuilt"));
            Directory.CreateDirectory(output);
            var build = new AssetBundleBuild
            {
                assetBundleName = "neuromita-vr-assets",
                assetNames = new[]
                {
                    "Assets/NeuroMitaVR/Materials/NeuroMitaVrComfort.mat",
                    "Assets/NeuroMitaVR/Meshes/NeuroMitaVrLeftHand.asset",
                    "Assets/NeuroMitaVR/Materials/NeuroMitaVrPointer.mat",
                    "Assets/NeuroMitaVR/Meshes/NeuroMitaVrRightHand.asset"
                },
                addressableNames = new[] { "NeuroMitaVrComfort", "NeuroMitaVrLeftHand", "NeuroMitaVrPointer", "NeuroMitaVrRightHand" }
            };
            var manifest = BuildPipeline.BuildAssetBundles(output, new[] { build },
                BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows64);
            if (manifest == null) throw new System.InvalidOperationException("VR asset bundle build failed.");
            string result = Path.Combine(output, build.assetBundleName);
            Debug.Log("VR asset bundle built: " + result);
            return result;
        }
    }
}
