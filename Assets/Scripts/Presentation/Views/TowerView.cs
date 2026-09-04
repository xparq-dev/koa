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

            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.Stats.MaxHp);
            }

            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnAttackFired += HandleAttackFired;
            Logic.OnDestroyed += HandleDestroyed;
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnAttackFired -= HandleAttackFired;
                Logic.OnDestroyed -= HandleDestroyed;
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
            if (towerMeshRenderer != null)
            {
                towerMeshRenderer.material.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
            if (laserLineRenderer != null)
            {
                laserLineRenderer.enabled = false;
            }
        }
    }
}
