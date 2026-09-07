using KOA.Core.Structures;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับป้อมปราการและ Nexus (Section 3.3)
    /// จัดการโมเดล, ลำแสงเลเซอร์ Heating Laser, และแถบเลือด
    /// </summary>
    public class TowerView : MonoBehaviour
    {
        [Header("UI & Visuals")]
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private LineRenderer laserLineRenderer;
        [SerializeField] private Renderer towerMeshRenderer;

        [Header("Colors")]
        [SerializeField] private Color normalLaserColor = new Color(1f, 0.8f, 0.2f);
        [SerializeField] private Color heatedLaserColor = new Color(1f, 0.15f, 0.1f);

        public TowerEntity Logic { get; private set; }

        private float _laserTimer = 0f;
        private const float LaserDuration = 0.15f;

        public void BindLogic(TowerEntity towerEntity)
        {
            Logic = towerEntity;

            if (laserLineRenderer == null)
            {
                laserLineRenderer = GetComponentInChildren<LineRenderer>();
            }

            if (towerMeshRenderer == null)
            {
                towerMeshRenderer = GetComponent<Renderer>();
            }

            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.Stats.MaxHp);
            }

            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnAttackFired += HandleAttackFired;
            Logic.OnDestroyed += HandleDestroyed;
        }

        public void SetHealthBar(WorldSpaceHealthBar bar)
        {
            healthBar = bar;
            if (healthBar != null && Logic != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.Stats.MaxHp);
            }
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnAttackFired -= HandleAttackFired;
                Logic.OnDestroyed -= HandleDestroyed;
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
        }

        private void Update()
        {
            if (_laserTimer > 0f)
            {
                _laserTimer -= Time.deltaTime;
                if (_laserTimer <= 0f && laserLineRenderer != null)
                {
                    laserLineRenderer.enabled = false;
                }
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleAttackFired(Vector3 targetPos, float damage, bool isHeated)
        {
            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = true;
                Vector3 origin = transform.position + Vector3.up * 4.0f; // ยิงจากยอดป้อม
                laserLineRenderer.SetPosition(0, origin);
                laserLineRenderer.SetPosition(1, targetPos + Vector3.up * 1.0f);

                Color c = isHeated ? heatedLaserColor : normalLaserColor;
                laserLineRenderer.startColor = c;
                laserLineRenderer.endColor = c;
                _laserTimer = LaserDuration;
            }
        }

        private void HandleDestroyed()
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }

            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = false;
            }

            // 1. ปิด Collider ทันที เพื่อไม่ให้ตัวละครหรือครีบเดินชน และไม่สามารถคลิกเป็นเป้าหมายได้อีก
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // 2. ปรับ Visual ให้กลายเป็นฐานซากปรักหักพัง (Destroyed Tower Ruins) สไตล์เกม MOBA
            if (towerMeshRenderer == null)
            {
                towerMeshRenderer = GetComponent<Renderer>();
            }

            if (towerMeshRenderer != null)
            {
                towerMeshRenderer.material.color = new Color(0.18f, 0.18f, 0.18f, 0.9f); // สีหินไหม้เกรียม
            }

            // ยุบความสูงของป้อมลงเหลือเพียงแท่นหินเตี้ยๆ ติดพื้น (ความสูง 0.22 เมตร)
            Vector3 ruinedScale = transform.localScale;
            ruinedScale.y = 0.22f;
            ruinedScale.x *= 1.05f;
            ruinedScale.z *= 1.05f;
            transform.localScale = ruinedScale;

            // วางแท่นหินแนบพื้นพอดี
            if (Logic != null)
            {
                transform.position = new Vector3(Logic.Position.x, 0.11f, Logic.Position.z);
            }
        }
    }
}
