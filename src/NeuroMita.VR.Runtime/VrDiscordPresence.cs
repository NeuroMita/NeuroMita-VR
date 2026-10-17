using System;
using System.Reflection;
using HarmonyLib;
using MitaAI;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrDiscordPresence
    {
        private static Harmony harmony;
        public static void Install()
        {
            if (harmony != null) return;
            var hooks = new Harmony("NeuroMita.VR.DiscordPresence");
            try
            {
                var target = typeof(DiscordManager).GetMethod("BuildSnapshot", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(string), typeof(string), typeof(string), typeof(int), typeof(long) }, null);
                if (target == null) throw new MissingMethodException("DiscordManager.BuildSnapshot");
                hooks.Patch(target, prefix: new HarmonyMethod(typeof(VrDiscordPresence).GetMethod(nameof(BeforeSnapshot), BindingFlags.Static | BindingFlags.NonPublic)));
                harmony = hooks;
                DiscordManager.Instance?.MarkDirty();
            }
            catch (Exception exception)
            {
                hooks.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] Discord VR status unavailable: " + exception);
            }
        }
        private static void BeforeSnapshot(ref string state)
        {
            if (VrRuntime.IsActive) state = "NeuroMita (VR)";
        }
        public static void Remove()
        {
            harmony?.UnpatchSelf();
            harmony = null;
            DiscordManager.Instance?.MarkDirty();
        }
    }
}
