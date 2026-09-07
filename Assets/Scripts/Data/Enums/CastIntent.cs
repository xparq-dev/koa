namespace KOA.Data.Enums
{
    /// <summary>
    /// สัญญาณ Intent การร่ายสกิลหรือการโจมตี จาก Input Abstraction Layer (Section 1.1)
    /// </summary>
    public enum CastIntent
    {
        None = 0,
        CastAttack = 1,
        CastSkill1 = 2,
        CastSkill2 = 3,
        CastSkill3 = 4,
        CastUltimate = 5
    }
}
