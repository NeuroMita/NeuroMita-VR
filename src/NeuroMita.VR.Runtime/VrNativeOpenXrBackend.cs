using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR;

namespace NeuroMita.VR
{
    internal sealed class VrNativeOpenXrBackend : IVrXrBackend
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NativeEvent(int kind, ulong payload);
        private readonly NativeEvent callback;
        private static bool resolverInstalled;
        private bool callbacksInstalled;
        private bool loaded, initialized, requested, ready, begun, stopping, disposed, actionsAttached;
        public XRDisplaySubsystem Display { get; private set; }
        public XRInputSubsystem Input { get; private set; }

        public VrNativeOpenXrBackend() => callback = OnNativeEvent;

        public IEnumerator Initialize()
        {
            if (!resolverInstalled)
            {
                string path = Path.Combine(Application.dataPath, "Plugins", "x86_64");
                NativeLibrary.SetDllImportResolver(typeof(VrNativeOpenXrBackend).Assembly,
                    (name, assembly, searchPath) => name == "UnityOpenXR"
                        ? NativeLibrary.Load(Path.Combine(path, "UnityOpenXR.dll")) : IntPtr.Zero);
                resolverInstalled = true;
            }
            var existing = new Il2CppSystem.Collections.Generic.List<XRDisplaySubsystem>();
            SubsystemManager.GetInstances(existing);
            foreach (var display in existing)
                if (display.running) throw new InvalidOperationException("Another XR display is already running; the VR mod will not take over its native session.");
            SetCallbacks(callback);
            callbacksInstalled = true;
            SetRenderMode(0);
            SetSuccessfullyInitialized(false);
            SetApplicationInfo(Application.productName + " VR mod", "0.1.0", 1, Application.unityVersion);
            var loader = System.Text.Encoding.Unicode.GetBytes(Path.Combine(Application.dataPath,
                "Plugins", "x86_64", "openxr_loader.dll") + "\0");
            loaded = LoadLoader(loader);
            if (!loaded) throw new InvalidOperationException("Native OpenXR loader could not be loaded.");
            var address = GetProcAddress(true);
            if (address == IntPtr.Zero) throw new InvalidOperationException("xrGetInstanceProcAddr is unavailable.");
            SetProcAddress(address);
            initialized = InitializeSession();
            if (!initialized) throw new InvalidOperationException("Native OpenXR initialization failed.");

            var displays = new Il2CppSystem.Collections.Generic.List<XRDisplaySubsystemDescriptor>();
            var inputs = new Il2CppSystem.Collections.Generic.List<XRInputSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(displays);
            SubsystemManager.GetSubsystemDescriptors(inputs);
            foreach (var descriptor in displays)
                if (descriptor.id == "OpenXR Display") { var pointer = SubsystemDescriptorBindings.Create(descriptor.m_Ptr); Display = SubsystemManager.GetIntegratedSubsystemByPtr(pointer)?.Cast<XRDisplaySubsystem>(); if (Display != null) Display.m_SubsystemDescriptor = descriptor.Cast<ISubsystemDescriptor>(); break; }
            foreach (var descriptor in inputs)
                if (descriptor.id == "OpenXR Input") { var pointer = SubsystemDescriptorBindings.Create(descriptor.m_Ptr); Input = SubsystemManager.GetIntegratedSubsystemByPtr(pointer)?.Cast<XRInputSubsystem>(); if (Input != null) Input.m_SubsystemDescriptor = descriptor.Cast<ISubsystemDescriptor>(); break; }
            if (Display == null || Input == null)
                throw new InvalidOperationException("OpenXR display/input subsystem creation failed.");
            SetSuccessfullyInitialized(true);
            yield break;
        }

        public bool Start()
        {
            if (disposed || !initialized || Display == null || Input == null) return false;
            requested = CreateSession();
            Debug.Log("[NeuroMita VR] Native session creation: " + requested);
            Pump();
            return requested;
        }

        public void Pump()
        {
            if (!initialized || disposed) return;
            PumpMessages();
            if (stopping)
            {
                Input?.Stop();
                Display?.Stop();
                if (begun) EndSession();
                begun = false;
                ready = false;
                stopping = false;
            }
            if (!requested || !ready || begun) return;
            Display.Start();
            if (!Display.running) return;
            BeginSession();
            begun = true;
            if (!actionsAttached) { VrNativeActions.Attach(); actionsAttached = true; }
            Input.Start();
            Debug.Log("[NeuroMita VR] Native XR display=" + Display.running + ", input=" + Input.running);
        }

