using UnityEngine;
using MitaAI;

namespace NeuroMita.VR
{
    public sealed partial class VrHandProxy
    {
        private readonly Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Collider> feedbackHits = new(64);
        private bool feedbackPrimed, feedbackTouching;
        private int feedbackRevision;
        private float feedbackLastTouch;
        private void UpdateContactHaptics()
        {
            var rig = VrRuntime.Rig;
            if (!ContactAvailable || rig?.Player == null || !VrRuntime.Owns(rig.Player) || !rig.HeadTracked
                || Time.timeScale <= 0f || GameplayPauseClock.IsPaused || rig.Player.Health?.IsDead == true
                || VrQteInput.HasActiveQte || VrDeathInput.IsVisible || SceneService.IsTransitionInProgress || VrTvPresenter.IsViewing)
            { feedbackPrimed = false; return; }
            if (feedbackRevision != ContactRevision) { feedbackPrimed = false; feedbackRevision = ContactRevision; }
            Vector3 scale = palm.transform.lossyScale;
            Vector3 center = physicsPosition + physicsRotation * Vector3.Scale(palm.center, scale);
            int count = Physics.OverlapBoxNonAlloc(center, Vector3.Scale(palm.size * 0.5f, scale) + Vector3.one * 0.004f,
                feedbackHits, physicsRotation, ~0, QueryTriggerInteraction.Ignore);
            bool touching = false;
            for (int i = 0; i < count; i++)
            {
                var collider = feedbackHits[i];
                if (collider == null || collider == palm || collider.isTrigger || collider.GetComponentInParent<VrHandProxy>() != null
                    || collider.transform.IsChildOf(rig.Player.transform)) continue;
                touching = true; break;
            }
            float now = Time.unscaledTime;
            if (!feedbackPrimed) { feedbackPrimed = true; feedbackTouching = touching; }
            if (touching)
            {
                if (!feedbackTouching) VrHaptics.Contact(target);
                feedbackTouching = true;
                feedbackLastTouch = now;
            }
            else if (now - feedbackLastTouch >= 0.1f) feedbackTouching = false;
        }
    }
}
