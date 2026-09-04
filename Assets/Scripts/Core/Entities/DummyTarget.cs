using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// หุ่นฝึกซ้อม (Dummy Target) สำหรับทดสอบระบบการทำดาเมจใน Phase 1
    /// รันบน Simulation Core โดยแยกออกจาก Presentation Layer (Decoupled Core)
    /// </summary>
    public class DummyTarget
    {
        public string TargetId { get; private set; }
        public float MaxHp { get; private set; } = 1000f;
        public float CurrentHp { get; private set; }
        public float Armor { get; private set; } = 20f;
        public float MagicResist { get; private set; } = 20f;
        public float Radius { get; private set; } = 0.8f;
        public Vector3 Position { get; set; }
        public bool IsAlive => CurrentHp > 0f;

        private float _respawnTimer = 0f;
        private const float RespawnDelay = 3.0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, DamageType> OnDamageTaken;
        public event Action OnDied;
        public event Action OnRespawned;

        public DummyTarget(string targetId, Vector3 initialPosition, float maxHp = 1000f, float armor = 20f, float mr = 20f)
        {
            TargetId = targetId;
            Position = initialPosition;
            MaxHp = maxHp;
            Armor = armor;
            MagicResist = mr;
            CurrentHp = MaxHp;
        }

        public void TakeDamage(float rawDamage, DamageType damageType)
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
            OnDamageTaken?.Invoke(netDamage, damageType);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0f)
            {
                _respawnTimer = RespawnDelay;
                OnDied?.Invoke();
            }
        }

        public void SimulationTick(float deltaTime)
        {
            if (!IsAlive)
            {
                _respawnTimer -= deltaTime;
                if (_respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
        }

        public void Respawn()
        {
            CurrentHp = MaxHp;
            OnRespawned?.Invoke();
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
