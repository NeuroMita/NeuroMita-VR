using NeuroMita.Menu;
using NeuroMita.Player.Controllers;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10025)]
#endif
    public sealed partial class VrHotbarPresenter : MonoBehaviour
    {
        private Canvas canvas;
        private System.IDisposable photoExclusion;
        private RectTransform panel;
        private PlayerInventoryController inventory;
        private readonly Button[] buttons = new Button[9];
        private readonly Image[] icons = new Image[9];
        private readonly Text[] labels = new Text[9];
        private int revision = -1;
        private bool wristVisible;

        private void LateUpdate() => Refresh(VrRuntime.Rig, VrRuntime.Presenter, VrRuntime.Settings?.VrHudOnWrist != false);

        public void Refresh(VrPlayerRig rig, VrUiPresenter pointer, bool wrist)
        {
            var current = rig?.Player?.Inventory;
            var slots = current?.Inventory?.GetSlots();
            int count = slots != null ? Mathf.Min(Mathf.Min(9, Mathf.Max(0, current.hotbarSlots)), slots.Count) : 0;
            bool visible = rig?.Eye != null && current != null && current.CanReceiveHotbarActions && count > 0;
            if (!visible)
            {
                if (canvas != null) canvas.gameObject.SetActive(false);
                inventory = null;
                wristVisible = false;
                return;
            }
            inventory = current;
            EnsureCanvas(pointer);
            canvas.worldCamera = rig.Eye;
            bool leftPointer = VrRuntime.Settings?.VrPointerLeft == true;
            var hand = leftPointer ? rig.RightHand : rig.LeftHand;
            wrist &= (leftPointer ? rig.RightHandTracked : rig.LeftHandTracked) && hand != null;
            bool gazeRequired = wrist && VrRuntime.Settings?.VrHotbarOnGaze != false;
            wristVisible = !gazeRequired || IsWristInView(rig.Eye, hand, wristVisible);
            canvas.gameObject.SetActive(wristVisible);
            if (!wristVisible) return;
            var eye = rig.Eye.transform;
            Vector3 center = wrist ? hand.position + hand.up * 0.09f :
                eye.position + eye.rotation * new Vector3(0.35f, -0.3f, 0.9f);
            Vector3 forward = center - eye.position;
            Quaternion facing = wrist && forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward, eye.up) : eye.rotation;
            panel.SetPositionAndRotation(center + (wrist ? facing * new Vector3(0f, -0.15f, 0f) : Vector3.zero), facing);
            panel.localScale = Vector3.one * (wrist ? 0.0007f : 0.001f);
            int rows = Mathf.CeilToInt(count / 3f);
            panel.sizeDelta = new Vector2(336f, rows * 112f + 12f);
            bool fontChanged = revision != LocalizationManager.CurrentRevision;
            if (fontChanged) revision = LocalizationManager.CurrentRevision;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].gameObject.SetActive(i < count);
                if (i >= count) continue;
                var rect = buttons[i].GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2((i % 3 - 1) * 108f, ((rows - 1) * 0.5f - i / 3) * 112f);
                var slot = slots[i];
                icons[i].sprite = slot?.item?.icon;
                icons[i].enabled = icons[i].sprite != null;
                labels[i].text = (i + 1).ToString() + (slot?.item != null && slot.quantity > 1 ? "  ×" + slot.quantity : "");
                if (fontChanged) labels[i].font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                buttons[i].GetComponent<Image>().color = i == current.SelectedIndex ?
                    new Color(0.65f, 0.3f, 0.65f) : new Color(0.22f, 0.09f, 0.25f);
            }
        }

        public static bool IsWristInView(Camera eye, Transform hand, bool wasVisible)
        {
            if (eye == null || hand == null) return false;
            Vector3 direction = hand.position - eye.transform.position;
            float distance = direction.magnitude;
            if (distance < 0.15f || distance > 0.85f) return false;
            direction /= distance;
            return Vector3.Dot(eye.transform.forward, direction) >= (wasVisible ? 0.88f : 0.94f) &&
                Vector3.Dot(hand.up, -direction) >= (wasVisible ? 0.15f : 0.35f);
        }

        private void EnsureCanvas(VrUiPresenter pointer)
        {
            if (canvas != null) return;
            var root = VrObject.Create("VR quick slots", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            root.GetComponent<Image>().color = new Color(0.07f, 0.035f, 0.1f, 0.96f);
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            canvas.sortingOrder = 550;
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var slot = VrObject.Create("Slot " + (i + 1), typeof(RectTransform), typeof(Image), typeof(Button));
                slot.transform.SetParent(panel, false);
                (slot.GetComponent<RectTransform>()).sizeDelta = new Vector2(100f, 104f);
                buttons[i] = slot.GetComponent<Button>();
                buttons[i].navigation = new Navigation { mode = Navigation.Mode.None };
                buttons[i].onClick.AddListener(VrUiEvents.Callback(() => inventory?.TrySelectHotbarSlot(index)));
                var icon = VrObject.Create("Icon", typeof(RectTransform), typeof(Image));
                icon.transform.SetParent(slot.transform, false);
                (icon.GetComponent<RectTransform>()).sizeDelta = new Vector2(70f, 70f);
                (icon.GetComponent<RectTransform>()).anchoredPosition = new Vector2(0f, 10f);
                icons[i] = icon.GetComponent<Image>();
                icons[i].preserveAspect = true;
                icons[i].raycastTarget = false;
                var text = VrObject.Create("Label", typeof(RectTransform), typeof(Text));
                text.transform.SetParent(slot.transform, false);
                (text.GetComponent<RectTransform>()).sizeDelta = new Vector2(92f, 28f);
                (text.GetComponent<RectTransform>()).anchoredPosition = new Vector2(0f, -36f);
                labels[i] = text.GetComponent<Text>();
                labels[i].fontSize = 22;
                labels[i].alignment = TextAnchor.MiddleCenter;
                labels[i].color = Color.white;
                labels[i].raycastTarget = false;
            }
            pointer?.RegisterWorldCanvas(canvas);
        }

        private void OnDestroy()
        {
            photoExclusion?.Dispose();
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
