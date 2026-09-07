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
    /// Complete Match HUD & Control Panel ตาม Section 8
    /// รวม HP/Mana Bars กราฟิก, Skill Cooldown Panel แบบ MOBA, Low-HP Vignette Warning,
    /// Death Screen + Respawn Countdown, Win/Lose End Screen, Kill Feed, Shop, และ Bot AI Controls
    /// </summary>
    public class MatchHUD : MonoBehaviour
    {
        public HeroBase3D CurrentPlayerHero { get; set; }
        public PlayerWallet PlayerWallet { get; set; }
        public MatchSimulation MatchSimulation { get; set; }
        public ModularBotBrain BotBrain { get; set; }
        public ShopSystem ShopSystem { get; set; }

        // Game State
        public bool IsPlayerDead { get; set; } = false;
        public float RespawnTimeRemaining { get; set; } = 0f;
        public bool IsGameOver { get; set; } = false;
        public bool PlayerWon { get; set; } = false;
        public float GameOverTime { get; set; } = 0f;

        public event Action<string> OnHeroSwitched;
        public event Action<string> OnBotHeroSwitched;
        public event Action OnPlayAgainRequested;

        private bool _isShopOpen = false;
        private bool _isTalentTreeOpen = false;
        private bool _isScoreboardOpen = false;
        private readonly List<string> _killFeed = new List<string>();
        private float _killFeedTimer = 0f;

        // Vignette
        private Texture2D _vignetteTexture;
        private float _vignetteAlpha = 0f;
        private float _vignettePulseTimer = 0f;

        // Overlay textures
        private Texture2D _overlayDarkTex;
        private Texture2D _redTex;
        private Texture2D _goldTex;

        private bool _stylesInitialized = false;
        private GUIStyle _boldLabel;
        private GUIStyle _centerLabel;
        private GUIStyle _bigLabel;
        private GUIStyle _hintLabel;

        private void Awake()
        {
            if (ShopSystem == null) ShopSystem = new ShopSystem();
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
                MatchSimulation.OnGameOver += HandleGameOver;
            }

            if (CurrentPlayerHero != null)
            {
                CurrentPlayerHero.OnDied += HandlePlayerDied;
                CurrentPlayerHero.OnRespawned += HandlePlayerRespawned;
            }
        }

        public void UnbindHero(HeroBase3D oldHero)
        {
            if (oldHero != null)
            {
                oldHero.OnDied -= HandlePlayerDied;
                oldHero.OnRespawned -= HandlePlayerRespawned;
            }
        }

        private void HandleGameOver(int winningTeam)
        {
            IsGameOver = true;
            PlayerWon = winningTeam == 0; // Blue = Player
            GameOverTime = MatchSimulation?.MatchTime ?? 0f;
        }

        private void HandlePlayerDied()
        {
            IsPlayerDead = true;
            if (CurrentPlayerHero != null)
            {
                RespawnTimeRemaining = CurrentPlayerHero.CalculateRespawnTime();
            }
        }

        private void HandlePlayerRespawned()
        {
            IsPlayerDead = false;
            RespawnTimeRemaining = 0f;
        }

        public void AddKillFeed(string message)
        {
            _killFeed.Add(message);
            if (_killFeed.Count > 5) _killFeed.RemoveAt(0);
            _killFeedTimer = 5.0f;
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _boldLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            _boldLabel.normal.textColor = Color.white;

            _centerLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleCenter
            };
            _centerLabel.normal.textColor = Color.white;

            _bigLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _bigLabel.normal.textColor = Color.white;

            _hintLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            _hintLabel.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            _vignetteTexture = CreateVignetteTex(256, 256);
            _overlayDarkTex = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.75f));
            _redTex = MakeTex(1, 1, new Color(0.85f, 0.05f, 0.05f));
            _goldTex = MakeTex(1, 1, new Color(0.95f, 0.78f, 0.1f));
        }

        private static Texture2D MakeTex(int w, int h, Color col)
        {
            var t = new Texture2D(w, h);
            var pix = new Color[w * h];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            t.SetPixels(pix);
            t.Apply();
            return t;
        }

        private static Texture2D CreateVignetteTex(int w, int h)
        {
            var tex = new Texture2D(w, h);
            Vector2 center = new Vector2(0.5f, 0.5f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 uv = new Vector2((float)x / w, (float)y / h);
                    float dist = Vector2.Distance(uv, center) * 2f;
                    dist = Mathf.Clamp01(dist - 0.5f) * 2f;
                    tex.SetPixel(x, y, new Color(0.7f, 0.02f, 0.02f, dist * dist));
                }
            }
            tex.Apply();
            return tex;
        }

        private void Update()
        {
            if (_killFeedTimer > 0f)
            {
                _killFeedTimer -= Time.deltaTime;
                if (_killFeedTimer <= 0f && _killFeed.Count > 0) _killFeed.RemoveAt(0);
            }

            // Respawn countdown
            if (IsPlayerDead && RespawnTimeRemaining > 0f)
            {
                RespawnTimeRemaining -= Time.deltaTime;
            }

            // Vignette pulse เมื่อ HP ต่ำกว่า 25%
            if (CurrentPlayerHero != null && CurrentPlayerHero.IsAlive)
            {
                float hpRatio = CurrentPlayerHero.CurrentHp / CurrentPlayerHero.EffectiveMaxHp;
                if (hpRatio < 0.25f)
                {
                    _vignettePulseTimer += Time.deltaTime * 2.5f;
                    float pulse = (Mathf.Sin(_vignettePulseTimer) + 1f) * 0.5f;
                    _vignetteAlpha = Mathf.Lerp(0.35f, 0.8f, pulse) * (1f - (hpRatio / 0.25f) * 0.4f);
                }
                else
                {
                    _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, 0f, Time.deltaTime * 2f);
                    _vignettePulseTimer = 0f;
                }
            }
            else
            {
                _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, 0f, Time.deltaTime * 2f);
            }

            bool pPressed = false;
            bool tPressed = false;
            bool tabPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                pPressed = Keyboard.current.pKey.wasPressedThisFrame;
                tPressed = Keyboard.current.tKey.wasPressedThisFrame;
                tabPressed = Keyboard.current.tabKey.wasPressedThisFrame;
            }
            else
#endif
            {
                pPressed = UnityEngine.Input.GetKeyDown(KeyCode.P);
                tPressed = UnityEngine.Input.GetKeyDown(KeyCode.T);
                tabPressed = UnityEngine.Input.GetKeyDown(KeyCode.Tab);
            }
            if (pPressed)
            {
                if (_isShopOpen)
                {
                    _isShopOpen = false;
                }
                else if (IsPlayerInFountain())
                {
                    _isShopOpen = true;
                }
                else
                {
                    // แจ้ง: ต้องอยู่ใน Fountain ถึงจะซื้อของได้ (Section 5.1)
                    AddKillFeed("⚠️ ต้องอยู่ใน Fountain Zone ถึงจะเปิดร้านค้าได้ (Return to fountain)");
                }
            }
            if (tPressed) _isTalentTreeOpen = !_isTalentTreeOpen;
            if (tabPressed) _isScoreboardOpen = !_isScoreboardOpen;

            // Dota 2 Quick Level-up Hotkeys: Ctrl + Q / W / E / R / U
            bool isCtrl = false;
            bool qDown = false, wDown = false, eDown = false, rDown = false, uDown = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                isCtrl = Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed;
                qDown = Keyboard.current.qKey.wasPressedThisFrame;
                wDown = Keyboard.current.wKey.wasPressedThisFrame;
                eDown = Keyboard.current.eKey.wasPressedThisFrame;
                rDown = Keyboard.current.rKey.wasPressedThisFrame;
                uDown = Keyboard.current.uKey.wasPressedThisFrame;
            }
            else
