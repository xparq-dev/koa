using KOA.Data.Enums;
using System;

namespace KOA.Data.Models
{
    /// <summary>
    /// สถิติและพารามิเตอร์โครงสร้างป้อมปราการตาม Section 3.3
    /// </summary>
    [Serializable]
    public class StructureStats
    {
        public string DisplayName;
        public float MaxHp;
        public float BaseArmor;
        public float BaseMagicResist;
        public float BaseAttackDamage;
        public float AttackRange;
        public float FireRateSeconds;
        public DamageType DamageType;
        public int GoldBounty;
        public int ExpBounty;

        // Mechanics
        public bool IsHeatingLaser;
        public float DamageIncreasePerHitPercent;
        public float MaxDamageMultiplier;
        public bool HasTowerPlating;
        public float PlatingDurationMinutes;
        public float PlatingBonusArmor;
        public float AoeGravitySlowRadius;
        public float AoeSlowPercent;
        public float HpRegenerationOutOfCombat;
        public bool IsGameOverOnDestruction;
        public bool RequiresTier2Destroyed;

        /// <summary>
        /// ข้อมูลตั้งต้นของ Outer Tower (Tier 1) ตาม Section 3.3
        /// </summary>
        public static StructureStats CreateTier1OuterTower()
        {
            return new StructureStats
            {
                DisplayName = "Outer Tower",
                MaxHp = 2800.0f,
                BaseArmor = 35f,
                BaseMagicResist = 35f,
                BaseAttackDamage = 110.0f,
                AttackRange = 8.0f,
                FireRateSeconds = 1.0f,
                DamageType = DamageType.Physical,
                GoldBounty = 150,
                ExpBounty = 120,
                IsHeatingLaser = true,
                DamageIncreasePerHitPercent = 20.0f,
                MaxDamageMultiplier = 2.5f,
                HasTowerPlating = true,
                PlatingDurationMinutes = 4.0f,
                PlatingBonusArmor = 120f
            };
        }

        /// <summary>
        /// ข้อมูลตั้งต้นของ Inner Tower (Tier 2) ตาม Section 3.3
        /// </summary>
        public static StructureStats CreateTier2InnerTower()
        {
            return new StructureStats
            {
                DisplayName = "Inner Tower",
                MaxHp = 4000.0f,
                BaseArmor = 55f,
                BaseMagicResist = 55f,
                BaseAttackDamage = 160.0f,
                AttackRange = 8.5f,
                FireRateSeconds = 0.9f,
                DamageType = DamageType.Physical,
                GoldBounty = 220,
                ExpBounty = 200,
                IsHeatingLaser = true,
                DamageIncreasePerHitPercent = 25.0f,
                MaxDamageMultiplier = 3.0f,
                HasTowerPlating = false,
                AoeGravitySlowRadius = 3.0f,
                AoeSlowPercent = 15.0f
            };
        }

        /// <summary>
        /// ข้อมูลตั้งต้นของ Nexus Core ตาม Section 3.3
        /// </summary>
        public static StructureStats CreateNexusCore()
        {
            return new StructureStats
            {
                DisplayName = "The Nexus Core",
                MaxHp = 6000.0f,
                BaseArmor = 90f,
                BaseMagicResist = 90f,
                BaseAttackDamage = 0.0f,
                AttackRange = 0.0f,
                FireRateSeconds = 0.0f,
                DamageType = DamageType.None,
                GoldBounty = 0,
                ExpBounty = 0,
                HpRegenerationOutOfCombat = 12.0f,
                IsGameOverOnDestruction = true,
                RequiresTier2Destroyed = true
            };
        }
    }
}
