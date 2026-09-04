using KOA.Core.Input;
using KOA.Data.Enums;
using UnityEngine;

namespace KOA.Presentation.Input
{
    /// <summary>
    /// PC Input Adapter ตาม Section 1.1 และ Section 7.2
    /// แปลงเมาส์และคีย์บอร์ด PC ให้เป็น InputFrame สำหรับ Simulation Core
    /// </summary>
    public class PCInputAdapter : MonoBehaviour, IInputAdapter
    {
        [Header("Raycast Settings")]
        [SerializeField] private LayerMask groundLayerMask = ~0; // ทุก Layer หรือเฉพาะ Ground Layer
        [SerializeField] private UnityEngine.Camera targetCamera;

        private InputFrame _currentInput;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            UpdateMouseAim();
            HandleMovementInput();
            HandleActionInput();
        }

        private void UpdateMouseAim()
        {
            if (targetCamera == null) return;

            Ray ray = targetCamera.ScreenPointToRay(UnityEngine.Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float enter))
            {
                _currentInput.AimVector = ray.GetPoint(enter);
            }
            else if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayerMask))
            {
                _currentInput.AimVector = hit.point;
            }
        }

        private void HandleMovementInput()
        {
            // คลิกขวาเดิน (Click-to-move ตาม Section 7.2)
            if (UnityEngine.Input.GetMouseButton(1))
            {
                _currentInput.HasMoveTarget = true;
                _currentInput.TargetDestination = _currentInput.AimVector;
            }
        }

        private void HandleActionInput()
        {
            // คลิกซ้าย หรือ A เพื่อโจมตี (Section 7.2)
            if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetKeyDown(KeyCode.A))
            {
                _currentInput.CastIntent = CastIntent.CastAttack;
            }
            // ปุ่ม Q สำหรับ Skill 1 (Iron Cleave)
            else if (UnityEngine.Input.GetKeyDown(KeyCode.Q))
            {
                _currentInput.CastIntent = CastIntent.CastSkill1;
            }
            // ปุ่ม W สำหรับ Skill 2
            else if (UnityEngine.Input.GetKeyDown(KeyCode.W))
            {
                _currentInput.CastIntent = CastIntent.CastSkill2;
            }
            // ปุ่ม E สำหรับ Ultimate
            else if (UnityEngine.Input.GetKeyDown(KeyCode.E))
            {
                _currentInput.CastIntent = CastIntent.CastUltimate;
            }
        }

        public InputFrame GetCurrentInput()
        {
            return _currentInput;
        }

        public void ConsumeIntent()
        {
            _currentInput.CastIntent = CastIntent.None;
            _currentInput.HasMoveTarget = false;
        }
    }
}
