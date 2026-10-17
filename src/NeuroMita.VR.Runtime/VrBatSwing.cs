using System;
using NeuroMita.Player;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrBatSwing : MonoBehaviour
    {
        private GameObject visual;
        private PlayerActor actor;
        private Vector3 localTip, previousTip, previousGrip, previousBody;
        private Quaternion previousBodyRotation;
        private float previousTime, quietSince = -1f, cooldownUntil;
        private bool hasPose, armed;
        public VrBatSwing(IntPtr pointer) : base(pointer) { }

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            var player = rig?.Player;
            if (!VrRuntime.IsActive || rig?.HeadTracked != true || !rig.RightHandTracked || player == null
                || player.Health?.IsDead == true || Time.timeScale <= 0f || SceneService.IsTransitionInProgress || player.InputLocked || player.MoveLocked
                || player.Sequence?.IsPlaying == true || VrQteInput.HasActiveQte || VrRuntime.Presenter?.HasUiTarget == true
                || AppServices.GetOrNull<InputManager>()?.CurrentState != InputManager.InputState.Gameplay)
            { ResetPose(); return; }
            var placement = player.ItemPlacementManager;
            var item = placement?.EquippedItem;
            var nextVisual = placement?.EquippedVisual;
            var mount = player.MountRig?.RightHandMount?.ItemMount;
            if (item == null || player.Inventory?.SelectedItem != item || nextVisual == null || mount == null
                || !nextVisual.activeInHierarchy || !nextVisual.transform.IsChildOf(mount) || !IsBat(item)
                || player.GetComponent<PlayerBatStrikeController>()?.strikeRoutine != null)
            { ResetPose(); return; }
            if (visual != nextVisual || actor != player)
            {
                ResetPose(); visual = nextVisual; actor = player;
                if (!FindTip()) { visual = null; return; }
            }
            Vector3 tip = visual.transform.TransformPoint(localTip);
            Vector3 grip = mount.position;
            Vector3 body = player.transform.position;
            float now = Time.unscaledTime, dt = now - previousTime;
            if (!hasPose || dt <= 0f || dt > 0.1f || Vector3.Distance(tip, previousTip) > 0.5f
                || Vector3.Distance(body, previousBody) > 0.15f
                || Quaternion.Angle(player.transform.rotation, previousBodyRotation) > 15f)
            {
                armed = false; quietSince = -1f;
                Store(player, tip, grip, body, now); return;
            }
            float speed = ((tip - previousTip) - (body - previousBody)).magnitude / dt;
            if (speed < 0.45f)
            {
                if (quietSince < 0f) quietSince = now;
                if (now - quietSince >= 0.15f && now >= cooldownUntil) armed = true;
            }
            else quietSince = -1f;
            if (armed && speed >= 1.5f && now >= cooldownUntil)
            {
                Mob_Maneken hit = Sweep(grip, tip);
                if (hit != null)
                {
                    armed = false; cooldownUntil = now + 0.75f;
                    if (hit.DisableByPlayerStrike()) Debug.Log("[NeuroMita VR] Bat swing hit mannequin");
                }
            }
            Store(player, tip, grip, body, now);
        }
        private static bool IsBat(ItemDefinition item)
        {
            if (item.Actions == null) return false;
            foreach (var binding in item.Actions)
                if (binding.Action != null && binding.Action.TryCast<BatStrikeAction>() != null) return true;
            return false;
        }
        private bool FindTip()
        {
            float farthest = 0.15f;
            foreach (var filter in visual.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                Bounds bounds = filter.sharedMesh.bounds;
                Vector3 extent = bounds.extents;
                Vector3 axis = extent.x >= extent.y && extent.x >= extent.z ? Vector3.right * extent.x
                    : extent.y >= extent.z ? Vector3.up * extent.y : Vector3.forward * extent.z;
                foreach (float sign in new[] { -1f, 1f })
                {
                    Vector3 world = filter.transform.TransformPoint(bounds.center + axis * sign);
                    float reach = Vector3.Distance(world, actor.MountRig.RightHandMount.ItemMount.position);
                    if (reach <= farthest || reach > 1.5f) continue;
                    farthest = reach; localTip = visual.transform.InverseTransformPoint(world);
                }
            }
            return farthest > 0.15f;
        }
        private Mob_Maneken Sweep(Vector3 grip, Vector3 tip)
        {
            // Sweep the working half of the shaft; each sample also needs a clear path from the hand.
            for (int i = 0; i <= 5; i++)
            {
                float t = 0.5f + i * 0.1f;
                Vector3 start = Vector3.Lerp(previousGrip, previousTip, t);
                Vector3 end = Vector3.Lerp(grip, tip, t);
                Vector3 delta = end - start;
                if (delta.sqrMagnitude < 0.000001f) continue;
                var hits = Physics.SphereCastAll(start, 0.035f, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
                RaycastHit nearest = default;
                bool found = false;
                foreach (var hit in hits)
                {
                    if (Ignore(hit.collider)) continue;
                    if (!found || hit.distance < nearest.distance) { nearest = hit; found = true; }
                }
                if (!found) continue;
                var mannequin = nearest.collider.GetComponentInParent<Mob_Maneken>();
                if (mannequin == null || !mannequin.isActiveAndEnabled) continue;
                Vector3 reach = nearest.point - grip;
                bool blocked = false;
                Vector3 eye = VrRuntime.Rig.Eye.transform.position;
                Vector3 toGrip = grip - eye;
                foreach (var obstruction in Physics.RaycastAll(eye, toGrip.normalized, toGrip.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (!Ignore(obstruction.collider)) { blocked = true; break; }
                foreach (var obstruction in Physics.RaycastAll(grip, reach.normalized, reach.magnitude, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (Ignore(obstruction.collider) || obstruction.collider.GetComponentInParent<Mob_Maneken>() == mannequin) continue;
                    blocked = true; break;
                }
                if (!blocked) return mannequin;
            }
            return null;
        }
        private bool Ignore(Collider collider) => collider == null || collider.transform.IsChildOf(actor.transform)
            || collider.transform.IsChildOf(visual.transform) || collider.GetComponentInParent<VrHandProxy>() != null;
        private void Store(PlayerActor player, Vector3 tip, Vector3 grip, Vector3 body, float now)
        {
            previousTip = tip; previousGrip = grip; previousBody = body;
            previousBodyRotation = player.transform.rotation; previousTime = now; hasPose = true;
        }
        private void ResetPose() { hasPose = armed = false; quietSince = -1f; visual = null; actor = null; }
    }
}
