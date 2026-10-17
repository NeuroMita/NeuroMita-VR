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
        private Camera probeCamera;
        private int probeMask;
        private CameraClearFlags probeClear;
        private Color probeColor;
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
                else if (command == "eye-state" || command == "render-probe" || command == "render-restore")
                {
                    var camera = VrRuntime.Rig?.Eye;
                    if (camera == null) throw new InvalidOperationException("No eye camera");
                    VrDiagnostics.Mark("camera projection", "near=" + camera.nearClipPlane + ", far=" + camera.farClipPlane + ", orthographic=" + camera.orthographic + ", clear=" + camera.clearFlags + ", color=" + camera.backgroundColor + ", mask=" + camera.cullingMask + ", frame=" + Time.frameCount);
                    foreach (Camera.StereoscopicEye eye in Enum.GetValues(typeof(Camera.StereoscopicEye)))
                    {
                        var view = camera.GetStereoViewMatrix(eye);
                        var world = view.inverse;
                        VrDiagnostics.Mark("eye matrix", eye + ", cameraPosition=" + camera.transform.position + ", eyeWorld=" + world.GetColumn(3) + ", projection=" + camera.GetStereoProjectionMatrix(eye));
                    }
                    if (command == "render-probe")
                    {
                        if (probeCamera == null) { probeCamera = camera; probeMask = camera.cullingMask; probeClear = camera.clearFlags; probeColor = camera.backgroundColor; }
                        camera.cullingMask = 0;
                        camera.clearFlags = CameraClearFlags.SolidColor;
                        camera.backgroundColor = new Color(0.8f, 0.1f, 0.7f, 1f);
                        VrDiagnostics.Mark("render probe", "solid magenta, no geometry");
                    }
                    else if (command == "render-restore" && probeCamera != null)
                    {
                        probeCamera.cullingMask = probeMask;
                        probeCamera.clearFlags = probeClear;
                        probeCamera.backgroundColor = probeColor;
                        probeCamera = null;
                    }
                }
                else if (command == "render-state" || command == "render-no-effects")
                {
                    var rig = VrRuntime.Rig;
                    var camera = rig?.Eye;
                    if (camera == null) throw new InvalidOperationException("No VR camera");
                    foreach (var component in camera.GetComponents<UnityEngine.Behaviour>())
                        VrDiagnostics.Mark("camera behaviour", component.GetIl2CppType().FullName + ", enabled=" + component.enabled);
                    var comfort = camera.GetComponent<VrComfortPresenter>();
                    if (comfort != null)
                    {
                        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                        VrDiagnostics.Mark("comfort state", "fade=" + typeof(VrComfortPresenter).GetField("fade", flags)?.GetValue(comfort) + ", vignette=" + typeof(VrComfortPresenter).GetField("vignette", flags)?.GetValue(comfort));
                    }
                    if (command == "render-no-effects")
                    {
                        if (comfort != null) comfort.enabled = false;
                        var post = camera.GetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
                        if (post != null) post.enabled = false;
                        VrDiagnostics.Mark("render isolation", "comfort and PostProcessLayer disabled for this run");
                    }
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