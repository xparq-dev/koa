using KOA.Core.AI;
using KOA.Core.Entities;
using KOA.Core.Match;
using KOA.Core.Minions;
using KOA.Core.Structures;
using KOA.Presentation.Camera;
using KOA.Presentation.Arena;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using KOA.Presentation.Views;
using System.Collections.Generic;
using UnityEngine;

namespace KOA.Presentation.Testing
{
    /// <summary>
    /// Auto Bootstrapper สำหรับทดสอบ Phase 3 เต็มรูปแบบ (Full Match Setup)
    /// สร้างแผนที่ 1v1 เลนเดียว 130x26m, ป้อมปราการ 2 ฝั่ง, Player Hero, AI Bot (Easy/Med/Hard), Shop, Minion Waves, และ MatchHUD
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

            // 2. Presentation-only dark-fantasy bridge arena (ขนาดเล่นจริงยังคง 130x26m ตาม Section 3.1)
            GameObject arenaArt = new GameObject("BridgeArenaArtDirector");
            arenaArt.transform.SetParent(transform, false);
            arenaArt.AddComponent<BridgeArenaArtDirector>().BuildArena();

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
            SpawnTowerView(MatchSimulation.BlueNexus, "BlueNexus", new Color(0.18f, 0.62f, 1f));

            SpawnTowerView(MatchSimulation.RedOuterTower, "RedOuterTower", new Color(1f, 0.4f, 0f));
            SpawnTowerView(MatchSimulation.RedInnerTower, "RedInnerTower", Color.red);
            SpawnTowerView(MatchSimulation.RedNexus, "RedNexus", new Color(1f, 0.24f, 0.12f));

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
            camCtrl.UseDefaultPresentationHeading();
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
            Vector3 scale = minion.Type switch
            {
                MinionType.Super => new Vector3(1.5f, 1.5f, 1.5f),
                MinionType.Cannon => new Vector3(1.1f, 0.9f, 1.1f),
                MinionType.Melee => new Vector3(0.85f, 0.85f, 0.85f),
                _ => new Vector3(0.65f, 0.65f, 0.65f) // Ranged
            };

            Vector3 spawnPosition = minion.Position + Vector3.up * 0.4f;
            GameObject minionGo = CreateDemoObject(
                $"Minion_{minion.Type}",
                $"{minion.MinionId}_{minion.Type}",
                pType,
                spawnPosition,
                scale,
                0.4f);

            Color barColor = minion.TeamId == 0 ? Color.cyan : Color.red;
            CreateTeamMarker(minionGo.transform, barColor, 0.65f, 0.4f);
            var hb = CreateHealthBar(minionGo.transform, $"{minion.MinionId}_HealthBar", barColor);

            MinionView view = minionGo.AddComponent<MinionView>();
            view.BindLogic(minion);
            view.SetHealthBar(hb);

