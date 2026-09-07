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
                // แยกสีตามทีม: Blue (0) สีฟ้า, Red (1) สีแดง
                minionRenderer.material.color = Logic.TeamId == 0 ? new Color(0.2f, 0.6f, 1f) : new Color(1f, 0.25f, 0.2f);
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
            Destroy(gameObject);
        }
    }
}
