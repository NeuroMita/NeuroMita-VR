using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace NeuroMita.VR
{
    public sealed class VrSettingsPanel : MonoBehaviour
    {
        private RectTransform root, page;
        private Canvas owner;
        private int pageIndex;
        private float nextDiscovery;
        private readonly PropertyInfo[] settings = typeof(VrSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && (p.PropertyType == typeof(bool) || p.PropertyType == typeof(int))).ToArray();
        public VrSettingsPanel(IntPtr pointer) : base(pointer) { }

        private void LateUpdate()
        {
            if (Time.unscaledTime < nextDiscovery) return;
            nextDiscovery = Time.unscaledTime + 0.25f;
            var pause = FindFirstObjectByType<InterfaceFastMenu>();
            var canvas = pause != null ? pause.GetComponentInParent<Canvas>() : null;
            if (canvas == null || !canvas.isActiveAndEnabled)
            {
                if (root != null) root.gameObject.SetActive(false);
                if (page != null) page.gameObject.SetActive(false);
                return;
            }
            if (root != null && owner == canvas) { root.gameObject.SetActive(true); return; }
            if (root != null) Destroy(root.gameObject);
            owner = canvas;
            root = VrModPanel.Rect("VR settings", canvas.transform, new Vector2(780f, 600f), Vector2.zero);
            VrModPanel.Button(root, "VR Settings", new Vector2(260f, 60f), new Vector2(0f, -365f), () => Open());
            page = VrModPanel.Rect("VR settings page", root, new Vector2(780f, 600f), new Vector2(0f, 0f));
            page.localPosition = new Vector3(0f, 0f, -2f);
            page.gameObject.AddComponent<Image>().color = new Color(0.055f, 0.025f, 0.075f, 1f);
            Rebuild();
            page.gameObject.SetActive(false);
        }

        private void Open() { Rebuild(); page.gameObject.SetActive(true); page.SetAsLastSibling(); }
        internal bool ClosePage()
        {
            if (page == null || !page.gameObject.activeInHierarchy) return false;
            page.gameObject.SetActive(false);
            return true;
        }

        private void Rebuild()
        {
            for (int i = page.childCount - 1; i >= 0; i--)
            {
                var old = page.GetChild(i).gameObject;
                old.SetActive(false);
                Destroy(old);
            }
            int pages = (settings.Length + 5) / 6;
            VrModPanel.Label(page, "VR Settings  " + (pageIndex + 1) + " / " + pages, new Vector2(600f, 50f), new Vector2(0f, 260f), 30);
            for (int row = 0; row < 6; row++)
            {
                int index = pageIndex * 6 + row;
                if (index >= settings.Length) break;
                var property = settings[index];
                float y = 190f - row * 68f;
                string title = System.Text.RegularExpressions.Regex.Replace(property.Name.Substring(2), "([a-z])([A-Z])", "$1 $2");
                VrModPanel.Label(page, title, new Vector2(440f, 56f), new Vector2(-145f, y));
                if (property.PropertyType == typeof(bool))
                    VrModPanel.Button(page, (bool)property.GetValue(VrSettings.Current) ? "On" : "Off", new Vector2(230f, 54f), new Vector2(235f, y), () => Change(property, 1));
                else
                {
                    VrModPanel.Label(page, Display(property), new Vector2(150f, 56f), new Vector2(235f, y));
                    VrModPanel.Button(page, "-", new Vector2(54f, 54f), new Vector2(130f, y), () => Change(property, -1));
                    VrModPanel.Button(page, "+", new Vector2(54f, 54f), new Vector2(340f, y), () => Change(property, 1));
                }
            }
            VrModPanel.Button(page, "Previous", new Vector2(200f, 54f), new Vector2(-250f, -240f), () => { pageIndex = (pageIndex + pages - 1) % pages; Rebuild(); });
            VrModPanel.Button(page, "Close", new Vector2(200f, 54f), new Vector2(0f, -240f), () => ClosePage());
            VrModPanel.Button(page, "Next", new Vector2(200f, 54f), new Vector2(250f, -240f), () => { pageIndex = (pageIndex + 1) % pages; Rebuild(); });
        }

        private static string[] Choices(string name) => name switch
        {
            "VrTurnMode" => new[] { "Snap", "Smooth" },
            "VrMovementDirection" => new[] { "Head", "Left hand", "Body" },
            "VrSprintMode" => new[] { "Hold", "Toggle", "Off" },
            "VrUiFollow" => new[] { "Fixed", "Comfort", "Follow" },
            "VrAvatarMode" => new[] { "VR hands", "Full body", "Hidden" },
            "VrGraphicsPreset" => new[] { "Quality", "Performance", "Balanced" },
            "VrHaptics" => new[] { "Off", "Low", "Normal" },
            _ => null
        };
        private static string Display(PropertyInfo property)
        {
            int value = (int)property.GetValue(VrSettings.Current);
            var choices = Choices(property.Name);
            return choices != null && value >= 0 && value < choices.Length ? choices[value] : value.ToString();
        }
        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private void Change(PropertyInfo property, int direction)
        {
            if (property.PropertyType == typeof(bool)) property.SetValue(VrSettings.Current, !(bool)property.GetValue(VrSettings.Current));
            else
            {
                var choices = Choices(property.Name);
                int min = 0, max = 100, step = 5;
                if (choices != null) { max = choices.Length - 1; step = 1; }
                else if (property.Name.Contains("Wrist")) { min = -180; max = 180; }
                else if (property.Name is "VrHeightOffset" or "VrUiHeight") { min = -100; max = 100; }
                else if (property.Name == "VrUiDistance") { min = 60; max = 300; step = 10; }
                else if (property.Name == "VrUiScale") { min = 50; max = 180; }
                else if (property.Name == "VrMovementSpeed") { min = 25; max = 200; }
                else if (property.Name == "VrSmoothTurnRateDegreesPerSecond") { min = 30; max = 180; step = 15; }
                else if (property.Name == "VrSnapAngle") { min = 15; max = 90; step = 15; }
                property.SetValue(VrSettings.Current, Math.Clamp((int)property.GetValue(VrSettings.Current) + direction * step, min, max));
            }
            VrSettings.PersistChange?.Invoke(property.Name);
            Rebuild();
        }
        private void OnDestroy() { if (root != null) Destroy(root.gameObject); }
    }
}
