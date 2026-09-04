using KOA.Data.Models;
using System;
using UnityEngine;

namespace KOA.Core.Items
{
    /// <summary>
    /// ระบบช่องเก็บของ 6 ช่อง (Inventory Slots) ตาม Section 5.1
    /// จัดการการใส่ไอเทม, การคิดคำนวณโบนัส Stat, และคูลดาวน์ของ Active Item
    /// </summary>
    public class Inventory
    {
        public const int SlotCount = 6; // Section 5.1

        private readonly ItemData[] _slots = new ItemData[SlotCount];
        private readonly float[] _activeCooldowns = new float[SlotCount];

        public event Action<int, ItemData> OnSlotChanged;
        public event Action<int, ItemData> OnActiveUsed;
        public event Action OnStatsRecalculated;

        // ค่าโบนัสรวมจากไอเทมทั้งหมดในกระเป๋า
        public float TotalBonusMaxHp { get; private set; }
        public float TotalBonusMaxMana { get; private set; }
        public float TotalBonusArmor { get; private set; }
        public float TotalBonusMagicResist { get; private set; }
        public float TotalBonusAttackDamage { get; private set; }
        public float TotalBonusMoveSpeed { get; private set; }
        public float TotalBonusCooldownReduction { get; private set; }
        public float TotalBonusAttackRatePercent { get; private set; }

        public ItemData GetItemInSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return null;
            return _slots[slotIndex];
        }

        public float GetActiveCooldownRemaining(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return 0f;
            return _activeCooldowns[slotIndex];
        }

        public bool AddItem(ItemData item, out int placedSlot)
        {
            placedSlot = -1;
            if (item == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = item;
                    _activeCooldowns[i] = 0f;
                    placedSlot = i;
                    RecalculateStats();
                    OnSlotChanged?.Invoke(i, item);
                    return true;
                }
            }
            return false; // กระเป๋าเต็ม
        }

        public bool RemoveItemAt(int slotIndex, out ItemData removedItem)
        {
            removedItem = null;
            if (slotIndex < 0 || slotIndex >= SlotCount || _slots[slotIndex] == null)
            {
                return false;
            }

            removedItem = _slots[slotIndex];
            _slots[slotIndex] = null;
            _activeCooldowns[slotIndex] = 0f;
            RecalculateStats();
            OnSlotChanged?.Invoke(slotIndex, null);
            return true;
        }

        public bool TryUseActive(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SlotCount) return false;
            ItemData item = _slots[slotIndex];
            if (item == null || !item.HasActive) return false;

            if (_activeCooldowns[slotIndex] > 0f) return false;

            // เริ่มนับคูลดาวน์ Active
            _activeCooldowns[slotIndex] = item.ActiveCooldownSeconds;
            OnActiveUsed?.Invoke(slotIndex, item);
            return true;
        }

        public void SimulationTick(float deltaTime)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_activeCooldowns[i] > 0f)
                {
                    _activeCooldowns[i] = Mathf.Max(0f, _activeCooldowns[i] - deltaTime);
                }
            }
        }

        private void RecalculateStats()
        {
            TotalBonusMaxHp = 0f;
            TotalBonusMaxMana = 0f;
            TotalBonusArmor = 0f;
            TotalBonusMagicResist = 0f;
            TotalBonusAttackDamage = 0f;
            TotalBonusMoveSpeed = 0f;
            TotalBonusCooldownReduction = 0f;
            TotalBonusAttackRatePercent = 0f;

            for (int i = 0; i < SlotCount; i++)
            {
                var item = _slots[i];
                if (item != null)
                {
                    TotalBonusMaxHp += item.BonusMaxHp;
                    TotalBonusMaxMana += item.BonusMaxMana;
                    TotalBonusArmor += item.BonusArmor;
                    TotalBonusMagicResist += item.BonusMagicResist;
                    TotalBonusAttackDamage += item.BonusAttackDamage;
                    TotalBonusMoveSpeed += item.BonusMoveSpeed;
                    TotalBonusCooldownReduction += item.BonusCooldownReduction;
                    TotalBonusAttackRatePercent += item.BonusAttackRatePercent;
                }
            }

            OnStatsRecalculated?.Invoke();
        }
    }
}
