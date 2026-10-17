using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using NeuroMita.Player;
using NeuroMita.Player.Controllers;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrWorldInteractionHooks
    {
        private static Harmony hooks;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.WorldInteractions");
            try
            {
                Patch(candidate, typeof(PlayerBlockHub).GetMethod("Acquire", Flags), nameof(AcquirePrefix));
                Patch(candidate, typeof(PlayerInputController).GetMethod("Tick", Flags), nameof(InputPostfix), true);
                Patch(candidate, typeof(PlayerInteractionController).GetMethod("Tick", Flags), nameof(InteractionPrefix));
                var camera = typeof(PlayerCameraController).GetMethods(Flags).First(m => m.Name == "BeginExternalOverride" && m.GetParameters()[0].ParameterType == typeof(Vector3));
                Patch(candidate, camera, nameof(CameraPrefix));
                var zoom = typeof(TVDeviceController).GetMethod("ApplyZoomToCamera", Flags);
                Patch(candidate, zoom, nameof(ZoomPrefix));
                Patch(candidate, zoom, nameof(ZoomPostfix), true);
                Patch(candidate, typeof(TVDeviceController).GetMethod("BeginSession", Flags), nameof(TvSessionPostfix), true);
                hooks = candidate;
            }
            catch (Exception error)
            {
                candidate.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] World interaction hooks unavailable: " + error);
            }
        }
        private static void Patch(Harmony candidate, MethodInfo method, string name, bool postfix = false)
        {
            if (method == null) throw new MissingMethodException(name);
            var hook = new HarmonyMethod(typeof(VrWorldInteractionHooks).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static));
            if (postfix) candidate.Patch(method, postfix: hook); else candidate.Patch(method, prefix: hook);
        }
        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; }
        private static void AcquirePrefix(UnityEngine.Object owner, ref PlayerBlockMask mask, string reason)
        {
            if (!VrRuntime.IsActive || owner == null) return;
            if ((reason == "Easel_Drawing" && owner.TryCast<EaselController>() != null) ||
                (reason == "TV_YouTube_Mode" && owner.TryCast<TVInteractionController>() != null))
                mask &= ~(PlayerBlockMask.Move | PlayerBlockMask.Input);
        }
        private static void InputPostfix(PlayerInputController __instance) => VrWorldInteractionSession.ApplyMovement(__instance.player);
        private static bool CameraPrefix(PlayerCameraController __instance) => !VrWorldInteractionSession.HasOpenEasel(__instance.player);
        private static bool InteractionPrefix(PlayerInteractionController __instance)
        {
            if (!VrRuntime.IsActive || VrRuntime.Presenter?.IsPointingAtToolkit != true) return true;
            __instance.ClearTargetsAndHover();
            return false;
        }
        private static void ZoomPrefix(TVDeviceController __instance, out float __state)
        {
            __state = float.NaN;
            if (!VrRuntime.IsActive || __instance._sessionOwner?.TryCast<TVInteractionController>() == null) return;
            __state = __instance._sessionViewClearance;
            __instance._sessionViewClearance = Mathf.Max(__state, 0.65f);
        }
        private static void ZoomPostfix(TVDeviceController __instance, float __state)
        {
            if (!float.IsNaN(__state)) __instance._sessionViewClearance = __state;
        }
        private static void TvSessionPostfix(TVDeviceController __instance, bool __result)
        {
            if (__result && VrRuntime.IsActive && __instance._sessionOwner?.TryCast<TVInteractionController>() != null)
                __instance.ApplyZoomToCamera(__instance._sessionCurrentZoom, 0.2f);
        }
    }
}
