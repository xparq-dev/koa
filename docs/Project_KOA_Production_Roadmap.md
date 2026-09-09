# Production Roadmap — Project KOA (Kronos Origin Area)
## เส้นทางจาก 0 ถึง Version 1.0.0 (PC-only) — ฉบับปรับปรุงตาม Scope ใหม่

**บริบท:** 1 คน, Full-time, มือใหม่ Unity/C#, ใช้ AI (Claude Code / Unity AI Assistant) ช่วยเต็มที่
**อ้างอิง:** `Project_KOA_1v1_Complete_Requirement_v1.0.0.md` (รวม Section 6.6-6.7 ที่เพิ่มใหม่)

## ⚠️ สิ่งที่เปลี่ยนจาก Roadmap ฉบับก่อนหน้า

Scope ของเกมขยายขึ้นจริงจากที่วางแผนไว้ตอนแรก:

| รายการ | เดิม | ใหม่ |
|---|---|---|
| จำนวนสกิลต่อฮีโร่ | 2 Active + Ultimate | **3 Active + Ultimate** (16 → 20 ความสามารถรวม) |
| ระบบ Talent Tree | ไม่มี | **มี** (เลือกที่ Level 4/8/12) |
| ระบบ Attribute Bonus | ไม่มี | **มี** (ลงแต้มทุก Level Up) |
| Tower Escalation | ไม่มี | **มี** (Cannon Minion / Super Creep) |
| แผนที่ | ~70m x 20m ประมาณการ | **130m x 26m พิกัดแน่นอน** (รายละเอียดเพิ่ม ไม่กระทบเวลามาก) |

**ผลกระทบต่อ Timeline:** จากเดิม ~32 สัปดาห์ (~8 เดือน) เพิ่มเป็น **~47 สัปดาห์ (~11-12 เดือน)** — เพิ่มขึ้น ~47% ตรงตามสัดส่วนงานที่เพิ่มจริง ไม่ใช่ตัวเลขเดา

---

## หลักการทำงานร่วมกับ AI (คงเดิมจากฉบับก่อน)

**ใช้ AI เต็มที่กับ:** เขียน Boilerplate/Scaffold, อธิบาย Error, Review โค้ด, แปลงสูตรจาก Requirement เป็นโค้ด
**ต้องลงมือเองใน Editor:** จัด Scene/GameObject, Import โมเดล/Animation, เทสความรู้สึกของ Gameplay
**เครื่องมือแนะนำ:** Claude Code หรือ Unity AI Assistant (เชื่อมผ่าน Unity AI Gateway ได้) ทำงานกับไฟล์โปรเจกต์โดยตรง

---

## Phase 0: Foundation & Setup (สัปดาห์ 1-6) — ไม่เปลี่ยนจากเดิม

พื้นฐาน Unity/C# ไม่ขึ้นกับ Scope ของเกม จึงใช้เวลาเท่าเดิม

| สัปดาห์ | งาน |
|---|---|
| 1 | Unity Hub + URP Template, ทำ "Roll-a-Ball" tutorial |
| 2 | C# พื้นฐาน: class, inheritance, coroutine, event/delegate |
| 3 | Rigidbody + Collider (3D), Controller เดิน WASD (practice project แยก) |
| 4 | Simple FSM (State Pattern) ด้วย enum + switch-case |
| 5 | Unity UI (Canvas, World-space UI) |
| 6 | Git repo, โครงสร้างโฟลเดอร์ (Core/Presentation/Data ตาม Decoupled Core), เริ่ม `HeroBase3D.cs` |

**Exit Criteria:** ทำ Controller เดิน 3D เองได้, อ่าน error message เข้าใจโดยไม่ต้องพึ่ง AI ทุกบรรทัด

---

## Phase 1: Vertical Slice — "Vorkas ครบชุดสกิล" (สัปดาห์ 7-16, 10 สัปดาห์)

**เป้าหมาย:** พิสูจน์สถาปัตยกรรม Decoupled Core ด้วย Vorkas ตัวเดียว **แต่ครบทั้ง Passive + 3 Active + Ultimate** (เพิ่มจากเดิมที่ทำแค่ 2 สกิล) เพราะ Vorkas ใช้ Type ครบ 3 ใน 4 แบบของ Taxonomy (`SKILLSHOT_LINE`, `SELF_CAST`, `GROUND_TARGET_AOE`) เหมาะเป็นต้นแบบให้ฮีโร่ตัวอื่นในเฟสถัดไป

