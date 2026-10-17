using System;
using System.Reflection;
using HarmonyLib;
using MitaAI;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrDialogueContext
    {
        private static Harmony hooks;
        private static bool failed;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.DialogueContext");
            try
            {
                var method = typeof(DialogueRuntimeContextBuffer).GetMethod("Capture", BindingFlags.Public | BindingFlags.Static);
                if (method == null) throw new MissingMethodException("DialogueRuntimeContextBuffer.Capture");
                candidate.Patch(method, postfix: new HarmonyMethod(typeof(VrDialogueContext).GetMethod(nameof(AfterCapture), BindingFlags.NonPublic | BindingFlags.Static)));
                hooks = candidate;
                Debug.Log("[NeuroMita VR] Installed dialogue capture context hooks");
            }
            catch (Exception error)
            {
                candidate.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] Dialogue context adapter unavailable: " + error);
            }
        }
        private static void AfterCapture(ref DialogueRuntimeContextBatch __result)
        {
            if (!VrRuntime.IsActive || __result == null || __result.Character == CharacterType.None) return;
            if (failed) return;
            try
            {
                var events = new Il2CppSystem.Collections.Generic.List<string>();
                int count = __result.Events.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<string>>().Count;
                for (int i = 0; i < count; i++) events.Add(__result.Events[i]);
                events.Add("The player is currently using a VR headset and motion controllers. They may need extra time or help with controls when using menus and interactions.");
                __result = new DialogueRuntimeContextBatch(__result.Character, __result.EntryIds,
                    events.Cast<Il2CppSystem.Collections.Generic.IReadOnlyList<string>>());
            }
            catch (Exception error)
            {
                failed = true;
                Debug.LogWarning("[NeuroMita VR] Dialogue context disabled after capture failure: " + error);
            }
        }
        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; failed = false; }
    }
}
