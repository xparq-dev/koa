using KOA.Core.Minions;
using KOA.Core.World;
using KOA.Data.Enums;
using System;
using System.Collections.Generic;
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

        // Skill 3: Graviton Well (Section 6.4 E)
        public const float Skill3CooldownDuration = 10.0f;
        public const float Skill3ManaCost = 75.0f;
        public const float Skill3Range = 6.0f;
        public const float Skill3Radius = 3.5f;

        public const float UltimateCooldownDuration = 110.0f;
        public const float UltimateManaCost = 150.0f;
        public const float UltimateRange = 7.5f;
        public const float UltimateRadius = 4.5f;

        // Timers & Shield
        public float PassiveCooldownRemaining { get; private set; } = 0f;
        public float CurrentShield { get; private set; } = 0f;
        public float ShieldRemaining { get; private set; } = 0f;
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float Skill3CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float AttackCooldownRemaining { get; private set; } = 0f;
        public float BaseAttackCooldown { get; set; } = 1.2f; // Tank — โจมตีเร็ว เพราะเข้าใกล้และเป็น Melee

        private sealed class ActiveGravitonWell
        {
            public Vector3 Center;
            public List<ITargetable> Targets;
            public float DamagePerTick;
            public float TickTimer = 0.5f;
            public int TicksRemaining = 4;
        }

        private readonly List<ActiveGravitonWell> _activeWells = new List<ActiveGravitonWell>();

        // Navigation
        public Vector3 TargetDestination { get; private set; }

        // Events
        public event Action<Vector3> OnMagneticPullCast;
        public event Action<float> OnRepulsionZoneCast;
        public event Action<Vector3, float, bool> OnGravitonWellCast;
        public event Action<Vector3, float> OnGravityCollapseCast;

        public GravitorHero(Vector3 spawnPosition) : base(
            heroId: "hero_gravitor",
            displayName: "Gravitor",
            baseHp: 660f,          // Balance Pass: 700 → 660 (ลด early unkillable)
            baseMana: 240f,
            baseArmor: 36f,        // Balance Pass: 42 → 36 (ลด armor reduction จาก 29.5% → 26.5%)
            baseMr: 36f,
            baseAd: 48f,
            baseSpeed: 4.1f,
            attackRange: 2.0f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Gravitor")
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
            if (distance > (AttackRange + target.Radius)) return false;
            if (AttackCooldownRemaining > 0f) return false;

            Vector3 lookDir = (target.Position - Position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Rotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z));
            }

            target.TakeDamage(EffectiveAttackDamage, DamageType.Magic, HeroId);
            AttackCooldownRemaining = EffectiveAttackCooldownFromBase(BaseAttackCooldown);
            InvokeBasicAttackExecuted(target.Position);
            return true;
        }

        public bool TryBasicAttack(DummyTarget target) => TryBasicAttack((ITargetable)target);

        /// <summary>
        /// Skill 1: Magnetic Pull (SINGLE_TARGET / Aim) Section 6.4 & 6.5
        /// ดึงเป้าหมายเข้าหาตัว Gravitor
        /// </summary>
        public bool TryCastMagneticPull(Vector3 aimWorldPos, ITargetable target = null)
        {
            if (!IsAlive || Skill1Rank <= 0 || Skill1CooldownRemaining > 0f) return false;
            if (target == null || !target.IsAlive) return false;
            if (target.TeamId == TeamId && target.TeamId != -1) return false;

            float targetDistance = Vector3.Distance(Position, target.Position);
            if (targetDistance > Skill1Range + target.Radius) return false;
            if (!TryConsumeMana(Skill1ManaCost)) return false;

            Skill1CooldownRemaining = ApplyAbilityCooldownReduction(Mathf.Max(6.0f, Skill1CooldownDuration - (Skill1Rank - 1) * 1.0f));

            // ดึงเป้าหมายมาอยู่ตรงหน้า Gravitor (ระยะ 1.8m)
            Vector3 pullDirection = target.Position - Position;
            pullDirection.y = 0f;
            if (pullDirection.sqrMagnitude > 0.001f)
                Rotation = Quaternion.LookRotation(pullDirection.normalized);
            Vector3 pullDest = Position + (Rotation * Vector3.forward * 1.8f);
            if (target is DummyTarget dt) dt.Position = pullDest;
            else if (target is MinionEntity me) me.Position = pullDest;
            else if (target is HeroBase3D hb) hb.TryDisplace(pullDest);

            float baseDmg = 70f + (Skill1Rank - 1) * 40f;
            float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * 0.50f));
            target.TakeDamage(damage, DamageType.Magic, HeroId);

            OnMagneticPullCast?.Invoke(aimWorldPos);
            return true;
        }

        public bool TryCastMagneticPull(Vector3 aimWorldPos, DummyTarget dummyTarget) => TryCastMagneticPull(aimWorldPos, (ITargetable)dummyTarget);

        /// <summary>
        /// Skill 2: Repulsion Zone (SELF_CAST) Section 6.4 & 6.5
        /// ผลักศัตรูรอบตัวออกในระยะ 4m
        /// </summary>
        public bool TryCastRepulsionZone(ITargetable target = null)
        {
            if (!IsAlive || Skill2Rank <= 0 || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            Skill2CooldownRemaining = ApplyAbilityCooldownReduction(Mathf.Max(7.0f, Skill2CooldownDuration - (Skill2Rank - 1) * 1.0f));

            if (target != null && target.IsAlive && (target.TeamId != TeamId || target.TeamId == -1))
            {
                float dist = Vector3.Distance(Position, target.Position);
                if (dist <= Skill2Radius)
                {
                    Vector3 pushDir = (target.Position - Position).normalized;
                    Vector3 pushDest = target.Position + pushDir * 3.0f;
                    if (target is DummyTarget dt) dt.Position = pushDest;
                    else if (target is MinionEntity me) me.Position = pushDest;
                    else if (target is HeroBase3D hb) hb.TryDisplace(pushDest);

                    float baseDmg = 80f + (Skill2Rank - 1) * 45f;
                    float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * 0.55f));
                    target.TakeDamage(damage, DamageType.Magic, HeroId);
                }
            }

            OnRepulsionZoneCast?.Invoke(Skill2Radius);
            return true;
        }

        public bool TryCastRepulsionZone(DummyTarget dummyTarget) => TryCastRepulsionZone((ITargetable)dummyTarget);

        /// <summary>
        /// Skill 3: Graviton Well (GROUND_TARGET_AOE) Section 6.4 E
        /// วางบ่อแรงโน้มถ่วงบนพื้นรัศมี 3.5m ทำเวทดาเมจและ Slow ศัตรู
        /// </summary>
        public bool TryCastGravitonWell(Vector3 groundPos, System.Collections.Generic.IEnumerable<ITargetable> targets = null)
        {
            if (!IsAlive || Skill3Rank <= 0 || Skill3CooldownRemaining > 0f) return false;
            if (!ArenaBounds.Contains(groundPos)) return false;
            if (!TryConsumeMana(Skill3ManaCost)) return false;

            float cd = Mathf.Max(6.0f, Skill3CooldownDuration - (Skill3Rank - 1) * 0.8f);
            Skill3CooldownRemaining = ApplyAbilityCooldownReduction(cd);

            Vector3 toGround = groundPos - Position;
            toGround.y = 0;
            float dist = Mathf.Min(toGround.magnitude, Skill3Range);
            Vector3 center = dist > 0.1f ? Position + (toGround.normalized * dist) : Position;

            bool hit = false;
            float damage = EffectiveSkillDamage(40f + (Skill3Rank - 1) * 25f);
            var trackedTargets = new List<ITargetable>();

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null && t.IsAlive && (t.TeamId != TeamId || t.TeamId == -1))
                    {
                        float d = Vector3.Distance(center, t.Position);
                        if (d <= Skill3Radius + t.Radius)
                        {
                            hit = true;
                            trackedTargets.Add(t);
                            t.TakeDamage(damage, DamageType.Magic, HeroId);
                            if (t is HeroBase3D heroTarget)
                                heroTarget.ApplyMovementSlow(0.50f, 2.5f);
                        }
                    }
                }
            }

            if (trackedTargets.Count > 0)
            {
                _activeWells.Add(new ActiveGravitonWell
                {
                    Center = center,
                    Targets = trackedTargets,
                    DamagePerTick = damage
                });
            }

            OnGravitonWellCast?.Invoke(center, Skill3Radius, hit);
            return true;
        }

        public bool TryCastGravitonWell(Vector3 groundPos, ITargetable target) => TryCastGravitonWell(groundPos, target != null ? new[] { target } : null);
        public bool TryCastGravitonWell(Vector3 groundPos, DummyTarget dummyTarget) => TryCastGravitonWell(groundPos, (ITargetable)dummyTarget);

        /// <summary>
        /// Ultimate: Gravity Kore Collapse (GROUND_TARGET_AOE) Section 6.4
        /// สร้างหลุมดำดูดเป้าหมายและระเบิด
        /// </summary>
        public bool TryCastGravityCollapse(Vector3 groundPos, ITargetable target = null)
        {
            if (!IsAlive || UltimateRank <= 0 || UltimateCooldownRemaining > 0f) return false;
            if (!ArenaBounds.Contains(groundPos)) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            UltimateCooldownRemaining = ApplyAbilityCooldownReduction(120f - (UltimateRank * 15f));

            Vector3 toGround = groundPos - Position;
            toGround.y = 0f;
            float castDistance = Mathf.Min(toGround.magnitude, UltimateRange);
            Vector3 center = castDistance > 0.01f ? Position + toGround.normalized * castDistance : Position;

            if (target != null && target.IsAlive && (target.TeamId != TeamId || target.TeamId == -1))
            {
                float dist = Vector3.Distance(center, target.Position);
                if (dist <= UltimateRadius)
                {
                    if (target is DummyTarget dt) dt.Position = center;
                    else if (target is MinionEntity me) me.Position = center;
                    else if (target is HeroBase3D hb)
                    {
                        if (hb.TryDisplace(center)) hb.ApplyStun(1.5f);
                    }

                    float baseDmg = 260f + (UltimateRank - 1) * 130f;
                    float damage = EffectiveSkillDamage(baseDmg + EffectiveAttackDamage);
                    target.TakeDamage(damage, DamageType.Magic, HeroId);
                }
            }

            OnGravityCollapseCast?.Invoke(center, UltimateRadius);
            return true;
        }

        public bool TryCastGravityCollapse(Vector3 groundPos, DummyTarget dummyTarget) => TryCastGravityCollapse(groundPos, (ITargetable)dummyTarget);

        public override void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
        {
            if (!IsAlive || IsInvulnerable) return;

            // Passive: Antigravity Shield = 80 + 8% Max HP นาน 3 วินาที (Section 6.4)
            if (PassiveCooldownRemaining <= 0f && CurrentShield <= 0f)
            {
                CurrentShield = 80f + (EffectiveMaxHp * 0.08f);
                ShieldRemaining = 3f;
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
                    ShieldRemaining = 0f;
                }
            }

            if (remainingDamage > 0f)
            {
                base.TakeDamage(remainingDamage, damageType, attackerId);
            }
        }

        public override void SimulationTick(float deltaTime)
        {
            TickSharedSystems(deltaTime);
            if (!IsAlive) return;

            if (PassiveCooldownRemaining > 0f) PassiveCooldownRemaining -= deltaTime;
            if (ShieldRemaining > 0f)
            {
                ShieldRemaining = Mathf.Max(0f, ShieldRemaining - deltaTime);
                if (ShieldRemaining <= 0f) CurrentShield = 0f;
            }
            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (Skill3CooldownRemaining > 0f) Skill3CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;
            if (AttackCooldownRemaining > 0f) AttackCooldownRemaining = UnityEngine.Mathf.Max(0f, AttackCooldownRemaining - deltaTime);

            TickActiveWells(deltaTime);

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

        private void TickActiveWells(float deltaTime)
        {
            for (int i = _activeWells.Count - 1; i >= 0; i--)
            {
                ActiveGravitonWell well = _activeWells[i];
                well.TickTimer -= deltaTime;
                while (well.TickTimer <= 0f && well.TicksRemaining > 0)
                {
                    well.TickTimer += 0.5f;
                    well.TicksRemaining--;
                    foreach (ITargetable target in well.Targets)
                    {
                        if (target != null && target.IsAlive && Vector3.Distance(well.Center, target.Position) <= Skill3Radius + target.Radius)
                        {
                            target.TakeDamage(well.DamagePerTick, DamageType.Magic, HeroId);
                            if (target is HeroBase3D heroTarget)
                                heroTarget.ApplyMovementSlow(0.50f, 2.5f);
                        }
                    }
                }

                if (well.TicksRemaining <= 0) _activeWells.RemoveAt(i);
            }
        }
    }
}
