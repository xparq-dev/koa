using KOA.Core.Entities;
using KOA.Presentation.Views;
using UnityEngine;

namespace KOA.Presentation.UI
{
    /// <summary>Renders a presentation-only clone for the F1 hero profile (Section 8).</summary>
    public sealed class HeroPortraitPreview : MonoBehaviour
    {
        private const int PreviewLayer = 30;
        private readonly Vector3 _previewOrigin = new Vector3(0f, -500f, 0f);
        private HeroBase3D _boundHero;
        private GameObject _previewRoot;
        private UnityEngine.Camera _previewCamera;
        private RenderTexture _renderTexture;
        private int _lastRenderedFrame = -1;

        public Texture GetTexture(HeroBase3D hero)
        {
            if (hero == null) return null;
            if (_boundHero != hero || _previewRoot == null) Rebuild(hero);
            if (_previewCamera != null && _lastRenderedFrame != Time.frameCount)
            {
                _previewCamera.Render();
                _lastRenderedFrame = Time.frameCount;
            }
            return _renderTexture;
        }

        private void Rebuild(HeroBase3D hero)
        {
            ClearPreview();
            _boundHero = hero;

            HeroView sourceView = null;
            HeroView[] views = FindObjectsByType<HeroView>();
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].Hero == hero)
                {
                    sourceView = views[i];
                    break;
                }
            }

            Animator sourceAnimator = sourceView != null ? sourceView.GetComponentInChildren<Animator>(true) : null;
            if (sourceAnimator == null) return;

            _previewRoot = new GameObject("KOA_HeroPortraitPreview") { hideFlags = HideFlags.HideAndDontSave };
            GameObject model = Instantiate(sourceAnimator.gameObject, _previewRoot.transform);
            model.name = "PortraitModel";
            model.transform.position = _previewOrigin;
            model.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            SetLayerRecursively(model, PreviewLayer);

            MonoBehaviour[] scripts = model.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < scripts.Length; i++) scripts[i].enabled = false;
            Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;
            Animator previewAnimator = model.GetComponentInChildren<Animator>(true);
            if (previewAnimator != null)
            {
                previewAnimator.Rebind();
                previewAnimator.Update(0.016f);
            }

            Bounds bounds = CalculateBounds(model);
            float height = Mathf.Max(1f, bounds.size.y);

            _renderTexture = new RenderTexture(440, 560, 24, RenderTextureFormat.ARGB32)
            {
                name = "KOA_HeroPortrait_RT",
                antiAliasing = 4,
                hideFlags = HideFlags.HideAndDontSave
            };
            _renderTexture.Create();

            GameObject cameraObject = new GameObject("PortraitCamera") { hideFlags = HideFlags.HideAndDontSave };
            cameraObject.transform.SetParent(_previewRoot.transform, false);
            _previewCamera = cameraObject.AddComponent<UnityEngine.Camera>();
            _previewCamera.enabled = false;
            _previewCamera.clearFlags = CameraClearFlags.SolidColor;
            _previewCamera.backgroundColor = new Color(0.025f, 0.04f, 0.06f, 0f);
            _previewCamera.cullingMask = 1 << PreviewLayer;
            _previewCamera.fieldOfView = 26f;
            _previewCamera.nearClipPlane = 0.05f;
            _previewCamera.farClipPlane = 50f;
            _previewCamera.targetTexture = _renderTexture;
            Vector3 lookPoint = bounds.center + Vector3.up * height * 0.04f;
            cameraObject.transform.position = lookPoint + new Vector3(0f, height * 0.05f, height * 2.15f);
            cameraObject.transform.LookAt(lookPoint);

            GameObject lightObject = new GameObject("PortraitKeyLight") { hideFlags = HideFlags.HideAndDontSave };
            lightObject.transform.SetParent(_previewRoot.transform, false);
            lightObject.transform.position = bounds.center + new Vector3(-2f, height * 1.2f, height * 1.5f);
            lightObject.transform.rotation = Quaternion.Euler(28f, -28f, 0f);
            Light keyLight = lightObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.intensity = 1.35f;
            keyLight.color = new Color(0.78f, 0.9f, 1f);
            keyLight.cullingMask = 1 << PreviewLayer;

            GameObject fillObject = new GameObject("PortraitFillLight") { hideFlags = HideFlags.HideAndDontSave };
            fillObject.transform.SetParent(_previewRoot.transform, false);
            Light fillLight = fillObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.range = height * 4f;
            fillLight.intensity = 1.2f;
            fillLight.color = new Color(1f, 0.58f, 0.3f);
            fillLight.cullingMask = 1 << PreviewLayer;
            fillObject.transform.position = bounds.center + new Vector3(1.5f, 0.3f, 1.2f) * height;
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Bounds bounds = new Bounds(root.transform.position, Vector3.one);
            bool initialized = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!initialized)
                {
                    bounds = renderers[i].bounds;
                    initialized = true;
                }
                else bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        private static void SetLayerRecursively(GameObject owner, int layer)
        {
            owner.layer = layer;
            foreach (Transform child in owner.transform) SetLayerRecursively(child.gameObject, layer);
        }

        private void OnDestroy() => ClearPreview();

        private void ClearPreview()
        {
            if (_previewCamera != null) _previewCamera.targetTexture = null;
            if (_previewRoot != null) Destroy(_previewRoot);
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
            }
            _previewRoot = null;
            _previewCamera = null;
            _renderTexture = null;
            _boundHero = null;
            _lastRenderedFrame = -1;
        }
    }
}
