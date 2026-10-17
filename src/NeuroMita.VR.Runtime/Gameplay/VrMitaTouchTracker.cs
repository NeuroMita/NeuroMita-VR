using System.Collections.Generic;
using MitaAI;
using NeuroMita.World.Perception;
using UnityEngine;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10030)]
#endif
    public sealed partial class VrMitaTouchTracker : MonoBehaviour
    {
        private readonly Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider> hits = new(64);
        private readonly Dictionary<MitaActor, VrTouchSession> sessions = new();
        private readonly Dictionary<MitaActor, bool> contacts = new();
        private readonly List<MitaActor> expired = new();
        private VrPlayerRig rig;
        private VrHandProxy left, right;
        private IReadOnlyList<MitaActor> actors;
        private float nextActorsRefresh;
        private int leftRevision, rightRevision;

        private bool CanSample => VrRuntime.Settings?.VrTouchAi != false && rig != null && VrRuntime.Owns(rig.Player) && rig.HeadTracked &&
            rig.Player?.Health?.IsDead != true && !GameplayPauseClock.IsPaused && Time.timeScale > 0f &&
            ((left != null && left.ContactAvailable) || (right != null && right.ContactAvailable));

        private void Update()
        {
            CheckHandContinuity();
            if (!CanSample) Cancel();
        }

        private void CheckHandContinuity()
        {
            int l = left != null ? left.ContactRevision : 0;
            int r = right != null ? right.ContactRevision : 0;
            if (l != leftRevision || r != rightRevision) Cancel();
            leftRevision = l;
            rightRevision = r;
        }

        public void Bind(VrPlayerRig owner, VrHandProxy leftHand, VrHandProxy rightHand)
        {
            rig = owner; left = leftHand; right = rightHand;
        }

        private void FixedUpdate()
        {
            CheckHandContinuity();
            if (!CanSample)
            {
                Cancel();
                return;
            }
            float now = GameplayPauseClock.Time;
            if (actors == null || now >= nextActorsRefresh)
            {
                actors = VrHostViews.ActiveActors();
                nextActorsRefresh = now + 0.5f;
            }
            contacts.Clear();
            Collect(left);
            Collect(right);
            foreach (var pair in contacts)
                if (!sessions.ContainsKey(pair.Key)) sessions.Add(pair.Key, new VrTouchSession());
            expired.Clear();
            foreach (var pair in sessions)
            {
                var actor = pair.Key;
                if (!IsEligible(actor))
                {
                    if (actor != null && pair.Value.TouchCount > 0)
                        RuntimeEventReporter.ReportPlayerContact(actor, pair.Value.ContactSeconds, pair.Value.TouchCount, pair.Value.HeadTouched, false, true);
                    pair.Value.Cancel();
                    if (actor == null) expired.Add(actor);
                    continue;
                }
                bool touched = contacts.TryGetValue(actor, out bool head);
                var session = pair.Value;
                if (session.Sample(touched, head, Time.fixedUnscaledDeltaTime, now))
                    RuntimeEventReporter.ReportPlayerContact(actor, session.ContactSeconds, session.TouchCount,
                        session.HeadTouched, session.InContact, false);
                if (touched && session.ReactionDue(now))
                {
                    bool accepted = CanReact(actor) && !string.IsNullOrWhiteSpace(DialogueApi.SendReact(
                        actor.CharacterType, ReactLevel.Answer, ReactReasonType.Generic,
                        $"The player has touched you with VR hands {session.TouchCount} times, with {session.ContactSeconds:0.#} seconds of contact" +
                        (session.HeadTouched ? ", including your head/hair. " : ". ") +
                        "React naturally to this physical contact; its emotional meaning is up to you.", 5f));
                    session.ReactionAttempted(now, accepted);
                }
            }
            foreach (var actor in expired) sessions.Remove(actor);
        }

        private void Collect(VrHandProxy hand)
        {
            if (hand == null || !hand.ContactAvailable) return;
            var palm = hand.GetComponent<BoxCollider>();
            var body = palm.attachedRigidbody;
            Vector3 center = body.position + body.rotation * Vector3.Scale(palm.center, palm.transform.lossyScale);
            int count = Physics.OverlapBoxNonAlloc(center,
                Vector3.Scale(palm.size * 0.5f, palm.transform.lossyScale) + Vector3.one * 0.004f,
                hits, body.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = hits[i];
                var actor = collider.GetComponentInParent<MitaActor>();
                if (actor == null && actors != null)
                    foreach (var candidate in actors)
                        if (candidate != null && candidate.PersonObject != null && collider.transform.IsChildOf(candidate.PersonObject.transform))
                        { actor = candidate; break; }
                if (!IsEligible(actor) || !VrMitaContactGeometry.IsBodyCollider(actor, collider)) continue;
                var head = actor.Links.HeadBone != null ? actor.Links.HeadBone : actor.Links.Head;
                bool isHead = head != null && (collider.transform == head || collider.transform.IsChildOf(head));
                contacts[actor] = isHead || (contacts.TryGetValue(actor, out bool previous) && previous);
            }
        }

        private bool IsEligible(MitaActor actor)
        {
            if (actor == null || !actor.isActiveAndEnabled || !actor.IsLive || actor.CharacterType == CharacterType.None || actors == null)
                return false;
            bool active = false;
            foreach (var candidate in actors) if (candidate == actor) { active = true; break; }
            var world = MitaSpatialAwareness.ResolveWorld(actor);
            return active && world != null && world == WorldRuntime.Current;
        }

        private static bool CanReact(MitaActor actor) => DialogueApi.CanAcceptInteractionReaction(actor);

        public void Cancel()
        {
            foreach (var pair in sessions)
            {
                if (pair.Key != null && pair.Value.TouchCount > 0)
                    RuntimeEventReporter.ReportPlayerContact(pair.Key, pair.Value.ContactSeconds, pair.Value.TouchCount, pair.Value.HeadTouched, false, true);
                pair.Value.Cancel();
            }
            contacts.Clear();
        }

        private void OnDisable() => Cancel();
    }
}
