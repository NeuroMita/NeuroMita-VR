using System;
using System.Reflection;
using HarmonyLib;
using NeuroMita.Legacy.Games;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrSnowballInput
    {
        private static Harmony hooks;
        private static TVGamesModule module;
        private static float discoverAt;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.SnowballInput");
            try
            {
                foreach (string name in new[] { "FixedUpdate", "UpdateGameplay" })
                {
                    var method = typeof(MT_GameCnowballs).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method == null) throw new MissingMethodException("MT_GameCnowballs." + name);
                    candidate.Patch(method, postfix: new HarmonyMethod(typeof(VrSnowballInput).GetMethod(name + "Postfix", BindingFlags.Static | BindingFlags.NonPublic)));
                }
                hooks = candidate;
                Debug.Log("[NeuroMita VR] Installed snowball controller input hooks");
            }
            catch (Exception error)
            {
                candidate.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] Snowball input adapter unavailable: " + error);
            }
        }
        private static bool Ready(MT_GameCnowballs game)
        {
            if (!VrRuntime.IsActive || !game.play || VrRuntime.Rig?.HeadTracked != true || !VrRuntime.Rig.LeftHandTracked
                || VrQteInput.HasActiveQte || Time.timeScale <= 0f || SceneService.IsTransitionInProgress
                || AppServices.GetOrNull<UIManager>()?.FocusedType != null) return false;
            if (module == null || !module.IsOpen || Time.unscaledTime >= discoverAt)
            {
                discoverAt = Time.unscaledTime + 0.5f;
                module = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsByType<TVGamesModule>(FindObjectsSortMode.None))
                    if (candidate.IsOpen) { module = candidate; break; }
            }
            var active = module?.Controller?.ActiveGame?.TryCast<LegacyPinguinTelevisionGame>();
            return module?.IsOpen == true && module.Controller.IsGameActive && active?.ResolveSnowballGame() == game;
        }
        private static void FixedUpdatePostfix(MT_GameCnowballs __instance)
        {
            if (!Ready(__instance) || __instance.playerRb == null) return;
            Vector2 move = VrRuntime.Input.Move;
            Vector3 velocity = __instance.playerRb.linearVelocity;
            velocity.x += move.x * Time.fixedDeltaTime * 8f;
            velocity.z += move.y * Time.fixedDeltaTime * 8f;
            __instance.playerRb.linearVelocity = velocity;
        }
        private static void UpdateGameplayPostfix(MT_GameCnowballs __instance)
        {
            if (!Ready(__instance)) return;
            Vector2 move = VrRuntime.Input.Move;
            if (move.sqrMagnitude <= 0.000001f) return;
            if (__instance.playerRb != null)
            {
                var body = __instance.playerRb.transform;
                var rotation = Quaternion.LookRotation(new Vector3(move.x, 0f, move.y), Vector3.up);
                body.rotation = Quaternion.Lerp(body.rotation, rotation, Time.deltaTime);
            }
            MT_GameCnowballs.SetAnimatorMoveParam(__instance.animPlayer, 1f);
            MT_GameCnowballs.UpdateAnimatorSpeed(__instance.animPlayer, __instance.playerRb != null ? __instance.playerRb.linearVelocity.magnitude : 0f);
            MT_GameCnowballs.PlayParticleIfStopped(__instance.particleMovePlayer);
        }
        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; module = null; discoverAt = 0f; }
    }
}
