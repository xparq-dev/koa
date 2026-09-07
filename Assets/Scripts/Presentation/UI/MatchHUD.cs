using KOA.Core.AI;
using KOA.Core.Economy;
using KOA.Core.Entities;
using KOA.Core.Items;
using KOA.Core.Match;
using KOA.Data.Models;
using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace KOA.Presentation.UI
{
    /// <summary>
    /// Complete Match HUD & Control Panel ตาม Section 8 และมติจาก /grill-me
    /// รวมระบบ HUD ด้านบน, กระเป๋า 6 ช่อง, เมนูร้านค้า 8 ไอเทม, แผงสลับฮีโร่ 4 ตัว, และปุ่มปรับระดับ Bot
    /// ใช้งานผ่าน OnGUI ได้ทันทีโดยไม่ต้อง setup Canvas Prefab ด้วยมือ
    /// </summary>
    public class MatchHUD : MonoBehaviour
    {
        public HeroBase3D CurrentPlayerHero { get; set; }
        public PlayerWallet PlayerWallet { get; set; }
        public MatchSimulation MatchSimulation { get; set; }
        public ModularBotBrain BotBrain { get; set; }
        public ShopSystem ShopSystem { get; set; }

        public event Action<string> OnHeroSwitched; // heroName

        private bool _isShopOpen = false;
        private readonly List<string> _killFeed = new List<string>();
        private float _killFeedTimer = 0f;

        private void Awake()
        {
            if (ShopSystem == null)
            {
                ShopSystem = new ShopSystem();
            }
        }

        public void BindMatch(MatchSimulation match, HeroBase3D playerHero, PlayerWallet wallet, ModularBotBrain bot)
        {
            MatchSimulation = match;
            CurrentPlayerHero = playerHero;
            PlayerWallet = wallet;
            BotBrain = bot;

            if (MatchSimulation != null)
            {
                MatchSimulation.OnKillFeedMessage += AddKillFeed;
            }
        }

        public void AddKillFeed(string message)
        {
            _killFeed.Add(message);
            if (_killFeed.Count > 4) _killFeed.RemoveAt(0);
            _killFeedTimer = 4.0f;
        }

        private void Update()
        {
            if (_killFeedTimer > 0f)
            {
                _killFeedTimer -= Time.deltaTime;
                if (_killFeedTimer <= 0f && _killFeed.Count > 0)
                {
                    _killFeed.RemoveAt(0);
                }
            }

            // ปุ่ม P เพื่อเปิด/ปิดร้านค้า
            bool pPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                pPressed = Keyboard.current.pKey.wasPressedThisFrame;
            }
            else
#endif
            {
                pPressed = UnityEngine.Input.GetKeyDown(KeyCode.P);
            }

            if (pPressed)
            {
                _isShopOpen = !_isShopOpen;
            }
        }

        private void OnGUI()
        {
            DrawTopStatsBar();
            DrawHeroStatsPanel();
            DrawInventoryBar();
            DrawControlPanel();
            DrawKillFeed();

            if (_isShopOpen)
            {
                DrawShopCatalog();
            }
        }

        private void DrawTopStatsBar()
        {
            int screenWidth = Screen.width;

            // พื้นหลัง Top Bar
            GUI.Box(new Rect(10, 10, screenWidth - 20, 45), "");

            // ข้อมูลทองคำ & สตรีค
            int gold = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
            int streak = PlayerWallet != null ? PlayerWallet.CurrentKillStreak : 0;
            string matchTimeStr = MatchSimulation != null ? FormatTime(MatchSimulation.MatchTime) : "00:00";

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            GUI.Label(new Rect(25, 20, 200, 30), $"🪙 Gold: {gold}g  (🔥 Streak: {streak})", titleStyle);
            GUI.Label(new Rect(screenWidth / 2 - 50, 20, 100, 30), $"⏱ {matchTimeStr}", titleStyle);

            // ปุ่มสลับเปิด/ปิด Shop
            string shopBtnText = _isShopOpen ? "Close Shop [P]" : "Open Shop [P]";
            if (GUI.Button(new Rect(screenWidth - 170, 18, 140, 28), shopBtnText))
            {
                _isShopOpen = !_isShopOpen;
            }
        }

        private void DrawHeroStatsPanel()
        {
            if (CurrentPlayerHero == null) return;

            GUI.Box(new Rect(10, 65, 220, 175), $"Hero: {CurrentPlayerHero.DisplayName} (Lv.{CurrentPlayerHero.CurrentLevel})");

            float hp = CurrentPlayerHero.CurrentHp;
            float maxHp = CurrentPlayerHero.EffectiveMaxHp;
            float mana = CurrentPlayerHero.CurrentMana;
            float maxMana = CurrentPlayerHero.EffectiveMaxMana;

            GUI.Label(new Rect(20, 90, 200, 20), $"HP: {Mathf.RoundToInt(hp)} / {Mathf.RoundToInt(maxHp)}");
            GUI.Label(new Rect(20, 110, 200, 20), $"Mana: {Mathf.RoundToInt(mana)} / {Mathf.RoundToInt(maxMana)}");
            GUI.Label(new Rect(20, 130, 200, 20), $"AD: {CurrentPlayerHero.EffectiveAttackDamage:F1} | Armor: {CurrentPlayerHero.EffectiveArmor:F1}");
            GUI.Label(new Rect(20, 150, 200, 20), $"MR: {CurrentPlayerHero.EffectiveMagicResist:F1} | Spd: {CurrentPlayerHero.EffectiveMoveSpeed:F1}m/s");
            GUI.Label(new Rect(20, 170, 200, 20), $"EXP: {CurrentPlayerHero.CurrentExp:F0} / {CurrentPlayerHero.RequiredExp:F0}");

            // ปุ่ม Cheat เพิ่ม EXP เพื่อทดสอบ Level Up (Section 2.2)
            if (GUI.Button(new Rect(20, 200, 95, 24), "+200 EXP"))
            {
                CurrentPlayerHero.AddExp(200f);
            }
            if (GUI.Button(new Rect(120, 200, 95, 24), "+500 Gold"))
            {
                PlayerWallet?.AddGold(500);
            }
        }

        private void DrawInventoryBar()
        {
            if (CurrentPlayerHero == null) return;

            int barWidth = 480;
            int startX = Screen.width / 2 - (barWidth / 2);
            int startY = Screen.height - 70;

            GUI.Box(new Rect(startX, startY, barWidth, 60), "Inventory Slots [1 - 6] (Click slot to sell 70%)");

            for (int i = 0; i < Inventory.SlotCount; i++)
            {
                ItemData item = CurrentPlayerHero.Inventory.GetItemInSlot(i);
                float cd = CurrentPlayerHero.Inventory.GetActiveCooldownRemaining(i);
                int btnX = startX + 10 + (i * 76);
                int btnY = startY + 22;

                string slotText = item != null ? (cd > 0f ? $"CD: {cd:F1}s" : item.DisplayName.Substring(0, Mathf.Min(8, item.DisplayName.Length))) : $"Slot {i + 1}";

                if (GUI.Button(new Rect(btnX, btnY, 70, 32), slotText))
                {
                    if (item != null)
                    {
                        // คลิกเพื่อขายไอเทม
                        ShopSystem.TrySellItem(i, PlayerWallet, CurrentPlayerHero.Inventory);
                    }
                }
            }
        }

        private void DrawControlPanel()
        {
            int panelX = Screen.width - 240;
            int panelY = 65;

            GUI.Box(new Rect(panelX, panelY, 230, 240), "Select Hero & Bot AI");

            // Hero Selector (Section 6.1 - 6.4)
            GUI.Label(new Rect(panelX + 10, panelY + 22, 210, 20), "Choose Player Hero:");
            string[] heroes = { "Vorkas", "Zenthis", "Korvax", "Gravitor" };
            for (int i = 0; i < heroes.Length; i++)
            {
                int btnX = panelX + 10 + (i % 2 * 105);
                int btnY = panelY + 45 + (i / 2 * 32);
                if (GUI.Button(new Rect(btnX, btnY, 100, 28), heroes[i]))
                {
                    OnHeroSwitched?.Invoke(heroes[i]);
                }
            }

            // Bot Difficulty Selector (Section 9)
            GUI.Label(new Rect(panelX + 10, panelY + 120, 210, 20), "Bot AI Difficulty:");
            BotDifficulty[] diffs = { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard };
            for (int i = 0; i < diffs.Length; i++)
            {
                int btnX = panelX + 10 + (i * 70);
                int btnY = panelY + 145;
                bool isCurrent = BotBrain != null && BotBrain.Difficulty == diffs[i];
                string label = isCurrent ? $"[{diffs[i]}]" : diffs[i].ToString();

                if (GUI.Button(new Rect(btnX, btnY, 66, 28), label))
                {
                    if (BotBrain != null) BotBrain.Difficulty = diffs[i];
                }
            }

            // ข้อมูล Bot ปัจจุบัน
            if (BotBrain != null)
            {
                GUI.Label(new Rect(panelX + 10, panelY + 185, 210, 20), $"Bot State: {BotBrain.CurrentState}");
                GUI.Label(new Rect(panelX + 10, panelY + 205, 210, 20), $"Bot HP: {BotBrain.BotHero.CurrentHp:F0} / {BotBrain.BotHero.EffectiveMaxHp:F0}");
            }
        }

        private void DrawKillFeed()
        {
            int startY = 260;
            for (int i = 0; i < _killFeed.Count; i++)
            {
                GUI.Box(new Rect(10, startY + (i * 26), 340, 24), $"📢 {_killFeed[i]}");
            }
        }

        private void DrawShopCatalog()
        {
            int width = 500;
            int height = 380;
            int x = (Screen.width - width) / 2;
            int y = (Screen.height - height) / 2;

            GUI.Box(new Rect(x, y, width, height), "🏪 KOA Duel Shop — 8 Items (Section 5.2)");

            var items = ShopSystem.AvailableCatalog;
            int rowHeight = 38;

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int itemY = y + 35 + (i * rowHeight);

                GUI.Label(new Rect(x + 15, itemY, 170, 30), $"<b>{item.DisplayName}</b>\n<size=10>{item.Description}</size>");
                GUI.Label(new Rect(x + 320, itemY + 5, 70, 25), $"{item.Cost}g");

                bool canAfford = PlayerWallet != null && PlayerWallet.CurrentGold >= item.Cost;
                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(x + 400, itemY + 3, 75, 28), "Buy"))
                {
                    if (CurrentPlayerHero != null && PlayerWallet != null)
                    {
                        ShopSystem.TryBuyItem(item, PlayerWallet, CurrentPlayerHero.Inventory);
                    }
                }
                GUI.enabled = true;
            }
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
