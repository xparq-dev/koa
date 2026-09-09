using UnityEngine;
using UnityEngine.UI;

namespace KOA.Presentation.UI
{
    /// <summary>
    /// World-space Health Bar เหนือหัวตัวละครตาม Section 8
    /// หันหน้าเข้าหากล้องเสมอ (Billboard effect) และปรับขนาดตามค่า HP
    /// ทำลายตัวเองอัตโนมัติเมื่อเป้าหมาย (Unit) ถูกทำลาย เพื่อไม่ให้มีแถบค้างบนแผนที่
    /// </summary>
    public class WorldSpaceHealthBar : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image manaFillImage;
        [SerializeField] private Transform fillScaleBar; // สำหรับกรณีไม่ใช้ UI Image แต่ใช้ 3D Quad/Transform scale

        [Header("Billboard Settings")]
        [SerializeField] private Vector3 offset = new Vector3(0, 2.3f, 0);

        private Transform _targetTransform;
        private UnityEngine.Camera _mainCamera;
        private float _initialScaleX = 1.36f;
        private Vector3 _initialLocalPos = new Vector3(0, 0, -0.01f);

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        public void BindTarget(Transform target)
        {
            _targetTransform = target;
        }

        public void SetOffset(Vector3 worldOffset)
        {
            offset = worldOffset;
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetupScaleBar(Transform fillBarTransform)
        {
            fillScaleBar = fillBarTransform;
            if (fillScaleBar != null)
            {
                _initialScaleX = fillScaleBar.localScale.x > 0.01f ? fillScaleBar.localScale.x : 1.36f;
                _initialLocalPos = fillScaleBar.localPosition;
            }
        }

        private void LateUpdate()
        {
            // หากเป้าหมายตายหรือถูกทำลาย (Destroyed) ให้ทำลายหลอดเลือดตามไปด้วย ไม่ให้ค้างในฉาก
            if (_targetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = _targetTransform.position + offset;

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
            }

            if (_mainCamera != null)
            {
                // Billboard หันหน้าเข้าหากล้องเสมอ
                transform.rotation = _mainCamera.transform.rotation;
            }
        }

        public void SetHealth(float currentHp, float maxHp)
        {
            float fillRatio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = fillRatio;
            }

            if (fillScaleBar != null)
            {
                if (fillRatio <= 0.001f)
                {
                    fillScaleBar.gameObject.SetActive(false);
                }
                else
                {
                    fillScaleBar.gameObject.SetActive(true);
                    Vector3 scale = fillScaleBar.localScale;
                    scale.x = _initialScaleX * fillRatio;
                    fillScaleBar.localScale = scale;

                    // เลื่อน localPosition ให้แถบเลือดหดจากขวาไปซ้ายอย่างเป็นธรรมชาติ
                    float offsetLeft = -(_initialScaleX * (1f - fillRatio) * 0.5f);
                    fillScaleBar.localPosition = new Vector3(_initialLocalPos.x + offsetLeft, _initialLocalPos.y, _initialLocalPos.z);
                }
            }
        }

        public void SetMana(float currentMana, float maxMana)
        {
            if (manaFillImage != null)
            {
                float fillRatio = maxMana > 0f ? Mathf.Clamp01(currentMana / maxMana) : 0f;
                manaFillImage.fillAmount = fillRatio;
            }
        }
    }
}