| สัปดาห์ | งาน |
|---|---|
| 7-8 | Simulation Core: Vorkas เดิน (click-to-move), HP/Mana, โจมตีพื้นฐาน |
| 9-10 | Input Abstraction Layer (Section 1.1) เวอร์ชัน PC-only |
| 11 | **Q: Iron Cleave** (`SKILLSHOT_LINE`) ตาม Section 6.1 |
| 12 | **W: Vanguard's Will** (`SELF_CAST`) |
| 13 | **E: Seismic Slam** (`GROUND_TARGET_AOE`) — สกิลที่ 3 ที่เพิ่มใหม่ |
| 14 | **R: Rebellion Impact** (Ultimate, `GROUND_TARGET_AOE` + Knockup + skill rank scaling 1/2/3) |
| 15 | Dummy Target รับดาเมจ/ตาย, Damage Popup, HP bar world-space UI |
| 16 | กล้อง Top-down (Section 7.1), Playtest ครบชุดสกิล, แก้บั๊ก |

**Exit Criteria:** Vorkas ใช้ได้ครบ Passive + 4 สกิลกับหุ่นนิ่ง ไม่มี Critical Bug

---

## Phase 2: Core Loop สมบูรณ์ — Bot + แผนที่ + เศรษฐกิจ + Escalation (สัปดาห์ 17-27, 11 สัปดาห์)

**เป้าหมาย:** Vorkas ตัวเดียวสู้ AI Bot จบเกมได้จริง รวมระบบ Tower Escalation ใหม่

| สัปดาห์ | งาน |
|---|---|
| 17-18 | แผนที่จริงตาม Section 3.1 (130m x 26m, พิกัด Fountain/Tower/Bush, ขอบเขตบังคับ และ Mini Map ที่เคารพ Vision/Brush) |
| 19 | Creep Spawner เวฟพื้นฐาน (Section 3.2) |
| 20 | **Tower Escalation System** (Cannon Minion เมื่อพัง Outer Tower, Super Creep เมื่อพัง Inner Tower) — ระบบใหม่ |
| 21-22 | FSM Bot **Medium Tier เท่านั้นก่อน** (Easy/Hard ไปทำ Phase 3) |
| 23 | ระบบ Tower (2 tier + Nexus) ตาม Section 3.3 พร้อม Mechanics (Heating Laser, Plating, AOE Slow) |
| 24 | ระบบเศรษฐกิจ (Section 4: Gold/EXP sources) |
| 25 | Leveling/EXP (Section 2.2), Respawn Timer (Section 2.3), ระบบแจก Skill Point ตอน Level Up |
| 26 | เชื่อม Skill rank-up เข้ากับปุ่ม Quick Skill Level-Up (Ctrl+Q/W/E/R ตาม Section 7.2) |
| 27 | Playtest เต็มแมตช์ Vorkas vs Bot ตั้งแต่ Spawn จนถึง Nexus แตก |

**Exit Criteria:** เล่นแมตช์ 1v1 กับ Bot จบทั้งเกมได้ รวม Tower Escalation ทำงานถูกต้องเมื่อป้อมพัง

---

## Phase 3: ฮีโร่ครบ 4 ตัว + Shop + Talent + Attribute + Balance (สัปดาห์ 28-43, 16 สัปดาห์)

**เป้าหมาย:** ขยายจาก 1 ฮีโร่เป็น 4 ฮีโร่ (ใช้ pattern จาก Vorkas) + เพิ่มระบบใหม่ทั้งหมดที่ยังไม่เคยทำ

| สัปดาห์ | งาน |
|---|---|
| 28-30 | **Zenthis** ครบ 4 ท่า (Section 6.2) — สกิล E: Temporal Rift เป็นสกิลใหม่ที่ต้องทำเพิ่ม |
| 31-33 | **Korvax** ครบ 4 ท่า (Section 6.3) — มี `SINGLE_TARGET` type ตัวแรกที่ยังไม่เคยทำ (Concussive Blast) ต้องขยาย Taxonomy Handler |
| 34-36 | **Gravitor** ครบ 4 ท่า (Section 6.4) |
| 37 | ระบบร้านค้า + ไอเทม 8 รายการ (Section 5) |
| 38-39 | **Talent Tree System** (Section 6.6) — UI เลือก A/B ที่ Level 4/8/12, เชื่อมกับปุ่ม `T` |
| 40 | **Attribute Bonus System** (Section 6.7) — UI ลงแต้ม 4 หมวด, เชื่อมกับปุ่ม `Ctrl+U` |
| 41 | FSM Bot Easy Tier + Hard Tier (Hard ใช้ Prediction Algorithm ให้ AI ช่วยเขียนสมการเวกเตอร์) |
| 42 | UI/HUD และ Control Polish (Section 7-8): Kill Feed, Cooldown/NO MANA state, Skill Tooltip, F1 Hero Profile, Mini Map interaction/expand, HUD input shield, Camera FREE/LOCKED และแผง Control แบบย่อได้ |
| 43 | Combat Readability & Balance Pass รอบแรก: ทดสอบทุกคู่ matchup (4x4), textured VFX/SFX ของฮีโร่-ป้อม-ครีป, Animation timing, movement pace และปรับค่าตัวเลขที่ไม่สมดุล |

