using System;
using System.Collections.Generic;
using MitaAI;
using NeuroMita.Modding;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrQteInput
    {
        private static readonly ExternalInputAction[] controls = {
            ExternalInputAction.Click, ExternalInputAction.Inventory, ExternalInputAction.Interact,
            ExternalInputAction.PushToTalk, ExternalInputAction.Jump, ExternalInputAction.OpenChat,
            ExternalInputAction.RadialMenu
        };
        private static readonly string[] names = {
            "Right trigger", "Left trigger", "Right grip", "Left grip", "Right A", "Left X", "Right B"
        };
        private static readonly Dictionary<KeyCode, int> keys = new();
        private static QTEStateController owner;
        private static int runId = -1;
        private static bool waitForRelease;
        public static bool CapturesActions { get; private set; }
        public static bool HasActiveQte { get; private set; }

        public static void Prepare()
        {
            if (VrDeathInput.IsVisible) { Clear(); return; }
            var manager = QTEManager.Instance;
            bool active = VrRuntime.IsActive && VrQteHooks.IsInstalled && manager != null && manager.IsActive;
            bool anyHeld = false;
            foreach (var action in controls) anyHeld |= VrRuntime.Input?.Held(action) == true;
            HasActiveQte = active;
            if (!active)
            {
                if (owner != null) waitForRelease = true;
                owner = null;
                keys.Clear();
                if (!anyHeld) waitForRelease = false;
                CapturesActions = VrRuntime.IsActive && waitForRelease;
                return;
            }
            var current = manager.stateController;
            if (current == null) { CapturesActions = true; return; }
            if (owner == null || owner.Pointer != current.Pointer || runId != manager._runId)
            {
                owner = current;
                runId = manager._runId;
                keys.Clear();
                var data = current._currentData;
                var choice = data?.TryCast<QTEChoiceData>();
                var single = data?.TryCast<QTESinglePressData>();
                var mash = data?.TryCast<QTEMashData>();
                var combo = data?.TryCast<QTEComboData>();
                if (choice != null) { Add(choice.choiceKeyA); Add(choice.choiceKeyB); }
                else if (single != null) Add(single.primaryKey);
                else if (mash != null) Add(mash.primaryKey);
                else if (combo != null)
                    for (int i = 0; i < combo.comboSteps.Count; i++) Add(combo.comboSteps[i].key);
                waitForRelease = true;
                VrDiagnostics.Mark("QTE VR input", "run=" + runId + ", mapped keys=" + keys.Count);
            }
            bool tracked = VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.LeftHandTracked && VrRuntime.Rig.RightHandTracked;
            if (!tracked) waitForRelease = true;
            else if (!anyHeld) waitForRelease = false;
            CapturesActions = true;
            RefreshLabels();
        }

        private static void Add(KeyCode key)
        {
            if (!keys.ContainsKey(key)) keys.Add(key, keys.Count);
        }
        internal static bool Owns(QTEStateController controller) => VrRuntime.IsActive && HasActiveQte &&
            owner != null && controller != null && owner.Pointer == controller.Pointer;
        private static bool Ready => !waitForRelease && owner != null && owner._isActive && !owner._isSuspended && VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.LeftHandTracked && VrRuntime.Rig.RightHandTracked;
        public static bool GetKey(KeyCode key) => Ready && keys.TryGetValue(key, out var index) && index < controls.Length && VrRuntime.Input.Held(controls[index]);
        public static bool GetKeyDown(KeyCode key) => Ready && keys.TryGetValue(key, out var index) && index < controls.Length && VrRuntime.Input.Down(controls[index]);
        public static bool anyKeyDown
        {
            get { if (!Ready) return false; foreach (var action in controls) if (VrRuntime.Input.Down(action)) return true; return false; }
        }
        private static string Label(KeyCode key) => keys.TryGetValue(key, out var index) && index < names.Length ? names[index] : "Unmapped VR input";
        private static void RefreshLabels()
        {
            if (owner?._view != null)
                foreach (var button in owner._view.GetComponentsInChildren<QTEButtonView>(true))
                    if (button.keyText != null && Enum.TryParse<KeyCode>(button.keyText.text, out var key))
                        button.keyText.text = Label(key);
            var choice = owner?._currentData?.TryCast<QTEChoiceData>();
            if (choice == null) return;
            SetHint(owner._hintHandleA.GetGameObject(), choice.choiceKeyA);
            SetHint(owner._hintHandleB.GetGameObject(), choice.choiceKeyB);
        }
        private static void SetHint(GameObject root, KeyCode key)
        {
            var hint = root != null ? root.GetComponent<KeyHintView>() : null;
            if (hint?.textKey != null) hint.textKey.text = Label(key);
        }
        public static void Clear()
        {
            owner = null;
            runId = -1;
            keys.Clear();
            waitForRelease = CapturesActions = HasActiveQte = false;
        }
    }
}
