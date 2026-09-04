using KOA.Data.Enums;
using UnityEngine;

namespace KOA.Presentation.UI
{
    /// <summary>
    /// ตัวเลข Damage Popup ลอยขึ้นเมื่อโดนตี/โดนสกิลตาม Section 8
    /// แยกสีตาม DamageType และหันหน้าเข้าหากล้องเสมอ
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TextMesh textMesh;
        [SerializeField] private float floatSpeed = 2.0f;
        [SerializeField] private float lifetime = 0.8f;

        private float _currentTimer = 0f;
        private Color _initialColor;
        private UnityEngine.Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = UnityEngine.Camera.main;
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMesh>();
                if (textMesh == null)
                {
                    textMesh = gameObject.AddComponent<TextMesh>();
                    textMesh.alignment = TextAlignment.Center;
                    textMesh.anchor = TextAnchor.MiddleCenter;
                    textMesh.fontSize = 24;
                    textMesh.characterSize = 0.08f;
                }
            }
        }

        public void Setup(float damageAmount, DamageType damageType)
        {
            if (textMesh == null) Awake();

            textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

            // แยกสีตาม Section 8
            switch (damageType)
            {
                case DamageType.Physical:
                    _initialColor = new Color(1.0f, 0.45f, 0.1f); // ส้ม/แดง
                    break;
                case DamageType.Magic:
                    _initialColor = new Color(0.3f, 0.7f, 1.0f); // ฟ้าสดใส
                    break;
                case DamageType.TrueDamage:
                    _initialColor = Color.white;
                    break;
                default:
                    _initialColor = Color.yellow;
                    break;
            }

            textMesh.color = _initialColor;
            _currentTimer = lifetime;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }

            // ลอยขึ้นเรื่อยๆ
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);

            // ค่อยๆ จางลง
            _currentTimer -= Time.deltaTime;
            if (_currentTimer > 0f)
            {
                float alpha = _currentTimer / lifetime;
                Color c = _initialColor;
                c.a = alpha;
                textMesh.color = c;
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
