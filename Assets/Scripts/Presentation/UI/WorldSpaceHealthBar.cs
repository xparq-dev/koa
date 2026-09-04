using UnityEngine;
using UnityEngine.UI;

namespace KOA.Presentation.UI
{
    /// <summary>
    /// World-space Health Bar เหนือหัวตัวละครตาม Section 8
    /// หันหน้าเข้าหากล้องเสมอ (Billboard effect) และปรับขนาดตามค่า HP
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

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
        }

        public void BindTarget(Transform target)
        {
            _targetTransform = target;
        }

        private void LateUpdate()
        {
            if (_targetTransform != null)
            {
                transform.position = _targetTransform.position + offset;
            }

            if (_mainCamera != null)
            {
                // Billboard หันหน้าเข้าหากล้อง
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
                Vector3 scale = fillScaleBar.localScale;
                scale.x = fillRatio;
                fillScaleBar.localScale = scale;
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
