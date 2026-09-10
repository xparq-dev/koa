using KOA.Core.Entities;
using KOA.Core.World;
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

        public static int GetBrushIndex(Vector3 position)
        {
            if (IsInsideBrush(position, DuelArenaLayout.BlueBrush)) return 0;
            if (IsInsideBrush(position, DuelArenaLayout.RedBrush)) return 1;
            return -1;
        }

        private static bool IsInsideBrush(Vector3 position, Vector3 center)
        {
            return Mathf.Abs(position.x - center.x) <= DuelArenaLayout.BrushHalfWidth
                && Mathf.Abs(position.z - center.z) <= DuelArenaLayout.BrushHalfLength;
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
