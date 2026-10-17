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
        private Vector3 placedEye;
        private float placedYaw;
        private readonly System.Collections.Generic.Dictionary<GameObject, bool> hiddenHints = new();
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
            foreach (var legacy in FindObjectsByType<NeuroMita.Legacy.Games.LegacyFightTelevisionGame>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var root = legacy.interfaceHintRoot;
                if (root == null) continue;
                if (!hiddenHints.ContainsKey(root.gameObject)) hiddenHints.Add(root.gameObject, root.gameObject.activeSelf);
                root.gameObject.SetActive(false);
            }
            EnsureCanvas();
            bool opening = !canvas.gameObject.activeSelf;
            canvas.gameObject.SetActive(Time.timeScale > 0f && AppServices.GetOrNull<UIManager>()?.FocusedType == null);
            canvas.worldCamera = eye;
            if (opening || Vector3.Distance(placedEye, eye.transform.position) > 0.3f || Mathf.Abs(Mathf.DeltaAngle(placedYaw, VrRuntime.Rig.BodyYaw)) > 20f)
            {
                var device = current.GetComponentInParent<TVDeviceController>();
                Vector3 screen = device?.ScreenTarget != null ? device.ScreenTarget.position : current.transform.position;
                Vector3 direction = Vector3.ProjectOnPlane(screen - eye.transform.position, Vector3.up);
                var facing = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
                VrPanelPlacement.Place(canvas.GetComponent<RectTransform>(), eye, eye.transform.position + facing * new Vector3(0f, -0.35f, 0.85f), 0.9f);
                placedEye = eye.transform.position;
                placedYaw = VrRuntime.Rig.BodyYaw;
            }
            title.text = "TV: " + (current.Controller.IsMenuActive ? current.SelectedGameDisplayName : current.ActiveGameDisplayName) +
                VrSettingsText.Pick("\nLeft Y: exit | Stick: select / move | A: play / jump | Grip: hit", "\nY: выйти | Стик: выбор / движение | A: играть / прыжок | Хват: удар");
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
        private void Hide()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
            foreach (var hint in hiddenHints) if (hint.Key != null) hint.Key.SetActive(hint.Value);
            hiddenHints.Clear();
        }
        private void OnDestroy() { Hide(); if (current?.Controller?.IsMenuActive == true) current.Controller.KeysMenuActive(true); photoExclusion?.Dispose(); if (canvas != null) Destroy(canvas.gameObject); }
    }
}
