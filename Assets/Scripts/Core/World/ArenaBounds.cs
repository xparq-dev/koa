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

    /// <summary>
    /// Authoritative gameplay anchors for the 1v1 lane (Section 3.1).
    /// Blue anchors are authored once and Red anchors are derived by point symmetry,
    /// preventing either team from receiving a distance advantage.
    /// </summary>
    public static class DuelArenaLayout
    {
        public const float OuterTowerZ = 12f;
        public const float InnerTowerZ = 25f;
        public const float NexusZ = 40f;
        public const float FountainZ = 57f;

        public const float BrushHalfWidth = 1.6f;
        public const float BrushHalfLength = 3.0f;

        public static readonly Vector3 BlueOuterTower = new Vector3(0f, 0f, -OuterTowerZ);
        public static readonly Vector3 BlueInnerTower = new Vector3(0f, 0f, -InnerTowerZ);
        public static readonly Vector3 BlueNexus = new Vector3(0f, 0f, -NexusZ);
        public static readonly Vector3 BlueFountain = new Vector3(0f, 0f, -FountainZ);
        public static readonly Vector3 BlueBrush = new Vector3(-8f, 0f, -3.5f);

        public static readonly Vector3 RedOuterTower = MirrorPoint(BlueOuterTower);
        public static readonly Vector3 RedInnerTower = MirrorPoint(BlueInnerTower);
        public static readonly Vector3 RedNexus = MirrorPoint(BlueNexus);
        public static readonly Vector3 RedFountain = MirrorPoint(BlueFountain);
        public static readonly Vector3 RedBrush = MirrorPoint(BlueBrush);

        public static Vector3 MirrorPoint(Vector3 position)
        {
            return new Vector3(-position.x, position.y, -position.z);
        }
    }
}
