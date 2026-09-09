using KOA.Core.Entities;
using UnityEngine;

namespace KOA.Core.Vision
{
    /// <summary>
    /// กฎ Vision/Brush ของสนาม 1v1 ตาม Section 3.1 และ Section 8
    /// เก็บกติกาไว้ใน Core โดยไม่อ้างอิง GameObject หรือ Renderer
    /// </summary>
    public static class VisionSystem
    {
        public const float HeroVisionRange = 12.0f;

        private const float BrushCenterX = 8.0f;
        private const float BrushHalfWidth = 1.6f;
        private const float BrushHalfLength = 4.5f;

        public static int GetBrushIndex(Vector3 position)
        {
            if (Mathf.Abs(position.z) > BrushHalfLength) return -1;
            if (Mathf.Abs(position.x + BrushCenterX) <= BrushHalfWidth) return 0;
            if (Mathf.Abs(position.x - BrushCenterX) <= BrushHalfWidth) return 1;
            return -1;
        }

        public static bool IsHeroVisible(HeroBase3D observer, HeroBase3D target)
        {
            if (observer == null || target == null || !target.IsAlive) return false;
            if (ReferenceEquals(observer, target) || observer.TeamId == target.TeamId) return true;
            if (Vector3.Distance(observer.Position, target.Position) > HeroVisionRange) return false;

            int targetBrush = GetBrushIndex(target.Position);
            return targetBrush < 0 || GetBrushIndex(observer.Position) == targetBrush;
        }
    }
}