#endif
            {
                isCtrl = UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);
                qDown = UnityEngine.Input.GetKeyDown(KeyCode.Q);
                wDown = UnityEngine.Input.GetKeyDown(KeyCode.W);
                eDown = UnityEngine.Input.GetKeyDown(KeyCode.E);
                rDown = UnityEngine.Input.GetKeyDown(KeyCode.R);
                uDown = UnityEngine.Input.GetKeyDown(KeyCode.U);
            }

            if (isCtrl && CurrentPlayerHero != null && CurrentPlayerHero.AvailableSkillPoints > 0)
            {
                if (qDown) CurrentPlayerHero.TryLevelSkill1();
                else if (wDown) CurrentPlayerHero.TryLevelSkill2();
                else if (eDown) CurrentPlayerHero.TryLevelSkill3();
                else if (rDown) CurrentPlayerHero.TryLevelUltimate();
                else if (uDown) CurrentPlayerHero.TryLevelStatBonus();
            }
        }

        private void OnGUI()
        {
            InitStyles();

            // Low-HP Vignette (วาดก่อนสุด — ด้านล่างสุด)
            if (_vignetteAlpha > 0.01f)
            {
                DrawVignette();
            }

            // Game Over Screen (วาดแทน HUD ปกติ)
            if (IsGameOver)
            {
                DrawEndGameScreen();
                return; // ไม่วาด HUD ปกติเมื่อเกมจบ
            }

            // Death Screen Overlay (วาดซ้อนทับ HUD ได้)
            if (IsPlayerDead)
            {
                DrawDeathScreen();
            }

            // HUD ปกติ
            DrawTopBar();
            DrawHeroBottomPanel();
            DrawSkillBar();
            DrawControlPanel();
            DrawKillFeed();

            if (_isShopOpen) DrawShopCatalog();
            if (_isTalentTreeOpen) DrawTalentTreeModal();
            if (_isScoreboardOpen) DrawScoreboardModal();
        }

        // ══════════════════════════════════════════════════════════
        //  LOW-HP VIGNETTE EFFECT (Section 8)
        // ══════════════════════════════════════════════════════════
        private void DrawVignette()
        {
            if (_vignetteTexture == null) return;

            Color prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, _vignetteAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _vignetteTexture, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        // ══════════════════════════════════════════════════════════
        //  DEATH SCREEN + RESPAWN COUNTDOWN (Section 2.3 / 8)
        // ══════════════════════════════════════════════════════════
        private void DrawDeathScreen()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            // Overlay มืด
            var darkStyle = new GUIStyle(GUI.skin.box);
            if (_overlayDarkTex != null) darkStyle.normal.background = _overlayDarkTex;
            GUI.Box(new Rect(0, 0, sw, sh), "", darkStyle);

            // YOU DIED
            int panelW = 420;
            int panelH = 160;
            int px = (sw - panelW) / 2;
            int py = sh / 2 - panelH / 2 - 30;

            GUI.Box(new Rect(px, py, panelW, panelH), "");

            var diedStyle = new GUIStyle(_bigLabel) { fontSize = 38 };
            diedStyle.normal.textColor = new Color(0.85f, 0.08f, 0.08f);
            GUI.Label(new Rect(px, py + 10, panelW, 50), "YOU DIED", diedStyle);

            if (RespawnTimeRemaining > 0f)
            {
                var cdStyle = new GUIStyle(_bigLabel) { fontSize = 22 };
                cdStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);
                GUI.Label(new Rect(px, py + 60, panelW, 36), $"Respawning in  {RespawnTimeRemaining:F1}s", cdStyle);

                // Progress Bar
                float progress = 1f - Mathf.Clamp01(RespawnTimeRemaining / (CurrentPlayerHero?.CalculateRespawnTime() ?? 10f));
                DrawBar(new Rect(px + 30, py + 104, panelW - 60, 18), progress, new Color(0.85f, 0.72f, 0.15f), new Color(0.15f, 0.15f, 0.15f));
            }
            else
            {
                var waitStyle = new GUIStyle(_centerLabel) { fontSize = 16 };
                waitStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(px, py + 70, panelW, 30), "Respawning...", waitStyle);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  WIN / LOSE END SCREEN (Section 8 / Acceptance Criteria 1)
        // ══════════════════════════════════════════════════════════
        private void DrawEndGameScreen()
        {
            int sw = Screen.width;
            int sh = Screen.height;

            // Background overlay
            var bgStyle = new GUIStyle(GUI.skin.box);
            if (_overlayDarkTex != null) bgStyle.normal.background = _overlayDarkTex;
            GUI.Box(new Rect(0, 0, sw, sh), "", bgStyle);

            int panelW = 500;
            int panelH = 300;
            int px = (sw - panelW) / 2;
            int py = (sh - panelH) / 2;

            GUI.Box(new Rect(px, py, panelW, panelH), "");

            // VICTORY / DEFEAT Header
            string resultText = PlayerWon ? "🏆  VICTORY!" : "💀  DEFEAT";
            Color resultColor = PlayerWon ? new Color(0.95f, 0.78f, 0.1f) : new Color(0.85f, 0.15f, 0.1f);

            var resultStyle = new GUIStyle(_bigLabel) { fontSize = 42 };
            resultStyle.normal.textColor = resultColor;
            GUI.Label(new Rect(px, py + 15, panelW, 60), resultText, resultStyle);

            // Stats
            var statStyle = new GUIStyle(_centerLabel) { fontSize = 16 };
            statStyle.normal.textColor = new Color(0.88f, 0.88f, 0.88f);

            string timeStr = FormatTime(GameOverTime);
            int gold = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
            int level = CurrentPlayerHero != null ? CurrentPlayerHero.CurrentLevel : 1;

            GUI.Label(new Rect(px, py + 90, panelW, 28), $"Match Duration:  {timeStr}", statStyle);
            GUI.Label(new Rect(px, py + 120, panelW, 28), $"Level Reached:  {level}", statStyle);
            GUI.Label(new Rect(px, py + 150, panelW, 28), $"Gold Earned:  {gold}g", statStyle);

            if (MatchSimulation != null)
            {
                int blueTowersDown = CountDestroyedTowers(MatchSimulation.RedTowers);
                int redTowersDown = CountDestroyedTowers(MatchSimulation.BlueTowers);
                GUI.Label(new Rect(px, py + 180, panelW, 28), $"Enemy Towers Destroyed:  {blueTowersDown}", statStyle);
            }

            // Play Again
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = PlayerWon ? new Color(0.2f, 0.65f, 0.2f) : new Color(0.6f, 0.2f, 0.2f);
            if (GUI.Button(new Rect(px + panelW / 2 - 90, py + 240, 180, 40), "▶  Play Again"))
            {
                OnPlayAgainRequested?.Invoke();
            }
            GUI.backgroundColor = prevBg;
        }

        private int CountDestroyedTowers(System.Collections.Generic.List<KOA.Core.Structures.TowerEntity> towers)
        {
            int count = 0;
            foreach (var t in towers) if (t.IsDestroyed) count++;
            return count;
        }

        // ══════════════════════════════════════════════════════════
        //  TOP BAR
        // ══════════════════════════════════════════════════════════
        private void DrawTopBar()
        {
            int sw = Screen.width;
            GUI.Box(new Rect(0, 0, sw, 46), "");

            int gold = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
            int streak = PlayerWallet != null ? PlayerWallet.CurrentKillStreak : 0;
            string matchTimeStr = MatchSimulation != null ? FormatTime(MatchSimulation.MatchTime) : "00:00";

            GUI.Label(new Rect(16, 12, 280, 26), $"🪙 Gold: {gold}g    🔥 Streak: {streak}", _boldLabel);

            var timeStyle = new GUIStyle(_boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(sw / 2 - 70, 8, 140, 32), $"⏱  {matchTimeStr}", timeStyle);

            // Scoreboard & Hero Status Button [TAB]
            string tabBtnText = _isScoreboardOpen ? "✖ Close [TAB]" : "📊 Stats [TAB]";
            Color prevBgTab = GUI.backgroundColor;
            if (_isScoreboardOpen) GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
            if (GUI.Button(new Rect(sw - 350, 8, 155, 30), tabBtnText))
            {
                _isScoreboardOpen = !_isScoreboardOpen;
            }
            GUI.backgroundColor = prevBgTab;

            bool inFountain = IsPlayerInFountain();
            string shopBtnText = _isShopOpen ? "✖ Close Shop [P]" : (inFountain ? "🏪 Shop [P]" : "🔒 Shop (Fountain Only)");
            Color prevBg2 = GUI.backgroundColor;
            GUI.backgroundColor = inFountain ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.8f);
            if (GUI.Button(new Rect(sw - 185, 8, 170, 30), shopBtnText))
            {
                if (_isShopOpen) _isShopOpen = false;
                else if (inFountain) _isShopOpen = true;
                else AddKillFeed("⚠️ Return to Fountain to open Shop");
            }
            GUI.backgroundColor = prevBg2;

            // Fountain Zone Badge
            if (inFountain)
            {
                var fountainStyle = new GUIStyle(_boldLabel) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
                fountainStyle.normal.textColor = new Color(0.4f, 0.9f, 1f);
                GUI.Label(new Rect(sw - 185, 40, 170, 18), "💧 In Fountain Zone", fountainStyle);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  BOTTOM-LEFT HERO PANEL (HP / Mana / EXP Bars)
        // ══════════════════════════════════════════════════════════
        private void DrawHeroBottomPanel()
        {
            if (CurrentPlayerHero == null) return;

            int sh = Screen.height;
            int panelX = 10, panelY = sh - 190, panelW = 220, panelH = 180;
            GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");

            var nameStyle = new GUIStyle(_boldLabel) { fontSize = 14 };
            GUI.Label(new Rect(panelX + 8, panelY + 6, panelW - 16, 22),
                $"{CurrentPlayerHero.DisplayName}  Lv.{CurrentPlayerHero.CurrentLevel}", nameStyle);

            float hp = CurrentPlayerHero.CurrentHp;
            float maxHp = CurrentPlayerHero.EffectiveMaxHp;
            float mana = CurrentPlayerHero.CurrentMana;
            float maxMana = CurrentPlayerHero.EffectiveMaxMana;

            int bx = panelX + 8, bw = panelW - 18;

            // HP — เปลี่ยนสีตาม HP ratio (เขียว → เหลือง → แดง)
            float hpRatio = Mathf.Clamp01(hp / maxHp);
            Color hpColor = hpRatio > 0.5f
                ? Color.Lerp(new Color(0.85f, 0.75f, 0.1f), new Color(0.18f, 0.72f, 0.25f), (hpRatio - 0.5f) * 2f)
                : Color.Lerp(new Color(0.85f, 0.12f, 0.08f), new Color(0.85f, 0.75f, 0.1f), hpRatio * 2f);

            GUI.Label(new Rect(bx, panelY + 32, bw, 18), $"HP  {Mathf.RoundToInt(hp)} / {Mathf.RoundToInt(maxHp)}", _centerLabel);
            DrawBar(new Rect(bx, panelY + 50, bw, 14), hpRatio, hpColor, new Color(0.2f, 0.2f, 0.2f));

            GUI.Label(new Rect(bx, panelY + 68, bw, 18), $"MP  {Mathf.RoundToInt(mana)} / {Mathf.RoundToInt(maxMana)}", _centerLabel);
            DrawBar(new Rect(bx, panelY + 86, bw, 14), Mathf.Clamp01(mana / maxMana), new Color(0.22f, 0.46f, 0.95f), new Color(0.2f, 0.2f, 0.2f));

            GUI.Label(new Rect(bx, panelY + 106, bw, 18), $"AD {CurrentPlayerHero.EffectiveAttackDamage:F0}  Armor {CurrentPlayerHero.EffectiveArmor:F0}  MR {CurrentPlayerHero.EffectiveMagicResist:F0}", _centerLabel);
            GUI.Label(new Rect(bx, panelY + 124, bw, 18), $"EXP  {CurrentPlayerHero.CurrentExp:F0} / {CurrentPlayerHero.RequiredExp:F0}", _centerLabel);

            float expRatio = CurrentPlayerHero.CurrentLevel >= HeroBase3D.MaxLevel ? 1f
                           : Mathf.Clamp01(CurrentPlayerHero.CurrentExp / CurrentPlayerHero.RequiredExp);
            DrawBar(new Rect(bx, panelY + 142, bw, 10), expRatio, new Color(0.85f, 0.72f, 0.15f), new Color(0.2f, 0.2f, 0.2f));

            if (GUI.Button(new Rect(bx, panelY + 158, 96, 18), "+200 EXP")) CurrentPlayerHero.AddExp(200f);
            if (GUI.Button(new Rect(bx + 106, panelY + 158, 96, 18), "+500 Gold")) PlayerWallet?.AddGold(500);
        }

        private void DrawBar(Rect fullRect, float ratio, Color fillColor, Color bgColor)
        {
            var bg = new GUIStyle(GUI.skin.box);
            bg.normal.background = MakeTex(1, 1, bgColor);
            GUI.Box(fullRect, "", bg);

            ratio = Mathf.Clamp01(ratio);
            if (ratio > 0f)
            {
                var fill = new GUIStyle(GUI.skin.box);
                fill.normal.background = MakeTex(1, 1, fillColor);
                GUI.Box(new Rect(fullRect.x, fullRect.y, fullRect.width * ratio, fullRect.height), "", fill);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SKILL BAR (Q / W / E / Auto) — Dota 2 Progression Style
        // ══════════════════════════════════════════════════════════
        // ══════════════════════════════════════════════════════════
        //  BOTTOM-CENTER ACTION CONSOLE (Skills + Inventory + Buttons)
        // ══════════════════════════════════════════════════════════
        private void DrawSkillBar()
        {
            if (CurrentPlayerHero == null) return;

            int sw = Screen.width, sh = Screen.height;
            const int slotSize = 64, gap = 6, barCount = 5;
            int totalSkillsWidth = barCount * slotSize + (barCount - 1) * gap; // 5 * 64 + 4 * 6 = 344

            const int invColW = 46, invRowH = 29, invGap = 4;
            int totalInvWidth = 3 * invColW + 2 * invGap; // 3 * 46 + 8 = 146
            int utilBtnW = 96;
            int sectionGap = 10;
            int totalConsoleWidth = totalSkillsWidth + sectionGap + totalInvWidth + sectionGap + utilBtnW; // 344 + 10 + 146 + 10 + 96 = 606

            int startX = sw / 2 - totalConsoleWidth / 2;
            int startY = sh - 76;

            int availPoints = CurrentPlayerHero.AvailableSkillPoints;

            // 1. Level-Up Buttons / Banner (sitting cleanly directly above Q, W, E, R, [A] skills)
            if (availPoints > 0)
            {
                var bannerStyle = new GUIStyle(_centerLabel) { fontSize = 12, fontStyle = FontStyle.Bold };
                bannerStyle.normal.textColor = new Color(1f, 0.9f, 0.2f);
                GUI.Box(new Rect(startX, startY - 48, totalSkillsWidth, 20), "");
                GUI.Label(new Rect(startX, startY - 48, totalSkillsWidth, 20), $"★ {availPoints} SKILL POINT{(availPoints > 1 ? "S" : "")} AVAILABLE (Click [+] or Ctrl+Q/W/E/R)", bannerStyle);

                // [+] Level-up button above Q
                if (CurrentPlayerHero.CanLevelSkill1())
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                    if (GUI.Button(new Rect(startX, startY - 25, slotSize, 22), "[+] LVL"))
                    {
                        CurrentPlayerHero.TryLevelSkill1();
                    }
                }

                // [+] Level-up button above W
                if (CurrentPlayerHero.CanLevelSkill2())
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                    if (GUI.Button(new Rect(startX + (slotSize + gap), startY - 25, slotSize, 22), "[+] LVL"))
                    {
                        CurrentPlayerHero.TryLevelSkill2();
                    }
                }

                // [+] Level-up button above E
                if (CurrentPlayerHero.CanLevelSkill3())
                {
                    GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                    if (GUI.Button(new Rect(startX + 2 * (slotSize + gap), startY - 25, slotSize, 22), "[+] LVL"))
                    {
                        CurrentPlayerHero.TryLevelSkill3();
                    }
                }

                // [+] Level-up button above R (Ultimate)
                if (CurrentPlayerHero.CanLevelUltimate())
                {
                    GUI.backgroundColor = new Color(1f, 0.75f, 0.1f);
                    if (GUI.Button(new Rect(startX + 3 * (slotSize + gap), startY - 25, slotSize, 22), "[+] ULT"))
                    {
                        CurrentPlayerHero.TryLevelUltimate();
                    }
                }
                else if (CurrentPlayerHero.UltimateRank == 0)
                {
                    var reqLvStyle = new GUIStyle(_centerLabel) { fontSize = 10 };
                    reqLvStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                    GUI.Label(new Rect(startX + 3 * (slotSize + gap), startY - 24, slotSize, 20), "Req Lv 6", reqLvStyle);
                }

                // [+STATS] button above Auto Attack
                if (CurrentPlayerHero.CanLevelStatBonus())
                {
                    GUI.backgroundColor = new Color(0.2f, 0.8f, 0.5f);
                    if (GUI.Button(new Rect(startX + 4 * (slotSize + gap), startY - 25, slotSize, 22), $"+STAT ({CurrentPlayerHero.StatBonusRank}/4)"))
                    {
                        CurrentPlayerHero.TryLevelStatBonus();
                    }
                }

                GUI.backgroundColor = Color.white;
            }

            // 2. Background box for entire console
            GUI.Box(new Rect(startX - 8, startY - 4, totalConsoleWidth + 16, slotSize + 8), "");

            // 3. Draw Skill Slots (Q, W, E, R, [A])
            float[] cds = GetHeroCooldowns(CurrentPlayerHero);
            float[] maxCds = GetHeroMaxCooldowns(CurrentPlayerHero);
            string[] keys = { "Q", "W", "E", "R", "[A]" };
            string[] names = GetSkillNames(CurrentPlayerHero);
            int[] ranks = { CurrentPlayerHero.Skill1Rank, CurrentPlayerHero.Skill2Rank, CurrentPlayerHero.Skill3Rank, CurrentPlayerHero.UltimateRank, 1 };
            int[] maxRanks = { 4, 4, 4, 3, 0 };

            for (int i = 0; i < barCount; i++)
            {
                int x = startX + i * (slotSize + gap);
                int y = startY;
                float cd = i < cds.Length ? cds[i] : 0f;
                float maxCd = i < maxCds.Length ? maxCds[i] : 1f;
                int rank = ranks[i];
                int maxRank = maxRanks[i];

                Rect slotRect = new Rect(x, y, slotSize, slotSize);
                Color oldBg = GUI.backgroundColor;

                // Unlearned / Locked Skill (Rank == 0)
                if (i < 4 && rank == 0)
                {
                    GUI.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
                    GUI.Box(slotRect, "");
                    GUI.backgroundColor = oldBg;

                    var dimKey = new GUIStyle(_boldLabel);
                    dimKey.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
                    GUI.Label(new Rect(x + 4, y + 2, 26, 18), keys[i], dimKey);

                    var sml = new GUIStyle(_centerLabel) { fontSize = 10 };
                    sml.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                    GUI.Label(new Rect(x, y + 20, slotSize, 18), names[i], sml);

                    var lockStyle = new GUIStyle(_centerLabel) { fontSize = 9, fontStyle = FontStyle.Bold };
                    lockStyle.normal.textColor = new Color(0.7f, 0.3f, 0.3f);
                    string lockText = (i == 3 && CurrentPlayerHero.CurrentLevel < 6) ? "LOCKED (LV6)" : "UNLEARNED";
                    GUI.Label(new Rect(x, y + 38, slotSize, 18), lockText, lockStyle);
                }
                else if (cd <= 0f)
                {
                    GUI.backgroundColor = GetSkillColor(i);
                    GUI.Box(slotRect, "");
                    GUI.backgroundColor = oldBg;

                    GUI.Label(new Rect(x + 4, y + 2, 26, 18), keys[i], _boldLabel);
                    var sml = new GUIStyle(_centerLabel) { fontSize = 10 };
                    GUI.Label(new Rect(x, y + 20, slotSize, 18), names[i], sml);
                    var rdy = new GUIStyle(_centerLabel) { fontSize = 11, fontStyle = FontStyle.Bold };
                    rdy.normal.textColor = Color.green;
                    GUI.Label(new Rect(x, y + 38, slotSize, 18), "READY", rdy);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
                    GUI.Box(slotRect, "");
                    GUI.backgroundColor = oldBg;

                    float cdRatio = Mathf.Clamp01(cd / maxCd);
                    var cdFill = new GUIStyle(GUI.skin.box);
                    cdFill.normal.background = MakeTex(1, 1, new Color(0.1f, 0.1f, 0.15f, 0.9f));
                    GUI.Box(new Rect(x, y + slotSize * (1f - cdRatio), slotSize, slotSize * cdRatio), "", cdFill);

                    GUI.Label(new Rect(x + 4, y + 2, 26, 18), keys[i], _boldLabel);
                    var cdNum = new GUIStyle(_centerLabel) { fontSize = 20, fontStyle = FontStyle.Bold };
                    cdNum.normal.textColor = Color.white;
                    GUI.Label(new Rect(x, y + 16, slotSize, 26), $"{cd:F1}", cdNum);
                    var sml = new GUIStyle(_centerLabel) { fontSize = 10 };
                    GUI.Label(new Rect(x, y + 42, slotSize, 18), names[i], sml);
                }

                // Dota 2 Skill Rank Pips under slot
                if (maxRank > 0)
                {
                    int pipW = (slotSize - 8 - (maxRank - 1) * 3) / maxRank;
                    int pipH = 4;
                    int pipStartY = y + slotSize - 7;
                    for (int p = 0; p < maxRank; p++)
                    {
                        int px = x + 4 + p * (pipW + 3);
                        Color pipColor = p < rank ? new Color(1f, 0.85f, 0.1f) : new Color(0.2f, 0.2f, 0.25f);
                        DrawBar(new Rect(px, pipStartY, pipW, pipH), 1f, pipColor, pipColor);
                    }
                }
            }

            // 4. Draw Inventory (3x2 Grid: Slots 1-6) directly beside skills
            int invStartX = startX + totalSkillsWidth + sectionGap;
            for (int i = 0; i < 6; i++)
            {
                int col = i % 3;
                int row = i / 3;
                int ix = invStartX + col * (invColW + invGap);
                int iy = startY + 1 + row * (invRowH + invGap);

                ItemData item = CurrentPlayerHero.Inventory.GetItemInSlot(i);
                float itemCd = CurrentPlayerHero.Inventory.GetActiveCooldownRemaining(i);

                string slotText;
                if (item != null)
                {
                    if (itemCd > 0f) slotText = $"{itemCd:F0}s";
                    else slotText = item.DisplayName.Length > 5 ? item.DisplayName.Substring(0, 5) : item.DisplayName;
                }
                else
                {
                    slotText = $"[{i + 1}]";
                }

                Color prevInvBg = GUI.backgroundColor;
                if (item != null)
                {
                    GUI.backgroundColor = itemCd > 0f ? new Color(0.4f, 0.4f, 0.45f) : new Color(0.28f, 0.6f, 0.9f);
                }
                else
                {
                    GUI.backgroundColor = new Color(0.22f, 0.22f, 0.28f);
                }

                if (GUI.Button(new Rect(ix, iy, invColW, invRowH), slotText) && item != null)
                {
                    ShopSystem.TrySellItem(i, PlayerWallet, CurrentPlayerHero.Inventory);
                }
                GUI.backgroundColor = prevInvBg;
            }

            // 5. Utility Buttons (Talents [T] & Stats [TAB])
            bool hasAvailableTalents = (CurrentPlayerHero.CurrentLevel >= 4 && CurrentPlayerHero.TalentTier1Choice == -1)
                                    || (CurrentPlayerHero.CurrentLevel >= 8 && CurrentPlayerHero.TalentTier2Choice == -1)
                                    || (CurrentPlayerHero.CurrentLevel >= 12 && CurrentPlayerHero.TalentTier3Choice == -1);

            int utilX = invStartX + totalInvWidth + sectionGap;

            Color prevBtnBg = GUI.backgroundColor;
            GUI.backgroundColor = hasAvailableTalents ? new Color(1f, 0.85f, 0.15f) : new Color(0.32f, 0.32f, 0.42f);
            string talentText = hasAvailableTalents ? "★ TALENTS [T]" : "Talents [T]";
            if (GUI.Button(new Rect(utilX, startY + 1, utilBtnW, 29), talentText))
            {
                _isTalentTreeOpen = !_isTalentTreeOpen;
            }

            GUI.backgroundColor = _isScoreboardOpen ? new Color(0.25f, 0.82f, 0.52f) : new Color(0.26f, 0.36f, 0.5f);
            string statsBtnText = _isScoreboardOpen ? "✖ Close [TAB]" : "📊 Stats [TAB]";
            if (GUI.Button(new Rect(utilX, startY + 34, utilBtnW, 29), statsBtnText))
            {
                _isScoreboardOpen = !_isScoreboardOpen;
            }
            GUI.backgroundColor = prevBtnBg;

            // Small footer hint
            GUI.Label(new Rect(startX - 8, startY + slotSize + 4, totalConsoleWidth + 16, 16),
                "Keys: [Q/W/E/R] Skills | [1-6] Items (click sell 70%) | [TAB] Stats | [P] Shop | [T] Talents | A+Click: Move", _hintLabel);
        }

        private void DrawTalentTreeModal()
        {
            if (CurrentPlayerHero == null) return;

            int sw = Screen.width, sh = Screen.height;
            int modalW = 460, modalH = 280;
            int mx = sw / 2 - modalW / 2;
            int my = sh / 2 - modalH / 2 - 40;

            GUI.Box(new Rect(mx, my, modalW, modalH), "");

            var titleStyle = new GUIStyle(_bigLabel) { fontSize = 20 };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(mx, my + 10, modalW, 28), "🌳 HERO TALENT TREE", titleStyle);

            if (GUI.Button(new Rect(mx + modalW - 32, my + 8, 24, 24), "X"))
            {
                _isTalentTreeOpen = false;
            }

            // Tier 3: Level 12
            DrawTalentRow(mx + 20, my + 50, modalW - 40, 12, 3, "+30 Attack Damage", "+250 Max Health", CurrentPlayerHero.TalentTier3Choice);

            // Tier 2: Level 8
            DrawTalentRow(mx + 20, my + 115, modalW - 40, 8, 2, "+10 Armor", "+12% Cooldown Reduc.", CurrentPlayerHero.TalentTier2Choice);

            // Tier 1: Level 4
            DrawTalentRow(mx + 20, my + 180, modalW - 40, 4, 1, "+150 Max Health", "+12 Attack Damage", CurrentPlayerHero.TalentTier1Choice);

            var noteStyle = new GUIStyle(_centerLabel) { fontSize = 11 };
            noteStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(mx, my + 250, modalW, 20), "Permanent stat enhancements per hero branch. Press [T] or [X] to close.", noteStyle);
        }

        private void DrawTalentRow(int rx, int ry, int rw, int reqLevel, int tier, string option1Text, string option2Text, int chosen)
        {
            bool isUnlocked = CurrentPlayerHero.CurrentLevel >= reqLevel;
            int colW = (rw - 60) / 2;

            // Center Level Badge
            var badgeStyle = new GUIStyle(_centerLabel) { fontSize = 13, fontStyle = FontStyle.Bold };
            badgeStyle.normal.textColor = isUnlocked ? new Color(1f, 0.85f, 0.2f) : new Color(0.5f, 0.5f, 0.5f);
            GUI.Box(new Rect(rx + colW + 5, ry + 8, 50, 26), $"Lv {reqLevel}");
            GUI.Label(new Rect(rx + colW + 5, ry + 11, 50, 20), $"{reqLevel}", badgeStyle);

            // Option 1
            Color oldBg = GUI.backgroundColor;
            if (chosen == 0)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                GUI.Box(new Rect(rx, ry, colW, 42), $"✔ {option1Text}");
            }
            else if (isUnlocked && chosen == -1)
            {
                GUI.backgroundColor = new Color(0.9f, 0.75f, 0.15f);
                if (GUI.Button(new Rect(rx, ry, colW, 42), option1Text))
                {
                    CurrentPlayerHero.TrySelectTalent(tier, 0);
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
                GUI.Box(new Rect(rx, ry, colW, 42), option1Text);
            }

            // Option 2
            int op2X = rx + colW + 60;
            if (chosen == 1)
            {
                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                GUI.Box(new Rect(op2X, ry, colW, 42), $"✔ {option2Text}");
            }
            else if (isUnlocked && chosen == -1)
            {
                GUI.backgroundColor = new Color(0.9f, 0.75f, 0.15f);
                if (GUI.Button(new Rect(op2X, ry, colW, 42), option2Text))
                {
                    CurrentPlayerHero.TrySelectTalent(tier, 1);
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.25f, 0.25f, 0.3f);
                GUI.Box(new Rect(op2X, ry, colW, 42), option2Text);
            }

            GUI.backgroundColor = oldBg;
        }

        private float[] GetHeroCooldowns(HeroBase3D hero)
        {
            if (hero is VorkasHero v) return new[] { v.Skill1CooldownRemaining, v.Skill2CooldownRemaining, v.Skill3CooldownRemaining, v.UltimateCooldownRemaining, v.AttackCooldownRemaining };
            if (hero is ZenthisHero z) return new[] { z.Skill1CooldownRemaining, z.Skill2CooldownRemaining, z.Skill3CooldownRemaining, z.UltimateCooldownRemaining, 0f };
            if (hero is KorvaxHero k) return new[] { k.Skill1CooldownRemaining, k.Skill2CooldownRemaining, k.Skill3CooldownRemaining, k.UltimateCooldownRemaining, 0f };
            if (hero is GravitorHero g) return new[] { g.Skill1CooldownRemaining, g.Skill2CooldownRemaining, g.Skill3CooldownRemaining, g.UltimateCooldownRemaining, 0f };
            return new float[5];
        }

        private float[] GetHeroMaxCooldowns(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[] { VorkasHero.Skill1CooldownDuration, VorkasHero.Skill2CooldownDuration, VorkasHero.Skill3CooldownDuration, VorkasHero.UltimateCooldownDuration, 1.1f };
            if (hero is ZenthisHero) return new[] { ZenthisHero.Skill1CooldownDuration, ZenthisHero.Skill2CooldownDuration, ZenthisHero.Skill3CooldownDuration, ZenthisHero.UltimateCooldownDuration, 1.3f };
            if (hero is KorvaxHero) return new[] { KorvaxHero.Skill1CooldownDuration, KorvaxHero.Skill2CooldownDuration, KorvaxHero.Skill3CooldownDuration, KorvaxHero.UltimateCooldownDuration, 1.4f };
            if (hero is GravitorHero) return new[] { GravitorHero.Skill1CooldownDuration, GravitorHero.Skill2CooldownDuration, GravitorHero.Skill3CooldownDuration, GravitorHero.UltimateCooldownDuration, 1.2f };
            return new float[] { 1f, 1f, 1f, 1f, 1f };
        }

        private string[] GetSkillNames(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[] { "Iron Cleave", "Vanguard", "Seismic", "Rebellion", "Auto Atk" };
            if (hero is ZenthisHero) return new[] { "Hourglass", "Aura", "Temporal", "Rewind", "Auto Atk" };
            if (hero is KorvaxHero) return new[] { "Heavy Bolt", "Focus", "Concussive", "Ballista", "Auto Atk" };
            if (hero is GravitorHero) return new[] { "Mag.Pull", "Repulsion", "Grav.Well", "Collapse", "Auto Atk" };
            return new[] { "Skill 1", "Skill 2", "Skill 3", "Ultimate", "Attack" };
        }

        private Color GetSkillColor(int index) => index switch
        {
            0 => new Color(0.25f, 0.55f, 0.95f), // Q: Blue
            1 => new Color(0.25f, 0.75f, 0.55f), // W: Teal/Green
            2 => new Color(0.65f, 0.35f, 0.85f), // E: Purple
            3 => new Color(0.85f, 0.45f, 0.15f), // R: Orange/Gold
            _ => new Color(0.55f, 0.55f, 0.55f)  // A: Gray
        };

        // ══════════════════════════════════════════════════════════
        //  SCOREBOARD & HERO STATS MODAL [TAB] (Section 8 / Phase 3)
        // ══════════════════════════════════════════════════════════
        private void DrawScoreboardModal()
        {
            int sw = Screen.width, sh = Screen.height;
            int modalW = Mathf.Min(840, sw - 40);
            int modalH = Mathf.Min(530, sh - 60);
            int mx = (sw - modalW) / 2;
            int my = (sh - modalH) / 2;

            // Dark translucent overlay backdrop
            var overlayStyle = new GUIStyle(GUI.skin.box);
            if (_overlayDarkTex != null) overlayStyle.normal.background = _overlayDarkTex;
            GUI.Box(new Rect(mx - 6, my - 6, modalW + 12, modalH + 12), "", overlayStyle);
            GUI.Box(new Rect(mx, my, modalW, modalH), "");

            // Header Title
            var titleStyle = new GUIStyle(_bigLabel) { fontSize = 17 };
            titleStyle.normal.textColor = new Color(1f, 0.88f, 0.25f);
            string timeStr = MatchSimulation != null ? FormatTime(MatchSimulation.MatchTime) : "00:00";
            GUI.Label(new Rect(mx, my + 8, modalW, 26), $"⚔ MATCH OVERVIEW & HERO STATS — [{timeStr}]", titleStyle);

            if (GUI.Button(new Rect(mx + modalW - 34, my + 8, 26, 24), "X"))
            {
                _isScoreboardOpen = false;
            }

            // Two Columns: Player (Left / Blue) vs Bot (Right / Red)
            int pad = 12;
            int colW = (modalW - pad * 3) / 2;
            int colH = modalH - 86;
            int colY = my + 38;

            DrawTeamHeroStatsCard(mx + pad, colY, colW, colH, CurrentPlayerHero, isPlayer: true);
            DrawTeamHeroStatsCard(mx + pad * 2 + colW, colY, colW, colH, BotBrain?.BotHero, isPlayer: false);

            // Footer Hotkey Guide
            var footerStyle = new GUIStyle(_centerLabel) { fontSize = 11 };
            footerStyle.normal.textColor = new Color(0.75f, 0.8f, 0.9f);
            GUI.Label(new Rect(mx + 10, my + modalH - 44, modalW - 20, 38),
                "Keybinds: [TAB] Close/Open Stats | [P] Shop (in Fountain) | [T] Talents | [Q/W/E/R] Skills | [Ctrl+Skill] Upgrade\nRight-Click: Move/Attack | Left-Click: Select | A+Click: Attack-Move | [1-6] Item Actives", footerStyle);
        }

        private void DrawTeamHeroStatsCard(int rx, int ry, int rw, int rh, HeroBase3D hero, bool isPlayer)
        {
            GUI.Box(new Rect(rx, ry, rw, rh), "");

            if (hero == null)
            {
                GUI.Label(new Rect(rx, ry + rh / 2 - 10, rw, 20), "No Hero Data Available", _centerLabel);
                return;
            }

            // Header Banner
            var teamHeaderStyle = new GUIStyle(_boldLabel) { fontSize = 13 };
            if (isPlayer)
            {
                teamHeaderStyle.normal.textColor = new Color(0.35f, 0.75f, 1f);
                GUI.Label(new Rect(rx + 10, ry + 6, rw - 20, 20), $"🟦 BLUE TEAM — {hero.DisplayName} (Player)", teamHeaderStyle);
                int k = PlayerWallet != null ? PlayerWallet.TotalKills : 0;
                int d = PlayerWallet != null ? PlayerWallet.TotalDeaths : 0;
                int g = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
                GUI.Label(new Rect(rx + rw - 140, ry + 6, 130, 20), $"K/D: {k}/{d} | {g}g", _boldLabel);
            }
            else
            {
                teamHeaderStyle.normal.textColor = new Color(1f, 0.45f, 0.45f);
                string diffStr = BotBrain != null ? $"[{BotBrain.Difficulty}]" : "";
                string stateStr = BotBrain != null ? $"({BotBrain.CurrentState})" : "";
                GUI.Label(new Rect(rx + 10, ry + 6, rw - 20, 20), $"🟥 RED TEAM — {hero.DisplayName} {diffStr}", teamHeaderStyle);
                var subStyle = new GUIStyle(_hintLabel) { alignment = TextAnchor.MiddleRight };
                GUI.Label(new Rect(rx + rw - 140, ry + 6, 130, 20), stateStr, subStyle);
            }

            int curY = ry + 28;

            // HP & Mana Bars
            float hpRatio = Mathf.Clamp01(hero.CurrentHp / Mathf.Max(1f, hero.EffectiveMaxHp));
            float mpRatio = Mathf.Clamp01(hero.CurrentMana / Mathf.Max(1f, hero.EffectiveMaxMana));
            GUI.Label(new Rect(rx + 10, curY, rw - 20, 16), $"Level {hero.CurrentLevel}  |  HP: {hero.CurrentHp:F0}/{hero.EffectiveMaxHp:F0}  |  MP: {hero.CurrentMana:F0}/{hero.EffectiveMaxMana:F0}", _hintLabel);
            curY += 18;
            DrawBar(new Rect(rx + 10, curY, rw - 20, 8), hpRatio, new Color(0.2f, 0.8f, 0.3f), new Color(0.2f, 0.2f, 0.2f));
            curY += 10;
            DrawBar(new Rect(rx + 10, curY, rw - 20, 6), mpRatio, new Color(0.25f, 0.55f, 0.95f), new Color(0.2f, 0.2f, 0.2f));
            curY += 10;

            // Attribute Table (Two Columns)
            GUI.Box(new Rect(rx + 8, curY, rw - 16, 88), "");
            var statTitleStyle = new GUIStyle(_boldLabel) { fontSize = 11 };
            statTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            GUI.Label(new Rect(rx + 14, curY + 2, rw - 28, 16), "Attributes & Combat Stats", statTitleStyle);

            float physReduc = (1f - 100f / (100f + Mathf.Max(0f, hero.EffectiveArmor))) * 100f;
            float magReduc = (1f - 100f / (100f + Mathf.Max(0f, hero.EffectiveMagicResist))) * 100f;
            int halfW = (rw - 28) / 2;

            var statStyle = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            // Col 1
            GUI.Label(new Rect(rx + 14, curY + 18, halfW, 16), $"⚔ AD: {hero.EffectiveAttackDamage:F0} (Base {hero.BaseAttackDamage:F0})", statStyle);
            GUI.Label(new Rect(rx + 14, curY + 34, halfW, 16), $"🛡 Armor: {hero.EffectiveArmor:F0} ({physReduc:F0}% Reduc)", statStyle);
            GUI.Label(new Rect(rx + 14, curY + 50, halfW, 16), $"🔮 MR: {hero.EffectiveMagicResist:F0} ({magReduc:F0}% Reduc)", statStyle);
            GUI.Label(new Rect(rx + 14, curY + 66, halfW, 16), $"⏱ CDR: {hero.EffectiveCooldownReduction * 100f:F0}%", statStyle);

            // Col 2
            GUI.Label(new Rect(rx + 14 + halfW, curY + 18, halfW, 16), $"👟 Move Speed: {hero.EffectiveMoveSpeed:F1}", statStyle);
            GUI.Label(new Rect(rx + 14 + halfW, curY + 34, halfW, 16), $"🎯 Range: {hero.AttackRange:F1}", statStyle);
            GUI.Label(new Rect(rx + 14 + halfW, curY + 50, halfW, 16), $"⭐ Skill Pts: {hero.AvailableSkillPoints}", statStyle);
            GUI.Label(new Rect(rx + 14 + halfW, curY + 66, halfW, 16), $"🌳 Stats Rank: +{hero.StatBonusRank}/4", statStyle);

            curY += 92;

            // Skill Progression Overview
            string qInfo = $"Q: Lv.{hero.Skill1Rank}/4";
            string wInfo = $"W: Lv.{hero.Skill2Rank}/4";
            string eInfo = $"E: Lv.{hero.Skill3Rank}/4";
            string rInfo = $"R: Lv.{hero.UltimateRank}/3";
            GUI.Label(new Rect(rx + 10, curY, rw - 20, 16), $"Skill Levels:  {qInfo}  |  {wInfo}  |  {eInfo}  |  {rInfo}", _hintLabel);
            curY += 18;

            // Equipped Items (6 Slots)
            int itemBoxH = rh - (curY - ry) - 6;
            GUI.Box(new Rect(rx + 8, curY, rw - 16, itemBoxH), "");
            GUI.Label(new Rect(rx + 14, curY + 2, rw - 28, 16), "Equipped Items (Slots 1–6):", statTitleStyle);

            int itemStartY = curY + 18;
            int itemRowH = (itemBoxH - 22) / 6;
            for (int i = 0; i < 6; i++)
            {
                int iy = itemStartY + i * itemRowH;
                ItemData item = hero.Inventory.GetItemInSlot(i);
                float cd = hero.Inventory.GetActiveCooldownRemaining(i);

                if (item != null)
                {
                    string cdStr = cd > 0f ? $" [CD:{cd:F1}s]" : "";
                    string itemLine = $"[{i + 1}] <b>{item.DisplayName}</b>{cdStr} — <size=10>{item.Description}</size>";
                    int textW = isPlayer ? rw - 106 : rw - 36;
                    GUI.Label(new Rect(rx + 14, iy, textW, itemRowH), itemLine);

                    if (isPlayer)
                    {
                        int sellPrice = item.Cost * 7 / 10;
                        if (GUI.Button(new Rect(rx + rw - 96, iy + 1, 80, Mathf.Max(18, itemRowH - 2)), $"Sell ({sellPrice}g)"))
                        {
                            ShopSystem.TrySellItem(i, PlayerWallet, hero.Inventory);
                        }
                    }
                }
                else
                {
                    var emptyStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
                    emptyStyle.normal.textColor = new Color(0.45f, 0.45f, 0.5f);
                    GUI.Label(new Rect(rx + 14, iy, rw - 28, itemRowH), $"[{i + 1}] <i>Empty Slot</i>", emptyStyle);
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  RIGHT PANEL: Hero Selector / Bot AI
        // ══════════════════════════════════════════════════════════
        private void DrawControlPanel()
        {
            int px = Screen.width - 240, py = 56;
            GUI.Box(new Rect(px, py, 230, 290), "⚙ Hero & Bot Controls");

            string[] heroes = { "Vorkas", "Zenthis", "Korvax", "Gravitor" };

            // 1. Choose Player Hero
            GUI.Label(new Rect(px + 10, py + 20, 210, 18), "Choose Player Hero:", _boldLabel);
            for (int i = 0; i < heroes.Length; i++)
            {
                int bx = px + 10 + (i % 2 * 105), by2 = py + 38 + (i / 2 * 26);
                bool isCurrent = CurrentPlayerHero?.DisplayName == heroes[i];
                Color prev = GUI.backgroundColor;
                if (isCurrent) GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUI.Button(new Rect(bx, by2, 100, 24), heroes[i])) OnHeroSwitched?.Invoke(heroes[i]);
                GUI.backgroundColor = prev;
            }

            // 2. Choose Bot Hero
            GUI.Label(new Rect(px + 10, py + 94, 210, 18), "Choose Bot Hero:", _boldLabel);
            for (int i = 0; i < heroes.Length; i++)
            {
                int bx = px + 10 + (i % 2 * 105), by2 = py + 112 + (i / 2 * 26);
                bool isCurrent = BotBrain?.BotHero?.DisplayName == heroes[i];
                Color prev = GUI.backgroundColor;
                if (isCurrent) GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUI.Button(new Rect(bx, by2, 100, 24), heroes[i])) OnBotHeroSwitched?.Invoke(heroes[i]);
                GUI.backgroundColor = prev;
            }

            // 3. Bot AI Difficulty
            GUI.Label(new Rect(px + 10, py + 168, 210, 18), "Bot AI Difficulty:", _boldLabel);
            BotDifficulty[] diffs = { BotDifficulty.Easy, BotDifficulty.Medium, BotDifficulty.Hard };
            for (int i = 0; i < diffs.Length; i++)
            {
                int bx = px + 10 + (i * 70), by2 = py + 188;
                bool isCurrent = BotBrain != null && BotBrain.Difficulty == diffs[i];
                Color prev = GUI.backgroundColor;
                if (isCurrent) GUI.backgroundColor = new Color(1f, 0.65f, 0.2f);
                string label = isCurrent ? $"[{diffs[i]}]" : diffs[i].ToString();
                if (GUI.Button(new Rect(bx, by2, 66, 24), label) && BotBrain != null) BotBrain.Difficulty = diffs[i];
                GUI.backgroundColor = prev;
            }

            if (BotBrain != null && BotBrain.BotHero != null)
            {
                GUI.Label(new Rect(px + 10, py + 220, 210, 20), $"Bot: {BotBrain.BotHero.DisplayName} [{BotBrain.CurrentState}]");
                GUI.Label(new Rect(px + 10, py + 240, 210, 20), $"Bot HP: {BotBrain.BotHero.CurrentHp:F0} / {BotBrain.BotHero.EffectiveMaxHp:F0}");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  KILL FEED
        // ══════════════════════════════════════════════════════════
        private void DrawKillFeed()
        {
            int sy = 56;
            for (int i = 0; i < _killFeed.Count; i++)
                GUI.Box(new Rect(10, sy + (i * 26), 340, 24), $"📢 {_killFeed[i]}");
        }

        // ══════════════════════════════════════════════════════════
        //  SHOP CATALOG
        // ══════════════════════════════════════════════════════════
        private void DrawShopCatalog()
        {
            int w = 500, h = 380, x = (Screen.width - w) / 2, y = (Screen.height - h) / 2;
            GUI.Box(new Rect(x, y, w, h), "🏪 KOA Duel Shop — 8 Items (Section 5.2)");

            var items = ShopSystem.AvailableCatalog;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int iy = y + 35 + (i * 38);
                GUI.Label(new Rect(x + 15, iy, 250, 30), $"<b>{item.DisplayName}</b>\n<size=10>{item.Description}</size>");
                GUI.Label(new Rect(x + 330, iy + 5, 60, 25), $"{item.Cost}g");
                bool canAfford = PlayerWallet != null && PlayerWallet.CurrentGold >= item.Cost;
                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(x + 410, iy + 3, 70, 28), canAfford ? "Buy" : "—") && CurrentPlayerHero != null && PlayerWallet != null)
                    ShopSystem.TryBuyItem(item, PlayerWallet, CurrentPlayerHero.Inventory);
                GUI.enabled = true;
            }

            if (GUI.Button(new Rect(x + w - 80, y + 4, 70, 22), "Close")) _isShopOpen = false;
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }

        /// <summary>
        /// ตรวจสอบว่าผู้เล่นอยู่ใน Fountain Zone หรือไม่ (Section 5.1 — เปิด Shop ได้เฉพาะใน Fountain)
        /// </summary>
        private bool IsPlayerInFountain()
        {
            if (CurrentPlayerHero == null || MatchSimulation == null) return false;
            float dist = Vector3.Distance(CurrentPlayerHero.Position, MatchSimulation.BlueFountainPos);
            return dist <= KOA.Core.Match.MatchSimulation.FountainZoneRadius;
        }
    }
}
