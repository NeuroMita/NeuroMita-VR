using NeuroMita.Menu;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10030)]
#endif
    public sealed partial class VrVirtualKeyboard : MonoBehaviour
    {
        private Selectable target;
        private Canvas canvas;
        private System.IDisposable photoExclusion;
        private RectTransform panel;
        private Transform keys;
        private int anchor, focus;
        private bool upper, russian;
        private int languageRevision = -1;

        internal Selectable CurrentField => target;
        internal bool Owns(GameObject value) => value != null && panel != null && value.transform.IsChildOf(panel);

        private void LateUpdate()
        {
            if (!VrRuntime.IsActive || VrRuntime.Rig?.Eye == null || VrRuntime.Settings?.VrKeyboard == false || VrQteInput.HasActiveQte)
            {
                Hide();
                target = null;
                ClearToolkit();
                return;
            }
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && (panel == null || !selected.transform.IsChildOf(panel)))
            {
                var field = selected.GetComponentInParent<TMP_InputField>() as Selectable ?? selected.GetComponentInParent<InputField>();
                if (field != null && field != target) Focus(field);
                else if (field == null) target = null;
            }
            var composer = MitaAI.InputControl.Instance;
            bool composerHidden = composer != null && composer.ComposerField == target &&
                (!MitaAI.InputControl.isInputActive ||
                 (AppServices.GetOrNull<UIManager>() is { } manager && manager.FocusedType?.FullName != typeof(MitaAI.InputControl).FullName));
            if (toolkitTarget != null && !ToolkitUsable) ClearToolkit();
            if ((!Usable(target) || composerHidden) && !ToolkitUsable) { Hide(); return; }
            EnsureCanvas();
            canvas.gameObject.SetActive(true);
            canvas.worldCamera = VrRuntime.Rig.Eye;
            if (languageRevision != LocalizationManager.CurrentRevision)
            {
                languageRevision = LocalizationManager.CurrentRevision;
                russian = LocalizationManager.CurrentLanguage == "Russian";
                RebuildKeys();
            }
            var eye = VrRuntime.Rig.Eye.transform;
            var rotation = Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
            float distance = (VrRuntime.Settings?.VrUiDistance ?? 150) * 0.0085f;
            var position = eye.position + rotation * new Vector3(0f, -0.65f + (VrRuntime.Settings?.VrUiHeight ?? 0) * 0.01f, distance);
            int follow = VrRuntime.Settings?.VrUiFollow ?? 1;
            if (follow == 2 || Vector3.Distance(panel.position, position) > 0.6f ||
                Mathf.Abs(Mathf.DeltaAngle(panel.eulerAngles.y, rotation.eulerAngles.y)) > 45f)
                panel.SetPositionAndRotation(position, rotation);
            panel.localScale = Vector3.one * ((VrRuntime.Settings?.VrUiScale ?? 100) * 0.00001f);
            PreserveSelection();
        }

        public void Focus(Selectable field)
        {
            if (!Usable(field)) return;
            ClearToolkit();
            target = field;
            CaptureSelection();
            EnsureCanvas();
            if (VrRuntime.Rig?.Eye != null)
            {
                var eye = VrRuntime.Rig.Eye.transform;
                var rotation = Quaternion.Euler(0f, eye.eulerAngles.y, 0f);
                panel.SetPositionAndRotation(eye.position + rotation * new Vector3(0f, -0.65f,
                    (VrRuntime.Settings?.VrUiDistance ?? 150) * 0.0085f), rotation);
            }
        }

        public void PreserveSelection()
        {
            var selected = EventSystem.current?.currentSelectedGameObject;
            if (target != null && selected != null && selected.transform.IsChildOf(target.transform)) CaptureSelection();
        }

        private void CaptureSelection()
        {
            if (ToolkitUsable) { anchor = toolkitTarget.cursorIndex; focus = toolkitTarget.selectIndex; return; }
            if (target?.GetComponent<TMP_InputField>() is {} tmp) { anchor = tmp.selectionStringAnchorPosition; focus = tmp.selectionStringFocusPosition; }
            else if (target?.GetComponent<InputField>() is {} input) { anchor = input.selectionAnchorPosition; focus = input.selectionFocusPosition; }
        }

        private static bool Usable(Selectable field)
        {
            if (field == null || !field.gameObject.activeInHierarchy || !field.IsInteractable()) return false;
            if (field?.GetComponent<TMP_InputField>() is {} tmp && tmp.readOnly || field?.GetComponent<InputField>() is {} input && input.readOnly) return false;
            if (field.GetComponent<TMP_InputField>() == null && field.GetComponent<InputField>() == null) return false;
            foreach (var group in field.GetComponentsInParent<CanvasGroup>())
                if (group.isActiveAndEnabled && group.alpha <= 0.01f) return false;
            return true;
        }

        public void Insert(string text)
        {
            if (ToolkitUsable) { InsertToolkit(text); return; }
            if (!Usable(target)) return;
            RestoreSelection();
            foreach (char character in text ?? string.Empty) Send(new Event { type = EventType.KeyDown, character = character });
            CaptureSelection();
        }

        public void Key(KeyCode code)
        {
            if (ToolkitUsable) { KeyToolkit(code); return; }
            if (!Usable(target)) return;
            RestoreSelection();
            Send(new Event { type = EventType.KeyDown, keyCode = code });
            CaptureSelection();
        }

        private void RestoreSelection()
        {
            if (target?.GetComponent<TMP_InputField>() is {} tmp)
            {
                if (!tmp.isFocused) tmp.ActivateInputField();
                tmp.selectionStringAnchorPosition = Mathf.Clamp(anchor, 0, tmp.text.Length);
                tmp.selectionStringFocusPosition = Mathf.Clamp(focus, 0, tmp.text.Length);
            }
            else if (target?.GetComponent<InputField>() is {} input)
            {
                if (!input.isFocused) input.ActivateInputField();
                input.selectionAnchorPosition = Mathf.Clamp(anchor, 0, input.text.Length);
                input.selectionFocusPosition = Mathf.Clamp(focus, 0, input.text.Length);
            }
        }

        private void Send(Event inputEvent)
        {
            if (target?.GetComponent<TMP_InputField>() is {} tmp) { tmp.ProcessEvent(inputEvent); tmp.ForceLabelUpdate(); }
            else if (target?.GetComponent<InputField>() is {} input) { input.ProcessEvent(inputEvent); input.ForceLabelUpdate(); }
        }

        public void Submit()
        {
            if (ToolkitUsable) { SubmitToolkit(); return; }
            if (!Usable(target)) return;
            var composer = MitaAI.InputControl.Instance;
            if (composer != null && composer.ComposerField == target) composer.TrySubmitDraft();
            else
            {
                if (target?.GetComponent<TMP_InputField>() is {} tmp) tmp.onSubmit.Invoke(tmp.text);
                else if (target?.GetComponent<InputField>() is {} input) input.onSubmit.Invoke(input.text);
                if (target?.GetComponent<TMP_InputField>() is {} tmpField) tmpField.DeactivateInputField();
                else if (target?.GetComponent<InputField>() is {} inputField) inputField.DeactivateInputField();
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                target = null;
                Hide();
            }
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            var root = VrObject.Create("VR virtual keyboard", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(Image));
            root.transform.SetParent(transform, false);
            panel = root.GetComponent<RectTransform>();
            panel.sizeDelta = new Vector2(1240f, 420f);
            panel.localScale = Vector3.one * 0.001f;
            canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            photoExclusion = VrHostLease.Wrap(NeuroMita.Player.PlayerSelfieController.ExcludeCanvasFromPhotos(canvas));
            canvas.sortingOrder = 500;
            root.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.96f);
            keys = VrObject.Create("Keys", typeof(RectTransform)).transform;
            keys.SetParent(panel, false);
            russian = LocalizationManager.CurrentLanguage == "Russian";
            RebuildKeys();
            VrRuntime.Presenter?.RegisterWorldCanvas(canvas);
        }

        private void RebuildKeys()
        {
            if (keys == null) return;
            for (int i = keys.childCount - 1; i >= 0; i--)
            {
                var old = keys.GetChild(i).gameObject;
                old.SetActive(false);
                DisposeObject(old);
            }
            string[] rows = russian
                ? new[] { "1234567890-", "йцукенгшщзхъ", "фывапролджэ", "ячсмитьбю,." }
                : new[] { "1234567890-", "qwertyuiop[]", "asdfghjkl;'", "zxcvbnm,.?!" };
            for (int row = 0; row < rows.Length; row++)
                for (int column = 0; column < rows[row].Length; column++)
                {
                    string value = rows[row][column].ToString();
                    if (upper) value = value.ToUpperInvariant();
                    string key = value;
                    MakeKey(key, new Vector2((column - (rows[row].Length - 1) * 0.5f) * 100f, 160f - row * 75f), 90f, () => Insert(key));
                }
            MakeKey(Localized("shift", "Shift"), new Vector2(-520f, -150f), 130f, () => { upper = !upper; RebuildKeys(); });
            MakeKey(russian ? "EN" : "RU", new Vector2(-375f, -150f), 120f, () => { russian = !russian; RebuildKeys(); });
            MakeKey("←", new Vector2(-265f, -150f), 80f, () => Key(KeyCode.LeftArrow));
            MakeKey(Localized("space", "Space"), new Vector2(-50f, -150f), 330f, () => Insert(" "));
            MakeKey("→", new Vector2(165f, -150f), 80f, () => Key(KeyCode.RightArrow));
            MakeKey(Localized("backspace", "Delete"), new Vector2(310f, -150f), 185f, () => Key(KeyCode.Backspace));
            MakeKey(Localized("submit", "Enter"), new Vector2(505f, -150f), 175f, Submit);
        }

        private static string Localized(string key, string fallback)
        {
            string value = LocalizationManager.GetTableString("vr.keyboard." + key);
            return string.IsNullOrEmpty(value) ? fallback : value;
        }

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void MakeKey(string label, Vector2 position, float width, System.Action action)
        {
            var key = VrObject.Create(label, typeof(RectTransform), typeof(Image), typeof(Button));
            key.transform.SetParent(keys, false);
            var rect = key.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(width, 62f);
            key.GetComponent<Image>().color = new Color(0.17f, 0.21f, 0.28f);
            key.GetComponent<Button>().navigation = new Navigation { mode = Navigation.Mode.None };
            key.GetComponent<Button>().onClick.AddListener(VrUiEvents.Callback(action));
            var text = VrObject.Create("Label", typeof(RectTransform), typeof(Text));
            text.transform.SetParent(key.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            var view = text.GetComponent<Text>();
            view.font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            view.fontSize = 30;
            view.color = Color.white;
            view.alignment = TextAnchor.MiddleCenter;
            view.raycastTarget = false;
            view.text = label;
        }

        private void Hide() { if (canvas != null) canvas.gameObject.SetActive(false); }

        public void Clear()
        {
            ClearToolkit();
            photoExclusion?.Dispose();
            photoExclusion = null;
            if (canvas != null) DisposeObject(canvas.gameObject);
            canvas = null;
            panel = null;
            keys = null;
            target = null;
        }

        private void OnDestroy() => Clear();
        private static void DisposeObject(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
