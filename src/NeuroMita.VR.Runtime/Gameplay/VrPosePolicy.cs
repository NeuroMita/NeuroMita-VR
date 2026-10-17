using UnityEngine;

namespace NeuroMita.VR
{
    public static class VrAvatarPolicy
    {
        public static bool IsolateHands(int mode) => mode != 1;
        public static bool HideAvatar(int mode) => mode == 2;
        public static bool ShowIsolatedHands(int mode) => mode == 0;
        public static bool HideBodyRenderer(int mode, string meshName)
            => mode == 2 || (mode == 0 && meshName != "Body");
        public static bool BodyCastsOnlyShadow(int mode, string meshName)
            => mode == 0 && meshName == "Body";
    }

    public sealed class VrSnapTurn
    {
        private bool armed = true;

        public float Sample(float axis, float degrees)
        {
            if (Mathf.Abs(axis) < 0.25f) armed = true;
            if (!armed || Mathf.Abs(axis) < 0.7f) return 0f;
            armed = false;
            return Mathf.Sign(axis) * degrees;
        }
    }

    public static class VrTurnPolicy
    {
        public static float SmoothDelta(float axis, int rateDegreesPerSecond, float unscaledDeltaTime, float deadzone = 0.15f)
        {
            if (Mathf.Abs(axis) <= deadzone || unscaledDeltaTime <= 0f) return 0f;
            float rate = Mathf.Clamp(rateDegreesPerSecond, 45, 240);
            return axis * rate * unscaledDeltaTime;
        }
    }

    public sealed class VrButtonState
    {
        private int frame = -1;
        public bool Held { get; private set; }
        public bool Down { get; private set; }
        public bool Up { get; private set; }

        public void Sample(bool value, int sampleFrame)
        {
            if (frame == sampleFrame) return;
            frame = sampleFrame;
            Down = value && !Held;
            Up = !value && Held;
            Held = value;
        }
    }

    public sealed class VrRecenterHold
    {
        private const float HoldSeconds = 0.5f;
        private float heldSeconds;
        private bool triggered;

        public bool Sample(bool held, float deltaTime)
        {
            if (!held)
            {
                heldSeconds = 0f;
                triggered = false;
                return false;
            }
            if (triggered) return false;
            heldSeconds += Mathf.Max(0f, deltaTime);
            if (heldSeconds < HoldSeconds) return false;
            triggered = true;
            return true;
        }
    }

    public static class VrPosePolicy
    {
        public static bool IsCrouched(bool wasCrouched, float heightDelta)
            => heightDelta <= (wasCrouched ? -0.2f : -0.35f);

        public static Quaternion WristRotation(Quaternion controllerRotation, Vector3 pointerDirection)
        {
            if (pointerDirection.sqrMagnitude < 0.0001f) return controllerRotation;
            var forward = pointerDirection.normalized;
            var up = Vector3.ProjectOnPlane(controllerRotation * Vector3.forward, forward);
            if (up.sqrMagnitude < 0.0001f)
                up = Vector3.ProjectOnPlane(controllerRotation * Vector3.right, forward);
            return Quaternion.LookRotation(forward, up.normalized);
        }

        public static Vector3 PointerDirection(Quaternion controllerRotation)
        {
            var pointerPose = controllerRotation * Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(-90f, 0f, 0f);
            return -(pointerPose * Vector3.forward);
        }

        public static Quaternion CalibratedWrist(Quaternion wrist, Vector3 degrees) => wrist * Quaternion.Euler(degrees);

        public static Vector3 WristOffset(Quaternion wrist, Vector3 centimeters, bool left)
            => wrist * new Vector3(left ? -centimeters.x : centimeters.x, centimeters.y, centimeters.z) * 0.01f;

        public static Vector3 HeadOffset(Vector3 position, Vector3 reference) => position - reference;

        public static Vector2 MovementInBodyFrame(Vector2 stick, float headYaw, float bodyYaw)
        {
            var direction = Quaternion.Euler(0f, headYaw - bodyYaw, 0f) * new Vector3(stick.x, 0f, stick.y);
            return Vector2.ClampMagnitude(new Vector2(direction.x, direction.z), 1f);
        }
    }
}
