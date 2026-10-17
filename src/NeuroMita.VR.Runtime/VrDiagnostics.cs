using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeuroMita.VR
{
    internal static class VrDiagnostics
    {
        private static readonly object gate = new();
        private static StreamWriter writer;
        public static void Start()
        {
            string directory = Environment.GetEnvironmentVariable("NEUROMITA_VR_DIAGNOSTICS");
            if (string.IsNullOrEmpty(directory)) return;
            lock (gate)
            {
                if (writer != null) return;
                Directory.CreateDirectory(directory);
                writer = new StreamWriter(new FileStream(Path.Combine(directory, "vr-trace.log"), FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            }
            Mark("runtime", "diagnostics started, assembly=" + typeof(VrDiagnostics).Assembly.GetName().Version);
        }
        public static void Mark(string phase, string details = "")
        {
            lock (gate)
                try { writer?.WriteLine(DateTime.UtcNow.ToString("O") + " | " + phase + " | " + details); }
                catch (IOException) { }
        }
        private static float nextControls;
        public static void Controls(VrPlayerRig rig, ulong held, ulong down, Vector2 movement)
        {
            if (writer == null || (Time.unscaledTime < nextControls && down == 0)) return;
            nextControls = Time.unscaledTime + (movement.sqrMagnitude > 0.01f ? 0.25f : 2f);
            var input = AppServices.GetOrNull<InputManager>();
            Mark("controls", "held=" + held + ", down=" + down + ", movement=" + movement +
                ", tracking=" + rig?.HeadTracked + "/" + rig?.LeftHandTracked + "/" + rig?.RightHandTracked +
                ", input=" + input?.CurrentState + ", locked=" + rig?.Player?.InputLocked + ", timeScale=" + Time.timeScale);
        }
        private static float nextHost;
        public static void HostControls(VrPlayerRig rig, ulong down, Vector2 movement)
        {
            if (writer == null || (down == 0 && (movement.sqrMagnitude < 0.1f || Time.unscaledTime < nextHost))) return;
            nextHost = Time.unscaledTime + 0.25f;
            bool hasMovement = NeuroMita.Modding.ExternalInputRegistry.TryGetMovement(false, out var hostMovement);
            Mark("host input", "canReceive=" + NeuroMita.Modding.ExternalInputRegistry.CanReceive(false) +
                ", movement=" + hasMovement + "/" + hostMovement + ", pause=" +
                NeuroMita.Modding.ExternalInputRegistry.Down(NeuroMita.Modding.ExternalInputAction.Pause, false) +
                ", playerMove=" + rig?.Player?.MoveInput + ", position=" + rig?.Player?.transform.position);
        }

        public static void UiState()
        {
            if (writer == null) return;
            var ui = AppServices.GetOrNull<UIManager>();
            var help = UnityEngine.Object.FindFirstObjectByType<GameHelpHintController>();
            var radio = UnityEngine.Object.FindFirstObjectByType<Radio>();
            Mark("UI state", "focus=" + ui?.FocusedType?.FullName + ", canBack=" + ui?.CanGoBack +
                ", pauseBlocked=" + ui?.IsPauseBlocked + ", helpVisible=" + help?.IsVisible() + ", radioOpen=" + radio?.IsUIOpen);
        }
        public static void MenuLighting(Camera camera, MenuMita character)
        {
            if (writer == null) return;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<MenuMita>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Mark("menu character", candidate.name + ", scene=" + candidate.gameObject.scene.name + ", active=" + candidate.gameObject.activeInHierarchy + ", animator=" + (candidate.mitaAnim != null ? candidate.mitaAnim.name + ", active=" + candidate.mitaAnim.gameObject.activeInHierarchy : "null"));
            Mark("menu camera", "position=" + camera.transform.position + ", rotation=" + camera.transform.eulerAngles + ", mask=" + camera.cullingMask);
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (light.gameObject.scene == camera.gameObject.scene)
                    Mark("menu light", light.name + ", enabled=" + light.enabled + ", active=" + light.gameObject.activeInHierarchy + ", type=" + light.type + ", intensity=" + light.intensity + ", range=" + light.range + ", mask=" + light.cullingMask + ", position=" + light.transform.position + ", forward=" + light.transform.forward);
            if (character == null || character.mitaAnim == null) return;
            foreach (var renderer in character.mitaAnim.GetComponentsInChildren<Renderer>(true))
                Mark("menu renderer", renderer.name + ", active=" + renderer.gameObject.activeInHierarchy + ", enabled=" + renderer.enabled + ", layer=" + renderer.gameObject.layer + ", bounds=" + renderer.bounds + ", materials=" + renderer.sharedMaterials.Length);
        }
    }
}