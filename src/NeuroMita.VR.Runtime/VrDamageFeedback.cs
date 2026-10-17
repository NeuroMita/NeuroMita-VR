using System;
using System.Reflection;
using HarmonyLib;
using NeuroMita.Player.Controllers;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrDamageFeedback
    {
        private static Harmony hooks;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.DamageFeedback");
            try
            {
                var method = typeof(PlayerHealthController).GetMethod("Damage", BindingFlags.Instance | BindingFlags.Public);
                if (method == null) throw new MissingMethodException("PlayerHealthController.Damage");
                candidate.Patch(method,
                    prefix: new HarmonyMethod(typeof(VrDamageFeedback).GetMethod(nameof(BeforeDamage), BindingFlags.NonPublic | BindingFlags.Static)),
                    postfix: new HarmonyMethod(typeof(VrDamageFeedback).GetMethod(nameof(AfterDamage), BindingFlags.NonPublic | BindingFlags.Static)));
                hooks = candidate;
                Debug.Log("[NeuroMita VR] Installed actual-damage feedback hooks");
            }
            catch (Exception error) { candidate.UnpatchSelf(); Debug.LogWarning("[NeuroMita VR] Damage feedback unavailable: " + error); }
        }
        private static void BeforeDamage(PlayerHealthController __instance, out int __state) => __state = __instance.Health;
        private static void AfterDamage(PlayerHealthController __instance, int __state)
        {
            var health = VrRuntime.Rig?.Player?.Health;
            if (VrRuntime.IsActive && health != null && health.Pointer == __instance.Pointer && __instance.Health < __state) VrHaptics.Damage();
        }
        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; }
    }
}
