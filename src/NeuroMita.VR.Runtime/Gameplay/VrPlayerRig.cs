using NeuroMita.Player;
using UnityEngine;
using UnityEngine.XR;

namespace NeuroMita.VR
{
#if !NEUROMITA_VR_IL2CPP
    [DefaultExecutionOrder(10000)]
#endif
    public sealed partial class VrPlayerRig : MonoBehaviour
    {
        private sealed class HandMountBinding
        {
            public readonly Transform Mount;
            private readonly Transform parent;
            private readonly int sibling;
            private readonly Vector3 position, scale;
            private readonly Quaternion rotation;

            public HandMountBinding(Transform mount, Transform hand)
            {
                Mount = mount;
                parent = mount.parent;
                sibling = mount.GetSiblingIndex();
                position = mount.localPosition;
                rotation = mount.localRotation;
                scale = mount.localScale;
                mount.SetParent(hand, false);
                mount.localPosition = Vector3.zero;
                mount.localRotation = Quaternion.identity;
                mount.localScale = Vector3.one;
            }

        public void Restore()
            {
                if (Mount == null) return;
                Mount.SetParent(parent, false);
                Mount.SetSiblingIndex(sibling);
                Mount.localPosition = position;
                Mount.localRotation = rotation;
                Mount.localScale = scale;
            }
        }

        private HandMountBinding leftMountBinding, rightMountBinding;
        private Camera eye;
        private PlayerActor player;
        private Vector3 savedPosition, reference;
        private Quaternion savedRotation, referenceYaw;
        private float savedNear;
        private StereoTargetEyeMask savedStereo;
        private bool savedOrthographic;
        private int uiLayerMask;
        private bool savedUiLayerVisible;
        private Animator cameraAnimator;
        private bool cameraAnimatorEnabled;
        private VrMenuStage menuStage;
        private bool calibrated;
        private readonly VrSnapTurn snapTurn = new VrSnapTurn();
        private readonly VrRecenterHold recenterHold = new VrRecenterHold();
        private Vector3 menuAnchor;
        private Quaternion menuRotation;
        private Transform leftHand, rightHand;
        private bool leftTracked, rightTracked;
        private Vector3 pointerDirection;
        private Vector3 rawHeadPosition;
        private XRInputSubsystem inputSubsystem;
        private bool restored;
        private NeuroMita.Player.Controllers.PlayerHandPoserController poser;
        private VrHandProxy leftProxy, rightProxy;
        private VrClothHandContacts clothContacts;
        private VrMitaTouchTracker touchTracker;
        private VrMitaHandPhysics handPhysics;
        private VrComfortPresenter comfort;
        public bool UseIsolatedHands => HasIsolatedHands && VrAvatarPolicy.IsolateHands(VrRuntime.Settings?.VrAvatarMode ?? 0);
        public bool HasIsolatedHands => leftProxy != null && rightProxy != null;

        public Camera Eye => eye;
        public PlayerActor Player => player;
        public Transform LeftHand => leftHand;
        public Transform RightHand => rightHand;
        public bool HeadTracked { get; private set; }
        public float TrackedHeightDelta => HeadTracked && calibrated ? rawHeadPosition.y - reference.y : 0f;
        public bool RightHandTracked => rightTracked;
        public bool LeftHandTracked => leftTracked;
        public Vector3 BodyPosition => player != null ? player.transform.position : menuAnchor;
        public float BodyYaw => AnchorRotation.eulerAngles.y;
        public bool PointerHandTracked => VrRuntime.Settings?.VrPointerLeft == true ? leftTracked : rightTracked;
        public Ray PointerRay => PointerHandTracked
            ? new Ray(VrRuntime.Settings?.VrPointerLeft == true ? leftHand.position : rightHand.position,
                VrRuntime.Settings?.VrPointerLeft == true ? leftPointerDirection : pointerDirection)
            : new Ray(eye.transform.position, eye.transform.forward);
        private Vector3 leftPointerDirection;
        private int lastTurnMode = -1;
        private bool lastSeated;

