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
            if (stats != null) PresentHud(stats.GetComponent<RectTransform>(), new Vector2(-0.055f, 0f));
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
            if (interaction != null && interactionPanel != null)
            {
                var actor = interactionPanel.CurrentActor;
                if (actor != null)
                {
                    var headTarget = MitaAI.MitaAttentionTargetResolver.ResolveHead(actor);
                    Vector3 head = headTarget != null ? headTarget.position :
                        (actor.PersonObject != null ? actor.PersonObject.transform.position : actor.transform.position) + Vector3.up * 1.4f;
                    Quaternion actorFacing = FaceEye(head, eye);
                    Vector3 position = head + actorFacing * new Vector3(0.5f, -0.3f, 0f);
                    interaction.Place(position, FaceEye(position, eye), 0.55f, rig.Eye);
                }
            }
        }

        private static Quaternion FaceEye(Vector3 position, Transform eye)
        {
            Vector3 forward = position - eye.position;
            return forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, eye.up) : eye.rotation;
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + 0.25f;
                foreach (var view in views) view.Surface.RefreshGeometry();
                interaction?.RefreshGeometry();
            }
            ApplyLayout(VrRuntime.Settings?.VrHudOnWrist != false);
        }

        private void BeforeRender() => ApplyLayout(VrRuntime.Settings?.VrHudOnWrist != false);
        private void OnEnable() => VrFrameHooks.BeforeRender += BeforeRender;
        private void OnDisable() => VrFrameHooks.BeforeRender -= BeforeRender;

        public void Restore()
        {
            for (int i = views.Count - 1; i >= 0; i--) views[i].Surface.Dispose();
            views.Clear();
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
