namespace NeuroMita.VR
{
    internal sealed class VrHapticGate
    {
        private float until;
        private int priority = -1;
        private readonly float[] next = new float[4];
        internal bool Accept(float now, float duration, int kind)
        {
            if (kind < 0 || kind >= next.Length || float.IsNaN(now) || float.IsInfinity(now)
                || duration <= 0f || duration > 0.18f || float.IsNaN(duration)) return false;
            if (now < next[kind] || (now < until && kind < priority)) return false;
            next[kind] = now + (kind == 0 ? 0.18f : kind == 3 ? 0.15f : 0.04f);
            priority = kind;
            until = now + duration;
            return true;
        }
        internal void Reset() { until = 0f; priority = -1; System.Array.Clear(next, 0, next.Length); }
    }
}
