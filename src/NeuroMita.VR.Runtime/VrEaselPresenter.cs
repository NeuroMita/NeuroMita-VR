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
        private VrEaselLayout layout;
        private Vector3 placedEye;

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
                {
                    easelPanel = new VrToolkitWorldPanel(doc, transform, rig.Eye, VrRuntime.Presenter, "Easel tools", 800, 1100, 0.8f, 1.5f);
                    layout = VrEaselDrawing.IsInstalled ? new VrEaselLayout(current.easelUI) : null;
                    placedEye = Vector3.positiveInfinity;
                }
            }
            if (easel == null || !easel.IsOpen) { ReleaseEasel(); return; }
            easelPanel?.Refresh(rig.Eye);
            layout?.Refresh(easelDocument?.rootVisualElement?.layout.width ?? 533f);
            if (easelPanel != null && Vector3.Distance(placedEye, rig.Eye.transform.position) > 0.25f)
            {
                var eye = rig.Eye.transform;
                Vector3 center = easel.canvasQuad != null ? easel.canvasQuad.bounds.center : easel.transform.position + Vector3.up;
                Vector3 direction = Vector3.ProjectOnPlane(center - eye.position, Vector3.up);
                var facing = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
                easelPanel.Place(rig.Eye, eye.position + facing * new Vector3(-0.65f, 0.1f, 0.9f), 0.8f);
                placedEye = eye.position;
            }
        }

        private void ReleaseEasel()
        {
            layout?.Dispose();
            layout = null;
            easelPanel?.Dispose();
            easelPanel = null;
            easelDocument = null;
            easel = null;
        }


        private void OnDisable() => ReleaseEasel();
        private void OnDestroy() => ReleaseEasel();
    }
}
