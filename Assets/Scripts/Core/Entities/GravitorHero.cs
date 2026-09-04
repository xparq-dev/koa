using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// ฮีโร่ Gravitor (The Gravity Singularity) ตาม Section 2.1, 2.2 และ Section 6.4
    /// Role: Tank / Disruptor / Initiator
    /// </summary>
    public class GravitorHero : HeroBase3D
    {
        // Skill Cooldown & Mana constants (Section 6.4)
        public const float PassiveInternalCooldown = 12.0f;
        public const float Skill1CooldownDuration = 12.0f;
        public const float Skill1ManaCost = 70.0f;
        public const float Skill1Range = 7.0f;

        public const float Skill2CooldownDuration = 14.0f;
        public const float Skill2ManaCost = 80.0f;
        public const float Skill2Radius = 4.0f;

        public const float UltimateCooldownDuration = 110.0f;
        public const float UltimateManaCost = 150.0f;
        public const float UltimateRange = 7.5f;
        public const float UltimateRadius = 4.5f;

        // Timers & Shield
        public float PassiveCooldownRemaining { get; private set; } = 0f;
        public float CurrentShield { get; private set; } = 0f;
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;

        // Navigation
        public Vector3 TargetDestination { get; private set; }
        public bool IsMoving { get; private set; }

        // Events
        public event Action<Vector3> OnMagneticPullCast;
        public event Action<float> OnRepulsionZoneCast;
        public event Action<Vector3, float> OnGravityCollapseCast;

        public GravitorHero(Vector3 spawnPosition) : base(
            heroId: "hero_gravitor",
            displayName: "Gravitor",
            baseHp: 700f,
            baseMana: 240f,
            baseArmor: 42f,
            baseMr: 36f,
            baseAd: 58f,
            baseSpeed: 6.9f,
            attackRange: 2.0f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Gravitor")
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
        /// Skill 1: Magnetic Pull (SINGLE_TARGET / Aim) Section 6.4 & 6.5
        /// ดึงเป้าหมายเข้าหาตัว Gravitor
        /// </summary>
        public bool TryCastMagneticPull(Vector3 aimWorldPos, DummyTarget dummyTarget)
        {
            if (!IsAlive || Skill1CooldownRemaining > 0f || CurrentMana < Skill1ManaCost) return false;

            CurrentMana -= Skill1ManaCost;
            Skill1CooldownRemaining = Skill1CooldownDuration;
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);

            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                float dist = Vector3.Distance(Position, dummyTarget.Position);
                if (dist <= Skill1Range)
                {
                    // ดึงเป้าหมายมาอยู่ตรงหน้า Gravitor (ระยะ 1.8m)
                    Vector3 pullDest = Position + (Rotation * Vector3.forward * 1.8f);
                    dummyTarget.Position = pullDest;
                    dummyTarget.TakeDamage(100f + (EffectiveAttackDamage * 0.5f), DamageType.Magic);
                }
            }

            OnMagneticPullCast?.Invoke(aimWorldPos);
            return true;
        }

        /// <summary>
        /// Skill 2: Repulsion Zone (SELF_CAST) Section 6.4 & 6.5
        /// ผลักศัตรูรอบตัวออกในระยะ 4m
        /// </summary>
        public bool TryCastRepulsionZone(DummyTarget dummyTarget)
        {
            if (!IsAlive || Skill2CooldownRemaining > 0f || CurrentMana < Skill2ManaCost) return false;

            CurrentMana -= Skill2ManaCost;
            Skill2CooldownRemaining = Skill2CooldownDuration;
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);

            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                float dist = Vector3.Distance(Position, dummyTarget.Position);
                if (dist <= Skill2Radius)
                {
                    Vector3 pushDir = (dummyTarget.Position - Position).normalized;
                    dummyTarget.Position += pushDir * 2.5f; // ผลักออกไป 2.5m
                    dummyTarget.TakeDamage(120f + (EffectiveAttackDamage * 0.6f), DamageType.Magic);
                }
            }

            OnRepulsionZoneCast?.Invoke(Skill2Radius);
            return true;
        }

        /// <summary>
        /// Ultimate: Gravity Kore Collapse (GROUND_TARGET_AOE) Section 6.4
        /// สร้างหลุมดำดูดเป้าหมายและระเบิด
        /// </summary>
        public bool TryCastGravityCollapse(Vector3 groundPos, DummyTarget dummyTarget)
        {
            if (!IsAlive || UltimateCooldownRemaining > 0f || CurrentMana < UltimateManaCost) return false;

            CurrentMana -= UltimateManaCost;
            UltimateCooldownRemaining = UltimateCooldownDuration;
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);

            Vector3 center = groundPos;
            center.y = Position.y;

            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                float dist = Vector3.Distance(center, dummyTarget.Position);
                if (dist <= UltimateRadius)
                {
                    dummyTarget.Position = center; // ดูดเข้ากึ่งกลาง
                    dummyTarget.TakeDamage(280f + EffectiveAttackDamage, DamageType.Magic);
                }
            }

            OnGravityCollapseCast?.Invoke(center, UltimateRadius);
            return true;
        }

        public override void TakeDamage(float rawDamage, DamageType damageType)
        {
            if (!IsAlive) return;

            // Passive: Antigravity Shield (Section 6.4)
            if (PassiveCooldownRemaining <= 0f && CurrentShield <= 0f)
            {
                CurrentShield = EffectiveMaxHp * 0.15f; // โล่ 15% Max HP
                PassiveCooldownRemaining = PassiveInternalCooldown;
            }

            float remainingDamage = rawDamage;
            if (CurrentShield > 0f)
            {
                if (CurrentShield >= remainingDamage)
                {
                    CurrentShield -= remainingDamage;
                    remainingDamage = 0f;
                }
                else
                {
                    remainingDamage -= CurrentShield;
                    CurrentShield = 0f;
                }
            }

            if (remainingDamage > 0f)
            {
                base.TakeDamage(remainingDamage, damageType);
            }
        }

        public override void SimulationTick(float deltaTime)
        {
            if (!IsAlive) return;

            Inventory.SimulationTick(deltaTime);

            if (PassiveCooldownRemaining > 0f) PassiveCooldownRemaining -= deltaTime;
            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;

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
                    Vector3 moveDir = toTarget / dist;
                    float step = EffectiveMoveSpeed * deltaTime;
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
