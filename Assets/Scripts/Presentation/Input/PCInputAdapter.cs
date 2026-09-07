using KOA.Core.Input;
using KOA.Data.Enums;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KOA.Presentation.Input
{
    /// <summary>
    /// PC Input Adapter ตาม Section 1.1 และ Section 7.2
    /// แปลงเมาส์และคีย์บอร์ด PC ให้เป็น InputFrame สำหรับ Simulation Core
    /// รองรับทั้ง New Input System และ Legacy Input Manager อัตโนมัติ
    /// </summary>
    public class PCInputAdapter : MonoBehaviour, IInputAdapter
    {
        [Header("Raycast Settings")]
        [SerializeField] private LayerMask groundLayerMask = ~0;
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

            Vector3 mouseScreenPos = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mPos = Mouse.current.position.ReadValue();
                mouseScreenPos = new Vector3(mPos.x, mPos.y, 0f);
            }
            else
#endif
            {
                mouseScreenPos = UnityEngine.Input.mousePosition;
            }

            Ray ray = targetCamera.ScreenPointToRay(mouseScreenPos);
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
            bool isRightClick = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isRightClick = Mouse.current.rightButton.isPressed;
            }
            else
#endif
            {
                isRightClick = UnityEngine.Input.GetMouseButton(1);
            }

            if (isRightClick)
            {
                _currentInput.HasMoveTarget = true;
                _currentInput.TargetDestination = _currentInput.AimVector;
            }
        }

        private void HandleActionInput()
        {
            bool attackPressed = false;
            bool qPressed = false;
            bool wPressed = false;
            bool ePressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Keyboard.current != null)
            {
                attackPressed = Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame;
                qPressed = Keyboard.current.qKey.wasPressedThisFrame;
                wPressed = Keyboard.current.wKey.wasPressedThisFrame;
                ePressed = Keyboard.current.eKey.wasPressedThisFrame;

                if (Keyboard.current.digit1Key.wasPressedThisFrame) ActiveItemSlotToUse = 0;
                else if (Keyboard.current.digit2Key.wasPressedThisFrame) ActiveItemSlotToUse = 1;
                else if (Keyboard.current.digit3Key.wasPressedThisFrame) ActiveItemSlotToUse = 2;
                else if (Keyboard.current.digit4Key.wasPressedThisFrame) ActiveItemSlotToUse = 3;
                else if (Keyboard.current.digit5Key.wasPressedThisFrame) ActiveItemSlotToUse = 4;
                else if (Keyboard.current.digit6Key.wasPressedThisFrame) ActiveItemSlotToUse = 5;
            }
            else
#endif
            {
                attackPressed = UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetKeyDown(KeyCode.A);
                qPressed = UnityEngine.Input.GetKeyDown(KeyCode.Q);
                wPressed = UnityEngine.Input.GetKeyDown(KeyCode.W);
                ePressed = UnityEngine.Input.GetKeyDown(KeyCode.E);

                for (int i = 0; i < 6; i++)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        ActiveItemSlotToUse = i;
                        break;
                    }
                }
            }

            if (attackPressed)
            {
                _currentInput.CastIntent = CastIntent.CastAttack;
            }
            else if (qPressed)
            {
                _currentInput.CastIntent = CastIntent.CastSkill1;
            }
            else if (wPressed)
            {
                _currentInput.CastIntent = CastIntent.CastSkill2;
            }
            else if (ePressed)
            {
                _currentInput.CastIntent = CastIntent.CastUltimate;
            }
        }

        public int ActiveItemSlotToUse { get; private set; } = -1;

        public void ConsumeActiveItemSlot()
        {
            ActiveItemSlotToUse = -1;
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
