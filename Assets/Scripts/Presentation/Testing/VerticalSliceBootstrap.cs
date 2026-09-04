using KOA.Core.AI;
using KOA.Core.Entities;
using KOA.Core.Match;
using KOA.Presentation.Camera;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using KOA.Presentation.Views;
using UnityEngine;

namespace KOA.Presentation.Testing
{
    /// <summary>
    /// Auto Bootstrapper สำหรับทดสอบ Phase 3 เต็มรูปแบบ (Full Match Setup)
    /// สร้างแผนที่ 1v1 เลนเดียว 70x20m, ป้อมปราการ 2 ฝั่ง, Player Hero, AI Bot (Easy/Med/Hard), Shop, และ MatchHUD
    /// </summary>
    public class VerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Bootstrap Options")]
        [SerializeField] private bool autoBuildOnStart = true;
        [SerializeField] private BotDifficulty initialBotDifficulty = BotDifficulty.Medium;

        public MatchSimulation MatchSimulation { get; private set; }
        public ModularBotBrain BotBrain { get; private set; }
        public HeroBase3D CurrentPlayerHero { get; private set; }
        public VorkasHero BotHero { get; private set; }

        private GameObject _playerHeroGo;
        private HeroView _playerHeroView;
        private MatchHUD _matchHud;

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildFullArena();
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
        }

        public void BuildFullArena()
        {
            Debug.Log("[Bootstrap] Initializing KOA Full Match Arena (Phase 3)...");

            // 1. Directional Light
            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                Light dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // 2. Ground Plane 70x20m (Section 3.1)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DuelArena_70x20m";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2.0f, 1.0f, 7.0f); // 20m x 70m
            ground.GetComponent<Renderer>().material.color = new Color(0.20f, 0.23f, 0.22f);

            // 3. DamagePopupManager
            if (FindObjectOfType<DamagePopupManager>() == null)
            {
                new GameObject("DamagePopupManager").AddComponent<DamagePopupManager>();
            }

            // 4. Match Simulation (Blue Fountain: Z = -32, Red Fountain: Z = +32)
            Vector3 blueFountain = new Vector3(0, 0, -32f);
            Vector3 redFountain = new Vector3(0, 0, 32f);
            MatchSimulation = new MatchSimulation(blueFountain, redFountain);

            // 5. สร้างป้อมปราการใน Scene ทั้ง 2 ฝั่ง (Section 3.3)
            SpawnTowerView(MatchSimulation.BlueOuterTower, "BlueOuterTower", Color.cyan);
            SpawnTowerView(MatchSimulation.BlueInnerTower, "BlueInnerTower", Color.blue);
            SpawnTowerView(MatchSimulation.BlueNexus, "BlueNexus", Color.magenta);

            SpawnTowerView(MatchSimulation.RedOuterTower, "RedOuterTower", new Color(1f, 0.4f, 0f));
            SpawnTowerView(MatchSimulation.RedInnerTower, "RedInnerTower", Color.red);
            SpawnTowerView(MatchSimulation.RedNexus, "RedNexus", new Color(0.7f, 0f, 0f));

            // 6. สร้าง Player Hero (เริ่มด้วย Vorkas)
            SpawnPlayerHero("Vorkas");

            // 7. สร้าง Bot Hero (Red Team) ควบคุมโดย ModularBotBrain (Section 9)
            GameObject botGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            botGo.name = "Bot_Vorkas";
            botGo.transform.position = new Vector3(0, 1.0f, 8.0f);
            botGo.GetComponent<Renderer>().material.color = new Color(0.9f, 0.2f, 0.2f);

            BotHero = new VorkasHero(botGo.transform.position);
            CreateHealthBar(botGo.transform, "BotHealthBar", Color.red);

            BotBrain = new ModularBotBrain(BotHero, redFountain, blueFountain, initialBotDifficulty);
            BotBrain.TargetEnemyHero = CurrentPlayerHero;

            // 8. สร้าง MatchHUD
            GameObject hudGo = new GameObject("MatchHUD");
            _matchHud = hudGo.AddComponent<MatchHUD>();
            _matchHud.BindMatch(MatchSimulation, CurrentPlayerHero, MatchSimulation.BlueWallet, BotBrain);
            _matchHud.OnHeroSwitched += SwitchHero;

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

        private void SpawnPlayerHero(string heroName)
        {
            if (_playerHeroGo != null)
            {
                Destroy(_playerHeroGo);
            }

            _playerHeroGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _playerHeroGo.name = $"Hero_{heroName}";
            _playerHeroGo.transform.position = new Vector3(0, 1.0f, -4.0f);

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

            CreateHealthBar(_playerHeroGo.transform, "PlayerHealthBar", Color.green);

            PCInputAdapter inputAdapter = _playerHeroGo.AddComponent<PCInputAdapter>();
            _playerHeroView = _playerHeroGo.AddComponent<HeroView>();
            _playerHeroView.BindHero(CurrentPlayerHero);

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

            CreateHealthBar(towerGo.transform, $"{name}_HealthBar", color);

            TowerView view = towerGo.AddComponent<TowerView>();
            view.BindLogic(tower);
        }

        private void CreateHealthBar(Transform parentTarget, string barName, Color color)
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
        }
    }
}