            minion.OnDamageTakenWithSource += (dmg, type, attackerId) =>
            {
                // Section 8: lane minion trades remain visually quiet. Hero and structure
                // damage still displays the resolved amount after target mitigation.
                if (!MinionEntity.IsMinionId(attackerId))
                    DamagePopupManager.Instance?.ShowDamage(minionGo.transform.position, dmg, type);
            };
        }

        private void SpawnPlayerHero(string heroName)
        {
            if (_playerHeroGo != null)
            {
                Destroy(_playerHeroGo);
            }

            Color heroColor = heroName switch
            {
                "Zenthis" => new Color(0.8f, 0.8f, 0.2f),
                "Korvax" => new Color(0.1f, 0.8f, 0.5f),
                "Gravitor" => new Color(0.6f, 0.2f, 0.8f),
                _ => new Color(0.2f, 0.5f, 0.9f) // Vorkas
            };
            _playerHeroGo = CreateDemoObject(
                $"Hero_{heroName}",
                $"Hero_{heroName}",
                PrimitiveType.Capsule,
                _blueFountain + Vector3.up,
                Vector3.one,
                1f);
            CreateTeamMarker(_playerHeroGo.transform, heroColor, 1.05f, 1f);

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
            _playerHeroGo.AddComponent<HeroAbilityVfxView>().Bind(CurrentPlayerHero, heroColor);
            _playerHeroGo.AddComponent<HeroEquipmentView>().Bind(CurrentPlayerHero);

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

            Color botColor = heroName switch
            {
                "Zenthis" => new Color(0.9f, 0.7f, 0.2f),
                "Korvax" => new Color(0.2f, 0.8f, 0.6f),
                "Gravitor" => new Color(0.7f, 0.2f, 0.9f),
                _ => new Color(0.9f, 0.2f, 0.2f) // Vorkas
            };
            _botGoRef = CreateDemoObject(
                $"Hero_{heroName}",
                $"Bot_{heroName}",
                PrimitiveType.Capsule,
                _redFountain + Vector3.up,
                Vector3.one,
                1f);
            CreateTeamMarker(_botGoRef.transform, botColor, 1.05f, 1f);

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
            _botGoRef.AddComponent<HeroAbilityVfxView>().Bind(BotHero, botColor);
            _botGoRef.AddComponent<HeroEquipmentView>().Bind(BotHero);

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
            string resourceName = name.Contains("Outer") ? "Tower_Outer" : name.Contains("Inner") ? "Tower_Inner" : "Nexus";
            bool hasAuthoredStructureModel = Resources.Load<GameObject>($"KOA/Demo/{resourceName}") != null;
            float padRadius = resourceName == "Nexus" ? 2.85f : resourceName == "Tower_Inner" ? 2.25f : 1.8f;
            float markerRadius = resourceName == "Nexus" ? 3.0f : resourceName == "Tower_Inner" ? 2.4f : 1.95f;
            float beaconHeight = resourceName == "Nexus" ? 5.25f : resourceName == "Tower_Inner" ? 4.3f : 3.7f;
            float healthBarOffset = resourceName == "Nexus" ? 5.75f : resourceName == "Tower_Inner" ? 4.75f : 4.15f;
            CreateStructurePad(tower.Position, $"{name}_Base", color, padRadius, resourceName == "Nexus" ? 0.18f : 0.12f);
            GameObject towerGo = CreateDemoObject(
                resourceName,
                name,
                PrimitiveType.Cylinder,
                tower.Position,
                new Vector3(2.5f, 2f, 2.5f),
                0f);
            // The selected structure meshes have a dark open rear face; orient the authored facade
            // toward the gameplay camera so both teams remain readable from the standard approach.
            towerGo.transform.rotation = Quaternion.Euler(0f, 215f, 0f);
            ImproveStructureReadability(towerGo);
            CreateTeamMarker(towerGo.transform, color, markerRadius, 0f);
            // Primitive geometry is a recovery fallback only. Drawing it over the authored FBX
            // hides the actual tower and makes every structure look like a block.
            if (!hasAuthoredStructureModel)
                CreateStructureSilhouette(towerGo.transform, name, color, resourceName);
            CreateStructureBeacon(towerGo.transform, name, color, beaconHeight, resourceName == "Nexus");

            LineRenderer lineRenderer = towerGo.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.2f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.enabled = false;

            WorldSpaceHealthBar towerHb = CreateHealthBar(towerGo.transform, $"{name}_HealthBar", color);
            towerHb.SetOffset(new Vector3(0f, healthBarOffset, 0f));

            TowerView view = towerGo.AddComponent<TowerView>();
            view.SetLaserRenderer(lineRenderer);
            view.BindLogic(tower);
            view.SetHealthBar(towerHb);

            tower.OnDamageTaken += (dmg, type) => DamagePopupManager.Instance?.ShowDamage(towerGo.transform.position, dmg, type);
        }

        private static void ImproveStructureReadability(GameObject structure)
        {
            Shader readableShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            foreach (Renderer renderer in structure.GetComponentsInChildren<Renderer>(true))
            {
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                Material sourceMaterial = renderer.sharedMaterial;
                if (sourceMaterial == null || readableShader == null) continue;
                Color baseColor = sourceMaterial.HasProperty("_BaseColor")
                    ? sourceMaterial.GetColor("_BaseColor")
                    : sourceMaterial.color;
                Color correctedColor = Color.Lerp(baseColor, Color.white, 0.12f);
                var readableMaterial = new Material(readableShader)
                {
                    name = $"{sourceMaterial.name}_Readable"
                };
                if (readableMaterial.HasProperty("_BaseColor")) readableMaterial.SetColor("_BaseColor", correctedColor);
                if (readableMaterial.HasProperty("_Color")) readableMaterial.SetColor("_Color", correctedColor);
                renderer.material = readableMaterial;
            }
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
            CreateWorldRing(name, center, radius, color, 0.22f);
            CreateWorldRing($"{name}_Inner", center, 2.6f, new Color(color.r, color.g, color.b, 0.8f), 0.12f);

            Vector3 structurePosition = center + Vector3.forward * Mathf.Sign(center.z) * 3.5f;
            GameObject fountain = CreateDemoObject(
                "Fountain",
                $"{name}_Structure",
                PrimitiveType.Cylinder,
                structurePosition,
                new Vector3(2.5f, 1f, 2.5f),
                0f);
            fountain.transform.rotation = Quaternion.Euler(0f, center.z > 0f ? 180f : 0f, 0f);
        }

        private void CreateBushVisual(Vector3 center, Vector3 size, string name)
        {
            GameObject fernPrefab = Resources.Load<GameObject>("KOA/Demo/Environment_Fern");
            GameObject grassPrefab = Resources.Load<GameObject>("KOA/Demo/Environment_Grass");
            GameObject fallbackPrefab = Resources.Load<GameObject>("KOA/Demo/Environment_Bush");
            if (fernPrefab == null && grassPrefab == null && fallbackPrefab == null)
            {
                GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bush.name = name;
                bush.transform.position = center;
                bush.transform.localScale = size;
                Destroy(bush.GetComponent<Collider>());
                var mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = new Color(0.12f, 0.42f, 0.16f, 0.85f);
                bush.GetComponent<Renderer>().material = mat;
                return;
            }

            GameObject group = new GameObject(name);
            group.transform.position = new Vector3(center.x, 0f, center.z);
            for (int i = 0; i < 14; i++)
            {
                GameObject source = (i % 3 == 0 ? grassPrefab : fernPrefab) ?? fallbackPrefab;
                GameObject bush = Instantiate(source, group.transform);
                bush.name = $"BrushFoliage_{i + 1}";
                float laneAcross = (i & 1) == 0 ? -0.72f : 0.72f;
                float along = -size.z * 0.42f + (i / 2) * size.z * 0.14f;
                bush.transform.localPosition = new Vector3(laneAcross + Mathf.Sin(i * 1.7f) * 0.24f, 0.055f, along);
                bush.transform.localRotation = Quaternion.Euler(0f, i * 47f, 0f);
                bush.transform.localScale = Vector3.one * (i % 3 == 0 ? 0.95f : 1.55f);
            }
        }

        private GameObject CreateDemoObject(
            string resourceName,
            string instanceName,
            PrimitiveType fallbackType,
            Vector3 position,
            Vector3 fallbackScale,
            float visualGroundOffset)
        {
            GameObject prefab = Resources.Load<GameObject>($"KOA/Demo/{resourceName}");
            if (prefab == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(fallbackType);
                fallback.name = instanceName;
                fallback.transform.position = position;
                fallback.transform.localScale = fallbackScale;
                return fallback;
            }

            GameObject root = new GameObject(instanceName);
            root.transform.position = position;
            GameObject visual = Instantiate(prefab, root.transform);
            visual.name = "DemoVisual";
            visual.transform.localPosition = Vector3.down * visualGroundOffset;
            visual.transform.localRotation = Quaternion.identity;
            return root;
        }

        private void CreateTeamMarker(Transform parent, Color color, float radius, float groundOffset)
        {
            GameObject marker = new GameObject("TeamMarker");
            marker.name = "TeamMarker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = Vector3.down * groundOffset + Vector3.up * 0.08f;
            LineRenderer line = marker.AddComponent<LineRenderer>();
            ConfigureCircle(line, radius, color, 0.11f, false);
        }

        private void CreateLaneVisuals()
        {
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lane.name = "Lane_Surface";
            lane.transform.position = Vector3.up * 0.025f;
            lane.transform.localScale = new Vector3(1.22f, 1f, 13f);
            Material stoneMaterial = Resources.Load<Material>("KOA/Demo/Materials/KOA_Lane_Stone");
            lane.GetComponent<Renderer>().material = stoneMaterial != null
                ? stoneMaterial
                : CreateRuntimeMaterial(new Color(0.31f, 0.29f, 0.25f));
            Destroy(lane.GetComponent<Collider>());

            CreateLaneBorder(-6.25f, "LaneBorder_West");
            CreateLaneBorder(6.25f, "LaneBorder_East");
            CreateArenaBoundaryVisuals();
            CreateWorldRing("CenterClashZone", Vector3.zero, 6.2f, new Color(1f, 0.72f, 0.18f, 0.9f), 0.14f);

            for (int z = -52; z <= 52; z += 8)
            {
                if (Mathf.Abs(z) < 5) continue;
                GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dash.name = $"Lane_Direction_{z}";
                dash.transform.position = new Vector3(0f, 0.075f, z);
                dash.transform.localScale = new Vector3(0.16f, 0.05f, 2.2f);
                dash.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.7f, 0.65f, 0.5f));
                Destroy(dash.GetComponent<Collider>());
            }
        }

        private void CreateLaneBorder(float x, string name)
        {
            GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
            border.name = name;
            border.transform.position = new Vector3(x, 0.08f, 0f);
            border.transform.localScale = new Vector3(0.28f, 0.12f, 130f);
            border.GetComponent<Renderer>().material = CreateRuntimeMaterial(new Color(0.48f, 0.42f, 0.31f));
            Destroy(border.GetComponent<Collider>());
        }

        private void CreateArenaBoundaryVisuals()
        {
            Color wallColor = new Color(0.20f, 0.23f, 0.22f);
            CreateArenaBoundarySegment("ArenaBoundary_West", new Vector3(-12.78f, 0.34f, 0f), new Vector3(0.44f, 0.68f, 130f), wallColor);
            CreateArenaBoundarySegment("ArenaBoundary_East", new Vector3(12.78f, 0.34f, 0f), new Vector3(0.44f, 0.68f, 130f), wallColor);
            CreateArenaBoundarySegment("ArenaBoundary_Blue", new Vector3(0f, 0.34f, -64.78f), new Vector3(26f, 0.68f, 0.44f), wallColor);
            CreateArenaBoundarySegment("ArenaBoundary_Red", new Vector3(0f, 0.34f, 64.78f), new Vector3(26f, 0.68f, 0.44f), wallColor);
        }

        private void CreateArenaBoundarySegment(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boundary.name = name;
            boundary.transform.position = position;
            boundary.transform.localScale = scale;
            boundary.GetComponent<Renderer>().material = CreateRuntimeMaterial(color);
            Destroy(boundary.GetComponent<Collider>());
        }

        private void CreateStructurePad(Vector3 position, string name, Color color, float radius, float height)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.position = new Vector3(position.x, height * 0.5f, position.z);
            pad.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            Color padColor = Color.Lerp(color, new Color(0.1f, 0.12f, 0.14f), 0.62f);
            pad.GetComponent<Renderer>().material = CreateRuntimeMaterial(padColor);
            Destroy(pad.GetComponent<Collider>());
            CreateWorldRing($"{name}_Ring", position, radius, color, 0.16f);
        }

        private void CreateStructureBeacon(Transform parent, string structureName, Color color, float height, bool isNexus)
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = $"{structureName}_Beacon";
            beacon.transform.SetParent(parent, false);
            beacon.transform.localPosition = new Vector3(0f, height, 0f);
            beacon.transform.localScale = Vector3.one * (isNexus ? 0.58f : 0.28f);
            Destroy(beacon.GetComponent<Collider>());

            Material material = CreateRuntimeMaterial(Color.Lerp(color, Color.white, 0.28f));
            beacon.GetComponent<Renderer>().material = material;

            GameObject crown = new GameObject($"{structureName}_Crown");
            crown.transform.SetParent(parent, false);
            crown.transform.localPosition = new Vector3(0f, height, 0f);
            LineRenderer crownLine = crown.AddComponent<LineRenderer>();
            ConfigureCircle(crownLine, isNexus ? 1.35f : 0.72f, color, isNexus ? 0.13f : 0.075f, false);
        }

        private void CreateStructureSilhouette(Transform parent, string structureName, Color teamColor, string resourceName)
        {
            Color stoneColor = Color.Lerp(teamColor, new Color(0.18f, 0.2f, 0.22f), 0.78f);
            Color accentColor = Color.Lerp(teamColor, Color.white, 0.18f);
            bool isNexus = resourceName == "Nexus";
            float width = isNexus ? 5.8f : resourceName == "Tower_Inner" ? 4.3f : 3.8f;
            float bodyHeight = isNexus ? 4.8f : resourceName == "Tower_Inner" ? 4.0f : 3.5f;

            CreateStructurePiece(parent, $"{structureName}_Foundation", PrimitiveType.Cylinder,
                new Vector3(0f, -1.55f, 0f), new Vector3(width, 0.45f, width), stoneColor);
            CreateStructurePiece(parent, $"{structureName}_Body", PrimitiveType.Cylinder,
                new Vector3(0f, -1.55f + bodyHeight * 0.5f, 0f),
                new Vector3(isNexus ? 2.15f : 1.45f, bodyHeight * 0.5f, isNexus ? 2.15f : 1.45f), stoneColor);

            if (isNexus)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                    CreateStructurePiece(parent, $"{structureName}_Pillar_{i + 1}", PrimitiveType.Cube,
                        new Vector3(Mathf.Cos(angle) * 2.1f, 0.25f, Mathf.Sin(angle) * 2.1f),
                        new Vector3(0.55f, 3.6f, 0.55f), accentColor);
                }
                CreateStructurePiece(parent, $"{structureName}_Core", PrimitiveType.Sphere,
                    new Vector3(0f, 3.45f, 0f), Vector3.one * 1.6f, accentColor);
            }
            else
            {
                CreateStructurePiece(parent, $"{structureName}_Battlement", PrimitiveType.Cube,
                    new Vector3(0f, bodyHeight - 1.2f, 0f),
                    new Vector3(width * 0.72f, 0.8f, width * 0.72f), accentColor);
            }
        }

        private void CreateStructurePiece(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject piece = GameObject.CreatePrimitive(primitiveType);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = localPosition;
            piece.transform.localScale = localScale;
            Destroy(piece.GetComponent<Collider>());
            piece.GetComponent<Renderer>().material = CreateRuntimeMaterial(color);
        }

        private void CreateWorldRing(string name, Vector3 center, float radius, Color color, float width)
        {
            GameObject ring = new GameObject(name);
            ring.transform.position = new Vector3(center.x, 0.09f, center.z);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            ConfigureCircle(line, radius, color, width, false);
        }

        private static void ConfigureCircle(LineRenderer line, float radius, Color color, float width, bool useWorldSpace)
        {
            const int segments = 64;
            line.useWorldSpace = useWorldSpace;
            line.loop = true;
            line.positionCount = segments;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 3;
            line.sortingOrder = 8;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = color;
            line.endColor = color;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }

        private static Material CreateRuntimeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = color;
            return material;
        }

        private void CreateEnvironmentDecorations()
        {
            (string asset, Vector3 position, float yaw)[] decorations =
            {
                ("Environment_Tree", new Vector3(-16f, 0f, -42f), 15f),
                ("Environment_Pine", new Vector3(16f, 0f, -36f), 210f),
                ("Environment_Rock", new Vector3(-15f, 0f, -18f), 55f),
                ("Environment_Tree", new Vector3(16f, 0f, 18f), 130f),
                ("Environment_Pine", new Vector3(-16f, 0f, 36f), 330f),
                ("Environment_Rock", new Vector3(15f, 0f, 44f), 270f)
            };

            foreach ((string asset, Vector3 position, float yaw) decoration in decorations)
            {
                GameObject prefab = Resources.Load<GameObject>($"KOA/Demo/{decoration.asset}");
                if (prefab == null) continue;
                GameObject instance = Instantiate(prefab, decoration.position, Quaternion.Euler(0f, decoration.yaw, 0f));
                instance.name = $"Decor_{decoration.asset}";
            }
        }
    }
}
