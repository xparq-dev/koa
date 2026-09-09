using System;
using System.Collections.Generic;
using KOA.Data.Enums;

namespace KOA.Data.Models
{
    public enum ItemCategory
    {
        Consumables = 0,
        Attributes = 1,
        Equipment = 2,
        Miscellaneous = 3,
        Upgraded = 4
    }

    /// <summary>
    /// ข้อมูลไอเทมในร้านค้าตาม Section 5.2
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string ItemId;
        public string DisplayName;
        public int Cost;
        public string Description;
        public ItemCategory Category;

        // Stat Bonuses (Section 5.2)
        public float BonusMaxHp;
        public float BonusMaxMana;
        public float BonusArmor;
        public float BonusMagicResist;
        public float BonusAttackDamage;
        public float BonusMoveSpeed;
        public float BonusCooldownReduction; // เช่น 0.05f = 5%
        public float BonusAttackRatePercent; // เช่น 0.25f = 25%

        // Active Ability
        public bool HasActive;
        public ItemActiveEffect ActiveEffect;
        public float ActiveCooldownSeconds;
        public string ActiveDescription;

        /// <summary>
        /// รายการไอเทมเริ่มต้นทั้ง 8 รายการตาม Section 5.2
        /// </summary>
        public static List<ItemData> GetAllStartingItems()
        {
            return new List<ItemData>
            {
                new ItemData
                {
                    ItemId = "item_iron_plate_bracer",
                    DisplayName = "Iron Plate Bracer",
                    Cost = 500,
                    Category = ItemCategory.Attributes,
                    BonusMaxHp = 150f,
                    BonusArmor = 10f,
                    Description = "+150 Max HP, +10 Armor"
                },
                new ItemData
                {
                    ItemId = "item_focus_crystal",
                    DisplayName = "Focus Crystal",
                    Cost = 550,
                    Category = ItemCategory.Attributes,
                    BonusMaxMana = 100f,
                    BonusCooldownReduction = 0.05f,
                    Description = "+100 Max Mana, +5% Cooldown Reduction"
                },
                new ItemData
                {
                    ItemId = "item_kinetic_boots",
                    DisplayName = "Kinetic Boots",
                    Cost = 500,
                    Category = ItemCategory.Equipment,
                    BonusMoveSpeed = 1.0f,
                    Description = "+1.0 m/s Move Speed"
                },
                new ItemData
                {
                    ItemId = "item_warblade_fang",
                    DisplayName = "Warblade Fang",
                    Cost = 600,
                    Category = ItemCategory.Equipment,
                    BonusAttackDamage = 20f,
                    Description = "+20 Attack Damage"
                },
                new ItemData
                {
                    ItemId = "item_void_emblem",
                    DisplayName = "Void Emblem",
                    Cost = 550,
                    Category = ItemCategory.Attributes,
                    BonusMaxHp = 50f,
                    BonusMagicResist = 15f,
                    Description = "+50 Max HP, +15 Magic Resist"
                },
                new ItemData
                {
                    ItemId = "item_overclock_core",
                    DisplayName = "Overclock Core",
                    Cost = 650,
                    Category = ItemCategory.Equipment,
                    BonusAttackRatePercent = 0.25f,
                    Description = "+25% Attack Rate"
                },
                new ItemData
                {
                    ItemId = "item_nullifying_cloak",
                    DisplayName = "Nullifying Cloak (Active)",
                    Cost = 800,
                    Category = ItemCategory.Miscellaneous,
                    HasActive = true,
                    ActiveEffect = ItemActiveEffect.CleanseAndCrowdControlImmunity,
                    ActiveCooldownSeconds = 60.0f,
                    ActiveDescription = "กดใช้: ล้างสถานะ Debuff ทั้งหมด + Immune ต่อ CC 1.0 วินาที (CD 60s)",
                    Description = "Active: Cleanse Debuffs + CC Immunity 1s"
                },
                new ItemData
                {
                    ItemId = "item_gravity_anchor",
                    DisplayName = "Gravity Anchor (Capstone)",
                    Cost = 1800,
                    Category = ItemCategory.Upgraded,
                    BonusMaxHp = 300f,
                    BonusArmor = 30f,
                    BonusMagicResist = 30f,
                    Description = "+300 Max HP, +30 Armor, +30 Magic Resist"
                }
            };
        }
    }
}
