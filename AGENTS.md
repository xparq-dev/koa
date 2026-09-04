# Project KOA — Context หลัก

โปรเจกต์นี้คือเกม MOBA แบบ 1v1 บน Unity URP (C#)

ก่อนเริ่มงานทุกครั้ง ให้อ่านไฟล์เหล่านี้ก่อนเสมอ:
- docs/Project_KOA_1v1_Complete_Requirement_v1.0.0.md (Requirement ฉบับเต็ม)
- docs/Project_KOA_Production_Roadmap.md (แผนการสร้างรายสัปดาห์)

## กติกาการทำงาน
- ทุกโค้ดต้องยึดสถาปัตยกรรม Decoupled Core (Section 1 ใน Requirement)
  ห้าม mix Simulation Logic กับ Presentation Layer
- Engine: Unity 3D URP, ภาษา C#
- ตอนนี้กำลังอยู่ Phase 3 (ฮีโร่ครบ 4 ตัว + Shop + Balance) ของ Roadmap — เช็คตาราง task ในไฟล์ roadmap ก่อนเริ่มงานทุกครั้ง
- อ้างอิง Section เลขจาก Requirement เสมอเวลาอธิบายงาน (เช่น "ตาม Section 6.1")
