using KOA.Core.Minions;
using KOA.Presentation.UI;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation View สำหรับครีปเลน (Section 3.2)
    /// ควบคุม Transform และแถบเลือดให้อัปเดตตาม MinionEntity Core
    /// </summary>
    public class MinionView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private WorldSpaceHealthBar healthBar;
        [SerializeField] private Renderer minionRenderer;

        public MinionEntity Logic { get; private set; }

        public void SetHealthBar(WorldSpaceHealthBar bar)
        {
            healthBar = bar;
            if (healthBar != null && Logic != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.MaxHp);
            }
        }

        public void BindLogic(MinionEntity minionEntity)
        {
            Logic = minionEntity;
            transform.position = Logic.Position;

            if (minionRenderer == null)
            {
                minionRenderer = GetComponentInChildren<Renderer>();
            }

            if (minionRenderer != null)
            {
                // แยกสีตามชนิดและทีม:
                Color baseColor = Logic.TeamId == 0 
                    ? (Logic.Type == MinionType.Super ? new Color(0.35f, 0.2f, 0.85f) :
                       Logic.Type == MinionType.Cannon ? new Color(0.2f, 0.85f, 0.85f) : new Color(0.2f, 0.6f, 1f))
                    : (Logic.Type == MinionType.Super ? new Color(0.75f, 0.05f, 0.25f) :
                       Logic.Type == MinionType.Cannon ? new Color(0.95f, 0.55f, 0.1f) : new Color(1f, 0.25f, 0.2f));

                minionRenderer.material.color = baseColor;
            }

            if (healthBar != null)
            {
                healthBar.BindTarget(transform);
                healthBar.SetHealth(Logic.CurrentHp, Logic.MaxHp);
            }

            Logic.OnHealthChanged += HandleHealthChanged;
            Logic.OnKilled += HandleKilled;
        }

        private void OnDestroy()
        {
            if (Logic != null)
            {
                Logic.OnHealthChanged -= HandleHealthChanged;
                Logic.OnKilled -= HandleKilled;
            }
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
        }

        private void Update()
        {
            if (Logic != null && Logic.IsAlive)
            {
                transform.position = Logic.Position;
                transform.rotation = Logic.Rotation;
            }
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (healthBar != null)
            {
                healthBar.SetHealth(current, max);
            }
        }

        private void HandleKilled(MinionEntity minion, string killerId)
        {
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
            Destroy(gameObject);
        }
    }
}
