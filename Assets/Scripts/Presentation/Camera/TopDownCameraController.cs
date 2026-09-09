using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KOA.Presentation.Camera
{
    /// <summary>
    /// ระบบกล้อง Top-down Perspective ตาม Section 7.1
    /// รองรับ Zoom 18-32m, Free Camera, Edge Pan และ Focus/Lock
    /// รองรับทั้ง New Input System และ Legacy Input Manager
    /// </summary>
    public class TopDownCameraController : MonoBehaviour
    {
        public const float DefaultPitchAngle = 60.0f;
        public const float DefaultPresentationYaw = -45.0f;
        public const float DefaultFieldOfView = 46.0f;
        public const float DefaultCameraDistance = 24.0f;
        public const float MinimumCameraDistance = 18.0f;
        public const float MaximumCameraDistance = 32.0f;

        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5.0f;
        [SerializeField] private float lookAheadFactor = 1.2f;

        [Header("Camera Isometric Angle (Section 7.1)")]
        [SerializeField] private float pitchAngle = DefaultPitchAngle;
        [SerializeField] private float yawAngle = DefaultPresentationYaw;
        [SerializeField] private float perspectiveFieldOfView = DefaultFieldOfView;

        [Header("Zoom Settings (Section 7.1: 18 - 32 meters)")]
        [SerializeField] private float currentDistance = DefaultCameraDistance;
        [SerializeField] private float minDistance = MinimumCameraDistance;
        [SerializeField] private float maxDistance = MaximumCameraDistance;
        [SerializeField] private float zoomSpeed = 4.0f;

        [Header("Free Camera Controls")]
        [SerializeField] private bool startPermanentlyLocked;
        [SerializeField] private float edgePanBorderPixels = 14.0f;
        [SerializeField] private float edgePanSpeed = 17.0f;
        [SerializeField] private Vector2 horizontalFocusBounds = new Vector2(-11.5f, 11.5f);
        [SerializeField] private Vector2 laneFocusBounds = new Vector2(-62.0f, 62.0f);

        [Header("Focus Controls")]
        [SerializeField] private float holdThresholdSeconds = 0.22f;
        [SerializeField] private float repeatedTapWindowSeconds = 0.55f;

        [Header("Depth Precision")]
        [SerializeField] private float nearClipPlane = 0.2f;
        [SerializeField] private float farClipPlane = 180.0f;

        private Vector3 _lastTargetPos;
        private Vector3 _targetVelocity;
        private Vector3 _cameraFocusPoint;
        private readonly List<Transform> _focusTargets = new List<Transform>();
        private int _focusTargetIndex;
        private bool _hasFocusPoint;
        private bool _persistentLock;
        private bool _spaceHeld;
        private float _spacePressedAt;
        private float _lastQuickTapAt = -10f;

        public bool IsLocked => _persistentLock || _spaceHeld;
        public bool IsPermanentlyLocked => _persistentLock;

        public void UseDefaultPresentationHeading()
        {
            pitchAngle = DefaultPitchAngle;
            yawAngle = DefaultPresentationYaw;
            perspectiveFieldOfView = DefaultFieldOfView;
            minDistance = MinimumCameraDistance;
            maxDistance = MaximumCameraDistance;
            currentDistance = DefaultCameraDistance;
            ApplyLensSettings();
            if (_hasFocusPoint) ApplyCameraTransform(true);
        }

        public void TogglePersistentLock()
        {
            _persistentLock = !_persistentLock;
            if (_persistentLock) FocusPrimaryTarget();
        }

        public void PanToWorldPosition(Vector3 worldPosition)
        {
            _persistentLock = false;
            _spaceHeld = false;
            _cameraFocusPoint = worldPosition;
            _cameraFocusPoint.y = 0f;
            _hasFocusPoint = true;
            ClampFocusPoint();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _focusTargets.Clear();
            _focusTargetIndex = 0;
            if (target != null)
            {
                _focusTargets.Add(target);
                _lastTargetPos = target.position;
                FocusOnTarget();
            }
        }

        /// <summary>
        /// Future-ready hook for team modes. Repeated quick Space taps cycle registered focus targets.
        /// </summary>
        public void RegisterFocusTarget(Transform focusTarget)
        {
            if (focusTarget != null && !_focusTargets.Contains(focusTarget))
                _focusTargets.Add(focusTarget);
        }

        private void Start()
        {
            ApplyLensSettings();

            _persistentLock = startPermanentlyLocked;
            if (target != null)
            {
                _lastTargetPos = target.position;
                FocusOnTarget();
            }

            ApplyCameraTransform(true);
        }

        private void ApplyLensSettings()
        {
            UnityEngine.Camera controlledCamera = GetComponent<UnityEngine.Camera>();
            if (controlledCamera == null) return;
            controlledCamera.nearClipPlane = nearClipPlane;
            controlledCamera.farClipPlane = farClipPlane;
            controlledCamera.fieldOfView = perspectiveFieldOfView;
        }

        private void Update()
        {
            HandleZoomInput();
            HandleFocusInput();
            if (!IsLocked)
                HandleEdgePanInput();
        }

        private void LateUpdate()
        {
            if (IsLocked && target != null)
            {
                if (Time.deltaTime > 0f)
                {
                    Vector3 currentVelocity = (target.position - _lastTargetPos) / Time.deltaTime;
                    _targetVelocity = Vector3.Lerp(_targetVelocity, currentVelocity, Time.deltaTime * 5f);
                    _lastTargetPos = target.position;
                }

                Vector3 lookAhead = _targetVelocity.normalized * Mathf.Min(_targetVelocity.magnitude * 0.15f, lookAheadFactor);
                Vector3 desiredFocus = target.position + lookAhead;
                float followBlend = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
                _cameraFocusPoint = Vector3.Lerp(_cameraFocusPoint, desiredFocus, followBlend);
            }

            ClampFocusPoint();
            ApplyCameraTransform(false);
        }

        private void HandleFocusInput()
        {
            bool spaceDown;
            bool spacePressed;
            bool spaceUp;
            bool toggleLockDown;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                spaceDown = Keyboard.current.spaceKey.wasPressedThisFrame;
                spacePressed = Keyboard.current.spaceKey.isPressed;
                spaceUp = Keyboard.current.spaceKey.wasReleasedThisFrame;
                toggleLockDown = Keyboard.current.yKey.wasPressedThisFrame;
            }
            else
#endif
            {
                spaceDown = UnityEngine.Input.GetKeyDown(KeyCode.Space);
                spacePressed = UnityEngine.Input.GetKey(KeyCode.Space);
                spaceUp = UnityEngine.Input.GetKeyUp(KeyCode.Space);
                toggleLockDown = UnityEngine.Input.GetKeyDown(KeyCode.Y);
            }

            if (toggleLockDown)
            {
                TogglePersistentLock();
            }

            if (spaceDown)
            {
                _spacePressedAt = Time.unscaledTime;
                _spaceHeld = true;
                FocusOnTarget();
            }
            else if (spacePressed)
            {
                _spaceHeld = true;
            }

            if (spaceUp)
            {
                float heldDuration = Time.unscaledTime - _spacePressedAt;
                _spaceHeld = false;
                if (heldDuration <= holdThresholdSeconds)
                {
                    CycleFocusTargetForQuickTap();
                    FocusOnTarget();
                }
            }
        }

        private void HandleEdgePanInput()
        {
            if (!Application.isFocused) return;

            Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                mousePosition = Mouse.current.position.ReadValue();
            else
#endif
                mousePosition = UnityEngine.Input.mousePosition;

            Vector2 screenDirection = Vector2.zero;
            if (mousePosition.x <= edgePanBorderPixels) screenDirection.x -= 1f;
            else if (mousePosition.x >= Screen.width - edgePanBorderPixels) screenDirection.x += 1f;
            if (mousePosition.y <= edgePanBorderPixels) screenDirection.y -= 1f;
            else if (mousePosition.y >= Screen.height - edgePanBorderPixels) screenDirection.y += 1f;

            if (screenDirection.sqrMagnitude > 1f) screenDirection.Normalize();
            if (screenDirection.sqrMagnitude > 0f)
            {
                float distanceScale = Mathf.Lerp(0.82f, 1.18f, Mathf.InverseLerp(minDistance, maxDistance, currentDistance));
                Quaternion heading = Quaternion.Euler(0f, yawAngle, 0f);
                Vector3 worldDirection = heading * new Vector3(screenDirection.x, 0f, screenDirection.y);
                _cameraFocusPoint += worldDirection * (edgePanSpeed * distanceScale * Time.unscaledDeltaTime);
                ClampFocusPoint();
            }
        }

        private void CycleFocusTargetForQuickTap()
        {
            _focusTargets.RemoveAll(item => item == null);
            if (_focusTargets.Count == 0) return;

            if (Time.unscaledTime - _lastQuickTapAt <= repeatedTapWindowSeconds && _focusTargets.Count > 1)
                _focusTargetIndex = (_focusTargetIndex + 1) % _focusTargets.Count;
            else
                _focusTargetIndex = 0;

            target = _focusTargets[_focusTargetIndex];
            _lastQuickTapAt = Time.unscaledTime;
        }

        private void FocusPrimaryTarget()
        {
            _focusTargets.RemoveAll(item => item == null);
            if (_focusTargets.Count > 0)
            {
                _focusTargetIndex = 0;
                target = _focusTargets[0];
            }
            FocusOnTarget();
        }

        private void FocusOnTarget()
        {
            if (target == null) return;
            _cameraFocusPoint = target.position;
            _lastTargetPos = target.position;
            _targetVelocity = Vector3.zero;
            _hasFocusPoint = true;
            ClampFocusPoint();
        }

        private void ClampFocusPoint()
        {
            if (!_hasFocusPoint) return;
            _cameraFocusPoint.x = Mathf.Clamp(_cameraFocusPoint.x, horizontalFocusBounds.x, horizontalFocusBounds.y);
            _cameraFocusPoint.y = Mathf.Clamp(_cameraFocusPoint.y, 0f, 2f);
            _cameraFocusPoint.z = Mathf.Clamp(_cameraFocusPoint.z, laneFocusBounds.x, laneFocusBounds.y);
        }

        private void ApplyCameraTransform(bool snap)
        {
            if (!_hasFocusPoint) return;

            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);
            Vector3 desiredPosition = _cameraFocusPoint + offset;

            float blend = snap ? 1f : 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
            transform.rotation = rotation;
        }

        private void HandleZoomInput()
        {
            float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float scrollY = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollY) > 0.01f)
                {
                    scroll = Mathf.Sign(scrollY) * 0.2f;
                }
            }
            else
#endif
            {
                scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            }

            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance = Mathf.Clamp(currentDistance - (scroll * zoomSpeed * 5f), minDistance, maxDistance);
            }
        }
    }
}
