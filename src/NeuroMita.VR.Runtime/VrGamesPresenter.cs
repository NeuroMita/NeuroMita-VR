using System;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    public sealed class VrGamesPresenter : MonoBehaviour
    {
        private TVGamesModule current;
        private Canvas canvas;
        private Text title;
        private float nextDiscovery;
        private IDisposable photoExclusion;
        public VrGamesPresenter(IntPtr pointer) : base(pointer) { }

        internal bool CloseActive()
        {
            if (AppServices.GetOrNull<UIManager>()?.FocusedType != null) return false;
            Discover();
            if (current == null || !current.IsOpen) return false;
            current.ForceClose();
            Hide();
            return true;
        }
        private void Discover()
        {
            current = null;
            foreach (var module in FindObjectsByType<TVGamesModule>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (module.IsOpen) { current = module; break; }
        }
        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextDiscovery) { nextDiscovery = Time.unscaledTime + 0.25f; Discover(); }
            if (current == null || !current.IsOpen || VrRuntime.Rig?.Player == null) { Hide(); return; }
            var eye = VrRuntime.Rig.Eye;
            if (eye == null) return;
            current.Controller.KeysMenuActive(false);
            EnsureCanvas();
            bool opening = !canvas.gameObject.activeSelf;
            canvas.gameObject.SetActive(Time.timeScale > 0f && AppServices.GetOrNull<UIManager>()?.FocusedType == null);
            canvas.worldCamera = eye;
            if (opening)
            {
                var facing = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
                canvas.transform.SetPositionAndRotation(eye.transform.position + facing * new Vector3(0f, -0.42f, 1.15f), facing);
            }
            title.text = "TV: " + (current.Controller.IsMenuActive ? current.SelectedGameDisplayName : current.ActiveGameDisplayName) +
                "\nLeft Y: exit TV  |  Stick: move  |  Right A / grip: action";
        }
        private void EnsureCanvas()
        {
            if (canvas != null) return;
            var rect = VrModPanel.Rect("VR minigame controls", transform, new Vector2(900f, 220f), Vector2.zero);
            rect.localScale = Vector3.one * 0.001f;
            rect.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.025f, 0.075f, 0.97f);
            canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 650;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            title = VrModPanel.Label(rect, "TV", new Vector2(880f, 95f), new Vector2(0f, 55f));
            VrModPanel.Button(rect, "Previous", new Vector2(200f, 70f), new Vector2(-330f, -55f), () => Select(-1));
            VrModPanel.Button(rect, "Play / Continue", new Vector2(230f, 70f), new Vector2(-100f, -55f), () =>
            {
                if (current?.Controller == null) return;
                if (current.Controller.IsFinishActive) current.Controller.ContinueFinish();
                else if (current.Controller.IsMenuActive) current.Controller.PlayGame();
            });
            VrModPanel.Button(rect, "Next", new Vector2(170f, 70f), new Vector2(120f, -55f), () => Select(1));
            VrModPanel.Button(rect, "Exit TV (Y)", new Vector2(220f, 70f), new Vector2(330f, -55f), () => CloseActive());
            VrRuntime.Presenter?.RegisterWorldCanvas(canvas);
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            canvas.gameObject.SetActive(false);
        }
        private void Select(int direction) { if (current?.Controller?.IsMenuActive == true) current.Controller.ChangeGame(direction); }
        private void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }
        private void OnDestroy() { if (current?.Controller?.IsMenuActive == true) current.Controller.KeysMenuActive(true); photoExclusion?.Dispose(); if (canvas != null) Destroy(canvas.gameObject); }
    }
}
