namespace KOA.Data.Enums
{
    /// <summary>
    /// Ability Target Type Taxonomy สำหรับ Data-driven skill design ตาม Section 6.5
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary> ยิงเป็นเส้นตรงตามทิศ aimVector (เช่น Iron Cleave, Heavy Bolt) </summary>
        SkillshotLine = 0,

        /// <summary> เลือกพิกัดบนพื้น เกิด effect เป็นวงกลม (เช่น Sacred Hourglass, Rebellion Impact) </summary>
        GroundTargetAoe = 1,

        /// <summary> ล็อกเป้าหมายเดี่ยว (เช่น Magnetic Pull) </summary>
        SingleTarget = 2,

        /// <summary> ใช้กับตัวเองทันที ไม่ต้อง aim (เช่น Vanguard's Will, Hunter's Focus) </summary>
        SelfCast = 3
    }
}
