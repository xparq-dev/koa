using UnityEngine;
using UnityEngine.Rendering;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation-only feedback for discrete right-click commands (Section 7.2).
    /// It never changes Simulation state or movement destinations.
    /// </summary>
    public sealed class MoveCommandIndicatorView : MonoBehaviour
    {
        [SerializeField] private float visibleDuration = 0.42f;
        [SerializeField] private float baseRadius = 0.52f;
        [SerializeField] private Color moveColor = new Color(0.18f, 0.92f, 1f, 0.92f);
        [SerializeField] private Color attackColor = new Color(1f, 0.22f, 0.12f, 0.95f);

        private GameObject _markerRoot;
        private LineRenderer _ring;
        private LineRenderer _diamond;
        private Material _material;
        private float _remaining;
        private Color _activeColor;

        public void Show(Vector3 worldPosition, bool hostileCommand)
        {
            EnsureVisuals();
            _activeColor = hostileCommand ? attackColor : moveColor;
            _remaining = visibleDuration;
            _markerRoot.transform.position = new Vector3(worldPosition.x, 0.22f, worldPosition.z);
            _markerRoot.transform.localScale = Vector3.one * 1.22f;
            _markerRoot.SetActive(true);
            ApplyColor(_activeColor);
        }

        private void EnsureVisuals()
        {
            if (_markerRoot != null) return;

            _markerRoot = new GameObject("MoveCommand_Marker");
            _markerRoot.SetActive(false);

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            _material = new Material(shader);
            ConfigureTransparentMaterial(_material);

            _ring = CreateLine("Ring", 32, true, 0.075f);
            for (int i = 0; i < 32; i++)
            {
                float angle = i / 32f * Mathf.PI * 2f;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * baseRadius, 0f, Mathf.Sin(angle) * baseRadius));
            }

            _diamond = CreateLine("DirectionGlyph", 5, false, 0.07f);
            float glyph = baseRadius * 0.52f;
            _diamond.SetPositions(new[]
            {
                new Vector3(0f, 0.012f, glyph),
                new Vector3(glyph, 0.012f, 0f),
                new Vector3(0f, 0.012f, -glyph),
                new Vector3(-glyph, 0.012f, 0f),
                new Vector3(0f, 0.012f, glyph)
            });
        }

        private LineRenderer CreateLine(string name, int pointCount, bool loop, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(_markerRoot.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.loop = loop;
            line.positionCount = pointCount;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 3;
            line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Update()
        {
            if (_markerRoot == null || !_markerRoot.activeSelf) return;

            _remaining -= Time.deltaTime;
            if (_remaining <= 0f)
            {
                _markerRoot.SetActive(false);
                return;
            }

            float normalized = Mathf.Clamp01(_remaining / visibleDuration);
            float scale = Mathf.Lerp(0.78f, 1.08f, normalized);
            _markerRoot.transform.localScale = Vector3.one * scale;
            Color faded = _activeColor;
            faded.a *= normalized;
            ApplyColor(faded);
        }

        private void ApplyColor(Color color)
        {
            if (_material != null)
            {
                if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", color);
                else _material.color = color;
            }
            if (_ring != null)
            {
                _ring.startColor = color;
                _ring.endColor = color;
            }
            if (_diamond != null)
            {
                _diamond.startColor = color;
                _diamond.endColor = color;
            }
        }

        private static void ConfigureTransparentMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetOverrideTag("Queue", "Transparent");
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private void OnDestroy()
        {
            if (_markerRoot != null) Destroy(_markerRoot);
            if (_material != null) Destroy(_material);
        }
    }
}
