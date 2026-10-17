using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace NeuroMita.VR
{
    internal static class VrNativeActions
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct ActionGuid { public ulong Low, High; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct Binding { public ulong Action; public string Path; }
        private const string Profile = "/interaction_profiles/oculus/touch_controller";
        private static readonly string[] Hands = { "/user/hand/left", "/user/hand/right" };

        public static void Attach()
        {
            for (int i = 0; i < Hands.Length; i++)
            {
                var flags = InputDeviceCharacteristics.HeldInHand | InputDeviceCharacteristics.TrackedDevice |
                    InputDeviceCharacteristics.Controller | (i == 0 ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);
                if (RegisterDevice(Hands[i], Profile, false, (uint)flags, "NeuroMita Touch Controller", "NeuroMita VR", "") == 0)
                    throw new InvalidOperationException("OpenXR controller definition failed.");
            }
            ulong set = CreateSet("neuromita_vr", "NeuroMita VR", default);
            if (set == 0) throw new InvalidOperationException("OpenXR controller action set failed.");
            var bindings = new List<Binding>();
            Add(set, bindings, "pose", 3, "Device", "/input/grip/pose");
            Add(set, bindings, "aim", 3, "Pointer", "/input/aim/pose");
            Add(set, bindings, "stick", 2, "Primary2DAxis", "/input/thumbstick");
            Add(set, bindings, "stick_click", 0, "Primary2DAxisClick", "/input/thumbstick/click");
            Add(set, bindings, "trigger", 1, "Trigger", "/input/trigger/value");
            Add(set, bindings, "grip", 1, "Grip", "/input/squeeze/value");
            Add(set, bindings, "primary", 0, "PrimaryButton", "/input/x/click", "/input/a/click");
            Add(set, bindings, "secondary", 0, "SecondaryButton", "/input/y/click", "/input/b/click");
            Add(set, bindings, "haptic", 4, "Haptic", "/output/haptic");
            if (!Suggest(Profile, bindings.ToArray(), (uint)bindings.Count) || !AttachSets())
                throw new InvalidOperationException("OpenXR controller bindings could not be attached.");
            Debug.Log("[NeuroMita VR] Touch controller actions attached.");
        }

        private static void Add(ulong set, List<Binding> bindings, string name, uint type, string usage, string leftPath, string rightPath = null)
        {
            ulong action = CreateAction(set, name, name, type, default, Hands, 2, false, new[] { usage }, 1);
            if (action == 0) throw new InvalidOperationException("OpenXR action creation failed: " + name);
            bindings.Add(new Binding { Action = action, Path = Hands[0] + leftPath });
            bindings.Add(new Binding { Action = action, Path = Hands[1] + (rightPath ?? leftPath) });
        }

        [DllImport("UnityOpenXR", EntryPoint = "OpenXRInputProvider_RegisterDeviceDefinition", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ulong RegisterDevice(string path, string profile, [MarshalAs(UnmanagedType.I1)] bool additive, uint characteristics, string name, string manufacturer, string serial);
        [DllImport("UnityOpenXR", EntryPoint = "OpenXRInputProvider_CreateActionSet", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ulong CreateSet(string name, string localized, ActionGuid guid);
        [DllImport("UnityOpenXR", EntryPoint = "OpenXRInputProvider_CreateAction", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern ulong CreateAction(ulong set, string name, string localized, uint type, ActionGuid guid, string[] paths, uint pathCount, [MarshalAs(UnmanagedType.I1)] bool additive, string[] usages, uint usageCount);
        [DllImport("UnityOpenXR", EntryPoint = "OpenXRInputProvider_SuggestBindings", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool Suggest(string profile, Binding[] bindings, uint count);
        [DllImport("UnityOpenXR", EntryPoint = "OpenXRInputProvider_AttachActionSets", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool AttachSets();
    }
}