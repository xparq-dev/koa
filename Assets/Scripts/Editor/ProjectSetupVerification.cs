#if UNITY_EDITOR
using System.IO;
using KOA.Presentation.Testing;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace KOA.Editor
{
    /// <summary>
    /// สร้าง Scene และ Windows verification build แบบทำซ้ำได้สำหรับ Phase 4
    /// </summary>
    public static class ProjectSetupVerification
    {
        public const string PlayableScenePath = "Assets/Scenes/DuelArena.unity";
        public const string WindowsBuildPath = "Builds/Windows/KOA.exe";
        public const string RendererDataPath = "Assets/Settings/KOA_UniversalRenderer.asset";
        public const string PipelineAssetPath = "Assets/Settings/KOA_UniversalRenderPipeline.asset";

        [MenuItem("KOA/Ensure Playable Scene")]
        public static void EnsurePlayableScene()
        {
            EnsureUniversalRenderPipeline();
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var runner = new GameObject("[KOA_MatchRunner]");
            runner.AddComponent<VerticalSliceBootstrap>();

            if (!EditorSceneManager.SaveScene(scene, PlayableScenePath))
                throw new System.Exception($"Unable to save playable scene at {PlayableScenePath}");

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(PlayableScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[KOA Project Setup] Playable scene ready: {PlayableScenePath}");
        }

        private static void EnsureUniversalRenderPipeline()
        {
            Directory.CreateDirectory("Assets/Settings");
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererDataPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
            }

            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipelineAsset, PipelineAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;
        }

        [MenuItem("KOA/Build Windows Verification Player")]
        public static void BuildWindowsVerificationPlayer()
        {
            EnsurePlayableScene();
            Directory.CreateDirectory(Path.GetDirectoryName(WindowsBuildPath));

            var options = new BuildPlayerOptions
            {
                scenes = new[] { PlayableScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new System.Exception($"Windows verification build failed: {report.summary.result}");

            Debug.Log($"[KOA Build] Windows verification build succeeded: {report.summary.totalSize} bytes");
        }
    }
}
#endif
