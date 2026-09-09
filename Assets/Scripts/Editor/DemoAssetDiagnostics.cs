#if UNITY_EDITOR
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KOA.Editor
{
    /// <summary>
    /// Read-only diagnostics for imported third-party demo art.
    /// </summary>
    public static class DemoAssetDiagnostics
    {
        [MenuItem("KOA/Assets/Log Imported Demo Asset Summary")]
        public static void LogImportedAssetSummary()
        {
            string[] paths =
            {
                "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Models/Superhero_Male_FullBody.fbx",
                "Assets/ThirdParty/Quaternius/Characters/BaseCharacters/Models/Superhero_Female_FullBody.fbx",
                "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Male_Ranger.fbx",
                "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Female_Ranger.fbx",
                "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Male_Peasant.fbx",
                "Assets/ThirdParty/Quaternius/Characters/FantasyOutfits/Models/Female_Peasant.fbx",
                "Assets/ThirdParty/Quaternius/Monsters/Dungeon/FBX (Unity)/Imp.fbx",
                "Assets/ThirdParty/Quaternius/Monsters/Dungeon/FBX (Unity)/Puglin.fbx",
                "Assets/ThirdParty/Quaternius/Animations/UAL1/UAL1_Standard.fbx",
                "Assets/ThirdParty/Quaternius/Animations/UAL2/UAL2_Standard.fbx"
            };

            foreach (string path in paths)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                {
                    Debug.LogError($"KOA_ASSET_DIAGNOSTIC:MISSING path={path}");
                    continue;
                }

                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                Animator animator = model.GetComponentInChildren<Animator>(true);
                AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                    .Where(clip => !clip.name.StartsWith("__preview__"))
                    .ToArray();
                string materials = string.Join(",", renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Select(material => material.name)
                    .Distinct());
                string clipSample = string.Join(",", clips.Take(12).Select(clip => clip.name));

                Debug.Log(
                    $"KOA_ASSET_DIAGNOSTIC:OK path={path} renderers={renderers.Length} " +
                    $"animator={(animator != null)} avatar={(animator != null && animator.avatar != null)} " +
                    $"clips={clips.Length} materials=[{materials}] clipSample=[{clipSample}]");

                if (path.Contains("/Animations/"))
                    Debug.Log($"KOA_ASSET_CLIPS:path={path} names=[{string.Join(",", clips.Select(clip => clip.name))}]");
            }

            ValidateGeneratedPrefabs();
            ValidateArenaMaterials();
        }

        private static void ValidateGeneratedPrefabs()
        {
            string[] animatedPrefabs =
            {
                "Hero_Vorkas", "Hero_Zenthis", "Hero_Korvax", "Hero_Gravitor",
                "Minion_Melee", "Minion_Ranged", "Minion_Cannon", "Minion_Super"
            };
            string[] staticPrefabs =
            {
                "Tower_Outer", "Tower_Inner", "Nexus", "Fountain",
                "Environment_Bush", "Environment_Tree", "Environment_Pine", "Environment_Rock",
                "Environment_CliffRock", "Environment_TwistedTree", "Environment_DeadTree",
                "Environment_Grass", "Environment_Fern", "Environment_Mushroom"
            };
            var expectedControllers = new Dictionary<string, string>
            {
                ["Hero_Vorkas"] = "KOA_HeroMelee",
                ["Hero_Zenthis"] = "KOA_HeroCaster",
                ["Hero_Korvax"] = "KOA_HeroRanged",
                ["Hero_Gravitor"] = "KOA_Monster",
                ["Minion_Melee"] = "KOA_Monster",
                ["Minion_Ranged"] = "KOA_Monster",
                ["Minion_Cannon"] = "KOA_Monster",
                ["Minion_Super"] = "KOA_Monster"
            };

            int validCount = 0;
            foreach (string prefabName in animatedPrefabs.Concat(staticPrefabs))
            {
                string path = $"Assets/Resources/KOA/Demo/{prefabName}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Renderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                Animator animator = prefab != null ? prefab.GetComponentInChildren<Animator>(true) : null;
                bool needsAnimator = animatedPrefabs.Contains(prefabName);
                Material[] prefabMaterials = renderers
                    .SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .Distinct()
                    .ToArray();
                bool materialsValid = renderers.Length > 0
                    && prefabMaterials.Length > 0
                    && prefabMaterials.All(material => material.shader != null
                        && material.shader.name != "Hidden/InternalErrorShader");
                bool correctController = !needsAnimator || (animator != null
                    && animator.runtimeAnimatorController != null
                    && animator.runtimeAnimatorController.name == expectedControllers[prefabName]);
                bool completeController = !needsAnimator || HasRequiredAnimationMotions(animator);
                bool valid = prefab != null
                    && renderers.Length > 0
                    && materialsValid
                    && (!needsAnimator || (animator != null
                        && animator.avatar != null
                        && animator.avatar.isValid
                        && animator.runtimeAnimatorController != null
                        && correctController
                        && completeController));

                if (!valid)
                {
                    Debug.LogError(
                        $"KOA_DEMO_PREFAB_VALIDATION:FAIL name={prefabName} renderers={renderers.Length} " +
                        $"materialsValid={materialsValid} animator={(animator != null)} " +
                        $"correctController={correctController} completeController={completeController}");
                    continue;
                }

                validCount++;
                Debug.Log(
                    $"KOA_DEMO_PREFAB_VALIDATION:OK name={prefabName} renderers={renderers.Length} animated={needsAnimator} " +
                    $"materialsValid={materialsValid} materials=[{string.Join(",", prefabMaterials.Select(material => material.name))}] " +
                    $"controller={(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "none")}");
            }

            int expectedCount = animatedPrefabs.Length + staticPrefabs.Length;
            if (validCount == expectedCount)
                Debug.Log($"KOA_DEMO_PREFAB_VALIDATION:PASS count={validCount}/{expectedCount}");
            else
                Debug.LogError($"KOA_DEMO_PREFAB_VALIDATION:FAIL count={validCount}/{expectedCount}");
        }

        private static bool HasRequiredAnimationMotions(Animator animator)
        {
            if (animator == null || !(animator.runtimeAnimatorController is AnimatorController controller)) return false;
            string[] requiredStates = { "Idle", "Move", "Attack", "Cast", "Death" };
            string[] statesWithMotion = controller.layers
                .SelectMany(layer => layer.stateMachine.states)
                .Where(child => child.state.motion != null)
                .Select(child => child.state.name)
                .ToArray();
            return requiredStates.All(statesWithMotion.Contains);
        }

        private static void ValidateArenaMaterials()
        {
            string[] materialNames =
            {
                "KOA_Bridge_Grass",
                "KOA_Bridge_Dirt",
                "KOA_Bridge_Cliff",
                "KOA_Lane_Stone",
                "KOA_Chasm_Void"
            };
            int validCount = 0;
            foreach (string materialName in materialNames)
            {
                string path = $"Assets/Resources/KOA/Demo/Materials/{materialName}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                bool valid = material != null
                    && material.shader != null
                    && material.shader.name != "Hidden/InternalErrorShader"
                    && (materialName == "KOA_Chasm_Void" || material.GetTexture("_BaseMap") != null);
                if (valid)
                {
                    validCount++;
                    Debug.Log($"KOA_ARENA_MATERIAL_VALIDATION:OK name={materialName} shader={material.shader.name}");
                }
                else
                {
                    Debug.LogError($"KOA_ARENA_MATERIAL_VALIDATION:FAIL name={materialName} path={path}");
                }
            }

            if (validCount == materialNames.Length)
                Debug.Log($"KOA_ARENA_MATERIAL_VALIDATION:PASS count={validCount}/{materialNames.Length}");
            else
                Debug.LogError($"KOA_ARENA_MATERIAL_VALIDATION:FAIL count={validCount}/{materialNames.Length}");
        }
    }
}
#endif
