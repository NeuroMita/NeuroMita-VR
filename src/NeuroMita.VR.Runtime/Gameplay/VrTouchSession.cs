using UnityEngine;

namespace NeuroMita.VR
{
    public sealed class VrTouchSession
    {
        private float lastContact = float.NegativeInfinity;
        private float segmentSeconds, headSeconds, nextContext, nextReaction, nextAttempt;
        private bool qualifiedSegment, reported, ended, reacted;
        public float ContactSeconds { get; private set; }
        public int TouchCount { get; private set; }
        public bool HeadTouched { get; private set; }
        public bool InContact { get; private set; }

        public bool Sample(bool touching, bool head, float delta, float now)
        {
            if (!touching) headSeconds = 0;
            if (now - lastContact >= 3f && !touching) { Reset(); return false; }
            if (touching)
            {
                if (now - lastContact >= 3f) Reset();
                if (now - lastContact > 0.25f) { segmentSeconds = headSeconds = 0; qualifiedSegment = false; }
                lastContact = now;
                InContact = true;
                ended = false;
                float elapsed = Mathf.Clamp(delta, 0f, 0.1f);
                segmentSeconds += elapsed;
                headSeconds = head ? headSeconds + elapsed : 0;
                if (qualifiedSegment) ContactSeconds += elapsed;
                if (!qualifiedSegment && segmentSeconds >= 0.12f)
                {
                    qualifiedSegment = true;
                    TouchCount++;
                    ContactSeconds += segmentSeconds;
                }
                if (qualifiedSegment && headSeconds >= 0.12f) HeadTouched = true;
                if (TouchCount > 0 && (!reported || now >= nextContext))
                {
                    reported = true;
                    nextContext = now + 1f;
                    return true;
                }
            }
            else if (now - lastContact > 0.25f)
            {
                InContact = false;
                segmentSeconds = headSeconds = 0;
                qualifiedSegment = false;
                if (reported && !ended) { ended = true; return true; }
            }
            return false;
        }

        public bool ReactionDue(float now) => InContact && !reacted && now >= nextReaction && now >= nextAttempt &&
            (ContactSeconds >= 5f || TouchCount >= 4);

        public void ReactionAttempted(float now, bool accepted)
        {
            nextAttempt = now + 2f;
            if (!accepted) return;
            reacted = true;
            nextReaction = now + 30f;
        }

        public void Cancel() => Reset();

        private void Reset()
        {
            ContactSeconds = 0;
            TouchCount = 0;
            HeadTouched = InContact = qualifiedSegment = reported = ended = reacted = false;
            segmentSeconds = headSeconds = nextContext = 0;
            lastContact = float.NegativeInfinity;
        }
    }
}