**Exit Criteria:** เล่นได้ครบ 4 ฮีโร่ x 3 ระดับความยาก Bot, Shop/Talent/Attribute ใช้งานได้จริงครบ

---

## Phase 4: Hardening & Release Prep (สัปดาห์ 44-47, 4 สัปดาห์)

| สัปดาห์ | งาน |
|---|---|
| 44 | Bug bash — เล่นซ้ำๆ ครอบคลุมทุกระบบใหม่ (Talent/Attribute/Escalation) จด list บั๊กทั้งหมด |
| 45 | แก้บั๊กจาก list, Performance profiling (Unity Profiler บนสเปกใกล้เคียง minimum spec) |
| 46 | รัน Acceptance Criteria ทั้ง **8 ข้อ** จาก Section 11 ของ Requirement (เพิ่มจาก 6 เป็น 8 ข้อ ตามระบบใหม่) ทีละข้อ |
| 47 | Build สุดท้าย, README/คู่มือติดตั้งสำหรับผู้ทดสอบกลุ่มแรก |

**Exit Criteria:** ผ่าน Acceptance Criteria ครบ 8 ข้อ = **1.0.0 เสร็จสมบูรณ์**

---

## สรุปเปรียบเทียบ Timeline

| Phase | เดิม | ใหม่ | ส่วนต่าง |
|---|---|---|---|
| Phase 0 | 6 สัปดาห์ | 6 สัปดาห์ | ไม่เปลี่ยน |
| Phase 1 | 8 สัปดาห์ | 10 สัปดาห์ | +2 (สกิลที่ 3 + Ultimate) |
| Phase 2 | 8 สัปดาห์ | 11 สัปดาห์ | +3 (Tower Escalation + skill rank-up) |
| Phase 3 | 6 สัปดาห์ | 16 สัปดาห์ | +10 (สกิลที่ 3 ทุกฮีโร่ + Talent + Attribute) |
| Phase 4 | 4 สัปดาห์ | 4 สัปดาห์ | ไม่เปลี่ยน |
| **รวม** | **32 สัปดาห์** | **47 สัปดาห์** | **+15 สัปดาห์ (+47%)** |

---

## หลังจากนี้ (นอก Scope ของแผนนี้)

- **Version 1.1.0 (Mobile Port):** ~6-8 สัปดาห์ ตามเดิม เพราะ Core Logic ไม่ต้องแตะ
- **Phase-02 เป็นต้นไป:** 5v5, 3-เลน, Networking — ควรพิจารณาหาทีมเสริมเมื่อถึงจุดนี้
- **Future Ranked 5v5 Design Gate:** Secret Shop เฉพาะ Ranked, Ward System, Tree-consume Healing และ Gold Buyback ตาม Requirement Section 12.1 ต้องผ่าน Mode Rule Set, UX, Economy และ Balance Review ก่อนเริ่ม implementation; ห้ามนำเข้าร้าน 1v1 Version 1.0.0

---

*แผนนี้เป็น Living Document — ถ้า Scope ขยายอีกระหว่างทาง (เช่น เพิ่มฮีโร่ หรือเพิ่มระบบใหม่) ให้กลับมาคำนวณ Timeline ใหม่แบบเดียวกับที่ทำในรอบนี้ อย่าปล่อยให้ Roadmap เก่ากับ Requirement ใหม่ไม่ตรงกัน เพราะจะประเมินเวลาที่เหลือผิดพลาด*

---

## สถานะการดำเนินงานล่าสุด

อัปเดตเมื่อ **2026-09-09**: Phase 0-2 มี implementation artifacts ครบ; Phase 3 เปิดงาน Week 42-43 กลับมาเพื่อแก้ Combat Readability, Animation/Movement, Camera/Mini Map, HUD/Shop และ Free Asset integration ตามผล playtest ล่าสุด Unity batch import/script compile ผ่านแล้วและ Core verification ผ่าน 15/15; งานรอบนี้ยังต้องรอ manual playtest และการยอมรับด้านภาพ/เสียงจาก Owner ก่อนปิด Phase 3 ส่วน Phase 4 ยังต้องดำเนินการตาม Week 44-47 ก่อนประกาศ Version 1.0.0

ดูหลักฐานและรายการ gate ที่เหลือใน [`Project_KOA_Phase_Status_2026-09-08.md`](Project_KOA_Phase_Status_2026-09-08.md)
