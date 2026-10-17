namespace NeuroMita.VR
{
    public sealed class VrSettings
    {
        public static VrSettings Current { get; set; } = new VrSettings();
        public int VrSmoothTurnRateDegreesPerSecond { get; set; } = 90;
        public int VrTurnMode { get; set; } = 0;
        public int VrSnapAngle { get; set; } = 45;
        public int VrMovementDirection { get; set; } = 0;
        public int VrMovementSpeed { get; set; } = 100;
        public int VrSprintMode { get; set; } = 0;
        public bool VrRoomscale { get; set; } = true;
        public bool VrSeated { get; set; } = false;
        public int VrHeightOffset { get; set; } = 0;
        public int VrMoveDeadzone { get; set; } = 15;
        public int VrTurnDeadzone { get; set; } = 15;
        public int VrUiDistance { get; set; } = 150;
        public int VrUiScale { get; set; } = 100;
        public int VrUiHeight { get; set; } = 0;
        public int VrUiFollow { get; set; } = 1;
        public bool VrHudOnWrist { get; set; } = true;
        public bool VrHotbarOnGaze { get; set; } = true;
        public bool VrPointerLeft { get; set; } = false;
        public bool VrHandPhysics { get; set; } = true;
        public bool VrTouchAi { get; set; } = true;
        public int VrGraphicsPreset { get; set; } = 0;
        public bool VrKeyboard { get; set; } = true;
        public int VrAvatarMode { get; set; } = 0;
        public int VrLeftWristPitch { get; set; } = 0;
        public int VrLeftWristYaw { get; set; } = 0;
        public int VrLeftWristRoll { get; set; } = 0;
        public int VrRightWristPitch { get; set; } = 0;
        public int VrRightWristYaw { get; set; } = 0;
        public int VrRightWristRoll { get; set; } = 0;
        public int VrWristSideOffset { get; set; } = 0;
        public int VrWristHeightOffset { get; set; } = 0;
        public int VrWristForwardOffset { get; set; } = 0;
        public int VrVignette { get; set; } = 0;
        public bool VrWallFade { get; set; } = true;
        public int VrHaptics { get; set; } = 2;
        public bool VrSmoothTurn => VrTurnMode == 1;
        public bool VrHideHands => VrAvatarMode == 2;

        public static VrSettings Resolve()
        {
            if (Current == null) Current = new VrSettings();
#if !NEUROMITA_VR_EXTERNAL
            var host = AppServices.GetOrNull<NeuroMita.Settings.GameSettingsService>();
            if (host != null)
            {
                Current.VrSmoothTurnRateDegreesPerSecond = host.VrSmoothTurnRateDegreesPerSecond;
                Current.VrTurnMode = host.VrTurnMode;
                Current.VrSnapAngle = host.VrSnapAngle;
                Current.VrMovementDirection = host.VrMovementDirection;
                Current.VrMovementSpeed = host.VrMovementSpeed;
                Current.VrSprintMode = host.VrSprintMode;
                Current.VrRoomscale = host.VrRoomscale;
                Current.VrSeated = host.VrSeated;
                Current.VrHeightOffset = host.VrHeightOffset;
                Current.VrMoveDeadzone = host.VrMoveDeadzone;
                Current.VrTurnDeadzone = host.VrTurnDeadzone;
                Current.VrUiDistance = host.VrUiDistance;
                Current.VrUiScale = host.VrUiScale;
                Current.VrUiHeight = host.VrUiHeight;
                Current.VrUiFollow = host.VrUiFollow;
                Current.VrHudOnWrist = host.VrHudOnWrist;
                Current.VrHotbarOnGaze = host.VrHotbarOnGaze;
                Current.VrPointerLeft = host.VrPointerLeft;
                Current.VrHandPhysics = host.VrHandPhysics;
                Current.VrTouchAi = host.VrTouchAi;
                Current.VrGraphicsPreset = host.VrGraphicsPreset;
                Current.VrKeyboard = host.VrKeyboard;
                Current.VrAvatarMode = host.VrAvatarMode;
                Current.VrLeftWristPitch = host.VrLeftWristPitch;
                Current.VrLeftWristYaw = host.VrLeftWristYaw;
                Current.VrLeftWristRoll = host.VrLeftWristRoll;
                Current.VrRightWristPitch = host.VrRightWristPitch;
                Current.VrRightWristYaw = host.VrRightWristYaw;
                Current.VrRightWristRoll = host.VrRightWristRoll;
                Current.VrWristSideOffset = host.VrWristSideOffset;
                Current.VrWristHeightOffset = host.VrWristHeightOffset;
                Current.VrWristForwardOffset = host.VrWristForwardOffset;
                Current.VrVignette = host.VrVignette;
                Current.VrWallFade = host.VrWallFade;
                Current.VrHaptics = host.VrHaptics;
            }
#endif
            return Current;
        }
    }
}
