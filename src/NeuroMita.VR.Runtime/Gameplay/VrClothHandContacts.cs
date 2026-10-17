using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed partial class VrClothHandContacts : MonoBehaviour
    {
        private readonly HashSet<MagicaCloth> registered = new();
        private ColliderComponent left, right;
        private Transform owner;
        private float nextScan;

        public void Bind(ColliderComponent leftHand, ColliderComponent rightHand, Transform player)
        {
            Clear();
            left = leftHand;
            right = rightHand;
            owner = player;
            nextScan = 0;
        }

        private void Update()
        {
            if (!Application.isPlaying || left == null || right == null || Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            foreach (var cloth in Object.FindObjectsByType<MagicaCloth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                Register(cloth);
            registered.RemoveWhere(c => c == null);
        }

        public void Register(MagicaCloth cloth)
        {
            if (cloth == null || left == null || right == null || registered.Contains(cloth) ||
                (owner != null && cloth.transform.IsChildOf(owner)) ||
                cloth.SerializeData.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.None) return;
            var colliders = cloth.SerializeData.colliderCollisionConstraint.colliderList;
            if (!colliders.Contains(left)) colliders.Add(left);
            if (!colliders.Contains(right)) colliders.Add(right);
            registered.Add(cloth);
            cloth.SetParameterChange();
        }

        public void Clear()
        {
            Unregister();
            left = right = null;
            owner = null;
        }

        private void Unregister()
        {
            foreach (var cloth in registered)
            {
                if (cloth == null) continue;
                var colliders = cloth.SerializeData.colliderCollisionConstraint.colliderList;
                colliders.Remove(left);
                colliders.Remove(right);
                cloth.SetParameterChange();
            }
            registered.Clear();
        }

        private void OnEnable() => nextScan = 0;
        private void OnDisable() => Unregister();
    }
}
