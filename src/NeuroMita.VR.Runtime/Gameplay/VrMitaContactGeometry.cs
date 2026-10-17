using MitaAI;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrMitaContactGeometry
    {
        public static bool IsBodyCollider(MitaActor actor, Collider collider)
        {
            Transform root = actor.PersonObject != null ? actor.PersonObject.transform : actor.transform;
            if (collider.transform != root && !collider.transform.IsChildOf(root)) return false;
            if (IsBelow(collider.transform, actor.Links.FixedRightItem) || IsBelow(collider.transform, actor.Links.FixedLeftItem) ||
                IsBelow(collider.transform, actor.Links.BoneRightItem) || IsBelow(collider.transform, actor.Links.BoneLeftItem)) return false;
            if (collider.attachedRigidbody != null && collider.attachedRigidbody.transform != root &&
                collider.attachedRigidbody.transform != actor.transform) return false;
            return collider.transform == root || collider.transform == actor.transform ||
                (actor.Animator != null && collider.transform == actor.Animator.transform) ||
                collider.transform == actor.Links.Head || collider.transform == actor.Links.HeadBone;
        }

        private static bool IsBelow(Transform value, Transform parent) => parent != null &&
            (value == parent || value.IsChildOf(parent));
    }
}
