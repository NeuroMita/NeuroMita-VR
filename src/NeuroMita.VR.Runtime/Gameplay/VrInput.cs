using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if NEUROMITA_VR_IL2CPP
using InputAction = NeuroMita.VR.VrNativeControl;
using InputActionMap = NeuroMita.VR.VrNativeControlMap;
#endif

namespace NeuroMita.VR
{
    public sealed class VrInput : IDisposable
#if !NEUROMITA_VR_IL2CPP
        , NeuroMita.Modding.IExternalInputProvider
#endif
    {
        private readonly InputActionMap map = new InputActionMap("NeuroMitaVR");
        private readonly Dictionary<NeuroMita.Modding.ExternalInputAction, InputAction> actions = new Dictionary<NeuroMita.Modding.ExternalInputAction, InputAction>();
        private readonly Dictionary<NeuroMita.Modding.ExternalInputAction, VrButtonState> buttons = new Dictionary<NeuroMita.Modding.ExternalInputAction, VrButtonState>();
        private readonly InputAction move, turn, leftClick, rightClick;
        private int sampledFrame = -1;
        private bool sprintToggled;
        private bool disposed;
        private readonly VrButtonState crouch = new VrButtonState();
        private readonly VrCrouchToggle crouchToggle = new VrCrouchToggle();
        private bool physicalCrouched;
        private IntPtr crouchPlayer;

        public bool IsActive => !disposed && VrRuntime.IsActive && ReferenceEquals(VrRuntime.Input, this);
        public bool AllowUnfocusedInput => true;

        public bool TryGetMovement(out Vector2 movement)
        {
            movement = Move;
            return IsActive;
        }

        public bool TryGetLookDelta(out Vector2 delta)
        {
            delta = Vector2.zero;
            return IsActive;
        }

        public bool TryGetPointerPosition(out Vector2 position)
        {
            position = IsActive && VrRuntime.Presenter != null ? VrRuntime.Presenter.PointerPosition : Vector2.zero;
            return IsActive && VrRuntime.Presenter != null;
        }

        public VrInput()
        {
            move = map.AddAction("Move", InputActionType.Value, "<XRController>{LeftHand}/primary2DAxis");
            turn = map.AddAction("Turn", InputActionType.Value, "<XRController>{RightHand}/primary2DAxis");
            leftClick = map.AddAction("LeftStickClick", InputActionType.Button, "<XRController>{LeftHand}/{Primary2DAxisClick}");
            rightClick = map.AddAction("RightStickClick", InputActionType.Button, "<XRController>{RightHand}/{Primary2DAxisClick}");
            Add(NeuroMita.Modding.ExternalInputAction.Click, "RightHand", "triggerPressed");
            Add(NeuroMita.Modding.ExternalInputAction.Interact, "RightHand", "gripPressed");
            Add(NeuroMita.Modding.ExternalInputAction.Jump, "RightHand", "primaryButton");
            Add(NeuroMita.Modding.ExternalInputAction.SubmitChat, "RightHand", "primaryButton");
            Add(NeuroMita.Modding.ExternalInputAction.RadialMenu, "RightHand", "secondaryButton");
            Add(NeuroMita.Modding.ExternalInputAction.OpenChat, "LeftHand", "primaryButton");
            Add(NeuroMita.Modding.ExternalInputAction.Inventory, "LeftHand", "triggerPressed");
            Add(NeuroMita.Modding.ExternalInputAction.PushToTalk, "LeftHand", "gripPressed");
            Add(NeuroMita.Modding.ExternalInputAction.Pause, "LeftHand", "secondaryButton");
            Add(NeuroMita.Modding.ExternalInputAction.Run, "LeftHand", "{Primary2DAxisClick}");
            foreach (var direction in new[] { NeuroMita.Modding.ExternalInputAction.MoveForward, NeuroMita.Modding.ExternalInputAction.MoveBackward,
                NeuroMita.Modding.ExternalInputAction.MoveLeft, NeuroMita.Modding.ExternalInputAction.MoveRight }) buttons[direction] = new VrButtonState();
            map.Enable();
        }

        private void Add(NeuroMita.Modding.ExternalInputAction action, string hand, string control)
        {
            actions[action] = map.AddAction(action.ToString(), InputActionType.Button, "<XRController>{" + hand + "}/" + control);
            buttons[action] = new VrButtonState();
        }

        private void Sample()
        {
            if (disposed) return;
            if (sampledFrame == Time.frameCount) return;
            sampledFrame = Time.frameCount;
            bool sprintTracking = VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.LeftHandTracked;
            var input = AppServices.GetOrNull<InputManager>();
            var rig = VrRuntime.Rig;
            if (!sprintTracking || Time.timeScale <= 0f || input?.CurrentState != InputManager.InputState.Gameplay)
                sprintToggled = false;
            SampleDirections();
            foreach (var pair in actions)
            {
                bool left = pair.Key == NeuroMita.Modding.ExternalInputAction.OpenChat || pair.Key == NeuroMita.Modding.ExternalInputAction.Inventory ||
                    pair.Key == NeuroMita.Modding.ExternalInputAction.PushToTalk || pair.Key == NeuroMita.Modding.ExternalInputAction.Pause || pair.Key == NeuroMita.Modding.ExternalInputAction.Run;
                bool tracked = VrRuntime.Rig != null && VrRuntime.Rig.HeadTracked &&
                    (left ? VrRuntime.Rig.LeftHandTracked : VrRuntime.Rig.RightHandTracked);
                buttons[pair.Key].Sample(VrRuntime.IsActive && tracked && pair.Value.IsPressed(), sampledFrame);
                if (pair.Key == NeuroMita.Modding.ExternalInputAction.Run && buttons[pair.Key].Down &&
                    sprintTracking && input?.CurrentState == InputManager.InputState.Gameplay)
                    sprintToggled = !sprintToggled;
            }
            SampleCrouch(input, rig);
        }

        private void SampleCrouch(InputManager input, VrPlayerRig rig)
        {
            var player = rig?.Player;
            IntPtr owner = player != null ? player.Pointer : IntPtr.Zero;
            bool tracked = VrRuntime.IsActive && rig?.HeadTracked == true && player != null;
            if (owner != crouchPlayer || !tracked || player?.Health?.IsDead == true || SceneService.IsTransitionInProgress)
            {
                ResetPosture();
                crouchPlayer = owner;
            }
            else if (!rig.RightHandTracked) crouchToggle.Reset();
            bool canCrouch = tracked && !player.InputLocked && player.Health?.IsDead != true && Time.timeScale > 0f
                && input?.CurrentState == InputManager.InputState.Gameplay && !SceneService.IsTransitionInProgress;
            physicalCrouched = canCrouch && VrPosePolicy.IsCrouched(physicalCrouched, rig.TrackedHeightDelta);
            bool canToggle = canCrouch && rig.RightHandTracked && !player.MoveLocked && !VrQteInput.HasActiveQte
                && player.Sequence?.IsPlaying != true && AppServices.GetOrNull<UIManager>()?.FocusedType == null
                && player.GetComponent<NeuroMita.Player.PlayerSelfieController>()?.IsActive != true
                && VrRuntime.Presenter?.HasUiTarget != true;
            Vector2 axis = Turn;
            crouchToggle.Sample(axis.x, axis.y, canToggle);
            crouch.Sample(canCrouch && (physicalCrouched || crouchToggle.IsCrouched), sampledFrame);
        }

        internal void ResetPosture()
        {
            crouchToggle.Reset();
            physicalCrouched = false;
            crouchPlayer = IntPtr.Zero;
        }

        private void SampleDirections()
        {
            Vector2 axis = Move;
            SampleDirection(NeuroMita.Modding.ExternalInputAction.MoveLeft, -axis.x);
            SampleDirection(NeuroMita.Modding.ExternalInputAction.MoveRight, axis.x);
            SampleDirection(NeuroMita.Modding.ExternalInputAction.MoveForward, axis.y);
            SampleDirection(NeuroMita.Modding.ExternalInputAction.MoveBackward, -axis.y);
        }
        private void SampleDirection(NeuroMita.Modding.ExternalInputAction action, float axis)
        {
            var state = buttons[action];
            state.Sample(axis > (state.Held ? 0.3f : 0.6f), sampledFrame);
        }

        public bool Held(NeuroMita.Modding.ExternalInputAction action)
        {
            if (disposed) return false;
            Sample();
            if (action == NeuroMita.Modding.ExternalInputAction.Sit) return crouch.Held;
            if (action == NeuroMita.Modding.ExternalInputAction.Run)
            {
                int mode = VrRuntime.Settings?.VrSprintMode ?? 0;
                if (mode == 2) return false;
                if (mode == 1) return sprintToggled;
            }
            return buttons.TryGetValue(action, out var state) && state.Held;
        }

        public bool Down(NeuroMita.Modding.ExternalInputAction action)
        {
            if (disposed) return false;
            Sample();
            if (action == NeuroMita.Modding.ExternalInputAction.Sit) return crouch.Down;
            if (action == NeuroMita.Modding.ExternalInputAction.Inventory && VrRuntime.Settings?.VrPointerLeft == true &&
                VrRuntime.Presenter?.HasUiTarget == true) return false;
            return buttons.TryGetValue(action, out var state) && state.Down;
        }

        public bool Up(NeuroMita.Modding.ExternalInputAction action)
        {
            if (disposed) return false;
            Sample();
            if (action == NeuroMita.Modding.ExternalInputAction.Sit) return crouch.Up;
            return buttons.TryGetValue(action, out var state) && state.Up;
        }

        private VrButtonState PointerPrimaryState
        {
            get
            {
                if (disposed) return null;
                Sample();
                var action = VrRuntime.Settings?.VrPointerLeft == true
                    ? NeuroMita.Modding.ExternalInputAction.Inventory : NeuroMita.Modding.ExternalInputAction.Click;
                return buttons.TryGetValue(action, out var state) ? state : null;
            }
        }

        public bool PointerPrimaryHeld => PointerPrimaryState?.Held == true;
        public bool PointerPrimaryDown => PointerPrimaryState?.Down == true;
        public bool PointerPrimaryUp => PointerPrimaryState?.Up == true;
        public bool PointerSecondaryHeld => Held(NeuroMita.Modding.ExternalInputAction.Interact);
        public bool PointerHeld => PointerPrimaryHeld || PointerSecondaryHeld;

        public Vector2 Move => IsActive && VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.LeftHandTracked
            ? ApplyDeadzone(move.ReadValue<Vector2>()) : Vector2.zero;
        public Vector2 Turn => IsActive && VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.RightHandTracked
            ? turn.ReadValue<Vector2>() : Vector2.zero;
        public bool RecenterHeld => IsActive && VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.LeftHandTracked &&
            VrRuntime.Rig.RightHandTracked && leftClick.IsPressed() && rightClick.IsPressed();
        public bool MenuRecenterDown => IsActive && VrRuntime.Rig?.Player == null &&
            VrRuntime.Rig?.HeadTracked == true && VrRuntime.Rig.RightHandTracked && rightClick.WasPressedThisFrame();
        public bool HelpDown => IsActive && VrRuntime.Rig?.Player != null && VrRuntime.Rig?.HeadTracked == true &&
            VrRuntime.Rig.RightHandTracked && rightClick.WasPressedThisFrame() && !leftClick.IsPressed();

        private static Vector2 ApplyDeadzone(Vector2 value)
        {
            float length = value.magnitude;
            float deadzone = Mathf.Clamp(VrRuntime.Settings?.VrMoveDeadzone ?? 15, 0, 95) * 0.01f;
            return length < deadzone ? Vector2.zero : value.normalized * Mathf.Clamp01((length - deadzone) / (1f - deadzone));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            sprintToggled = false;
            ResetPosture();
            map.Dispose();
        }
    }
}
