using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using Il2CppInterop.Runtime;

namespace NeuroMita.VR
{
    internal static class VrEarlyInputLoop
    {
        private static PlayerLoopSystem.UpdateFunction callback;
        public static void Install()
        {
            if (callback != null) return;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            callback = DelegateSupport.ConvertDelegate<PlayerLoopSystem.UpdateFunction>((Action)VrRuntime.PrepareInputFrame);
            if (!Insert(loop)) { callback = null; throw new InvalidOperationException("Unity behaviour update stage missing."); }
            PlayerLoop.SetPlayerLoop(loop);
            if (!Contains(PlayerLoop.GetCurrentPlayerLoop())) throw new InvalidOperationException("VR input PlayerLoop node did not persist.");
            VrDiagnostics.Mark("input loop", "verified before ScriptRunBehaviourUpdate");
        }

        private static bool Insert(PlayerLoopSystem parent)
        {
            var children = parent.subSystemList;
            if (children == null) return false;
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].type == Il2CppType.Of<UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate>())
                {
                    var updated = new PlayerLoopSystem[children.Length + 1];
                    for (int j = 0; j < i; j++) updated[j] = children[j];
                    updated[i] = new PlayerLoopSystem { type = Il2CppType.Of<VrRuntime>(), updateDelegate = callback };
                    for (int j = i; j < children.Length; j++) updated[j + 1] = children[j];
                    parent.subSystemList = updated;
                    return true;
                }
                var child = children[i];
                if (Insert(child))
                {
                    children[i] = child;
                    parent.subSystemList = children;
                    return true;
                }
            }
            return false;
        }

        private static bool Contains(PlayerLoopSystem parent)
        {
            if (parent.type == Il2CppType.Of<VrRuntime>() && parent.updateDelegate == callback) return true;
            if (parent.subSystemList != null)
                foreach (var child in parent.subSystemList) if (Contains(child)) return true;
            return false;
        }
        public static void Remove()
        {
            if (callback == null) return;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            RemoveOwnNodes(loop);
            PlayerLoop.SetPlayerLoop(loop);
            callback = null;
        }

        private static void RemoveOwnNodes(PlayerLoopSystem parent)
        {
            var children = parent.subSystemList;
            if (children == null) return;
            var retained = new List<PlayerLoopSystem>();
            foreach (var child in children)
            {
                if (child.type == Il2CppType.Of<VrRuntime>() && child.updateDelegate == callback) continue;
                RemoveOwnNodes(child);
                retained.Add(child);
            }
            parent.subSystemList = retained.ToArray();
        }
    }
}
