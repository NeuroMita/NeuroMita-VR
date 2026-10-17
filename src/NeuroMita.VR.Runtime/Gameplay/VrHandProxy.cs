using System.Collections.Generic;
using UnityEngine;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10020)]
#endif
    public sealed partial class VrHandProxy : MonoBehaviour
    {
        private Rigidbody body;
        private BoxCollider palm;
        private MeshRenderer visual;
        private Transform target;
        private bool tracked;
        private bool hasPose;
        private bool bodyHasPose, suspended;
        private Vector3 resolvedPosition, lastTargetPosition;
        private Quaternion resolvedRotation;
        private Vector3 physicsPosition;
        private Quaternion physicsRotation;
        private readonly RaycastHit[] sweepHits = new RaycastHit[128];
        private readonly Collider[] overlaps = new Collider[128];
        private const float WallSkin = 0.002f;
        private float contactResumeTime;
        public bool ContactAvailable => tracked && palm != null && palm.enabled && Time.time >= contactResumeTime;
        public int ContactRevision { get; private set; }
        public MagicaCloth2.MagicaCapsuleCollider ClothCollider { get; private set; }
        private Camera eye;
        private readonly Stack<bool> cameraVisibility = new();

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        public void Bind(Transform controller, Mesh mesh, Material[] materials, Collider[] ownerColliders, Camera headsetCamera = null)
        {
            target = controller;
            eye = headsetCamera;
            gameObject.AddComponent<NeuroMita.World.Interaction.InteractionRayIgnore>();
            var model = VrObject.Create("Visible hand");
            model.transform.SetParent(transform, false);
            var filter = model.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            visual = model.AddComponent<MeshRenderer>();
            visual.sharedMaterials = materials;
            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            palm = gameObject.AddComponent<BoxCollider>();
            palm.center = mesh.bounds.center;
            palm.size = Vector3.Max(mesh.bounds.size, new Vector3(0.075f, 0.035f, 0.11f));
            ClothCollider = gameObject.AddComponent<MagicaCloth2.MagicaCapsuleCollider>();
            ClothCollider.direction = MagicaCloth2.MagicaCapsuleCollider.Direction.Z;
            ClothCollider.center = palm.center;
            ClothCollider.SetSize(0.028f, 0.028f, 0.12f);
            ClothCollider.enabled = false;
            palm.enabled = visual.enabled = false;
            foreach (var collider in ownerColliders)
                if (collider != null) Physics.IgnoreCollision(palm, collider);
        }

        public void SetTracked(bool value)
        {
            if (tracked && !value) ContactRevision++;
            if (tracked != value) { hasPose = false; bodyHasPose = false; suspended = false; }
            tracked = value;
            if (!tracked && palm != null) palm.enabled = false;
            if (!tracked && ClothCollider != null) ClothCollider.enabled = false;
            UpdateVisual();
        }

        private void FixedUpdate()
        {
            bool enabled = tracked && target != null && Time.timeScale > 0f && VrRuntime.Settings?.VrHandPhysics != false;
            if (!enabled) { palm.enabled = false; ClothCollider.enabled = false; suspended = true; return; }
            ResolvePose();
            if (!palm.enabled)
            {
                contactResumeTime = Time.time + 0.2f;
                ContactRevision++;
                ClothCollider.enabled = false;
                if (!bodyHasPose)
                {
                    body.position = resolvedPosition;
                    body.rotation = resolvedRotation;
                    bodyHasPose = true;
                }
                palm.enabled = true;
            }
            ClothCollider.enabled = true;
            ConstrainPhysicsStep(Vector3.MoveTowards(body.position, resolvedPosition, 3f * Time.fixedDeltaTime), resolvedRotation);
            body.MovePosition(physicsPosition);
            body.MoveRotation(physicsRotation);
        }

        private void ConstrainPhysicsStep(Vector3 desiredPosition, Quaternion desiredRotation)
        {
            physicsRotation = IsClear(body.position, desiredRotation) ? desiredRotation : body.rotation;
            physicsPosition = Sweep(Depenetrate(body.position, physicsRotation), desiredPosition, physicsRotation);
        }

        private void LateUpdate()
        {
            if (Time.timeScale <= 0f)
            {
                suspended = true;
                if (palm != null) palm.enabled = false;
                if (ClothCollider != null) ClothCollider.enabled = false;
            }
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (visual == null) return;
            visual.enabled = tracked && target != null && VrAvatarPolicy.ShowIsolatedHands(VrRuntime.Settings?.VrAvatarMode ?? 0);
            if (visual.enabled)
            {
                ResolvePose();
                visual.transform.SetPositionAndRotation(resolvedPosition, resolvedRotation);
            }
        }

        private void ResolvePose()
        {
            bool wasEnabled = palm.enabled;
            if (VrRuntime.Settings?.VrHandPhysics != false) palm.enabled = true;
            try { ResolvePoseCore(); }
            finally { palm.enabled = wasEnabled; }
        }

        private void ResolvePoseCore()
        {
            bool discontinuity = hasPose && !suspended && Time.timeScale > 0f && Vector3.Distance(lastTargetPosition, target.position) > 0.75f;
            suspended = Time.timeScale <= 0f;
            bool constrain = VrRuntime.Settings?.VrHandPhysics != false;
            if (!hasPose || discontinuity || !constrain)
            {
                resolvedPosition = target.position;
                resolvedRotation = target.rotation;
                hasPose = true;
                if (constrain) resolvedPosition = Depenetrate(resolvedPosition, resolvedRotation);
                if (discontinuity)
                {
                    contactResumeTime = Time.time + 0.2f;
                    ContactRevision++;
                    body.position = resolvedPosition;
                    body.rotation = resolvedRotation;
                }
            }
            else
            {
                resolvedPosition = Depenetrate(resolvedPosition, resolvedRotation);
                if (IsClear(resolvedPosition, target.rotation)) resolvedRotation = target.rotation;
                resolvedPosition = Sweep(resolvedPosition, target.position, resolvedRotation);
            }
            lastTargetPosition = target.position;
        }

        private Vector3 Sweep(Vector3 position, Vector3 destination, Quaternion rotation)
        {
            Vector3 remaining = destination - position;
            for (int iteration = 0; iteration < 3 && remaining.sqrMagnitude > 0.000001f; iteration++)
            {
                float distance = remaining.magnitude;
                Vector3 direction = remaining / distance;
                int count = Physics.BoxCastNonAlloc(position + rotation * palm.center,
                    palm.size * 0.5f, direction, sweepHits, rotation, distance + WallSkin,
                    Physics.AllLayers, QueryTriggerInteraction.Ignore);
                if (count == sweepHits.Length) break;
                float travel = distance;
                Vector3 normal = Vector3.zero;
                for (int i = 0; i < count; i++)
                {
                    var hit = sweepHits[i];
                    if (!BlocksHand(hit.collider) || Vector3.Dot(direction, hit.normal) >= -0.0001f) continue;
                    if (hit.distance - WallSkin < travel)
                    {
                        travel = Mathf.Max(0f, hit.distance - WallSkin);
                        normal = hit.normal;
                    }
                }
                position += direction * travel;
                remaining = normal == Vector3.zero ? Vector3.zero :
                    Vector3.ProjectOnPlane(remaining - direction * travel, normal);
            }
            return position;
        }

        private bool BlocksHand(Collider collider)
        {
            if (collider == null || collider == palm || collider.isTrigger ||
                Physics.GetIgnoreLayerCollision(gameObject.layer, collider.gameObject.layer) ||
                Physics.GetIgnoreCollision(palm, collider)) return false;
            return collider.GetComponentInParent<MitaAI.MitaActor>() == null;
        }

        private bool IsClear(Vector3 position, Quaternion rotation)
        {
            int count = FindOverlaps(position, rotation);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (BlocksHand(overlaps[i]) && Penetration(overlaps[i], position, rotation, out _, out float depth) && depth > WallSkin)
                    return false;
            return true;
        }

        private int FindOverlaps(Vector3 position, Quaternion rotation) =>
            Physics.OverlapBoxNonAlloc(position + rotation * palm.center, palm.size * 0.5f,
                overlaps, rotation, Physics.AllLayers, QueryTriggerInteraction.Ignore);

        private bool Penetration(Collider collider, Vector3 position, Quaternion rotation, out Vector3 direction, out float depth) =>
            Physics.ComputePenetration(palm, position, rotation, collider,
                collider.transform.position, collider.transform.rotation, out direction, out depth);

        private Vector3 Depenetrate(Vector3 position, Quaternion rotation)
        {
            for (int iteration = 0; iteration < 8; iteration++)
            {
                int count = FindOverlaps(position, rotation);
                bool moved = false;
                for (int i = 0; i < count; i++)
                {
                    var collider = overlaps[i];
                    if (!BlocksHand(collider) || !Penetration(collider, position, rotation, out var direction, out float depth)) continue;
                    position += direction * (depth + WallSkin);
                    moved = true;
                }
                if (!moved) break;
            }
            return position;
        }

        private void OnEnable()
        {
            VrFrameHooks.BeforeRender += UpdateVisual;
            VrHostEvents.Camera(BeforeCamera, true, true);
            VrHostEvents.Camera(AfterCamera, false, true);
        }

        private void BeforeCamera(Camera camera)
        {
            if (visual == null) return;
            cameraVisibility.Push(visual.enabled);
            visual.enabled = tracked && target != null && VrAvatarPolicy.ShowIsolatedHands(VrRuntime.Settings?.VrAvatarMode ?? 0) && (eye == null || camera == eye);
        }

        private void AfterCamera(Camera camera)
        {
            if (visual != null && cameraVisibility.Count > 0) visual.enabled = cameraVisibility.Pop();
        }

        private void OnDisable()
        {
            VrFrameHooks.BeforeRender -= UpdateVisual;
            VrHostEvents.Camera(BeforeCamera, true, false);
            VrHostEvents.Camera(AfterCamera, false, false);
            cameraVisibility.Clear();
            if (palm != null) palm.enabled = false;
            if (ClothCollider != null) ClothCollider.enabled = false;
            if (visual != null) visual.enabled = false;
        }
    }
}
