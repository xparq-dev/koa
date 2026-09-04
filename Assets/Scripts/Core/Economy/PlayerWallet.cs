using System;
using UnityEngine;

namespace KOA.Core.Economy
{
    /// <summary>
    /// กระเป๋าเงินและระบบเศรษฐกิจของผู้เล่นตาม Section 4
    /// จัดการ Passive Gold, Last-hit Gold, Tower Bounty และ Kill Streaks
    /// </summary>
    public class PlayerWallet
    {
        public const float PassiveGoldRate = 2.0f; // 2.0 gold/sec ตาม Section 4.1
        public const int MeleeCreepGold = 42;
        public const int RangedCreepGold = 50;
        public const int Tier1TowerGold = 150;
        public const int Tier2TowerGold = 220;
        public const int BaseHeroKillGold = 200;
        public const int StreakBonusStep = 25;
        public const int MaxStreakBonus = 150;

        public int CurrentGold { get; private set; } = 500; // เงินตั้งต้นสำหรับซื้อไอเทมชิ้นแรก
        public int CurrentKillStreak { get; private set; } = 0;

        private float _passiveGoldAccumulator = 0f;

        public event Action<int, int> OnGoldChanged; // currentGold, delta
        public event Action<int> OnStreakChanged;

        public PlayerWallet(int startingGold = 500)
        {
            CurrentGold = startingGold;
        }

        public void SimulationTick(float deltaTime)
        {
            // สะสม Passive Gold 2.0g/s
            _passiveGoldAccumulator += PassiveGoldRate * deltaTime;
            if (_passiveGoldAccumulator >= 1.0f)
            {
                int goldToAdd = Mathf.FloorToInt(_passiveGoldAccumulator);
                _passiveGoldAccumulator -= goldToAdd;
                AddGold(goldToAdd);
            }
        }

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold, amount);
        }

        public bool TrySpendGold(int cost)
        {
            if (cost < 0 || CurrentGold < cost) return false;

            CurrentGold -= cost;
            OnGoldChanged?.Invoke(CurrentGold, -cost);
            return true;
        }

        public void RecordHeroKill()
        {
            CurrentKillStreak++;
            int bonus = Mathf.Min(CurrentKillStreak * StreakBonusStep, MaxStreakBonus);
            int totalBounty = BaseHeroKillGold + bonus;
            AddGold(totalBounty);
            OnStreakChanged?.Invoke(CurrentKillStreak);
        }

        public void RecordDeath()
        {
            CurrentKillStreak = 0;
            OnStreakChanged?.Invoke(CurrentKillStreak);
        }
    }
}
