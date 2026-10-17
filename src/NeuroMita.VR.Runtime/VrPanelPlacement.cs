using UnityEngine;

namespace NeuroMita.VR
{
    internal static class VrPanelPlacement
    {
        private static readonly RaycastHit[] hits = new RaycastHit[64];

        internal static void Place(RectTransform rect, Camera eye, Vector3 desired, float width)
        {
            Vector3 origin = eye.transform.position;
            Vector3 direction = desired - origin;
            if (direction.sqrMagnitude < 0.001f) direction = eye.transform.forward;
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            float height = width * rect.sizeDelta.y / rect.sizeDelta.x;
            float fraction = 1f;
            var rig = VrRuntime.Rig;
            for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    Vector3 end = desired + rotation * new Vector3(x * width * 0.5f, y * height * 0.5f, 0f);
                    Vector3 travel = end - origin;
                    float distance = travel.magnitude;
                    if (distance < 0.001f) continue;
                    int count = VrPhysicsQueries.BoxCastNonAlloc(origin, Vector3.one * 0.01f,
                        travel / distance, hits, Quaternion.identity, distance + 0.04f,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    if (count == hits.Length) fraction = Mathf.Min(fraction, 0.25f);
                    for (int i = 0; i < count; i++)
                    {
                        var collider = hits[i].collider;
                        if (collider == null || collider.transform.IsChildOf(rect) ||
                            (rig != null && collider.transform.IsChildOf(rig.transform)) ||
                            (rig?.Player != null && collider.transform.IsChildOf(rig.Player.transform)) ||
                            collider.GetComponentInParent<MitaAI.MitaActor>() != null) continue;
                        fraction = Mathf.Min(fraction, Mathf.Max(0.02f, hits[i].distance - 0.06f) / distance);
                    }
                }
            fraction = Mathf.Clamp(fraction, 0.02f, 1f);
            rect.SetPositionAndRotation(origin + (desired - origin) * fraction, rotation);
            rect.localScale = Vector3.one * (width * fraction / rect.sizeDelta.x);
        }
    }
}
