using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrEaselDrawing
    {
        private static Harmony hooks;
        internal static bool IsInstalled => hooks != null;
        internal static void Install()
        {
            if (hooks != null) return;
            var candidate = new Harmony("NeuroMita.VR.EaselDrawing");
            try
            {
                foreach (var pair in new[] { ("HandleDrawingInput", "DrawPrefix"), ("ProcessQueuedPointerSamples", "QueuePrefix") })
                {
                    var target = typeof(EaselController).GetMethod(pair.Item1, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var prefix = typeof(VrEaselDrawing).GetMethod(pair.Item2, BindingFlags.NonPublic | BindingFlags.Static);
                    if (target == null || prefix == null) throw new MissingMethodException(pair.Item1);
                    candidate.Patch(target, prefix: new HarmonyMethod(prefix));
                }
                var layout = typeof(EaselInteractionUI).GetMethod("UpdateResponsiveLayout", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (layout == null) throw new MissingMethodException("UpdateResponsiveLayout");
                candidate.Patch(layout, prefix: new HarmonyMethod(typeof(VrEaselDrawing).GetMethod(nameof(LayoutPrefix), BindingFlags.NonPublic | BindingFlags.Static)));
                hooks = candidate;
            }
            catch (Exception error)
            {
                candidate.UnpatchSelf();
                Debug.LogWarning("[NeuroMita VR] Easel drawing hooks unavailable: " + error.Message);
            }
        }

        internal static void Remove() { hooks?.UnpatchSelf(); hooks = null; }

        private static bool LayoutPrefix(EaselInteractionUI __instance) => !VrEaselLayout.TryApply(__instance);

        private static bool QueuePrefix(EaselController __instance, ref bool __result)
        {
            if (!VrRuntime.IsActive) return true;
            __instance._pendingPointerSamples.Clear();
            __result = false;
            return false;
        }

        private static void Cancel(EaselController c)
        {
            c._pointerWasPressing = false;
            c.SetCanvasCursorActive(false);
            c.Canvas?.CancelStroke();
        }

        private static bool DrawPrefix(EaselController __instance)
        {
            if (!VrRuntime.IsActive || !__instance.IsOpen) return true;
            var c = __instance;
            c._pendingPointerSamples.Clear();
            var rig = VrRuntime.Rig;
            if (rig?.HeadTracked != true || !rig.PointerHandTracked || VrQteInput.CapturesActions || Time.timeScale <= 0f)
            {
                c._awaitPointerReleaseAfterOpen = true;
                Cancel(c);
                return false;
            }
            bool held = VrRuntime.Input.PointerPrimaryHeld;
            if (c._awaitPointerReleaseAfterOpen)
            {
                if (!held) c._awaitPointerReleaseAfterOpen = false;
                Cancel(c);
                return false;
            }
            if (VrRuntime.Presenter?.HasUiTarget == true && held) c._awaitPointerReleaseAfterOpen = true;
            if (VrRuntime.Presenter?.HasUiTarget == true || c._canvasCollider == null || c.Canvas == null ||
                !c._canvasCollider.Raycast(rig.PointerRay, out var hit, 5f))
            {
                Cancel(c);
                return false;
            }
            c.UpdateBrushPreview(hit.textureCoord, held ? 1f : 0f, false);
            if (held)
            {
                if (!c._pointerWasPressing) c.Canvas.CancelStroke();
                if (!c._drawingStrokeStarted)
                {
                    c._drawingStrokeStarted = true;
                    MitaAI.RuntimeEventReporter.PlayerStartedDrawing(c._drawingContext);
                }
                c.Canvas.HandleInput(hit.textureCoord, true, 1f, false);
                c._pointerWasPressing = true;
            }
            else { c._pointerWasPressing = false; c.Canvas.CancelStroke(); }
            c.Canvas.FlushPendingComposite();
            return false;
        }
    }
}
