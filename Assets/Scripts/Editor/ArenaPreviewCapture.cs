#if UNITY_EDITOR
using System.IO;
using System.Linq;
using KOA.Core.World;
using KOA.Presentation.Testing;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KOA.Editor
{
    /// <summary>
    /// Renders presentation-only arena review stills in the Editor without producing a player build.
    /// </summary>
    public static class ArenaPreviewCapture
    {
        private const string OutputDirectory = "Logs/arena_preview_2026-09-09";

        public static void CaptureBridgeReview()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 46f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 180f;
            camera.allowHDR = true;
            camera.allowMSAA = true;

            GameObject bootstrapObject = new GameObject("KOA_ArenaPreview");
            VerticalSliceBootstrap bootstrap = bootstrapObject.AddComponent<VerticalSliceBootstrap>();
            bootstrap.BuildFullArena();
            LogStructureRenderers("BlueOuterTower");
            LogStructureRenderers("BlueInnerTower");
            LogStructureRenderers("BlueNexus");

            Directory.CreateDirectory(OutputDirectory);
            CaptureAt(camera, DuelArenaLayout.BlueOuterTower + Vector3.up * 0.7f, Path.Combine(OutputDirectory, "tower_lane.png"));
            CaptureAt(camera, Vector3.up * 0.7f, Path.Combine(OutputDirectory, "waterfall_center.png"));
            CaptureAt(camera, DuelArenaLayout.BlueNexus + Vector3.up * 0.7f, Path.Combine(OutputDirectory, "nexus_valley.png"));

            Debug.Log($"KOA_ARENA_PREVIEW:PASS output={OutputDirectory} count=3");
        }

        private static void LogStructureRenderers(string objectName)
        {
            GameObject structure = GameObject.Find(objectName);
            if (structure == null)
            {
                Debug.LogError($"KOA_ARENA_PREVIEW:STRUCTURE_MISSING name={objectName}");
                return;
            }

            Renderer[] renderers = structure.GetComponentsInChildren<Renderer>(true);
            string details = string.Join(";", renderers.Select(renderer =>
                $"{renderer.name}|active={renderer.gameObject.activeInHierarchy}|enabled={renderer.enabled}|" +
                $"center={renderer.bounds.center}|size={renderer.bounds.size}|material={renderer.sharedMaterial?.name}"));
            Debug.Log($"KOA_ARENA_PREVIEW:STRUCTURE name={objectName} root={structure.transform.position} renderers={renderers.Length} details={details}");
        }

        private static void CaptureAt(Camera camera, Vector3 focusPoint, string path)
        {
            Quaternion rotation = Quaternion.Euler(60f, -45f, 0f);
            camera.transform.SetPositionAndRotation(
                focusPoint + rotation * new Vector3(0f, 0f, -24f),
                rotation);

            var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            image.Apply(false, false);
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(renderTexture);
        }
    }
}
#endif
