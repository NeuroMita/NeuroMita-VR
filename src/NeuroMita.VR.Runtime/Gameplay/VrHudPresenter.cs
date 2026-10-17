using System.Collections.Generic;
using UnityEngine;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10015)]
#endif
    public sealed partial class VrHudPresenter : MonoBehaviour
    {
        private sealed class HudView
        {
            public VrUiSurface Surface;
            public Vector2 Offset;
        }

        private readonly List<HudView> views = new();
        private VrPlayerRig rig;
        private VrUiPresenter pointer;
        private GameUIRoot owner;
        private VrUiSurface interaction;
        private VrUiSurface mood;
        private CanvasGroup reticleMask;
        private float reticleAlpha;
        private bool reticleMaskCreated;
        private MitaInteractionPanel interactionPanel;
        private VrUiSurface roster;
        private VrUiSurface desktopHotbar;
        private float refreshAt;

        public void Bind(VrPlayerRig playerRig, VrUiPresenter uiPointer)
        {
            rig = playerRig;
            pointer = uiPointer;
        }

        public void RefreshViews(GameUIRoot ui)
        {
            if (rig?.Eye == null) return;
            if (ui == owner && owner != null) return;
            Restore();
            owner = ui;
            if (owner == null) return;
            var desktopCrosshair = owner.GetComponentInChildren<CrosshairUIController>(true);
            if (desktopCrosshair != null)
            {
                reticleMask = desktopCrosshair.GetComponent<CanvasGroup>();
                reticleMaskCreated = reticleMask == null;
                if (reticleMaskCreated) reticleMask = desktopCrosshair.gameObject.AddComponent<CanvasGroup>();
                reticleAlpha = reticleMask.alpha;
                reticleMask.alpha = 0f;
            }
            var miniInventory = owner.GetComponentInChildren<InventoryMiniUI>(true);
            if (miniInventory?.inventoryUIParent != null && miniInventory.inventoryUIParent.GetComponent<RectTransform>() is { } hotbar)
            {
                desktopHotbar = new VrUiSurface(hotbar, rig.Eye, pointer);
                desktopHotbar.Canvas.enabled = false;
            }
            var activeCharacters = owner.GetComponentInChildren<MitaAI.UI.ActiveCharactersUI>(true);
            if (activeCharacters != null && activeCharacters.GetComponent<RectTransform>() is { } rosterView)
            {
                roster = new VrUiSurface(rosterView, rig.Eye, pointer);
                roster.Canvas.enabled = false;
            }
            var stats = owner.GetComponentInChildren<MitaUnitFrame>(true);
            if (stats != null) mood = new VrUiSurface(stats.GetComponent<RectTransform>(), rig.Eye, pointer);
            if (owner.MicrophoneIndicator != null)
                PresentHud(owner.MicrophoneIndicator.GetComponent<RectTransform>(), new Vector2(0.13f, 0f));
            interactionPanel = owner.MitaInteractionPanel;
            if (interactionPanel != null && interactionPanel.GetComponent<RectTransform>() is { } actions)
                interaction = new VrUiSurface(actions, rig.Eye, pointer);
        }

        public void PresentHud(RectTransform view, Vector2 offset)
        {
            if (view == null || rig?.Eye == null || views.Exists(v => v.Surface.View == view)) return;
            views.Add(new HudView { Surface = new VrUiSurface(view, rig.Eye, pointer), Offset = offset });
        }

        public void ApplyLayout(bool wrist)
        {
            if (rig?.Eye == null) return;
            var eye = rig.Eye.transform;
            wrist &= rig.LeftHandTracked && rig.LeftHand != null;
            Vector3 center = wrist ? rig.LeftHand.position + rig.LeftHand.up * 0.08f :
                eye.position + eye.rotation * new Vector3(0f, -0.32f, 0.9f);
            Quaternion facing = wrist ? FaceEye(center, eye) : eye.rotation;
            float factor = wrist ? 1f : 1.8f;
            foreach (var view in views)
            {
                float width = view.Surface.View != null && view.Surface.View.GetComponent<AsrMicrophoneIndicator>() != null ? 0.06f : 0.24f;
                view.Surface.Place(center + facing * (Vector3)(view.Offset * factor), facing, width * factor, rig.Eye);
            }
            if (mood?.Canvas != null) mood.Canvas.enabled = false;
            if (interaction != null && interactionPanel != null && interactionPanel.isActiveAndEnabled)
            {
                var actor = interactionPanel.CurrentActor;
                if (actor != null)
                {
                    var headTarget = MitaAI.MitaAttentionTargetResolver.ResolveHead(actor);
                    Vector3 head = headTarget != null ? headTarget.position :
                        (actor.PersonObject != null ? actor.PersonObject.transform.position : actor.transform.position) + Vector3.up * 1.4f;
                    var body = actor.PersonObject != null ? actor.PersonObject.transform : actor.transform;
                    Vector3 side = Vector3.ProjectOnPlane(body.right, Vector3.up).normalized;
                    if (side.sqrMagnitude < 0.1f) side = Vector3.right;
                    Vector3 position = head + side * 0.55f + Vector3.down * 0.3f;
                    if (mood?.Canvas != null)
                    {
                        mood.Canvas.enabled = true;
                        Vector3 above = head + Vector3.up * 0.5f;
                        mood.Place(above, FaceEye(above, eye), 0.42f, rig.Eye);
                    }
                    interaction.Place(position, FaceEye(position, eye), 0.55f, rig.Eye);
                }
            }
        }

        private static Quaternion FaceEye(Vector3 position, Transform eye)
        {
            Vector3 forward = Vector3.ProjectOnPlane(position - eye.position, Vector3.up);
            return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, Vector3.up) :
                Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.25f;
                foreach (var view in views) view.Surface.RefreshGeometry();
                interaction?.RefreshGeometry();
                mood?.RefreshGeometry();
            }
            ApplyLayout(VrRuntime.Settings?.VrHudOnWrist != false);
        }

        private void BeforeRender() => ApplyLayout(VrRuntime.Settings?.VrHudOnWrist != false);
        private void OnEnable() => VrFrameHooks.BeforeRender += BeforeRender;
        private void OnDisable() => VrFrameHooks.BeforeRender -= BeforeRender;

        public void Restore()
        {
            if (reticleMask != null)
            {
                reticleMask.alpha = reticleAlpha;
                if (reticleMaskCreated) Destroy(reticleMask);
            }
            reticleMask = null;
            for (int i = views.Count - 1; i >= 0; i--) views[i].Surface.Dispose();
            views.Clear();
            mood?.Dispose();
            mood = null;
            roster?.Dispose();
            roster = null;
            desktopHotbar?.Dispose();
            desktopHotbar = null;
            interaction?.Dispose();
            interaction = null;
            interactionPanel = null;
            owner = null;
        }

        private void OnDestroy() => Restore();
    }
}
