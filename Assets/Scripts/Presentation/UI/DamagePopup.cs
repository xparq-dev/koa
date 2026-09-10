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
        private Transform _coinVisual;
        private Material _coinMaterial;
        private bool _isGoldReward;

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

        public void SetupGold(int amount)
        {
            if (textMesh == null) Awake();

            _isGoldReward = true;
            lifetime = 1.15f;
            floatSpeed = 1.45f;
            _initialColor = new Color(1f, 0.82f, 0.18f);
            textMesh.text = $"+{amount}g";
            textMesh.fontSize = 30;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.characterSize = 0.075f;
            textMesh.color = _initialColor;
            _currentTimer = lifetime;
            CreateCoinVisual();
        }

        private void CreateCoinVisual()
        {
            GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            coin.name = "KOA_GoldCoin";
            coin.transform.SetParent(transform, false);
            coin.transform.localPosition = new Vector3(-0.52f, 0f, 0.04f);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            coin.transform.localScale = new Vector3(0.22f, 0.045f, 0.22f);
            Collider coinCollider = coin.GetComponent<Collider>();
            if (coinCollider != null) Destroy(coinCollider);

            Renderer coinRenderer = coin.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (coinRenderer != null && shader != null)
            {
                _coinMaterial = new Material(shader);
                Color gold = new Color(1f, 0.63f, 0.04f);
                _coinMaterial.color = gold;
                if (_coinMaterial.HasProperty("_BaseColor")) _coinMaterial.SetColor("_BaseColor", gold);
                if (_coinMaterial.HasProperty("_Metallic")) _coinMaterial.SetFloat("_Metallic", 0.72f);
                if (_coinMaterial.HasProperty("_Smoothness")) _coinMaterial.SetFloat("_Smoothness", 0.8f);
                coinRenderer.material = _coinMaterial;
            }
            _coinVisual = coin.transform;
        }

        private void LateUpdate()
        {
            if (_mainCamera != null)
            {
                transform.rotation = _mainCamera.transform.rotation;
            }

            // ลอยขึ้นเรื่อยๆ
            transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
            if (_coinVisual != null)
            {
                _coinVisual.Rotate(Vector3.up, 420f * Time.deltaTime, Space.Self);
                if (_isGoldReward)
                {
                    float progress = 1f - Mathf.Clamp01(_currentTimer / lifetime);
                    float popScale = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(progress * 5f));
                    _coinVisual.localScale = new Vector3(0.22f, 0.045f, 0.22f) * popScale;
                }
            }

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

        private void OnDestroy()
        {
            if (_coinMaterial != null) Destroy(_coinMaterial);
        }
    }
}
