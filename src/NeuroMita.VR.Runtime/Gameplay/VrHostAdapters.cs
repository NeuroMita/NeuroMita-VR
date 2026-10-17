using System;
using System.Collections.Generic;
using MitaAI;
using NeuroMita.Player;
using NeuroMita.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace NeuroMita.VR
{
    internal static class VrUiEvents
    {
        public static UnityAction Callback(Action handler)
        {
#if NEUROMITA_VR_IL2CPP
            return (UnityAction)handler;
#else
            return new UnityAction(handler.Invoke);
#endif
        }
    }

    internal static class VrHostEvents
    {
        public static void Settings(GameSettingsService service, Action<GameSettingsChange> handler, bool add)
        {
#if NEUROMITA_VR_IL2CPP
            var callback = VrNativeCallbackCache<Il2CppSystem.Action<GameSettingsChange>>.Get(handler);
            if (add) service.add_SettingsChanged(callback); else { service.remove_SettingsChanged(callback); VrNativeCallbackCache<Il2CppSystem.Action<GameSettingsChange>>.Release(handler); }
#else
            if (add) service.SettingsChanged += handler; else service.SettingsChanged -= handler;
#endif
        }

        public static void Tracking(XRInputSubsystem input, Action<XRInputSubsystem> handler, bool add)
        {
#if NEUROMITA_VR_IL2CPP
            var callback = VrNativeCallbackCache<Il2CppSystem.Action<XRInputSubsystem>>.Get(handler);
            var combined = add ? Il2CppSystem.Delegate.Combine(input.trackingOriginUpdated, callback)
                : Il2CppSystem.Delegate.Remove(input.trackingOriginUpdated, callback);
            input.trackingOriginUpdated = combined?.Cast<Il2CppSystem.Action<XRInputSubsystem>>();
            if (!add) VrNativeCallbackCache<Il2CppSystem.Action<XRInputSubsystem>>.Release(handler);
#else
            if (add) input.trackingOriginUpdated += handler; else input.trackingOriginUpdated -= handler;
#endif
        }

        public static void Scenes(Action<Scene, LoadSceneMode> loaded, Action<Scene, Scene> changed, bool add)
        {
#if NEUROMITA_VR_IL2CPP
            var loadedCallback = VrNativeCallbackCache<UnityAction<Scene, LoadSceneMode>>.Get(loaded);
            var changedCallback = VrNativeCallbackCache<UnityAction<Scene, Scene>>.Get(changed);
            if (add) { SceneManager.add_sceneLoaded(loadedCallback); SceneManager.add_activeSceneChanged(changedCallback); }
            else { SceneManager.remove_sceneLoaded(loadedCallback); SceneManager.remove_activeSceneChanged(changedCallback); VrNativeCallbackCache<UnityAction<Scene, LoadSceneMode>>.Release(loaded); VrNativeCallbackCache<UnityAction<Scene, Scene>>.Release(changed); }
#else
            var loadedCallback = new UnityAction<Scene, LoadSceneMode>(loaded.Invoke);
            var changedCallback = new UnityAction<Scene, Scene>(changed.Invoke);
            if (add) { SceneManager.sceneLoaded += loadedCallback; SceneManager.activeSceneChanged += changedCallback; }
            else { SceneManager.sceneLoaded -= loadedCallback; SceneManager.activeSceneChanged -= changedCallback; }
#endif
        }

        public static void Camera(Action<Camera> handler, bool before, bool add)
        {
#if NEUROMITA_VR_IL2CPP
            var callback = VrNativeCallbackCache<UnityEngine.Camera.CameraCallback>.Get(handler);
            var previous = before ? UnityEngine.Camera.onPreCull : UnityEngine.Camera.onPostRender;
            var combined = add ? Il2CppSystem.Delegate.Combine(previous, callback) : Il2CppSystem.Delegate.Remove(previous, callback);
            var result = combined?.Cast<UnityEngine.Camera.CameraCallback>();
            if (before) UnityEngine.Camera.onPreCull = result; else UnityEngine.Camera.onPostRender = result;
            if (!add) VrNativeCallbackCache<UnityEngine.Camera.CameraCallback>.Release(handler);
#else
            var callback = new UnityEngine.Camera.CameraCallback(handler.Invoke);
            if (before) { if (add) UnityEngine.Camera.onPreCull += callback; else UnityEngine.Camera.onPreCull -= callback; }
            else { if (add) UnityEngine.Camera.onPostRender += callback; else UnityEngine.Camera.onPostRender -= callback; }
#endif
        }
    }

    internal static class VrHostViews
    {
        public static IReadOnlyList<MitaActor> ActiveActors()
        {
            var actors = AppServices.GetOrNull<MitaActorsManager>()?.GetActiveActorsSnapshot();
#if NEUROMITA_VR_IL2CPP
            if (actors == null) return null;
            int count = actors.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<MitaActor>>().Count;
            var copy = new List<MitaActor>(count);
            for (int i = 0; i < count; i++) copy.Add(actors[i]);
            return copy;
#else
            return actors;
#endif
        }

        public static IReadOnlyList<NeuroMita.Player.Controllers.PlayerItemActionController.HintInfo> ItemHints(NeuroMita.Player.Controllers.PlayerItemActionController controller)
        {
            var hints = controller?.CurrentHints;
#if NEUROMITA_VR_IL2CPP
            if (hints == null) return null;
            int count = hints.Cast<Il2CppSystem.Collections.Generic.IReadOnlyCollection<NeuroMita.Player.Controllers.PlayerItemActionController.HintInfo>>().Count;
            var copy = new List<NeuroMita.Player.Controllers.PlayerItemActionController.HintInfo>(count);
            for (int i = 0; i < count; i++) copy.Add(hints[i]);
            return copy;
#else
            return hints;
#endif
        }

        public static void CollectHints(NeuroMita.World.Interaction.WorldInteractionTarget target,
            List<NeuroMita.World.Interaction.WorldInteractionTarget.ScreenHintInfo> output)
        {
#if NEUROMITA_VR_IL2CPP
            var hints = new Il2CppSystem.Collections.Generic.List<NeuroMita.World.Interaction.WorldInteractionTarget.ScreenHintInfo>();
            target.CollectVisibleScreenHints(hints);
            output.Clear();
            for (int i = 0; i < hints.Count; i++) output.Add(hints[i]);
#else
            target.CollectVisibleScreenHints(output);
#endif
        }

        public static RectTransform ItemStatusRoot(ItemStatusController status)
        {
#if NEUROMITA_VR_IL2CPP
            return status?.root != null ? status.root.transform.TryCast<RectTransform>() : null;
#else
            return status?.ViewRoot;
#endif
        }

        public static Transform RadioAnchor(MusicListUI source)
        {
#if NEUROMITA_VR_IL2CPP
            return source?.radio != null ? source.radio.transform : null;
#else
            return source?.SourceRadio != null ? source.SourceRadio.transform : null;
#endif
        }

        public static bool IsScripted(MitaActor actor)
        {
#if NEUROMITA_VR_IL2CPP
            return actor?.Behaviour?.Active?.TryCast<MitaScriptedBehaviour>() != null;
#else
            return actor?.Behaviour?.Active is MitaScriptedBehaviour;
#endif
        }

        private static readonly System.Reflection.MethodInfo contextActionApi =
            typeof(NeuroMita.Player.Controllers.PlayerInteractionController).GetMethod("TryInvokeContextAction",
                new[] { typeof(ActionType), typeof(GameObject), typeof(bool) });
        private static readonly System.Reflection.MethodInfo tvHudApi =
            typeof(TVDeviceController).GetMethod("ShouldHideViewingHud", new[] { typeof(PlayerActor) });

        public static bool HideTvHud(PlayerActor actor)
        {
#if NEUROMITA_VR_IL2CPP
            return tvHudApi != null && (bool)tvHudApi.Invoke(null, new object[] { actor });
#else
            return TVDeviceController.ShouldHideViewingHud(actor);
#endif
        }

        public static bool InvokeContextAction(PlayerActor player, ActionType type, GameObject target, bool itemAction)
        {
#if NEUROMITA_VR_IL2CPP
            var input = AppServices.GetOrNull<InputManager>();
            var controller = player?.Interaction;
            if (controller == null || player.InputLocked || Time.timeScale <= 0f
                || (input != null && input.CurrentState != InputManager.InputState.Gameplay)) return false;
            if (target != null && target != controller.CurrentTarget && target != controller.CurrentTargetRaw
                && target != controller.HoveredInteractable?.gameObject
                && (!itemAction || (target != controller.CurrentLookTarget && target != controller.CurrentLookTargetRaw))) return false;
            if (contextActionApi != null) return (bool)contextActionApi.Invoke(controller, new object[] { type, target, itemAction });
            if (player.InteractLocked) return false;
            if (!itemAction && controller.TryDispatchInteractionAction(type)) return true;
            if (player.InteractLocked) return false;
            controller.ActionPressed?.Invoke(type);
            controller.ActionReleased?.Invoke(type);
            return true;
#else
            return player?.Interaction?.TryInvokeContextAction(type, target, itemAction) == true;
#endif
        }
    }
}