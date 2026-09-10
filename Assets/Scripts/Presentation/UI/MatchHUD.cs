using KOA.Core.AI;
using KOA.Core.Economy;
using KOA.Core.Entities;
using KOA.Core.Items;
using KOA.Core.Match;
using KOA.Core.Vision;
using KOA.Core.World;
using KOA.Data.Enums;
using KOA.Data.Models;
using KOA.Presentation.Views;
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
        private bool _isAttributePanelOpen = false;
        private bool _isScoreboardOpen = false;
        private bool _isHeroProfileOpen = false;
        private bool _isControlPanelExpanded = true;
        private bool _isMiniMapExpanded = false;
        private int _selectedShopCategory = 0;
        private int _hoveredSkillIndex = -1;
        private float _nextAbilityRejectMessageAt;
        private string _abilityFeedbackMessage = string.Empty;
        private float _abilityFeedbackExpiresAt;
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
        private GUIStyle _abilityFeedbackStyle;
        private GUIStyle _solidBoxStyle;
        private GUIStyle _panelFrameStyle;
        private AudioSource _uiAudioSource;
        private HeroPortraitPreview _portraitPreview;
        private KOA.Presentation.Camera.TopDownCameraController _cameraController;
        private static MatchHUD _activeHud;

        private void Awake()
        {
            _activeHud = this;
            if (ShopSystem == null) ShopSystem = new ShopSystem();
            _uiAudioSource = gameObject.AddComponent<AudioSource>();
            _uiAudioSource.playOnAwake = false;
            _uiAudioSource.spatialBlend = 0f;
            _portraitPreview = gameObject.AddComponent<HeroPortraitPreview>();
        }

        private void OnDestroy()
        {
            if (_activeHud == this) _activeHud = null;
            if (MatchSimulation != null)
            {
                MatchSimulation.OnKillFeedMessage -= AddKillFeed;
                MatchSimulation.OnGameOver -= HandleGameOver;
            }
            if (CurrentPlayerHero != null)
            {
                CurrentPlayerHero.OnDied -= HandlePlayerDied;
                CurrentPlayerHero.OnRespawned -= HandlePlayerRespawned;
            }

            if (_vignetteTexture != null) Destroy(_vignetteTexture);
            if (_overlayDarkTex != null) Destroy(_overlayDarkTex);
            if (_redTex != null) Destroy(_redTex);
            if (_goldTex != null) Destroy(_goldTex);
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
            if (_killFeed.Count > 3) _killFeed.RemoveAt(0);
            _killFeedTimer = 5.0f;
        }

        private void ShowAbilityFeedback(string message)
        {
            _abilityFeedbackMessage = message;
            _abilityFeedbackExpiresAt = Time.unscaledTime + 1.6f;
        }

        public static void NotifyAbilityRejected(HeroBase3D hero, int skillIndex, string targetingReason = null)
        {
            if (_activeHud == null || hero == null || skillIndex < 0 || skillIndex > 3) return;
            if (Time.unscaledTime < _activeHud._nextAbilityRejectMessageAt) return;
            _activeHud._nextAbilityRejectMessageAt = Time.unscaledTime + 0.65f;

            int[] ranks = { hero.Skill1Rank, hero.Skill2Rank, hero.Skill3Rank, hero.UltimateRank };
            string[] keys = { "Q", "W", "E", "R" };
            float[] costs = GetHeroManaCosts(hero);
            float[] cooldowns = _activeHud.GetHeroCooldowns(hero);
            string message;
            if (ranks[skillIndex] <= 0)
                message = $"{keys[skillIndex]} is not learned yet";
            else if (cooldowns[skillIndex] > 0.02f)
                message = $"{keys[skillIndex]} cooldown: {cooldowns[skillIndex]:F1}s";
            else if (hero.CurrentMana + 0.001f < costs[skillIndex])
                message = $"Not enough mana — {keys[skillIndex]} needs {costs[skillIndex]:F0} MP";
            else if (!string.IsNullOrEmpty(targetingReason))
                message = targetingReason;
            else
                message = $"{keys[skillIndex]} cannot be cast in the current state";

            _activeHud.ShowAbilityFeedback(message);
            _activeHud.PlayUiSound("error_004", 0.55f);
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

            _abilityFeedbackStyle = new GUIStyle(_centerLabel)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };

            // Reuse one engine-owned texture for all solid bars. Creating Texture2D in OnGUI
            // exhausts Unity graphics resource IDs after only a few minutes of play.
            _solidBoxStyle = new GUIStyle(GUI.skin.box);
            _solidBoxStyle.normal.background = Texture2D.whiteTexture;

            _overlayDarkTex = MakeTex(1, 1, new Color(0.015f, 0.022f, 0.028f, 0.88f));
            _redTex = MakeTex(1, 1, new Color(0.85f, 0.05f, 0.05f));
            _goldTex = MakeTex(1, 1, new Color(0.95f, 0.78f, 0.1f));

            // Keep in-match HUD framing quiet and resolution independent. Decorative
            // source textures are reserved for large modal layouts after nine-slice QA.
            _panelFrameStyle = new GUIStyle(GUI.skin.box);
            _panelFrameStyle.normal.background = _overlayDarkTex;
            _panelFrameStyle.border = new RectOffset(0, 0, 0, 0);
            _panelFrameStyle.padding = new RectOffset(8, 8, 7, 7);

            _vignetteTexture = CreateVignetteTex(256, 256);
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
                if (_killFeedTimer <= 0f) _killFeed.Clear();
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
            bool f1Pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                pPressed = Keyboard.current.pKey.wasPressedThisFrame;
                tPressed = Keyboard.current.tKey.wasPressedThisFrame;
                tabPressed = Keyboard.current.tabKey.wasPressedThisFrame;
                f1Pressed = Keyboard.current.f1Key.wasPressedThisFrame;
            }
            else