        public void Bind(Camera camera, PlayerActor actor)
        {
            eye = camera;
            player = actor;
            restored = false;
            VrDiagnostics.Mark("rig bind", "before poser");
            poser = actor != null ? NeuroMita.Player.Controllers.PlayerHandPoserController.Ensure(actor) : null;
            savedPosition = eye.transform.localPosition;
            savedRotation = eye.transform.localRotation;
            savedNear = eye.nearClipPlane;
            savedStereo = eye.stereoTargetEye;
            savedOrthographic = eye.orthographic;
            int uiLayer = LayerMask.NameToLayer("UI");
            uiLayerMask = uiLayer >= 0 ? 1 << uiLayer : 0;
            savedUiLayerVisible = (eye.cullingMask & uiLayerMask) != 0;
            cameraAnimator = actor == null ? eye.GetComponent<Animator>() : null;
            cameraAnimatorEnabled = cameraAnimator != null && cameraAnimator.enabled;

            if (actor == null) menuStage = VrMenuStage.Create(eye);
            menuAnchor = eye.transform.position;
            menuRotation = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
            eye.nearClipPlane = 0.02f;
            eye.orthographic = false;
            eye.ResetProjectionMatrix();
            eye.ResetStereoProjectionMatrices();
            eye.stereoTargetEye = StereoTargetEyeMask.Both;
            leftHand = CreateHand("VR Left Controller");
            rightHand = CreateHand("VR Right Controller");
            VrDiagnostics.Mark("rig bind", "before hand resources");
            if (player?.PersonRoot != null)
            {
                SkinnedMeshRenderer source = null;
                foreach (var renderer in player.PersonRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    if (renderer.sharedMesh != null && renderer.sharedMesh.name == "Arms") { source = renderer; break; }
                if (source != null && VrAssets.Load<Mesh>("NeuroMitaVrLeftHand") != null && VrAssets.Load<Mesh>("NeuroMitaVrRightHand") != null)
                {
                    VrDiagnostics.Mark("rig bind", "before left proxy");
                    leftProxy = CreateProxy(leftHand, "Left", source.sharedMaterials);
                    VrDiagnostics.Mark("rig bind", "before right proxy");
                    rightProxy = CreateProxy(rightHand, "Right", source.sharedMaterials);
                    VrDiagnostics.Mark("rig bind", "before cloth contacts");
                    clothContacts = gameObject.AddComponent<VrClothHandContacts>();
                    clothContacts.Bind(leftProxy.ClothCollider, rightProxy.ClothCollider, player.transform);
                    touchTracker = gameObject.AddComponent<VrMitaTouchTracker>();
                    touchTracker.Bind(this, leftProxy, rightProxy);
                    handPhysics = gameObject.AddComponent<VrMitaHandPhysics>();
                    handPhysics.Bind(this, leftProxy, rightProxy);
                }
            }
            VrDiagnostics.Mark("rig bind", "before tracking/render hooks");
            VrFrameHooks.BeforeRender += ApplyPose;
            inputSubsystem = VrRuntime.XrInput;
            if (inputSubsystem != null) VrHostEvents.Tracking(inputSubsystem, OnTrackingOriginUpdated, true);
            ApplyPose();
            VrDiagnostics.Mark("rig bind", "before comfort");
            comfort = gameObject.AddComponent<VrComfortPresenter>();
            comfort.Bind(this);
        }

        private Transform CreateHand(string handName)
        {
            var hand = VrObject.Create(handName).transform;
            hand.SetParent(transform, false);
            return hand;
        }

        [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
        private VrHandProxy CreateProxy(Transform target, string side, Material[] materials)
        {
            var mesh = VrAssets.Load<Mesh>("NeuroMitaVr" + side + "Hand");
            if (mesh == null) return null;
            var root = VrObject.Create("VR " + side + " Isolated Hand");
            var proxy = root.AddComponent<VrHandProxy>();
            proxy.Bind(target, mesh, materials, player.GetComponentsInChildren<Collider>(true), eye);
            return proxy;
        }

        private void Update()
        {
            if (eye == null || !VrRuntime.IsActive) return;
            ApplyPose();
            bool recenter = VrRuntime.Input.RecenterHeld;
            if (recenterHold.Sample(recenter, Time.unscaledDeltaTime)) Recenter();
            var settings = VrRuntime.Settings;
            if (lastSeated != (settings?.VrSeated == true))
            {
                lastSeated = settings?.VrSeated == true;
                Recenter();
            }
            var input = AppServices.GetOrNull<InputManager>();
            bool canTurn = player != null && !player.InputLocked && !player.LookLocked &&
                input != null && input.CurrentState == InputManager.InputState.Gameplay && Time.timeScale > 0f;
            float turnAxis = canTurn ? VrRuntime.Input.Turn.x : 0f;
            int turnMode = settings?.VrTurnMode ?? 0;
            if (turnMode != lastTurnMode)
            {
                snapTurn.Sample(0f, 45f);
                lastTurnMode = turnMode;
            }
            float turn = turnMode == 2 ? 0f : turnMode == 1
                ? VrTurnPolicy.SmoothDelta(turnAxis, settings?.VrSmoothTurnRateDegreesPerSecond ?? 90,
                    Time.unscaledDeltaTime, (settings?.VrTurnDeadzone ?? 15) * 0.01f)
                : snapTurn.Sample(turnAxis, settings?.VrSnapAngle ?? 45);
            if (turn != 0f)
            {
                var before = eye.transform.position;
                player.Camera.SetAngles(player.Camera.Yaw + turn, 0f);
                player.Camera.ApplyImmediate();
                ApplyPose();
                var correction = before - eye.transform.position;
                correction.y = 0f;
                if (player.Rigidbody != null) player.Rigidbody.position += correction;
                else player.transform.position += correction;
                ApplyPose();
            }
            UpdateHands();
        }

        private void LateUpdate() => ApplyPose();
        public void RefreshTrackingPose() => ApplyPose();

        private void FixedUpdate()
        {
            if (VrRuntime.Settings?.VrRoomscale == false) return;
            if (!calibrated || !HeadTracked || player == null || player.Rigidbody == null ||
                player.Rigidbody.isKinematic || player.MoveLocked || player.InputLocked || Time.timeScale <= 0f) return;
            var input = AppServices.GetOrNull<InputManager>();
            if (input == null || input.CurrentState != InputManager.InputState.Gameplay) return;
            Quaternion rotation = AnchorRotation * Quaternion.Inverse(referenceYaw);
            var offset = rawHeadPosition - reference;
            offset.y = 0f;
            var requested = Vector3.ClampMagnitude(rotation * offset, 2f * Time.fixedDeltaTime);
            var step = ClipRoomscaleStep(player.Rigidbody, requested);
            player.Rigidbody.position += step;
            reference += Quaternion.Inverse(rotation) * step;
        }

        private static Vector3 ClipRoomscaleStep(Rigidbody body, Vector3 requested)
        {
            float length = requested.magnitude;
            if (length < 0.001f) return Vector3.zero;
            if (body.SweepTest(requested / length, out var hit, length + 0.03f, QueryTriggerInteraction.Ignore))
                return requested.normalized * Mathf.Clamp(hit.distance - 0.03f, 0f, length);
            return requested;
        }

        public void Recenter()
        {
            calibrated = false;
            ApplyPose();
            VrRuntime.Presenter?.Recenter();
        }

        private static bool ReadPose(XRNode node, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            var device = InputDevices.GetDeviceAtXRNode(node);
#if NEUROMITA_VR_IL2CPP
            return VrNativeDeviceInput.ReadPose(device, out position, out rotation);
#else
            return device.isValid && device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
                device.TryGetFeatureValue(CommonUsages.devicePosition, out position) &&
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
#endif
        }

        private Quaternion AnchorRotation
        {
            get
            {
                if (player == null) return menuRotation;
                if (player.CameraRig != null && (player.Camera?.HasExternalOverride == true || player.Sequence?.IsCameraOverrideActive == true))
                    return Quaternion.Euler(0f, player.CameraRig.eulerAngles.y, 0f);
                return player.ViewRoot != null ? Quaternion.Euler(0f, player.ViewRoot.eulerAngles.y, 0f) : menuRotation;
            }
        }

        private void OnTrackingOriginUpdated(XRInputSubsystem subsystem) => calibrated = false;

        private Vector3 AnchorPosition
        {
            get
            {
                if (player == null || player.CameraRig == null) return menuAnchor;
                Vector3 position = player.CameraRig.TransformPoint(savedPosition);
                if (player.CrouchHeld && TrackedHeightDelta <= -0.2f && player.Sequence?.IsCameraOverrideActive != true)
                    position += player.transform.up * (player.CameraSettings.standEyeHeight - player.TargetEyeHeight);
                return position;
            }
        }

        private void ApplyPose()
        {
            if (eye == null || !VrRuntime.IsActive) return;
            eye.nearClipPlane = 0.02f;
            eye.orthographic = false;
            eye.stereoTargetEye = StereoTargetEyeMask.Both;
            eye.cullingMask |= uiLayerMask;
            TryCreateMenuStage();
            HeadTracked = ReadPose(XRNode.Head, out var headPosition, out var headRotation);
            if (!HeadTracked)
            {
                leftTracked = rightTracked = false;
                calibrated = false;
                RefreshItemMounts();
                UpdateHands();
                leftProxy?.SetTracked(false);
                rightProxy?.SetTracked(false);
                return;
            }
            ApplyTrackedHeadPose(headPosition, headRotation);
            if (menuPanelsPending)
            {
                menuPanelsPending = false;
                VrRuntime.Presenter?.Recenter();
                VrDiagnostics.Mark("menu panels recentered", "late menu stage");
            }
            Quaternion rotation = AnchorRotation * Quaternion.Inverse(referenceYaw);
            leftTracked = ApplyHand(leftHand, XRNode.LeftHand, rotation);
            rightTracked = ApplyHand(rightHand, XRNode.RightHand, rotation);
            leftProxy?.SetTracked(leftTracked);
            rightProxy?.SetTracked(rightTracked);
            RefreshItemMounts();
        }

        private bool menuPanelsPending;
        private float nextMenuStage;
        private void TryCreateMenuStage()
        {
            if (player != null || menuStage != null || Time.unscaledTime < nextMenuStage) return;
            nextMenuStage = Time.unscaledTime + 0.25f;
            menuStage = VrMenuStage.Create(eye);
            if (menuStage == null) return;
            menuPanelsPending = true;
            menuAnchor = eye.transform.position;
            menuRotation = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);
            calibrated = false;
        }
        private void ApplyTrackedHeadPose(Vector3 headPosition, Quaternion headRotation)
        {
            rawHeadPosition = headPosition;
            if (!calibrated)
            {
                reference = headPosition;
                referenceYaw = Quaternion.Euler(0f, headRotation.eulerAngles.y, 0f);
                calibrated = true;
            }
            Quaternion rotation = AnchorRotation * Quaternion.Inverse(referenceYaw);
            var position = AnchorPosition + rotation * VrPosePolicy.HeadOffset(headPosition, reference);
            position.y += (VrRuntime.Settings?.VrHeightOffset ?? 0) * 0.01f;
            eye.transform.SetPositionAndRotation(position, rotation * headRotation);
            menuStage?.HoldLights();
        }

        private bool ApplyHand(Transform target, XRNode node, Quaternion rotation)
        {
            bool tracked = ReadPose(node, out var position, out var poseRotation);
            if (tracked)
            {
                var controllerRotation = rotation * poseRotation;
                var handDirection = VrPosePolicy.PointerDirection(controllerRotation);
                if (node == XRNode.RightHand) pointerDirection = handDirection;
                else leftPointerDirection = handDirection;
                var handPosition = AnchorPosition + rotation * (position - reference);
                handPosition.y += (VrRuntime.Settings?.VrHeightOffset ?? 0) * 0.01f;
                var settings = VrRuntime.Settings;
                bool left = node == XRNode.LeftHand;
                var degrees = left
                    ? new Vector3(settings?.VrLeftWristPitch ?? 0, settings?.VrLeftWristYaw ?? 0, settings?.VrLeftWristRoll ?? 0)
                    : new Vector3(settings?.VrRightWristPitch ?? 0, settings?.VrRightWristYaw ?? 0, settings?.VrRightWristRoll ?? 0);
                var wrist = VrPosePolicy.CalibratedWrist(VrPosePolicy.WristRotation(controllerRotation, handDirection), degrees);
                handPosition += VrPosePolicy.WristOffset(wrist, new Vector3(settings?.VrWristSideOffset ?? 0,
                    settings?.VrWristHeightOffset ?? 0, settings?.VrWristForwardOffset ?? 0), left);
                target.SetPositionAndRotation(handPosition, wrist);
            }
            return tracked;
        }

        private void UpdateHands()
        {
            VrRuntime.PublishHandTargets(!UseIsolatedHands && leftTracked ? leftHand : null,
                !UseIsolatedHands && rightTracked ? rightHand : null);
        }

        private void RefreshItemMounts()
        {
            Transform leftTarget = UseIsolatedHands && leftProxy != null ? leftProxy.transform : leftHand;
            Transform rightTarget = UseIsolatedHands && rightProxy != null ? rightProxy.transform : rightHand;
            BindItemMount(ref leftMountBinding, player?.MountRig?.LeftHandMount?.ItemMount, leftTarget, leftTracked);
            BindItemMount(ref rightMountBinding, player?.MountRig?.RightHandMount?.ItemMount, rightTarget, rightTracked);
        }

        private static void BindItemMount(ref HandMountBinding binding, Transform mount, Transform hand, bool tracked)
        {
            if (binding != null && (!tracked || mount != binding.Mount || hand == null || binding.Mount == null || binding.Mount.parent != hand))
            {
                binding.Restore();
                binding = null;
            }
            if (binding == null && tracked && mount != null && hand != null && mount != hand && !hand.IsChildOf(mount))
                binding = new HandMountBinding(mount, hand);
        }
        public void Restore()
        {
            if (restored) return;
            restored = true;
            leftMountBinding?.Restore();
            rightMountBinding?.Restore();
            leftMountBinding = rightMountBinding = null;
            VrFrameHooks.BeforeRender -= ApplyPose;
            if (inputSubsystem != null) VrHostEvents.Tracking(inputSubsystem, OnTrackingOriginUpdated, false);
            inputSubsystem = null;
            menuStage?.Dispose();
            menuStage = null;
            if (eye != null)
            {
                eye.transform.localPosition = savedPosition;
                eye.transform.localRotation = savedRotation;
                eye.nearClipPlane = savedNear;
                eye.stereoTargetEye = savedStereo;
                eye.orthographic = savedOrthographic;
                eye.cullingMask = savedUiLayerVisible ? eye.cullingMask | uiLayerMask : eye.cullingMask & ~uiLayerMask;
            eye.ResetProjectionMatrix();
                eye.ResetStereoProjectionMatrices();
            }
            if (cameraAnimator != null) cameraAnimator.enabled = cameraAnimatorEnabled;
            cameraAnimator = null;
            if (comfort != null)
            {
                comfort.Clear();
                comfort.enabled = false;
                if (Application.isPlaying) Destroy(comfort);
                else DestroyImmediate(comfort);
                comfort = null;
            }
            player?.HandIK?.ClearTrackedTargets(leftHand, rightHand);
            poser?.ClearTrackedHandTargets(leftHand, rightHand);
            leftProxy?.SetTracked(false);
            rightProxy?.SetTracked(false);
            if (handPhysics != null)
            {
                handPhysics.Clear();
                if (Application.isPlaying) Destroy(handPhysics);
                else DestroyImmediate(handPhysics);
                handPhysics = null;
            }
            if (touchTracker != null)
            {
                touchTracker.Cancel();
                if (Application.isPlaying) Destroy(touchTracker);
                else DestroyImmediate(touchTracker);
                touchTracker = null;
            }
            if (clothContacts != null)
            {
                clothContacts.Clear();
                if (Application.isPlaying) Destroy(clothContacts);
                else DestroyImmediate(clothContacts);
                clothContacts = null;
            }
            if (leftProxy != null) { leftProxy.SetTracked(false); DestroyHand(leftProxy.transform); }
            if (rightProxy != null) { rightProxy.SetTracked(false); DestroyHand(rightProxy.transform); }
            leftProxy = rightProxy = null;
            if (leftHand != null) DestroyHand(leftHand);
            if (rightHand != null) DestroyHand(rightHand);
            leftHand = rightHand = null;
            eye = null;
        }

        private static void DestroyHand(Transform hand)
        {
            if (Application.isPlaying) Destroy(hand.gameObject);
            else DestroyImmediate(hand.gameObject);
        }

        private void OnDestroy() => Restore();
    }
}
