#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KOA.Editor
{
    /// <summary>
    /// Prepares the selected CC0/QAL demo art without importing third-party scripts or project settings.
    /// Generated prefabs stay in Presentation/Resources and are consumed through presentation-only fallbacks.
    /// </summary>
    public static class DemoAssetIntegration
    {
        private const string ArtRoot = "Assets/Presentation/Art";
        private const string MaterialRoot = ArtRoot + "/Materials";
        private const string EnvironmentMaterialRoot = MaterialRoot + "/Environment";
        private const string ControllerRoot = ArtRoot + "/Controllers";
        private const string PrefabRoot = "Assets/Resources/KOA/Demo";
        private const string RuntimeMaterialRoot = PrefabRoot + "/Materials";
        private const string LaneMaterialPath = RuntimeMaterialRoot + "/KOA_Lane_Stone.mat";
        private const string GrassMaterialPath = RuntimeMaterialRoot + "/KOA_Bridge_Grass.mat";
        private const string DirtMaterialPath = RuntimeMaterialRoot + "/KOA_Bridge_Dirt.mat";
        private const string CliffMaterialPath = RuntimeMaterialRoot + "/KOA_Bridge_Cliff.mat";
        private const string VoidMaterialPath = RuntimeMaterialRoot + "/KOA_Chasm_Void.mat";

        private static readonly string[] HumanoidModels =
        {
            "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Models/Superhero_Male_FullBody.fbx",
            "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Models/Superhero_Female_FullBody.fbx",
            "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Male_Ranger.fbx",
            "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Female_Ranger.fbx",
            "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Male_Peasant.fbx",
            "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Female_Peasant.fbx",
            "Assets/ThirdParty/Quaternius/Monsters/Dungeon/FBX (Unity)/Imp.fbx",
            "Assets/ThirdParty/Quaternius/Monsters/Dungeon/FBX (Unity)/Puglin.fbx"
        };

        private static readonly string[] AnimationLibraries =
        {
            "Assets/ThirdParty/Quaternius/Animations/UAL1/UAL1_Standard.fbx",
            "Assets/ThirdParty/Quaternius/Animations/UAL2/UAL2_Standard.fbx"
        };

        private static readonly string[] EnvironmentModels =
        {
            "Assets/ThirdParty/Quaternius/Environment/Structures/Models/WatchTower_FirstAge_Level2.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Temple_FirstAge_Level3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Wonder_FirstAge_Level3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Temple_SecondAge_Level3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Bush_Common_Flowers.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/CommonTree_3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Pine_3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Rock_Medium_2.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Rock_Medium_3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/TwistedTree_1.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/DeadTree_3.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Grass_Common_Tall.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Fern_1.fbx",
            "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Mushroom_Common.fbx"
        };

        [MenuItem("KOA/Assets/Prepare Selected Demo Assets")]
        public static void PrepareSelectedDemoAssets()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(EnvironmentMaterialRoot);
            EnsureFolder(ControllerRoot);
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/KOA");
            EnsureFolder(PrefabRoot);
            EnsureFolder(RuntimeMaterialRoot);

            ConfigureTextureImporters();
            foreach (string path in HumanoidModels) ConfigureHumanoid(path, false);
            foreach (string path in AnimationLibraries) ConfigureHumanoid(path, true);

            Dictionary<string, Material> materials = CreateMaterials();
            ApplyMaterialRemaps(materials);
            ApplyEnvironmentMaterialRemaps();
            CreateLaneSurfaceMaterial();
            CreateArenaSurfaceMaterials();
            Dictionary<string, AnimatorController> controllers = CreateAnimatorControllers();
            CreateDemoPrefabs(controllers, materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("KOA_DEMO_ASSETS:PASS selected assets configured and demo prefabs generated");
        }

        [MenuItem("KOA/Assets/Rebuild Generated Demo Prefabs")]
        public static void RebuildGeneratedDemoPrefabs()
        {
            EnsureFolder(ArtRoot);
            EnsureFolder(MaterialRoot);
            EnsureFolder(EnvironmentMaterialRoot);
            EnsureFolder(ControllerRoot);
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/KOA");
            EnsureFolder(PrefabRoot);
            EnsureFolder(RuntimeMaterialRoot);
            Dictionary<string, Material> materials = CreateMaterials();
            ApplyEnvironmentMaterialRemaps();
            CreateLaneSurfaceMaterial();
            CreateArenaSurfaceMaterials();
            Dictionary<string, AnimatorController> controllers = CreateAnimatorControllers();
            CreateDemoPrefabs(controllers, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("KOA_DEMO_ASSETS:REBUILD_PASS generated demo prefabs refreshed");
        }

        public static void RebuildAndValidateDemoAssets()
        {
            RebuildGeneratedDemoPrefabs();
            DemoAssetDiagnostics.LogImportedAssetSummary();
        }

        [InitializeOnLoadMethod]
        private static void QueueMissingGeneratedAssetRefresh()
        {
            EditorApplication.delayCall += RefreshGeneratedAssetsWhenSafe;
        }

        private static void RefreshGeneratedAssetsWhenSafe()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            string[] requiredControllers =
            {
                $"{ControllerRoot}/KOA_HeroMelee.controller",
                $"{ControllerRoot}/KOA_HeroCaster.controller",
                $"{ControllerRoot}/KOA_HeroRanged.controller",
                $"{ControllerRoot}/KOA_Monster.controller"
            };
            if (requiredControllers.All(path => AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null)
                && AssetDatabase.IsValidFolder(EnvironmentMaterialRoot)
                && AssetDatabase.LoadAssetAtPath<Material>(LaneMaterialPath) != null
                && AssetDatabase.LoadAssetAtPath<Material>(GrassMaterialPath) != null
                && AssetDatabase.LoadAssetAtPath<Material>(DirtMaterialPath) != null
                && AssetDatabase.LoadAssetAtPath<Material>(CliffMaterialPath) != null
                && AssetDatabase.LoadAssetAtPath<Material>(VoidMaterialPath) != null)
                return;

            RebuildGeneratedDemoPrefabs();
        }

        private static void ConfigureTextureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets/ThirdParty" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                bool isUi = path.Contains("/Kenney/UI/");
                bool isNormal = fileName.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isLinear = fileName.IndexOf("ORM", StringComparison.OrdinalIgnoreCase) >= 0
                    || fileName.IndexOf("Roughness", StringComparison.OrdinalIgnoreCase) >= 0
                    || fileName.IndexOf("Emissive", StringComparison.OrdinalIgnoreCase) >= 0;

                importer.textureType = isUi ? TextureImporterType.Sprite : isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !isNormal && !isLinear;
                importer.maxTextureSize = isUi ? 1024 : 2048;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.alphaIsTransparency = isUi || fileName.IndexOf("BaseColor", StringComparison.OrdinalIgnoreCase) >= 0;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureHumanoid(string path, bool containsAnimations)
        {
            if (!(AssetImporter.GetAtPath(path) is ModelImporter importer))
            {
                Debug.LogError($"KOA_DEMO_ASSETS:MISSING_MODEL path={path}");
                return;
            }

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = containsAnimations;
            importer.importBlendShapes = false;
            importer.optimizeGameObjects = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;

            if (containsAnimations)
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                for (int i = 0; i < clips.Length; i++)
                {
                    string name = clips[i].name;
                    clips[i].loopTime = name.IndexOf("Loop", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.EndsWith("Sword_Idle", StringComparison.OrdinalIgnoreCase);
                    clips[i].lockRootRotation = true;
                    clips[i].lockRootHeightY = true;
                    clips[i].lockRootPositionXZ = true;
                }
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var result = new Dictionary<string, Material>(StringComparer.Ordinal);
            result["MI_Ranger"] = CreateLitMaterial("KOA_Ranger_Primary", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Ranger/T_Ranger_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Ranger/T_Ranger_Normal.png");
            result["MI_Peasant"] = CreateLitMaterial("KOA_Peasant_Primary", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Peasant/T_Peasant_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Peasant/T_Peasant_Normal.png");
            result["MI_Regular_Male"] = CreateLitMaterial("KOA_Regular_Male", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Base/T_Regular_Male_Dark_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Base/T_Regular_Male_Normal.png");
            result["MI_Regular_Female"] = CreateLitMaterial("KOA_Regular_Female", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Base/T_Regular_Female_Dark_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Base/T_Regular_Female_Normal.png");
            result["MI_Hair_1"] = CreateLitMaterial("KOA_Hair_1", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Hair_1_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Hair_1_Normal.png");
            result["MI_Hair_2"] = CreateLitMaterial("KOA_Hair_2", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Hair_2_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Hair_2_Normal.png");
            result["MI_Eyes"] = CreateLitMaterial("KOA_Eyes", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Eye_Brown.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Eye_Normal.png");
            result["MI_Superhero_Male"] = CreateLitMaterial("KOA_Superhero_Male", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Male_Ligh.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Male_Normal.png");
            result["MI_Superhero_Female"] = CreateLitMaterial("KOA_Superhero_Female", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Female_Light_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Female_Normal.png");
            result["MI_Imp"] = CreateLitMaterial("KOA_Imp", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Imp_BaseColor_1.png", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Imp_Normal.png", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Imp_Emissive.png");
            result["MI_Puglin"] = CreateLitMaterial("KOA_Puglin", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Puglin_BaseColor_1.png", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Puglin_Normal.png", "Assets/ThirdParty/Quaternius/Monsters/Dungeon/Textures/T_Puglin_Emissive.png");

            result["KOA_Ranger_Alt"] = CreateLitMaterial("KOA_Ranger_Alt", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Ranger/T_Ranger_3_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Ranger/T_Ranger_Normal.png");
            result["KOA_Peasant_Alt"] = CreateLitMaterial("KOA_Peasant_Alt", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Peasant/T_Peasant_2_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Textures/Peasant/T_Peasant_Normal.png");
            result["KOA_Superhero_Male_Dark"] = CreateLitMaterial("KOA_Superhero_Male_Dark", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Male_Dark.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Male_Normal.png");
            result["KOA_Superhero_Female_Dark"] = CreateLitMaterial("KOA_Superhero_Female_Dark", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Female_Dark_BaseColor.png", "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Textures/T_Superhero_Female_Normal.png");
            return result;
        }

        private static Material CreateLitMaterial(string name, string baseMapPath, string normalPath, string emissionPath = null)
        {
            string assetPath = $"{MaterialRoot}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            Texture2D baseMap = AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath);
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            material.SetTexture("_BaseMap", baseMap);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.25f);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }

            if (!string.IsNullOrEmpty(emissionPath))
            {
                Texture2D emission = AssetDatabase.LoadAssetAtPath<Texture2D>(emissionPath);
                material.SetTexture("_EmissionMap", emission);
                material.SetColor("_EmissionColor", Color.white);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterialRemaps(IReadOnlyDictionary<string, Material> materials)
        {
            foreach (string path in HumanoidModels)
            {
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
                bool changed = false;
                foreach (Material embedded in embeddedMaterials)
                {
                    if (!materials.TryGetValue(embedded.name, out Material replacement)) continue;
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), replacement);
                    changed = true;
                }
                if (changed) importer.SaveAndReimport();
            }
        }

        private static void ApplyEnvironmentMaterialRemaps()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable; environment material remap cannot continue.");

            foreach (string modelPath in EnvironmentModels)
            {
                if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer))
                {
                    Debug.LogError($"KOA_DEMO_ASSETS:MISSING_ENVIRONMENT_MODEL path={modelPath}");
                    continue;
                }

                Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>().ToArray();
                bool changed = false;
                foreach (Material source in embeddedMaterials)
                {
                    string modelName = System.IO.Path.GetFileNameWithoutExtension(modelPath);
                    string safeMaterialName = SanitizeAssetName(source.name);
                    string materialPath = $"{EnvironmentMaterialRoot}/KOA_{modelName}_{safeMaterialName}.mat";
                    Material replacement = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (replacement == null)
                    {
                        replacement = new Material(urpShader) { name = $"KOA_{modelName}_{safeMaterialName}" };
                        AssetDatabase.CreateAsset(replacement, materialPath);
                    }

                    Color baseColor = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                    Texture baseTexture = ResolveEnvironmentBaseTexture(modelPath, source);
                    replacement.SetColor("_BaseColor", baseColor);
                    replacement.SetTexture("_BaseMap", baseTexture);
                    bool isStructure = modelPath.Contains("/Structures/");
                    replacement.SetFloat("_Smoothness", isStructure ? 0.28f : 0.12f);
                    if (baseTexture != null)
                    {
                        replacement.SetTextureScale("_BaseMap", source.mainTextureScale);
                        replacement.SetTextureOffset("_BaseMap", source.mainTextureOffset);
                    }

                    bool alphaClip = source.name.IndexOf("Leav", StringComparison.OrdinalIgnoreCase) >= 0
                        || source.name.IndexOf("Leaf", StringComparison.OrdinalIgnoreCase) >= 0
                        || source.name.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0
                        || source.name.IndexOf("Flower", StringComparison.OrdinalIgnoreCase) >= 0;
                    replacement.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
                    replacement.SetFloat("_Cutoff", 0.35f);
                    replacement.SetFloat("_Cull", isStructure || alphaClip ? 0f : 2f);
                    if (alphaClip)
                    {
                        replacement.EnableKeyword("_ALPHATEST_ON");
                        replacement.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    }
                    else
                    {
                        replacement.DisableKeyword("_ALPHATEST_ON");
                        replacement.renderQueue = -1;
                    }

                    EditorUtility.SetDirty(replacement);
                    AssetDatabase.SaveAssetIfDirty(replacement);
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source.name), replacement);
                    changed = true;
                }

                if (changed) importer.SaveAndReimport();
            }
        }

        private static Texture ResolveEnvironmentBaseTexture(string modelPath, Material source)
        {
            Texture texture = source.mainTexture;
            if (!modelPath.Contains("/Nature/") || texture == null) return texture;

            string texturePath = AssetDatabase.GetAssetPath(texture);
            string extension = System.IO.Path.GetExtension(texturePath);
            string coloredPath = texturePath.Substring(0, texturePath.Length - extension.Length) + "_C" + extension;
            Texture coloredTexture = AssetDatabase.LoadAssetAtPath<Texture>(coloredPath);
            return coloredTexture != null ? coloredTexture : texture;
        }

        private static void CreateLaneSurfaceMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable; lane material cannot be created.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(LaneMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "KOA_Lane_Stone" };
                AssetDatabase.CreateAsset(material, LaneMaterialPath);
            }

            Texture2D stoneTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/ThirdParty/Quaternius/Environment/Nature/Textures/PathRocks_Diffuse.png");
            material.SetTexture("_BaseMap", stoneTexture);
            material.SetTextureScale("_BaseMap", new Vector2(2.5f, 24f));
            material.SetColor("_BaseColor", new Color(0.72f, 0.69f, 0.61f));
            material.SetFloat("_Smoothness", 0.08f);
            EditorUtility.SetDirty(material);
        }

        private static void CreateArenaSurfaceMaterials()
        {
            CreateArenaMaterial(
                GrassMaterialPath,
                "KOA_Bridge_Grass",
                "Assets/ThirdParty/Quaternius/Environment/Nature/Textures/Rocks_Diffuse.png",
                null,
                new Color(0.34f, 0.50f, 0.29f),
                new Vector2(4f, 22f),
                0.04f);

            CreateArenaMaterial(
                DirtMaterialPath,
                "KOA_Bridge_Dirt",
                "Assets/ThirdParty/Quaternius/Environment/Nature/Textures/Rocks_Desert_Diffuse.png",
                null,
                new Color(0.58f, 0.43f, 0.29f),
                new Vector2(3f, 18f),
                0.03f);

            CreateArenaMaterial(
                CliffMaterialPath,
                "KOA_Bridge_Cliff",
                "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Textures/T_RockTrim_BaseColor.png",
                "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Textures/T_RockTrim_Normal.png",
                new Color(0.55f, 0.57f, 0.54f),
                new Vector2(2.5f, 12f),
                0.10f);

            CreateArenaMaterial(
                LaneMaterialPath,
                "KOA_Lane_Stone",
                "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Textures/T_UnevenBrick_BaseColor.png",
                "Assets/ThirdParty/Quaternius/Environment/MedievalVillage/Textures/T_UnevenBrick_Normal.png",
                new Color(0.72f, 0.70f, 0.64f),
                new Vector2(2.2f, 25f),
                0.08f);

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null)
                throw new InvalidOperationException("URP Unlit shader is unavailable; chasm material cannot be created.");

            Material voidMaterial = AssetDatabase.LoadAssetAtPath<Material>(VoidMaterialPath);
            if (voidMaterial == null)
            {
                voidMaterial = new Material(unlitShader) { name = "KOA_Chasm_Void" };
                AssetDatabase.CreateAsset(voidMaterial, VoidMaterialPath);
            }
            voidMaterial.SetColor("_BaseColor", new Color(0.012f, 0.018f, 0.026f, 1f));
            EditorUtility.SetDirty(voidMaterial);
        }

        private static Material CreateArenaMaterial(
            string assetPath,
            string materialName,
            string baseMapPath,
            string normalPath,
            Color tint,
            Vector2 tiling,
            float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable; arena materials cannot be created.");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMapPath));
            material.SetTextureScale("_BaseMap", tiling);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);

            Texture2D normal = string.IsNullOrEmpty(normalPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                material.SetTexture("_BumpMap", normal);
                material.SetTextureScale("_BumpMap", tiling);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SanitizeAssetName(string value)
        {
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        }

        private static Dictionary<string, AnimatorController> CreateAnimatorControllers()
        {
            return new Dictionary<string, AnimatorController>(StringComparer.Ordinal)
            {
                ["Melee"] = CreateAnimatorController(
                    "KOA_HeroMelee", FindClip(AnimationLibraries[0], "Idle_Loop"), FindClip(AnimationLibraries[0], "Walk_Loop"),
                    FindClip(AnimationLibraries[1], "Sword_Regular_A"), FindClip(AnimationLibraries[1], "Sword_Heavy_Combo"), FindClip(AnimationLibraries[0], "Death01")),
                ["Caster"] = CreateAnimatorController(
                    "KOA_HeroCaster", FindClip(AnimationLibraries[0], "Idle_Loop"), FindClip(AnimationLibraries[0], "Walk_Loop"),
                    FindClip(AnimationLibraries[0], "Spell_Simple_Shoot"), FindClip(AnimationLibraries[0], "Spell_Simple_Shoot"), FindClip(AnimationLibraries[0], "Death01")),
                ["Ranged"] = CreateAnimatorController(
                    "KOA_HeroRanged", FindClip(AnimationLibraries[0], "Idle_Loop"), FindClip(AnimationLibraries[0], "Walk_Loop"),
                    FindClip(AnimationLibraries[0], "Pistol_Shoot"), FindClip(AnimationLibraries[1], "OverhandThrow"), FindClip(AnimationLibraries[0], "Death01")),
                ["Monster"] = CreateAnimatorController(
                    "KOA_Monster", FindClip(AnimationLibraries[1], "Zombie_Idle_Loop"), FindClip(AnimationLibraries[1], "Zombie_Walk_Fwd_Loop"),
                    FindClip(AnimationLibraries[1], "Zombie_Scratch"), FindClip(AnimationLibraries[1], "Melee_Hook"), FindClip(AnimationLibraries[0], "Death01"))
            };
        }

        private static AnimatorController CreateAnimatorController(
            string controllerName,
            AnimationClip idleClip,
            AnimationClip moveClip,
            AnimationClip attackClip,
            AnimationClip castClip,
            AnimationClip deathClip)
        {
            if (idleClip == null || moveClip == null || attackClip == null || castClip == null || deathClip == null)
            {
                throw new InvalidOperationException(
                    $"Cannot create {controllerName}: one or more required animation clips are missing. " +
                    $"idle={(idleClip != null)} move={(moveClip != null)} attack={(attackClip != null)} " +
                    $"cast={(castClip != null)} death={(deathClip != null)}");
            }

            string path = $"{ControllerRoot}/{controllerName}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            }
            else
            {
                // These controllers are generated assets. Rebuild their state graph so animation
                // polish changes also migrate existing projects without changing controller GUIDs.
                controller.parameters = Array.Empty<AnimatorControllerParameter>();
                AnimatorStateMachine existingMachine = controller.layers[0].stateMachine;
                existingMachine.states = Array.Empty<ChildAnimatorState>();
                existingMachine.stateMachines = Array.Empty<ChildAnimatorStateMachine>();
                existingMachine.anyStateTransitions = Array.Empty<AnimatorStateTransition>();
                existingMachine.entryTransitions = Array.Empty<AnimatorTransition>();
            }

            controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            controller.AddParameter("LocomotionRate", AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Cast", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            idle.motion = idleClip;
            AnimatorState move = machine.AddState("Move");
            move.motion = moveClip;
            move.speedParameter = "LocomotionRate";
            move.speedParameterActive = true;
            AnimatorState attack = machine.AddState("Attack");
            attack.motion = attackClip;
            AnimatorState cast = machine.AddState("Cast");
            cast.motion = castClip;
            AnimatorState death = machine.AddState("Death");
            death.motion = deathClip;
            machine.defaultState = idle;

            AddConditionTransition(idle, move, "MoveSpeed", AnimatorConditionMode.Greater, 0.1f);
            AddConditionTransition(move, idle, "MoveSpeed", AnimatorConditionMode.Less, 0.1f);
            AddTriggeredTransition(machine, attack, "Attack");
            AddTriggeredTransition(machine, cast, "Cast");
            AddReturnTransition(attack, idle);
            AddReturnTransition(cast, idle);

            AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(death);
            deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "Dead");
            deathTransition.duration = 0.1f;
            deathTransition.canTransitionToSelf = false;
            AnimatorStateTransition reviveTransition = death.AddTransition(idle);
            reviveTransition.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
            reviveTransition.duration = 0.15f;
            return controller;
        }

        private static AnimationClip FindClip(string path, string suffix)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name.Equals(suffix, StringComparison.OrdinalIgnoreCase)
                    || clip.name.EndsWith("|" + suffix, StringComparison.OrdinalIgnoreCase));
        }

        private static void AddConditionTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.AddCondition(mode, threshold, parameter);
            transition.duration = 0.12f;
            transition.hasExitTime = false;
        }

        private static void AddTriggeredTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
        {
            AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
        }

        private static void AddReturnTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.92f;
            transition.duration = 0.08f;
        }

        private static void CreateDemoPrefabs(IReadOnlyDictionary<string, AnimatorController> controllers, IReadOnlyDictionary<string, Material> materials)
        {
            CreateVisualPrefab("Hero_Vorkas", HumanoidModels[0], 2.1f, controllers["Melee"], null);
            CreateVisualPrefab("Hero_Zenthis", HumanoidModels[1], 2.0f, controllers["Caster"], null);
            CreateVisualPrefab("Hero_Korvax", HumanoidModels[0], 2.0f, controllers["Ranged"], materials["KOA_Superhero_Male_Dark"]);
            CreateVisualPrefab("Hero_Gravitor", HumanoidModels[7], 2.35f, controllers["Monster"], null);

            CreateVisualPrefab("Minion_Melee", HumanoidModels[6], 1.15f, controllers["Monster"], null);
            CreateVisualPrefab("Minion_Ranged", HumanoidModels[7], 1.05f, controllers["Monster"], null);
            CreateVisualPrefab("Minion_Cannon", HumanoidModels[7], 1.45f, controllers["Monster"], null);
            CreateVisualPrefab("Minion_Super", HumanoidModels[6], 2.0f, controllers["Monster"], null);

            CreateVisualPrefab("Tower_Outer", "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Temple_FirstAge_Level3.fbx", 3.6f, null, null, true, 0.82f);
            CreateVisualPrefab("Tower_Inner", "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Temple_SecondAge_Level3.fbx", 4.3f, null, null, true, 0.92f);
            CreateVisualPrefab("Nexus", "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Wonder_FirstAge_Level3.fbx", 5.4f, null, null, true, 0.78f);
            CreateVisualPrefab("Fountain", "Assets/ThirdParty/Quaternius/Environment/Structures/Models/Temple_SecondAge_Level3.fbx", 3.8f, null, null, false, 0.90f);

            CreateVisualPrefab("Environment_Bush", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Bush_Common_Flowers.fbx", 1.15f, null, null, false);
            CreateVisualPrefab("Environment_Tree", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/CommonTree_3.fbx", 5.5f, null, null, false);
            CreateVisualPrefab("Environment_Pine", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Pine_3.fbx", 6.0f, null, null, false);
            CreateVisualPrefab("Environment_Rock", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Rock_Medium_2.fbx", 1.6f, null, null, false);
            CreateVisualPrefab("Environment_CliffRock", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Rock_Medium_3.fbx", 3.5f, null, null, false);
            CreateVisualPrefab("Environment_TwistedTree", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/TwistedTree_1.fbx", 6.5f, null, null, false);
            CreateVisualPrefab("Environment_DeadTree", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/DeadTree_3.fbx", 5.5f, null, null, false);
            CreateVisualPrefab("Environment_Grass", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Grass_Common_Tall.fbx", 0.8f, null, null, false);
            CreateVisualPrefab("Environment_Fern", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Fern_1.fbx", 0.75f, null, null, false);
            CreateVisualPrefab("Environment_Mushroom", "Assets/ThirdParty/Quaternius/Environment/Nature/FBX (Unity)/Mushroom_Common.fbx", 0.4f, null, null, false);
        }

        private static void CreateVisualPrefab(
            string prefabName,
            string modelPath,
            float targetHeight,
            RuntimeAnimatorController controller,
            Material variantMaterial,
            bool addCollider = true,
            float horizontalScale = 1f)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null)
            {
                Debug.LogError($"KOA_DEMO_ASSETS:PREFAB_SOURCE_MISSING name={prefabName} path={modelPath}");
                return;
            }

            var root = new GameObject(prefabName);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (model == null)
            {
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }
            model.name = "Model";
            model.transform.SetParent(visual.transform, false);

            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                renderer.forceRenderingOff = false;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            Bounds bounds = CalculateBounds(visual);
            float scale = bounds.size.y > 0.001f ? targetHeight / bounds.size.y : 1f;
            visual.transform.localScale = new Vector3(scale * horizontalScale, scale, scale * horizontalScale);
            bounds = CalculateBounds(visual);
            visual.transform.localPosition = Vector3.up * -bounds.min.y;

            if (variantMaterial != null)
            {
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] rendererMaterials = renderer.sharedMaterials;
                    for (int i = 0; i < rendererMaterials.Length; i++)
                    {
                        if (rendererMaterials[i] != null && (rendererMaterials[i].name.Contains("Ranger")
                            || rendererMaterials[i].name.Contains("Peasant")
                            || rendererMaterials[i].name.Contains("Superhero")))
                            rendererMaterials[i] = variantMaterial;
                    }
                    renderer.sharedMaterials = rendererMaterials;
                }
            }

            Animator animator = model.GetComponentInChildren<Animator>(true);
            if (controller != null)
            {
                if (animator == null) animator = model.AddComponent<Animator>();
                Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(item => item.isValid && item.isHuman);
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }

            if (addCollider)
            {
                var collider = root.AddComponent<CapsuleCollider>();
                collider.height = targetHeight;
                collider.radius = Mathf.Clamp(targetHeight * 0.22f, 0.35f, 1.4f);
                collider.center = Vector3.up * targetHeight * 0.5f;
            }

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabRoot}/{prefabName}.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.one);
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
