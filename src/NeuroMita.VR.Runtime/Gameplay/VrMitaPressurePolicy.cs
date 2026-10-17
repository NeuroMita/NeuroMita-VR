using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrMitaPressurePolicy
    {
        public const float MinimumDepth = 0.04f;
        public const float RequiredSeconds = 0.45f;
        public const float MaximumAcceleration = 0.6f;
        public const float MaximumSpeed = 0.08f;
        private float seconds;

        public Vector3 Sample(float depth, Vector3 direction, Vector3 velocity, float delta, bool allowed)
        {
            direction.y = 0;
            velocity.y = 0;
            if (!allowed || depth < MinimumDepth || direction.sqrMagnitude < 0.01f || delta <= 0)
            {
                Reset();
                return Vector3.zero;
            }
            seconds += Mathf.Min(delta, 0.05f);
            if (seconds < RequiredSeconds || velocity.sqrMagnitude >= MaximumSpeed * MaximumSpeed)
                return Vector3.zero;
            float strength = Mathf.InverseLerp(MinimumDepth, 0.09f, depth);
            float speedMargin = Mathf.Clamp01((MaximumSpeed - velocity.magnitude) /
                (MaximumAcceleration * Mathf.Max(delta, 0.001f)));
            return direction.normalized * (MaximumAcceleration * strength * speedMargin);
        }

        public void Reset() => seconds = 0;
    }
}
