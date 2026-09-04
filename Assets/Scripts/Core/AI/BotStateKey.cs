namespace KOA.Core.AI
{
    /// <summary>
    /// สถานะ Finite State Machine สำหรับ AI Bot ตาม Section 9
    /// </summary>
    public enum BotStateKey
    {
        Idle = 0,
        LanePushing = 1,
        AttackingHero = 2,
        Retreating = 3
    }
}
