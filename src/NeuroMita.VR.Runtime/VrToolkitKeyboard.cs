using UnityEngine;
using UnityEngine.UIElements;

namespace NeuroMita.VR
{
    public sealed partial class VrVirtualKeyboard
    {
        private TextField toolkitTarget;
        private UIDocument toolkitDocument;

        private bool ToolkitUsable => toolkitTarget != null && toolkitDocument != null && toolkitDocument.isActiveAndEnabled &&
            toolkitTarget.panel != null && toolkitTarget.panel == toolkitDocument.rootVisualElement?.panel &&
            toolkitTarget.enabledInHierarchy && !toolkitTarget.isReadOnly && toolkitTarget.visible && Time.timeScale > 0f;

        public void FocusToolkit(UIDocument document, TextField field)
        {
            if (toolkitTarget != null && (field == null || toolkitTarget.Pointer != field.Pointer)) toolkitTarget.Blur();
            toolkitDocument = document;
            toolkitTarget = field;
            target = null;
            if (!ToolkitUsable) { ClearToolkit(); Hide(); return; }
            toolkitTarget.Focus();
            anchor = toolkitTarget.cursorIndex;
            focus = toolkitTarget.selectIndex;
            EnsureCanvas();
            var eye = VrRuntime.Rig?.Eye;
            if (eye == null) return;
            var rotation = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
            panel.SetPositionAndRotation(eye.transform.position + rotation * new Vector3(0f, -0.65f,
                (VrRuntime.Settings?.VrUiDistance ?? 150) * 0.0085f), rotation);
        }

        private void ClearToolkit()
        {
            toolkitTarget?.Blur();
            toolkitTarget = null;
            toolkitDocument = null;
        }

        private void InsertToolkit(string text)
        {
            if (!ToolkitUsable) return;
            string value = toolkitTarget.value ?? string.Empty;
            int start = Mathf.Clamp(Mathf.Min(anchor, focus), 0, value.Length);
            int end = Mathf.Clamp(Mathf.Max(anchor, focus), start, value.Length);
            string addition = text ?? string.Empty;
            int maximum = toolkitTarget.maxLength;
            if (maximum >= 0 && value.Length - (end - start) + addition.Length > maximum)
                addition = addition.Substring(0, Mathf.Max(0, maximum - value.Length + end - start));
            toolkitTarget.value = value.Substring(0, start) + addition + value.Substring(end);
            anchor = focus = start + addition.Length;
            toolkitTarget.SelectRange(anchor, focus);
        }

        private void KeyToolkit(KeyCode key)
        {
            if (!ToolkitUsable) return;
            string value = toolkitTarget.value ?? string.Empty;
            anchor = Mathf.Clamp(anchor, 0, value.Length);
            focus = Mathf.Clamp(focus, 0, value.Length);
            int start = Mathf.Min(anchor, focus), end = Mathf.Max(anchor, focus);
            if (key == KeyCode.Backspace || key == KeyCode.Delete)
            {
                if (start == end)
                {
                    if (key == KeyCode.Backspace && start > 0)
                    {
                        start--;
                        if (start > 0 && char.IsLowSurrogate(value[start]) && char.IsHighSurrogate(value[start - 1])) start--;
                    }
                    else if (key == KeyCode.Delete && end < value.Length)
                    {
                        end++;
                        if (end < value.Length && char.IsHighSurrogate(value[end - 1]) && char.IsLowSurrogate(value[end])) end++;
                    }
                }
                toolkitTarget.value = value.Remove(start, end - start);
                anchor = focus = start;
            }
            else if (key == KeyCode.LeftArrow)
            {
                anchor = focus = start != end ? start : Mathf.Max(0, start - 1);
                if (focus > 0 && focus < value.Length && char.IsLowSurrogate(value[focus])) anchor = focus = focus - 1;
            }
            else if (key == KeyCode.RightArrow)
            {
                anchor = focus = start != end ? end : Mathf.Min(value.Length, end + 1);
                if (focus > 0 && focus < value.Length && char.IsLowSurrogate(value[focus])) anchor = focus = focus + 1;
            }
            toolkitTarget.SelectRange(anchor, focus);
        }

        private void SubmitToolkit()
        {
            if (!ToolkitUsable) return;
            var field = toolkitTarget;
            var input = new Event { type = EventType.KeyDown, keyCode = KeyCode.Return, character = '\n' };
            var evt = KeyDownEvent.GetPooled(input);
            try { evt.target = field.Cast<IEventHandler>(); field.SendEvent(evt); }
            finally { evt.Dispose(); ClearToolkit(); Hide(); }
        }
    }
}
