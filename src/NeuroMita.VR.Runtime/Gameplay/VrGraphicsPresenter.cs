using NeuroMita.Settings;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace NeuroMita.VR
{
    public readonly struct VrGraphicsProfile
    {
        public readonly int Msaa, Mipmap;
        public readonly ShadowResolution Shadows;
        public readonly float ShadowDistance, ResolutionScale;
        public readonly bool DisablePostProcessing;

        public VrGraphicsProfile(int msaa, int mipmap, ShadowResolution shadows, float distance, float scale, bool disablePost)
        {
            Msaa = msaa;
            Mipmap = mipmap;
            Shadows = shadows;
            ShadowDistance = distance;
            ResolutionScale = scale;
            DisablePostProcessing = disablePost;
        }
    }

    public static class VrGraphicsPolicy
    {
        public static VrGraphicsProfile Resolve(int preset) => preset switch
        {
            1 => new VrGraphicsProfile(2, 1, ShadowResolution.Low, 20f, 0.85f, true),
            2 => new VrGraphicsProfile(4, 0, ShadowResolution.Medium, 35f, 1f, true),
            _ => new VrGraphicsProfile(4, 0, ShadowResolution.High, 50f, 1.1f, false)
        };
    }

#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10040)]
#endif
    public sealed partial class VrGraphicsPresenter : MonoBehaviour
    {
        private GameSettingsService settings;
        private XRDisplaySubsystem display;
        private PostProcessLayer layer;
        private bool layerEnabled, layerSuppressed, captured;
        private int aa, mipmap, vsync;
        private ShadowResolution shadows;
        private float distance, scale;

        private void LateUpdate()
        {
            var current = AppServices.GetOrNull<GameSettingsService>();
            if (current != settings)
            {
                if (settings != null) VrHostEvents.Settings(settings, SettingsChanged, false);
                settings = current;
                if (settings != null) VrHostEvents.Settings(settings, SettingsChanged, true);
            }
            Apply(VrRuntime.IsActive ? VrRuntime.Settings?.VrGraphicsPreset ?? 0 : 0);
            BindCamera(VrRuntime.Rig?.Eye);
        }

        private void SettingsChanged(GameSettingsChange change)
        {
            if (!captured) return;
            if ((change & GameSettingsChange.Antialiasing) != 0)
            {
                aa = QualitySettings.antiAliasing;
                mipmap = QualitySettings.globalTextureMipmapLimit;
            }
            if ((change & GameSettingsChange.ShadowQuality) != 0) shadows = QualitySettings.shadowResolution;
            if ((change & GameSettingsChange.VSync) != 0) vsync = QualitySettings.vSyncCount;
        }

        public void Apply(int preset)
        {
            preset = Mathf.Clamp(preset, 0, 3);
            if (preset == 0)
            {
                RestoreQuality();
                RestoreLayer();
                return;
            }
            if (!captured)
            {
                aa = QualitySettings.antiAliasing;
                mipmap = QualitySettings.globalTextureMipmapLimit;
                vsync = QualitySettings.vSyncCount;
                shadows = QualitySettings.shadowResolution;
                distance = QualitySettings.shadowDistance;
                display = VrRuntime.XrDisplay;
                scale = display != null ? display.scaleOfAllRenderTargets : 1f;
                captured = true;
            }
            var profile = VrGraphicsPolicy.Resolve(preset);
            QualitySettings.antiAliasing = profile.Msaa;
            QualitySettings.globalTextureMipmapLimit = profile.Mipmap;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowResolution = profile.Shadows;
            QualitySettings.shadowDistance = profile.ShadowDistance;
            if (display != null) display.scaleOfAllRenderTargets = profile.ResolutionScale;
            if (layer != null && profile.DisablePostProcessing)
            {
                layer.enabled = false;
                layerSuppressed = true;
            }
            else RestoreLayer();
        }

        private void BindCamera(Camera camera)
        {
            var next = camera != null ? camera.GetComponent<PostProcessLayer>() : null;
            if (next == layer)
            {
                ApplyPostProcessPolicy(VrRuntime.Settings?.VrGraphicsPreset ?? 0);
                return;
            }
            RestoreLayer();
            layer = next;
            if (layer != null)
            {
                layerEnabled = layer.enabled;
                ApplyPostProcessPolicy(VrRuntime.Settings?.VrGraphicsPreset ?? 0);
            }
        }

        private void ApplyPostProcessPolicy(int preset)
        {
            if (layer == null) return;
            if (preset != 0 && VrGraphicsPolicy.Resolve(preset).DisablePostProcessing)
            {
                layer.enabled = false;
                layerSuppressed = true;
            }
            else RestoreLayer();
        }

        private void RestoreLayer()
        {
            if (layer != null && layerSuppressed) layer.enabled = layerEnabled;
            layerSuppressed = false;
        }

        private void RestoreQuality()
        {
            if (!captured) return;
            QualitySettings.antiAliasing = aa;
            QualitySettings.globalTextureMipmapLimit = mipmap;
            QualitySettings.vSyncCount = vsync;
            QualitySettings.shadowResolution = shadows;
            QualitySettings.shadowDistance = distance;
            if (display != null) display.scaleOfAllRenderTargets = scale;
            display = null;
            captured = false;
        }

        public void Restore()
        {
            RestoreQuality();
            RestoreLayer();
            layer = null;
            if (settings != null) VrHostEvents.Settings(settings, SettingsChanged, false);
            settings = null;
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();
    }
}
