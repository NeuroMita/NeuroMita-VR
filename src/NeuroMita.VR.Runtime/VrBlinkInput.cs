using System;
using System.Reflection;
using HarmonyLib;
using NeuroMita.Player.Controllers;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrBlinkInput
    {
        private static Harmony hooks;
        internal static bool CapturesHelp => hooks != null && VrRuntime.IsActive && VrRuntime.Rig?.Player?.Blink?.MechanicActive == true;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.BlinkInput");
            try
            {
                var target = typeof(PlayerBlinkController).GetMethod("HandleInput", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (target == null) throw new MissingMethodException("PlayerBlinkController.HandleInput");
                candidate.Patch(target, prefix: new HarmonyMethod(typeof(VrBlinkInput).GetMethod(nameof(BeforeInput), BindingFlags.Static | BindingFlags.NonPublic)));
                foreach (var name in new[] { "GetDescription", "GetKey" })
                {
                    var hint = typeof(BlinkHintPresenter).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (hint == null) throw new MissingMethodException("BlinkHintPresenter." + name);
                    candidate.Patch(hint, prefix: new HarmonyMethod(typeof(VrBlinkInput).GetMethod(name + "Prefix", BindingFlags.Static | BindingFlags.NonPublic)));
                }
                hooks = candidate;
                Debug.Log("[NeuroMita VR] Installed manual blink and controller hint hooks");
            }
            catch (Exception error)
            {
                candidate.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] Manual blink adapter unavailable: " + error);
            }
        }
        private static bool BeforeInput(PlayerBlinkController __instance)
        {
            if (!VrRuntime.IsActive || VrRuntime.Rig?.Player?.Blink == null || VrRuntime.Rig.Player.Blink.Pointer != __instance.Pointer) return true;
            var player = VrRuntime.Rig.Player;
            if (!__instance.CanManualBlink || player.InputLocked || Time.timeScale <= 0f || VrQteInput.HasActiveQte
                || player.Sequence?.IsPlaying == true || VrRuntime.Input?.HelpDown != true
                || AppServices.GetOrNull<InputManager>()?.CurrentState != InputManager.InputState.Gameplay) return false;
            __instance._manualBlinkTriggered = true;
            __instance._cooldown = __instance.manualBlinkCooldown;
            return false;
        }
        private static bool GetDescriptionPrefix(ref string __result)
        {
            if (!VrRuntime.IsActive) return true;
            __result = VrSettingsText.Pick("Blink — click right stick", "Моргнуть — нажмите правый стик");
            return false;
        }
        private static bool GetKeyPrefix(ref KeyCode __result)
        {
            if (!VrRuntime.IsActive) return true;
            __result = KeyCode.None;
            return false;
        }
        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; }
    }
}
