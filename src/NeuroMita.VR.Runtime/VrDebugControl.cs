using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using NeuroMita.Menu;

namespace NeuroMita.VR
{
    public sealed class VrDebugControl : MonoBehaviour
    {
        private string directory;
        private float nextPoll;
        internal static bool PauseRequested, HelpRequested;
        public VrDebugControl(IntPtr pointer) : base(pointer) { }
        private static unsafe void Capture(string filename)
        {
            fixed (char* characters = filename)
            {
                var path = new UnityEngine.Bindings.ManagedSpanWrapper(characters, filename.Length);
                ScreenCapture.CaptureScreenshot_Injected(ref path, 1, ScreenCapture.StereoScreenCaptureMode.LeftEye);
            }
        }
        private void Awake() => directory = Environment.GetEnvironmentVariable("NEUROMITA_VR_DIAGNOSTICS");
        private void Update()
        {
            if (string.IsNullOrEmpty(directory) || Time.unscaledTime < nextPoll) return;
            nextPoll = Time.unscaledTime + 0.25f;
            string path = Path.Combine(directory, "command.txt");
            if (!File.Exists(path)) return;
            string command = File.ReadAllText(path).Trim();
            File.Delete(path);
            VrDiagnostics.Mark("debug command", command);
            try
            {
                if (command == "capture") Capture(Path.Combine(directory, "game-" + DateTime.UtcNow.ToString("HHmmss") + ".png"));
                else if (command == "pause-test") PauseRequested = true;
                else if (command == "help-test") HelpRequested = true;
                else if (command == "radio-test")
                {
                    var radio = UnityEngine.Object.FindFirstObjectByType<Radio>();
                    if (radio == null) throw new InvalidOperationException("No radio in the current scene.");
                    radio.SetUIMode(true);
                }
                else if (command == "views")
                {
                    VrDiagnostics.Mark("XR render state", "display=" + VrRuntime.XrDisplay?.running + ", renderPasses=" + VrRuntime.XrDisplay?.GetRenderPassCount());
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        VrDiagnostics.Mark("view camera", camera.name + ", scene=" + camera.gameObject.scene.name + ", active=" + camera.isActiveAndEnabled + ", depth=" + camera.depth + ", stereoEnabled=" + camera.stereoEnabled + ", stereo=" + camera.stereoTargetEye + ", position=" + camera.transform.position + ", rotation=" + camera.transform.eulerAngles);
                    foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        VrDiagnostics.Mark("view canvas", canvas.name + ", scene=" + canvas.gameObject.scene.name + ", active=" + canvas.isActiveAndEnabled + ", mode=" + canvas.renderMode + ", order=" + canvas.sortingOrder + ", position=" + canvas.transform.position + ", scale=" + canvas.transform.lossyScale + ", root=" + canvas.isRootCanvas);
                        if (!canvas.isActiveAndEnabled) continue;
                        foreach (var graphic in canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(false))
                            VrDiagnostics.Mark("view graphic", graphic.name + ", type=" + graphic.GetIl2CppType().FullName + ", enabled=" + graphic.enabled + ", alpha=" + graphic.color.a + ", raycast=" + graphic.raycastTarget + ", position=" + graphic.transform.position);
                    }
                }
                else if (command == "state")
                {
                    VrDiagnostics.UiState();
                    foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        if (camera.gameObject.scene.name == "SceneMenu") VrDiagnostics.MenuLighting(camera, null);
                }
                else if (command == "house" || command == "crazy-house")
                {
                    var menu = MenuSceneResolver.ResolvePrimaryRoot(SceneManager.GetActiveScene());
                    if (menu == null) throw new InvalidOperationException("House repro must start through the menu.");
                    menu.LoadScene(command == "crazy-house" ? GameSceneId.CrazyHouse : GameSceneId.KindHouse);
                }
                else if (command == "menu") SceneService.LoadSingle(GameSceneId.SceneMenu);
                else if (command == "stop") VrRuntime.StopVr();
                else VrDiagnostics.Mark("debug command rejected", command);
            }
            catch (Exception exception) { VrDiagnostics.Mark("debug command failed", exception.ToString()); }
        }
    }
}