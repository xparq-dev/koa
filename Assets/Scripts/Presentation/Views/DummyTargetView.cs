using KOA.Core.Entities;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับ Dummy Target (Section 1.1 Decoupled Core)
    /// รับ Event จาก DummyTarget logic เพื่ออัปเดตการแสดงผลและแถบ HP
    /// </summary>
    public class DummyTargetView : MonoBehaviour
    {
        [Header("UI & Visuals")]
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private Renderer meshRenderer;
        [SerializeField] private Color normalColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color deadColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public DummyTarget Logic { get; private set; }

        private void Awake()
        {
            // สร้าง Core Logic instance ที่ตำแหน่งปัจจุบัน
            Logic = new DummyTarget("dummy_01", transform.position);

            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<Renderer>();
            }

            if (meshRenderer != null)
            {
                meshRenderer.material.color = normalColor;
            }

            // Bind กับ Health Bar
            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.MaxHp);
            }

            // สมัคร Events
            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnDamageTaken += HandleDamageTaken;
            Logic.OnDied += HandleDied;
            Logic.OnRespawned += HandleRespawned;
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnDamageTaken -= HandleDamageTaken;
                Logic.OnDied -= HandleDied;
                Logic.OnRespawned -= HandleRespawned;
            }
        }

        private void Update()
        {
            // ส่ง DeltaTime เข้า Simulation Core Tick (30 FPS Simulation)
            Logic?.SimulationTick(Time.deltaTime);
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleDamageTaken(float damageAmount, KOA.Data.Enums.DamageType damageType)
        {
            if (DamagePopupManager.Instance != null)
            {
                DamagePopupManager.Instance.ShowDamage(transform.position, damageAmount, damageType);
            }
        }

        private void HandleDied()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = deadColor;
            }
        }

        private void HandleRespawned()
        {
            if (meshRenderer != null)
            {
                meshRenderer.material.color = normalColor;
            }
        }
    }
}