        private void OnNativeEvent(int kind, ulong payload)
        {
            VrDiagnostics.Mark("XR event", kind + ", payload=" + payload);
            if (kind == 11) ready = true;
            if (kind == 15 || kind == 16 || kind == 17 || kind == 18) stopping = true;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Cleanup(() => Input?.Stop(), "input stop");
            Cleanup(() => Display?.Stop(), "display stop");
            if (begun) Cleanup(EndSession, "session end");
            if (initialized) Cleanup(DestroySession, "session destroy");
            Cleanup(() => Input?.Destroy(), "input destroy");
            Cleanup(() => Display?.Destroy(), "display destroy");
            if (callbacksInstalled) Cleanup(() => SetCallbacks(null), "callback detach");
            callbacksInstalled = false;
            Input = null;
            Display = null;
            initialized = false;
            begun = false;
            if (loaded) Cleanup(UnloadLoader, "loader unload");
            loaded = false;
            GC.KeepAlive(callback);
        }

        private static void Cleanup(Action action, string operation)
        {
            try { action(); }
            catch (Exception exception) { Debug.LogWarning("[NeuroMita VR] XR cleanup " + operation + ": " + exception.Message); }
        }
        [DllImport("UnityOpenXR", EntryPoint="NativeConfig_SetCallbacks", CallingConvention=CallingConvention.Cdecl)]
        private static extern void SetCallbacks(NativeEvent callback);
        [DllImport("UnityOpenXR", EntryPoint="NativeConfig_SetRenderMode", CallingConvention=CallingConvention.Cdecl)]
        private static extern void SetRenderMode(int mode);
        [DllImport("UnityOpenXR", EntryPoint="session_SetSuccessfullyInitialized", CallingConvention=CallingConvention.Cdecl)]
        private static extern void SetSuccessfullyInitialized([MarshalAs(UnmanagedType.I1)] bool value);
        [DllImport("UnityOpenXR", EntryPoint="NativeConfig_SetApplicationInfo", CallingConvention=CallingConvention.Cdecl, CharSet=CharSet.Ansi)]
        private static extern void SetApplicationInfo(string name, string version, uint hash, string engine);
        [DllImport("UnityOpenXR", EntryPoint="main_LoadOpenXRLibrary", CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] private static extern bool LoadLoader(byte[] path);
        [DllImport("UnityOpenXR", EntryPoint="NativeConfig_GetProcAddressPtr", CallingConvention=CallingConvention.Cdecl)]
        private static extern IntPtr GetProcAddress([MarshalAs(UnmanagedType.I1)] bool useDefault);
        [DllImport("UnityOpenXR", EntryPoint="NativeConfig_SetProcAddressPtrAndLoadStage1", CallingConvention=CallingConvention.Cdecl)]
        private static extern void SetProcAddress(IntPtr address);
        [DllImport("UnityOpenXR", EntryPoint="session_InitializeSession", CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] private static extern bool InitializeSession();
        [DllImport("UnityOpenXR", EntryPoint="session_CreateSessionIfNeeded", CallingConvention=CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)] private static extern bool CreateSession();
        [DllImport("UnityOpenXR", EntryPoint="messagepump_PumpMessageLoop", CallingConvention=CallingConvention.Cdecl)]
        private static extern void PumpMessages();
        [DllImport("UnityOpenXR", EntryPoint="session_BeginSession", CallingConvention=CallingConvention.Cdecl)]
        private static extern void BeginSession();
        [DllImport("UnityOpenXR", EntryPoint="session_EndSession", CallingConvention=CallingConvention.Cdecl)]
        private static extern void EndSession();
        [DllImport("UnityOpenXR", EntryPoint="session_DestroySession", CallingConvention=CallingConvention.Cdecl)]
        private static extern void DestroySession();
        [DllImport("UnityOpenXR", EntryPoint="main_UnloadOpenXRLibrary", CallingConvention=CallingConvention.Cdecl)]
        private static extern void UnloadLoader();
    }
}