using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    public sealed class VrEaselPresenter : MonoBehaviour
    {
        public VrEaselPresenter(IntPtr pointer) : base(pointer) { }
        private EaselController easel;
        private UIDocument easelDocument;
        private VrToolkitWorldPanel easelPanel;
        private float nextEaselDiscovery;

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            if (rig?.Eye == null) { ReleaseEasel(); return; }
            RefreshEasel(rig);
        }

        private void RefreshEasel(VrPlayerRig rig)
        {
            if (Time.unscaledTime >= nextEaselDiscovery)
            {
                nextEaselDiscovery = Time.unscaledTime + 0.25f;
                EaselController current = null;
                foreach (var candidate in FindObjectsByType<EaselController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    if (candidate.IsOpen) { current = candidate; break; }
                var doc = current?.easelUI != null ? current.easelUI.GetComponent<UIDocument>() : null;
                if (easel != current || easelDocument != doc || (easelPanel != null && easelPanel.Canvas == null))
                {
                    ReleaseEasel();
                    easel = current;
                    easelDocument = doc;
                }
                if (easelPanel == null && doc != null && doc.panelSettings != null)
                    easelPanel = new VrToolkitWorldPanel(doc, transform, rig.Eye, VrRuntime.Presenter, "Easel tools");
            }
            if (easel == null || !easel.IsOpen) { ReleaseEasel(); return; }
            easelPanel?.Refresh(rig.Eye);
        }

        private void ReleaseEasel()
        {
            easelPanel?.Dispose();
            easelPanel = null;
            easelDocument = null;
            easel = null;
        }


        private void OnDisable() => ReleaseEasel();
        private void OnDestroy() => ReleaseEasel();
    }
}
