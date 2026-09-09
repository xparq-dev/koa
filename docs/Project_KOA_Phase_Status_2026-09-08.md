# Project KOA — Phase Status Report

**วันที่ตรวจสอบล่าสุด:** 2026-09-09

**เอกสารฐาน:** `Project_KOA_1v1_Complete_Requirement_v1.0.0.md` และ `Project_KOA_Production_Roadmap.md` ฉบับอัปเดต 47 สัปดาห์
**สถานะ Release:** ยังไม่ประกาศ 1.0.0 จนกว่าจะผ่าน Phase 4 และ Acceptance Criteria ครบ 8 ข้อ

## สรุปสถานะตาม Phase

| Phase | สถานะ | หลักฐานปัจจุบัน | งานที่ยังเหลือก่อนปิด Phase |
|---|---|---|---|
| Phase 0 — Foundation & Setup | Deliverables พร้อม | Unity 6000.5.10f1 + URP, Packages/ProjectSettings, Decoupled Core, Scene และ Windows build pipeline | Exit Criteria ด้านทักษะผู้พัฒนาต้องให้ Owner ประเมินเอง |
| Phase 1 — Vorkas Vertical Slice | ผ่านด้าน implementation | Vorkas มี Passive + Q/W/E/R, input abstraction, camera, target/view และ HUD | ไม่มีช่องว่างโค้ดที่พบในการตรวจรอบนี้ |
| Phase 2 — Core Loop | ผ่านด้าน implementation/runtime | แผนที่ 130x26m, creep/tower/nexus, economy, EXP/respawn, skill rank, Easy/Medium/Hard bot lifecycle และ Tower Escalation | ควรยืนยันความรู้สึกการเล่นด้วย manual playtest ใน Phase 4 |
| Phase 3 — 4 Heroes + Shop + Talent + Attribute + Balance | Implementation และ Unity Compile/Import ล่าสุดผ่าน; รอ manual sign-off | ฮีโร่ 4 ตัว, active skills 16 ท่า + passives, Shop 8 items, active item, Talent Section 6.6, Attribute Section 6.7, Vision/Brush, 4x4 smoke matrix และ Demo art integration | เล่นจริงทุก matchup และทุกระดับ Bot เพื่อปรับ balance/UX/งานภาพก่อนให้ Owner ปิด Week 43 |
| Phase 4 — Hardening & Release Prep | ยังไม่ปิด | มี preliminary Windows build และ runtime soak แล้ว | Week 44 bug bash, Week 45 fix + profiler บนเครื่องสเปกเป้าหมาย, Week 46 acceptance 8 ข้อ, Week 47 final release build/คู่มือ |

## สิ่งที่แก้ในรอบนี้

