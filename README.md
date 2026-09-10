# Project KOA (Kronos Origin Area)

### 1v1 Single-Lane Duel Arena — Unity 3D URP / C#

[![Unity](https://img.shields.io/badge/Unity-6000.5.10f1%20%7C%20URP-black.svg?style=flat&logo=unity)](https://unity.com/)
[![Architecture](https://img.shields.io/badge/Architecture-Decoupled%20Core-blue.svg)](docs/Project_KOA_1v1_Complete_Requirement_v1.0.0.md)
[![Status](https://img.shields.io/badge/Roadmap-Phase%203%20Owner%20Playtest-yellow.svg)](docs/Project_KOA_Production_Roadmap.md)

Project KOA เป็นเกม MOBA แบบ 1v1 บนแผนที่เลนเดี่ยวสำหรับ PC พัฒนาด้วย Unity URP และ C# โดยแยก Simulation Core ที่ทำงาน 30 ticks/second ออกจาก Input และ Presentation Layer ตามสถาปัตยกรรม Decoupled Core ใน Requirement Section 1.1

## สถานะปัจจุบัน

อัปเดตล่าสุด: **2026-09-10**

- Phase 0-2 มี implementation artifacts ของ Foundation, Core Loop, Bot, Arena, Economy, Tower และ Minion แล้ว
- Phase 3 Week 28-43 มีฮีโร่ 4 ตัว, สกิล, Shop, Talent, Attribute, Bot 3 ระดับ และรอบ Combat/UI/Control Polish แล้ว
- Baseline ก่อนรอบ Environment ผ่าน Unity script compilation และ Core verification แล้ว; source รอบล่าสุดรอ Owner เปิด Unity เพื่อ compile และ visual playtest โดยไม่สั่ง Player Build จาก Codex
- ชุดตรวจ Core มีกรณี Q/W/E/R ซ้ำหลังคูลดาวน์, สกิลครบ 16 ท่า, matchup smoke matrix 4x4, simulation ต่อเนื่อง 20 นาที, Point Symmetry/Rhythmic Spacing และ Last-Hit ownership เมื่อทั้งสองฝั่งเลือกฮีโร่ชนิดเดียวกัน
- Phase 3 ยังไม่ปิดจนกว่า Owner จะทดสอบใน Unity Engine และยอมรับด้านภาพ เสียง Animation และการควบคุม
- ยังไม่มี Player Build จากรอบนี้ เจ้าของโปรเจกต์จะเป็นผู้ Run และ Build ผ่าน Unity Engine
- Phase 4 ยังเหลือ Bug Bash, Performance Profiling, Acceptance Criteria 8 ข้อ และ Release Preparation

รายละเอียดสถานะและ Gate ที่เหลืออยู่ใน [Production Roadmap](docs/Project_KOA_Production_Roadmap.md) และ [Phase Status](docs/Project_KOA_Phase_Status_2026-09-08.md)

## ฟีเจอร์ที่มีแล้ว

### ฮีโร่และการต่อสู้

- ฮีโร่ 4 ตัว: Vorkas, Zenthis, Korvax และ Gravitor
- ฮีโร่แต่ละตัวมี Passive และ Q/W/E/R ตาม Requirement Section 6.1-6.4
- Mana ฟื้นอัตโนมัตินอก Fountain และฟื้นเร็วขึ้นภายใน Fountain
- HUD แยกสถานะ READY, COOLDOWN, NO MANA และ UNLEARNED/LOCKED
- เมื่อร่ายไม่สำเร็จจะแสดงเหตุผลแบบข้อความเดี่ยวในพื้นที่ว่างกึ่งกลางใต้ Top Bar และมีเสียงตอบสนอง โดยไม่สะสมทับ Mini Map หรือ HUD ส่วนอื่น
- Targeting แยกตามชนิดสกิล: Single Target ต้องชี้ศัตรูในระยะก่อนและไม่เสีย Mana/Cooldown เมื่อไม่ผ่าน, Ground AOE ต้องอยู่ใน Arena, ส่วน Direction Skillshot สามารถยิงพลาดและเสียทรัพยากรตามปกติ
- Tooltip Q/W/E/R ระบุชัดว่าเป็น Enemy Target, Direction Skillshot, Ground Area หรือ Self Cast
- Damage Popup แสดงค่าดาเมจจริงหลังหักเกราะเพียงครั้งเดียว และซ่อนตัวเลขเมื่อครีปตีกันเองเพื่อลดความสับสนระหว่างการต่อสู้
- เอฟเฟกต์ของฮีโร่ ป้อม และครีปใช้ textured particle, projectile, ground glyph และ impact
- Idle, Walk, Attack, Cast และ Death แยกสถานะ โดยจังหวะโจมตี/ร่ายเริ่มจาก Core success event
- Base movement pace ปรับตาม Requirement Section 6.5.2

### แผนที่และ Core Loop

- Duel Arena ขนาดประมาณ 130 x 26 เมตร พร้อมขอบเขตบังคับ
- Fountain, Outer Tower, Inner Tower และ Nexus ฝั่งละหนึ่งชุด
- Gameplay anchors ใช้ Point Symmetry จาก Core layout เดียว และช่องไฟ Fountain→Nexus→Inner→Outer กระชับเข้าหากลางเลนแบบ 17→15→13→12 เมตร
- แผนที่สะพานสูงมีหน้าผาต่างระดับเป็นช่วง, ป่าจังหวะ rule-of-thirds, Terrain Detail Grass, ลำน้ำ/น้ำตก, เมฆ, ป้อมฐานและประตู Primitive
- Bush สองฝั่งเยื้องแกนกลางและวางแบบ Point Symmetry เพื่อสร้าง Sightline decision
- ลำดับแสงเป้าหมายคือ Nexus สว่างที่สุด ตามด้วย Fountain และ Tower
- ครีปพื้นฐาน 2 Melee + 1 Ranged เกิดทุก 25 วินาที
- Cannon Minion และ Super Creep จาก Tower Escalation
- Tower Heating, Tower Plating, Fountain regeneration และลำดับการทำลายโครงสร้าง
- ระบบ Gold, EXP, Level, Respawn, Kill Feed และเงื่อนไขชนะ/แพ้
- Last Hit ครีปตรวจผู้โจมตีแยกตามทีมและให้ทองเฉพาะฮีโร่ผู้ปิดงาน พร้อมเหรียญ `+Gold` ลอยเหนือจุดตายและเสียงยืนยัน
- Kill Feed ระบุผู้กำจัด ผู้ถูกกำจัด และทองจาก Hero/Tower reward โดยไม่เพิ่มข้อความจากครีปทุกตัวจนรบกวนการต่อสู้

### Bot, Shop และ Progression

- Bot AI ระดับ Easy, Medium และ Hard
- Shop ใช้ได้เฉพาะภายใน Fountain และมีไอเทมเริ่มต้น 8 รายการ
- Shop แบ่ง All Items, Consumables, Attributes, Equipment, Miscellaneous และ Upgraded
- Inventory 6 ช่องและ Active Item hotkeys 1-6
- Talent แบบเลือก A/B ที่ Level 4, 8 และ 12
- Attribute Point แยกจาก Skill Point สำหรับ Vitality, Focus, Armor และ Resolve

### HUD และ Camera

- Mini Map มุมซ้ายบน แสดง Fountain, Tower, Nexus, ครีป และฮีโร่ตามกฎ Vision/Brush
- Mini Map ย่อ/ขยายได้ คลิกซ้ายเพื่อเลื่อนกล้อง และคลิกขวาเพื่อส่งคำสั่งเดิน
- กล้องมีสถานะ FREE และ LOCKED พร้อมปุ่มแสดงสถานะบน HUD
- มุมกล้อง pitch 60° / yaw -45° / vertical FOV 46° / ระยะเริ่ม 24 เมตร (ซูม 18-32 เมตร) ทำให้แนว Blue → Red พาดจากซ้ายล่างไปขวาบน โดยไม่หมุนพิกัด Simulation Core และให้ระยะมองสนามที่อ่านการต่อสู้ได้ชัดขึ้น
- Edge Pan, scroll zoom, Space focus/temporary lock และ Y toggle lock
- HUD ป้องกันการคลิกทะลุไปเป็นคำสั่งเดินหรือเลือกเป้าหมาย
- Hero & Bot Controls ย่อ/ขยายได้
- Hover Q/W/E/R เพื่อดูชื่อ Mana Cost, Base Cooldown และรายละเอียดสกิล
- F1 เปิด Hero Profile ซึ่งมี Model Preview, Role, Lore, Passive และ Q/W/E/R
- UI ใช้กรอบสีเข้มแบบบางที่ปรับตามความละเอียดและเสียง CC0 ที่คัดเลือกไว้ใน Asset Manifest; กรอบภาพทดลองถูกปิดจาก HUD ระหว่างเล่นหลัง visual QA พบว่ารบกวนการอ่านข้อมูล
- Ability Feedback แยกจาก Kill Feed: Cooldown/NO MANA/UNLEARNED แสดงทีละข้อความใน safe area ระหว่าง Mini Map กับแผงควบคุม ส่วน Kill Feed จำกัด 3 รายการและเลื่อนลงอัตโนมัติเมื่อ Ability Feedback แสดงอยู่

## การควบคุมบน PC

| คำสั่ง | คีย์บอร์ด/เมาส์ |
|---|---|
| เดินและแสดง Move Marker | คลิกขวาบนสนาม |
| โจมตีเป้าหมาย | คลิกซ้าย |
| Attack Command | A |
| Skill 1 / 2 / 3 / Ultimate | Q / W / E / R |
| อัปสกิลด่วน | Ctrl + Q / W / E / R |
| Active Item ช่อง 1-6 | 1-6 |
| Talent | T |
| Attribute Panel | Ctrl + U |
| Shop ภายใน Fountain | P |
| Hero Profile | F1 |
| Scoreboard/Stats | Tab |
| Focus ตัวละคร | กด Space หนึ่งครั้ง |
| Lock กล้องชั่วคราว | กด Space ค้าง |
| สลับ FREE/LOCKED | Y หรือปุ่มบน HUD |
| เลื่อนกล้อง | Edge Pan |
| Zoom | Scroll Wheel |
| เลื่อนกล้องด้วย Mini Map | คลิกซ้ายบน Mini Map |
| สั่งเดินด้วย Mini Map | คลิกขวาบน Mini Map |

## วิธีเปิดทดสอบใน Unity

1. เปิดโฟลเดอร์โปรเจกต์นี้ผ่าน Unity Hub ด้วย **Unity 6000.5.10f1**
2. รอ Unity Import Assets และ Script Compilation ให้เสร็จ
3. เปิด Scene `Assets/Scenes/DuelArena.unity`
4. ตรวจ Console ว่าไม่มี compile error
5. กด Play ใน Unity Editor

รอบทดสอบ Phase 3 แนะนำให้ตรวจอย่างน้อย:

1. ใช้ Q/W/E/R ของฮีโร่ทั้ง 4 ซ้ำหลังคูลดาวน์และหลัง Mana ฟื้น
2. เปรียบเทียบ projectile, ground area, impact, สี และเสียงของทุกสกิล
3. ตรวจ Idle/Walk/Attack/Cast ว่ากลับสถานะถูกต้องและเท้าไม่ลื่น
4. ตรวจคลิกขวา, Move Marker, HUD input shield และ Shop
5. ตรวจ Edge Pan, Space, Y และปุ่ม CAMERA FREE/LOCKED
6. ตรวจ Mini Map ทั้งย่อ/ขยาย, เลื่อนกล้อง และสั่งเดิน
7. ตรวจ Tooltip และ Hero Profile ด้วย F1 ที่ความละเอียดหน้าจอจริง
8. Last Hit ครีปด้วยฮีโร่แล้วตรวจเหรียญ `+Gold`, จากนั้นปล่อยให้ครีปฆ่ากันเพื่อตรวจว่าไม่แจกทอง และตรวจ Hero/Tower reward ใน Kill Feed

## โครงสร้างโปรเจกต์

```text
Assets/
├── Presentation/               # Material, model, prefab และ visual presentation assets
├── Resources/KOA/              # Runtime-loaded VFX, audio และ UI assets
├── Scenes/                     # DuelArena และ scene assets
├── Scripts/
│   ├── Core/                   # Pure C# simulation logic
│   │   ├── AI/                 # FSM และ Bot difficulty
│   │   ├── Economy/            # Wallet และ Gold
│   │   ├── Entities/           # Hero และ target entities
│   │   ├── Input/              # Input abstraction contract
│   │   ├── Items/              # Inventory และ Shop
│   │   ├── Match/              # Match simulation และ rules
│   │   ├── Minions/            # Minion และ wave spawner
│   │   ├── Structures/         # Tower และ Nexus
│   │   ├── Vision/             # Vision และ Brush rules
│   │   └── World/              # Arena bounds และ world contracts
│   ├── Data/                   # Enums, item/structure models และ schemas
│   ├── Editor/                 # Setup, diagnostics และ verification tools
│   └── Presentation/           # MonoBehaviour, camera, input, UI, VFX และ views
└── ThirdParty/                 # Third-party assets และ license files
```

Simulation Core ห้ามอ้างอิง MonoBehaviour, GameObject, Animator หรือ UI โดยตรง Presentation จะอ่าน state/event จาก Core แล้วแปลงเป็นภาพ เสียง และ input feedback

## Assets และสิทธิ์การใช้งาน

Demo ปัจจุบันใช้และทดลอง Asset ฟรีจาก Kenney และ Quaternius พร้อมเก็บ license ที่เกี่ยวข้องไว้ใต้ `Assets/ThirdParty` รายการไฟล์ที่คัดใช้ ตำแหน่งต้นฉบับ checksum และแหล่งดาวน์โหลดอยู่ใน [Demo Asset Manifest](docs/Project_KOA_Demo_Asset_Manifest.md)

ก่อนนำ Asset ใหม่เข้าโปรเจกต์:

- ตรวจ license จากหน้าผู้สร้างและไฟล์ที่มากับชุด Asset
- เก็บไฟล์ license และ source URL
- บันทึกไฟล์ที่นำมาใช้จริงใน Asset Manifest
- ห้ามสมมติว่า “ดาวน์โหลดฟรี” เท่ากับอนุญาตเชิงพาณิชย์

## เอกสารอ้างอิง

- [Complete Requirement v1.0.0](docs/Project_KOA_1v1_Complete_Requirement_v1.0.0.md)
- [Production Roadmap](docs/Project_KOA_Production_Roadmap.md)
- [Demo Asset Manifest](docs/Project_KOA_Demo_Asset_Manifest.md)
- [MOBA Standard Control & Camera Research](docs/Project_KOA_MOBA_Standard_Control_Camera_Research_2026-09-09.md)
- [Phase Status](docs/Project_KOA_Phase_Status_2026-09-08.md)
- [Agent Guidelines](AGENTS.md)

## Deferred Scope

สิ่งต่อไปนี้ยังไม่ใช่ implementation ของ Version 1.0.0 แบบ 1v1:

- Mobile controls และ Mobile build
- Multiplayer networking และ matchmaking
- 5v5, แผนที่ 3 เลน, Jungle Camp และ Neutral Monster
- Ranked-only Secret Shop, Ward, Tree-consume Healing และ Gold Buyback
- Item Recipe/Crafting แบบหลายขั้น

ข้อกำหนดแนวคิดสำหรับ Ranked 5v5 ถูกแยกไว้ใน Requirement Section 12.1 และยังต้องผ่าน Mode Rule Set, UX, Economy และ Balance Review ก่อนเริ่ม implementation
