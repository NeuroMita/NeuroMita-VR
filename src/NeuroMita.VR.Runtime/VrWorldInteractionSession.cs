using System;
using NeuroMita.Player;
using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrWorldInteractionSession : MonoBehaviour
    {
        private static VrWorldInteractionSession instance;
        private MonoBehaviour owner;
        private string kind;
        private float discoverAt;
        private bool releasedTvCamera, releasedEaselCamera;
        private MonoBehaviour distanceOwner;
        private float openedDistance;
        public VrWorldInteractionSession(IntPtr pointer) : base(pointer) { }

        internal static bool CanMove(PlayerActor player)
        {
            if (!VrRuntime.IsActive || player == null || VrRuntime.Rig?.Player != player ||
                !VrRuntime.Rig.HeadTracked || Time.timeScale <= 0f || SceneService.IsTransitionInProgress ||
                player.InputLocked || player.MoveLocked || player.Sequence?.IsPlaying == true || VrQteInput.HasActiveQte) return false;
            string focus = AppServices.GetOrNull<UIManager>()?.FocusedType?.FullName;
            return instance?.owner != null && focus == instance.kind;
        }

        internal static void ApplyMovement(PlayerActor player)
        {
            if (!CanMove(player)) return;
            Vector2 move = VrRuntime.Input.Move;
            player.RunHeld = player.CrouchHeld = false;
            if (player.Camera != null && NeuroMita.Modding.ExternalControlBridge.TryGetLocomotion(player,
                move, player.Camera.GetYawRotation().eulerAngles.y, out var mapped, out _)) move = mapped;
            player.MoveInput = move;
            if (instance.kind == typeof(TVInputContext).FullName && player.Camera != null)
            {
                if (!player.Camera.HasExternalOverride) instance.releasedTvCamera = false;
                if (!instance.releasedTvCamera && move.sqrMagnitude > 0.05f && player.Camera.HasExternalOverride)
                {
                    instance.releasedTvCamera = true;
                    player.Camera.EndExternalOverride(duration: 0.2f);
                }
            }
        }

        private void LateUpdate()
        {
            instance = this;
            if (!VrRuntime.IsActive) return;
            string focus = AppServices.GetOrNull<UIManager>()?.FocusedType?.FullName;
            if (focus != kind || Time.unscaledTime >= discoverAt)
            {
                discoverAt = Time.unscaledTime + 0.25f;
                if (focus != kind) { releasedTvCamera = releasedEaselCamera = false; distanceOwner = null; }
                kind = focus;
                owner = null;
                if (kind == typeof(EaselController).FullName)
                    foreach (var item in FindObjectsByType<EaselController>(FindObjectsSortMode.None)) { if (item.IsOpen) { owner = item; break; } }
                else if (kind == typeof(Radio).FullName)
                    foreach (var item in FindObjectsByType<Radio>(FindObjectsSortMode.None)) { if (item.IsUIOpen) { owner = item; break; } }
                else if (kind == typeof(MitaInteractionPanel).FullName) owner = FindFirstObjectByType<MitaInteractionPanel>();
                else if (kind == typeof(TVInputContext).FullName) owner = TVInputContext.Active;
            }
            var rig = VrRuntime.Rig;
            if (owner == null || rig?.HeadTracked != true) return;
            Vector3 anchor = owner.transform.position;
            var easelOwner = owner.TryCast<EaselController>();
            var mitaOwner = owner.TryCast<MitaInteractionPanel>();
            var tvOwner = owner.TryCast<TVInputContext>();
            if (easelOwner?.canvasQuad != null) anchor = easelOwner.canvasQuad.bounds.center;
            if (mitaOwner?.CurrentActor != null) anchor = mitaOwner.CurrentActor.PersonObject != null
                ? mitaOwner.CurrentActor.PersonObject.transform.position : mitaOwner.CurrentActor.transform.position;
            if (tvOwner != null)
            {
                var runtime = tvOwner.GetComponent<TVInteractionController>();
                if (runtime?.ScreenTarget != null) anchor = runtime.ScreenTarget.position;
            }
            if (easelOwner != null && !releasedEaselCamera && CanMove(rig.Player) && rig.Player.Camera?.HasExternalOverride == true)
            {
                rig.Player.Camera.ClearExternalOverride(restoreImmediately: true);
                rig.RefreshTrackingPose();
                releasedEaselCamera = true;
            }
            float distance = Vector3.ProjectOnPlane(rig.Eye.transform.position - anchor, Vector3.up).magnitude;
            if (distanceOwner != owner) { distanceOwner = owner; openedDistance = distance; }
            if (distance > openedDistance + 1f) CloseOwner();
        }

        private void CloseOwner()
        {
            var ui = AppServices.GetOrNull<UIManager>();
            if (owner == null) return;
            if (kind == typeof(TVInputContext).FullName) owner.GetComponent<TVInteractionController>()?.CloseYouTube();
            else if (kind == typeof(EaselController).FullName) ui?.RequestClose<EaselController>();
            else if (kind == typeof(Radio).FullName) ui?.RequestClose<Radio>();
            else if (kind == typeof(MitaInteractionPanel).FullName) ui?.RequestClose<MitaInteractionPanel>();
            owner = null;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            if (!SceneService.IsTransitionInProgress) CloseOwner();
            instance = null;
        }
    }
}
