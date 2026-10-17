using System;
using System.Collections;
using NeuroMita.Menu;
using NeuroMita.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(-10000)]
#endif
    public sealed partial class VrRuntime : MonoBehaviour
    {
        private static VrRuntime instance;
        private IVrXrBackend xrBackend;
        private VrPlayerRig rig;
        private float nextDiscovery;
        private bool initialized;
        private int inputFrame = -1;
        private IDisposable inputRegistration;
        private string controlSession;
        private VrSettingsPanel settingsPanel;
        private VrGamesPresenter games;
        private VrGraphicsPresenter graphics;
        private VrHudPresenter hud;
        private VrContextActionsPresenter contextActions;
        private readonly System.Collections.Generic.Dictionary<MenuMita, IDisposable> menuAttention = new();
        private Transform menuAttentionTarget;

        private bool stopping;
        private static bool restartRequested;
        public static bool IsActive => instance != null && instance.initialized && Input != null;
        public static VrInput Input { get; private set; }
        public static VrUiPresenter Presenter { get; private set; }
        public static VrVirtualKeyboard Keyboard { get; private set; }
        public static VrSettings Settings => VrSettings.Resolve();
        public static VrPlayerRig Rig => instance != null ? instance.rig : null;
        public static XRDisplaySubsystem XrDisplay => instance?.xrBackend?.Display;
        public static XRInputSubsystem XrInput => instance?.xrBackend?.Input;
#if NEUROMITA_VR_IL2CPP
        public static string RuntimeBackend => "IL2CPP";
#else
        public static string RuntimeBackend => "Unity";
#endif
        public static string Status { get; private set; } = "Desktop";

#if UNITY_EDITOR
        public static bool EditorVrEnabled
        {
            get => UnityEditor.EditorPrefs.GetBool("NeuroMita.VR.Enabled." + Application.dataPath,
                UnityEditor.SessionState.GetBool("NeuroMita.VR.Enabled", false));
            set
            {
                UnityEditor.EditorPrefs.SetBool("NeuroMita.VR.Enabled." + Application.dataPath, value);
                UnityEditor.SessionState.SetBool("NeuroMita.VR.Enabled", value);
            }
        }
#endif

#if !NEUROMITA_VR_IL2CPP
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void ResetStatics()
        {
            instance = null;
            Input = null;
            Presenter = null;
            Keyboard = null;
            Status = "Desktop";
            restartRequested = false;
        }

#if !NEUROMITA_VR_IL2CPP
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
#endif
        private static void Bootstrap()
        {
            var args = Environment.GetCommandLineArgs();
            if (Array.Exists(args, a => a == "--no-vr")) return;
            bool requested = Array.Exists(args, a => a == "--vr");
#if UNITY_EDITOR
            requested |= EditorVrEnabled;
#endif
            if (requested) StartVr();
        }

        public static void StartVr()
        {
            if (instance != null)
            {
                if (instance.stopping) restartRequested = true;
                return;
            }
            VrDiagnostics.Start();
            VrDiagnostics.Mark("StartVr");
            var root = VrObject.Create("NeuroMita VR Runtime");
            DontDestroyOnLoad(root);
            instance = root.AddComponent<VrRuntime>();
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NEUROMITA_VR_DIAGNOSTICS"))) root.AddComponent<VrDebugControl>();
        }

        public static void StopVr()
        {
            restartRequested = false;
            if (instance != null)
            {
                instance.stopping = true;
                Destroy(instance.gameObject);
            }
        }

        public static bool Owns(PlayerActor player) => player != null && IsActive && Rig != null && Rig.Player == player;

        internal static bool PublishHandTargets(Transform left, Transform right)
            => instance != null && NeuroMita.Modding.ExternalControlBridge.SetTrackedHands(instance.controlSession, left, right);

#if NEUROMITA_VR_IL2CPP
        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
#endif
        private IEnumerator Start()
        {
            Status = "Initializing OpenXR";
#if NEUROMITA_VR_IL2CPP
            xrBackend = new VrNativeOpenXrBackend();
#else
            xrBackend = new VrManagedXrBackend();
#endif
            var initialization = xrBackend.Initialize();
            Exception initializationFailure = null;
            while (true)
            {
                bool more = false;
                object yielded = null;
                try { more = initialization.MoveNext(); if (more) yielded = initialization.Current; }
                catch (Exception exception) { initializationFailure = exception; }
                if (initializationFailure != null || !more) break;
                yield return yielded;
            }
            bool started = false;
            if (initializationFailure == null)
                try { started = xrBackend.Start(); } catch (Exception exception) { initializationFailure = exception; }
            if (initializationFailure != null)
            {
                Fail("OpenXR initialization failed: " + initializationFailure);
                yield break;
            }
            if (!started)
            {
                Fail("OpenXR unavailable. Start SteamVR, connect the headset and retry VR.");
                yield break;
            }
            var display = xrBackend.Display;
            float deadline = Time.realtimeSinceStartup + 15f;
            while (display != null && !display.running && Time.realtimeSinceStartup < deadline)
            {
                Exception pumpFailure = null;
                try { xrBackend.Pump(); } catch (Exception exception) { pumpFailure = exception; }
                if (pumpFailure != null) { Fail("OpenXR startup pump failed: " + pumpFailure); yield break; }
                yield return null;
            }
            if (display == null || !display.running)
            {
                Fail("OpenXR display did not start. Desktop mode remains available.");
                yield break;
            }

            try
            {
                Input = new VrInput();
                controlSession = NeuroMita.Modding.ExternalControlBridge.OpenSession("NeuroMita VR", true);
            }
            catch (Exception exception)
            {
                Fail("VR input could not start: " + exception.Message);
                yield break;
            }
            try { CompleteInitialization(); }
            catch (Exception exception) { Fail("VR presentation initialization failed: " + exception); }
        }

        private void CompleteInitialization()
        {
            VrAssets.Validate();
            Presenter = gameObject.AddComponent<VrUiPresenter>();
            hud = gameObject.AddComponent<VrHudPresenter>();
            gameObject.AddComponent<VrPhonePresenter>();
            gameObject.AddComponent<VrRadioPresenter>();
            contextActions = gameObject.AddComponent<VrContextActionsPresenter>();
            gameObject.AddComponent<VrTvPresenter>();
            settingsPanel = gameObject.AddComponent<VrSettingsPanel>();
            games = gameObject.AddComponent<VrGamesPresenter>();
            gameObject.AddComponent<VrHotbarPresenter>();
            VrQteHooks.Install();
            VrEarlyInputLoop.Install();
            initialized = true;
            VrDiscordPresence.Install();
            graphics = gameObject.AddComponent<VrGraphicsPresenter>();
            Keyboard = gameObject.AddComponent<VrVirtualKeyboard>();
            Status = "OpenXR active";
            VrHostEvents.Scenes(OnSceneLoaded, OnActiveSceneChanged, true);
            Discover();
            Debug.Log("[NeuroMita VR] OpenXR active; both stick clicks recenter.");
        }

        private void Fail(string message)
        {
            initialized = false;
            stopping = true;
            Status = message;
            VrDiagnostics.Mark("failure", message);
            Debug.LogWarning("[NeuroMita VR] " + message);
            Destroy(gameObject);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { VrDiagnostics.Mark("scene loaded", scene.name + ", mode=" + mode); nextDiscovery = 0f; }

        private void OnActiveSceneChanged(Scene previous, Scene current)
        {
            nextDiscovery = 0f;
            Presenter?.CancelInput();
            Keyboard?.Clear();
        }

        private bool TryMaintainControlSession()
        {
            if (NeuroMita.Modding.ExternalControlBridge.Heartbeat(controlSession)) return true;
            try
            {
                controlSession = NeuroMita.Modding.ExternalControlBridge.OpenSession("NeuroMita VR", true);
            }
            catch (InvalidOperationException) { return false; }
            Presenter?.CancelInput();
            return true;
        }

        public static void PrepareInputFrame() => instance?.Update();

        private void Update()
        {
            if (!initialized || inputFrame == Time.frameCount) return;
            inputFrame = Time.frameCount;
            if (!TryMaintainControlSession())
            {
                Fail("Another module owns external control; VR could not resume its session");
                return;
            }
            try
            {
                Status = rig != null && rig.HeadTracked ? "OpenXR active, headset tracked" : "OpenXR active, waiting for headset tracking";
                if (Time.unscaledTime >= nextDiscovery) Discover();
                PublishControls();
            }
            catch (Exception exception) { Fail("VR frame failed: " + exception); }
        }
        private void PublishControls()
        {
            xrBackend?.Pump();
            if (!TryMaintainControlSession())
            {
                Fail("Another module owns external control; VR could not resume its session");
                return;
            }
            rig?.RefreshTrackingPose();
            VrQteInput.Prepare();
            ulong held = 0, down = 0, up = 0;
            foreach (NeuroMita.Modding.ExternalInputAction externalAction in Enum.GetValues(typeof(NeuroMita.Modding.ExternalInputAction)))
            {
                int index = (int)externalAction;
                if (index < 0 || index >= 64) continue;
                ulong bit = 1UL << index;
                if (Input.Held(externalAction)) held |= bit;
                if (Input.Down(externalAction)) down |= bit;
                if (Input.Up(externalAction)) up |= bit;
            }
            if (VrDebugControl.PauseRequested)
            {
                held |= 1UL << (int)NeuroMita.Modding.ExternalInputAction.Pause;
                down |= 1UL << (int)NeuroMita.Modding.ExternalInputAction.Pause;
                VrDebugControl.PauseRequested = false;
            }
            ulong pauseBit = 1UL << (int)NeuroMita.Modding.ExternalInputAction.Pause;
            if (!VrQteInput.CapturesActions && (down & pauseBit) != 0 && (settingsPanel?.ClosePage() == true || games?.CloseActive() == true))
            {
                held &= ~pauseBit;
                down &= ~pauseBit;
                up &= ~pauseBit;
            }
            bool gameplay = rig?.Player != null && rig.Player.gameObject.activeInHierarchy;
            if (!gameplay)
            {
                ulong menuActions = 1UL << (int)NeuroMita.Modding.ExternalInputAction.Click;
                if (AppServices.GetOrNull<UIManager>()?.CanGoBack == true) menuActions |= pauseBit;
                held &= menuActions;
                down &= menuActions;
                up &= menuActions;
            }
            if (VrQteInput.CapturesActions) held = down = up = 0;
            Vector2 movement = gameplay && !VrQteInput.HasActiveQte ? Input.Move : Vector2.zero;
            VrDiagnostics.Controls(rig, held, down, movement);
            Vector2 pointer = Presenter != null ? Presenter.PointerPosition : Vector2.zero;
            if (!TryMaintainControlSession() || !NeuroMita.Modding.ExternalControlBridge.SetInput(controlSession, held, down, up,
                movement.x, movement.y, true, true, pointer.x, pointer.y, Presenter != null) ||
                !NeuroMita.Modding.ExternalControlBridge.SetPointerButtons(controlSession,
                    !VrQteInput.CapturesActions && Input.PointerPrimaryHeld, !VrQteInput.CapturesActions && Input.PointerPrimaryDown, !VrQteInput.CapturesActions && Input.PointerPrimaryUp))
            {
                Fail("External control session was lost");
                return;
            }
            VrDiagnostics.HostControls(rig, down, movement);
            NeuroMita.Modding.ExternalControlBridge.SetCameraOwnership(controlSession, rig != null ? rig.Player : null, true);
            NeuroMita.Modding.ExternalControlBridge.SetPresentation(controlSession, true, !VrQteInput.CapturesActions && (Input.HelpDown || VrDebugControl.HelpRequested), ResolveHelpText());
            VrDebugControl.HelpRequested = false;
            NeuroMita.Modding.ExternalControlBridge.SetSpeechPolicy(controlSession, true, !VrQteInput.CapturesActions && rig?.HeadTracked == true && rig.LeftHandTracked);
            NeuroMita.Modding.ExternalControlBridge.SetAvatarPresentation(controlSession, Settings?.VrAvatarMode ?? 0, rig?.UseIsolatedHands == true);
            while (NeuroMita.Modding.ExternalControlBridge.TryDequeueCanvas(controlSession, out var canvas))
                Presenter?.PresentCanvas(canvas);
            if (rig != null)
            {
                float bodyYaw = rig.Player?.Camera != null ? rig.Player.Camera.GetYawRotation().eulerAngles.y : 0f;
                float directionYaw = rig.Eye != null ? rig.Eye.transform.eulerAngles.y : bodyYaw;
                if (Settings?.VrMovementDirection == 2) directionYaw = bodyYaw;
                else if (Settings?.VrMovementDirection == 1 && rig.LeftHandTracked) directionYaw = rig.LeftHand.eulerAngles.y;
                NeuroMita.Modding.ExternalControlBridge.SetLocomotion(controlSession, directionYaw, (Settings?.VrMovementSpeed ?? 100) * 0.01f);
                Ray ray = rig.PointerRay;
                if (contextActions != null) ray = contextActions.ResolveInteractionRay(ray, Presenter);
                NeuroMita.Modding.ExternalControlBridge.SetPointer(controlSession, ray.origin.x, ray.origin.y, ray.origin.z,
                    ray.direction.x, ray.direction.y, ray.direction.z, rig.PointerHandTracked, Presenter?.HasUiTarget == true);
            }
        }

        private static string ResolveHelpText()
        {
            const string key = "ui.gameplay_help.vr";
            string text = LocalizationManager.GetTableString(key);
            if (string.IsNullOrWhiteSpace(text) || text == key)
                text = "<b>VR controls</b>\nLeft stick: move; right stick: turn\nLeft Y: pause / back / close player\nRight stick click: show / hide hints\nRight trigger: click / drag UI\nRight grip: interact\nLeft trigger: inventory; left X: chat\nRight A: jump / submit chat; right B: action menu\nHold both stick clicks: recenter";
            return text
                .Replace("Right trigger: click / drag UI", "Aim the cyan ray at UI\nRight trigger: click / drag UI")
                .Replace("Правый триггер: выбор / перетаскивание", "Цельтесь лучом в интерфейс\nПравый триггер: выбор / перетаскивание");
        }

        private void Discover()
        {
            nextDiscovery = Time.unscaledTime + 0.5f;
            var player = AppServices.GetOrNull<GlobalReferences>()?.PlayerActor;
            if (player == null) player = FindFirstObjectByType<PlayerActor>();
            Scene content = SceneManager.GetActiveScene();
            Camera menuCamera = MenuSceneResolver.ResolvePrimaryRoot(content) != null
                ? MenuSceneResolver.ResolvePrimaryCamera(content) : null;
            Camera camera = SelectCamera(player != null ? player.MainCamera : null, menuCamera, Camera.main, Camera.allCameras);
            if (player != null && camera != player.MainCamera) player = null;
            if (menuCamera != null) player = null;
            if (rig != null && (camera == null || rig.Eye != camera || rig.Player != player))
            {
                hud.Restore();
                Presenter.Bind(null);
                rig.Restore();
                Destroy(rig);
                rig = null;
            }
            if (camera != null && rig == null)
            {
                VrDiagnostics.Mark("rig attach", "scene=" + content.name + ", camera=" + camera.name + ", player=" + (player != null));
                rig = camera.gameObject.AddComponent<VrPlayerRig>();
                rig.Bind(camera, player);
                VrDiagnostics.Mark("rig bound");
                Presenter.Bind(rig);
                VrDiagnostics.Mark("pointer bound");
                hud.Bind(rig, Presenter);
                VrDiagnostics.Mark("hud bound");
            }
            Presenter.RefreshCanvases();
            RefreshMenuAttention(menuCamera != null && rig != null ? rig.Eye.transform : null);
            hud.RefreshViews(AppServices.GetOrNull<UIManager>()?.CurrentGameUIRoot);
        }

        private void RefreshMenuAttention(Transform target)
        {
            if (menuAttentionTarget != target || target == null)
            {
                foreach (var lease in menuAttention.Values) lease.Dispose();
                menuAttention.Clear();
                menuAttentionTarget = target;
            }
            if (target == null) return;
            var stale = new System.Collections.Generic.List<MenuMita>();
            foreach (var pair in menuAttention)
                if (pair.Key == null) { pair.Value.Dispose(); stale.Add(pair.Key); }
            foreach (var mita in stale) menuAttention.Remove(mita);
            foreach (var mita in FindObjectsByType<MenuMita>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (mita.gameObject.scene == SceneManager.GetActiveScene() && !menuAttention.ContainsKey(mita))
                    menuAttention.Add(mita, VrHostLease.Wrap(mita.AcquireAttentionTarget(target)));
        }

        private static Camera SelectCamera(Camera player, Camera menu, Camera fallback, Camera[] candidates)
        {
            static bool Usable(Camera camera) => camera != null && camera.isActiveAndEnabled && camera.targetTexture == null;
            if (Usable(menu)) return menu;
            if (Usable(player)) return player;
            if (Usable(fallback)) return fallback;
            foreach (var camera in candidates)
                if (Usable(camera) && camera.GetComponent<AudioListener>()?.enabled == true) return camera;
            return null;
        }

        private static void Cleanup(Action action, string operation)
        {
            try { action(); }
            catch (Exception exception) { Debug.LogWarning("[NeuroMita VR] Cleanup " + operation + ": " + exception); }
        }
        private void OnDestroy()
        {
            initialized = false;
            Cleanup(VrEarlyInputLoop.Remove, "early input loop");
            Cleanup(VrQteHooks.Remove, "QTE hooks");
            Cleanup(VrDiscordPresence.Remove, "Discord presence");
            Cleanup(() => VrHostEvents.Scenes(OnSceneLoaded, OnActiveSceneChanged, false), "scene callbacks");
            Cleanup(() => RefreshMenuAttention(null), "menu attention");
            Cleanup(() => hud?.Restore(), "HUD");
            Cleanup(() => NeuroMita.Modding.ExternalControlBridge.CloseSession(controlSession), "control session");
            controlSession = null;
            Cleanup(() => inputRegistration?.Dispose(), "input registration");
            inputRegistration = null;
            if (rig != null)
            {
                Cleanup(rig.Restore, "rig restore");
                Cleanup(() => Destroy(rig), "rig destroy");
            }
            Cleanup(() => Presenter?.Restore(), "UI restore");
            Cleanup(() => Keyboard?.Clear(), "keyboard");
            Keyboard = null;
            Cleanup(() => graphics?.Restore(), "graphics restore");
            Cleanup(() => Input?.Dispose(), "input dispose");
            Input = null;
            Presenter = null;
            Cleanup(() => xrBackend?.Dispose(), "XR dispose");
            xrBackend = null;
            if (instance == this) instance = null;
            if (Status.StartsWith("OpenXR active")) Status = "Desktop";
            if (restartRequested && Application.isPlaying)
            {
                restartRequested = false;
                StartVr();
            }
        }
    }
}
