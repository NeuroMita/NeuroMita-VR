using NeuroMita.Modding;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrConsoleAccess
    {
        private static float started;
        private static bool pressed, fired, pulse, waiting, commandsPending;
        private static float commandsDeadline;
        private static int commandsFrame;

        internal static void Filter(ref ulong held, ref ulong down, ref ulong up)
        {
            PumpCommands();
            ulong bit = 1UL << (int)ExternalInputAction.OpenChat;
            bool raw = (held & bit) != 0;
            bool ready = !VrQteInput.CapturesActions && VrRuntime.Rig?.Player != null &&
                VrRuntime.Rig.HeadTracked && VrRuntime.Rig.LeftHandTracked && !SceneService.IsTransitionInProgress;
            held &= ~bit; down &= ~bit; up &= ~bit;
            if (pulse) { up |= bit; pulse = false; }
            if (!ready) { pressed = false; fired = false; waiting = true; return; }
            if (waiting) { if (!raw) waiting = false; return; }
            if (raw)
            {
                if (!pressed) { started = Time.unscaledTime; pressed = true; fired = false; }
                if (!fired && Time.unscaledTime - started >= 3f) { fired = true; Toggle(); }
            }
            else if (pressed)
            {
                if (!fired) { held |= bit; down |= bit; up &= ~bit; pulse = true; }
                pressed = fired = false;
            }
        }

        internal static void Toggle()
        {
            var console = Object.FindFirstObjectByType<ConsoleInterface>(FindObjectsInactive.Include);
            var ui = AppServices.GetOrNull<UIManager>();
            if (console != null && console.isActiveAndEnabled)
            {
                commandsPending = false;
                VrRuntime.Keyboard?.Clear();
                ui?.RequestClose<ConsoleInterface>();
                return;
            }
            OpenCommands();
        }

        internal static void OpenCommands()
        {
            var ui = AppServices.GetOrNull<UIManager>();
            if (ui == null) return;
            ui.RequestOpen<ConsoleInterface>();
            commandsPending = true;
            commandsFrame = Time.frameCount;
            commandsDeadline = Time.unscaledTime + 5f;
        }

        private static void PumpCommands()
        {
            if (!commandsPending) return;
            if (Time.unscaledTime > commandsDeadline)
            {
                commandsPending = false;
                Debug.LogWarning("[NeuroMita VR] Console command catalog did not become available");
                return;
            }
            if (Time.frameCount - commandsFrame < 3) return;
            if (AppServices.GetOrNull<UIManager>()?.FocusedType?.FullName != typeof(ConsoleInterface).FullName) return;
            var console = Object.FindFirstObjectByType<ConsoleInterface>();
            if (console == null) return;
            var manager = console.GetComponent<ConsoleCommandManager>();
            if (manager == null) manager = console.GetComponentInChildren<ConsoleCommandManager>(true);
            if (manager == null) manager = Object.FindFirstObjectByType<ConsoleCommandManager>(FindObjectsInactive.Include);
            if (manager == null) return;
            manager.OpenThisManager();
            VrRuntime.Keyboard?.Clear();
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
            commandsPending = false;
            VrRuntime.Presenter?.RefreshCanvases();
            VrRuntime.Presenter?.Recenter();
            VrDiagnostics.Mark("console command catalog", "native ConsoleCommandManager opened");
        }

        internal static void Clear() { pressed = fired = pulse = commandsPending = false; waiting = true; }
    }
}
