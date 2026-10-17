using System;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrSceneTransitionPresenter : MonoBehaviour
    {
        private Camera curtain;
        private float readySince = -1f;
        private bool covering;
        public VrSceneTransitionPresenter(IntPtr pointer) : base(pointer) { }

        private void LateUpdate()
        {
            if (!VrRuntime.IsActive) { if (curtain != null) curtain.enabled = false; return; }
            var rig = VrRuntime.Rig;
            bool loading = SceneService.IsTransitionInProgress || rig?.Eye == null;
            if (loading) { covering = true; readySince = -1f; }
            else if (covering)
            {
                if (readySince < 0f) readySince = Time.unscaledTime;
                if (Time.unscaledTime - readySince >= 0.35f) covering = false;
            }
            if (covering && curtain == null)
            {
                var root = VrObject.Create("VR scene transition curtain", typeof(Camera));
                root.transform.SetParent(transform, false);
                curtain = root.GetComponent<Camera>();
                curtain.clearFlags = CameraClearFlags.SolidColor;
                curtain.backgroundColor = Color.black;
                curtain.cullingMask = 0;
                curtain.depth = 10000f;
                curtain.nearClipPlane = 0.02f;
                curtain.farClipPlane = 10f;
                curtain.stereoTargetEye = StereoTargetEyeMask.Both;
            }
            if (curtain == null) return;
            if (rig?.Eye != null) curtain.transform.SetPositionAndRotation(rig.Eye.transform.position, rig.Eye.transform.rotation);
            curtain.enabled = covering;
        }

        private void OnDisable() { if (curtain != null) curtain.enabled = false; }
        private void OnDestroy() { if (curtain != null) Destroy(curtain.gameObject); }
    }
}
