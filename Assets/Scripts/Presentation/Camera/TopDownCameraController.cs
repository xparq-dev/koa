using UnityEngine;

namespace KOA.Presentation.Camera
{
    /// <summary>
    /// ระบบกล้อง Top-down Isometric 50 องศา ตาม Section 7.1
    /// รองรับ Zoom 8-14m และ Soft-lerp พร้อม Look-ahead
    /// </summary>
    public class TopDownCameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private float smoothSpeed = 5.0f;
        [SerializeField] private float lookAheadFactor = 1.2f;

        [Header("Camera Isometric Angle (Section 7.1)")]
        [SerializeField] private float pitchAngle = 50.0f; // มุม 50 องศาคงที่
        [SerializeField] private float yawAngle = 0.0f;

        [Header("Zoom Settings (Section 7.1: 8 - 14 meters)")]
        [SerializeField] private float currentDistance = 11.0f;
        [SerializeField] private float minDistance = 8.0f;
        [SerializeField] private float maxDistance = 14.0f;
        [SerializeField] private float zoomSpeed = 4.0f;

        private Vector3 _lastTargetPos;
        private Vector3 _targetVelocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _lastTargetPos = target.position;
            }
        }

        private void Start()
        {
            if (target != null)
            {
                _lastTargetPos = target.position;
            }
        }

        private void Update()
        {
            HandleZoomInput();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // คำนวณความเร็วเป้าหมายเพื่อทำ Look-ahead เล็กน้อย
            if (Time.deltaTime > 0f)
            {
                Vector3 currentVelocity = (target.position - _lastTargetPos) / Time.deltaTime;
                _targetVelocity = Vector3.Lerp(_targetVelocity, currentVelocity, Time.deltaTime * 5f);
                _lastTargetPos = target.position;
            }

            Vector3 focusPoint = target.position + (_targetVelocity.normalized * Mathf.Min(_targetVelocity.magnitude * 0.15f, lookAheadFactor));

            // คำนวณตำแหน่งกล้องตามมุม Pitch 50 องศา และระยะ Distance
            Quaternion rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 offset = rotation * new Vector3(0, 0, -currentDistance);
            Vector3 desiredPosition = focusPoint + offset;

            // Soft-lerp การเคลื่อนที่ของกล้อง
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
            transform.rotation = rotation;
        }

        private void HandleZoomInput()
        {
            float scroll = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentDistance = Mathf.Clamp(currentDistance - (scroll * zoomSpeed * 5f), minDistance, maxDistance);
            }
        }
    }
}
