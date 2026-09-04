using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// ฮีโร่ Korvax (The Ballista Sniper) ตาม Section 2.1, 2.2 และ Section 6.3
    /// Role: Long-Range Marksman / Sniper
    /// </summary>
    public class KorvaxHero : HeroBase3D
    {
        // Skill Cooldown & Mana constants (Section 6.3)
        public const float Skill1CooldownDuration = 10.0f;
        public const float Skill1ManaCost = 60.0f;
        public const float Skill1Range = 10.0f;
        public const float Skill1Width = 1.2f;

        public const float Skill2CooldownDuration = 18.0f;
        public const float Skill2ManaCost = 50.0f;
        public const float Skill2Duration = 5.0f;

        public const float UltimateCooldownDuration = 100.0f;
        public const float UltimateManaCost = 120.0f;
        public const float UltimateRange = 18.0f;
        public const float UltimateWidth = 1.6f;

        // Timers
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float Skill2ActiveTimer { get; private set; } = 0f;

        // Passive Momentum Piercer Stacks (0-5)
        public int PassiveStacks { get; private set; } = 0;

        // Navigation
        public Vector3 TargetDestination { get; private set; }
        public bool IsMoving { get; private set; }

        // Events
        public event Action<Vector3, Vector3, bool> OnHeavyBoltFired;
        public event Action OnHuntersFocusActivated;
        public event Action<Vector3, Vector3, bool> OnBallistaOverdriveFired;

        public KorvaxHero(Vector3 spawnPosition) : base(
            heroId: "hero_korvax",
            displayName: "Korvax",
            baseHp: 540f,
            baseMana: 260f,
            baseArmor: 28f,
            baseMr: 26f,
            baseAd: 68f,
            baseSpeed: 7.0f,
            attackRange: 6.5f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Korvax")
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

        public float EffectiveRange => AttackRange + (Skill2ActiveTimer > 0f ? 1.5f : 0f);

        /// <summary>
        /// Skill 1: Heavy Bolt (SKILLSHOT_LINE) Section 6.3 & 6.5
        /// </summary>
        public bool TryCastHeavyBolt(Vector3 aimWorldPos, DummyTarget dummyTarget)
        {
            if (!IsAlive || Skill1CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill1ManaCost)) return false;

            Skill1CooldownRemaining = Skill1CooldownDuration;

            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Rotation * Vector3.forward;
            aimDir.Normalize();

            Rotation = Quaternion.LookRotation(aimDir);
            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * Skill1Range);

            bool hit = false;
            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                hit = Vector3.Distance(Position, dummyTarget.Position) <= Skill1Range;
                if (hit)
                {
                    float damage = 150f + EffectiveAttackDamage;
                    dummyTarget.TakeDamage(damage, DamageType.Physical);
                }
            }

            OnHeavyBoltFired?.Invoke(startPos, endPos, hit);
            return true;
        }

        /// <summary>
        /// Skill 2: Hunter's Focus (SELF_CAST) Section 6.3 & 6.5
        /// </summary>
        public bool TryCastHuntersFocus()
        {
            if (!IsAlive || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            Skill2CooldownRemaining = Skill2CooldownDuration;
            Skill2ActiveTimer = Skill2Duration;

            OnHuntersFocusActivated?.Invoke();
            return true;
        }

        /// <summary>
        /// Ultimate: Ballista Overdrive (SKILLSHOT_LINE) Section 6.3
        /// </summary>
        public bool TryCastBallistaOverdrive(Vector3 aimWorldPos, DummyTarget dummyTarget)
        {
            if (!IsAlive || UltimateCooldownRemaining > 0f) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            UltimateCooldownRemaining = UltimateCooldownDuration;

            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Rotation * Vector3.forward;
            aimDir.Normalize();

            Rotation = Quaternion.LookRotation(aimDir);
            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * UltimateRange);

            bool hit = false;
            if (dummyTarget != null && dummyTarget.IsAlive)
            {
                hit = Vector3.Distance(Position, dummyTarget.Position) <= UltimateRange;
                if (hit)
                {
                    float damage = 350f + (EffectiveAttackDamage * 1.4f);
                    dummyTarget.TakeDamage(damage, DamageType.Physical);
                }
            }

            OnBallistaOverdriveFired?.Invoke(startPos, endPos, hit);
            return true;
        }

        public override void SimulationTick(float deltaTime)
        {
            if (!IsAlive) return;

            Inventory.SimulationTick(deltaTime);

            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;
            if (Skill2ActiveTimer > 0f) Skill2ActiveTimer -= deltaTime;

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