- เติม Unity project metadata, URP settings, playable scene และ Windows build method
- ทำให้ค่าฮีโร่และสกิลสอดคล้อง Section 6.1-6.4 รวม crowd control, cleanse, cooldown reduction และ passive effects
- เติม Vision/Brush rule ตาม Section 8 โดย Core ไม่ผูกกับ Presentation ตาม Section 1.1
- ทำ Nullifying Cloak active ให้ล้าง debuff และกัน crowd control ชั่วคราวตาม Section 5.2
- ทำ Talent ทั้ง 6 ผลตาม Section 6.6 และให้ skill damage modifier ส่งผลกับสกิลจริง
- ทำ Attribute Point แยกจาก Skill Point, 4 หมวด, Bot auto-allocation และหน้าต่าง `Ctrl+U` ตาม Section 6.7/7.2
- เพิ่ม regression gate สำหรับ Tower Escalation ตาม Section 3.2
- ปรับ README, comments และข้อความให้ตรงเอกสารฉบับใหม่ พร้อมตรวจ Inspire MOBA Policy
- เปลี่ยนฮีโร่ Demo เป็นโมเดล full-body เพื่อแก้แขน/ขา/หัวไม่ครบ และแยก animation controller เป็น Melee/Caster/Ranged/Monster
- แก้ตัวเลือก animation ที่เคยเลือก `Crouch_Idle_Loop` แทน `Idle_Loop` พร้อมผูกความเร็วการเดินกับ Core movement
- เพิ่ม Presentation VFX ให้ Basic Attack และ Q/W/E/R ครบ 4 ฮีโร่ตาม Section 6.1-6.4
- เพิ่ม lane surface, lane border, structure pad/ring และ fountain ring ให้ Tower/Nexus/ทางเดินอ่านตำแหน่งได้ชัดตาม Section 3.1/3.3
- แก้ HUD ที่สร้าง Texture ใหม่ซ้ำทุก OnGUI จน Unity graphics resource เต็มและ Editor ค้าง
- แก้ Environment shader เป็น URP Lit 19 materials และ remap โครงสร้าง/ธรรมชาติ เพื่อกำจัดภาพสีชมพู
- ปรับ Outer Tower / Inner Tower / Nexus เป็น 4.6 / 5.2 / 7 เมตร ลดความสูงและความกว้างของโครงสำรอง พร้อมจัด beacon/crown/health bar ให้สัมพันธ์กับสเกลใหม่ตาม Section 3.3
- เพิ่ม Arena Bounds ใน Simulation Core ตาม Section 3.1 ครอบคลุมการเดิน, dash, displacement, rewind และ respawn พร้อมกำแพงขอบสนามใน Presentation
- เพิ่ม Mini Map มุมซ้ายบน แสดง Fountain/Tower/Nexus/ครีป/ฮีโร่ และซ่อนฮีโร่ศัตรูตาม Vision/Brush; Unity Compile ผ่านแล้วและเหลือ manual playtest
- เปลี่ยนงานภาพสนามเป็น Dark-Fantasy Bridge Arena: สะพานหิน/ดิน/มอสเหนือเหว มี cliff face, support, broken parapet, fog, brazier และป่าหนาทึบ โดย Core bounds/ตำแหน่ง Section 3.1 ไม่เปลี่ยน
- เพิ่มเกราะ เสื้อคลุม/หมวก และอาวุธเฉพาะฮีโร่ทั้ง 4 ผ่าน `HeroEquipmentView` ซึ่งผูกกับ Humanoid bones และอยู่ใน Presentation เท่านั้นตาม Section 1.1
- เปิดใช้ Tower/Nexus FBX จริงแทน primitive overlay, เพิ่ม lower valley/river/waterfalls/cloud layer ใต้สะพาน และแก้ z-fighting ขอบเลนด้วยการแยกระดับผิว
- แก้ตามวิดีโอ `Screen Recording 2026-09-09 095850.mp4`: น้ำตกไม่เป็นแผ่นเรืองแสง, เมฆไม่เป็นก้อนทึบบังกล้อง, ลดหินริมทาง, ถอดวงรีดิน/เส้นดำแบบ placeholder และเพิ่มพื้นที่ภาพชั้นล่าง
- แก้ prefab generator ที่ทำให้ Tower/Nexus renderer เหลือประมาณ 0.04 เมตร โดยใช้ `Visual` scale wrapper; ปัจจุบัน Outer/Inner/Nexus มี renderer สูง 3.6/4.3/5.4 เมตรจริง
- เปลี่ยน Tower mapping เป็น Temple First/Second Age ซึ่งอ่านรูปทรงชัดกว่า, ลดฐาน/วงระยะ/beacon/health bar และหัน facade เข้าหากล้องมาตรฐาน

## หลักฐาน Verification ของ baseline ก่อน visual remediation

ผลในตารางนี้เป็น baseline ก่อนการแก้ภาพล่าสุด จึงห้ามใช้แทนผล Compile/Playtest ของ revision ปัจจุบัน

