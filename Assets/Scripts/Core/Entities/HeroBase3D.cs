using KOA.Data.Enums;
using System;
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
        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.identity;
        public bool IsAlive => CurrentHp > 0f;
        public bool IsMoving { get; protected set; }
        public ITargetable CurrentAttackTarget { get; set; }

        // ระบบช่องเก็บของ 6 ช่อง (Section 5.1)
        public KOA.Core.Items.Inventory Inventory { get; } = new KOA.Core.Items.Inventory();

        // ระบบแต้มสกิลและระดับสกิลสไตล์ DOTA 2
        public int AvailableSkillPoints { get; protected set; } = 1;
        public int Skill1Rank { get; protected set; } = 0; // Max 4 (Q)
        public int Skill2Rank { get; protected set; } = 0; // Max 4 (W)
        public int Skill3Rank { get; protected set; } = 0; // Max 4 (E)
        public int UltimateRank { get; protected set; } = 0; // Max 3 (R - Unlocks at Lv 6, 10, 12)
        public int StatBonusRank { get; protected set; } = 0; // Max 4 (+All Attributes)

        // Talent Tree (Lv 4, 8, 12) (-1 = Unchosen, 0 = Option A, 1 = Option B)
        public int TalentTier1Choice { get; protected set; } = -1; // Lv 4: 0 = +15% AtkSpeed, 1 = +150 Max HP
        public int TalentTier2Choice { get; protected set; } = -1; // Lv 8: 0 = 15% CDR, 1 = +25 Base AD
        public int TalentTier3Choice { get; protected set; } = -1; // Lv 12: 0 = +20% Lifesteal/Regen, 1 = +30 Armor

        // สถิติสุทธิหลังรวมการเติบโตต่อเลเวล, ไอเทม, Stat Bonus (+Stats), และ Talents
        public float EffectiveMaxHp => BaseMaxHp + (StatGrowth.HpPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxHp + (StatBonusRank * 60f) + (TalentTier1Choice == 1 ? 150f : 0f);
        public float EffectiveMaxMana => BaseMaxMana + (StatGrowth.ManaPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxMana + (StatBonusRank * 40f);
        public float EffectiveArmor => BaseArmor + (StatGrowth.ArmorPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusArmor + (StatBonusRank * 1.5f) + (TalentTier3Choice == 1 ? 30f : 0f);
        public float EffectiveMagicResist => BaseMagicResist + (StatGrowth.MrPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMagicResist;
        public float EffectiveAttackDamage => BaseAttackDamage + (StatGrowth.AdPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusAttackDamage + (StatBonusRank * 3.0f) + (TalentTier2Choice == 1 ? 25f : 0f);
        public virtual float EffectiveMoveSpeed => BaseMoveSpeed + Inventory.TotalBonusMoveSpeed;
        public float EffectiveCooldownReduction => (TalentTier2Choice == 0 ? 0.15f : 0f) + Inventory.TotalBonusCooldownReduction;
        /// <summary>คูลดาวน์โจมตีปกติหลังคิดรวม Attack Rate Bonus จากไอเทม (TalentTier1 +15% ด้วย) (Section 5.2)</summary>
        public float EffectiveAttackCooldownFromBase(float baseAttackCooldown)
        {
            float rateBonus = Inventory.TotalBonusAttackRatePercent + (TalentTier1Choice == 0 ? 0.15f : 0f);
            return baseAttackCooldown / (1f + rateBonus);
        }

        // ค่าสถานะ Fountain Zone
        public bool IsInFountainZone { get; set; } = false;

        // Events สำหรับ Presentation Layer เพื่ออัปเดต UI/Billboard โดยไม่ผูกติดกัน (Decoupled Core)
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSkillPointsChanged;
        public event Action OnSkillProgressionChanged;
        public event Action OnDied;
        public event Action OnRespawned;

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
        /// เรียกทุก Tick ขณะอยู่ใน Fountain Zone: ฟื้น HP/Mana ~11% MaxHP-MaxMana ต่อวินาที (ประมาณ 8-10 วิเต็มหลอด แบบ LoL)
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
        // DOTA 2 SKILL PROGRESSION & TALENT METHODS
        // ==========================================

        public bool CanLevelSkill1() => AvailableSkillPoints > 0 && Skill1Rank < 4;
        public bool CanLevelSkill2() => AvailableSkillPoints > 0 && Skill2Rank < 4;
        public bool CanLevelSkill3() => AvailableSkillPoints > 0 && Skill3Rank < 4;
        public bool CanLevelUltimate() => AvailableSkillPoints > 0 && UltimateRank < 3 && ((UltimateRank == 0 && CurrentLevel >= 6) || (UltimateRank == 1 && CurrentLevel >= 10) || (UltimateRank == 2 && CurrentLevel >= 12));
        public bool CanLevelStatBonus() => AvailableSkillPoints > 0 && StatBonusRank < 4;

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
            // DOTA Style: Ultimate unlocks at Level 6, 10, 12
            if (UltimateRank == 0 && CurrentLevel < 6) return false;
            if (UltimateRank == 1 && CurrentLevel < 10) return false;
            if (UltimateRank == 2 && CurrentLevel < 12) return false;

            UltimateRank++;
            AvailableSkillPoints--;
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TryLevelStatBonus()
        {
            if (AvailableSkillPoints <= 0 || StatBonusRank >= 4) return false;
            StatBonusRank++;
            AvailableSkillPoints--;
            CurrentHp += 60f;
            CurrentMana += 40f;
            InvokeHealthChanged();
            InvokeManaChanged();
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
            OnSkillProgressionChanged?.Invoke();
            return true;
        }

        public bool TrySelectTalent(int tier, int optionIndex)
        {
            if (optionIndex < 0 || optionIndex > 1) return false;
            if (tier == 1 && CurrentLevel >= 4 && TalentTier1Choice == -1)
            {
                TalentTier1Choice = optionIndex;
                if (optionIndex == 1) CurrentHp += 150f;
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

            // ฮีล HP/Mana ส่วนที่เพิ่มขึ้นมา
            CurrentHp += StatGrowth.HpPerLevel;
            CurrentMana += StatGrowth.ManaPerLevel;

            OnLevelUp?.Invoke(CurrentLevel);
            OnSkillPointsChanged?.Invoke(AvailableSkillPoints);
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
            if (!IsAlive) return;

            float netDamage = rawDamage;
            if (damageType == DamageType.Physical)
            {
                // Damage Reduction = Armor / (100 + Armor)
                float reduction = EffectiveArmor / (100f + Mathf.Max(0f, EffectiveArmor));
                netDamage = rawDamage * (1f - reduction);
            }
            else if (damageType == DamageType.Magic)
            {
                // Damage Reduction = MR / (100 + MR)
                float reduction = EffectiveMagicResist / (100f + Mathf.Max(0f, EffectiveMagicResist));
                netDamage = rawDamage * (1f - reduction);
            }

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
