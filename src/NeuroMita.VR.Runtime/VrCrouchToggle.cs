using System;

namespace NeuroMita.VR
{
    internal sealed class VrCrouchToggle
    {
        private bool armed;
        internal bool IsCrouched { get; private set; }
        internal void Sample(float x, float y, bool canToggle)
        {
            if (!canToggle || float.IsNaN(x) || float.IsNaN(y) || float.IsInfinity(x) || float.IsInfinity(y))
            { armed = false; return; }
            if (Math.Abs(x) <= 0.25f && Math.Abs(y) <= 0.25f) { armed = true; return; }
            if (!armed) return;
            if (Math.Abs(x) >= 0.7f || y >= 0.7f) { armed = false; return; }
            if (y > -0.7f) return;
            if (Math.Abs(x) <= 0.35f) IsCrouched = !IsCrouched;
            armed = false;
        }
        internal void Reset() { IsCrouched = false; armed = false; }
    }
}
