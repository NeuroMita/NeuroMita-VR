using System;
using NeuroMita.Menu;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    internal static class VrModPanel
    {
        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var root = VrObject.Create(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        public static Text Label(Transform parent, string value, Vector2 size, Vector2 position, int fontSize = 24)
        {
            var rect = Rect("Label", parent, size, position);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = LocalizationManager.CurrentFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        public static Button Button(Transform parent, string title, Vector2 size, Vector2 position, Action action)
        {
            var rect = Rect(title, parent, size, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.28f, 0.1f, 0.32f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(VrUiEvents.Callback(action));
            Label(rect, title, size - new Vector2(10f, 4f), Vector2.zero);
            return button;
        }
    }
}
