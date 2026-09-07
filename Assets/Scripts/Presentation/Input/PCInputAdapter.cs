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
    /// พร้อม Input Latches ป้องกัน Frame Drop จาก Fixed Simulation Rate
    /// </summary>
    public class PCInputAdapter : MonoBehaviour, IInputAdapter
    {
        [Header("Raycast Settings")]
        [SerializeField] private LayerMask groundLayerMask = ~0;
        [SerializeField] private UnityEngine.Camera targetCamera;

        public Ray CurrentRay { get; private set; }
        public Collider HoveredCollider { get; private set; }
        public Vector3 HoveredPoint { get; private set; }
        public bool LeftClickDown { get; private set; }
        public bool RightClickDown { get; private set; }

        private InputFrame _currentInput;

        // Latches for reliable multi-frame simulation ticks
        private bool _leftClickPending;
        private bool _rightClickPending;
        private Collider _leftClickedCollider;
        private Vector3 _leftClickedPoint;
        private Collider _rightClickedCollider;
        private Vector3 _rightClickedPoint;

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
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
                if (targetCamera == null) return;
            }

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
            CurrentRay = ray;

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                HoveredCollider = hit.collider;
                HoveredPoint = hit.point;
            }
            else
            {
                HoveredCollider = null;
            }

            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            if (groundPlane.Raycast(ray, out float enter))
            {
                _currentInput.AimVector = ray.GetPoint(enter);
            }
            else if (HoveredCollider != null)
            {
                _currentInput.AimVector = HoveredPoint;
            }
        }

        private void HandleMovementInput()
        {
            // คลิกขวาเดิน (Click-to-move ตาม Section 7.2) หรือสั่งโจมตี
            bool isRightDown = false;
            bool isRightHeld = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                isRightDown = Mouse.current.rightButton.wasPressedThisFrame;
                isRightHeld = Mouse.current.rightButton.isPressed;
            }
            else
#endif
            {
                isRightDown = UnityEngine.Input.GetMouseButtonDown(1);
                isRightHeld = UnityEngine.Input.GetMouseButton(1);
            }

            RightClickDown = isRightDown;

            if (isRightDown)
            {
                _rightClickPending = true;
                _rightClickedCollider = HoveredCollider;
                _rightClickedPoint = HoveredPoint != Vector3.zero ? HoveredPoint : _currentInput.AimVector;
            }

            if (isRightDown || isRightHeld)
            {
                _currentInput.HasMoveTarget = true;
                _currentInput.TargetDestination = _currentInput.AimVector;
            }
        }

        private void HandleActionInput()
        {
            bool leftDown = false;
            bool aPressed = false;
            bool qPressed = false;
            bool wPressed = false;
            bool ePressed = false;
            bool rPressed = false;
            bool isCtrlHeld = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Keyboard.current != null)
            {
                leftDown = Mouse.current.leftButton.wasPressedThisFrame;
                isCtrlHeld = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                aPressed = Keyboard.current.aKey.wasPressedThisFrame;
                qPressed = Keyboard.current.qKey.wasPressedThisFrame;
                wPressed = Keyboard.current.wKey.wasPressedThisFrame;
                ePressed = Keyboard.current.eKey.wasPressedThisFrame;
                rPressed = Keyboard.current.rKey.wasPressedThisFrame;

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
                leftDown = UnityEngine.Input.GetMouseButtonDown(0);
                isCtrlHeld = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                aPressed = UnityEngine.Input.GetKeyDown(KeyCode.A);
                qPressed = UnityEngine.Input.GetKeyDown(KeyCode.Q);
                wPressed = UnityEngine.Input.GetKeyDown(KeyCode.W);
                ePressed = UnityEngine.Input.GetKeyDown(KeyCode.E);
                rPressed = UnityEngine.Input.GetKeyDown(KeyCode.R);

                for (int i = 0; i < 6; i++)
                {
                    if (UnityEngine.Input.GetKeyDown(KeyCode.Alpha1 + i))
                    {
                        ActiveItemSlotToUse = i;
                        break;
                    }
                }
            }

            LeftClickDown = leftDown;

            if (leftDown)
            {
                _leftClickPending = true;
                _leftClickedCollider = HoveredCollider;
                _leftClickedPoint = HoveredPoint != Vector3.zero ? HoveredPoint : _currentInput.AimVector;
            }

            // ถ้ากด Ctrl ค้างอยู่ (กำลังเลเวลอัปสกิล) จะไม่สั่งร่ายสกิล
            if (!isCtrlHeld)
            {
                if (aPressed)
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
                    _currentInput.CastIntent = CastIntent.CastSkill3;
                }
                else if (rPressed)
                {
                    _currentInput.CastIntent = CastIntent.CastUltimate;
                }
            }
        }

        public bool ConsumeLeftClick(out Collider clickedCollider, out Vector3 clickedPoint)
        {
            clickedCollider = _leftClickedCollider;
            clickedPoint = _leftClickedPoint;
            bool wasPending = _leftClickPending;
            _leftClickPending = false;
            return wasPending;
        }

        public bool ConsumeRightClick(out Collider clickedCollider, out Vector3 clickedPoint)
        {
            clickedCollider = _rightClickedCollider;
            clickedPoint = _rightClickedPoint;
            bool wasPending = _rightClickPending;
            _rightClickPending = false;
            return wasPending;
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
