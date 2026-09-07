using KOA.Core.Entities;
using UnityEngine;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// วงแหวนแสดงระยะโจมตีใต้เท้าตัวละคร (Attack Range Indicator) และวงแหวนแสดงเป้าหมายที่เลือก (Selection Ring) ตาม Section 7.2
    /// </summary>
    public class RangeIndicatorView : MonoBehaviour
    {
        private LineRenderer _rangeCircleRenderer;
        private LineRenderer _selectionCircleRenderer;
        private HeroBase3D _hero;
        private ITargetable _currentTarget;

        private const int Segments = 64;

        public void BindHero(HeroBase3D hero)
        {
            _hero = hero;
            if (_rangeCircleRenderer == null)
            {
                InitRangeCircle();
            }
            UpdateRangeCircleRadius();
        }

        public void SetSelectedTarget(ITargetable target)
        {
            _currentTarget = target;
            if (_selectionCircleRenderer == null)
            {
                InitSelectionCircle();
            }

            if (_selectionCircleRenderer != null)
            {
                bool isValid = (_currentTarget != null && _currentTarget.IsAlive);
                _selectionCircleRenderer.enabled = isValid;
                if (isValid)
                {
                    ApplySelectionColor();
                    UpdateCirclePoints(_selectionCircleRenderer, _currentTarget.Position, _currentTarget.Radius + 0.35f, 0.07f);
                }
            }
        }

        private void Awake()
        {
            if (_rangeCircleRenderer == null) InitRangeCircle();
            if (_selectionCircleRenderer == null) InitSelectionCircle();
        }

        private void InitRangeCircle()
        {
            GameObject rangeGo = new GameObject("AttackRangeIndicator");
            rangeGo.transform.SetParent(transform, false);
            _rangeCircleRenderer = rangeGo.AddComponent<LineRenderer>();
            _rangeCircleRenderer.useWorldSpace = true;
            _rangeCircleRenderer.loop = true;
            _rangeCircleRenderer.positionCount = Segments;
            _rangeCircleRenderer.startWidth = 0.10f;
            _rangeCircleRenderer.endWidth = 0.10f;
            
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _rangeCircleRenderer.material = new Material(shader);
            
            // สีฟ้าอ่อน Cyan นุ่มนวล แสดงขอบเขตการโจมตี
            Color cyanColor = new Color(0.2f, 0.85f, 1f, 0.45f);
            _rangeCircleRenderer.startColor = cyanColor;
            _rangeCircleRenderer.endColor = cyanColor;
        }

        private void InitSelectionCircle()
        {
            GameObject selGo = new GameObject("TargetSelectionRing");
            selGo.transform.SetParent(transform, false);
            _selectionCircleRenderer = selGo.AddComponent<LineRenderer>();
            _selectionCircleRenderer.useWorldSpace = true;
            _selectionCircleRenderer.loop = true;
            _selectionCircleRenderer.positionCount = Segments;
            _selectionCircleRenderer.startWidth = 0.14f;
            _selectionCircleRenderer.endWidth = 0.14f;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            _selectionCircleRenderer.material = new Material(shader);

            _selectionCircleRenderer.enabled = false;
        }

        private void ApplySelectionColor()
        {
            if (_currentTarget == null || _selectionCircleRenderer == null) return;

            bool isEnemy = (_hero == null || _currentTarget.TeamId != _hero.TeamId);
            Color selColor = isEnemy 
                ? new Color(1.0f, 0.22f, 0.15f, 0.95f) // สีส้มแดงสำหรับศัตรู
                : new Color(0.2f, 1.0f, 0.45f, 0.95f); // สีเขียวสำหรับพวกเดียวกัน

            _selectionCircleRenderer.startColor = selColor;
            _selectionCircleRenderer.endColor = selColor;
        }

        private void LateUpdate()
        {
            // 1. อัปเดตวงแหวนระยะโจมตีรอบตัว Hero
            if (_hero != null && _rangeCircleRenderer != null)
            {
                _rangeCircleRenderer.enabled = _hero.IsAlive;
                if (_hero.IsAlive)
                {
                    UpdateCirclePoints(_rangeCircleRenderer, _hero.Position, _hero.AttackRange, 0.04f);
                }
            }

            // 2. อัปเดตวงแหวนเป้าหมายที่ถูกเลือก
            if (_currentTarget != null && _selectionCircleRenderer != null)
            {
                if (_currentTarget.IsAlive)
                {
                    _selectionCircleRenderer.enabled = true;
                    ApplySelectionColor();
                    UpdateCirclePoints(_selectionCircleRenderer, _currentTarget.Position, _currentTarget.Radius + 0.35f, 0.07f);
                }
                else
                {
                    _selectionCircleRenderer.enabled = false;
                    _currentTarget = null;
                }
            }
            else if (_selectionCircleRenderer != null)
            {
                _selectionCircleRenderer.enabled = false;
            }
        }

        public void UpdateRangeCircleRadius()
        {
            if (_hero != null && _rangeCircleRenderer != null)
            {
                UpdateCirclePoints(_rangeCircleRenderer, _hero.Position, _hero.AttackRange, 0.04f);
            }
        }

        private void UpdateCirclePoints(LineRenderer lr, Vector3 center, float radius, float heightOffset)
        {
            // ฉายตำแหน่งความสูงลงสู่พื้นผิว (Ground level) อย่างแม่นยำ
            float groundY = heightOffset;
            if (Physics.Raycast(new Vector3(center.x, center.y + 1.5f, center.z), Vector3.down, out RaycastHit hit, 10f))
            {
                groundY = hit.point.y + heightOffset;
            }

            float angleStep = 360f / Segments;
            for (int i = 0; i < Segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                lr.SetPosition(i, new Vector3(center.x + x, groundY, center.z + z));
            }
        }
    }
}
