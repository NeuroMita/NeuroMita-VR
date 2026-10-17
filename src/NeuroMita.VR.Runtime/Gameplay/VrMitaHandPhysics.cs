using System.Collections.Generic;
using MitaAI;
using UnityEngine;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrMitaHandPhysics : MonoBehaviour
    {
        private sealed class ActorContacts
        {
            public readonly List<Collider> Colliders = new();
            public Rigidbody Body;
            public readonly VrMitaPressurePolicy Pressure = new();
        }

        private readonly Dictionary<MitaActor, ActorContacts> actors = new();
        private readonly Dictionary<Collider, bool> leftOriginal = new(), rightOriginal = new();
        private readonly HashSet<MitaActor> observed = new();
        private readonly HashSet<Collider> suppressed = new();
        private readonly List<Collider> released = new();
        private VrHandProxy left, right;
        private VrPlayerRig rig;
        private int leftRevision, rightRevision;

        public void Bind(VrPlayerRig owner, VrHandProxy leftHand, VrHandProxy rightHand)
        {
            rig = owner; left = leftHand; right = rightHand;
        }

        private void Update()
        {
            if (GameplayPauseClock.IsPaused || Time.timeScale <= 0 || rig?.Player?.Health?.IsDead == true)
                ResetPressure();
        }

        private void FixedUpdate()
        {
            var active = VrHostViews.ActiveActors();
            if (active == null || left == null || right == null) return;
            if (leftRevision != left.ContactRevision || rightRevision != right.ContactRevision) ResetPressure();
            leftRevision = left.ContactRevision;
            rightRevision = right.ContactRevision;
            observed.Clear();
            suppressed.Clear();
            foreach (var actor in active)
            {
                if (actor == null || !actor.IsLive) continue;
                observed.Add(actor);
                if (!actors.TryGetValue(actor, out var state))
                {
                    state = new ActorContacts();
                    actors.Add(actor, state);
                }
                var person = actor.PersonObject != null ? actor.PersonObject : actor.gameObject;
                state.Colliders.Clear();
                foreach (var item in person.GetComponentsInChildren<Collider>(true)) state.Colliders.Add(item);
                state.Body = person.GetComponent<Rigidbody>();
                float depth = 0;
                Vector3 direction = Vector3.zero;
                foreach (var collider in state.Colliders)
                {
                    if (collider == null || collider.isTrigger ||
                        ((state.Body == null || collider.attachedRigidbody != state.Body) && !VrMitaContactGeometry.IsBodyCollider(actor, collider))) continue;
                    suppressed.Add(collider);
                    Ignore(left, collider, leftOriginal);
                    Ignore(right, collider, rightOriginal);
                    if (collider.TryCast<CapsuleCollider>() != null && collider.transform == state.Body?.transform && collider.bounds.size.y > 0.5f)
                    {
                        Measure(left, collider, ref depth, ref direction);
                        Measure(right, collider, ref depth, ref direction);
                    }
                }
                var body = state.Body;
                bool allowed = rig != null && VrRuntime.Owns(rig.Player) && rig.HeadTracked &&
                    rig.Player.Health?.IsDead != true && !GameplayPauseClock.IsPaused && body != null &&
                    NeuroMita.World.Perception.MitaSpatialAwareness.ResolveWorld(actor) == WorldRuntime.Current &&
                    !body.isKinematic && body.detectCollisions && actor.State?.CoreState != MitaStateType.hunt &&
                    !SequenceService.IsActive(actor) && !VrHostViews.IsScripted(actor) &&
                    actor.LifeRuntime?.IsSleeping != true &&
                    !(actor.NavAgent != null && actor.NavAgent.enabled && (actor.NavAgent.pathPending || actor.NavAgent.hasPath));
                Vector3 acceleration = state.Pressure.Sample(depth, direction, body != null ? body.linearVelocity : Vector3.zero,
                    Time.fixedDeltaTime, allowed);
                if (acceleration.sqrMagnitude > 0) body.AddForce(acceleration, ForceMode.Acceleration);
            }
            foreach (var pair in actors)
                if (!observed.Contains(pair.Key)) pair.Value.Pressure.Reset();
            Reconcile(left, leftOriginal);
            Reconcile(right, rightOriginal);
        }

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void Reconcile(VrHandProxy hand, Dictionary<Collider, bool> originals)
        {
            released.Clear();
            var palm = hand.GetComponent<BoxCollider>();
            foreach (var pair in originals)
                if (pair.Key == null || !suppressed.Contains(pair.Key))
                {
                    if (pair.Key != null) Physics.IgnoreCollision(palm, pair.Key, pair.Value);
                    released.Add(pair.Key);
                }
            foreach (var collider in released) originals.Remove(collider);
        }

        private static void Ignore(VrHandProxy hand, Collider collider, Dictionary<Collider, bool> originals)
        {
            var palm = hand.GetComponent<BoxCollider>();
            if (!originals.ContainsKey(collider)) originals.Add(collider, Physics.GetIgnoreCollision(palm, collider));
            Physics.IgnoreCollision(palm, collider, true);
        }

        private static void Measure(VrHandProxy hand, Collider collider, ref float depth, ref Vector3 direction)
        {
            if (!hand.ContactAvailable || !collider.enabled || !collider.gameObject.activeInHierarchy) return;
            var palm = hand.GetComponent<BoxCollider>();
            var palmBody = palm.attachedRigidbody;
            if (Physics.ComputePenetration(palm, palmBody.position, palmBody.rotation, collider,
                collider.transform.position, collider.transform.rotation, out var normal, out var distance) && distance > depth)
            {
                depth = distance;
                direction = -normal;
            }
        }

        private void ResetPressure()
        {
            foreach (var state in actors.Values) state.Pressure.Reset();
        }

        public void Clear()
        {
            Restore(left, leftOriginal);
            Restore(right, rightOriginal);
            actors.Clear();
        }

        private static void Restore(VrHandProxy hand, Dictionary<Collider, bool> originals)
        {
            if (hand != null)
            {
                var palm = hand.GetComponent<BoxCollider>();
                foreach (var pair in originals)
                    if (pair.Key != null) Physics.IgnoreCollision(palm, pair.Key, pair.Value);
            }
            originals.Clear();
        }

        private void OnDisable() => ResetPressure();
        private void OnDestroy() => Clear();
    }
}
