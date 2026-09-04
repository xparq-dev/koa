# Project KOA (Kronos Origin Area) ⚔️
### 1v1 Single-Lane Duel Arena MOBA (Unity 3D URP / C#)

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B%20%7C%20URP-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Decoupled%20Core-blue.svg)](docs/Project_KOA_1v1_Complete_Requirement_v1.0.0.md)
[![Status](https://img.shields.io/badge/Roadmap-Phase%203%20Complete-success.svg)](docs/Project_KOA_Production_Roadmap.md)

**Project KOA** คือเกมแนว MOBA แบบ 1v1 บนแผนที่เลนเดี่ยว (Duel Arena) พัฒนาด้วย **Unity 3D (Universal Render Pipeline)** และภาษา C# โดยยึดหลักสถาปัตยกรรม **Decoupled Core Paradigm** แยกขาดระหว่าง Simulation Logic (รันที่ 30 Ticks/sec) ออกจาก Presentation/View Layer อย่างสิ้นเชิง

---

## 🌟 ฟีเจอร์หลัก (Key Features)

### 1. 🧙 ฮีโร่ครบ 4 ตัว (4 Distinct Heroes)
- **Vorkas (The Iron Vanguard):** Melee Juggernaut ถึกทน คุมสนามด้วยคลื่นดาบ *Iron Cleave*
- **Zenthis (The Chrono Guardian):** Ranged Time Mage สตันหมู่ด้วย *Sacred Hourglass* และย้อนเวลาด้วย *Grand Rewind*
- **Korvax (The Ballista Sniper):** Marksman ระยะไกล ยิงทะลวงเกราะด้วย *Heavy Bolt* และมหาศร *Ballista Overdrive*
- **Gravitor (The Singularity):** Disrupter Tank ดึงศัตรูด้วย *Magnetic Pull* ผลักออกด้วย *Repulsion Zone* และดูดด้วยหลุมดำ *Gravity Kore Collapse*

### 2. 🏪 ระบบร้านค้า & ไอเทม (Shop & 8 Starting Items)
- กระเป๋า 6 ช่อง (Inventory Slots)
- ไอเทมสำเร็จรูป 8 ชนิดตาม Section 5.2 (เช่น Warblade Fang, Kinetic Boots, Gravity Anchor Capstone)
- ไอเทมประเภทกดใช้ **Nullifying Cloak (Active)** ล้างสถานะ Debuff และกัน CC ชั่วคราว

### 3. 🤖 ระบบ Bot AI 3 ระดับความยาก (Multi-Tier AI)
- **Easy:** เหมาะสำหรับผู้เริ่มต้น ตอบสนองช้า
- **Medium:** ดันเลน โจมตีตามระยะ และถอยกลับบ้านเมื่อเลือดต่ำกว่า 30%
- **Hard:** มี **Prediction Algorithm** คำนวณความเร็วเป้าหมายเพื่อยิงสกิลดักหน้า และออกคอมโบอย่างดุดัน

### 4. 🏰 แผนที่เลนเดี่ยว, ป้อมปราการ และมินเนี่ยน (Map Structures & Minions)
- ป้อมปราการ 2 Tier + Nexus Core
- กลไก **Heating Laser** (ยิ่งยิงเป้าหมายเดิมดาเมจยิ่งเพิ่มขึ้น)
- กลไก **Tower Plating** (เกราะหนาพิเศษใน 4 นาทีแรก)
- ครีปเวฟ (2 Melee + 1 Ranged) เกิดทุก 25 วินาที พร้อมสูตร **Evolution Scaling** ทุกๆ 3 นาที

---

## 🕹️ การควบคุม (Controls)

| คำสั่ง | คีย์บอร์ด / เมาส์ (PC) |
|---|---|
| เคลื่อนที่ (Move) | **คลิกขวา (Right-Click)** บนพื้นระนาบสนาม |
| โจมตีปกติ (Basic Attack) | **คลิกซ้าย (Left-Click)** หรือกดปุ่ม **A** |
| Skill 1 | ปุ่ม **Q** (เล็งตามตำแหน่งเมาส์) |
| Skill 2 | ปุ่ม **W** |
| Ultimate | ปุ่ม **E** |
| กดใช้ Active Items | ปุ่มตัวเลข **1 - 6** ตามช่องกระเป๋า |
| เปิด / ปิด ร้านค้า (Shop) | ปุ่ม **P** หรือคลิกปุ่มบนหน้าจอ |

---

## 🚀 วิธีการเปิดทดสอบใน Unity (Getting Started)

1. เปิดโปรเจกต์นี้ด้วย **Unity Hub** (แนะนำ Unity 2022.3 LTS หรือใหม่กว่า พร้อม URP Template)
2. เปิด Scene เปล่า (New Scene)
3. สร้าง GameObject เปล่าขึ้นมา 1 ตัวใน Hierarchy (เช่น ตั้งชื่อว่า `MatchRunner`)
4. แปะสคริปต์ `VerticalSliceBootstrap` ลงใน GameObject นั้น
5. กดปุ่ม **Play** ใน Unity Editor:
   - ระบบจะสร้างสนาม 70x20m, แสง, ป้อมปราการ 2 ฝั่ง, ตัวละครผู้เล่น, บอท, ครีป, กล้อง และ HUD ให้โดยอัตโนมัติ พร้อมเล่นได้ทันที!

---

## 📁 โครงสร้างโปรเจกต์ (Decoupled Core Structure)

```text
Assets/Scripts/
├── Core/             # Simulation Core (Pure C# Logic 30 Ticks/sec ไม่ขึ้นกับ MonoBehaviour)
│   ├── AI/           # Multi-tier Bot Brain & FSM States
│   ├── Economy/      # Player Wallet & Gold Systems
│   ├── Entities/     # HeroBase3D, Vorkas, Zenthis, Korvax, Gravitor, DummyTarget
│   ├── FSM/          # Generic Finite State Machine
│   ├── Input/        # IInputAdapter (Input Abstraction Layer)
│   ├── Items/        # Inventory & ShopSystem
│   ├── Match/        # MatchSimulation & Rules
│   ├── Minions/      # MinionEntity & CreepSpawner
│   └── Structures/   # TowerEntity & Mechanics
├── Data/             # Enums, Data Models, Item Schemas, Structure Stats
└── Presentation/     # Unity MonoBehaviour, Camera, Views, UI & Auto Test Bootstrapper
```

---

## 📜 เอกสารอ้างอิง
- [Project Requirement v1.0.0](docs/Project_KOA_1v1_Complete_Requirement_v1.0.0.md)
- [Production Roadmap](docs/Project_KOA_Production_Roadmap.md)
- [Agent Guidelines](AGENTS.md)
