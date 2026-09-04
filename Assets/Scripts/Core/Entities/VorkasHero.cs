using KOA.Data.Enums;
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
        // ค่าคงที่ของ Skill 1: Iron Cleave (Section 6.1)
        public const float Skill1CooldownDuration = 8.0f;
        public const float Skill1ManaCost = 60.0f;
        public const float Skill1Range = 6.0f;
        public const float Skill1Width = 1.5f;
        public const float Skill1BaseDamage = 120.0f;
        public const float Skill1AdRatio = 0.8f;

        // การเคลื่อนที่
        public Vector3 TargetDestination { get; private set; }
        public bool IsMoving { get; private set; }

        // คูลดาวน์สกิลและคูลดาวน์โจมตีปกติ
        public float Skill1CooldownRemaining { get; private set; } = 0f;
        public float AttackCooldownRemaining { get; private set; } = 0f;
        public float BaseAttackCooldown { get; set; } = 1.1f;

        // Events สำหรับ Presentation Layer (Visuals / SFX / UI)
        public event Action<Vector3> OnDestinationSet;
        public event Action<Vector3> OnBasicAttackExecuted;
        public event Action<Vector3, Vector3, bool> OnIronCleaveExecuted; // startPos, endPos, isHit
        public event Action<float, float> OnSkill1CooldownUpdated;

        public VorkasHero(Vector3 spawnPosition) : base(
            heroId: "hero_vorkas",
            displayName: "Vorkas",
            baseHp: 620f,
            baseMana: 280f,
            baseArmor: 38f,
            baseMr: 32f,
            baseAd: 64f,
            baseSpeed: 7.2f,
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
            destination.y = Position.y; // ล็อกแกน Y ให้อยู่บนระนาบสนาม
            TargetDestination = destination;
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
        /// ตรรกะโจมตีพื้นฐาน (Basic Attack)
        /// </summary>
        public bool TryBasicAttack(DummyTarget target)
        {
            if (!IsAlive || target == null || !target.IsAlive) return false;
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
            target.TakeDamage(EffectiveAttackDamage, DamageType.Physical);
            AttackCooldownRemaining = BaseAttackCooldown;
            OnBasicAttackExecuted?.Invoke(target.Position);
            return true;
        }

        /// <summary>
        /// สกิล 1: Iron Cleave (Section 6.1 & 6.5 SKILLSHOT_LINE)
        /// ยิงคลื่นดาบเป็นเส้นตรง ทำดาเมจแก่เป้าหมายในระยะ 6.0m กว้าง 1.5m
        /// </summary>
        public bool TryCastIronCleave(Vector3 aimWorldPos, DummyTarget target)
        {
            if (!IsAlive) return false;
            if (Skill1CooldownRemaining > 0f) return false;
            if (CurrentMana < Skill1ManaCost) return false;

            // หักมานาและตั้งคูลดาวน์
            CurrentMana -= Skill1ManaCost;
            Skill1CooldownRemaining = Skill1CooldownDuration;
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
            OnSkill1CooldownUpdated?.Invoke(Skill1CooldownRemaining, Skill1CooldownDuration);

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
            if (target != null && target.IsAlive)
            {
                hit = CheckSkillshotLineHit(startPos, endPos, Skill1Width, target.Position, target.Radius);
                if (hit)
                {
                    float damage = Skill1BaseDamage + (EffectiveAttackDamage * Skill1AdRatio);
                    target.TakeDamage(damage, DamageType.Physical);
                }
            }

            OnIronCleaveExecuted?.Invoke(startPos, endPos, hit);
            return true;
        }

        /// <summary>
        /// ตรวจสอบการชนของเส้นทาง Skillshot Line กับวัตถุทรงกลม
        /// </summary>
        private static bool CheckSkillshotLineHit(Vector3 lineStart, Vector3 lineEnd, float lineWidth, Vector3 targetPos, float targetRadius)
        {
            Vector3 lineDir = lineEnd - lineStart;
            float lineLength = lineDir.magnitude;
            if (lineLength <= 0.001f) return false;

            Vector3 lineNorm = lineDir / lineLength;
            Vector3 toTarget = targetPos - lineStart;
            float projection = Vector3.Dot(toTarget, lineNorm);

            // เป้าหมายอยู่นอกช่วงความยาวเส้น
            if (projection < 0f || projection > lineLength)
            {
                // ตรวจสอบปลายจุด
                float distStart = Vector3.Distance(lineStart, targetPos);
                float distEnd = Vector3.Distance(lineEnd, targetPos);
                return Mathf.Min(distStart, distEnd) <= (lineWidth * 0.5f + targetRadius);
            }

            // ระยะห่างตั้งฉากจากเส้นถึงจุดกึ่งกลางเป้าหมาย
            Vector3 closestPoint = lineStart + (lineNorm * projection);
            float distanceToLine = Vector3.Distance(closestPoint, targetPos);
            return distanceToLine <= ((lineWidth * 0.5f) + targetRadius);
        }

        /// <summary>
        /// การจำลองฟิสิกส์และการเคลื่อนที่ต่อ Tick (Section 1.1: 30 Ticks/sec)
        /// </summary>
        public override void SimulationTick(float deltaTime)
        {
            if (!IsAlive) return;

            // นับถอยหลังคูลดาวน์สกิล
            if (Skill1CooldownRemaining > 0f)
            {
                Skill1CooldownRemaining = Mathf.Max(0f, Skill1CooldownRemaining - deltaTime);
                OnSkill1CooldownUpdated?.Invoke(Skill1CooldownRemaining, Skill1CooldownDuration);
            }

            // นับถอยหลังคูลดาวน์โจมตี
            if (AttackCooldownRemaining > 0f)
            {
                AttackCooldownRemaining = Mathf.Max(0f, AttackCooldownRemaining - deltaTime);
            }

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
