using System;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Bindings;

namespace NeuroMita.VR
{
    internal static unsafe class VrNativeDeviceInput
    {
        public static bool ReadPose(InputDevice device, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            if (!device.isValid) return false;
            fixed (char* trackedName = "IsTracked")
            {
                var usage = new ManagedSpanWrapper(trackedName, 9);
                if (!InputDevices.TryGetFeatureValue_bool_Injected(device.deviceId, ref usage, out bool tracked) || !tracked)
                    return false;
            }
            fixed (char* positionName = "DevicePosition")
            {
                var usage = new ManagedSpanWrapper(positionName, 14);
                if (!InputDevices.TryGetFeatureValue_Vector3f_Injected(device.deviceId, ref usage, out position)) return false;
            }
            fixed (char* rotationName = "DeviceRotation")
            {
                var usage = new ManagedSpanWrapper(rotationName, 14);
                return InputDevices.TryGetFeatureValue_Quaternionf_Injected(device.deviceId, ref usage, out rotation);
            }
        }
    }
}