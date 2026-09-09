using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KOA.Presentation.Arena
{
    /// <summary>
    /// Presentation-only art pass สำหรับสนามสะพานป่าเหนือเหวของ Project KOA
    /// ไม่มี Simulation Logic และไม่เปลี่ยนกติกาพิกัดจาก Section 3.1
    /// </summary>
    public sealed class BridgeArenaArtDirector : MonoBehaviour
    {
        private readonly List<Material> _runtimeMaterials = new List<Material>();
        private Transform _arenaRoot;
        private Material _grassMaterial;
        private Material _laneMaterial;
        private Material _dirtMaterial;
        private Material _cliffMaterial;
        private Material _voidMaterial;
        private Material _mistMaterial;
        private Material _waterMaterial;
        private Material _cloudMaterial;
        private Material _cloudBankMaterial;
        private Material _foamMaterial;
        private readonly List<Transform> _clouds = new List<Transform>();
        private readonly List<float> _cloudSpeeds = new List<float>();
        private readonly List<Texture2D> _runtimeTextures = new List<Texture2D>();
        private bool _built;

        public void BuildArena()
        {
            if (_built) return;
            _built = true;

            _arenaRoot = new GameObject("BridgeArena_Art").transform;
            _arenaRoot.SetParent(transform, false);

            LoadMaterials();
            ConfigureImageQualityAndAtmosphere();
            CreateChasm();
            CreateLowerValley();
            CreateWaterfalls();
            CreateCloudLayer();
            CreateBridgeDeck();
            CreateBridgeApproachOverscan();
            CreateBridgeSupports();
            CreateAbyssFraming();
            CreateBrokenParapets();
            CreateDenseForest();
            CreateBraziers();
        }

        private void LoadMaterials()
        {
            _grassMaterial = LoadOrCreateMaterial("KOA/Demo/Materials/KOA_Bridge_Grass", new Color(0.20f, 0.34f, 0.16f));
            _laneMaterial = LoadOrCreateMaterial("KOA/Demo/Materials/KOA_Lane_Stone", new Color(0.40f, 0.38f, 0.34f));
            _dirtMaterial = LoadOrCreateMaterial("KOA/Demo/Materials/KOA_Bridge_Dirt", new Color(0.32f, 0.23f, 0.15f));
            _cliffMaterial = LoadOrCreateMaterial("KOA/Demo/Materials/KOA_Bridge_Cliff", new Color(0.22f, 0.24f, 0.23f));
            _voidMaterial = LoadOrCreateMaterial("KOA/Demo/Materials/KOA_Chasm_Void", new Color(0.008f, 0.012f, 0.02f));
            Texture2D waterTexture = CreateWaterTexture();
            Texture2D cloudTexture = CreateCloudTexture();
            _mistMaterial = CreateTransparentMaterial(new Color(0.30f, 0.39f, 0.43f, 0.16f), cloudTexture);
            _waterMaterial = CreateTransparentLitMaterial(
                new Color(0.08f, 0.25f, 0.30f, 0.68f),
                new Color(0.03f, 0.13f, 0.16f),
                0.35f,
                waterTexture);
            _cloudMaterial = CreateTransparentMaterial(new Color(0.62f, 0.70f, 0.74f, 0.18f), cloudTexture);
            _cloudBankMaterial = CreateTransparentMaterial(new Color(0.24f, 0.31f, 0.36f, 0.30f), cloudTexture);
            _foamMaterial = CreateTransparentMaterial(new Color(0.76f, 0.90f, 0.90f, 0.42f), cloudTexture);
        }

        private void ConfigureImageQualityAndAtmosphere()
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 1.8f);
            QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 65f);
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.30f, 0.38f, 0.42f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.23f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.055f, 0.065f, 0.07f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.09f, 0.13f, 0.16f);
            RenderSettings.fogDensity = 0.017f;

            Light sun = FindAnyObjectByType<Light>();
            if (sun != null)
            {
                sun.type = LightType.Directional;
                sun.color = new Color(1f, 0.86f, 0.68f);
                sun.intensity = 1.15f;
                sun.shadows = LightShadows.Soft;
                sun.shadowStrength = 0.82f;
                sun.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            }

            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
                mainCamera.backgroundColor = new Color(0.025f, 0.042f, 0.052f);
                mainCamera.allowHDR = true;
                mainCamera.allowMSAA = true;
            }
        }

        private void CreateChasm()
        {
            GameObject voidFloor = CreatePrimitive(
                "Chasm_DeepVoid",
                PrimitiveType.Plane,
                new Vector3(0f, -14f, 0f),
                new Vector3(12f, 1f, 18f),
                _voidMaterial,
                false);
            voidFloor.transform.SetParent(_arenaRoot, true);

            CreatePrimitive("CliffFace_West", PrimitiveType.Cube, new Vector3(-13.25f, -4.35f, 0f), new Vector3(1.25f, 8.7f, 130f), _cliffMaterial, false);
            CreatePrimitive("CliffFace_East", PrimitiveType.Cube, new Vector3(13.25f, -4.35f, 0f), new Vector3(1.25f, 8.7f, 130f), _cliffMaterial, false);

            CreateMistRibbon("ChasmMist_West", new Vector3(-31f, -8.9f, 0f));
            CreateMistRibbon("ChasmMist_East", new Vector3(31f, -8.9f, 0f));
        }

        private void CreateLowerValley()
        {
            CreatePrimitive("LowerValley_West", PrimitiveType.Plane, new Vector3(-30f, -8.6f, 0f), new Vector3(3.4f, 1f, 15f), _grassMaterial, false);
            CreatePrimitive("LowerValley_East", PrimitiveType.Plane, new Vector3(30f, -8.6f, 0f), new Vector3(3.4f, 1f, 15f), _grassMaterial, false);
            GameObject westRiver = CreatePrimitive("LowerRiver_West", PrimitiveType.Plane, new Vector3(-19f, -8.35f, 0f), new Vector3(0.72f, 1f, 15f), _waterMaterial, false);
            GameObject eastRiver = CreatePrimitive("LowerRiver_East", PrimitiveType.Plane, new Vector3(19f, -8.35f, 0f), new Vector3(0.72f, 1f, 15f), _waterMaterial, false);
            DisableShadows(westRiver);
            DisableShadows(eastRiver);

            GameObject[] trees = LoadPrefabs("Environment_Tree", "Environment_Pine", "Environment_TwistedTree", "Environment_DeadTree");
            GameObject[] rocks = LoadPrefabs("Environment_CliffRock", "Environment_Rock");
            var random = new System.Random(9182);
            for (int i = 0; i < 42; i++)
            {
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * NextRange(random, 23f, 43f), -8.55f, NextRange(random, -70f, 70f));
                SpawnDecoration(trees, $"LowerForest_{i + 1}", position, random, NextRange(random, 0.58f, 1.02f));
            }

            for (int i = 0; i < 28; i++)
            {
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * NextRange(random, 15.5f, 39f), -8.42f, NextRange(random, -68f, 68f));
                SpawnDecoration(rocks, $"LowerValleyRock_{i + 1}", position, random, NextRange(random, 0.36f, 0.78f));
            }
        }

        private void CreateWaterfalls()
        {
            float[] waterfallZ = { -39f, 7f, 43f };
            int waterfallIndex = 0;
            foreach (float z in waterfallZ)
            {
                int side = (waterfallIndex & 1) == 0 ? -1 : 1;
                float edgeX = side * 11.95f;
                GameObject feed = CreatePrimitive($"WaterfallFeed_{waterfallIndex + 1}", PrimitiveType.Plane,
                    new Vector3(edgeX, 0.185f, z), new Vector3(0.28f, 1f, 0.16f), _waterMaterial, false);
                DisableShadows(feed);

                for (int ribbon = -1; ribbon <= 1; ribbon++)
                {
                    GameObject drop = CreatePrimitive($"WaterfallDrop_{waterfallIndex + 1}_{ribbon + 2}", PrimitiveType.Quad,
                        new Vector3(side * (13.47f + ribbon * 0.035f), -4.1f, z + ribbon * 0.46f),
                        new Vector3(ribbon == 0 ? 0.92f : 0.64f, 7.9f, 1f), _waterMaterial, false);
                    drop.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    DisableShadows(drop);
                }

                GameObject pool = CreatePrimitive($"WaterfallPool_{waterfallIndex + 1}", PrimitiveType.Plane,
                    new Vector3(side * 18.5f, -8.25f, z), new Vector3(0.46f, 1f, 0.36f), _waterMaterial, false);
                GameObject edgeFoam = CreatePrimitive($"WaterfallEdgeFoam_{waterfallIndex + 1}", PrimitiveType.Plane,
                    new Vector3(side * 12.86f, 0.205f, z), new Vector3(0.18f, 1f, 0.12f), _foamMaterial, false);
                GameObject poolFoam = CreatePrimitive($"WaterfallPoolFoam_{waterfallIndex + 1}", PrimitiveType.Plane,
                    new Vector3(side * 16.05f, -8.20f, z), new Vector3(0.28f, 1f, 0.22f), _foamMaterial, false);
                DisableShadows(pool);
                DisableShadows(edgeFoam);
                DisableShadows(poolFoam);
                waterfallIndex++;
            }
        }

        private void CreateCloudLayer()
        {
            var random = new System.Random(1209);
            for (int i = 0; i < 8; i++)
            {
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                GameObject cloud = CreatePrimitive(
                    $"ChasmCloud_{i + 1}",
                    PrimitiveType.Plane,
                    new Vector3(side * NextRange(random, 27f, 42f), NextRange(random, -8.0f, -6.8f), NextRange(random, -72f, 72f)),
                    new Vector3(NextRange(random, 0.85f, 1.55f), 1f, NextRange(random, 0.42f, 0.82f)),
                    _cloudMaterial,
                    false);
                cloud.transform.rotation = Quaternion.Euler(0f, NextRange(random, -18f, 18f), 0f);
                DisableShadows(cloud);
                _clouds.Add(cloud.transform);
                _cloudSpeeds.Add(NextRange(random, 0.12f, 0.28f));
            }
        }

        private void CreateMistRibbon(string name, Vector3 position)
        {
            GameObject mist = CreatePrimitive(name, PrimitiveType.Plane, position, new Vector3(3.6f, 1f, 15f), _mistMaterial, false);
            mist.transform.SetParent(_arenaRoot, true);
            DisableShadows(mist);
        }

        private void CreateBridgeDeck()
        {
            GameObject shoulder = CreatePrimitive(
                "Bridge_GrassAndMoss",
                PrimitiveType.Plane,
                new Vector3(0f, 0.055f, 0f),
                new Vector3(2.6f, 1f, 13f),
                _grassMaterial,
                true);
            shoulder.transform.SetParent(_arenaRoot, true);

            GameObject deck = CreatePrimitive(
                "Bridge_StoneLane",
                PrimitiveType.Plane,
                new Vector3(0f, 0.14f, 0f),
                new Vector3(1.22f, 1f, 13f),
                _laneMaterial,
                false);
            deck.transform.SetParent(_arenaRoot, true);

            CreatePrimitive("Bridge_Underside", PrimitiveType.Cube, new Vector3(0f, -0.75f, 0f), new Vector3(25.8f, 1.5f, 129.5f), _cliffMaterial, false);

            for (int z = -56; z <= 56; z += 8)
            {
                GameObject rib = CreatePrimitive(
                    $"Bridge_Rib_{z}",
                    PrimitiveType.Cube,
                    new Vector3(0f, -0.02f, z),
                    new Vector3(12.1f, 0.05f, 0.22f),
                    _cliffMaterial,
                    false);
                rib.transform.SetParent(_arenaRoot, true);
            }
        }

        private void CreateBridgeApproachOverscan()
        {
            const float approachCenter = 77f;
            const float approachLength = 24f;

            for (int end = -1; end <= 1; end += 2)
            {
                string endName = end < 0 ? "South" : "North";
                float centerZ = end * approachCenter;

                // Visual-only continuation. The playable collider remains the original 130 m surface,
                // so Section 3.1 movement bounds do not expand beyond Z -65..65.
                CreatePrimitive(
                    $"BridgeApproach_{endName}_Grass",
                    PrimitiveType.Plane,
                    new Vector3(0f, 0.055f, centerZ),
                    new Vector3(2.6f, 1f, approachLength / 10f),
                    _grassMaterial,
                    false);

                CreatePrimitive(
                    $"BridgeApproach_{endName}_Stone",
                    PrimitiveType.Plane,
                    new Vector3(0f, 0.14f, centerZ),
                    new Vector3(1.22f, 1f, approachLength / 10f),
                    _laneMaterial,
                    false);

                CreatePrimitive(
                    $"BridgeApproach_{endName}_Underside",
                    PrimitiveType.Cube,
                    new Vector3(0f, -0.75f, end * 76.875f),
                    new Vector3(25.8f, 1.5f, 24.25f),
                    _cliffMaterial,
                    false);

                for (int side = -1; side <= 1; side += 2)
                {
                    CreatePrimitive(
                        $"BridgeApproach_{endName}_CliffFace_{side}",
                        PrimitiveType.Cube,
                        new Vector3(side * 13.25f, -4.35f, centerZ),
                        new Vector3(1.25f, 8.7f, approachLength),
                        _cliffMaterial,
                        false);

                    CreatePrimitive(
                        $"BridgeApproach_{endName}_GatePillar_{side}",
                        PrimitiveType.Cube,
                        new Vector3(side * 10.7f, 0.85f, end * 68.2f),
                        new Vector3(1.15f, 1.7f, 1.35f),
                        _cliffMaterial,
                        false);
                }

                // A low threshold tells the player where the authored arena ends without exposing
                // the actual mesh boundary behind it.
                CreatePrimitive(
                    $"BridgeApproach_{endName}_Threshold",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.28f, end * 68.2f),
                    new Vector3(22.2f, 0.38f, 0.75f),
                    _cliffMaterial,
                    false);

                for (int offset = 64; offset <= 80; offset += 8)
                {
                    CreatePrimitive(
                        $"BridgeApproach_{endName}_Rib_{offset}",
                        PrimitiveType.Cube,
                        new Vector3(0f, -0.02f, end * offset),
                        new Vector3(12.1f, 0.05f, 0.22f),
                        _cliffMaterial,
                        false);
                }
            }
        }

        private void CreateBridgeSupports()
        {
            int[] supportZ = { -48, -24, 0, 24, 48 };
            foreach (int z in supportZ)
            {
                CreatePrimitive($"BridgeSupport_Cross_{z}", PrimitiveType.Cube, new Vector3(0f, -4.5f, z), new Vector3(27f, 1.2f, 1.5f), _cliffMaterial, false);
                CreatePrimitive($"BridgeSupport_West_{z}", PrimitiveType.Cube, new Vector3(-11.7f, -7f, z), new Vector3(2f, 13f, 2.2f), _cliffMaterial, false);
                CreatePrimitive($"BridgeSupport_East_{z}", PrimitiveType.Cube, new Vector3(11.7f, -7f, z), new Vector3(2f, 13f, 2.2f), _cliffMaterial, false);
            }
        }

        private void CreateAbyssFraming()
        {
            var random = new System.Random(4419);

            // Vertical facade panels make the bridge read as a constructed high platform,
            // while remaining below the Section 3.1 gameplay surface.
            for (int z = -56, index = 0; z <= 56; z += 8, index++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    GameObject panel = CreatePrimitive(
                        $"BridgeFacade_{side}_{index}",
                        PrimitiveType.Cube,
                        new Vector3(side * 12.82f, -2.15f, z),
                        new Vector3(0.58f, 3.25f, 4.9f),
                        _cliffMaterial,
                        false);
                    panel.transform.rotation = Quaternion.Euler(0f, 0f, side * NextRange(random, -2.2f, 2.2f));
                }
            }

            // Rock silhouettes provide a middle depth layer between the bridge and lower valley.
            for (int i = 0; i < 28; i++)
            {
                int side = (i & 1) == 0 ? -1 : 1;
                float height = NextRange(random, 2.8f, 7.2f);
                GameObject spire = CreatePrimitive(
                    $"AbyssRockSpire_{i + 1}",
                    PrimitiveType.Cube,
                    new Vector3(side * NextRange(random, 17.0f, 27.0f), -8.0f + height * 0.5f, NextRange(random, -67f, 67f)),
                    new Vector3(NextRange(random, 1.4f, 3.4f), height, NextRange(random, 1.4f, 3.6f)),
                    _cliffMaterial,
                    false);
                spire.transform.rotation = Quaternion.Euler(
                    NextRange(random, -9f, 9f),
                    NextRange(random, 0f, 360f),
                    NextRange(random, -7f, 7f));
            }

            // Cloud banks hide the hard edges of the generated world without covering gameplay.
            for (int i = 0; i < 24; i++)
            {
                int side = (i & 1) == 0 ? -1 : 1;
                GameObject bank = CreatePrimitive(
                    $"AbyssCloudBank_{i + 1}",
                    PrimitiveType.Plane,
                    new Vector3(side * NextRange(random, 21f, 34f), NextRange(random, -4.2f, -2.1f), NextRange(random, -72f, 72f)),
                    new Vector3(NextRange(random, 1.1f, 2.2f), 1f, NextRange(random, 0.55f, 1.20f)),
                    _cloudBankMaterial,
                    false);
                bank.transform.rotation = Quaternion.Euler(0f, NextRange(random, -22f, 22f), 0f);
                DisableShadows(bank);
                _clouds.Add(bank.transform);
                _cloudSpeeds.Add(NextRange(random, 0.035f, 0.10f));
            }

            for (int i = 0; i < 10; i++)
            {
                int end = (i & 1) == 0 ? -1 : 1;
                GameObject endBank = CreatePrimitive(
                    $"AbyssEndMist_{i + 1}",
                    PrimitiveType.Plane,
                    new Vector3(NextRange(random, -26f, 26f), NextRange(random, -3.7f, -2.0f), end * NextRange(random, 70f, 82f)),
                    new Vector3(NextRange(random, 1.2f, 2.5f), 1f, NextRange(random, 0.55f, 1.1f)),
                    _cloudBankMaterial,
                    false);
                endBank.transform.rotation = Quaternion.Euler(0f, NextRange(random, -25f, 25f), 0f);
                DisableShadows(endBank);
            }
        }

        private void CreateBrokenParapets()
        {
            var random = new System.Random(3108);
            for (int z = -60; z <= 60; z += 8)
            {
                if (Math.Abs(z) < 5 || Math.Abs(Math.Abs(z) - 32) < 4) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    float jitter = NextRange(random, -0.5f, 0.5f);
                    GameObject block = CreatePrimitive(
                        $"BrokenParapet_{side}_{z}",
                        PrimitiveType.Cube,
                        new Vector3(side * 12.68f, 0.19f, z + jitter),
                        new Vector3(0.46f, NextRange(random, 0.18f, 0.30f), NextRange(random, 2.0f, 3.2f)),
                        _cliffMaterial,
                        false);
                    block.transform.rotation = Quaternion.Euler(NextRange(random, -3f, 3f), NextRange(random, -5f, 5f), NextRange(random, -3f, 3f));
                }
            }
        }

        private void CreateDenseForest()
        {
            GameObject[] treePrefabs = LoadPrefabs("Environment_Tree", "Environment_Pine", "Environment_TwistedTree", "Environment_DeadTree");
            GameObject[] groundPrefabs = LoadPrefabs("Environment_Grass", "Environment_Fern", "Environment_Mushroom");
            GameObject[] smallRockPrefabs = LoadPrefabs("Environment_Rock");
            GameObject cliffRock = Resources.Load<GameObject>("KOA/Demo/Environment_CliffRock");
            var random = new System.Random(7751);

            for (int i = 0; i < 34; i++)
            {
                float z = NextRange(random, -61f, 61f);
                if (IsReservedStructureZone(z)) continue;
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * NextRange(random, 9.4f, 11.7f), 0.055f, z);
                SpawnDecoration(treePrefabs, $"ForestTree_{i + 1}", position, random, NextRange(random, 0.68f, 1.02f));
            }

            for (int i = 0; i < 96; i++)
            {
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * NextRange(random, 6.6f, 12.0f), 0.025f, NextRange(random, -63f, 63f));
                SpawnDecoration(groundPrefabs, $"ForestGround_{i + 1}", position, random, NextRange(random, 0.65f, 1.35f));
            }

            for (int i = 0; i < 20; i++)
            {
                int side = random.Next(0, 2) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * NextRange(random, 8.3f, 12.1f), 0.055f, NextRange(random, -61f, 61f));
                SpawnDecoration(smallRockPrefabs, $"ForestStone_{i + 1}", position, random, NextRange(random, 0.28f, 0.55f));
            }

            for (int z = -62, index = 0; z <= 62; z += 5, index++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 position = new Vector3(side * NextRange(random, 13.05f, 13.55f), -1.15f, z + NextRange(random, -1.4f, 1.4f));
                    SpawnDecoration(cliffRock != null ? new[] { cliffRock } : smallRockPrefabs, $"CliffRock_{side}_{index}", position, random, NextRange(random, 0.36f, 0.62f));
                }
            }
        }

        private static bool IsReservedStructureZone(float z)
        {
            float absoluteZ = Mathf.Abs(z);
            return absoluteZ < 6f
                || Mathf.Abs(absoluteZ - 14f) < 4f
                || Mathf.Abs(absoluteZ - 32f) < 4f
                || Mathf.Abs(absoluteZ - 46f) < 4f
                || Mathf.Abs(absoluteZ - 60f) < 3f;
        }

        private void SpawnDecoration(GameObject[] prefabs, string name, Vector3 position, System.Random random, float scale)
        {
            if (prefabs == null || prefabs.Length == 0) return;
            GameObject source = prefabs[random.Next(0, prefabs.Length)];
            if (source == null) return;

            GameObject instance = Instantiate(source, position, Quaternion.Euler(0f, NextRange(random, 0f, 360f), 0f), _arenaRoot);
            instance.name = name;
            instance.transform.localScale = Vector3.one * scale;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
        }

        private void CreateBraziers()
        {
            int[] zPositions = { -52, -8, 8, 52 };
            for (int i = 0; i < zPositions.Length; i++)
            {
                int side = (i & 1) == 0 ? -1 : 1;
                Vector3 position = new Vector3(side * 10.8f, 0f, zPositions[i]);
                GameObject root = new GameObject($"BridgeBrazier_{i + 1}");
                root.transform.SetParent(_arenaRoot, false);
                root.transform.position = position;
                CreatePrimitivePart(root.transform, "StonePedestal", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), new Vector3(0.72f, 0.45f, 0.72f), _cliffMaterial);
                Material flameMaterial = CreateEmissiveMaterial(new Color(1f, 0.28f, 0.035f), 3.5f);
                CreatePrimitivePart(root.transform, "Flame", PrimitiveType.Sphere, new Vector3(0f, 1.1f, 0f), new Vector3(0.38f, 0.65f, 0.38f), flameMaterial);

                GameObject lightObject = new GameObject("FireLight");
                lightObject.transform.SetParent(root.transform, false);
                lightObject.transform.localPosition = new Vector3(0f, 1.3f, 0f);
                Light fireLight = lightObject.AddComponent<Light>();
                fireLight.type = LightType.Point;
                fireLight.color = new Color(1f, 0.34f, 0.08f);
                fireLight.range = 9f;
                fireLight.intensity = 2.2f;
                fireLight.shadows = LightShadows.Soft;
            }
        }

        private GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool keepCollider)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(_arenaRoot, true);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (!keepCollider)
            {
                Collider collider = instance.GetComponent<Collider>();
                if (collider != null) Destroy(collider);
            }
            return instance;
        }

        private GameObject CreatePrimitivePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return part;
        }

        private GameObject[] LoadPrefabs(params string[] names)
        {
            var result = new List<GameObject>();
            foreach (string name in names)
            {
                GameObject prefab = Resources.Load<GameObject>($"KOA/Demo/{name}");
                if (prefab != null) result.Add(prefab);
            }
            return result.ToArray();
        }

        private Material LoadOrCreateMaterial(string resourcePath, Color fallbackColor)
        {
            Material material = Resources.Load<Material>(resourcePath);
            if (material != null) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material fallback = new Material(shader) { color = fallbackColor };
            _runtimeMaterials.Add(fallback);
            return fallback;
        }

        private Material CreateTransparentMaterial(Color color, Texture2D texture = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            SetMaterialTexture(material, texture, new Vector2(1f, 1f));
            ConfigureTransparentSurface(material);
            _runtimeMaterials.Add(material);
            return material;
        }

        private Material CreateTransparentLitMaterial(
            Color color,
            Color emissionColor,
            float emissionIntensity,
            Texture2D texture = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.64f);
            SetMaterialTexture(material, texture, new Vector2(1.6f, 6f));
            ConfigureTransparentSurface(material);
            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                material.EnableKeyword("_EMISSION");
            }
            _runtimeMaterials.Add(material);
            return material;
        }

        private static void ConfigureTransparentSurface(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetOverrideTag("Queue", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_AlphaClip")) material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_SrcBlendAlpha")) material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            if (material.HasProperty("_DstBlendAlpha")) material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void SetMaterialTexture(Material material, Texture2D texture, Vector2 tiling)
        {
            if (texture == null) return;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", texture);
                material.SetTextureScale("_MainTex", tiling);
            }
        }

        private Texture2D CreateWaterTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "KOA_Runtime_Water",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;
                    float broad = Mathf.PerlinNoise(u * 5.5f, v * 13f);
                    float ripple = 0.5f + 0.5f * Mathf.Sin((u * 5f + v * 18f + broad * 1.8f) * Mathf.PI);
                    byte value = (byte)Mathf.RoundToInt(Mathf.Lerp(150f, 236f, broad * 0.65f + ripple * 0.35f));
                    pixels[y * size + x] = new Color32(value, value, value, 255);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            _runtimeTextures.Add(texture);
            return texture;
        }

        private Texture2D CreateCloudTexture()
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
            {
                name = "KOA_Runtime_SoftCloud",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float radial = Mathf.Clamp01(1f - (nx * nx + ny * ny));
                    float noise = Mathf.PerlinNoise(x * 0.055f + 4.1f, y * 0.055f + 8.7f);
                    float alpha = Mathf.SmoothStep(0f, 1f, radial) * Mathf.Lerp(0.42f, 1f, noise);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            _runtimeTextures.Add(texture);
            return texture;
        }

        private static void DisableShadows(GameObject target)
        {
            if (target == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void Update()
        {
            if (_waterMaterial != null)
            {
                Vector2 offset = new Vector2(Time.time * 0.018f, Time.time * 0.045f);
                if (_waterMaterial.HasProperty("_BaseMap")) _waterMaterial.SetTextureOffset("_BaseMap", offset);
                if (_waterMaterial.HasProperty("_MainTex")) _waterMaterial.SetTextureOffset("_MainTex", offset);
            }

            for (int i = 0; i < _clouds.Count; i++)
            {
                Transform cloud = _clouds[i];
                if (cloud == null) continue;
                cloud.position += Vector3.forward * (_cloudSpeeds[i] * Time.deltaTime);
                if (cloud.position.z > 78f)
                    cloud.position = new Vector3(cloud.position.x, cloud.position.y, -78f);
            }
        }

        private Material CreateEmissiveMaterial(Color color, float intensity)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            material.SetColor("_EmissionColor", color * intensity);
            material.EnableKeyword("_EMISSION");
            _runtimeMaterials.Add(material);
            return material;
        }

        private static float NextRange(System.Random random, float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private void OnDestroy()
        {
            foreach (Material material in _runtimeMaterials)
            {
                if (material != null) Destroy(material);
            }
            _runtimeMaterials.Clear();

            foreach (Texture2D texture in _runtimeTextures)
            {
                if (texture != null) Destroy(texture);
            }
            _runtimeTextures.Clear();
        }
    }
}
