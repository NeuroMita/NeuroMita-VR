using UnityEngine;
using UnityEngine.Bindings;

namespace NeuroMita.VR
{
    internal static class VrPhysicsQueries
    {
        public static unsafe int BoxCastNonAlloc(Vector3 center, Vector3 halfExtents, Vector3 direction,
            RaycastHit[] results, Quaternion orientation, float distance, int mask, QueryTriggerInteraction triggers)
        {
            var scene = Physics.defaultPhysicsScene;
            fixed (RaycastHit* buffer = results)
            {
                var span = new ManagedSpanWrapper(buffer, results.Length);
                return PhysicsScene.Internal_BoxCastNonAlloc_Injected(ref scene, ref center, ref halfExtents,
                    ref direction, ref span, ref orientation, distance, mask, triggers);
            }
        }
    }
}
