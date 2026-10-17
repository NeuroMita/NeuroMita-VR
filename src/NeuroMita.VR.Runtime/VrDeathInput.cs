using System;
using System.Reflection;
using HarmonyLib;
using NeuroMita.Modding;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal static class VrDeathInput
    {
        private static Harmony hooks;
        private static DeathScreenMenu owner;
        private static bool waiting = true;
        internal static bool IsVisible => VrRuntime.IsActive && owner != null && owner.IsVisible;
        internal static bool BlocksPointer => IsVisible && waiting;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.DeathInput");
            try
            {
                Patch(candidate, typeof(DeathScreenMenu), "OnEnable", nameof(Opened), true);
                Patch(candidate, typeof(DeathScreenMenu), "OnDisable", nameof(Closed), true);
                Patch(candidate, typeof(DeathScreenMenu), "HandleOpeningAnimationSkipInput", nameof(BeforeSkip), false);
                Patch(candidate, typeof(DeathScreenFxRig), "BindCanvases", nameof(BeforeCanvasBinding), false);
                Patch(candidate, typeof(DeathScreenVisuals), "RefreshHintText", nameof(Hint), true);
                hooks = candidate;
                Debug.Log("[NeuroMita VR] Installed death input and controller hints hooks");
                foreach (var menu in UnityEngine.Object.FindObjectsByType<DeathScreenMenu>(FindObjectsSortMode.None))
                    if (menu.IsVisible) { Opened(menu); break; }
            }
            catch (Exception error) { candidate.UnpatchSelf(); hooks = null; Clear(); Debug.LogWarning("[NeuroMita VR] Death input unavailable: " + error); }
        }
        private static void Patch(Harmony candidate, Type type, string method, string handler, bool postfix)
        {
            var target = type.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (target == null) throw new MissingMethodException(type.Name + "." + method);
            var hook = new HarmonyMethod(typeof(VrDeathInput).GetMethod(handler, BindingFlags.Static | BindingFlags.NonPublic));
            if (postfix) candidate.Patch(target, postfix: hook); else candidate.Patch(target, prefix: hook);
        }
        private static void Opened(DeathScreenMenu __instance)
        {
            if (!VrRuntime.IsActive) return;
            owner = __instance; waiting = true;
            try
            {
                VrRuntime.Presenter?.CancelInput();
                VrRuntime.Keyboard?.Clear();
                VrRuntime.Presenter?.RefreshCanvases();
                VrRuntime.Presenter?.Recenter();
            }
            catch (Exception error) { Debug.LogWarning("[NeuroMita VR] Death UI presentation failed: " + error); }
        }
        private static void Closed(DeathScreenMenu __instance)
        {
            if (owner != null && owner.Pointer == __instance.Pointer) Clear();
        }
        private static bool BeforeSkip(DeathScreenMenu __instance)
        {
            if (!VrRuntime.IsActive) return true;
            if (!__instance.IsVisible) return false;
            if (owner == null || owner.Pointer != __instance.Pointer) Opened(__instance);
            var rig = VrRuntime.Rig;
            var input = VrRuntime.Input;
            if (rig?.HeadTracked != true || !rig.RightHandTracked || !rig.PointerHandTracked || input == null) { waiting = true; return false; }
            bool held = input.Held(ExternalInputAction.Jump) || input.PointerHeld;
            if (waiting) { if (!held) waiting = false; return false; }
            if (!input.Down(ExternalInputAction.Jump)) return false;
            var state = __instance._currentState;
            if (state == DeathScreenMenu.MenuState.Exiting || state == DeathScreenMenu.MenuState.FinalDeathReveal) return false;
            if (state == DeathScreenMenu.MenuState.FadingIn) __instance.CompleteOpeningAnimation();
            __instance._visuals?.terminalTypewriter?.SkipTyping();
            return false;
        }
        private static bool BeforeCanvasBinding() => !VrRuntime.IsActive;
        private static void Hint(DeathScreenVisuals __instance)
        {
            if (!VrRuntime.IsActive || __instance.hintTextObject == null) return;
            var label = __instance.hintTextObject.GetComponentInChildren<Text>(true);
            if (label != null) label.text = VrSettingsText.Pick("A: skip intro/text | Aim and trigger: choose a button", "A: пропустить вступление/текст | Луч и триггер: выбрать кнопку");
        }
        internal static void Filter(ref ulong held, ref ulong down, ref ulong up)
        {
            if (!IsVisible) return;
            ulong allowed = BlocksPointer ? 0UL : 1UL << (int)ExternalInputAction.Click;
            held &= allowed; down &= allowed; up &= allowed;
        }
        internal static bool AllowsTarget(GameObject target)
        {
            if (!IsVisible) return true;
            if (target == null) return false;
            var group = owner._visuals?.menuCanvasGroup;
            return target.transform.IsChildOf(owner.transform) || (group != null && target.transform.IsChildOf(group.transform));
        }
        internal static void Clear() { owner = null; waiting = true; }
        internal static void Remove()
        {
            hooks?.UnpatchSelf(); hooks = null;
            if (owner?._visuals != null) owner._visuals.RefreshHintText();
            Clear();
        }
    }
}
