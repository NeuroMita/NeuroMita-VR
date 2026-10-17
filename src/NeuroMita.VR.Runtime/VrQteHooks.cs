using System;
using System.Reflection;
using HarmonyLib;
using MitaAI;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrQteHooks
    {
        private static Harmony harmony;
        public static bool IsInstalled => harmony != null;
        public static void Install()
        {
            if (harmony != null) return;
            var hooks = new Harmony("NeuroMita.VR.QTE");
            try
            {
                foreach (var name in new[] { "UpdateChoice", "UpdateSingle", "UpdateMash", "UpdateComboInput" })
                {
                    var target = typeof(QTEStateController).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var prefix = typeof(VrQteHooks).GetMethod(name + "Prefix", BindingFlags.Static | BindingFlags.NonPublic);
                    if (target == null || prefix == null) throw new MissingMethodException(name);
                    hooks.Patch(target, prefix: new HarmonyMethod(prefix));
                }
                harmony = hooks;
            }
            catch (Exception exception) { hooks.UnpatchSelf(); Debug.LogWarning("[NeuroMita VR] QTE controller adapter unavailable: " + exception); }
        }
        public static void Remove() { harmony?.UnpatchSelf(); harmony = null; VrQteInput.Clear(); }
        private static bool UpdateChoicePrefix(QTEStateController __instance, QTEChoiceData choice, float dt)
        { if (!VrQteInput.Owns(__instance)) return true; Choice(__instance, choice, dt); return false; }
        private static bool UpdateSinglePrefix(QTEStateController __instance, QTESinglePressData single)
        { if (!VrQteInput.Owns(__instance)) return true; Single(__instance, single); return false; }
        private static bool UpdateMashPrefix(QTEStateController __instance, QTEMashData mash, float dt)
        { if (!VrQteInput.Owns(__instance)) return true; Mash(__instance, mash, dt); return false; }
        private static bool UpdateComboInputPrefix(QTEStateController __instance, QTEComboData combo, ref bool __result)
        { if (!VrQteInput.Owns(__instance)) return true; __result = Combo(__instance, combo); return false; }
        private static void Choice(QTEStateController c, QTEChoiceData choice, float dt) {
            c._view.UpdateChoiceTimer(c._timer / Mathf.Max(0.01f, c._currentData.duration));

            if (!choice.requireHold)
            {
                if (VrQteInput.GetKeyDown(choice.choiceKeyA)) c.OnComplete?.Invoke(QTEChoiceResult.OptionA);
                else if (VrQteInput.GetKeyDown(choice.choiceKeyB)) c.OnComplete?.Invoke(QTEChoiceResult.OptionB);
                return;
            }

            bool holdingA = VrQteInput.GetKey(choice.choiceKeyA);
            bool holdingB = VrQteInput.GetKey(choice.choiceKeyB);
            bool conflict = holdingA && holdingB;

            if (conflict && !c._wasConflict && choice.playHoldConflictSound)
                c.PlayHoldConflictSound();
            c._wasConflict = conflict;

            if (holdingA)
                c._holdProgressA = Mathf.Min(conflict ? 0.5f : 1f, c._holdProgressA + dt / Mathf.Max(0.01f, choice.holdDuration));
            else
            {
                float decay = choice.holdDecaySpeed * (holdingB && c._holdProgressA > 0f ? choice.holdDecayFastMultiplier : 1f);
                c._holdProgressA = Mathf.Max(0f, c._holdProgressA - dt * decay);
            }

            if (holdingB)
                c._holdProgressB = Mathf.Min(conflict ? 0.5f : 1f, c._holdProgressB + dt / Mathf.Max(0.01f, choice.holdDuration));
            else
            {
                float decay = choice.holdDecaySpeed * (holdingA && c._holdProgressB > 0f ? choice.holdDecayFastMultiplier : 1f);
                c._holdProgressB = Mathf.Max(0f, c._holdProgressB - dt * decay);
            }

            c.ApplyHoldVisual(c._hintHandleA.GetGameObject(), c._cgA, c._holdProgressA, c._holdProgressB, conflict, 0f, c._originPosA);
            c.ApplyHoldVisual(c._hintHandleB.GetGameObject(), c._cgB, c._holdProgressB, c._holdProgressA, conflict, Mathf.PI * 0.7f, c._originPosB);
            c._view.UpdateHoldProgress(c._hintHandleA, c._hintHandleB, c._holdProgressA, c._holdProgressB);

            if (!conflict)
            {
                if (c._holdProgressA >= 1f) c.OnComplete?.Invoke(QTEChoiceResult.OptionA);
                else if (c._holdProgressB >= 1f) c.OnComplete?.Invoke(QTEChoiceResult.OptionB);
            }
        }
        private static void Single(QTEStateController c, QTESinglePressData single) {
            c._view.UpdateVisuals(c._currentData, c._timer, 0f);

            if (VrQteInput.GetKeyDown(single.primaryKey))
                c.OnComplete?.Invoke(QTEChoiceResult.OptionA);
        }
        private static void Mash(QTEStateController c, QTEMashData mash, float dt) {
            float progress01 = Mathf.Clamp01(c._mashProgress / Mathf.Max(1, mash.mashAmount));
            c._view.UpdateVisuals(c._currentData, c._timer, progress01);

            if (VrQteInput.GetKeyDown(mash.primaryKey))
            {
                c._mashLastInputUnscaledTime = Time.unscaledTime;
                c._mashProgress++;

                if (c._currentData.playTickSound)
                    c.PlayTickSound();

                c._view.PlayPressFeedback();

                if (c._mashProgress >= mash.mashAmount)
                {
                    c.OnComplete?.Invoke(QTEChoiceResult.OptionA);
                    return;
                }
            }

            if (mash.decayEnabled && Time.unscaledTime - c._mashLastInputUnscaledTime >= mash.decayDelaySeconds)
                c._mashProgress = Mathf.Max(0f, c._mashProgress - mash.decayPerSecond * dt);
        }
        private static bool Combo(QTEStateController c, QTEComboData combo) {
            var step = combo.comboSteps[c._comboIndex];

            bool triggered = combo.comboAllowHeldKeys
                ? VrQteInput.GetKey(step.key) && !c._comboPressed.Contains(step.key)
                : VrQteInput.GetKeyDown(step.key);

            if (!triggered)
            {
                if (combo.comboResetSequenceOnWrongKey && VrQteInput.anyKeyDown && !VrQteInput.GetKeyDown(step.key))
                {
                    if (combo.playWrongKeySound)
                        c.PlayWrongKeySound();

                    c._comboIndex = 0;
                    c._comboPressed.Clear();
                    c._timer = combo.comboSteps[0].duration;
                    c._view.SetupCombo(combo);
                }
                return false;
            }

            c._comboPressed.Add(step.key);
            int pressedIndex = c._comboIndex;
            c._comboIndex++;

            if (combo.playComboStepSound)
                c.PlayComboStepSound();

            c.StartCoroutine(c.DelayedMarkAsPressed(pressedIndex));

            if (c._comboIndex < combo.comboSteps.Count)
            {
                c._comboPressed.Clear();
                c._timer = combo.comboSteps[c._comboIndex].duration;
                return false;
            }

            return true;
        }
    }
}
