using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.Minions
{
    public enum MinionType
    {
        Melee = 0,
        Ranged = 1
    }

    /// <summary>
    /// Simulation Core สำหรับมินเนี่ยน/ครีปเลนตาม Section 3.2 และระบบเศรษฐกิจ Section 4
    /// </summary>
    public class MinionEntity
    {
        public string MinionId { get; private set; }
        public MinionType Type { get; private set; }
        public int TeamId { get; private set; } // 0 = Blue, 1 = Red
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public float AttackDamage { get; private set; }
        public float Armor { get; private set; }
        public float MagicResist { get; private set; }
        public float MoveSpeed { get; private set; } = 5.5f;
        public float AttackRange { get; private set; }
        public float AttackCooldown { get; private set; } = 1.2f;

        public int GoldBounty { get; private set; } // 42g for Melee, 50g for Ranged (Section 4.1)
        public float ExpBounty { get; private set; } = 60f;

        public bool IsAlive => CurrentHp > 0f;

        private float _attackCooldownRemaining = 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<MinionEntity, string> OnKilled; // minion, killerId

        public MinionEntity(string id, MinionType type, int teamId, Vector3 spawnPos, int waveIndex)
        {
            MinionId = id;
            Type = type;
            TeamId = teamId;
            Position = spawnPos;

            // Evolution Scaling: +10% HP, +5% AD ทุกๆ 3 นาที (Section 3.2)
            // (คำนวณจาก waveIndex: เวฟละ 25 วิ ดังนั้น 180 วิ / 25 วิ ~= 7 เวฟต่อ 3 นาที)
            int evolutionStages = (waveIndex * 25) / 180;
            float hpMultiplier = 1.0f + (evolutionStages * 0.10f);
            float adMultiplier = 1.0f + (evolutionStages * 0.05f);

            if (type == MinionType.Melee)
            {
                MaxHp = 480f * hpMultiplier;
                AttackDamage = 18f * adMultiplier;
                Armor = 10f;
                MagicResist = 0f;
                AttackRange = 1.6f;
                GoldBounty = 42;
            }
            else
            {
                MaxHp = 320f * hpMultiplier;
                AttackDamage = 25f * adMultiplier;
                Armor = 0f;
                MagicResist = 0f;
                AttackRange = 5.5f;
                GoldBounty = 50;
            }

            CurrentHp = MaxHp;
        }

        public void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
        {
            if (!IsAlive) return;

            float netDamage = rawDamage;
            if (damageType == DamageType.Physical)
            {
                float reduction = Armor / (100f + Mathf.Max(0f, Armor));
                netDamage = rawDamage * (1f - reduction);
            }
            else if (damageType == DamageType.Magic)
            {
                float reduction = MagicResist / (100f + Mathf.Max(0f, MagicResist));
                netDamage = rawDamage * (1f - reduction);
            }

            CurrentHp = Mathf.Max(0f, CurrentHp - netDamage);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0f)
            {
                OnKilled?.Invoke(this, attackerId);
            }
        }

        public void SimulationTick(float deltaTime, Vector3 targetLaneDestination)
        {
            if (!IsAlive) return;

            if (_attackCooldownRemaining > 0f)
            {
                _attackCooldownRemaining -= deltaTime;
            }

            // เดินหน้าตามเลนไปยังจุดหมายปลายทาง
            Vector3 toTarget = targetLaneDestination - Position;
            toTarget.y = 0;
            if (toTarget.magnitude > 0.5f)
            {
                Vector3 moveDir = toTarget.normalized;
                Position += moveDir * (MoveSpeed * deltaTime);
                Rotation = Quaternion.LookRotation(moveDir);
            }
        }
    }
}
