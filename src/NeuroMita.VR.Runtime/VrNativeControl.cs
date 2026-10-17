using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Bindings;
using UnityEngine.InputSystem;

namespace NeuroMita.VR
{
    internal sealed class VrNativeControlMap : IDisposable
    {
        public VrNativeControlMap(string name) { }
        public VrNativeControl AddAction(string name, InputActionType type, string binding) => new(binding);
        public void Enable() { }
        public void Dispose() { }
    }

    internal sealed unsafe class VrNativeControl
    {
        private readonly XRNode hand;
        private readonly string usage;
        private readonly bool analog;
        private int frame = -1;
        private bool held, down;
        private bool reported;

        public VrNativeControl(string binding)
        {
            hand = binding.Contains("{LeftHand}") ? XRNode.LeftHand : XRNode.RightHand;
            string control = binding.Substring(binding.LastIndexOf('/') + 1).Trim('{', '}');
            usage = control switch
            {
                "triggerPressed" => "Trigger", "gripPressed" => "Grip", "primary2DAxis" => "Primary2DAxis",
                "primaryButton" => "PrimaryButton", "secondaryButton" => "SecondaryButton",
                "Primary2DAxisClick" => "Primary2DAxisClick",
                _ => throw new ArgumentException("Unsupported VR control: " + binding)
            };
            analog = control == "triggerPressed" || control == "gripPressed";
        }

        public bool IsPressed()
        {
            if (frame == Time.frameCount) return held;
            frame = Time.frameCount;
            bool previous = held;
            var device = InputDevices.GetDeviceAtXRNode(hand);
            held = false;
            if (device.isValid)
            {
                fixed (char* text = usage)
                {
                    var span = new ManagedSpanWrapper(text, usage.Length);
                    if (analog)
                        held = InputDevices.TryGetFeatureValue_float_Injected(device.deviceId, ref span, out float value) && value >= 0.5f;
                    else
                        held = InputDevices.TryGetFeatureValue_bool_Injected(device.deviceId, ref span, out bool value) && value;
                }
            }
            if (!reported || held != previous)
            {
                VrDiagnostics.Mark("raw control", hand + "/" + usage + ", device=" + device.deviceId + ", valid=" + device.isValid + ", held=" + held);
                reported = true;
            }
            down = held && !previous;
            return held;
        }

        public bool WasPressedThisFrame() { IsPressed(); return down; }

        public T ReadValue<T>() where T : struct
        {
            if (typeof(T) != typeof(Vector2)) throw new NotSupportedException(typeof(T).FullName);
            Vector2 value = Vector2.zero;
            var device = InputDevices.GetDeviceAtXRNode(hand);
            if (device.isValid)
            {
                fixed (char* text = usage)
                {
                    var span = new ManagedSpanWrapper(text, usage.Length);
                    InputDevices.TryGetFeatureValue_Vector2f_Injected(device.deviceId, ref span, out value);
                }
            }
            return (T)(object)value;
        }
    }
}