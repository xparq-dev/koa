using KOA.Data.Enums;
using UnityEngine;

namespace KOA.Core.Entities
{
    /// <summary>
    /// Interface กลางสำหรับ Unit ทุกชนิดในสนามที่สามารถถูกเลือกเป็นเป้าหมายและรับความเสียหายได้ (Decoupled Core Section 1.1)
    /// รองรับทั้ง Heroes, Minions, Towers, Nexus, และ Dummy Target
    /// </summary>
    public interface ITargetable
    {
        string TargetId { get; }
        Vector3 Position { get; }
        bool IsAlive { get; }
        float CurrentHp { get; }
        float MaxHp { get; }
        int TeamId { get; } // 0 = Blue, 1 = Red, -1 = Neutral
        float Radius { get; }
        void TakeDamage(float rawDamage, DamageType damageType, string attackerId = null);
    }
}
