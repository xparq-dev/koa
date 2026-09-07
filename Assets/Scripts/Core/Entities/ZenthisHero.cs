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

        // Skill 3: Temporal Rift (Section 6.2 E)
        public const float Skill3CooldownDuration = 9.0f;
        public const float Skill3ManaCost = 65.0f;
        public const float Skill3Range = 8.0f;
        public const float Skill3Width = 1.4f;

        public const float UltimateCooldownDuration = 120.0f;
        public const float UltimateManaCost = 150.0f;

        // Timers
        public float PassiveCooldownRemaining { get; private set; } = 0f;
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float Skill2CooldownRemaining { get; private set; } = 0f;
        public float Skill3CooldownRemaining { get; private set; } = 0f;
        public float UltimateCooldownRemaining { get; private set; } = 0f;
        public float Skill2ActiveTimer { get; private set; } = 0f;
        public float AttackCooldownRemaining { get; private set; } = 0f;
        public float BaseAttackCooldown { get; set; } = 1.3f; // Ranged Mage — โจมตีช้ากว่า Vorkas

        // Grand Rewind Snapshot History (4 วินาที)
        private readonly Queue<(Vector3 Pos, float Hp)> _historySnapshots = new();
        private float _snapshotTimer = 0f;

        // Navigation
        public Vector3 TargetDestination { get; private set; }

        // Events
        public event Action<Vector3, float> OnSacredHourglassCast; // center, radius
        public event Action OnAuraOfEternityCast;
        public event Action<Vector3, Vector3, bool> OnTemporalRiftCast; // startPos, endPos, isHit
        public event Action<Vector3, float> OnGrandRewindCast; // rewindPos, restoredHp

        public ZenthisHero(Vector3 spawnPosition) : base(
            heroId: "hero_zenthis",
            displayName: "Zenthis",
            baseHp: 520f,
            baseMana: 340f,
            baseArmor: 26f,
            baseMr: 30f,
            baseAd: 44f,
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

        public override bool TryBasicAttack(ITargetable target)
        {
            if (!IsAlive || target == null || !target.IsAlive) return false;
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
            return true;
        }

        public bool TryBasicAttack(DummyTarget target) => TryBasicAttack((ITargetable)target);

        /// <summary>
        /// Skill 1: Sacred Hourglass (GROUND_TARGET_AOE) Section 6.2 & 6.5
        /// </summary>
        public bool TryCastSacredHourglass(Vector3 targetGroundPos, ITargetable target = null)
        {
            if (!IsAlive || Skill1Rank <= 0 || Skill1CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill1ManaCost)) return false;

            Skill1CooldownRemaining = Mathf.Max(6.0f, Skill1CooldownDuration - (Skill1Rank - 1) * 1.0f);

            Vector3 center = targetGroundPos;
            center.y = Position.y;

            if (target != null && target.IsAlive && (target.TeamId != TeamId || target.TeamId == -1))
            {
                float dist = Vector3.Distance(center, target.Position);
                if (dist <= Skill1Radius + target.Radius)
                {
                    float baseDmg = 70f + (Skill1Rank - 1) * 45f;
                    float ratio = 0.5f + (Skill1Rank - 1) * 0.1f;
                    float damage = baseDmg + (EffectiveAttackDamage * ratio);
                    target.TakeDamage(damage, DamageType.Magic, HeroId);
                }
            }

            OnSacredHourglassCast?.Invoke(center, Skill1Radius);
            return true;
        }

        public bool TryCastSacredHourglass(Vector3 targetGroundPos, DummyTarget dummyTarget) => TryCastSacredHourglass(targetGroundPos, (ITargetable)dummyTarget);

        /// <summary>
        /// Skill 2: Aura of Eternity (SELF_CAST) Section 6.2 & 6.5
        /// บัฟความเร็วเคลื่อนที่ ชั่วคราว
        /// </summary>
        public bool TryCastAuraOfEternity()
        {
            if (!IsAlive || Skill2Rank <= 0 || Skill2CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill2ManaCost)) return false;

            Skill2CooldownRemaining = Skill2CooldownDuration;
            Skill2ActiveTimer = 2.5f + (Skill2Rank * 0.5f);

            OnAuraOfEternityCast?.Invoke();
            return true;
        }

        /// <summary>
        /// Skill 3: Temporal Rift (SKILLSHOT_LINE) Section 6.2 E
        /// ยิงลำแสงกาลเวลาทะลวงเป็นเส้นตรงระยะ 8.0m ทำเวทดาเมจและลด Attack Speed
        /// </summary>
        public bool TryCastTemporalRift(Vector3 aimWorldPos, System.Collections.Generic.IEnumerable<ITargetable> targets = null)
        {
            if (!IsAlive || Skill3Rank <= 0 || Skill3CooldownRemaining > 0f) return false;
            if (!TryConsumeMana(Skill3ManaCost)) return false;

            float cd = Mathf.Max(5.5f, Skill3CooldownDuration - (Skill3Rank - 1) * 0.7f);
            Skill3CooldownRemaining = cd;

            Vector3 aimDir = (aimWorldPos - Position);
            aimDir.y = 0;
            if (aimDir.sqrMagnitude < 0.01f) aimDir = Rotation * Vector3.forward;
            aimDir.Normalize();

            Rotation = Quaternion.LookRotation(aimDir);
            Vector3 startPos = Position;
            Vector3 endPos = Position + (aimDir * Skill3Range);

            bool hit = false;
            float baseDmg = 85f + (Skill3Rank - 1) * 50f;
            float damage = baseDmg + (EffectiveAttackDamage * 0.70f);

            if (targets != null)
            {
                foreach (var t in targets)
                {
                    if (t != null && t.IsAlive && (t.TeamId != TeamId || t.TeamId == -1))
                    {
                        if (CheckSkillshotLineHit(startPos, endPos, Skill3Width, t.Position, t.Radius))
                        {
                            hit = true;
                            t.TakeDamage(damage, DamageType.Magic, HeroId);
                        }
                    }
                }
            }

            OnTemporalRiftCast?.Invoke(startPos, endPos, hit);
            return true;
        }

        public bool TryCastTemporalRift(Vector3 aimWorldPos, ITargetable target) => TryCastTemporalRift(aimWorldPos, target != null ? new[] { target } : null);

        /// <summary>
        /// Ultimate: Grand Rewind (SELF_CAST) Section 6.2
        /// ย้อนตำแหน่งและ HP กลับไปเมื่อ 4 วินาทีก่อน
        /// </summary>
        public bool TryCastGrandRewind()
        {
            if (!IsAlive || UltimateRank <= 0 || UltimateCooldownRemaining > 0f) return false;
            if (_historySnapshots.Count == 0) return false;
            if (!TryConsumeMana(UltimateManaCost)) return false;

            UltimateCooldownRemaining = 130f - (UltimateRank * 20f);

            var oldest = _historySnapshots.Peek();
            Position = oldest.Pos;
            CurrentHp = Mathf.Max(CurrentHp, oldest.Hp); // ไม่ลดเลือดถ้าเลือดเดิมน้อยกว่า
            TargetDestination = Position;
            IsMoving = false;

            InvokeHealthChanged();
            OnGrandRewindCast?.Invoke(Position, CurrentHp);
            return true;
        }

        public override void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
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

            base.TakeDamage(rawDamage, damageType, attackerId);
        }

        public override void SimulationTick(float deltaTime)
        {
            if (!IsAlive) return;

            Inventory.SimulationTick(deltaTime);

            // Cooldowns
            if (PassiveCooldownRemaining > 0f) PassiveCooldownRemaining -= deltaTime;
            if (Skill1CooldownRemaining > 0f) Skill1CooldownRemaining -= deltaTime;
            if (Skill2CooldownRemaining > 0f) Skill2CooldownRemaining -= deltaTime;
            if (Skill3CooldownRemaining > 0f) Skill3CooldownRemaining -= deltaTime;
            if (UltimateCooldownRemaining > 0f) UltimateCooldownRemaining -= deltaTime;
            if (Skill2ActiveTimer > 0f) Skill2ActiveTimer -= deltaTime;
            if (AttackCooldownRemaining > 0f) AttackCooldownRemaining = UnityEngine.Mathf.Max(0f, AttackCooldownRemaining - deltaTime);

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
                    float speedBonus = Skill2ActiveTimer > 0f ? (1.15f + Skill2Rank * 0.05f) : 1.0f;
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
