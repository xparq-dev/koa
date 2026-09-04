using KOA.Core.Economy;
using KOA.Data.Models;
using System;
using System.Collections.Generic;

namespace KOA.Core.Items
{
    /// <summary>
    /// ระบบร้านค้า (Shop System) ตาม Section 5
    /// จัดการการซื้อและขายไอเทมระหว่าง PlayerWallet และ Inventory
    /// </summary>
    public class ShopSystem
    {
        public const float SellRefundRatio = 0.70f; // ขายคืนได้ 70% ของราคาเดิม

        public List<ItemData> AvailableCatalog { get; private set; }

        public event Action<ItemData> OnItemPurchased;
        public event Action<ItemData, int> OnItemSold; // item, refundGold

        public ShopSystem()
        {
            AvailableCatalog = ItemData.GetAllStartingItems();
        }

        public bool TryBuyItem(ItemData item, PlayerWallet wallet, Inventory inventory)
        {
            if (item == null || wallet == null || inventory == null) return false;

            if (wallet.CurrentGold < item.Cost)
            {
                return false; // เงินไม่พอ
            }

            if (!inventory.AddItem(item, out _))
            {
                return false; // กระเป๋าเต็ม 6 ช่อง
            }

            wallet.TrySpendGold(item.Cost);
            OnItemPurchased?.Invoke(item);
            return true;
        }

        public bool TrySellItem(int slotIndex, PlayerWallet wallet, Inventory inventory)
        {
            if (wallet == null || inventory == null) return false;

            if (inventory.RemoveItemAt(slotIndex, out ItemData soldItem))
            {
                int refundGold = (int)(soldItem.Cost * SellRefundRatio);
                wallet.AddGold(refundGold);
                OnItemSold?.Invoke(soldItem, refundGold);
                return true;
            }
            return false;
        }
    }
}
