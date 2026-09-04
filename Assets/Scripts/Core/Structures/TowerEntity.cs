using KOA.Data.Enums;
using KOA.Data.Models;
using System;
using UnityEngine;

namespace KOA.Core.Structures
{
    /// <summary>
    /// Simulation Core สำหรับป้อมปราการและ Nexus ตาม Section 3.3
    /// คำนวณ Heating Laser, Tower Plating, และ AoE Slow ใน Simulation Tick 30 FPS
    /// </summary>
    public class TowerEntity
    {
        public string TowerId { get; private set; }
        public StructureStats Stats { get; private set; }
        public Vector3 Position { get; set; }
        public float CurrentHp { get; private set; }
        public bool IsDestroyed => CurrentHp <= 0f;
        public bool IsInvulnerable { get; set; }

        // สถานะการยิงและการล็อคเป้า
        private float _fireCooldownRemaining = 0f;
        private string _currentTargetId = null;
        private int _consecutiveHitsOnTarget = 0;
        private float _matchElapsedTime = 0f;

        // เกราะสุทธิคำนวณรวม Tower Plating (4 นาทีแรก)
        public float EffectiveArmor
        {
            get
            {
                float armor = Stats.BaseArmor;
                if (Stats.HasTowerPlating && _matchElapsedTime <= (Stats.PlatingDurationMinutes * 60f))
                {
                    armor += Stats.PlatingBonusArmor;
                }
                return armor;
            }
        }

        // Events สำหรับ Presentation Layer (VFX ลำแสงเลเซอร์, เสียงยิง, UI เลือด)
        public event Action<float, float> OnHealthChanged;
        public event Action<Vector3, float, bool> OnAttackFired; // targetPos, damage, isHeatingLaser
        public event Action OnDestroyed;

        public TowerEntity(string towerId, StructureStats stats, Vector3 position)
        {
            TowerId = towerId;
            Stats = stats;
            Position = position;
            CurrentHp = stats.MaxHp;
            IsInvulnerable = stats.RequiresTier2Destroyed; // Nexus จะเริ่มด้วย Invulnerable
        }

        public void SimulationTick(float deltaTime)
        {
            if (IsDestroyed) return;

            _matchElapsedTime += deltaTime;

            // คูลดาวน์การยิง
            if (_fireCooldownRemaining > 0f)
            {
                _fireCooldownRemaining = Mathf.Max(0f, _fireCooldownRemaining - deltaTime);
            }

            // Nexus HP Regen นอกการต่อสู้ (Section 3.3)
            if (Stats.HpRegenerationOutOfCombat > 0f && CurrentHp < Stats.MaxHp)
            {
                CurrentHp = Mathf.Min(Stats.MaxHp, CurrentHp + (Stats.HpRegenerationOutOfCombat * deltaTime));
                OnHealthChanged?.Invoke(CurrentHp, Stats.MaxHp);
            }
        }

        /// <summary>
        /// ตรวจสอบและทำการยิงเป้าหมายตามกลไก Heating Laser (Section 3.3)
        /// </summary>
        public bool TryAttackTarget(string targetId, Vector3 targetPosition, Action<float, DamageType> applyDamageCallback)
        {
            if (IsDestroyed || Stats.BaseAttackDamage <= 0f) return false;
            if (_fireCooldownRemaining > 0f) return false;

            float distance = Vector3.Distance(Position, targetPosition);
            if (distance > Stats.AttackRange)
            {
                if (_currentTargetId == targetId)
                {
                    ResetTargetLock();
                }
                return false;
            }

            // คำนวณ Heating Laser Multiplier
            float damageMultiplier = 1.0f;
            if (Stats.IsHeatingLaser)
            {
                if (_currentTargetId == targetId)
                {
                    _consecutiveHitsOnTarget++;
                }
                else
                {
                    _currentTargetId = targetId;
                    _consecutiveHitsOnTarget = 1;
                }

                float bonusPercent = (_consecutiveHitsOnTarget - 1) * Stats.DamageIncreasePerHitPercent;
                damageMultiplier = Mathf.Min(1.0f + (bonusPercent / 100f), Stats.MaxDamageMultiplier);
            }
            else
            {
                _currentTargetId = targetId;
            }

            float finalDamage = Stats.BaseAttackDamage * damageMultiplier;
            applyDamageCallback?.Invoke(finalDamage, Stats.DamageType);

            _fireCooldownRemaining = Stats.FireRateSeconds;
            OnAttackFired?.Invoke(targetPosition, finalDamage, damageMultiplier > 1.0f);
            return true;
        }

        public void ResetTargetLock()
        {
            _currentTargetId = null;
            _consecutiveHitsOnTarget = 0;
        }

        public void TakeDamage(float rawDamage, DamageType damageType)
        {
            if (IsDestroyed || IsInvulnerable) return;

            float netDamage = rawDamage;
            if (damageType == DamageType.Physical)
            {
                float reduction = EffectiveArmor / (100f + Mathf.Max(0f, EffectiveArmor));
                netDamage = rawDamage * (1f - reduction);
            }
            else if (damageType == DamageType.Magic)
            {
                float reduction = Stats.BaseMagicResist / (100f + Mathf.Max(0f, Stats.BaseMagicResist));
                netDamage = rawDamage * (1f - reduction);
            }

            CurrentHp = Mathf.Max(0f, CurrentHp - netDamage);
            OnHealthChanged?.Invoke(CurrentHp, Stats.MaxHp);

            if (CurrentHp <= 0f)
            {
                OnDestroyed?.Invoke();
            }
        }
    }
}
