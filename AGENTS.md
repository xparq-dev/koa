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

## กฎการอ้างอิงและทรัพย์สินทางปัญญา (Inspire MOBA Policy)
- **ห้ามระบุชื่อเกมเชิงพาณิชย์ของค่ายอื่นเด็ดขาด** (เช่น League of Legends, LOL, LoL, Dota, Dota 2, ROV, Arena of Valor, Mobile Legends เป็นต้น) ในโค้ด คอมเมนต์ XML docs, commit message หรือเอกสาร Requirement ต่างๆ เพื่อป้องกันปัญหาลิขสิทธิ์และเครื่องหมายการค้า
- **การอ้างอิงแรงบันดาลใจ:** ให้ใช้คำกลางว่า **"Inspire MOBA"** หรือ **"MOBA Standard"** เท่านั้น
- **สิ่งที่คิดค้นหรือออกแบบเอง:** ให้ใช้ชื่อ **"Project KOA"** หรือชื่อระบบเฉพาะของ KOA ตามปกติ
