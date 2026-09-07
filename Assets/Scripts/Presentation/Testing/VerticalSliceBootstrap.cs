using KOA.Core.AI;
using KOA.Core.Entities;
using KOA.Core.Match;
using KOA.Core.Minions;
using KOA.Core.Structures;
using KOA.Presentation.Camera;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using KOA.Presentation.Views;
using KOA.Data.Enums;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Presentation.Testing
{
    /// <summary>
    /// Auto Bootstrapper สำหรับทดสอบ Phase 3 เต็มรูปแบบ (Full Match Setup)
    /// สร้างแผนที่ 1v1 เลนเดียว 70x20m, ป้อมปราการ 2 ฝั่ง, Player Hero, AI Bot (Easy/Med/Hard), Shop, Minion Waves, และ MatchHUD
    /// พร้อม RuntimeInitializeOnLoadMethod เริ่มทำงานอัตโนมัติ 100% เมื่อกด Play ใน Unity โดยไม่ต้องลากวางเอง
    /// </summary>
    public class VerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Bootstrap Options")]
        [SerializeField] private bool autoBuildOnStart = true;
        [SerializeField] private BotDifficulty initialBotDifficulty = BotDifficulty.Medium;

        public static VerticalSliceBootstrap Instance { get; private set; }

        public MatchSimulation MatchSimulation { get; private set; }
        public ModularBotBrain BotBrain { get; private set; }
        public HeroBase3D CurrentPlayerHero { get; private set; }
        public HeroBase3D BotHero { get; private set; }

        private GameObject _playerHeroGo;
        private HeroView _playerHeroView;
        private MatchHUD _matchHud;
        private bool _isBuilt = false;

        // Respawn System (Fountain Zone: Section 3.1)
        private float _playerRespawnTimer = -1f;
        private float _botRespawnTimer = -1f;
        private Vector3 _blueFountain = new Vector3(0, 0, -60f);
        private Vector3 _redFountain = new Vector3(0, 0, 60f);
        private GameObject _botGo;
        private GameObject _botGoRef;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeOnPlay()
        {
            if (FindAnyObjectByType<VerticalSliceBootstrap>() == null)
            {
                var runner = new GameObject("[KOA_MatchRunner]");
                runner.AddComponent<VerticalSliceBootstrap>();
                Debug.Log("<color=green>[KOA] Auto-started Duel Arena on Play Mode!</color>");
            }
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("KOA/Setup Match in Current Scene")]
        public static void SetupMatchInEditorMenu()
        {
            var existing = FindAnyObjectByType<VerticalSliceBootstrap>();
            if (existing == null)
            {
                var runner = new GameObject("[KOA_MatchRunner]");
                runner.AddComponent<VerticalSliceBootstrap>();
                UnityEditor.Undo.RegisterCreatedObjectUndo(runner, "Create KOA MatchRunner");
                Debug.Log("<color=green>[KOA] Created [KOA_MatchRunner] in scene!</color>");
            }
            else
            {
                Debug.Log("[KOA] [KOA_MatchRunner] already exists in scene.");
            }
        }
#endif

        private void Awake()
        {
            Instance = this;
            if (autoBuildOnStart && !_isBuilt)
            {
                BuildFullArena();
            }
        }

        private void Start()
        {
            if (autoBuildOnStart && !_isBuilt)
            {
                BuildFullArena();
            }
        }

        private void OnEnable()
        {
            if (autoBuildOnStart && !_isBuilt)
            {
                BuildFullArena();
            }
        }

        [ContextMenu("Build Arena Now")]
        public void BuildArenaContext()
        {
            BuildFullArena();
        }

        private void OnGUI()
        {
            // หากระบบยังไม่ได้ถูกสร้าง (เช่น กรณีสร้าง GameObject เปล่าขึ้นมาใน Editor)
            if (!_isBuilt)
            {
                int btnWidth = 320;
                int btnHeight = 60;
                int x = (Screen.width - btnWidth) / 2;
                int y = (Screen.height - btnHeight) / 2;

                GUI.Box(new Rect(x - 20, y - 40, btnWidth + 40, btnHeight + 70), "KOA Duel Arena Setup");
                if (GUI.Button(new Rect(x, y, btnWidth, btnHeight), "⚔️ กดปุ่มนี้เพื่อเริ่มสร้างสนาม (Build Arena)"))
                {
                    BuildFullArena();
                }
            }
        }

        private void Update()
        {
            if (MatchSimulation != null)
            {
                MatchSimulation.SimulationTick(Time.deltaTime);
            }
            if (BotBrain != null)
            {
                BotBrain.SimulationTick(Time.deltaTime);
            }

            // Player Respawn Countdown
            if (_playerRespawnTimer > 0f)
            {
                _playerRespawnTimer -= Time.deltaTime;
                if (_matchHud != null)
                    _matchHud.RespawnTimeRemaining = _playerRespawnTimer;

                if (_playerRespawnTimer <= 0f)
                {
                    _playerRespawnTimer = -1f;
                    if (CurrentPlayerHero != null)
                    {
                        CurrentPlayerHero.Respawn(_blueFountain + Vector3.up);
                        if (_matchHud != null)
                        {
                            _matchHud.IsPlayerDead = false;
                            _matchHud.RespawnTimeRemaining = 0f;
                        }
                    }
                }
            }

            // Bot Respawn Countdown
            if (_botRespawnTimer > 0f)
            {
                _botRespawnTimer -= Time.deltaTime;
                if (_botRespawnTimer <= 0f)
                {
                    _botRespawnTimer = -1f;
                    if (BotHero != null)
                    {
                        BotHero.Respawn(_redFountain + Vector3.up);
                        if (_botGoRef != null)
                            _botGoRef.transform.position = _redFountain + Vector3.up;
                    }
                }
            }
        }

        public void BuildFullArena()
        {
            if (_isBuilt) return;
            _isBuilt = true;

            Debug.Log("[Bootstrap] Initializing KOA Full Match Arena (Phase 3)...");

            // 1. Directional Light
            if (FindAnyObjectByType<Light>() == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                Light dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // 2. Ground Plane 130x26m (Section 3.1)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DuelArena_130x26m";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2.6f, 1.0f, 13.0f); // 26m x 130m
            ground.GetComponent<Renderer>().material.color = new Color(0.20f, 0.23f, 0.22f);

            // 2.1 Fountain Visual Platforms (ลานวงกลมบ่อน้ำฮีลลิ่ง รัศมี 7.5m)
            CreateFountainVisual(_blueFountain, "BlueFountain_Zone", new Color(0.15f, 0.5f, 0.95f, 0.4f), 7.5f);
            CreateFountainVisual(_redFountain, "RedFountain_Zone", new Color(0.95f, 0.25f, 0.2f, 0.4f), 7.5f);

            // 2.2 พุ่มไม้ 2 จุดกลางเลน (Section 3.1 Bush Zones)
            CreateBushVisual(new Vector3(-8f, 0.4f, 0f), new Vector3(3.2f, 0.8f, 9.0f), "Bush_West");
            CreateBushVisual(new Vector3(8f, 0.4f, 0f), new Vector3(3.2f, 0.8f, 9.0f), "Bush_East");

            // 3. DamagePopupManager
            if (FindAnyObjectByType<DamagePopupManager>() == null)
            {
                new GameObject("DamagePopupManager").AddComponent<DamagePopupManager>();
            }

            // 4. Match Simulation (Blue Fountain: Z = -60, Red Fountain: Z = +60)
            MatchSimulation = new MatchSimulation(_blueFountain, _redFountain);

            // Hook Minion wave visuals
            MatchSimulation.BlueSpawner.OnWaveSpawned += HandleWaveVisuals;
            MatchSimulation.RedSpawner.OnWaveSpawned += HandleWaveVisuals;

            // 5. สร้างป้อมปราการใน Scene ทั้ง 2 ฝั่ง (Section 3.3)
            SpawnTowerView(MatchSimulation.BlueOuterTower, "BlueOuterTower", Color.cyan);
            SpawnTowerView(MatchSimulation.BlueInnerTower, "BlueInnerTower", Color.blue);
            SpawnTowerView(MatchSimulation.BlueNexus, "BlueNexus", Color.magenta);

            SpawnTowerView(MatchSimulation.RedOuterTower, "RedOuterTower", new Color(1f, 0.4f, 0f));
            SpawnTowerView(MatchSimulation.RedInnerTower, "RedInnerTower", Color.red);
            SpawnTowerView(MatchSimulation.RedNexus, "RedNexus", new Color(0.7f, 0f, 0f));

            // 6. สร้าง Player Hero (เริ่มด้วย Vorkas เกิดที่บ่อ Blue)
            SpawnPlayerHero("Vorkas");

            // 7. สร้าง Bot Hero (Red Team เกิดที่บ่อ Red) ควบคุมโดย ModularBotBrain (Section 9)
            SpawnBotHero("Vorkas");

            // 8. สร้าง MatchHUD
            GameObject hudGo = new GameObject("MatchHUD");
            _matchHud = hudGo.AddComponent<MatchHUD>();
            _matchHud.BindMatch(MatchSimulation, CurrentPlayerHero, MatchSimulation.BlueWallet, BotBrain);
            _matchHud.OnPlayAgainRequested += HandlePlayAgain;
            _matchHud.OnHeroSwitched += SwitchHero;
            _matchHud.OnBotHeroSwitched += SwitchBotHero;

            // 9. กล้อง Top-Down Isometric 50 องศา (Section 7.1)
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<UnityEngine.Camera>();
                camGo.tag = "MainCamera";
            }

            TopDownCameraController camCtrl = mainCam.GetComponent<TopDownCameraController>();
            if (camCtrl == null)
            {
                camCtrl = mainCam.gameObject.AddComponent<TopDownCameraController>();
            }
            camCtrl.SetTarget(_playerHeroGo.transform);

            Debug.Log("[Bootstrap] Phase 3 Ready! Press [P] to toggle Shop, 1-6 for Items, Right-click to Move.");
        }

        private void HandleWaveVisuals(List<MinionEntity> wave)
        {
            foreach (var minion in wave)
            {
                SpawnMinionView(minion);
            }
        }

        private void SpawnMinionView(MinionEntity minion)
        {
            PrimitiveType pType = minion.Type == MinionType.Cannon ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject minionGo = GameObject.CreatePrimitive(pType);
            minionGo.name = $"{minion.MinionId}_{minion.Type}";
            minionGo.transform.position = minion.Position + Vector3.up * 0.4f;

            Vector3 scale = minion.Type switch
            {
                MinionType.Super => new Vector3(1.5f, 1.5f, 1.5f),
                MinionType.Cannon => new Vector3(1.1f, 0.9f, 1.1f),
                MinionType.Melee => new Vector3(0.85f, 0.85f, 0.85f),
                _ => new Vector3(0.65f, 0.65f, 0.65f) // Ranged
            };
            minionGo.transform.localScale = scale;

            Color barColor = minion.TeamId == 0 ? Color.cyan : Color.red;
            var hb = CreateHealthBar(minionGo.transform, $"{minion.MinionId}_HealthBar", barColor);

            MinionView view = minionGo.AddComponent<MinionView>();
            view.BindLogic(minion);
            view.SetHealthBar(hb);

            minion.OnAttackExecuted += (targetPos) => DamagePopupManager.Instance?.ShowDamage(targetPos, minion.AttackDamage, DamageType.Physical);
            minion.OnDamageTaken += (dmg, type) => DamagePopupManager.Instance?.ShowDamage(minionGo.transform.position, dmg, type);
        }

        private void SpawnPlayerHero(string heroName)
        {
            if (_playerHeroGo != null)
            {
                Destroy(_playerHeroGo);
            }

            _playerHeroGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _playerHeroGo.name = $"Hero_{heroName}";
            _playerHeroGo.transform.position = _blueFountain + new Vector3(0, 1.0f, 0);

            Color heroColor = heroName switch
            {
                "Zenthis" => new Color(0.8f, 0.8f, 0.2f),
                "Korvax" => new Color(0.1f, 0.8f, 0.5f),
                "Gravitor" => new Color(0.6f, 0.2f, 0.8f),
                _ => new Color(0.2f, 0.5f, 0.9f) // Vorkas
            };
            _playerHeroGo.GetComponent<Renderer>().material.color = heroColor;

            CurrentPlayerHero = heroName switch
            {
                "Zenthis" => new ZenthisHero(_playerHeroGo.transform.position),
                "Korvax" => new KorvaxHero(_playerHeroGo.transform.position),
                "Gravitor" => new GravitorHero(_playerHeroGo.transform.position),
                _ => new VorkasHero(_playerHeroGo.transform.position)
            };
            CurrentPlayerHero.TeamId = 0;
            if (MatchSimulation != null) MatchSimulation.BlueHero = CurrentPlayerHero;

            CurrentPlayerHero.OnDamageTaken += (dmg, type) => DamagePopupManager.Instance?.ShowDamage(_playerHeroGo.transform.position, dmg, type);
            CurrentPlayerHero.OnDied += () =>
            {
                if (CurrentPlayerHero != null)
                {
                    float respawnTime = CurrentPlayerHero.CalculateRespawnTime();
                    _playerRespawnTimer = respawnTime;
                    // Section 4.1: Kill Gold = 200g base + Streak Bonus ให้ Red Wallet
                    MatchSimulation?.RedWallet?.RecordHeroKill();
                    // Section 4.2: EXP ให้แก่ Bot Hero
                    BotHero?.AddExp(350f);
                    // Reset Player streak เมื่อตาย
                    MatchSimulation?.BlueWallet?.RecordDeath();
                    Debug.Log($"[Bootstrap] Player died. Respawning in {respawnTime:F1}s");
                }
            };

            WorldSpaceHealthBar playerHb = CreateHealthBar(_playerHeroGo.transform, "PlayerHealthBar", Color.green);

            _playerHeroGo.AddComponent<PCInputAdapter>();
            _playerHeroView = _playerHeroGo.AddComponent<HeroView>();
            _playerHeroView.BindHero(CurrentPlayerHero);
            _playerHeroView.SetHealthBar(playerHb);

            // อัปเดตกล้องให้จับตามฮีโร่ใหม่
            var cam = UnityEngine.Camera.main?.GetComponent<TopDownCameraController>();
            if (cam != null) cam.SetTarget(_playerHeroGo.transform);

            if (_matchHud != null)
            {
                _matchHud.CurrentPlayerHero = CurrentPlayerHero;
            }
            if (BotBrain != null)
            {
                BotBrain.TargetEnemyHero = CurrentPlayerHero;
            }
        }

        private void SwitchHero(string heroName)
        {
            Debug.Log($"[Bootstrap] Switching Player Hero to: {heroName}");
            SpawnPlayerHero(heroName);
        }

        private void SpawnBotHero(string heroName)
        {
            if (_botGoRef != null)
            {
                Destroy(_botGoRef);
            }

            _botGoRef = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _botGoRef.name = $"Bot_{heroName}";
            _botGoRef.transform.position = _redFountain + new Vector3(0, 1.0f, 0);

            Color botColor = heroName switch
            {
                "Zenthis" => new Color(0.9f, 0.7f, 0.2f),
                "Korvax" => new Color(0.2f, 0.8f, 0.6f),
                "Gravitor" => new Color(0.7f, 0.2f, 0.9f),
                _ => new Color(0.9f, 0.2f, 0.2f) // Vorkas
            };
            _botGoRef.GetComponent<Renderer>().material.color = botColor;

            BotHero = heroName switch
            {
                "Zenthis" => new ZenthisHero(_botGoRef.transform.position),
                "Korvax" => new KorvaxHero(_botGoRef.transform.position),
                "Gravitor" => new GravitorHero(_botGoRef.transform.position),
                _ => new VorkasHero(_botGoRef.transform.position)
            };
            BotHero.TeamId = 1;

            WorldSpaceHealthBar botHb = CreateHealthBar(_botGoRef.transform, "BotHealthBar", Color.red);

            HeroView botHeroView = _botGoRef.AddComponent<HeroView>();
            botHeroView.BindHero(BotHero);
            botHeroView.SetHealthBar(botHb);

            BotHero.OnDamageTaken += (dmg, type) => DamagePopupManager.Instance?.ShowDamage(_botGoRef.transform.position, dmg, type);
            BotHero.OnDied += () =>
            {
                if (BotHero != null)
                {
                    float respawnTime = BotHero.CalculateRespawnTime();
                    _botRespawnTimer = respawnTime;
                    // Section 4.1: Kill Gold = 200g base + Streak Bonus (RecordHeroKill handles this)
                    MatchSimulation?.BlueWallet?.RecordHeroKill();
                    // Section 4.2: EXP เทียบเท่าครีป ~10 ตัว (~350 EXP)
                    CurrentPlayerHero?.AddExp(350f);
                    // รีเซ็ต Bot streak เมื่อตาย
                    MatchSimulation?.RedWallet?.RecordDeath();
                    Debug.Log($"[Bootstrap] Bot died. Respawning in {respawnTime:F1}s");
                }
            };

            if (MatchSimulation != null)
            {
                MatchSimulation.RedHero = BotHero;
            }

            BotDifficulty currentDiff = BotBrain != null ? BotBrain.Difficulty : initialBotDifficulty;
            BotBrain = new ModularBotBrain(BotHero, _redFountain, _blueFountain, MatchSimulation, currentDiff);
            BotBrain.TargetEnemyHero = CurrentPlayerHero;

            if (_matchHud != null)
            {
                _matchHud.BotBrain = BotBrain;
            }
        }

        private void SwitchBotHero(string heroName)
        {
            Debug.Log($"[Bootstrap] Switching Bot Hero to: {heroName}");
            SpawnBotHero(heroName);
        }

        private void HandlePlayAgain()
        {
            Debug.Log("[Bootstrap] Play Again requested — reloading scene.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
#endif
        }

        private void SpawnTowerView(TowerEntity tower, string name, Color color)
        {
            GameObject towerGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerGo.name = name;
            towerGo.transform.position = tower.Position + Vector3.up * 2.0f;
            towerGo.transform.localScale = new Vector3(2.5f, 2.0f, 2.5f);
            towerGo.GetComponent<Renderer>().material.color = color;

            LineRenderer lineRenderer = towerGo.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.enabled = false;

            WorldSpaceHealthBar towerHb = CreateHealthBar(towerGo.transform, $"{name}_HealthBar", color);

            TowerView view = towerGo.AddComponent<TowerView>();
            view.BindLogic(tower);
            view.SetHealthBar(towerHb);

            tower.OnAttackFired += (targetPos, dmg, isHeated) => DamagePopupManager.Instance?.ShowDamage(targetPos, dmg, DamageType.Physical);
            tower.OnDamageTaken += (dmg, type) => DamagePopupManager.Instance?.ShowDamage(towerGo.transform.position, dmg, type);
        }

        private WorldSpaceHealthBar CreateHealthBar(Transform parentTarget, string barName, Color color)
        {
            GameObject barRoot = new GameObject(barName);
            WorldSpaceHealthBar wsHealthBar = barRoot.AddComponent<WorldSpaceHealthBar>();
            wsHealthBar.BindTarget(parentTarget);

            // Background
            GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgQuad.transform.SetParent(barRoot.transform);
            bgQuad.transform.localPosition = Vector3.zero;
            bgQuad.transform.localScale = new Vector3(1.4f, 0.2f, 1f);
            Destroy(bgQuad.GetComponent<Collider>());
            bgQuad.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
            bgQuad.GetComponent<Renderer>().material.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill
            GameObject fillQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillQuad.transform.SetParent(barRoot.transform);
            fillQuad.transform.localPosition = new Vector3(0, 0, -0.01f);
            fillQuad.transform.localScale = new Vector3(1.36f, 0.16f, 1f);
            Destroy(fillQuad.GetComponent<Collider>());
            fillQuad.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
            fillQuad.GetComponent<Renderer>().material.color = color;

            wsHealthBar.SetupScaleBar(fillQuad.transform);

            return wsHealthBar;
        }

        private void CreateFountainVisual(Vector3 center, string name, Color color, float radius)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.position = new Vector3(center.x, 0.02f, center.z);
            pad.transform.localScale = new Vector3(radius * 2f, 0.03f, radius * 2f);
            Destroy(pad.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            pad.GetComponent<Renderer>().material = mat;
        }

        private void CreateBushVisual(Vector3 center, Vector3 size, string name)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bush.name = name;
            bush.transform.position = center;
            bush.transform.localScale = size;
            Destroy(bush.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0.12f, 0.42f, 0.16f, 0.85f);
            bush.GetComponent<Renderer>().material = mat;
        }
    }
}
