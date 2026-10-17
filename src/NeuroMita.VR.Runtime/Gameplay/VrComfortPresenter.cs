using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NeuroMita.VR
{
    public static class VrComfortPolicy
    {
        public static float MotionTarget(int strength, float movement, float smoothTurn)
            => Mathf.Clamp01(strength * 0.01f) * Mathf.Clamp01(Mathf.Max(movement, Mathf.Abs(smoothTurn)));

        public static float WallFade(float distance, float radius)
            => radius > 0f ? 1f - Mathf.Clamp01(distance / radius) : 0f;

        public static float Advance(float current, float target, float deltaTime)
            => Mathf.MoveTowards(current, target, Mathf.Max(0f, deltaTime) * (target > current ? 8f : 4f));
    }

#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10020)]
#endif
    public sealed partial class VrComfortPresenter : MonoBehaviour
    {
        private static readonly int VignetteId = Shader.PropertyToID("_Vignette");
        private static readonly int FadeId = Shader.PropertyToID("_Fade");
        private readonly Collider[] overlaps = new Collider[128];
        private readonly Stack<bool> visibility = new();
        private VrPlayerRig rig;
        private Camera eye;
        private Mesh mesh;
        private Material material;
        private MeshRenderer overlay;
        private float vignette, fade;

        public void Bind(VrPlayerRig owner)
        {
            Clear();
            rig = owner;
            eye = owner.Eye;
            var template = VrAssets.Load<Material>("NeuroMitaVrComfort");
            if (template == null || eye == null) return;
            material = new Material(template);
            mesh = new Mesh { name = "VR comfort screen" };
            mesh.vertices = new[] { new Vector3(-1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(1f, -1f, 0f) };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            var screen = VrObject.Create("VR comfort overlay", typeof(MeshFilter), typeof(MeshRenderer));
            screen.transform.SetParent(eye.transform, false);
            screen.GetComponent<MeshFilter>().sharedMesh = mesh;
            overlay = screen.GetComponent<MeshRenderer>();
            overlay.sharedMaterial = material;
            overlay.shadowCastingMode = ShadowCastingMode.Off;
            overlay.receiveShadows = false;
            overlay.lightProbeUsage = LightProbeUsage.Off;
            overlay.reflectionProbeUsage = ReflectionProbeUsage.Off;
            overlay.enabled = false;
            for (int layer = 0; layer < 32; layer++)
                if ((eye.cullingMask & (1 << layer)) != 0) { screen.layer = layer; break; }
            Subscribe();
        }

        private void LateUpdate()
        {
            if (rig == null || eye == null || material == null) return;
            var settings = VrRuntime.Settings;
            var player = rig.Player;
            var input = AppServices.GetOrNull<InputManager>();
            bool active = VrRuntime.IsActive && rig.HeadTracked;
            bool moving = active && player != null && !player.InputLocked && !player.MoveLocked &&
                Time.timeScale > 0f && input?.CurrentState == InputManager.InputState.Gameplay;
            float movement = moving ? player.MoveInput.magnitude : 0f;
            float turn = moving && !player.LookLocked && settings?.VrTurnMode == 1 ? VrRuntime.Input.Turn.x : 0f;
            if (Mathf.Abs(turn) <= (settings?.VrTurnDeadzone ?? 15) * 0.01f) turn = 0f;
            float target = VrComfortPolicy.MotionTarget(settings?.VrVignette ?? 0, movement, turn);
            vignette = VrComfortPolicy.Advance(vignette, target, Time.unscaledDeltaTime);
            float wallTarget = active && settings?.VrWallFade != false ? SampleWallFade() : 0f;
            fade = VrComfortPolicy.Advance(fade, wallTarget, Time.unscaledDeltaTime);
            material.SetFloat(VignetteId, vignette);
            material.SetFloat(FadeId, fade);
        }

        private float SampleWallFade()
        {
            const float radius = 0.12f;
            Vector3 head = eye.transform.position;
            int count = Physics.OverlapSphereNonAlloc(head, radius, overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float result = 0f;
            for (int i = 0; i < count; i++)
            {
                var collider = overlaps[i];
                if (collider == null || collider.transform.IsChildOf(rig.transform) ||
                    (rig.Player != null && collider.transform.IsChildOf(rig.Player.transform)) ||
                    collider.GetComponentInParent<MitaAI.MitaActor>() != null) continue;
                result = Mathf.Max(result, VrComfortPolicy.WallFade(Vector3.Distance(head, collider.ClosestPoint(head)), radius));
            }
            return result;
        }

        private void BeforeCamera(Camera camera)
        {
            if (overlay == null) return;
            visibility.Push(overlay.enabled);
            overlay.enabled = camera == eye && VrRuntime.IsActive && (vignette > 0.001f || fade > 0.001f);
        }

        private void AfterCamera(Camera camera)
        {
            if (overlay != null && visibility.Count > 0) overlay.enabled = visibility.Pop();
        }

        private void Subscribe()
        {
            if (overlay == null) return;
            VrHostEvents.Camera(BeforeCamera, true, false);
            VrHostEvents.Camera(AfterCamera, false, false);
            VrHostEvents.Camera(BeforeCamera, true, true);
            VrHostEvents.Camera(AfterCamera, false, true);
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            VrHostEvents.Camera(BeforeCamera, true, false);
            VrHostEvents.Camera(AfterCamera, false, false);
            if (overlay != null) overlay.enabled = false;
            visibility.Clear();
        }

        public void Clear()
        {
            OnDisable();
            if (overlay != null) DisposeObject(overlay.gameObject);
            if (mesh != null) DisposeObject(mesh);
            if (material != null) DisposeObject(material);
            overlay = null;
            mesh = null;
            material = null;
            rig = null;
            eye = null;
            vignette = fade = 0f;
        }

        private void OnDestroy() => Clear();

        private static void DisposeObject(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
