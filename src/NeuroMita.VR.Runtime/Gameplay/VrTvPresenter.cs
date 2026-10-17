using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    public sealed partial class VrTvPresenter : MonoBehaviour
    {
        private readonly Dictionary<TVUIController, VrToolkitWorldPanel> screens = new();
        private readonly List<TVUIController> stale = new();
        private TVUIController[] candidates = System.Array.Empty<TVUIController>();
        private readonly Dictionary<Renderer, bool> hiddenItems = new();
        private float nextDiscovery;
        internal static bool IsViewing { get; private set; }

        private void LateUpdate()
        {
            var rig = VrRuntime.Rig;
            if (!VrRuntime.IsActive || rig?.Eye == null) { Release(); return; }
            bool discover = Time.unscaledTime >= nextDiscovery;
            if (discover)
            {
                nextDiscovery = Time.unscaledTime + 0.5f;
                candidates = FindObjectsByType<TVUIController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
            IsViewing = false;
            foreach (var controller in candidates)
            {
                if (controller == null || !controller.isActiveAndEnabled || controller.view == null) continue;
                var owner = controller.InteractionController ?? controller.GetComponentInParent<TVInteractionController>();
                if (owner == null || owner.ScreenTarget == null) continue;
                var document = controller.view.GetComponent<UIDocument>();
                if (document == null || document.panelSettings == null) continue;
                if (!screens.TryGetValue(controller, out var panel))
                {
                    if (!discover) continue;
                    try
                    {
                        panel = new VrToolkitWorldPanel(document, transform, rig.Eye, VrRuntime.Presenter, "TV screen", 1600, 900);
                        panel.EnableDepthRendering();
                        screens.Add(controller, panel);
                        VrDiagnostics.Mark("TV panel bound", "controller=" + controller.name + ", owner=" + owner.name + ", document=" + document.name + ", size=" + document.worldSpaceSize);
                    }
                    catch (System.Exception error)
                    {
                        panel?.Dispose();
                        Debug.LogWarning("[NeuroMita VR] TV screen projection unavailable: " + error.Message);
                        continue;
                    }
                }
                bool focused = owner.IsViewActive;
                IsViewing |= focused;
                panel.Refresh(rig.Eye);
                panel.MountOnDocument(rig.Eye);
                var raycaster = panel.Canvas != null ? panel.Canvas.GetComponent<GraphicRaycaster>() : null;
                if (raycaster != null) raycaster.enabled = focused;
                if (focused && VrRuntime.Presenter?.IsPointingAt(panel.Canvas) == true)
                {
                    var view = controller.view;
                    view.lastPointerActivity = Time.unscaledTime;
                    view.pointerOver = true;
                    if (view.controls != null) { view.controls.style.opacity = 1f; view.controls.SetEnabled(true); }
                    if (view.full && view.browse != null)
                    {
                        view.browse.style.visibility = Visibility.Visible;
                        view.browse.SetEnabled(true);
                    }
                }
            }
            stale.Clear();
            foreach (var entry in screens)
                if (entry.Key == null || !entry.Key.isActiveAndEnabled || entry.Value.Canvas == null) stale.Add(entry.Key);
            foreach (var key in stale) { screens[key].Dispose(); screens.Remove(key); }
            RefreshHeldItems(rig);
        }

        private void RefreshHeldItems(VrPlayerRig rig)
        {
            if (!IsViewing || rig.Player?.MountRig == null) { RestoreItems(); return; }
            var mounts = rig.Player.MountRig;
            if (rig.Player.PersonRoot != null)
                foreach (var arms in rig.Player.PersonRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (arms.sharedMesh != null && arms.sharedMesh.name == "Arms")
                    {
                        if (!hiddenItems.ContainsKey(arms)) hiddenItems.Add(arms, arms.forceRenderingOff);
                        arms.forceRenderingOff = true;
                    }
            foreach (NeuroMita.Player.PlayerItemMountKind kind in System.Enum.GetValues(typeof(NeuroMita.Player.PlayerItemMountKind)))
            {
                var mount = mounts.ResolveMountPoint(kind)?.ItemMount;
                if (mount == null) continue;
                foreach (var renderer in mount.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hiddenItems.ContainsKey(renderer)) hiddenItems.Add(renderer, renderer.forceRenderingOff);
                    renderer.forceRenderingOff = true;
                }
            }
        }

        private void RestoreItems()
        {
            foreach (var item in hiddenItems) if (item.Key != null) item.Key.forceRenderingOff = item.Value;
            hiddenItems.Clear();
        }

        private void Release()
        {
            IsViewing = false;
            RestoreItems();
            foreach (var panel in screens.Values) panel.Dispose();
            screens.Clear();
        }
        private void OnDisable() => Release();
        private void OnDestroy() => Release();
    }
}
