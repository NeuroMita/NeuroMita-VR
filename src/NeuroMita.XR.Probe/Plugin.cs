using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using UnityEngine.XR;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NeuroMitaVRmod.Validation;

[BepInPlugin("vinerx.neuromita.xr-deployment-probe", "NeuroMita XR deployment probe", "0.1.0")]
public sealed class Plugin : BasePlugin
{
    internal static BepInEx.Logging.ManualLogSource Logger;
    public override void Load()
    {
        Logger = Log;
        AddComponent<DescriptorProbe>();
        Log.LogInfo("XR deployment probe scheduled; this is not the VR runtime.");
    }
}

public sealed class DescriptorProbe : MonoBehaviour
{
    private bool completed;
    public DescriptorProbe(IntPtr pointer) : base(pointer) { }

    private void Update()
    {
        if (completed || Time.realtimeSinceStartup < 5f) return;
        completed = true;
        try
        {
            var displays = new Il2CppSystem.Collections.Generic.List<XRDisplaySubsystemDescriptor>();
            var inputs = new Il2CppSystem.Collections.Generic.List<XRInputSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(displays);
            SubsystemManager.GetSubsystemDescriptors(inputs);
            bool display = false, input = false;
            foreach (var descriptor in displays)
            {
                Plugin.Logger.LogInfo("XR display descriptor: " + descriptor.id);
                display |= descriptor.id == "OpenXR Display";
            }
            foreach (var descriptor in inputs)
            {
                Plugin.Logger.LogInfo("XR input descriptor: " + descriptor.id);
                input |= descriptor.id == "OpenXR Input";
            }
            Plugin.Logger.LogInfo(display && input ? "XR_DESCRIPTOR_PROBE_PASS" : "XR_DESCRIPTOR_PROBE_MISSING: display=" + display + "; input=" + input);
            if (display && input) NativeSessionProbe.TryInitialize();
        }
        catch (Exception exception) { Plugin.Logger.LogError("XR_DESCRIPTOR_PROBE_FAIL: " + exception); }
    }
}

internal static class NativeSessionProbe
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NativeEvent(int kind, ulong payload);
    private static readonly NativeEvent callback = (kind, payload) => Plugin.Logger.LogInfo("XR native event: " + kind + "; payload=" + payload);
    private static bool initialized;

    internal static void TryInitialize()
    {
        if (Environment.GetEnvironmentVariable("NEUROMITA_XR_SESSION_PROBE") != "1") return;
        string nativeDirectory = Path.Combine(Application.dataPath, "Plugins", "x86_64");
        NativeLibrary.SetDllImportResolver(typeof(NativeSessionProbe).Assembly, (name, assembly, path) =>
            name == "UnityOpenXR" ? NativeLibrary.Load(Path.Combine(nativeDirectory, "UnityOpenXR.dll")) : IntPtr.Zero);
        try
        {
            SetCallbacks(callback);
            SetRenderMode(0);
            SetApplicationInfo("NeuroMita external XR probe", "0.1", 1, Application.unityVersion);
            byte[] loader = System.Text.Encoding.Unicode.GetBytes(Path.Combine(nativeDirectory, "openxr_loader.dll") + "\0");
            if (!LoadLoader(loader)) { Plugin.Logger.LogWarning("XR_NATIVE_LOADER_UNAVAILABLE"); return; }
            var proc = GetProcAddress(true);
            if (proc == IntPtr.Zero) { Plugin.Logger.LogWarning("XR_NATIVE_PROC_UNAVAILABLE"); return; }
            SetProcAddress(proc);
            initialized = InitializeSession();
            Plugin.Logger.LogInfo("XR_NATIVE_INITIALIZE_RESULT=" + initialized);
            if (initialized)
            {
                Plugin.Logger.LogInfo("XR_NATIVE_CREATE_SESSION_RESULT=" + CreateSession());
                PumpMessages();
            }
        }
        catch (Exception exception) { Plugin.Logger.LogError("XR_NATIVE_INITIALIZE_FAIL: " + exception); }
        finally
        {
            if (initialized) { DestroySession(); initialized = false; }
            UnloadLoader();
            Plugin.Logger.LogInfo("XR native session probe cleaned up; rendering/input were not started.");
        }
    }

    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_SetCallbacks", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetCallbacks(NativeEvent callback);
    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_SetRenderMode", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetRenderMode(int mode);
    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_SetApplicationInfo", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern void SetApplicationInfo(string name, string version, uint hash, string engineVersion);
    [DllImport("UnityOpenXR", EntryPoint = "main_LoadOpenXRLibrary", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool LoadLoader(byte[] path);
    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_GetProcAddressPtr", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr GetProcAddress([MarshalAs(UnmanagedType.I1)] bool loaderDefault);
    [DllImport("UnityOpenXR", EntryPoint = "NativeConfig_SetProcAddressPtrAndLoadStage1", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SetProcAddress(IntPtr pointer);
    [DllImport("UnityOpenXR", EntryPoint = "session_InitializeSession", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool InitializeSession();
    [DllImport("UnityOpenXR", EntryPoint = "session_CreateSessionIfNeeded", CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool CreateSession();
    [DllImport("UnityOpenXR", EntryPoint = "messagepump_PumpMessageLoop", CallingConvention = CallingConvention.Cdecl)]
    private static extern void PumpMessages();
    [DllImport("UnityOpenXR", EntryPoint = "session_DestroySession", CallingConvention = CallingConvention.Cdecl)]
    private static extern void DestroySession();
    [DllImport("UnityOpenXR", EntryPoint = "main_UnloadOpenXRLibrary", CallingConvention = CallingConvention.Cdecl)]
    private static extern void UnloadLoader();
}