| Gate | ผล |
|---|---|
| Unity compile + regression suite | PASS — 13/13, ไม่มี C# warning/error ใน log |
| Ability cast contract | PASS — active skills 16/16 |
| Hero matchup smoke matrix | PASS — 4x4 = 16 scenarios |
| Core stability | PASS — 20 นาทีที่ 30 simulation ticks/sec |
| Tower Escalation | PASS — Outer เพิ่ม Cannon, Inner เพิ่ม Super ในเวฟถัดไป |
| Windows build | PASS — `Builds/Windows/KOA.exe`, build folder 164,845,597 bytes |
| Runtime Easy | PASS — 1,200 simulated seconds, ถึงสถานะจบเกม |
| Runtime Medium | PASS — 1,200 simulated seconds, ถึงสถานะจบเกม |
| Runtime Hard | PASS — 2,400 simulated seconds, ถึงสถานะจบเกม (แมตช์ยาวกว่า Easy/Medium) |
| Runtime graphics path | PASS — Medium, 1,200 simulated seconds, ถึงสถานะจบเกม |
| Prohibited commercial-name scan | PASS — ไม่พบใน `Assets`, `docs`, `README.md` |
| `git diff --check` | PASS |

## สถานะ gate ของ visual remediation หลังเปิด Unity ใหม่

| Gate | สถานะ |
|---|---|
| ตรวจชื่อ animation clip จาก Unity `.fbx.meta` | PASS — คลิปที่ controller ใหม่ต้องใช้มีครบ |
| ตรวจ event-to-VFX coverage | PASS จาก source review — Basic Attack + Q/W/E/R ครบ 4 ฮีโร่ |
| `git diff --check` เฉพาะไฟล์ที่แก้ | PASS |
| Unity Import/Compile revision ล่าสุด | PASS — Unity 6000.5.10f1 ไม่พบ C# error ใน `Logs/video_followup_validation_2026-09-09_final.log` และ compile follow-up logs |
| Generated controller validation | PASS — 4/4 controller มี Idle/Move/Attack/Cast/Death, motion และ parameter ครบ |
| Animated prefab artifact validation | PASS — 8/8 prefab ผูก controller ถูกชุด, ปิด Root Motion และมี CapsuleCollider |
| Hero full-body source validation | PASS — 4/4 prefab ใช้ full-body/Puglin source ตาม mapping ใหม่ |
| Environment material validation | PASS — Tower/Nexus/Fountain/Nature ใช้ material ที่ valid และไม่พบ `Hidden/InternalErrorShader` |
| Generated demo prefab validation หลังเพิ่มป่า/ขอบเหว | PASS — 22/22 จาก Unity batch validation |
| Arena surface materials | PASS — 5/5 ใช้ URP shader และ texture/normal map ตามชนิด |
| Tower/Nexus renderer bounds | PASS — Outer 3.6m, Inner 4.3m, Nexus 5.4m จาก Editor preview diagnostic |
| Editor still preview | PASS ด้านการเกิดภาพ 3 มุม — `Logs/arena_preview_2026-09-09`; ยังไม่แทน Owner gameplay review |
| Manual visual/playability review | PENDING — Owner Run ใน Engine |
| Build | NOT RUN — ตามคำสั่ง Owner ให้ Run ที่ Engine เอง |

## Gates ที่ยังต้องทำด้วยคน/เครื่องเป้าหมาย

1. Manual playtest ทุก hero matchup และ Easy/Medium/Hard: ตรวจ input, camera, HUD, shop, Talent, Attribute และ gameplay feel
2. Bug bash พร้อม issue list ตาม Week 44
3. Unity Profiler บนเครื่องใกล้ minimum/recommended spec ตาม Week 45; ค่า FPS จาก accelerated/hidden verification ใช้ยืนยันสเปกเครื่องจริงไม่ได้
4. รันและลงนาม Acceptance Criteria Section 11 ครบ 8 ข้อ โดยเฉพาะ PC input บน build จริงและ performance gate
5. สร้าง final release build และคู่มือผู้ทดสอบตาม Week 47

## ขอบเขตหลัง 1.0.0

Mobile controls ยังคง Deferred ไป Version 1.1.0 ตาม Section 7.3 และ 12 ไม่ใช่งานค้างของ Phase 3/4 ปัจจุบัน
