using System.Collections.Generic;
using NeuroMita.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrTvPresenter : MonoBehaviour
    {
        private Canvas canvas;
        private VrToolkitWorldPanel browser;
        private UnityEngine.UIElements.UIDocument browserDocument;
        private System.IDisposable photoExclusion;
        private RectTransform panel;
        private TVUIController controller;
        private TVInteractionController interaction;
        private int revision = -1;
        private float nextDiscovery;
        private readonly List<Text> labels = new();
        private readonly List<Button> buttons = new();
        private TVUIController[] candidates = System.Array.Empty<TVUIController>();

        private void LateUpdate()
        {
            if (Time.unscaledTime >= nextDiscovery)
            {
                nextDiscovery = Time.unscaledTime + 0.5f;
                candidates = FindObjectsByType<TVUIController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            }
            TVUIController current = null;
            foreach (var candidate in candidates)
            {
                if (candidate == null || !candidate.isActiveAndEnabled) continue;
                var owner = candidate.GetComponentInParent<TVInteractionController>();
                if (owner != null && owner.IsViewActive) { current = candidate; break; }
            }
            Refresh(current, VrRuntime.Rig, VrRuntime.Presenter);
        }

        public void Refresh(TVUIController current, VrPlayerRig rig, VrUiPresenter pointer)
        {
            var owner = current != null ? current.GetComponentInParent<TVInteractionController>() : null;
            var ui = AppServices.GetOrNull<UIManager>();
            bool visible = current != null && current.isActiveAndEnabled && owner != null && owner.IsViewActive &&
                rig?.Eye != null && Time.timeScale > 0f &&
                (ui == null || ui.FocusedType == null || ui.FocusedType?.FullName == typeof(TVInputContext).FullName || ui.FocusedType?.FullName == typeof(TypingBlocker).FullName);
            if (!visible)
            {
                if (canvas != null) canvas.gameObject.SetActive(false);
                browser?.Dispose();
                browser = null;
                browserDocument = null;
                controller = null;
                interaction = null;
                return;
            }
            bool opening = controller != current || canvas == null || !canvas.gameObject.activeSelf;
            controller = current;
            interaction = owner;
            EnsureCanvas(pointer);
            canvas.gameObject.SetActive(true);
            canvas.worldCamera = rig.Eye;
            if (opening)
            {
                var eye = rig.Eye.transform;
                var facing = Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
                panel.SetPositionAndRotation(eye.position + facing * new Vector3(0.5f, -0.15f, 0.8f), facing);
            }
            var document = current.view != null ? current.view.GetComponent<UnityEngine.UIElements.UIDocument>() : null;
            if (document != browserDocument)
            {
                browser?.Dispose();
                browser = null;
                browserDocument = document;
            }
            if (browser == null && document?.panelSettings != null)
                browser = new VrToolkitWorldPanel(document, transform, rig.Eye, pointer);
            browser?.Refresh(rig.Eye);
            if (browser?.Canvas != null && pointer?.IsPointingAt(browser.Canvas) == true && current.view != null)
            {
                var view = current.view;
                view.lastPointerActivity = Time.unscaledTime;
                view.pointerOver = true;
                if (view.controls != null) { view.controls.style.opacity = 1f; view.controls.SetEnabled(true); }
                if (view.full && view.browse != null)
                {
                    view.browse.style.visibility = UnityEngine.UIElements.Visibility.Visible;
                    view.browse.SetEnabled(true);
                }
            }
            if (revision != LocalizationManager.CurrentRevision) RefreshLabels();
            for (int i = 0; i < 7; i++) buttons[i].interactable = current.CanReceivePlaybackActions;
        }

        private void EnsureCanvas(VrUiPresenter pointer)
        {
            if (canvas != null) return;
            var root = VrObject.Create("VR TV controls", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(600f, 360f);
            panel.localScale = Vector3.one * 0.001f;
            root.GetComponent<Image>().color = new Color(0.07f, 0.035f, 0.1f, 0.96f);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            canvas.sortingOrder = 600;
            for (int i = 0; i < 8; i++)
            {
                int index = i;
                var button = VrObject.Create(i < 7 ? ((TVUIController.PlaybackAction)i).ToString() : "Close", typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(panel, false);
                var rect = button.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(276f, 72f);
                rect.anchoredPosition = new Vector2((i % 2 - 0.5f) * 288f, 126f - (i / 2) * 84f);
                button.GetComponent<Image>().color = new Color(0.3f, 0.12f, 0.32f);
                var control = button.GetComponent<Button>();
                control.navigation = new Navigation { mode = Navigation.Mode.None };
                control.onClick.AddListener(VrUiEvents.Callback(() =>
                {
                    if (index < 7) controller?.TryPerformPlaybackAction((TVUIController.PlaybackAction)index);
                    else interaction?.CloseYouTube();
                }));
                buttons.Add(control);
                var text = VrObject.Create("Label", typeof(RectTransform), typeof(Text));
                text.transform.SetParent(button.transform, false);
                var textRect = text.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = new Vector2(-12f, -8f);
                var label = text.GetComponent<Text>();
                label.fontSize = 24;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
                labels.Add(label);
            }
            RefreshLabels();
            pointer?.RegisterWorldCanvas(canvas);
        }

        private void RefreshLabels()
        {
            revision = LocalizationManager.CurrentRevision;
            for (int i = 0; i < labels.Count; i++)
            {
                string action = i < 7 ? ((TVUIController.PlaybackAction)i).ToString() : "Close";
                labels[i].font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labels[i].text = LocalizationManager.GetTableString("vr.tv." + action) ?? action;
            }
        }

        private void OnDestroy()
        {
            browser?.Dispose();
            browser = null;
            browserDocument = null;
            photoExclusion?.Dispose();
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
