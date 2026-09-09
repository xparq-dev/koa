using KOA.Data.Enums;
using KOA.Core.World;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// สถิติการเติบโตต่อเลเวลของฮีโร่ตาม Section 2.2
    /// </summary>
    [Serializable]
    public struct HeroStatGrowth
    {
        public float HpPerLevel;
        public float ArmorPerLevel;
        public float MrPerLevel;
        public float AdPerLevel;
        public float ManaPerLevel;

        public static HeroStatGrowth GetGrowthFor(string heroName)
        {
            return heroName switch
            {
                "Vorkas" => new HeroStatGrowth { HpPerLevel = 45f, ArmorPerLevel = 2.0f, MrPerLevel = 1.0f, AdPerLevel = 3.0f, ManaPerLevel = 15f },
                "Zenthis" => new HeroStatGrowth { HpPerLevel = 30f, ArmorPerLevel = 1.0f, MrPerLevel = 1.5f, AdPerLevel = 2.5f, ManaPerLevel = 25f },
                "Korvax" => new HeroStatGrowth { HpPerLevel = 28f, ArmorPerLevel = 1.0f, MrPerLevel = 1.0f, AdPerLevel = 4.0f, ManaPerLevel = 18f },
                "Gravitor" => new HeroStatGrowth { HpPerLevel = 55f, ArmorPerLevel = 2.5f, MrPerLevel = 2.5f, AdPerLevel = 2.0f, ManaPerLevel = 20f },
                _ => default
            };
        }
    }

    /// <summary>
    /// Abstract Base Class สำหรับ Hero 3D Logic (Section 2.1)
    /// ทำงานใน Simulation Core โดยแยกออกจาก Presentation Layer (Decoupled Core Paradigm)
    /// </summary>
    public abstract class HeroBase3D : ITargetable
    {
        public const int MaxLevel = 12; // Level Cap สำหรับ 1.0.0 (Section 2.2)
        public const float PassiveManaRegenerationPercentPerSecond = 0.0125f;

        // ข้อมูลระบุตัวตนและ ITargetable
        public string HeroId { get; protected set; }
        public string TargetId => HeroId;
        public string DisplayName { get; protected set; }
        public int TeamId { get; set; } = 0; // 0 = Blue (Player), 1 = Red (Bot)
        public float Radius { get; set; } = 0.8f;
        public float MaxHp => EffectiveMaxHp;

        // เลเวลและค่าประสบการณ์ (Section 2.2)
        public int CurrentLevel { get; protected set; } = 1;
        public float CurrentExp { get; protected set; } = 0f;
        public float RequiredExp => CalculateRequiredExp(CurrentLevel);

        // ค่าสถิติพื้นฐาน (Base Stats)
        public float BaseMaxHp { get; protected set; }
        public float BaseMaxMana { get; protected set; }
        public float BaseArmor { get; protected set; }
        public float BaseMagicResist { get; protected set; }
        public float BaseAttackDamage { get; protected set; }
        public float BaseMoveSpeed { get; protected set; }
        public float AttackRange { get; protected set; }

        // ค่าการเติบโตต่อเลเวล (Section 2.2)
        public HeroStatGrowth StatGrowth { get; protected set; }

        // สถานะปัจจุบันใน Simulation Core
        public float CurrentHp { get; protected set; }
        public float CurrentMana { get; protected set; }
        private Vector3 _position;
        public Vector3 Position
        {
            get => _position;
            set => _position = ArenaBounds.Clamp(value, Radius);
        }
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public bool IsAlive => CurrentHp > 0f;
        public bool IsMoving { get; protected set; }
        public ITargetable CurrentAttackTarget { get; set; }

        // ระบบช่องเก็บของ 6 ช่อง (Section 5.1)
        public KOA.Core.Items.Inventory Inventory { get; } = new KOA.Core.Items.Inventory();

        // ระบบแต้มสกิลและระดับสกิลตาม MOBA Standard
        public int AvailableSkillPoints { get; protected set; } = 1;
        public int Skill1Rank { get; protected set; } = 0; // Max 4 (Q)
        public int Skill2Rank { get; protected set; } = 0; // Max 4 (W)
        public int Skill3Rank { get; protected set; } = 0; // Max 4 (E)
        public int UltimateRank { get; protected set; } = 0; // Max 3 (R - Unlocks at Lv 6, 10, 12)
        public int AvailableAttributePoints { get; protected set; }
        public int VitalityPoints { get; protected set; }
        public int FocusPoints { get; protected set; }
        public int ArmorPoints { get; protected set; }
        public int ResolvePoints { get; protected set; }

        // Talent Tree (Lv 4, 8, 12) (-1 = Unchosen, 0 = Option A, 1 = Option B)
        public int TalentTier1Choice { get; protected set; } = -1; // Lv 4: A +75 HP, B +10% CDR
        public int TalentTier2Choice { get; protected set; } = -1; // Lv 8: A +15% Attack Speed, B +15% Skill Damage
        public int TalentTier3Choice { get; protected set; } = -1; // Lv 12: A +20% Move Speed, B -10% Damage Taken

        // สถิติสุทธิหลังรวมการเติบโตต่อเลเวล, ไอเทม, Attribute Points และ Talents (Section 6.6-6.7)
        private float MaxHpBeforeAttributes => BaseMaxHp + (StatGrowth.HpPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxHp + (TalentTier1Choice == 0 ? 75f : 0f);
        private float MaxManaBeforeAttributes => BaseMaxMana + (StatGrowth.ManaPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxMana;
        public float EffectiveMaxHp => MaxHpBeforeAttributes * Mathf.Pow(1.02f, VitalityPoints);
        public float EffectiveMaxMana => MaxManaBeforeAttributes * Mathf.Pow(1.02f, FocusPoints);
        public float ManaRegenerationPerSecond => EffectiveMaxMana * PassiveManaRegenerationPercentPerSecond;
        public float EffectiveArmor => BaseArmor + (StatGrowth.ArmorPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusArmor + ArmorPoints;
        public float EffectiveMagicResist => BaseMagicResist + (StatGrowth.MrPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMagicResist + ResolvePoints;
        public virtual float EffectiveAttackDamage => BaseAttackDamage + (StatGrowth.AdPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusAttackDamage;
        public virtual float EffectiveMoveSpeed => (BaseMoveSpeed + Inventory.TotalBonusMoveSpeed) * (TalentTier3Choice == 0 ? 1.20f : 1f) * (1f - MovementSlowPercent);
        public float EffectiveCooldownReduction => (TalentTier1Choice == 1 ? 0.10f : 0f) + Inventory.TotalBonusCooldownReduction;
        /// <summary>คูลดาวน์โจมตีปกติหลังคิดรวม Attack Rate Bonus จากไอเทมและ Talent Lv 8 A (Section 5.2, 6.6)</summary>
        public float EffectiveAttackCooldownFromBase(float baseAttackCooldown)
        {
            float rateBonus = Inventory.TotalBonusAttackRatePercent + (TalentTier2Choice == 0 ? 0.15f : 0f) + AdditionalAttackRatePercent;
            float attackSpeedMultiplier = Mathf.Max(0.1f, (1f + rateBonus) * (1f - AttackSpeedSlowPercent));
            return baseAttackCooldown / attackSpeedMultiplier;
        }

        public float ApplyAbilityCooldownReduction(float cooldown)
        {
            return cooldown * (1f - Mathf.Clamp(EffectiveCooldownReduction, 0f, 0.40f));
        }

        public float EffectiveSkillDamage(float rawDamage)
        {
            return rawDamage * (TalentTier2Choice == 1 ? 1.15f : 1f);
        }

        // Debuff และ Crowd Control state อยู่ใน Simulation Core เพื่อให้ Item/Ability ไม่ผูกกับ View (Section 1, 5.2, 6)
        public float MovementSlowPercent { get; private set; }
        public float MovementSlowRemaining { get; private set; }
        public float AttackSpeedSlowPercent { get; private set; }
        public float AttackSpeedSlowRemaining { get; private set; }
        public float StunRemaining { get; private set; }
        public float CrowdControlImmunityRemaining { get; private set; }
        public float InvulnerabilityRemaining { get; private set; }
        public bool IsStunned => StunRemaining > 0f;
        public bool IsCrowdControlImmune => CrowdControlImmunityRemaining > 0f;
        public bool IsInvulnerable => InvulnerabilityRemaining > 0f;
        public bool CanPerformActions => IsAlive && !IsStunned;
        public bool HasDebuff => MovementSlowRemaining > 0f || AttackSpeedSlowRemaining > 0f || StunRemaining > 0f || _armorShredByAttacker.Count > 0;
        protected virtual float AdditionalAttackRatePercent => 0f;

        private readonly Dictionary<string, float> _armorShredByAttacker = new Dictionary<string, float>();

        // ค่าสถานะ Fountain Zone
        public bool IsInFountainZone { get; set; } = false;

        // Events สำหรับ Presentation Layer เพื่ออัปเดต UI/Billboard โดยไม่ผูกติดกัน (Decoupled Core)
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSkillPointsChanged;
        public event Action<int> OnAttributePointsChanged;
        public event Action OnSkillProgressionChanged;
        public event Action<Vector3> OnBasicAttackExecuted;
        public event Action OnDied;
        public event Action OnRespawned;

        protected void InvokeBasicAttackExecuted(Vector3 targetPosition)
        {
            OnBasicAttackExecuted?.Invoke(targetPosition);
        }

        protected void InvokeHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);
        }

        protected void InvokeManaChanged()
        {
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
        }

        public bool TryConsumeMana(float cost)
        {
            if (!CanPerformActions) return false;
            if (CurrentMana < cost) return false;
            CurrentMana -= cost;
            InvokeManaChanged();
            return true;
        }

        public void RestoreHealth(float amount)
        {
            CurrentHp = Mathf.Min(EffectiveMaxHp, CurrentHp + amount);
            InvokeHealthChanged();
        }

        /// <summary>
        /// เรียกทุก Tick ขณะอยู่ใน Fountain Zone: ฟื้น HP/Mana ~11% MaxHP-MaxMana ต่อวินาที (ประมาณ 8-10 วิเต็มหลอดตาม MOBA Standard)
        /// </summary>
        public void FountainZoneTick(float deltaTime)
        {
            if (!IsAlive) return;
            float hpRegen = EffectiveMaxHp * 0.11f * deltaTime;
            float manaRegen = EffectiveMaxMana * 0.11f * deltaTime;
            CurrentHp = Mathf.Min(EffectiveMaxHp, CurrentHp + hpRegen);
            CurrentMana = Mathf.Min(EffectiveMaxMana, CurrentMana + manaRegen);
            InvokeHealthChanged();
            InvokeManaChanged();
        }

        public void SetHealthExplicit(float newHp)
        {
            CurrentHp = Mathf.Clamp(newHp, 0f, EffectiveMaxHp);
            InvokeHealthChanged();
        }

        // ==========================================
        // MOBA STANDARD SKILL PROGRESSION & TALENTS
        // ==========================================

        public bool CanLevelSkill1() => AvailableSkillPoints > 0 && Skill1Rank < 4;
        public bool CanLevelSkill2() => AvailableSkillPoints > 0 && Skill2Rank < 4;
        public bool CanLevelSkill3() => AvailableSkillPoints > 0 && Skill3Rank < 4;
        public bool CanLevelUltimate() => AvailableSkillPoints > 0 && UltimateRank < 3 && ((UltimateRank == 0 && CurrentLevel >= 6) || (UltimateRank == 1 && CurrentLevel >= 10) || (UltimateRank == 2 && CurrentLevel >= 12));

        public bool TryLevelSkill1()
        {
            if (AvailableSkillPoints <= 0 || Skill1Rank >= 4) return false;
            Skill1Rank++;
            AvailableSkillPoints--;
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TryLevelSkill2()
        {
            if (AvailableSkillPoints <= 0 || Skill2Rank >= 4) return false;
            Skill2Rank++;
            AvailableSkillPoints--;
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TryLevelSkill3()
        {
            if (AvailableSkillPoints <= 0 || Skill3Rank >= 4) return false;
            Skill3Rank++;
            AvailableSkillPoints--;
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TryLevelUltimate()
        {
            if (AvailableSkillPoints <= 0 || UltimateRank >= 3) return false;
            // MOBA Standard: Ultimate unlocks at Level 6, 10, 12
            if (UltimateRank == 0 && CurrentLevel < 6) return false;
            if (UltimateRank == 1 && CurrentLevel < 10) return false;
            if (UltimateRank == 2 && CurrentLevel < 12) return false;

            UltimateRank++;
            AvailableSkillPoints--;
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TrySpendAttributePoint(HeroAttribute attribute)
        {
            if (AvailableAttributePoints <= 0) return false;

            float previousMaxHp = EffectiveMaxHp;
            float previousMaxMana = EffectiveMaxMana;
            switch (attribute)
            {
                case HeroAttribute.Vitality:
                    VitalityPoints++;
                    break;
                case HeroAttribute.Focus:
                    FocusPoints++;
                    break;
                case HeroAttribute.Armor:
                    ArmorPoints++;
                    break;
                case HeroAttribute.Resolve:
                    ResolvePoints++;
                    break;
                default:
                    return false;
            }

            AvailableAttributePoints--;
            CurrentHp += EffectiveMaxHp - previousMaxHp;
            CurrentMana += EffectiveMaxMana - previousMaxMana;
            InvokeHealthChanged();
            InvokeManaChanged();
            OnAttributePointsChanged?.Invoke(AvailableAttributePoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TrySelectTalent(int tier, int optionIndex)
        {
            if (optionIndex < 0 || optionIndex > 1) return false;
            if (tier == 1 && CurrentLevel >= 4 && TalentTier1Choice == -1)
            {
                float previousMaxHp = EffectiveMaxHp;
                TalentTier1Choice = optionIndex;
                CurrentHp += EffectiveMaxHp - previousMaxHp;
                InvokeHealthChanged();
                OnSkillProgressionChanged?.Invoke();
                return true;
            }
            if (tier == 2 && CurrentLevel >= 8 && TalentTier2Choice == -1)
            {
                TalentTier2Choice = optionIndex;
                OnSkillProgressionChanged?.Invoke();
                return true;
            }
            if (tier == 3 && CurrentLevel >= 12 && TalentTier3Choice == -1)
            {
                TalentTier3Choice = optionIndex;
                OnSkillProgressionChanged?.Invoke();
                return true;
            }
            return false;
        }

        protected HeroBase3D(string heroId, string displayName, float baseHp, float baseMana, float baseArmor, float baseMr, float baseAd, float baseSpeed, float attackRange, HeroStatGrowth statGrowth)
        {
            HeroId = heroId;
            DisplayName = displayName;
            BaseMaxHp = baseHp;
            BaseMaxMana = baseMana;
            BaseArmor = baseArmor;
            BaseMagicResist = baseMr;
            BaseAttackDamage = baseAd;
            BaseMoveSpeed = baseSpeed;
            AttackRange = attackRange;
            StatGrowth = statGrowth;

            CurrentLevel = 1;
            CurrentHp = EffectiveMaxHp;
            CurrentMana = EffectiveMaxMana;

            Inventory.OnStatsRecalculated += () =>
            {
                OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);
                OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
            };
            Inventory.OnActiveUsed += HandleActiveItemUsed;
        }

        private void HandleActiveItemUsed(int slotIndex, KOA.Data.Models.ItemData item)
        {
            if (item != null && item.ActiveEffect == ItemActiveEffect.CleanseAndCrowdControlImmunity)
            {
                ClearDebuffsAndGrantCrowdControlImmunity(1.0f);
            }
        }

        public bool ApplyMovementSlow(float percent, float duration)
        {
            if (!IsAlive || CrowdControlImmunityRemaining > 0f || percent <= 0f || duration <= 0f) return false;
            MovementSlowPercent = Mathf.Max(MovementSlowPercent, Mathf.Clamp(percent, 0f, 0.9f));
            MovementSlowRemaining = Mathf.Max(MovementSlowRemaining, duration);
            return true;
        }

        public bool ApplyAttackSpeedSlow(float percent, float duration)
        {
            if (!IsAlive || CrowdControlImmunityRemaining > 0f || percent <= 0f || duration <= 0f) return false;
            AttackSpeedSlowPercent = Mathf.Max(AttackSpeedSlowPercent, Mathf.Clamp(percent, 0f, 0.9f));
            AttackSpeedSlowRemaining = Mathf.Max(AttackSpeedSlowRemaining, duration);
            return true;
        }

        public bool ApplyStun(float duration)
        {
            if (!IsAlive || CrowdControlImmunityRemaining > 0f || duration <= 0f) return false;
            StunRemaining = Mathf.Max(StunRemaining, duration);
            return true;
        }

        public bool TryDisplace(Vector3 newPosition)
        {
            if (!IsAlive || IsCrowdControlImmune) return false;
            newPosition.y = Position.y;
            Position = newPosition;
            return true;
        }

        /// <summary>จำกัดจุดหมายให้อยู่ในสนามตาม Section 3.1 ก่อนเริ่มเดิน</summary>
        protected Vector3 ClampMoveDestination(Vector3 destination)
        {
            destination.y = Position.y;
            return ArenaBounds.Clamp(destination, Radius);
        }

        public void ClearDebuffsAndGrantCrowdControlImmunity(float immunityDuration)
        {
            ClearDebuffs();
            CrowdControlImmunityRemaining = Mathf.Max(CrowdControlImmunityRemaining, Mathf.Max(0f, immunityDuration));
        }

        public void ClearDebuffs()
        {
            MovementSlowPercent = 0f;
            MovementSlowRemaining = 0f;
            AttackSpeedSlowPercent = 0f;
            AttackSpeedSlowRemaining = 0f;
            StunRemaining = 0f;
            _armorShredByAttacker.Clear();
        }

        public void GrantInvulnerability(float duration)
        {
            InvulnerabilityRemaining = Mathf.Max(InvulnerabilityRemaining, Mathf.Max(0f, duration));
        }

        public void ApplyArmorShred(string attackerId, float percent)
        {
            if (string.IsNullOrEmpty(attackerId) || !IsAlive || IsCrowdControlImmune) return;
            _armorShredByAttacker[attackerId] = Mathf.Clamp(percent, 0f, 0.90f);
        }

        public void RemoveArmorShred(string attackerId)
        {
            if (!string.IsNullOrEmpty(attackerId)) _armorShredByAttacker.Remove(attackerId);
        }

        public float GetArmorShred(string attackerId)
        {
            return !string.IsNullOrEmpty(attackerId) && _armorShredByAttacker.TryGetValue(attackerId, out float value) ? value : 0f;
        }

        protected void TickSharedSystems(float deltaTime)
        {
            Inventory.SimulationTick(deltaTime);

            // Passive resource recovery keeps the duel loop playable between fountain visits.
            // This remains deterministic Simulation Core state; Presentation only reads the result.
            if (IsAlive && !IsInFountainZone && CurrentMana < EffectiveMaxMana)
            {
                CurrentMana = Mathf.Min(EffectiveMaxMana, CurrentMana + ManaRegenerationPerSecond * deltaTime);
                InvokeManaChanged();
            }

            if (CrowdControlImmunityRemaining > 0f)
                CrowdControlImmunityRemaining = Mathf.Max(0f, CrowdControlImmunityRemaining - deltaTime);

            if (InvulnerabilityRemaining > 0f)
                InvulnerabilityRemaining = Mathf.Max(0f, InvulnerabilityRemaining - deltaTime);

            if (MovementSlowRemaining > 0f)
            {
                MovementSlowRemaining = Mathf.Max(0f, MovementSlowRemaining - deltaTime);
                if (MovementSlowRemaining <= 0f) MovementSlowPercent = 0f;
            }

            if (AttackSpeedSlowRemaining > 0f)
            {
                AttackSpeedSlowRemaining = Mathf.Max(0f, AttackSpeedSlowRemaining - deltaTime);
                if (AttackSpeedSlowRemaining <= 0f) AttackSpeedSlowPercent = 0f;
            }

            if (StunRemaining > 0f)
                StunRemaining = Mathf.Max(0f, StunRemaining - deltaTime);
        }

        /// <summary>
        /// สูตรคำนวณ EXP สะสมที่ต้องใช้เพื่อขึ้น Level ถัดไป: EXP = 200 * (level ^ 1.3) (Section 2.2)
        /// </summary>
        public static float CalculateRequiredExp(int level)
        {
            if (level >= MaxLevel) return 0f;
            return 200f * Mathf.Pow(level, 1.3f);
        }

        /// <summary>
        /// สูตรคำนวณเวลาเกิดใหม่: RespawnTime (seconds) = 4 + (CurrentLevel * 2.5) (Section 2.3)
        /// </summary>
        public float CalculateRespawnTime()
        {
            return 4f + (CurrentLevel * 2.5f);
        }

        /// <summary>
        /// รับ EXP และทำการ Level up หากค่าถึงกำหนด (Section 2.2)
        /// </summary>
        public virtual void AddExp(float expAmount)
        {
            if (CurrentLevel >= MaxLevel || expAmount <= 0f) return;

            CurrentExp += expAmount;
            while (CurrentLevel < MaxLevel && CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                LevelUp();
            }

            OnExpChanged?.Invoke(CurrentExp, RequiredExp);
        }

        protected virtual void LevelUp()
        {
            CurrentLevel++;
            AvailableSkillPoints++;
            AvailableAttributePoints++;

            // ฮีล HP/Mana ส่วนที่เพิ่มขึ้นมา
            CurrentHp += StatGrowth.HpPerLevel;
            CurrentMana += StatGrowth.ManaPerLevel;

            OnLevelUp?.Invoke(CurrentLevel);
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnAttributePointsChanged?.Invoke(AvailableAttributePoints);
            OnSkillProgressionChanged?.Invoke();
            OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
        }

        public event Action<float, DamageType> OnDamageTaken;

        /// <summary>
        /// คำนวณความเสียหายที่ได้รับตามเกราะและค่าต้านทานเวท (Section 6)
        /// </summary>
        public virtual void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null)
        {
            if (!IsAlive || IsInvulnerable) return;

            float netDamage = rawDamage;
            if (damageType == DamageType.Physical)
            {
                // Damage Reduction = Armor / (100 + Armor)
                float armorAfterShred = EffectiveArmor * (1f - GetArmorShred(attackerId));
                float reduction = armorAfterShred / (100f + Mathf.Max(0f, armorAfterShred));
                netDamage = rawDamage * (1f - reduction);
            }
            else if (damageType == DamageType.Magic)
            {
                // Damage Reduction = MR / (100 + MR)
                float reduction = EffectiveMagicResist / (100f + Mathf.Max(0f, EffectiveMagicResist));
                netDamage = rawDamage * (1f - reduction);
            }

            if (TalentTier3Choice == 1)
                netDamage *= 0.90f;

            CurrentHp = Mathf.Max(0f, CurrentHp - netDamage);
            OnDamageTaken?.Invoke(netDamage, damageType);
            OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);

            if (CurrentHp <= 0f)
            {
                Die();
            }
        }

        protected virtual void Die()
        {
            OnDied?.Invoke();
        }

        public virtual void Respawn(Vector3 respawnPosition)
        {
            Position = respawnPosition;
            CurrentHp = EffectiveMaxHp;
            CurrentMana = EffectiveMaxMana;
            ClearDebuffsAndGrantCrowdControlImmunity(0f);
            CrowdControlImmunityRemaining = 0f;
            InvulnerabilityRemaining = 0f;
            OnRespawned?.Invoke();
            OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
        }

        /// <summary>
        /// สั่งเคลื่อนที่ไปยังจุดหมายปลายทาง (Click-to-move Section 7.2)
        /// </summary>
        public abstract void SetMoveDestination(Vector3 destination);

        /// <summary>
        /// สั่งหยุดเดิน
        /// </summary>
        public abstract void StopMoving();

        /// <summary>
        /// Tick ฟังก์ชันสำหรับ Simulation Core (รันที่ 30 Ticks/sec ตาม Section 1.1)
        /// </summary>
        public abstract void SimulationTick(float deltaTime);

        /// <summary>
        /// ตรรกะโจมตีพื้นฐาน (Basic Attack) รองรับทุกเป้าหมายที่เป็น ITargetable
        /// </summary>
        public abstract bool TryBasicAttack(ITargetable target);

        /// <summary>
        /// ตรวจสอบการชนของเส้นทาง Skillshot Line กับเป้าหมายทรงกลม (Section 6.5)
        /// </summary>
        public static bool CheckSkillshotLineHit(Vector3 lineStart, Vector3 lineEnd, float lineWidth, Vector3 targetPos, float targetRadius)
        {
            Vector3 lineDir = lineEnd - lineStart;
            float lineLength = lineDir.magnitude;
            if (lineLength <= 0.001f) return false;

            Vector3 lineNorm = lineDir / lineLength;
            Vector3 toTarget = targetPos - lineStart;
            float projection = Vector3.Dot(toTarget, lineNorm);

            if (projection < 0f || projection > lineLength)
            {
                float distStart = Vector3.Distance(lineStart, targetPos);
                float distEnd = Vector3.Distance(lineEnd, targetPos);
                return Mathf.Min(distStart, distEnd) <= (lineWidth * 0.5f + targetRadius);
            }

            Vector3 closestPoint = lineStart + (lineNorm * projection);
            float distanceToLine = Vector3.Distance(closestPoint, targetPos);
            return distanceToLine <= ((lineWidth * 0.5f) + targetRadius);
        }
    }
}
