# Production Roadmap — Project KOA
## เส้นทางจาก 0 ถึง Version 1.0.0 (PC-only) สำหรับนักพัฒนาคนเดียว

**บริบท:** 1 คน, Full-time, มือใหม่ Unity/C#, ใช้ AI (Claude Code) ช่วยเขียนโค้ด/debug/อธิบายเต็มที่
**เป้าหมาย:** 1.0.0 บน PC ตาม `Project_KOA_1v1_Complete_Requirement_v1.0.0.md`
**ประมาณการเวลารวม:** ~32 สัปดาห์ (~8 เดือน) ทำงานเต็มเวลา — ตัวเลขนี้รวม buffer สำหรับการเรียนรู้แล้ว อย่ากดดันตัวเองให้เร็วกว่านี้ในช่วงแรก เพราะพื้นฐานที่แน่นจะทำให้ Phase หลังเร็วขึ้นเยอะ

---

## หลักการทำงานร่วมกับ AI ตลอดโปรเจกต์

AI ช่วยได้ดีที่สุดในงานเหล่านี้ — ใช้ให้เต็มที่:
- **เขียน Boilerplate/Scaffold โค้ด** (เช่น สร้างคลาส C# ตาม schema, สร้าง FSM skeleton, สร้าง JSON loader)
- **อธิบาย Error message** ที่ Unity/C# ขึ้นเวลา compile ไม่ผ่าน (วางข้อความ error เต็มๆ ให้ AI อ่าน)
- **Review โค้ดที่เขียนเอง** ก่อน commit — ถามหา bug หรือจุดที่ไม่ตรงกับสถาปัตยกรรม Decoupled Core
- **แปลง Requirement เป็นโค้ดตรงๆ** เช่น เอาสูตร Damage Reduction ใน Section 6 ของ Requirement ไปให้ AI แปลงเป็นฟังก์ชัน C#

AI ช่วย**ไม่ได้ดี**ในงานเหล่านี้ — ต้องลงมือเองในโปรแกรม Unity Editor จริง:
- การจัด Scene, วาง GameObject, ปรับ Inspector values ด้วยมือ
- Import โมเดล/Animation แล้วเช็คว่าเข้ากับ Prefab ถูกต้อง
- การเทส "ความรู้สึก" ของ gameplay (feel) ต้องเล่นเองเท่านั้น

**แนะนำ:** ใช้ **Claude Code** (ติดตั้งในเครื่อง ทำงานกับไฟล์โปรเจกต์ Unity โดยตรง) แทนการ copy-paste โค้ดไปมาระหว่างแชทกับ Editor — จะเร็วกว่ามาก

---

## Phase 0: Foundation & Setup (สัปดาห์ 1-6)

**เป้าหมาย:** ปูพื้นฐาน Unity/C# ให้แน่นพอจะเริ่มงานจริงได้ ไม่ใช่การสร้างเกม KOA เลยในช่วงนี้

| สัปดาห์ | งาน |
|---|---|
| 1 | ติดตั้ง Unity Hub + Unity URP Template, เรียนรู้ Interface (Scene, Hierarchy, Inspector, Project), ทำ Unity official "Roll-a-Ball" tutorial ให้จบ |
| 2 | เรียน C# พื้นฐานให้แน่น: class, inheritance (สำคัญมากเพราะ `HeroBase3D` เป็น abstract class), coroutine, event/delegate |
| 3 | หัด Rigidbody + Collider (3D) เขียน Controller เดินไปมาง่ายๆ ด้วย WASD (ยังไม่ใช่ KOA — เป็น practice project แยก) |
| 4 | หัดทำ Simple FSM (State Pattern) ด้วย enum + switch-case ก่อน แล้วค่อยลองแบบ Interface-based FSM ให้เข้าใจหลักการที่ Section 3 ของ Requirement ต้องใช้ |
| 5 | หัด Unity UI (Canvas, World-space UI สำหรับ HP bar เหนือหัวตัวละคร) |
| 6 | ตั้ง Git repository จริงสำหรับโปรเจกต์ KOA, ตั้งโครงสร้างโฟลเดอร์ (Scripts/Core, Scripts/Presentation, Scripts/Data ตาม Decoupled Core), เริ่มเขียน `HeroBase3D.cs` จริงตาม Section 2.1 |

**Exit Criteria:** ทำ Controller เดิน 3D ง่ายๆ ได้เอง, เข้าใจ inheritance พอจะอ่าน error message เข้าใจโดยไม่ต้องพึ่ง AI ทุกบรรทัด

---

## Phase 1: Vertical Slice — "1 ฮีโร่ ต่อยหุ่น" (สัปดาห์ 7-14)

**เป้าหมาย:** พิสูจน์ว่าสถาปัตยกรรม Decoupled Core ทำงานได้จริง แบบง่ายที่สุดเท่าที่จะทำได้ — **ยังไม่ทำครบ 4 ฮีโร่, ยังไม่มี Bot, ยังไม่มี Shop/Economy**

| สัปดาห์ | งาน |
|---|---|
| 7-8 | สร้าง Simulation Core: Vorkas เดินได้ (click-to-move), มี HP/Mana, โจมตีพื้นฐานได้ |
| 9-10 | สร้าง Input Abstraction Layer (Section 1.1) เวอร์ชัน PC-only ก่อน — ให้ AI ช่วยออกแบบ interface `IInputAdapter` |
| 11 | ทำ Skill 1 ของ Vorkas (Iron Cleave — ประเภท `SKILLSHOT_LINE`) ให้ยิงได้จริง มี Cooldown ตาม Section 6.1 |
| 12 | สร้าง Dummy Target (หุ่นนิ่งไม่โจมตีกลับ) รับดาเมจได้ ตายได้ แสดง Damage Popup |
| 13 | ทำกล้อง Top-down ตาม Section 7.1 |
| 14 | Playtest + แก้บั๊ก + ทำ HP bar world-space UI ตาม Section 8 |

**Exit Criteria:** เดิน-โจมตี-ใช้สกิล-หุ่นตาย ได้ครบวงจร ไม่มี Critical Bug

---

## Phase 2: Core Loop สมบูรณ์ — Bot + แผนที่ + เศรษฐกิจ (สัปดาห์ 15-22)

**เป้าหมาย:** เอา Vorkas ตัวเดียวสู้กับ AI Bot ได้จบเกมจริง (ชนะ/แพ้)

| สัปดาห์ | งาน |
|---|---|
| 15-16 | สร้างแผนที่จริงตาม Section 3 (เลนเดียว, Fountain 2 ฝั่ง, Bush 2 จุด) |
| 17 | สร้าง Creep Spawner ตาม Section 3.2 |
| 18-19 | สร้าง FSM Bot **Medium Tier เท่านั้นก่อน** (Section 9) — เอา Easy/Hard ไปทำทีหลังใน Phase 3 |
| 20 | สร้างระบบ Tower (2 tier + Nexus) ตาม Section 3.3, ระบบ Economy (Section 4) |
| 21 | ระบบ Leveling/EXP (Section 2.2), Respawn Timer (Section 2.3) |
| 22 | Playtest เต็มแมตช์ Vorkas vs Bot ตั้งแต่ต้นจนจบ (ชนะ = ทำลาย Nexus) |

**Exit Criteria:** เล่นแมตช์ 1v1 กับ Bot จบได้ทั้งเกม (Spawn → Laning → Tower → Nexus → Win Screen) ด้วย Vorkas ตัวเดียว

---

## Phase 3: ฮีโร่ครบ 4 ตัว + Shop + Balance (สัปดาห์ 23-28)

**เป้าหมาย:** ขยายจาก 1 ฮีโร่เป็น 4 ฮีโร่ (ใช้ pattern ที่ทำกับ Vorkas ซ้ำ) + เพิ่ม Shop + Bot อีก 2 ระดับความยาก

| สัปดาห์ | งาน |
|---|---|
| 23-24 | สร้าง Zenthis + Korvax + Gravitor ตาม Section 6.2-6.4 (ใช้ Ability Taxonomy Section 6.5 เป็นแม่แบบ ลอกโครงสร้างจาก Vorkas) |
| 25 | สร้างระบบ Shop + ไอเทม 8 รายการ (Section 5) |
| 26 | เพิ่ม FSM Bot Easy Tier + Hard Tier (Section 3.2 ของเอกสารต้นฉบับ — Prediction Algorithm ของ Hard Tier อาจต้องให้ AI ช่วยเขียนสมการเวกเตอร์) |
| 27 | UI/HUD ให้ครบตาม Section 8 (Kill Feed, Cooldown radial, Gold/Level counter) |
| 28 | Balance Pass รอบแรก — เล่นทดสอบทุกคู่ matchup (4 ฮีโร่ x 4 ฮีโร่) ปรับค่าตัวเลขที่รู้สึกว่าไม่สมดุล |

**Exit Criteria:** เล่นได้ครบ 4 ฮีโร่ x 3 ระดับความยาก Bot, Shop ใช้งานได้จริง

---

## Phase 4: Hardening & Release Prep (สัปดาห์ 29-32)

| สัปดาห์ | งาน |
|---|---|
| 29 | Bug bash — เล่นซ้ำๆ หาบั๊ก จด list ทั้งหมด |
| 30 | แก้บั๊กจาก list, Performance profiling (ใช้ Unity Profiler เช็ค FPS บนสเปกที่ใกล้เคียง minimum spec) |
| 31 | รัน Acceptance Criteria ทั้ง 6 ข้อจาก Section 11 ของ Requirement ทีละข้อ |
| 32 | Build สุดท้าย, ทำ README/คู่มือติดตั้งสำหรับผู้ทดสอบกลุ่มแรก |

**Exit Criteria:** ผ่าน Acceptance Criteria ครบ 6 ข้อ = **1.0.0 เสร็จสมบูรณ์**

---

## หลังจากนี้ (นอก Scope ของแผนนี้)

- **Version 1.1.0 (Mobile Port):** เพิ่ม Touch Input Adapter ตัวใหม่เข้า Input Abstraction Layer เดิม, ปรับ UI ให้รองรับจอมือถือ, เทสบนเครื่องจริง — ประเมินเวลาเพิ่มอีก ~6-8 สัปดาห์ เพราะ Core Logic ไม่ต้องแตะเลย
- **Phase-02 เป็นต้นไป:** ตามที่ Requirement เดิมวางไว้ (P2P, 5v5, Cloud) — ควรพิจารณาหาทีมเสริมตอนถึงจุดนี้ เพราะ Networking Code เดี่ยวๆ ก็ใช้เวลาระดับเดียวกับ 1.0.0 ทั้งหมด

---

*แผนนี้เป็น Living Document เช่นกัน — ถ้าทำไปแล้วช้ากว่า/เร็วกว่าที่ประเมิน ให้ปรับตัวเลขสัปดาห์ตามจริง ไม่ต้องยึดติดกับตัวเลขในนี้แบบตายตัว*
