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
    public abstract class HeroBase3D
    {
        public const int MaxLevel = 12; // Level Cap สำหรับ 1.0.0 (Section 2.2)

        // ข้อมูลระบุตัวตน
        public string HeroId { get; protected set; }
        public string DisplayName { get; protected set; }

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

        // ระบบช่องเก็บของ 6 ช่อง (Section 5.1)
        public KOA.Core.Items.Inventory Inventory { get; } = new KOA.Core.Items.Inventory();

        // สถิติสุทธิหลังรวมการเติบโตต่อเลเวล และไอเทมในกระเป๋า (Section 2.2 & Section 5.2)
        public float EffectiveMaxHp => BaseMaxHp + (StatGrowth.HpPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxHp;
        public float EffectiveMaxMana => BaseMaxMana + (StatGrowth.ManaPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMaxMana;
        public float EffectiveArmor => BaseArmor + (StatGrowth.ArmorPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusArmor;
        public float EffectiveMagicResist => BaseMagicResist + (StatGrowth.MrPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusMagicResist;
        public float EffectiveAttackDamage => BaseAttackDamage + (StatGrowth.AdPerLevel * (CurrentLevel - 1)) + Inventory.TotalBonusAttackDamage;
        public float EffectiveMoveSpeed => BaseMoveSpeed + Inventory.TotalBonusMoveSpeed;

        // Events สำหรับ Presentation Layer เพื่ออัปเดต UI/Billboard โดยไม่ผูกติดกัน (Decoupled Core)
        public event Action<float, float> OnHealthChanged;
        public event Action<float, float> OnManaChanged;
        public event Action<float, float> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action OnDied;
        public event Action OnRespawned;

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
            // ฮีล HP/Mana ส่วนที่เพิ่มขึ้นมา
            CurrentHp += StatGrowth.HpPerLevel;
            CurrentMana += StatGrowth.ManaPerLevel;

            OnLevelUp?.Invoke(CurrentLevel);
            OnHealthChanged?.Invoke(CurrentHp, EffectiveMaxHp);
            OnManaChanged?.Invoke(CurrentMana, EffectiveMaxMana);
        }

        /// <summary>
        /// คำนวณความเสียหายที่ได้รับตามเกราะและค่าต้านทานเวท (Section 6)
        /// </summary>
        public virtual void TakeDamage(float rawDamage, DamageType damageType)
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
    }
}
