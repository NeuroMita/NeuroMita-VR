using System;
using UnityEngine;
using UnityEngine.XR;

namespace NeuroMita.VR
{
    internal static class VrHaptics
    {
        private static readonly VrHapticGate left = new(), right = new();
        private static bool failed;
        internal static void Ui(float amplitude, float duration) => Pulse(VrRuntime.Settings?.VrPointerLeft == true ? XRNode.LeftHand : XRNode.RightHand, amplitude, duration, 1);
        internal static void Contact(Transform hand) => Pulse(hand == VrRuntime.Rig?.LeftHand ? XRNode.LeftHand : XRNode.RightHand, 0.10f, 0.025f, 0);
        internal static void BatImpact() => Pulse(XRNode.RightHand, 0.25f, 0.05f, 2);
        internal static void Damage()
        {
            Pulse(XRNode.LeftHand, 0.40f, 0.10f, 3);
            Pulse(XRNode.RightHand, 0.40f, 0.10f, 3);
        }
        private static void Pulse(XRNode hand, float amplitude, float duration, int kind)
        {
            int strength = VrRuntime.Settings?.VrHaptics ?? 2;
            var rig = VrRuntime.Rig;
            if (failed || !VrRuntime.IsActive || strength <= 0 || rig?.HeadTracked != true
                || (hand == XRNode.LeftHand ? !rig.LeftHandTracked : !rig.RightHandTracked)) return;
            if (!(hand == XRNode.LeftHand ? left : right).Accept(Time.unscaledTime, duration, kind)) return;
            try
            {
                var device = InputDevices.GetDeviceAtXRNode(hand);
                if (device.isValid) device.SendHapticImpulse(0, Mathf.Clamp(amplitude, 0f, 0.65f) * Mathf.Clamp(strength, 0, 2) * 0.5f, duration);
            }
            catch (Exception error)
            {
                failed = true;
                Debug.LogWarning("[NeuroMita VR] Haptic output disabled: " + error);
            }
        }
        internal static void Reset()
        {
            left.Reset(); right.Reset(); failed = false;
            try
            {
                var leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                var rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (leftDevice.isValid) leftDevice.StopHaptics();
                if (rightDevice.isValid) rightDevice.StopHaptics();
            }
            catch (Exception error) { Debug.LogWarning("[NeuroMita VR] Could not stop haptics: " + error.Message); }
        }
    }
}
