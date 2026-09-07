using KOA.Core.Entities;
using KOA.Data.Enums;
using System;
using UnityEngine;

namespace KOA.Core.Minions
{
    public enum MinionType
    {
        Melee = 0,
        Ranged = 1,
        Cannon = 2,
        Super = 3
    }

    /// <summary>
    /// Simulation Core สำหรับมินเนี่ยน/ครีปเลนตาม Section 3.2 และระบบเศรษฐกิจ Section 4
    /// รองรับ ITargetable, Aggro Detection, และระบบต่อสู้จำลอง (Decoupled Core Section 1.1)
    /// </summary>
    public class MinionEntity : ITargetable
    {
        public string MinionId { get; private set; }
        public string TargetId => MinionId;
        public MinionType Type { get; private set; }
        public int TeamId { get; private set; } // 0 = Blue, 1 = Red
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public float AttackDamage { get; private set; }
        public float Armor { get; private set; }
        public float MagicResist { get; private set; }
        public float MoveSpeed { get; private set; } = 5.5f;
        public float AttackRange { get; private set; }
        public float AttackCooldown { get; private set; } = 1.2f;
        public float Radius => Type switch
        {
            MinionType.Super => 1.0f,
            MinionType.Cannon => 0.75f,
            MinionType.Melee => 0.6f,
            _ => 0.5f // Ranged
        };

        public int GoldBounty { get; private set; } // 42g Melee, 50g Ranged, 65g Cannon, 90g Super
        public float ExpBounty { get; private set; } = 60f;

        public bool IsAlive => CurrentHp > 0f;

        public ITargetable CurrentTarget { get; private set; }
        private float _attackCooldownRemaining = 0f;

        public event Action<float, float> OnHealthChanged;
        public event Action<float, DamageType> OnDamageTaken;
        public event Action<Vector3> OnAttackExecuted; // targetPos
        public event Action<MinionEntity, string> OnKilled; // minion, killerId

        public MinionEntity(string id, MinionType type, int teamId, Vector3 spawnPos, int waveIndex)
        {
            MinionId = id;
            Type = type;
            TeamId = teamId;
            Position = spawnPos;

            // Evolution Scaling: +8% HP, +4% AD ทุกๆ 2.5 นาที (150 วิ)
            int evolutionStages = (waveIndex * 25) / 150;
            float hpMultiplier = 1.0f + (evolutionStages * 0.08f);
            float adMultiplier = 1.0f + (evolutionStages * 0.04f);

            switch (type)
            {
                case MinionType.Melee:
                    MaxHp = 420f * hpMultiplier;
                    AttackDamage = 12f * adMultiplier;
                    Armor = 8f;
                    MagicResist = 0f;
                    AttackRange = 1.6f;
                    GoldBounty = 42;
                    ExpBounty = 55f;
                    break;
                case MinionType.Ranged:
                    MaxHp = 280f * hpMultiplier;
                    AttackDamage = 16f * adMultiplier;
                    Armor = 0f;
                    MagicResist = 0f;
                    AttackRange = 5.5f;
                    GoldBounty = 50;
                    ExpBounty = 55f;
                    break;
                case MinionType.Cannon:
                    MaxHp = 750f * hpMultiplier;
                    AttackDamage = 35f * adMultiplier;
                    Armor = 20f;
                    MagicResist = 10f;
                    AttackRange = 6.5f;
                    AttackCooldown = 1.4f;
                    GoldBounty = 65;
                    ExpBounty = 80f;
                    break;
                case MinionType.Super:
                    MaxHp = 1600f * hpMultiplier;
                    AttackDamage = 75f * adMultiplier;
                    Armor = 35f;
                    MagicResist = 20f;
                    AttackRange = 2.0f;
                    AttackCooldown = 1.1f;
                    GoldBounty = 90;
                    ExpBounty = 120f;
                    break;
            }

            CurrentHp = MaxHp;
        }

        public void SetTarget(ITargetable target)
        {
            CurrentTarget = target;
        }

        public void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
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
                OnKilled?.Invoke(this, attackerId);
            }
        }

        public void SimulationTick(float deltaTime, Vector3 targetLaneDestination)
        {
            if (!IsAlive) return;

            if (_attackCooldownRemaining > 0f)
            {
                _attackCooldownRemaining -= deltaTime;
            }

            // ถ้ามีเป้าหมายศัตรูที่ยังมีชีวิตอยู่
            if (CurrentTarget != null && CurrentTarget.IsAlive)
            {
                Vector3 toTarget = CurrentTarget.Position - Position;
                toTarget.y = 0;
                float dist = toTarget.magnitude;

                if (dist <= AttackRange + CurrentTarget.Radius)
                {
                    // อยู่ในระยะโจมตี: หยุดเดินและหันหน้าหาเป้าหมาย
                    if (toTarget.sqrMagnitude > 0.001f)
                    {
                        Rotation = Quaternion.LookRotation(toTarget.normalized);
                    }

                    // โจมตีตาม Cooldown
                    if (_attackCooldownRemaining <= 0f)
                    {
                        CurrentTarget.TakeDamage(AttackDamage, DamageType.Physical, MinionId);
                        _attackCooldownRemaining = AttackCooldown;
                        OnAttackExecuted?.Invoke(CurrentTarget.Position);
                    }
                    return;
                }
                else if (dist <= 7.0f) // ถ้ายังอยู่ในระยะตาม
                {
                    // เดินเข้าหาศัตรู
                    Vector3 moveDir = toTarget.normalized;
                    Position += moveDir * (MoveSpeed * deltaTime);
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Rotation = Quaternion.LookRotation(moveDir);
                    }
                    return;
                }
                else
                {
                    // ศัตรูหลุดระยะ
                    CurrentTarget = null;
                }
            }
            else
            {
                CurrentTarget = null;
            }

            // เดินหน้าตามเลนไปยังจุดหมายปลายทาง Nexus
            Vector3 toLaneDest = targetLaneDestination - Position;
            toLaneDest.y = 0;
            if (toLaneDest.magnitude > 0.5f)
            {
                Vector3 moveDir = toLaneDest.normalized;
                Position += moveDir * (MoveSpeed * deltaTime);
                if (moveDir.sqrMagnitude > 0.001f)
                {
                    Rotation = Quaternion.LookRotation(moveDir);
                }
            }
        }
    }
}
