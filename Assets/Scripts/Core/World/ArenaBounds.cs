using UnityEngine;

namespace KOA.Core.World
{
    /// <summary>
    /// ขอบเขตสนามแข่งขันจาก Section 3.1 เก็บไว้ใน Simulation Core
    /// เพื่อให้การเดิน, dash, displacement, rewind และ respawn ใช้กฎเดียวกัน
    /// </summary>
    public static class ArenaBounds
    {
        public const float HalfWidth = 13f;
        public const float HalfLength = 65f;

        public static Vector3 Clamp(Vector3 position, float radius = 0f)
        {
            float safeRadius = Mathf.Max(0f, radius);
            position.x = Mathf.Clamp(position.x, -HalfWidth + safeRadius, HalfWidth - safeRadius);
            position.z = Mathf.Clamp(position.z, -HalfLength + safeRadius, HalfLength - safeRadius);
            return position;
        }

        public static bool Contains(Vector3 position, float radius = 0f)
        {
            float safeRadius = Mathf.Max(0f, radius);
            return position.x >= -HalfWidth + safeRadius
                && position.x <= HalfWidth - safeRadius
                && position.z >= -HalfLength + safeRadius
                && position.z <= HalfLength - safeRadius;
        }

        public static Vector2 ToNormalized(Vector3 position)
        {
            Vector3 clamped = Clamp(position);
            return new Vector2(
                Mathf.InverseLerp(-HalfWidth, HalfWidth, clamped.x),
                Mathf.InverseLerp(-HalfLength, HalfLength, clamped.z));
        }
    }
}
