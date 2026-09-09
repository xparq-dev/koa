using KOA.Core.Minions;
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

        // Skill 3: Concussive Blast (Section 6.3 E)
        public const float Skill3CooldownDuration = 11.0f;
        public const float Skill3ManaCost = 65.0f;
        public const float Skill3Range = 4.0f;

        public const float UltimateCooldownDuration = 100.0f;
        public const float UltimateManaCost = 120.0f;
        public const float UltimateRange = 18.0f;
        public const float UltimateWidth = 1.6f;

        // Timers
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float Skill3CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float Skill2ActiveTimer { get; private set; } = 0f;
        public float AttackCooldownRemaining { get; private set; } = 0f;
        public float BaseAttackCooldown { get; set; } = 1.4f; // Sniper — อัตราโจมตีช้าแต่ระยะยิงไกลและรุนแรง

        // Passive Momentum Piercer Stacks (0-5)
        public int PassiveStacks { get; private set; } = 0;
        private HeroBase3D _passiveTargetHero;

        // Navigation
        public Vector3 TargetDestination { get; private set; }

        // Events
        public event Action<Vector3, Vector3, bool> OnHeavyBoltFired;
        public event Action OnHuntersFocusActivated;
        public event Action<Vector3, bool> OnConcussiveBlastFired;
        public event Action<Vector3, Vector3, bool> OnBallistaOverdriveFired;

        public KorvaxHero(Vector3 spawnPosition) : base(
            heroId: "hero_korvax",
            displayName: "Korvax",
            baseHp: 540f,
            baseMana: 260f,
            baseArmor: 28f,
            baseMr: 26f,
            baseAd: 46f,           // Balance Pass: 52 → 46 (Ranged ไม่ควร AD เท่า Melee Tank)
            baseSpeed: 4.5f,
            attackRange: 6.5f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Korvax")
        )
        {
            Position = spawnPosition;
            TargetDestination = spawnPosition;
        }

        public override void SetMoveDestination(Vector3 destination)
        {
            TargetDestination = ClampMoveDestination(destination);
            IsMoving = true;
        }

        public override void StopMoving()
        {
            IsMoving = false;
            TargetDestination = Position;
        }

        public override bool TryBasicAttack(ITargetable target)
        {
            if (!CanPerformActions || target == null || !target.IsAlive) return false;
            if (target.TeamId == TeamId && target.TeamId != -1) return false;

            float distance = Vector3.Distance(Position, target.Position);
            if (distance > (EffectiveRange + target.Radius)) return false;
            if (AttackCooldownRemaining > 0f) return false;

            Vector3 lookDir = (target.Position - Position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Rotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z));
            }

            if (target is HeroBase3D heroTarget)
            {
                if (!ReferenceEquals(_passiveTargetHero, heroTarget))
                {
                    _passiveTargetHero?.RemoveArmorShred(HeroId);
                    _passiveTargetHero = heroTarget;
                    PassiveStacks = 0;
                }
                PassiveStacks = Mathf.Min(5, PassiveStacks + 1);
                heroTarget.ApplyArmorShred(HeroId, PassiveStacks * 0.03f);
            }
            else
            {
                _passiveTargetHero?.RemoveArmorShred(HeroId);
                _passiveTargetHero = null;
                PassiveStacks = 0;
            }

            target.TakeDamage(EffectiveAttackDamage, DamageType.Physical, HeroId);
            AttackCooldownRemaining = EffectiveAttackCooldownFromBase(BaseAttackCooldown);
            InvokeBasicAttackExecuted(target.Position);
            return true;
        }

        public bool TryBasicAttack(DummyTarget target) => TryBasicAttack((ITargetable)target);

        public float EffectiveRange => AttackRange + (Skill2ActiveTimer > 0f ? 2.5f : 0f);
        public override float EffectiveAttackDamage => base.EffectiveAttackDamage + (Skill2ActiveTimer > 0f ? 10f + Skill2Rank * 10f : 0f);

        /// <summary>
        /// Skill 1: Heavy Bolt (SKILLSHOT_LINE) Section 6.3 & 6.5
        /// </summary>
        public bool TryCastHeavyBolt(Vector3 aimWorldPos, ITargetable target = null)
        {
            if (!IsAlive || Skill1Rank <= 0 || Skill1CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill1ManaCost)) return false;

            Skill1CooldownRemaining = ApplyAbilityCooldownReduction(Mathf.Max(6.0f, Skill1CooldownDuration - (Skill1Rank - 1) * 0.8f));

            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Rotation * Vector3.forward;
            aimDir.Normalize();

            Rotation = Quaternion.LookRotation(aimDir);
            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * Skill1Range);

            bool hit = false;
            if (target != null && target.IsAlive && (target.TeamId != TeamId || target.TeamId == -1))
            {
                hit = Vector3.Distance(Position, target.Position) <= Skill1Range;
                if (hit)
                {
                    float baseDmg = 90f + (Skill1Rank - 1) * 55f;
                    float adRatio = 0.90f;
                    float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * adRatio));
                    target.TakeDamage(damage, DamageType.Physical, HeroId);
                }
            }

            OnHeavyBoltFired?.Invoke(startPos, endPos, hit);
            return true;
        }

        public bool TryCastHeavyBolt(Vector3 aimWorldPos, DummyTarget dummyTarget) => TryCastHeavyBolt(aimWorldPos, (ITargetable)dummyTarget);

        /// <summary>
        /// Skill 2: Hunter's Focus (SELF_CAST) Section 6.3 & 6.5
        /// </summary>
        public bool TryCastHuntersFocus()
        {
            if (!IsAlive || Skill2Rank <= 0 || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            Skill2CooldownRemaining = ApplyAbilityCooldownReduction(Skill2CooldownDuration);
            Skill2ActiveTimer = Skill2Duration;

            OnHuntersFocusActivated?.Invoke();
            return true;
        }

        /// <summary>
        /// Skill 3: Concussive Blast (SINGLE_TARGET / CONE) Section 6.3 E
        /// ยิงกระสุนระเบิดผลักเป้าหมายตรงหน้าให้กระเด็นถอยหลัง 3.5m ทำดาเมจกายภาพและ Slow
        /// </summary>
        public bool TryCastConcussiveBlast(Vector3 aimWorldPos, ITargetable target = null)
        {
            if (!IsAlive || Skill3Rank <= 0 || Skill3CooldownRemaining > 0f) return false;
            if (target == null || !target.IsAlive) return false;
            if (target.TeamId == TeamId && target.TeamId != -1) return false;

            float targetDistance = Vector3.Distance(Position, target.Position);
            if (targetDistance > Skill3Range + target.Radius) return false;
            if (!TryConsumeMana(Skill3ManaCost)) return false;

            float cd = Mathf.Max(6.5f, Skill3CooldownDuration - (Skill3Rank - 1) * 0.8f);
            Skill3CooldownRemaining = ApplyAbilityCooldownReduction(cd);

            Vector3 blastDir = (aimWorldPos - Position);
            blastDir.y = 0;
            if (blastDir.sqrMagnitude < 0.01f) blastDir = Rotation * Vector3.forward;
            blastDir.Normalize();

            Rotation = Quaternion.LookRotation(blastDir);

            float baseDmg = 70f + (Skill3Rank - 1) * 40f;
            float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * 0.50f));
            target.TakeDamage(damage, DamageType.Physical, HeroId);

            // ผลักศัตรูถอยหลัง 3.5m (ยกเว้นป้อม)
            Vector3 pushDir = (target.Position - Position).normalized;
            if (target is HeroBase3D heroTarget)
            {
                if (heroTarget.TryDisplace(heroTarget.Position + pushDir * 3.5f))
                    heroTarget.ApplyMovementSlow(0.40f, 2.0f);
            }
            else if (target is MinionEntity minionTarget)
            {
                minionTarget.Position += pushDir * 3.5f;
            }

            OnConcussiveBlastFired?.Invoke(Position, true);
            return true;
        }

        public bool TryCastConcussiveBlast(Vector3 aimWorldPos, DummyTarget dummyTarget) => TryCastConcussiveBlast(aimWorldPos, (ITargetable)dummyTarget);

        /// <summary>
        /// Ultimate: Ballista Overdrive (SKILLSHOT_LINE) Section 6.3
        /// </summary>
        public bool TryCastBallistaOverdrive(Vector3 aimWorldPos, ITargetable target = null)
        {
            if (!IsAlive || UltimateRank <= 0 || UltimateCooldownRemaining > 0f) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            UltimateCooldownRemaining = ApplyAbilityCooldownReduction(115f - (UltimateRank * 15f));

            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Rotation * Vector3.forward;
            aimDir.Normalize();

            Rotation = Quaternion.LookRotation(aimDir);
            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * UltimateRange);

            bool hit = false;
            if (target != null && target.IsAlive && (target.TeamId != TeamId || target.TeamId == -1))
            {
                hit = Vector3.Distance(Position, target.Position) <= UltimateRange;
                if (hit)
                {
                    float baseDmg = 300f + (UltimateRank - 1) * 150f;
                    float adRatio = 1.40f;
                    float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * adRatio));
                    target.TakeDamage(damage, DamageType.Physical, HeroId);
                }
            }

            OnBallistaOverdriveFired?.Invoke(startPos, endPos, hit);
            return true;
        }

        public bool TryCastBallistaOverdrive(Vector3 aimWorldPos, DummyTarget dummyTarget) => TryCastBallistaOverdrive(aimWorldPos, (ITargetable)dummyTarget);

        public override void SimulationTick(float deltaTime)
        {
            TickSharedSystems(deltaTime);
            if (!IsAlive) return;

            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (Skill3CooldownRemaining > 0f) Skill3CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;
            if (Skill2ActiveTimer > 0f) Skill2ActiveTimer -= deltaTime;
            if (AttackCooldownRemaining > 0f) AttackCooldownRemaining = UnityEngine.Mathf.Max(0f, AttackCooldownRemaining - deltaTime);

            if (IsStunned) return;

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
