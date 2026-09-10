using KOA.Data.Enums;
using KOA.Core.World;
using System;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// ฮีโร่ Vorkas (The Iron Vanguard) ตาม Section 2.1, 2.2 และ Section 6.1
    /// รัน Simulation Logic บน 30 Ticks/sec โดยไม่ผูกกับ MonoBehaviour
    /// </summary>
    public class VorkasHero : HeroBase3D
    {
        // ค่าคงที่ของ Skill 1: Iron Cleave (Section 6.1 Q)
        public const float Skill1CooldownDuration = 8.0f;
        public const float Skill1ManaCost = 60.0f;
        public const float Skill1Range = 6.0f;
        public const float Skill1Width = 1.5f;

        // ค่าคงที่ของ Skill 2: Vanguard's Will (Section 6.1 W)
        public const float Skill2CooldownDuration = 14.0f;
        public const float Skill2ManaCost = 75.0f;
        public const float Skill2Duration = 4.0f;

        // ค่าคงที่ของ Skill 3: Seismic Slam (Section 6.1 E)
        public const float Skill3CooldownDuration = 10.0f;
        public const float Skill3ManaCost = 70.0f;
        public const float Skill3Radius = 3.5f;

        // ค่าคงที่ของ Ultimate: Rebellion Impact (Section 6.1 R)
        public const float UltimateCooldownDuration = 90.0f;
        public const float UltimateManaCost = 100.0f;
        public const float UltimateRange = 7.0f;
        public const float UltimateRadius = 3.0f;

        // การเคลื่อนที่
        public Vector3 TargetDestination { get; private set; }

        // คูลดาวน์สกิลและคูลดาวน์โจมตีปกติ
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float Skill3CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float Skill2ActiveTimer { get; private set; } = 0f;
        public float CurrentShield { get; private set; } = 0f;
        public float AttackCooldownRemaining { get; private set; } = 0f;
        public float BaseAttackCooldown { get; set; } = 1.1f;

        public override float EffectiveMoveSpeed => base.EffectiveMoveSpeed * (Skill2ActiveTimer > 0f ? 1.20f : 1.0f);

        // Events สำหรับ Presentation Layer (Visuals / SFX / UI)
        public event Action<Vector3> OnDestinationSet;
        public event Action<Vector3, Vector3, bool> OnIronCleaveExecuted; // startPos, endPos, isHit
        public event Action<float, float> OnSkill1CooldownUpdated;
        public event Action OnVanguardsWillExecuted;
        public event Action<Vector3, float, bool> OnSeismicSlamExecuted; // center, radius, hit
        public event Action<Vector3, float, bool> OnRebellionImpactExecuted; // targetPos, radius, hit

        public VorkasHero(Vector3 spawnPosition) : base(
            heroId: "hero_vorkas",
            displayName: "Vorkas",
            baseHp: 620f,
            baseMana: 280f,
            baseArmor: 38f,
            baseMr: 32f,
            baseAd: 54f,
            baseSpeed: 4.4f,
            attackRange: 2.2f,
            statGrowth: HeroStatGrowth.GetGrowthFor("Vorkas")
        )
        {
            Position = spawnPosition;
            TargetDestination = spawnPosition;
        }

        /// <summary>
        /// สั่งเคลื่อนที่ไปยังจุดหมายปลายทาง (Click-to-move ตาม Section 7.2)
        /// </summary>
        public override void SetMoveDestination(Vector3 destination)
        {
            TargetDestination = ClampMoveDestination(destination);
            IsMoving = true;
            OnDestinationSet?.Invoke(TargetDestination);
        }

        /// <summary>
        /// สั่งหยุดเดิน
        /// </summary>
        public override void StopMoving()
        {
            IsMoving = false;
            TargetDestination = Position;
        }

        /// <summary>
        /// ตรรกะโจมตีพื้นฐาน (Basic Attack) รองรับทุกเป้าหมายที่เป็น ITargetable (Section 6.1)
        /// </summary>
        public override bool TryBasicAttack(ITargetable target)
        {
            if (!CanPerformActions || target == null || !target.IsAlive) return false;
            if (target.TeamId == TeamId && target.TeamId != -1) return false; // ไม่ตีพวกเดียวกัน
            if (AttackCooldownRemaining > 0f) return false;

            float distance = Vector3.Distance(Position, target.Position);
            if (distance > (AttackRange + target.Radius))
            {
                return false; // นอกระยะโจมตี
            }

            // หันหน้าหาเป้าหมาย
            Vector3 lookDir = (target.Position - Position).normalized;
            if (lookDir.sqrMagnitude > 0.001f)
            {
                Rotation = Quaternion.LookRotation(new Vector3(lookDir.x, 0, lookDir.z));
            }

            // คำนวณดาเมจ
            target.TakeDamage(EffectiveAttackDamage, DamageType.Physical, DamageSourceId);
            AttackCooldownRemaining = EffectiveAttackCooldownFromBase(BaseAttackCooldown);
            InvokeBasicAttackExecuted(target.Position);
            return true;
        }

        public bool TryBasicAttack(DummyTarget target) => TryBasicAttack((ITargetable)target);

        /// <summary>
        /// สกิล 1: Iron Cleave (Section 6.1 & 6.5 SKILLSHOT_LINE)
        /// ยิงคลื่นดาบเป็นเส้นตรง ทำดาเมจแก่เป้าหมายในระยะ 6.0m กว้าง 1.5m
        /// </summary>
        public bool TryCastIronCleave(Vector3 aimWorldPos, System.Collections.Generic.IEnumerable<ITargetable> targets = null)
        {
            if (!IsAlive || Skill1Rank <= 0 || Skill1CooldownRemaining > 0f) return false;
            // หักมานาและตั้งคูลดาวน์
            if (!TryConsumeMana(Skill1ManaCost)) return false;
            float cd = ApplyAbilityCooldownReduction(Mathf.Max(5.0f, Skill1CooldownDuration - (Skill1Rank - 1) * 0.5f));
            Skill1CooldownRemaining = cd;
            OnSkill1CooldownUpdated?.Invoke(Skill1CooldownRemaining, cd);

            // ทิศทาง Aim Vector
            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f)
            {
                aimDir = Rotation * Vector3.forward;
            }
            aimDir.Normalize();

            // หันหน้าตามสกิล
            Rotation = Quaternion.LookRotation(aimDir);

            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * Skill1Range);

            bool hit = false;
            // Inspire MOBA: Rank 1: 75, Rank 2: 125, Rank 3: 175, Rank 4: 225
            float baseDmg = 75f + (Skill1Rank - 1) * 50f;
            float adRatio = 0.80f;
            float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * adRatio));

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null && t.IsAlive && (t.TeamId != TeamId || t.TeamId == -1))
                    {
                        if (CheckSkillshotLineHit(startPos, endPos, Skill1Width, t.Position, t.Radius))
                        {
                            hit = true;
                            t.TakeDamage(damage, DamageType.Physical, DamageSourceId);
                        }
                    }
                }
            }

            OnIronCleaveExecuted?.Invoke(startPos, endPos, hit);
            return true;
        }

        public bool TryCastIronCleave(Vector3 aimWorldPos, ITargetable target) => TryCastIronCleave(aimWorldPos, target != null ? new[] { target } : null);
        public bool TryCastIronCleave(Vector3 aimWorldPos, DummyTarget target) => TryCastIronCleave(aimWorldPos, (ITargetable)target);

        /// <summary>
        /// สกิล 2: Vanguard's Will (Section 6.1 W — SELF_CAST)
        /// ได้รับบาเรียดูดซับ 100/160/220/280 HP นาน 4.0s และวิ่งเร็วขึ้น +20%
        /// </summary>
        public bool TryCastVanguardsWill()
        {
            if (!IsAlive || Skill2Rank <= 0 || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            float cd = ApplyAbilityCooldownReduction(Mathf.Max(9.0f, Skill2CooldownDuration - (Skill2Rank - 1) * 1.0f));
            Skill2CooldownRemaining = cd;
            Skill2ActiveTimer = Skill2Duration;
            CurrentShield = 100f + (Skill2Rank - 1) * 60f;

            OnVanguardsWillExecuted?.Invoke();
            return true;
        }

        /// <summary>
        /// สกิล 3: Seismic Slam (Section 6.1 E — GROUND_TARGET_AOE)
        /// กระทืบพื้นสร้างคลื่นสั่นสะเทือนรัศมี 3.5m ทำกายภาพดาเมจ 80/130/180/230 (+60% AD) และ Slow ศัตรู 40% นาน 2.5s
        /// </summary>
        public bool TryCastSeismicSlam(Vector3 aimWorldPos, System.Collections.Generic.IEnumerable<ITargetable> targets = null)
        {
            if (!IsAlive || Skill3Rank <= 0 || Skill3CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill3ManaCost)) return false;

            float cd = ApplyAbilityCooldownReduction(Mathf.Max(6.0f, Skill3CooldownDuration - (Skill3Rank - 1) * 0.8f));
            Skill3CooldownRemaining = cd;

            Vector3 center = Position; // Self-AoE slam centered on Vorkas
            bool hit = false;
            float baseDmg = 80f + (Skill3Rank - 1) * 50f;
            float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * 0.60f));

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null && t.IsAlive && (t.TeamId != TeamId || t.TeamId == -1))
                    {
                        float dist = Vector3.Distance(center, t.Position);
                        if (dist <= Skill3Radius + t.Radius)
                        {
                            hit = true;
                            t.TakeDamage(damage, DamageType.Physical, DamageSourceId);
                            if (t is HeroBase3D heroTarget)
                                heroTarget.ApplyMovementSlow(0.40f, 2.5f);
                        }
                    }
                }
            }

            OnSeismicSlamExecuted?.Invoke(center, Skill3Radius, hit);
            return true;
        }

        public bool TryCastSeismicSlam(Vector3 aimWorldPos, ITargetable target) => TryCastSeismicSlam(aimWorldPos, target != null ? new[] { target } : null);

        /// <summary>
        /// สกิลอัลติเมท (R): Rebellion Impact (Section 6.1 R — GROUND_TARGET_AOE)
        /// พุ่งกระโดดฟาดดาบลงพื้นระยะ 7.0m ทำดาเมจ 250/375/500 (+120% AD) และ Knockup ศัตรูในระยะ 3.0m
        /// </summary>
        public bool TryCastRebellionImpact(Vector3 aimWorldPos, System.Collections.Generic.IEnumerable<ITargetable> targets = null)
        {
            if (!IsAlive || UltimateRank <= 0 || UltimateCooldownRemaining > 0f) return false;
            if (!ArenaBounds.Contains(aimWorldPos)) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            float cd = ApplyAbilityCooldownReduction(Mathf.Max(60.0f, UltimateCooldownDuration - (UltimateRank - 1) * 10.0f));
            UltimateCooldownRemaining = cd;

            Vector3 toAim = aimWorldPos - Position;
            toAim.y = 0f;
            float targetDist = Mathf.Min(toAim.magnitude, UltimateRange);
            Vector3 landingPos = targetDist > 0.1f ? Position + (toAim.normalized * targetDist) : Position;
            Position = landingPos; // พุ่งเข้าจุดเป้าหมาย

            bool hit = false;
            float baseDmg = 250f + (UltimateRank - 1) * 125f;
            float damage = EffectiveSkillDamage(baseDmg + (EffectiveAttackDamage * 1.20f));

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null && t.IsAlive && (t.TeamId != TeamId || t.TeamId == -1))
                    {
                        float dist = Vector3.Distance(landingPos, t.Position);
                        if (dist <= UltimateRadius + t.Radius)
                        {
                            hit = true;
                            t.TakeDamage(damage, DamageType.Physical, DamageSourceId);
                            if (t is HeroBase3D heroTarget)
                                heroTarget.ApplyStun(1.0f);
                        }
                    }
                }
            }

            OnRebellionImpactExecuted?.Invoke(landingPos, UltimateRadius, hit);
            return true;
        }

        public bool TryCastRebellionImpact(Vector3 aimWorldPos, ITargetable target) => TryCastRebellionImpact(aimWorldPos, target != null ? new[] { target } : null);

        public override void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
        {
            if (!IsAlive || IsInvulnerable) return;
            if (damageType == DamageType.Magic)
                rawDamage *= 0.85f; // Passive Anti-Energy Aura: ลด Magic Damage 15% (Section 6.1)

            if (CurrentShield > 0f)
            {
                if (CurrentShield >= rawDamage)
                {
                    CurrentShield -= rawDamage;
                    return;
                }
                else
                {
                    rawDamage -= CurrentShield;
                    CurrentShield = 0f;
                }
            }
            base.TakeDamage(rawDamage, damageType, attackerId);
        }


        /// <summary>
        /// การจำลองฟิสิกส์และการเคลื่อนที่ต่อ Tick (Section 1.1: 30 Ticks/sec)
        /// </summary>
        public override void SimulationTick(float deltaTime)
        {
            TickSharedSystems(deltaTime);
            if (!IsAlive) return;

            // นับถอยหลังคูลดาวน์สกิล 1-3 & Ultimate
            if (Skill1CooldownRemaining > 0f)
            {
                Skill1CooldownRemaining = Mathf.Max(0f, Skill1CooldownRemaining - deltaTime);
                OnSkill1CooldownUpdated?.Invoke(Skill1CooldownRemaining, Skill1CooldownDuration);
            }
            if (Skill2CooldownRemaining > 0f)
            {
                Skill2CooldownRemaining = Mathf.Max(0f, Skill2CooldownRemaining - deltaTime);
            }
            if (Skill3CooldownRemaining > 0f)
            {
                Skill3CooldownRemaining = Mathf.Max(0f, Skill3CooldownRemaining - deltaTime);
            }
            if (UltimateCooldownRemaining > 0f)
            {
                UltimateCooldownRemaining = Mathf.Max(0f, UltimateCooldownRemaining - deltaTime);
            }

            if (Skill2ActiveTimer > 0f)
            {
                Skill2ActiveTimer = Mathf.Max(0f, Skill2ActiveTimer - deltaTime);
                if (Skill2ActiveTimer <= 0f) CurrentShield = 0f;
            }

            // นับถอยหลังคูลดาวน์โจมตี
            if (AttackCooldownRemaining > 0f)
            {
                AttackCooldownRemaining = Mathf.Max(0f, AttackCooldownRemaining - deltaTime);
            }

            if (IsStunned) return;

            // ประมวลผลการเดิน Click-to-move
            if (IsMoving)
            {
                Vector3 toTarget = TargetDestination - Position;
                toTarget.y = 0;
                float distance = toTarget.magnitude;

                if (distance <= 0.08f)
                {
                    Position = TargetDestination;
                    IsMoving = false;
                }
                else
                {
                    Vector3 moveDir = toTarget / distance;
                    float moveStep = EffectiveMoveSpeed * deltaTime;
                    if (moveStep >= distance)
                    {
                        Position = TargetDestination;
                        IsMoving = false;
                    }
                    else
                    {
                        Position += moveDir * moveStep;
                    }

                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Rotation = Quaternion.LookRotation(moveDir);
                    }
                }
            }
        }
    }
}
