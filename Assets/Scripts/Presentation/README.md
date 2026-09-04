# Presentation Layer (View / Visuals)

โฟลเดอร์นี้สำหรับเก็บสคริปต์ที่เป็น **Unity MonoBehaviour / Presentation / View Layer** ทั้งหมดตาม **Section 1.1 (Decoupled Core Paradigm)**

## ข้อควรระวัง:
- ห้ามใส่ Game Logic หรือ Damage Calculation ไว้ในโฟลเดอร์นี้
- คลาสในเลเยอร์นี้ทำหน้าที่เป็นตัวแสดงผล (Visuals, Animations, Audio, VFX, World-space UI Billboard)
- เลเยอร์นี้จะคอยฟัง Event จาก `KOA.Core` (เช่น `OnHealthChanged`, `OnLevelUp`, `OnDied`) เพื่ออัปเดตการแสดงผลบนหน้าจอ
