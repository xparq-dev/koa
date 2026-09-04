using KOA.Data.Enums;
using UnityEngine;

namespace KOA.Core.Input
{
    /// <summary>
    /// ข้อมูล Input สรุปต่อเฟรมที่ส่งเข้า Simulation Core (Section 1.1)
    /// </summary>
    public struct InputFrame
    {
        /// <summary> เวกเตอร์การเคลื่อนที่ normalized (-1..1, -1..1) </summary>
        public Vector2 MoveVector;

        /// <summary> สัญญาณการกดโจมตีหรือใช้สกิล </summary>
        public CastIntent CastIntent;

        /// <summary> ตำแหน่งเป้าหมายในโลก 3 มิติ (World-space position) สำหรับการเล็ง </summary>
        public Vector3 AimVector;

        /// <summary> ธงระบุว่ามีคำสั่งเคลื่อนที่ในเฟรมนี้หรือไม่ (เช่น สำหรับ click-to-move) </summary>
        public bool HasMoveTarget;

        /// <summary> จุดหมายปลายทางในโลก 3 มิติสำหรับการคลิกเดิน (Section 7.2) </summary>
        public Vector3 TargetDestination;
    }

    /// <summary>
    /// Input Abstraction Layer Interface ตาม Section 1.1 และ Section 7
    /// Simulation Core จะไม่รับรู้ว่าอินพุตมาจาก PC หรือ Mobile
    /// </summary>
    public interface IInputAdapter
    {
        /// <summary>
        /// อ่าน Input ล่าสุดที่แปลงแล้วสำหรับ Tick ถัดไปของ Simulation Core
        /// </summary>
        InputFrame GetCurrentInput();

        /// <summary>
        /// เคลียร์ Intent หลังจาก Simulation Core นำไปประมวลผลแล้ว
        /// </summary>
        void ConsumeIntent();
    }
}
