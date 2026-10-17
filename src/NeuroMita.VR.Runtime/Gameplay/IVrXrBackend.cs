using System;
using System.Collections;
using UnityEngine.XR;
#if !NEUROMITA_VR_IL2CPP
using UnityEngine.XR.Management;
#endif

namespace NeuroMita.VR
{
    public interface IVrXrBackend : IDisposable
    {
        XRDisplaySubsystem Display { get; }
        XRInputSubsystem Input { get; }
        IEnumerator Initialize();
        bool Start();
        void Pump();
    }

#if !NEUROMITA_VR_IL2CPP
    internal sealed class VrManagedXrBackend : IVrXrBackend
    {
        private XRManagerSettings manager;
        private bool ownsLoader;
        public XRDisplaySubsystem Display => manager?.activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
        public XRInputSubsystem Input => manager?.activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();

        public IEnumerator Initialize()
        {
            manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager != null && manager.activeLoader == null)
            {
                ownsLoader = true;
                yield return manager.InitializeLoader();
            }
        }

        public bool Start()
        {
            if (manager?.activeLoader == null) return false;
            manager.StartSubsystems();
            return Display != null;
        }

        public void Pump() { }

        public void Dispose()
        {
            if (!ownsLoader || manager == null) return;
            manager.StopSubsystems();
            manager.DeinitializeLoader();
            ownsLoader = false;
            manager = null;
        }
    }
#endif
}