#endif
            {
                pPressed = UnityEngine.Input.GetKeyDown(KeyCode.P);
                tPressed = UnityEngine.Input.GetKeyDown(KeyCode.T);
                tabPressed = UnityEngine.Input.GetKeyDown(KeyCode.Tab);
                f1Pressed = UnityEngine.Input.GetKeyDown(KeyCode.F1);
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
            if (f1Pressed) _isHeroProfileOpen = !_isHeroProfileOpen;

            // MOBA Standard Quick Level-up Hotkeys: Ctrl + Q/W/E/R; Ctrl+U opens Attributes (Section 6.7)
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

            if (isCtrl && CurrentPlayerHero != null)
            {
                if (uDown) _isAttributePanelOpen = !_isAttributePanelOpen;
                else if (CurrentPlayerHero.AvailableSkillPoints > 0)
                {
                    if (qDown) CurrentPlayerHero.TryLevelSkill1();
                    else if (wDown) CurrentPlayerHero.TryLevelSkill2();
                    else if (eDown) CurrentPlayerHero.TryLevelSkill3();
                    else if (rDown) CurrentPlayerHero.TryLevelUltimate();
                }
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
            DrawMiniMap();
            DrawHeroBottomPanel();
            DrawSkillBar();
            DrawAbilityFeedback();
            DrawControlPanel();
            DrawKillFeed();

            if (_isShopOpen) DrawShopCatalog();
            if (_isTalentTreeOpen) DrawTalentTreeModal();
            if (_isAttributePanelOpen) DrawAttributePanelModal();
            if (_isScoreboardOpen) DrawScoreboardModal();
            if (_isHeroProfileOpen) DrawHeroProfileModal();
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

        private void DrawPanelFrame(Rect rect, string title = "")
        {
            GUI.Box(rect, title, _panelFrameStyle ?? GUI.skin.box);
            Color edge = new Color(0.28f, 0.40f, 0.46f, 0.78f);
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 1f), edge);
            DrawSolidRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), edge);
            DrawSolidRect(new Rect(rect.x, rect.y, 1f, rect.height), edge);
            DrawSolidRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), edge);
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
        //  MINI MAP — อ่านสถานะจาก Core และแสดงผลใน Presentation เท่านั้น
        // ══════════════════════════════════════════════════════════
        private void DrawMiniMap()
        {
            if (MatchSimulation == null) return;

            Rect panelRect = GetMiniMapPanelRect();
            DrawSolidRect(panelRect, new Color(0.025f, 0.035f, 0.04f, 0.9f));
            DrawPanelFrame(panelRect);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 3f, panelRect.width, 20f), "MINI MAP", _hintLabel);
            if (GUI.Button(new Rect(panelRect.xMax - 28f, panelRect.y + 3f, 23f, 20f), _isMiniMapExpanded ? "−" : "+"))
            {
                _isMiniMapExpanded = !_isMiniMapExpanded;
                PlayUiSound(_isMiniMapExpanded ? "maximize_003" : "minimize_003", 0.55f);
                panelRect = GetMiniMapPanelRect();
            }

            Rect mapRect = new Rect(panelRect.x + 10f, panelRect.y + 25f, panelRect.width - 20f, panelRect.height - 35f);
            DrawSolidRect(mapRect, new Color(0.10f, 0.19f, 0.17f, 0.98f));

            float laneWidth = mapRect.width * (12.2f / (ArenaBounds.HalfWidth * 2f));
            Rect laneRect = new Rect(mapRect.center.x - laneWidth * 0.5f, mapRect.y, laneWidth, mapRect.height);
            DrawSolidRect(laneRect, new Color(0.31f, 0.29f, 0.25f, 1f));
            DrawSolidRect(new Rect(laneRect.x, mapRect.y, 2f, mapRect.height), new Color(0.60f, 0.52f, 0.35f, 0.9f));
            DrawSolidRect(new Rect(laneRect.xMax - 2f, mapRect.y, 2f, mapRect.height), new Color(0.60f, 0.52f, 0.35f, 0.9f));
            DrawSolidRect(new Rect(mapRect.x, mapRect.center.y - 1f, mapRect.width, 2f), new Color(0.95f, 0.68f, 0.16f, 0.75f));

            DrawMiniMapZone(mapRect, MatchSimulation.BlueFountainPos, new Color(0.1f, 0.75f, 0.95f, 0.38f));
            DrawMiniMapZone(mapRect, MatchSimulation.RedFountainPos, new Color(0.95f, 0.18f, 0.16f, 0.38f));

            DrawMiniMapStructures(mapRect, MatchSimulation.BlueTowers, new Color(0.1f, 0.85f, 1f));
            DrawMiniMapStructures(mapRect, MatchSimulation.RedTowers, new Color(1f, 0.18f, 0.15f));

            for (int i = 0; i < MatchSimulation.ActiveMinions.Count; i++)
            {
                var minion = MatchSimulation.ActiveMinions[i];
                if (minion == null || !minion.IsAlive) continue;
                Color color = minion.TeamId == 0
                    ? new Color(0.2f, 0.78f, 1f, 0.95f)
                    : new Color(1f, 0.32f, 0.28f, 0.95f);
                DrawMiniMapMarker(mapRect, minion.Position, 3f, color);
            }

            if (MatchSimulation.BlueHero != null && MatchSimulation.BlueHero.IsAlive)
                DrawMiniMapHero(mapRect, MatchSimulation.BlueHero, MatchSimulation.BlueHero == CurrentPlayerHero);

            if (MatchSimulation.RedHero != null && MatchSimulation.RedHero.IsAlive
                && (CurrentPlayerHero == null || VisionSystem.IsHeroVisible(CurrentPlayerHero, MatchSimulation.RedHero)))
                DrawMiniMapHero(mapRect, MatchSimulation.RedHero, MatchSimulation.RedHero == CurrentPlayerHero);

            DrawMiniMapFrame(mapRect);

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && mapRect.Contains(currentEvent.mousePosition))
            {
                Vector3 worldPoint = MiniMapToWorld(mapRect, currentEvent.mousePosition);
                if (currentEvent.button == 0)
                {
                    GetCameraController()?.PanToWorldPosition(worldPoint);
                    PlayUiSound("click_003", 0.42f);
                }
                else if (currentEvent.button == 1 && CurrentPlayerHero != null)
                {
                    CurrentPlayerHero.CurrentAttackTarget = null;
                    CurrentPlayerHero.SetMoveDestination(worldPoint);
                    ShowMiniMapMoveMarker(worldPoint);
                    PlayUiSound("confirmation_002", 0.45f);
                }
                currentEvent.Use();
            }
        }

        private Rect GetMiniMapPanelRect()
        {
            float panelWidth = _isMiniMapExpanded ? Mathf.Min(340f, Screen.width * 0.34f) : 176f;
            float panelHeight = _isMiniMapExpanded
                ? Mathf.Min(540f, Screen.height - 132f)
                : Mathf.Clamp(Screen.height * 0.29f, 210f, 278f);
            return new Rect(12f, 56f, panelWidth, panelHeight);
        }

        private static Vector3 MiniMapToWorld(Rect mapRect, Vector2 guiPoint)
        {
            float normalizedX = Mathf.InverseLerp(mapRect.xMin, mapRect.xMax, guiPoint.x);
            float normalizedZ = 1f - Mathf.InverseLerp(mapRect.yMin, mapRect.yMax, guiPoint.y);
            return new Vector3(
                Mathf.Lerp(-ArenaBounds.HalfWidth, ArenaBounds.HalfWidth, normalizedX),
                0f,
                Mathf.Lerp(-ArenaBounds.HalfLength, ArenaBounds.HalfLength, normalizedZ));
        }

        private KOA.Presentation.Camera.TopDownCameraController GetCameraController()
        {
            if (_cameraController == null && UnityEngine.Camera.main != null)
                _cameraController = UnityEngine.Camera.main.GetComponent<KOA.Presentation.Camera.TopDownCameraController>();
            return _cameraController;
        }

        private void ShowMiniMapMoveMarker(Vector3 worldPoint)
        {
            HeroView[] views = FindObjectsByType<HeroView>();
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].Hero == CurrentPlayerHero)
                {
                    views[i].ShowMoveCommand(worldPoint, false);
                    break;
                }
            }
        }

        private void DrawMiniMapStructures(Rect mapRect, List<KOA.Core.Structures.TowerEntity> structures, Color teamColor)
        {
            if (structures == null) return;
            for (int i = 0; i < structures.Count; i++)
            {
                var structure = structures[i];
                if (structure == null) continue;
                bool isNexus = structure.TowerId != null && structure.TowerId.Contains("nexus");
                float size = isNexus ? 10f : 7f;
                Color color = structure.IsDestroyed ? new Color(0.22f, 0.22f, 0.22f, 0.9f) : teamColor;
                DrawMiniMapMarker(mapRect, structure.Position, size, color);
            }
        }

        private void DrawMiniMapHero(Rect mapRect, HeroBase3D hero, bool isPlayer)
        {
            Color teamColor = hero.TeamId == 0 ? new Color(0.1f, 0.9f, 1f) : new Color(1f, 0.16f, 0.12f);
            if (isPlayer) DrawMiniMapMarker(mapRect, hero.Position, 13f, new Color(1f, 0.9f, 0.15f));
            DrawMiniMapMarker(mapRect, hero.Position, isPlayer ? 8f : 9f, teamColor);
        }

        private void DrawMiniMapZone(Rect mapRect, Vector3 worldPosition, Color color)
        {
            Vector2 point = WorldToMiniMap(mapRect, worldPosition);
            DrawSolidRect(new Rect(point.x - 14f, point.y - 6f, 28f, 12f), color);
        }

        private void DrawMiniMapMarker(Rect mapRect, Vector3 worldPosition, float size, Color color)
        {
            Vector2 point = WorldToMiniMap(mapRect, worldPosition);
            DrawSolidRect(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), color);
        }

        private static Vector2 WorldToMiniMap(Rect mapRect, Vector3 worldPosition)
        {
            Vector2 normalized = ArenaBounds.ToNormalized(worldPosition);
            return new Vector2(
                Mathf.Lerp(mapRect.xMin, mapRect.xMax, normalized.x),
                Mathf.Lerp(mapRect.yMax, mapRect.yMin, normalized.y));
        }

        private static void DrawMiniMapFrame(Rect rect)
        {
            Color frame = new Color(0.72f, 0.64f, 0.42f, 0.95f);
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), frame);
            DrawSolidRect(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), frame);
            DrawSolidRect(new Rect(rect.x, rect.y, 2f, rect.height), frame);
            DrawSolidRect(new Rect(rect.xMax - 2f, rect.y, 2f, rect.height), frame);
        }

        private static void DrawSolidRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        /// <summary>
        /// Presentation input shield used by the PC input adapter. Unity IMGUI does not expose
        /// EventSystem pointer blocking, so the HUD publishes its interactive rectangles explicitly.
        /// </summary>
        public static bool IsPointerOverAnyHud(Vector2 screenPoint)
        {
            return _activeHud != null && _activeHud.IsPointerOverHud(screenPoint);
        }

        private bool IsPointerOverHud(Vector2 screenPoint)
        {
            Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            if (IsGameOver || _isShopOpen || _isTalentTreeOpen || _isAttributePanelOpen || _isScoreboardOpen || _isHeroProfileOpen)
                return true;

            if (new Rect(0f, 0f, Screen.width, 58f).Contains(guiPoint)) return true;
            if (GetMiniMapPanelRect().Contains(guiPoint)) return true;
            if (new Rect(10f, Screen.height - 190f, 220f, 180f).Contains(guiPoint)) return true;

            const float consoleWidth = 622f;
            Rect actionConsole = new Rect(Screen.width * 0.5f - consoleWidth * 0.5f, Screen.height - 128f, consoleWidth, 126f);
            if (actionConsole.Contains(guiPoint)) return true;

            Rect controls = _isControlPanelExpanded
                ? new Rect(Screen.width - 240f, 56f, 230f, 324f)
                : new Rect(Screen.width - 240f, 56f, 230f, 32f);
            return controls.Contains(guiPoint);
        }

        // ══════════════════════════════════════════════════════════
        //  TOP BAR
        // ══════════════════════════════════════════════════════════
        private void DrawTopBar()
        {
            int sw = Screen.width;
            DrawPanelFrame(new Rect(0, 0, sw, 46));

            int gold = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
            int streak = PlayerWallet != null ? PlayerWallet.CurrentKillStreak : 0;
            string matchTimeStr = MatchSimulation != null ? FormatTime(MatchSimulation.MatchTime) : "00:00";

            GUI.Label(new Rect(16, 12, 280, 26), $"🪙 Gold: {gold}g    🔥 Streak: {streak}", _boldLabel);

            var timeStyle = new GUIStyle(_boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(sw / 2 - 70, 8, 140, 32), $"⏱  {matchTimeStr}", timeStyle);

            Color previousHeroButtonColor = GUI.backgroundColor;
            if (_isHeroProfileOpen) GUI.backgroundColor = new Color(0.65f, 0.48f, 0.18f);
            if (GUI.Button(new Rect(sw - 485, 8, 125, 30), _isHeroProfileOpen ? "✖ Hero [F1]" : "Hero Info [F1]"))
            {
                _isHeroProfileOpen = !_isHeroProfileOpen;
                PlayUiSound(_isHeroProfileOpen ? "open_002" : "close_002", 0.5f);
            }
            GUI.backgroundColor = previousHeroButtonColor;

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
            DrawPanelFrame(new Rect(panelX, panelY, panelW, panelH));

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
            Color previousColor = GUI.color;
            GUI.color = bgColor;
            GUI.Box(fullRect, "", _solidBoxStyle);

            ratio = Mathf.Clamp01(ratio);
            if (ratio > 0f)
            {
                GUI.color = fillColor;
                GUI.Box(new Rect(fullRect.x, fullRect.y, fullRect.width * ratio, fullRect.height), "", _solidBoxStyle);
            }
            GUI.color = previousColor;
        }

        // ══════════════════════════════════════════════════════════
        //  SKILL BAR (Q / W / E / Auto) — MOBA Standard Progression
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
            _hoveredSkillIndex = -1;

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

                GUI.backgroundColor = Color.white;
            }

            // 2. Background box for entire console
            DrawPanelFrame(new Rect(startX - 8, startY - 4, totalConsoleWidth + 16, slotSize + 8));

            // 3. Draw Skill Slots (Q, W, E, R, [A])
            float[] cds = GetHeroCooldowns(CurrentPlayerHero);
            float[] maxCds = GetHeroMaxCooldowns(CurrentPlayerHero);
            float[] manaCosts = GetHeroManaCosts(CurrentPlayerHero);
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
                float manaCost = i < manaCosts.Length ? manaCosts[i] : 0f;
                int rank = ranks[i];
                int maxRank = maxRanks[i];

                Rect slotRect = new Rect(x, y, slotSize, slotSize);
                if (slotRect.Contains(Event.current.mousePosition)) _hoveredSkillIndex = i;
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
                else if (cd <= 0f && CurrentPlayerHero.CurrentMana + 0.001f < manaCost)
                {
                    GUI.backgroundColor = new Color(0.16f, 0.22f, 0.38f);
                    GUI.Box(slotRect, "");
                    GUI.backgroundColor = oldBg;

                    GUI.Label(new Rect(x + 4, y + 2, 26, 18), keys[i], _boldLabel);
                    var sml = new GUIStyle(_centerLabel) { fontSize = 10 };
                    GUI.Label(new Rect(x, y + 19, slotSize, 18), names[i], sml);
                    var manaStyle = new GUIStyle(_centerLabel) { fontSize = 9, fontStyle = FontStyle.Bold };
                    manaStyle.normal.textColor = new Color(0.35f, 0.68f, 1f);
                    GUI.Label(new Rect(x, y + 37, slotSize, 20), $"NO MANA\n{manaCost:F0} MP", manaStyle);
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
                    Color previousColor = GUI.color;
                    GUI.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
                    GUI.Box(new Rect(x, y + slotSize * (1f - cdRatio), slotSize, slotSize * cdRatio), "", _solidBoxStyle);
                    GUI.color = previousColor;

                    GUI.Label(new Rect(x + 4, y + 2, 26, 18), keys[i], _boldLabel);
                    var cdNum = new GUIStyle(_centerLabel) { fontSize = 20, fontStyle = FontStyle.Bold };
                    cdNum.normal.textColor = Color.white;
                    GUI.Label(new Rect(x, y + 16, slotSize, 26), $"{cd:F1}", cdNum);
                    var sml = new GUIStyle(_centerLabel) { fontSize = 10 };
                    GUI.Label(new Rect(x, y + 42, slotSize, 18), names[i], sml);
                }

                // MOBA Standard Skill Rank Pips under slot
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

            // 5. Utility Buttons (Talents, Attributes, Stats)
            bool hasAvailableTalents = (CurrentPlayerHero.CurrentLevel >= 4 && CurrentPlayerHero.TalentTier1Choice == -1)
                                    || (CurrentPlayerHero.CurrentLevel >= 8 && CurrentPlayerHero.TalentTier2Choice == -1)
                                    || (CurrentPlayerHero.CurrentLevel >= 12 && CurrentPlayerHero.TalentTier3Choice == -1);

            int utilX = invStartX + totalInvWidth + sectionGap;

            Color prevBtnBg = GUI.backgroundColor;
            GUI.backgroundColor = hasAvailableTalents ? new Color(1f, 0.85f, 0.15f) : new Color(0.32f, 0.32f, 0.42f);
            string talentText = hasAvailableTalents ? "★ TALENTS [T]" : "Talents [T]";
            if (GUI.Button(new Rect(utilX, startY + 1, utilBtnW, 19), talentText))
            {
                _isTalentTreeOpen = !_isTalentTreeOpen;
            }

            GUI.backgroundColor = CurrentPlayerHero.AvailableAttributePoints > 0 ? new Color(0.2f, 0.8f, 0.5f) : new Color(0.30f, 0.38f, 0.42f);
            string attributeText = CurrentPlayerHero.AvailableAttributePoints > 0
                ? $"★ ATTR ({CurrentPlayerHero.AvailableAttributePoints})"
                : "Attributes [^U]";
            if (GUI.Button(new Rect(utilX, startY + 23, utilBtnW, 19), attributeText))
            {
                _isAttributePanelOpen = !_isAttributePanelOpen;
            }

            GUI.backgroundColor = _isScoreboardOpen ? new Color(0.25f, 0.82f, 0.52f) : new Color(0.26f, 0.36f, 0.5f);
            string statsBtnText = _isScoreboardOpen ? "✖ Close [TAB]" : "📊 Stats [TAB]";
            if (GUI.Button(new Rect(utilX, startY + 45, utilBtnW, 19), statsBtnText))
            {
                _isScoreboardOpen = !_isScoreboardOpen;
            }
            GUI.backgroundColor = prevBtnBg;

            // Small footer hint
            GUI.Label(new Rect(startX - 8, startY + slotSize + 4, totalConsoleWidth + 16, 16),
                "Keys: [Q/W/E/R] Skills | [F1] Hero | [TAB] Stats | [P] Shop | [T] Talents | [Ctrl+U] Attributes", _hintLabel);

            if (_hoveredSkillIndex >= 0) DrawSkillTooltip(_hoveredSkillIndex, startY);
        }

        private void DrawSkillTooltip(int skillIndex, int skillBarY)
        {
            string[] names = GetSkillNames(CurrentPlayerHero);
            string[] descriptions = GetSkillDescriptions(CurrentPlayerHero);
            string[] targetingHints = GetSkillTargetingHints(CurrentPlayerHero);
            float[] manaCosts = GetHeroManaCosts(CurrentPlayerHero);
            float[] cooldowns = GetHeroMaxCooldowns(CurrentPlayerHero);
            string[] keys = { "Q", "W", "E", "R", "A" };

            const float width = 390f;
            const float height = 130f;
            float x = Mathf.Clamp(Event.current.mousePosition.x - width * 0.5f, 8f, Screen.width - width - 8f);
            float y = Mathf.Max(62f, skillBarY - height - 12f);
            Rect rect = new Rect(x, y, width, height);
            DrawSolidRect(rect, new Color(0.025f, 0.035f, 0.055f, 0.97f));
            GUI.Box(rect, "");

            var title = new GUIStyle(_boldLabel) { fontSize = 14 };
            title.normal.textColor = GetSkillColor(skillIndex);
            GUI.Label(new Rect(x + 12f, y + 7f, width - 24f, 22f), $"[{keys[skillIndex]}] {names[skillIndex]}", title);
            string resource = skillIndex < 4
                ? $"Mana {manaCosts[skillIndex]:F0}   •   Base cooldown {cooldowns[skillIndex]:F0}s"
                : "Basic attack — no mana cost";
            GUI.Label(new Rect(x + 12f, y + 30f, width - 24f, 18f), resource, _hintLabel);
            var targetingStyle = new GUIStyle(_hintLabel) { alignment = TextAnchor.MiddleLeft };
            targetingStyle.normal.textColor = new Color(0.95f, 0.74f, 0.28f);
            GUI.Label(new Rect(x + 12f, y + 49f, width - 24f, 18f), targetingHints[skillIndex], targetingStyle);
            var body = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true };
            body.normal.textColor = new Color(0.88f, 0.91f, 0.96f);
            GUI.Label(new Rect(x + 12f, y + 69f, width - 24f, 54f), descriptions[skillIndex], body);
        }

        private void DrawTalentTreeModal()
        {
            if (CurrentPlayerHero == null) return;

            int sw = Screen.width, sh = Screen.height;
            int modalW = 460, modalH = 280;
            int mx = sw / 2 - modalW / 2;
            int my = sh / 2 - modalH / 2 - 40;

            DrawPanelFrame(new Rect(mx, my, modalW, modalH));

            var titleStyle = new GUIStyle(_bigLabel) { fontSize = 20 };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUI.Label(new Rect(mx, my + 10, modalW, 28), "🌳 HERO TALENT TREE", titleStyle);

            if (GUI.Button(new Rect(mx + modalW - 32, my + 8, 24, 24), "X"))
            {
                _isTalentTreeOpen = false;
            }

            // Tier 3: Level 12
            DrawTalentRow(mx + 20, my + 50, modalW - 40, 12, 3, "+20% Move Speed", "-10% Damage Taken", CurrentPlayerHero.TalentTier3Choice);

            // Tier 2: Level 8
            DrawTalentRow(mx + 20, my + 115, modalW - 40, 8, 2, "+15% Attack Speed", "+15% Skill Damage", CurrentPlayerHero.TalentTier2Choice);

            // Tier 1: Level 4
            DrawTalentRow(mx + 20, my + 180, modalW - 40, 4, 1, "+75 Max Health", "+10% Cooldown Reduc.", CurrentPlayerHero.TalentTier1Choice);

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

        private void DrawAttributePanelModal()
        {
            if (CurrentPlayerHero == null) return;

            int modalW = 520, modalH = 300;
            int mx = Screen.width / 2 - modalW / 2;
            int my = Screen.height / 2 - modalH / 2 - 30;
            GUI.Box(new Rect(mx, my, modalW, modalH), "");

            var titleStyle = new GUIStyle(_bigLabel) { fontSize = 20 };
            titleStyle.normal.textColor = new Color(0.3f, 0.9f, 0.65f);
            GUI.Label(new Rect(mx, my + 10, modalW, 28), "HERO ATTRIBUTES", titleStyle);
            GUI.Label(new Rect(mx + 20, my + 42, modalW - 40, 22),
                $"Available Attribute Points: {CurrentPlayerHero.AvailableAttributePoints}", _centerLabel);

            if (GUI.Button(new Rect(mx + modalW - 32, my + 8, 24, 24), "X"))
                _isAttributePanelOpen = false;

            HeroAttribute[] attributes = { HeroAttribute.Vitality, HeroAttribute.Focus, HeroAttribute.Armor, HeroAttribute.Resolve };
            int[] pointCounts = { CurrentPlayerHero.VitalityPoints, CurrentPlayerHero.FocusPoints, CurrentPlayerHero.ArmorPoints, CurrentPlayerHero.ResolvePoints };
            string[] descriptions =
            {
                "+2% current Max HP per point",
                "+2% current Max Mana per point",
                "+1 Armor per point",
                "+1 Magic Resist per point"
            };

            for (int i = 0; i < attributes.Length; i++)
            {
                int rowY = my + 72 + i * 48;
                GUI.Box(new Rect(mx + 20, rowY, modalW - 40, 40), "");
                GUI.Label(new Rect(mx + 32, rowY + 3, 160, 20), $"{attributes[i]}  Lv.{pointCounts[i]}", _boldLabel);
                GUI.Label(new Rect(mx + 190, rowY + 3, 200, 20), descriptions[i], _hintLabel);
                GUI.enabled = CurrentPlayerHero.AvailableAttributePoints > 0;
                if (GUI.Button(new Rect(mx + modalW - 100, rowY + 7, 68, 26), "+1"))
                    CurrentPlayerHero.TrySpendAttributePoint(attributes[i]);
                GUI.enabled = true;
            }

            GUI.Label(new Rect(mx, my + modalH - 24, modalW, 18),
                "One separate Attribute Point is earned each level. Ctrl+U toggles this panel.", _hintLabel);
        }

        private void DrawHeroProfileModal()
        {
            if (CurrentPlayerHero == null) return;

            int modalW = Mathf.Min(930, Screen.width - 36);
            int modalH = Mathf.Min(590, Screen.height - 52);
            int mx = (Screen.width - modalW) / 2;
            int my = (Screen.height - modalH) / 2;
            DrawSolidRect(new Rect(mx - 6, my - 6, modalW + 12, modalH + 12), new Color(0.01f, 0.018f, 0.03f, 0.96f));
            GUI.Box(new Rect(mx, my, modalW, modalH), "");

            var nameStyle = new GUIStyle(_bigLabel) { fontSize = 24, alignment = TextAnchor.MiddleLeft };
            nameStyle.normal.textColor = new Color(0.95f, 0.82f, 0.28f);
            GUI.Label(new Rect(mx + 22, my + 10, modalW - 80, 34), CurrentPlayerHero.DisplayName.ToUpperInvariant(), nameStyle);
            GUI.Label(new Rect(mx + 24, my + 42, modalW - 80, 22), GetHeroRole(CurrentPlayerHero), _boldLabel);
            if (GUI.Button(new Rect(mx + modalW - 42, my + 12, 28, 26), "X"))
            {
                _isHeroProfileOpen = false;
                PlayUiSound("close_002", 0.55f);
            }

            int portraitW = Mathf.Min(300, modalW / 3);
            Rect portraitRect = new Rect(mx + 20, my + 74, portraitW, modalH - 98);
            DrawSolidRect(portraitRect, new Color(0.025f, 0.06f, 0.08f, 0.96f));
            GUI.Box(portraitRect, "");
            Texture portrait = _portraitPreview != null ? _portraitPreview.GetTexture(CurrentPlayerHero) : null;
            if (portrait != null)
                GUI.DrawTexture(new Rect(portraitRect.x + 6, portraitRect.y + 6, portraitRect.width - 12, portraitRect.height * 0.66f), portrait, ScaleMode.ScaleToFit, true);
            else
                GUI.Label(new Rect(portraitRect.x + 8, portraitRect.y + 50, portraitRect.width - 16, 30), "MODEL PREVIEW LOADING", _centerLabel);

            var loreStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, wordWrap = true, alignment = TextAnchor.UpperLeft };
            loreStyle.normal.textColor = new Color(0.78f, 0.84f, 0.9f);
            float loreY = portraitRect.y + portraitRect.height * 0.68f;
            GUI.Label(new Rect(portraitRect.x + 12, loreY, portraitRect.width - 24, portraitRect.yMax - loreY - 12), GetHeroLore(CurrentPlayerHero), loreStyle);

            float contentX = portraitRect.xMax + 16;
            float contentW = mx + modalW - 18 - contentX;
            float contentY = my + 74;
            DrawSolidRect(new Rect(contentX, contentY, contentW, 62), new Color(0.13f, 0.18f, 0.25f, 0.94f));
            GUI.Box(new Rect(contentX, contentY, contentW, 62), "");
            var passiveTitle = new GUIStyle(_boldLabel) { fontSize = 12 };
            passiveTitle.normal.textColor = new Color(0.65f, 0.9f, 1f);
            GUI.Label(new Rect(contentX + 10, contentY + 5, contentW - 20, 18), "PASSIVE", passiveTitle);
            var passiveBody = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
            GUI.Label(new Rect(contentX + 10, contentY + 22, contentW - 20, 36), GetPassiveDescription(CurrentPlayerHero), passiveBody);

            string[] names = GetSkillNames(CurrentPlayerHero);
            string[] descriptions = GetSkillDescriptions(CurrentPlayerHero);
            string[] targetingHints = GetSkillTargetingHints(CurrentPlayerHero);
            float[] costs = GetHeroManaCosts(CurrentPlayerHero);
            float[] cooldowns = GetHeroMaxCooldowns(CurrentPlayerHero);
            string[] keys = { "Q", "W", "E", "R" };
            float rowY = contentY + 70;
            float rowHeight = (portraitRect.yMax - rowY - 8) / 4f;
            for (int i = 0; i < 4; i++)
            {
                Rect row = new Rect(contentX, rowY + i * rowHeight, contentW, rowHeight - 6);
                DrawSolidRect(row, new Color(0.055f, 0.072f, 0.105f, 0.97f));
                GUI.Box(row, "");
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = GetSkillColor(i);
                GUI.Box(new Rect(row.x + 7, row.y + 8, 38, 38), keys[i]);
                GUI.backgroundColor = old;
                var abilityTitle = new GUIStyle(_boldLabel) { fontSize = 12 };
                abilityTitle.normal.textColor = GetSkillColor(i);
                GUI.Label(new Rect(row.x + 54, row.y + 5, row.width - 64, 20), names[i], abilityTitle);
                GUI.Label(new Rect(row.x + 54, row.y + 24, row.width - 64, 17), $"Mana {costs[i]:F0}  •  Base cooldown {cooldowns[i]:F0}s", _hintLabel);
                var targetHint = new GUIStyle(_hintLabel) { alignment = TextAnchor.MiddleLeft, fontSize = 9 };
                targetHint.normal.textColor = new Color(0.95f, 0.74f, 0.28f);
                GUI.Label(new Rect(row.x + 54, row.y + 41, row.width - 64, 16), targetingHints[i], targetHint);
                var abilityBody = new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true };
                abilityBody.normal.textColor = new Color(0.86f, 0.89f, 0.94f);
                GUI.Label(new Rect(row.x + 54, row.y + 57, row.width - 64, row.height - 61), descriptions[i], abilityBody);
            }

            GUI.Label(new Rect(mx + modalW - 210, my + 44, 180, 20), "Press [F1] to close", _hintLabel);
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

        private static float[] GetHeroManaCosts(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[] { VorkasHero.Skill1ManaCost, VorkasHero.Skill2ManaCost, VorkasHero.Skill3ManaCost, VorkasHero.UltimateManaCost, 0f };
            if (hero is ZenthisHero) return new[] { ZenthisHero.Skill1ManaCost, ZenthisHero.Skill2ManaCost, ZenthisHero.Skill3ManaCost, ZenthisHero.UltimateManaCost, 0f };
            if (hero is KorvaxHero) return new[] { KorvaxHero.Skill1ManaCost, KorvaxHero.Skill2ManaCost, KorvaxHero.Skill3ManaCost, KorvaxHero.UltimateManaCost, 0f };
            if (hero is GravitorHero) return new[] { GravitorHero.Skill1ManaCost, GravitorHero.Skill2ManaCost, GravitorHero.Skill3ManaCost, GravitorHero.UltimateManaCost, 0f };
            return new float[5];
        }

        private string[] GetSkillNames(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[] { "Iron Cleave", "Vanguard", "Seismic", "Rebellion", "Auto Atk" };
            if (hero is ZenthisHero) return new[] { "Hourglass", "Aura", "Temporal", "Rewind", "Auto Atk" };
            if (hero is KorvaxHero) return new[] { "Heavy Bolt", "Focus", "Concussive", "Ballista", "Auto Atk" };
            if (hero is GravitorHero) return new[] { "Mag.Pull", "Repulsion", "Grav.Well", "Collapse", "Auto Atk" };
            return new[] { "Skill 1", "Skill 2", "Skill 3", "Ultimate", "Attack" };
        }

        private static string[] GetSkillTargetingHints(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[]
            {
                "DIRECTION SKILLSHOT — can miss",
                "SELF CAST — no target required",
                "SELF-CENTERED AREA — can miss",
                "GROUND AREA — arena ground required; can miss",
                "ENEMY TARGET — right-click or press A near a target"
            };
            if (hero is ZenthisHero) return new[]
            {
                "GROUND AREA — arena ground required; can miss",
                "SELF CAST — no target required",
                "DIRECTION SKILLSHOT — can miss",
                "SELF CAST — requires a recorded rewind state",
                "ENEMY TARGET — right-click or press A near a target"
            };
            if (hero is KorvaxHero) return new[]
            {
                "DIRECTION SKILLSHOT — can miss",
                "SELF CAST — no target required",
                "ENEMY TARGET REQUIRED — place cursor on target",
                "DIRECTION SKILLSHOT — can miss",
                "ENEMY TARGET — right-click or press A near a target"
            };
            if (hero is GravitorHero) return new[]
            {
                "ENEMY TARGET REQUIRED — place cursor on target",
                "SELF-CENTERED AREA — can miss",
                "GROUND AREA — arena ground required; can miss",
                "GROUND AREA — arena ground required; can miss",
                "ENEMY TARGET — right-click or press A near a target"
            };
            return new[]
            {
                "Targeting unavailable",
                "Targeting unavailable",
                "Targeting unavailable",
                "Targeting unavailable",
                "ENEMY TARGET"
            };
        }

        private static string[] GetSkillDescriptions(HeroBase3D hero)
        {
            if (hero is VorkasHero) return new[]
            {
                "A 6m iron wave that deals 75/125/175/225 physical damage plus 80% Attack Damage.",
                "Gain a 100/160/220/280 shield for 4s and 20% movement speed while the guard holds.",
                "Slam a 3.5m area for 80/130/180/230 physical damage plus 60% Attack Damage; slows by 40% for 2.5s.",
                "Leap up to 7m and crash down for 250/375/500 physical damage plus 120% Attack Damage; knocks up for 1s.",
                "Close-range weapon strike. Repeats automatically while an attack target stays in range."
            };
            if (hero is ZenthisHero) return new[]
            {
                "Place a 3m temporal field up to 8m away; deals 70/115/160/205 magic damage plus 65% Attack Damage and slows by 35%.",
                "Gain 30% attack rate and regenerate 25/40/55/70 health per second for 4s.",
                "Send an 8m temporal rift for 85/135/185/235 magic damage plus 70% Attack Damage; reduces enemy attack rate by 40% for 3s.",
                "Return to the position and health recorded 4s ago, then clear all debuffs.",
                "Ranged chronal attack. Repeats automatically while an attack target stays in range."
            };
            if (hero is KorvaxHero) return new[]
            {
                "Fire a heavy 10m bolt at the first target for 90/145/200/255 physical damage plus 90% Attack Damage.",
                "Focus for 5s, gaining 2.5m attack range and 20/30/40/50 Attack Damage.",
                "Blast a target within 4m for 70/110/150/190 physical damage plus 50% Attack Damage; pushes 3.5m and slows 40% for 2s.",
                "Fire an 18m piercing shot for 300/450/600 physical damage plus 140% Attack Damage; pierces minions and stops on the first hero.",
                "Long-range ballista shot. Repeated hits build Momentum Piercer armor shred."
            };
            if (hero is GravitorHero) return new[]
            {
                "Mark a target within 7m, deal 70/110/150/190 magic damage plus 50% Attack Damage, and pull it toward Gravitor.",
                "Release a 4m repulsion wave for 80/125/170/215 magic damage plus 55% Attack Damage and push enemies 3m away.",
                "Create a 3.5m gravity well up to 6m away. It pulses every 0.5s and slows enemies by 50% for 2.5s.",
                "Create a 4.5m collapse up to 7.5m away; pulls enemies to its core, deals 260/390/520 magic damage plus 100% Attack Damage, and stuns 1.5s.",
                "Heavy melee strike. Repeats automatically while an attack target stays in range."
            };
            return new[] { "No description available.", "No description available.", "No description available.", "No description available.", "Basic attack." };
        }

        private static string GetHeroRole(HeroBase3D hero)
        {
            if (hero is VorkasHero) return "Melee Vanguard • Tank / Bruiser";
            if (hero is ZenthisHero) return "Ranged Chronomancer • Controller";
            if (hero is KorvaxHero) return "Ranged Ballista Hunter • Marksman";
            if (hero is GravitorHero) return "Melee Singularity • Tank / Disruptor";
            return "Unknown Role";
        }

        private static string GetHeroLore(HeroBase3D hero)
        {
            if (hero is VorkasHero) return "Once the shield of a fallen iron citadel, Vorkas carries its last oath into every duel. He turns enemy magic aside and answers with decisive force.";
            if (hero is ZenthisHero) return "Keeper of a fractured hour, Zenthis protects the present by borrowing moments from futures that may never exist.";
            if (hero is KorvaxHero) return "Korvax learned to read distance like a language. Every measured shot strips away another weakness until no armor can hide its bearer.";
            if (hero is GravitorHero) return "Forged around an unstable kore, Gravitor bends weight and momentum to shelter allies, scatter attackers, and collapse the battlefield itself.";
            return "A challenger enters the KOA arena.";
        }

        private static string GetPassiveDescription(HeroBase3D hero)
        {
            if (hero is VorkasHero) return "ANTI-ENERGY AURA — Reduces incoming magic damage by 15% and suppresses nearby hostile spell power.";
            if (hero is ZenthisHero) return "CHRONO STASIS — Below 20% health, enter 1.5s invulnerability and restore 15% health. Internal cooldown: 45s.";
            if (hero is KorvaxHero) return "MOMENTUM PIERCER — Repeated attacks on one target apply 3% armor shred per stack, up to 5 stacks.";
            if (hero is GravitorHero) return "ANTIGRAVITY SHIELD — Taking damage creates a shield for 80 + 8% maximum health for 3s. Internal cooldown: 12s.";
            return "No passive description available.";
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
            GUI.Label(new Rect(rx + 14 + halfW, curY + 66, halfW, 16), $"◆ Attr Pts: {hero.AvailableAttributePoints} (V{hero.VitalityPoints}/F{hero.FocusPoints}/A{hero.ArmorPoints}/R{hero.ResolvePoints})", statStyle);

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
            int panelHeight = _isControlPanelExpanded ? 334 : 32;
            DrawPanelFrame(new Rect(px, py, 230, panelHeight), "⚙ Hero & Bot Controls");
            if (GUI.Button(new Rect(px + 198, py + 4, 26, 22), _isControlPanelExpanded ? "−" : "+"))
            {
                _isControlPanelExpanded = !_isControlPanelExpanded;
                PlayUiSound(_isControlPanelExpanded ? "open_002" : "close_002", 0.5f);
            }

            if (!_isControlPanelExpanded) return;

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

            KOA.Presentation.Camera.TopDownCameraController cameraController = GetCameraController();
            bool cameraLocked = cameraController != null && cameraController.IsPermanentlyLocked;
            Color oldCameraColor = GUI.backgroundColor;
            GUI.backgroundColor = cameraLocked ? new Color(0.25f, 0.78f, 0.52f) : new Color(0.28f, 0.58f, 0.9f);
            string cameraText = cameraLocked ? "CAMERA: LOCKED [Y]" : "CAMERA: FREE [Y]";
            if (GUI.Button(new Rect(px + 10, py + 266, 210, 25), cameraText) && cameraController != null)
            {
                cameraController.TogglePersistentLock();
                PlayUiSound("toggle_001", 0.5f);
            }
            GUI.backgroundColor = oldCameraColor;
            GUI.Label(new Rect(px + 8, py + 293, 214, 24), "FREE: edge pan  |  LOCKED: follow hero", _hintLabel);
        }

        // ══════════════════════════════════════════════════════════
        //  ABILITY FEEDBACK
        // ══════════════════════════════════════════════════════════
        private void DrawAbilityFeedback()
        {
            float remaining = _abilityFeedbackExpiresAt - Time.unscaledTime;
            if (remaining <= 0f || string.IsNullOrEmpty(_abilityFeedbackMessage)) return;

            float fade = Mathf.Clamp01(remaining / 0.25f);
            Rect panel = GetTopCenterNotificationRect(360f, 27f, 56f);

            DrawSolidRect(panel, new Color(0.04f, 0.055f, 0.07f, 0.88f * fade));
            DrawSolidRect(new Rect(panel.x, panel.yMax - 2f, panel.width, 2f), new Color(0.95f, 0.66f, 0.12f, 0.95f * fade));

            _abilityFeedbackStyle.normal.textColor = new Color(1f, 0.83f, 0.38f, fade);
            GUI.Label(panel, _abilityFeedbackMessage, _abilityFeedbackStyle);
        }

        // ══════════════════════════════════════════════════════════
        //  KILL FEED
        // ══════════════════════════════════════════════════════════
        private void DrawKillFeed()
        {
            float sy = Time.unscaledTime < _abilityFeedbackExpiresAt ? 87f : 56f;
            Rect firstRow = GetTopCenterNotificationRect(340f, 24f, sy);
            for (int i = 0; i < _killFeed.Count; i++)
            {
                Rect row = new Rect(firstRow.x, sy + (i * 26), firstRow.width, firstRow.height);
                DrawSolidRect(row, new Color(0.025f, 0.035f, 0.045f, 0.9f));
                DrawSolidRect(new Rect(row.x, row.y, 3f, row.height), new Color(1f, 0.7f, 0.12f, 0.95f));
                GUI.Label(new Rect(row.x + 7f, row.y, row.width - 12f, row.height), _killFeed[i], _centerLabel);
            }
        }

        private Rect GetTopCenterNotificationRect(float preferredWidth, float height, float y)
        {
            float safeLeft = GetMiniMapPanelRect().xMax + 10f;
            float safeRight = Screen.width - 250f;
            float safeWidth = safeRight - safeLeft;

            if (safeWidth >= 220f)
            {
                float width = Mathf.Min(preferredWidth, safeWidth);
                return new Rect(safeLeft + (safeWidth - width) * 0.5f, y, width, height);
            }

            float fallbackWidth = Mathf.Min(preferredWidth, Mathf.Max(1f, Screen.width - 24f));
            return new Rect((Screen.width - fallbackWidth) * 0.5f, y, fallbackWidth, height);
        }

        // ══════════════════════════════════════════════════════════
        //  SHOP CATALOG
        // ══════════════════════════════════════════════════════════
        private void DrawShopCatalog()
        {
            int w = Mathf.Min(760, Screen.width - 36);
            int h = Mathf.Min(500, Screen.height - 54);
            int x = (Screen.width - w) / 2, y = (Screen.height - h) / 2;
            DrawSolidRect(new Rect(x - 6, y - 6, w + 12, h + 12), new Color(0.01f, 0.025f, 0.035f, 0.97f));
            DrawPanelFrame(new Rect(x, y, w, h), "KOA DUEL SHOP — Section 5.2");

            int currentGold = PlayerWallet != null ? PlayerWallet.CurrentGold : 0;
            GUI.Label(new Rect(x + w - 220, y + 5, 130, 22), $"Gold: {currentGold}g", _boldLabel);
            if (GUI.Button(new Rect(x + w - 80, y + 4, 70, 24), "Close"))
            {
                _isShopOpen = false;
                PlayUiSound("close_002", 0.5f);
            }

            const int railWidth = 150;
            Rect rail = new Rect(x + 12, y + 36, railWidth, h - 50);
            DrawSolidRect(rail, new Color(0.035f, 0.065f, 0.075f, 0.96f));
            GUI.Box(rail, "CATEGORIES");
            string[] categoryLabels = { "All Items", "Consumables", "Attributes", "Equipment", "Misc", "Upgraded" };
            for (int i = 0; i < categoryLabels.Length; i++)
            {
                Color old = GUI.backgroundColor;
                if (_selectedShopCategory == i) GUI.backgroundColor = new Color(0.22f, 0.72f, 0.62f);
                if (GUI.Button(new Rect(rail.x + 8, rail.y + 30 + i * 38, rail.width - 16, 31), categoryLabels[i]))
                {
                    _selectedShopCategory = i;
                    PlayUiSound("click_003", 0.38f);
                }
                GUI.backgroundColor = old;
            }
            GUI.Label(new Rect(rail.x + 8, rail.yMax - 72, rail.width - 16, 62),
                "Future mode-only inventory is specified separately and is not sold in this 1v1 base shop.",
                new GUIStyle(_hintLabel) { wordWrap = true, alignment = TextAnchor.UpperLeft });

            List<ItemData> filtered = new List<ItemData>();
            List<ItemData> catalog = ShopSystem.AvailableCatalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                if (_selectedShopCategory == 0 || (int)catalog[i].Category == _selectedShopCategory - 1)
                    filtered.Add(catalog[i]);
            }

            float listX = rail.xMax + 12;
            float listW = x + w - 12 - listX;
            Rect listRect = new Rect(listX, y + 36, listW, h - 50);
            GUI.Box(listRect, filtered.Count > 0 ? categoryLabels[_selectedShopCategory] : "No 1v1 items in this category");
            if (filtered.Count == 0)
            {
                GUI.Label(new Rect(listRect.x + 15, listRect.y + 65, listRect.width - 30, 80),
                    "This category is reserved for a later approved mode. The current 1v1 catalog remains balance-safe.",
                    new GUIStyle(_centerLabel) { wordWrap = true });
                return;
            }

            int rowHeight = Mathf.Clamp((int)((listRect.height - 42) / filtered.Count), 46, 72);
            for (int i = 0; i < filtered.Count; i++)
            {
                ItemData item = filtered[i];
                float iy = listRect.y + 28 + i * rowHeight;
                Rect itemRect = new Rect(listRect.x + 8, iy, listRect.width - 16, rowHeight - 5);
                DrawSolidRect(itemRect, new Color(0.07f, 0.085f, 0.105f, 0.96f));
                GUI.Box(itemRect, "");
                GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 5, itemRect.width - 190, 20), $"<b>{item.DisplayName}</b>");
                GUI.Label(new Rect(itemRect.x + 10, itemRect.y + 24, itemRect.width - 190, itemRect.height - 26), item.Description,
                    new GUIStyle(GUI.skin.label) { fontSize = 10, wordWrap = true });
                GUI.Label(new Rect(itemRect.xMax - 165, itemRect.y + 12, 70, 24), $"{item.Cost}g", _boldLabel);
                bool canAfford = PlayerWallet != null && PlayerWallet.CurrentGold >= item.Cost;
                GUI.enabled = canAfford;
                if (GUI.Button(new Rect(itemRect.xMax - 88, itemRect.y + 8, 76, 30), canAfford ? "BUY" : "NO GOLD")
                    && CurrentPlayerHero != null && PlayerWallet != null)
                {
                    bool purchased = ShopSystem.TryBuyItem(item, PlayerWallet, CurrentPlayerHero.Inventory);
                    PlayUiSound(purchased ? "confirmation_002" : "error_004", 0.55f);
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

        private void PlayUiSound(string resourceName, float volume)
        {
            if (_uiAudioSource == null || string.IsNullOrEmpty(resourceName)) return;
            AudioClip clip = Resources.Load<AudioClip>($"KOA/Audio/{resourceName}");
            if (clip != null) _uiAudioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
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
