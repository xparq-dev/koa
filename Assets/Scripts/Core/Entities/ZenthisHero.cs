using KOA.Data.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// ฮีโร่ Zenthis (The Chrono Guardian) ตาม Section 2.1, 2.2 และ Section 6.2
    /// Role: Time Mage / Controller (Ranged)
    /// </summary>
    public class ZenthisHero : HeroBase3D
    {
        // Skill Cooldown & Mana constants (Section 6.2)
        public const float PassiveInternalCooldown = 45.0f;
        public const float Skill1CooldownDuration = 12.0f;
        public const float Skill1ManaCost = 90.0f;
        public const float Skill1Range = 8.0f;
        public const float Skill1Radius = 2.5f;

        public const float Skill2CooldownDuration = 16.0f;
        public const float Skill2ManaCost = 70.0f;
        public const float Skill2Duration = 4.0f;

        public const float UltimateCooldownDuration = 120.0f;
        public const float UltimateManaCost = 150.0f;

        // Timers
        public float PassiveCooldownRemaining { get; private set; } = 0f;
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float Skill2ActiveTimer { get; private set; } = 0f;

        // Grand Rewind Snapshot History (4 วินาที)
        private readonly Queue<(Vector3 Pos, float Hp)> _historySnapshots = new();
        private float _snapshotTimer = 0f;

        // Navigation
        public Vector3 TargetDestination { get; private set; }
        public bool IsMoving { get; private set; }

        // Events
        public event Action<Vector3, float> OnSacredHourglassCast; // center, radius
        public event Action OnAuraOfEternityCast;
        public event Action<Vector3, float> OnGrandRewindCast; // rewindPos, restoredHp

        public ZenthisHero(Vector3 spawnPosition) : base(
            heroId: "hero_zenthis",
            displayName: "Zenthis",
            baseHp: 520f,
            baseMana: 340f,
            baseArmor: 26f,
            baseMr: 30f,
            baseAd: 48f,
            baseSpeed: 6.8f,
            attackRange: 5.5f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Zenthis")
        )
        {
            Position = spawnPosition;
            TargetDestination = spawnPosition;
        }

        public override void SetMoveDestination(Vector3 destination)
        {
            destination.y = Position.y;
            TargetDestination = destination;
            IsMoving = true;
        }

        public override void StopMoving()
        {
            IsMoving = false;
            TargetDestination = Position;
        }

        /// <summary>
        /// Skill 1: Sacred Hourglass (GROUND_TARGET_AOE) Section 6.2 & 6.5
        /// </summary>
        public bool TryCastSacredHourglass(Vector3 targetGroundPos, DummyTarget dummyTarget)
        {
            if (!IsAlive || Skill1CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill1ManaCost)) return false;

            Skill1CooldownRemaining = Skill1CooldownDuration;

            Vector3 center = targetGroundPos;
            center.y = Position.y;

            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                float dist = Vector3.Distance(center, dummyTarget.Position);
                if (dist <= Skill1Radius + dummyTarget.Radius)
                {
                    float damage = 140f + (EffectiveAttackDamage * 0.7f);
                    dummyTarget.TakeDamage(damage, DamageType.Magic);
                }
            }

            OnSacredHourglassCast?.Invoke(center, Skill1Radius);
            return true;
        }

        /// <summary>
        /// Skill 2: Aura of Eternity (SELF_CAST) Section 6.2 & 6.5
        /// บัฟความเร็วเคลื่อนที่ +30% ชั่วคราว 4 วินาที
        /// </summary>
        public bool TryCastAuraOfEternity()
        {
            if (!IsAlive || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            Skill2CooldownRemaining = Skill2CooldownDuration;
            Skill2ActiveTimer = Skill2Duration;

            OnAuraOfEternityCast?.Invoke();
            return true;
        }

        /// <summary>
        /// Ultimate: Grand Rewind (SELF_CAST) Section 6.2
        /// ย้อนตำแหน่งและ HP กลับไปเมื่อ 4 วินาทีก่อน
        /// </summary>
        public bool TryCastGrandRewind()
        {
            if (!IsAlive || UltimateCooldownRemaining > 0f) return false;
            if (_historySnapshots.Count == 0) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            UltimateCooldownRemaining = UltimateCooldownDuration;

            var oldest = _historySnapshots.Peek();
            Position = oldest.Pos;
            CurrentHp = Mathf.Max(CurrentHp, oldest.Hp); // ไม่ลดเลือดถ้าเลือดเดิมน้อยกว่า
            TargetDestination = Position;
            IsMoving = false;

            InvokeHealthChanged();
            OnGrandRewindCast?.Invoke(Position, CurrentHp);
            return true;
        }

        public override void TakeDamage(float rawDamage, DamageType damageType)
        {
            if (!IsAlive) return;

            // Passive: Chrono Stasis (Section 6.2)
            if (PassiveCooldownRemaining <= 0f && CurrentHp <= rawDamage)
            {
                PassiveCooldownRemaining = PassiveInternalCooldown;
                CurrentHp = EffectiveMaxHp * 0.20f; // รอดตายฟื้น HP 20%
                InvokeHealthChanged();
                return;
            }

            base.TakeDamage(rawDamage, damageType);
        }

        public override void SimulationTick(float deltaTime)
        {
            if (!IsAlive) return;

            Inventory.SimulationTick(deltaTime);

            // Cooldowns
            if (PassiveCooldownRemaining > 0f) PassiveCooldownRemaining -= deltaTime;
            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;
            if (Skill2ActiveTimer > 0f) Skill2ActiveTimer -= deltaTime;

            // Snapshot history for Grand Rewind
            _snapshotTimer += deltaTime;
            if (_snapshotTimer >= 0.5f)
            {
                _snapshotTimer = 0f;
                _historySnapshots.Enqueue((Position, CurrentHp));
                if (_historySnapshots.Count > 8) // 8 snapshots * 0.5s = 4.0s
                {
                    _historySnapshots.Dequeue();
                }
            }

            // Movement
            if (IsMoving)
            {
                Vector3 toTarget = TargetDestination - Position;
                toTarget.y = 0;
                float dist = toTarget.magnitude;
                if (dist <= 0.08f)
                {
                    Position = TargetDestination;
                    IsMoving = false;
                }
                else
                {
                    float speedBonus = Skill2ActiveTimer > 0f ? 1.30f : 1.0f;
                    Vector3 moveDir = toTarget / dist;
                    float step = EffectiveMoveSpeed * speedBonus * deltaTime;
                    if (step >= dist)
                    {
                        Position = TargetDestination;
                        IsMoving = false;
                    }
                    else
                    {
                        Position += moveDir * step;
                    }
                    Rotation = Quaternion.LookRotation(moveDir);
                }
            }
        }
    }
}
