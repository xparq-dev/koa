# System Requirements Document (SRD) — Complete Edition
## Project Code: KOA (Kronos Origin Area)
### Scope: Phase-01 / Version 1.0.0 — "Duel Arena" (1v1 Single Lane)

**สถานะเอกสาร:** ฉบับนี้ต่อยอดจาก `Project_KOA_3D_Requirements.md` เดิม โดยล็อกขอบเขตให้ชัดเจนสำหรับ 1.0.0 เท่านั้น (ระบบ 5v5 / 3-เลน / ป่า ถูกเลื่อนไปเวอร์ชันหลังตามที่ตัดสินใจไว้)

---

## 0. Decision Log (สรุปการตัดสินใจจากการคุยกัน)

| หัวข้อ | การตัดสินใจ | เหตุผลสั้นๆ |
|---|---|---|
| Engine | **Unity 3D (URP)** | Runtime Fee ถูกยกเลิกแล้ว (ก.ย. 2024), C#/mobile/WebRTC เสถียรกว่า Godot ในตอนนี้ |
| Game Mode (1.0.0) | **1v1 เท่านั้น** | โฟกัสพิสูจน์ Core Loop ก่อนขยายเป็น 5v5 |
| แผนที่ | **เลนเดียว ไม่มีป่า/มอนสเตอร์ฟาร์ม** มีพุ่มหญ้า/เนินหญ้าเพื่อกลไก Brush | เน้น VS ตัวต่อตัวล้วนๆ ลดความซับซ้อนของ FSM Bot |
| โครงสร้างป้อม | **2 ป้อม + Nexus ต่อข้าง** | สมดุลระหว่างจังหวะเกม 3 ช่วง กับความยาวแมตช์ที่เหมาะกับ 1v1 |
| Roster | **4 ฮีโร่เดิม** (Vorkas, Zenthis, Korvax, Gravitor) | ครอบคลุม 4 role หลัก พอสำหรับ 1v1 ไม่ติดปัญหาฮีโร่ไม่พอเติมทีม (เพราะไม่มีทีม 5 คน) |
| ระบบไอเทม/ร้านค้า | **มีระบบพื้นฐานใน 1.0.0** | เป็นหัวใจของ MOBA ขาดไม่ได้แม้ใน scope เล็ก |
| Controls | **PC-first สำหรับ 1.0.0** — มือถือ Port ทีหลังหลัง 1.0.0 สำเร็จบน PC | ทีมพัฒนาคนเดียว มือใหม่ Unity/C# — ลดความเสี่ยง debug 2 แพลตฟอร์มพร้อมกัน, Input Abstraction Layer ที่ออกแบบไว้ยังรองรับการเพิ่มทีหลังได้โดยไม่ต้องรื้อ |
| ทีมพัฒนา | **1 คน, Full-time, มือใหม่ Unity/C#, ใช้ AI ช่วยเต็มที่ (Claude Code)** | บริบทจริงของโปรเจกต์ กระทบ timeline และลำดับงานทั้งหมด |

---

## 1. สถาปัตยกรรม (คงเดิมจากเอกสารต้นฉบับ)

### 1.1 Decoupled Core Paradigm
คงหลักการ Model-View-Presenter เดิมทั้งหมด: Simulation Core รันที่ 30 Ticks/sec บน `Vector3` space, View Layer แยกอิสระ

**ส่วนที่เพิ่มสำหรับ 1.0.0 — Input Abstraction Layer:**
เนื่องจากต้องรองรับทั้ง PC และมือถือพร้อมกัน จึงต้องมี Input Layer กลางที่แปลงสัญญาณดิบจากทั้งสองแพลตฟอร์มให้เป็นรูปแบบเดียวกันก่อนส่งเข้า Simulation Core:

```
[PC: Mouse/Keyboard]  ---\
                          +--> [Input Abstraction Layer] --> { moveVector, castIntent, aimVector } --> [Simulation Core]
[Mobile: Touch/Joystick] --/
```

- `moveVector`: Vector2 normalized (-1..1, -1..1) แปลงจาก WASD หรือ Virtual Joystick
- `castIntent`: enum (None, CastSkill1, CastSkill2, CastUltimate, CastAttack)
- `aimVector`: Vector3 world-space position ที่สกิลจะยิงไป (แปลงจาก mouse world-position หรือ drag-direction บนมือถือ)

Simulation Core **ไม่รับรู้เลย** ว่าคำสั่งมาจาก platform ไหน — นี่คือจุดสำคัญที่ทำให้ dual-platform ทำได้โดยไม่เพิ่มความซับซ้อนใน Logic layer

---

## 2. ตัวละครและค่าพื้นฐาน (คงเดิม + เพิ่มระบบ Leveling)

### 2.1 Base Class Schema (คงเดิม)
ใช้ `HeroBase3D` schema เดิมจากเอกสารต้นฉบับ section 2.1 ไม่มีการเปลี่ยนแปลง

### 2.2 ระบบ Leveling & EXP (ใหม่ — ช่องโหว่ที่เติมให้)

**Level Cap สำหรับ 1.0.0:** Level 12 (สั้นกว่ามาตรฐาน MOBA ทั่วไปที่ 18 เพื่อให้แมตช์ 1v1 จบในเวลาที่เหมาะสม ~15-20 นาที)

**สูตร EXP สะสมที่ต้องใช้เพื่อขึ้น Level ถัดไป:**
```
EXP_Required(level) = 200 * (level ^ 1.3)
```

**Stat Growth ต่อ Level (ค่าคงที่ ไม่ใช้ระบบ primary attribute เพื่อความเรียบง่ายใน 1.0.0):**

| Hero | +HP/lv | +Armor/lv | +MR/lv | +AD/lv | +Mana/lv |
|---|---|---|---|---|---|
| Vorkas | 45 | 2.0 | 1.0 | 3.0 | 15 |
| Zenthis | 30 | 1.0 | 1.5 | 2.5 | 25 |
| Korvax | 28 | 1.0 | 1.0 | 4.0 | 18 |
| Gravitor | 55 | 2.5 | 2.5 | 2.0 | 20 |

*(ค่าเหล่านี้เป็น draft ตั้งต้น ต้องปรับจาก playtest จริงหลัง Phase-01 มีต้นแบบเล่นได้)*

### 2.3 Respawn Timer Formula (ใหม่)
```
RespawnTime (seconds) = 4 + (CurrentLevel * 2.5)
```
ตัวอย่าง: ตายตอน Level 1 = 6.5 วิ, ตายตอน Level 12 = 34 วิ (ยิ่งเลเวลสูง โทษการตายยิ่งหนัก มาตรฐาน MOBA ทั่วไป)

---

## 3. แผนที่ 1v1 Duel Arena (ใหม่ทั้งหมด — แทนที่ Section 7 เดิม)

### 3.1 ขนาดและโครงสร้าง (อัปเดต 130m Scale)
- **รูปแบบ:** เลนเดียว แนวยาว (Corridor) เชื่อม Fountain ทั้งสองฝั่ง
- **ขนาดสนามโดยประมาณ:** ยาว 130 เมตร x กว้าง 26 เมตร (1 Grid Unit = 1 เมตร)
  - ขอบเขตที่เล่นได้คือ X = -13m ถึง +13m และ Z = -65m ถึง +65m โดยศูนย์กลางของฮีโร่ต้องเว้นขอบตามรัศมีตัวละคร การเดิน, dash, displacement, rewind และ respawn ต้องไม่สามารถพาฮีโร่ออกนอกสนาม
  - Blue Fountain: Z = -57m, Blue Nexus: Z = -40m, Blue Inner Tower: Z = -25m, Blue Outer Tower: Z = -12m
  - Red Outer Tower: Z = +12m, Red Inner Tower: Z = +25m, Red Nexus: Z = +40m, Red Fountain: Z = +57m
  - ทุกตำแหน่งฝั่ง Red ต้องคำนวณจากตำแหน่งฝั่ง Blue ด้วย Point Symmetry `(x, y, z) → (-x, y, -z)` ห้ามกำหนดแยกจนเกิดระยะคลาดเคลื่อน
  - ระยะจากฐานเข้าสู่กลางเลนต้องค่อยๆ กระชับ: Fountain→Nexus 17m, Nexus→Inner 15m, Inner→Outer 13m และ Outer→Center 12m
  - พื้นที่ปะทะกลางเลน (Center Clash Zone): ระหว่าง Outer Towers กว้าง 24 เมตร (Z = -12m ถึง +12m)
- **Fountain Zone:** รัศมี 7.5 เมตร อยู่ด้านหลัง Nexus แต่ละฝั่ง มี HP/Mana regen สไตล์ Inspire MOBA (~11% Max HP-MP/sec ใช้เวลาประมาณ 8-10 วินาทีเต็มหลอด) และ invulnerability
- **Bush/พุ่มหญ้า:** วางแบบ Point Symmetry และเยื้อง Sightline กลางเลน โดยมีจุดศูนย์กลาง Blue `(X=-8m, Z=-3.5m)` และ Red `(X=+8m, Z=+3.5m)` ขนาดประมาณ 3.2m x 6.0m
- **ไม่มี Jungle Camp / Neutral Monster** ใน 1.0.0

### 3.1.1 Environment Composition Principles

ฉากทดสอบ `Assets/Scenes/DuelArena.unity` ต้องสร้าง Presentation environment โดยไม่เปลี่ยนกฎ Simulation Core และใช้หลักดังนี้:

1. **Point Symmetry:** Fountain, Nexus, Tower และ Brush เป็นตำแหน่ง gameplay ที่สะท้อนผ่านจุดศูนย์กลางเดียวกัน
2. **Rhythmic Spacing:** ช่องไฟโครงสร้างไม่เท่ากันและกระชับเข้าหา Duel Plaza ตามค่าที่ล็อกใน Section 3.1
3. **Occlusion Framing:** หน้าผา, เรือนยอดไม้ และก้อนหินริมทางแบ่งเป็นช่วงความสูง/ความถี่ไม่สม่ำเสมอ ห้ามใช้กำแพงสูงเท่ากันตลอดเลน
4. **Sightline Bush Placement:** Brush ต้องเยื้องแกน X=0 และเยื้อง Z=0 เพื่อสร้างการตัดสินใจด้าน vision ไม่ใช่ของตกแต่งกลางทาง
5. **Focal Lighting:** Nexus สว่างที่สุด, Fountain เป็นลำดับสอง และ Tower เป็นลำดับสาม; แสงต้องไม่บดบัง Health Bar หรือ telegraph การต่อสู้

พื้นหญ้าจุลภาคใช้ Unity Terrain Detail/Grass แบบ runtime-generated และ mirrored density map; ป้อมฐาน, กำแพง, ประตู, บ่อ Fountain และ silhouette สำรองใช้ Primitive ก่อน ส่วน Asset ภายนอกใช้ได้เฉพาะรายการที่มี License ชัดเจนใน Demo Asset Manifest เท่านั้น องค์ประกอบ Presentation ต้องไม่เพิ่ม collider ที่ขยาย/ลดขอบเขตเล่นจริง

### 3.2 Creep Spawner & Escalation (อัปเดตระบบป้อมแตก)
- **ความถี่:** ทุก 25.0 วินาที
- **รูปแบบเวฟพื้นฐาน:** 2 Melee Minion + 1 Ranged Minion ต่อเวฟ
- **Tower Escalation Advantage (ระบบความได้เปรียบเมื่อทำลายป้อม):**
  - **เมื่อทำลาย Outer Tower ศัตรูสำเร็จ:** เวฟของฝั่งเราจะได้รับ **Cannon Minion (ครีปปืนใหญ่)** เพิ่ม 1 ตัวต่อเวฟ (ยิงไกล 6.5m, HP สูง, ตีหนัก)
  - **เมื่อทำลาย Inner Tower ศัตรูสำเร็จ:** เวฟของฝั่งเราจะได้รับ **Super Creep (Boss Creep)** เพิ่ม 1 ตัวต่อเวฟ (ตัวใหญ่ เลือดมหาศาล 1600+ HP, ตีป้อมรุนแรง)
- **Evolution Scaling:** +8% Max HP / +4% AD ทุก 2.5 นาที (150 วินาที)

### 3.3 โครงสร้างป้อมปราการ (แทนที่ Section 8 เดิม — ลดจาก 3 tier เหลือ 2 tier)

```json
{
  "map_structures_database_1v1": {
    "tier_1_outer_tower": {
      "display_name": "Outer Tower",
      "max_hp": 2800.0,
      "base_armor": 35,
      "base_magic_resist": 35,
      "base_attack_damage": 110.0,
      "attack_range": 8.0,
      "fire_rate_seconds": 1.0,
      "damage_type": "Physical",
      "gold_bounty": 150,
      "exp_bounty": 120,
      "mechanics": {
        "is_heating_laser": true,
        "damage_increase_per_hit_percent": 20.0,
        "max_damage_multiplier": 2.5,
        "has_tower_plating": true,
        "plating_duration_minutes": 4.0,
        "plating_bonus_armor": 120
      }
    },
    "tier_2_inner_tower": {
      "display_name": "Inner Tower",
      "max_hp": 4000.0,
      "base_armor": 55,
      "base_magic_resist": 55,
      "base_attack_damage": 160.0,
      "attack_range": 8.5,
      "fire_rate_seconds": 0.9,
      "damage_type": "Physical",
      "gold_bounty": 220,
      "exp_bounty": 200,
      "mechanics": {
        "is_heating_laser": true,
        "damage_increase_per_hit_percent": 25.0,
        "max_damage_multiplier": 3.0,
        "has_tower_plating": false,
        "aoe_gravity_slow_radius": 3.0,
        "aoe_slow_percent": 15.0
      }
    },
    "nexus_core": {
      "display_name": "The Nexus Core",
      "max_hp": 6000.0,
      "base_armor": 90,
      "base_magic_resist": 90,
      "base_attack_damage": 0.0,
      "damage_type": "None",
      "mechanics": {
        "can_attack": false,
        "hp_regeneration_out_of_combat": 12.0,
        "is_game_over_on_destruction": true,
        "requires_tier_2_destroyed": true
      }
    }
  }
}
```

**เงื่อนไขชนะ:** ต้องทำลาย Tier 1 → Tier 2 → จึงโจมตี Nexus ได้ (Nexus มี Damage Immunity จนกว่า Tier 2 ของฝั่งนั้นจะถูกทำลาย) ทำลาย Nexus = จบเกมทันที

---

## 4. ระบบเศรษฐกิจ (ใหม่ทั้งหมด — เดิมไม่มีเลย)

### 4.1 แหล่งรายได้ทองคำ
| แหล่งที่มา | จำนวน |
|---|---|
| Passive Gold (ต่อวินาที) | 2.0 gold/sec ตลอดเกม |
| Last-Hit Melee Creep | 42 gold |
| Last-Hit Ranged Creep | 50 gold |
| ทำลาย Tier 1 Tower | 150 gold |
| ทำลาย Tier 2 Tower | 220 gold |
| ฆ่าฮีโร่ศัตรู | Base 200 gold + (Killing Streak Bonus: +25 gold ต่อ streak, สูงสุด +150) |

- Last-Hit Gold ต้องตรวจจาก Damage Source ของฮีโร่และทีมผู้โจมตีโดยตรง หากครีปหรือป้อมเป็นผู้ปิดงานจะไม่เพิ่มทองให้ Wallet
- เมื่อผู้เล่น Last Hit สำเร็จ Presentation ต้องแสดงเหรียญทองแบบ world-space พร้อมจำนวน `+Gold` เหนือจุดที่ครีปตายและเล่นเสียงยืนยันสั้น โดยข้อมูลรางวัลต้องมาจาก Event ของ Simulation Core
- เมื่อฮีโร่ถูกกำจัด Kill Feed ต้องระบุทีม/ชื่อผู้กำจัด ทีม/ชื่อผู้ถูกกำจัด และทองที่ผู้กำจัดได้รับจากเหตุการณ์นั้น

### 4.2 แหล่งที่มา EXP
- ฆ่า/ร่วมสังหารครีป: ให้ EXP ตามระยะที่อยู่ใกล้ (Area-based, ไม่ต้อง Last-hit เพื่อได้ EXP — ต่างจาก Gold ที่ต้อง Last-hit)
- ฆ่าฮีโร่ศัตรู: EXP ก้อนใหญ่ (เทียบเท่า EXP จากครีป ~8-10 ตัว) เพื่อชดเชยการไม่มีทีมช่วยสังหาร

### 4.3 Denying
เนื่องจากเป็น 1v1 ไม่มีเพื่อนร่วมทีมให้ deny แทน — **ระบบ Deny ไม่จำเป็นใน 1.0.0** (ตัดออกจาก scope ปัจจุบัน ไม่ต้องพัฒนา)

---

## 5. ระบบร้านค้า/ไอเทม (ใหม่ทั้งหมด — เดิมไม่มีเลย)

### 5.1 โครงสร้างพื้นฐาน
- **Item Slots:** 6 ช่องต่อฮีโร่ (มาตรฐาน MOBA)
- **รูปแบบ 1.0.0:** ไอเทมสำเร็จรูป ซื้อขายตรงในร้าน **ไม่มีระบบ Recipe/Crafting ต่อกันแบบซับซ้อน** (เลื่อนไป 2.0.0/3.0.0) เพื่อลด scope
- **ที่ตั้งร้านค้า:** เข้าถึงได้เฉพาะเมื่ออยู่ในระยะ Fountain Zone เท่านั้น (มาตรฐาน MOBA — บังคับให้กลับบ้านเพื่อช้อป)

### 5.2 รายการไอเทมเริ่มต้น (Draft — ปรับได้หลัง Playtest)

| ไอเทม | ราคา | ผลลัพธ์ |
|---|---|---|
| Iron Plate Bracer | 500g | +150 Max HP, +10 Armor |
| Focus Crystal | 550g | +100 Max Mana, +5% Cooldown Reduction |
| Kinetic Boots | 500g | +1.0 m/s Move Speed |
| Warblade Fang | 600g | +20 Attack Damage |
| Void Emblem | 550g | +50 Max HP, +15 Magic Resist |
| Overclock Core | 650g | +25% Attack Rate |
| Nullifying Cloak (Active) | 800g | กดใช้: ล้างสถานะ Debuff ทั้งหมด + Immune ต่อ CC 1.0 วินาที (Cooldown 60 วิ) |
| Gravity Anchor (Capstone) | 1800g | +300 Max HP, +30 Armor, +30 Magic Resist |

### 5.3 หมวดร้านค้าและขอบเขตสินค้า

- UI ร้านค้าต้องแบ่งหมวดอย่างน้อยเป็น Consumables, Attributes, Equipment, Miscellaneous และ Upgraded พร้อมหน้า All Items
- ไอเทม 1v1 Version 1.0.0 ยังคงเป็นรายการ 8 ชิ้นใน Section 5.2; หมวดที่ยังไม่มีสินค้าให้แสดงสถานะว่างอย่างชัดเจน ห้ามดึงสินค้าของโหมดอนาคตมาปะปน
- รายการสินค้าแต่ละชิ้นต้องแสดงชื่อ ราคา หมวด ผลค่าสถานะ และสถานะซื้อได้/ทองไม่พอ
- การคลิก UI ร้านค้าและ Inventory ต้องไม่ส่งคำสั่งเคลื่อนที่ไปยัง Simulation Core

---

## 6. Ability System — เติมค่าตัวเลขที่ขาด (Cooldown / Mana Cost)

*หมายเหตุ: Effect ของสกิลทั้งหมดคงตามคำบรรยายเดิมใน Section 10 ของเอกสารต้นฉบับ ที่นี่เติมเฉพาะค่าตัวเลขที่ขาดหายไป*

### 6.1 Vorkas (The Iron Vanguard — Melee Tank / Bruiser)
| Ability / Key | Type | Cooldown | Mana Cost | รายละเอียดความสามารถ |
|---|---|---|---|---|
| Passive: Anti-Energy Aura | Passive | — | — | ลด Magic Damage ที่ได้รับ 15% และลด Spell Power ศัตรูรอบตัว |
| **Q (Skill 1): Iron Cleave** | `SKILLSHOT_LINE` | 8.0s | 60 | ฟันคลื่นดาบเหล็กกล้าเป็นเส้นตรงระยะ 6.0m กว้าง 1.5m ทำกายภาพดาเมจ 75/125/175/225 (+80% AD) |
| **W (Skill 2): Vanguard's Will** | `SELF_CAST` | 14.0s | 75 | ปลุกจิตวิญญาณแห่งแนวหน้า ได้รับบาเรียดูดซับ 100/160/220/280 HP นาน 4.0s และวิ่งเร็วขึ้น +20% |
| **E (Skill 3): Seismic Slam** | `GROUND_TARGET_AOE` | 10.0s | 70 | กระทืบพื้นสร้างคลื่นสั่นสะเทือนรัศมี 3.5m ทำกายภาพดาเมจ 80/130/180/230 (+60% AD) และ Slow ศัตรู 40% นาน 2.5s |
| **R (Ultimate): Rebellion Impact** | `GROUND_TARGET_AOE` | 90s/80s/70s | 100 | พุ่งกระโดดฟาดดาบลงพื้นในระยะ 7.0m ระเบิดทำดาเมจ 250/375/500 (+120% AD) พร้อม Knockup ศัตรูลอยขึ้น 1.0s |

### 6.2 Zenthis (The Chrono Guardian — Ranged Time Mage / Controller)
| Ability / Key | Type | Cooldown | Mana Cost | รายละเอียดความสามารถ |
|---|---|---|---|---|
| Passive: Chrono Stasis | Passive | Internal CD 45s | — | เมื่อ HP ต่ำกว่า 20% จะหยุดนิ่งไร้เป้าหมาย 1.5s ป้องกันดาเมจทั้งหมดและฟื้นฟู HP 15% |
| **Q (Skill 1): Sacred Hourglass** | `GROUND_TARGET_AOE` | 12.0s | 90 | วางนาฬิกาทรายบิดเบือนเวลารัศมี 3.0m ในระยะ 8.0m ทำเวทดาเมจ 70/115/160/205 (+65% AD) และ Slow 35% |
| **W (Skill 2): Aura of Eternity** | `SELF_CAST` | 16.0s | 70 | เปิดมิติเวลาคุ้มครอง เพิ่มความเร็วโจมตี +30% และรีเจนเลือด 25/40/55/70 HP/s นาน 4.0s |
| **E (Skill 3): Temporal Rift** | `SKILLSHOT_LINE` | 9.0s | 65 | ยิงลำแสงกาลเวลาทะลวงเป็นเส้นตรงระยะ 8.0m ทำเวทดาเมจ 85/135/185/235 (+70% AD) และลด Attack Speed ศัตรูลง 40% นาน 3.0s |
| **R (Ultimate): Grand Rewind** | `SELF_CAST` | 120s/105s/90s | 150 | ย้อนเวลาทั้งตำแหน่งและพลังชีวิตของ Zenthis กลับไปสู่สภาวะเมื่อ 4.0 วินาทีก่อน พร้อมล้าง Debuff ทั้งหมด |

### 6.3 Korvax (The Ballista Sniper — Ranged Physical Marksman)
| Ability / Key | Type | Cooldown | Mana Cost | รายละเอียดความสามารถ |
|---|---|---|---|---|
| Passive: Momentum Piercer | Passive | — | — | ยิงเป้าหมายเดิมซ้อนกันจะสะสม Stack เจาะเกราะ (Armor Shred 3% ต่อ Stack, สูงสุด 5 Stacks) |
| **Q (Skill 1): Heavy Bolt** | `SKILLSHOT_LINE` | 10.0s | 60 | ยิงลูกเกาทัณฑ์หนักระยะ 10.0m ทำดาเมจกายภาพ 90/145/200/255 (+90% AD) แก่เป้าหมายแรกที่ขวาง |
| **W (Skill 2): Hunter's Focus** | `SELF_CAST` | 18.0s | 50 | รวบรวมสมาธินักล่า เพิ่มระยะโจมตี +2.5m และเพิ่มพลังโจมตี +20/30/40/50 AD นาน 5.0s |
| **E (Skill 3): Concussive Blast** | `SINGLE_TARGET` | 11.0s | 65 | ยิงกระสุนระเบิดผลักเป้าหมายตรงหน้าระยะ 4.0m ให้กระเด็นถอยหลัง 3.5m ทำกายภาพดาเมจ 70/110/150/190 (+50% AD) และ Slow 40% นาน 2.0s |
| **R (Ultimate): Ballista Overdrive** | `SKILLSHOT_LINE` | 100s/85s/70s | 120 | ชาร์จยิงสไนเปอร์ความเร็วสูงระยะ 18.0m ทำดาเมจกายภาพมหาศาล 300/450/600 (+140% AD) ทะลวงครีปและหยุดที่ฮีโร่ตัวแรก |

### 6.4 Gravitor (The Gravity Singularity — Melee Tank / Disruptor)
| Ability / Key | Type | Cooldown | Mana Cost | รายละเอียดความสามารถ |
|---|---|---|---|---|
| Passive: Antigravity Shield | Passive | Internal CD 12s | — | เมื่อได้รับความเสียหาย สร้างเกราะดูดซับ 80 (+8% Max HP) นาน 3.0s |
| **Q (Skill 1): Magnetic Pull** | `SINGLE_TARGET` | 12.0s | 70 | ยิงสนามแม่เหล็กในระยะ 7.0m ดึงดูดเป้าหมายให้ลอยเข้ามาหา Gravitor ทำเวทดาเมจ 70/110/150/190 (+50% AD) |
| **W (Skill 2): Repulsion Zone** | `SELF_CAST` | 14.0s | 80 | ปล่อยคลื่นแรงโน้มถ่วงผลักศัตรูรอบตัวรัศมี 4.0m ให้กระเด็นออกไป 3.0m ทำเวทดาเมจ 80/125/170/215 (+55% AD) |
| **E (Skill 3): Graviton Well** | `GROUND_TARGET_AOE` | 10.0s | 75 | สร้างบ่อแรงโน้มถ่วงบนพื้นรัศมี 3.5m ในระยะ 6.0m ทำเวทดาเมจต่อเนื่อง 40/65/90/115 ทุก 0.5s และ Slow 50% นาน 2.5s |
| **R (Ultimate): Gravity Kore Collapse** | `GROUND_TARGET_AOE` | 110s/95s/80s | 150 | วางหลุมดำขนาดยักษ์ในระยะ 7.5m ดูดศัตรูทุกคนในรัศมี 4.5m เข้าสู่ศูนย์กลาง ทำเวทดาเมจ 260/390/520 (+100% AD) พร้อม Stun 1.5s |

### 6.5 Ability Target Type Taxonomy (จัดหมวดให้เป็นระบบ)
เพื่อให้ implement แบบ data-driven ได้ ทุกสกิลต้องจัดอยู่ใน 1 ใน 4 ประเภทนี้:
1. **`SKILLSHOT_LINE`** — ยิงเป็นเส้นตรงตามทิศ aimVector (เช่น Iron Cleave, Heavy Bolt, Ballista Overdrive, Temporal Rift)
2. **`GROUND_TARGET_AOE`** — เลือกพิกัดบนพื้น เกิด effect เป็นวงกลม (เช่น Sacred Hourglass, Graviton Well, Rebellion Impact, Gravity Kore Collapse)
3. **`SINGLE_TARGET`** — ล็อกเป้าหมายเดี่ยว (เช่น Magnetic Pull, Concussive Blast)
4. **`SELF_CAST`** — ใช้กับตัวเองหรือพื้นที่รอบตัวทันที ไม่ต้อง aim (เช่น Vanguard's Will, Seismic Slam, Hunter's Focus, Aura of Eternity, Repulsion Zone, Grand Rewind)

### 6.5.1 Combat Readability และ Resource Feedback

- Mana ฟื้นอัตโนมัติขณะมีชีวิตและอยู่นอก Fountain ที่ **1.25% ของ Max Mana ต่อวินาที**; Fountain regeneration ตาม Section 3.1 ยังคงเร็วกว่าและไม่ซ้อนกับค่านี้
- HUD ของ Q/W/E/R ต้องแยกสถานะ READY, COOLDOWN, NO MANA, UNLEARNED/LOCKED ห้ามแสดง READY เมื่อ Mana ไม่พอ
- เมื่อกดสกิลไม่สำเร็จ ต้องมีข้อความและเสียงตอบสนองที่ระบุสาเหตุ เช่น Mana ไม่พอ, ยังไม่เรียน หรือยังติด Cooldown; ข้อความนี้ต้องแสดงทีละรายการใน Ability Feedback บริเวณกึ่งกลางใต้ Top Bar แยกจาก Kill Feed และคำนวณ safe area เพื่อไม่ทับ Mini Map หรือแผงควบคุม
- สกิลทั้ง 16 ท่าต้องแยกอ่านได้ด้วยอย่างน้อย 3 มิติ: สีประจำชุด, รูปทรง/วิถี (slash, projectile, aura, ground glyph, vortex) และจังหวะ impact
- เอฟเฟกต์โจมตีจริงของฮีโร่ ป้อม และครีปต้องใช้ textured particle/projectile; เส้นเรขาคณิตให้ใช้เฉพาะ telegraph ที่ช่วยอ่านระยะและต้องไม่กลบเป้าหมาย
- Animation Idle, Walk, Attack, Cast, Death ต้องแยก state ชัดเจน การโจมตี/ร่ายเริ่มจาก Core success event เท่านั้น และต้องกลับสู่ neutral idle หลังจบ state

### 6.5.2 Movement Pace Baseline

| Unit | Base Move Speed |
|---|---:|
| Vorkas | 4.4 m/s |
| Zenthis | 4.2 m/s |
| Korvax | 4.5 m/s |
| Gravitor | 4.1 m/s |
| Lane Minion | 3.25 m/s |

ไอเทม, Talent, Buff และ Slow คำนวณต่อจากค่า Base นี้ตาม Section 5 และ Section 6.6 โดย visual locomotion ต้องปรับ playback rate ให้เท้าสัมพันธ์กับระยะเคลื่อนที่

### 6.5.3 Target Validation และ Resource Commitment

| Target Type | กฎก่อนใช้ Mana/Cooldown | ผลเมื่อไม่โดนศัตรู |
|---|---|---|
| `SINGLE_TARGET` | ต้องมีศัตรูมีชีวิตอยู่ใต้เคอร์เซอร์และอยู่ในระยะ; เป้าหมายว่าง, ฝ่ายเดียวกัน หรือนอกระยะต้อง Reject ก่อนหักทรัพยากร | ไม่ร่าย ไม่เสีย Mana และไม่เริ่ม Cooldown |
| `SKILLSHOT_LINE` | ต้องมีทิศทางเล็งที่ถูกต้อง; ไม่จำเป็นต้องล็อกเป้าหมาย | ยิงออกและใช้ Mana/Cooldown แม้ยิงพลาด |
| `GROUND_TARGET_AOE` | จุดศูนย์กลางต้องอยู่ใน Arena Bounds; ไม่จำเป็นต้องมีศัตรูอยู่ในพื้นที่ตอนกด | วางพื้นที่และใช้ Mana/Cooldown แม้ไม่มีศัตรูโดน |
| `SELF_CAST` | ผู้ร่ายต้องอยู่ในสถานะใช้สกิลได้ และเงื่อนไขภายในสกิลต้องพร้อม เช่น Grand Rewind ต้องมี snapshot | ใช้ Mana/Cooldownเมื่อ effect เริ่มทำงานสำเร็จเท่านั้น |

สำหรับ Version 1.0.0 สกิลที่บังคับ `SINGLE_TARGET` คือ **Gravitor Q: Magnetic Pull** และ **Korvax E: Concussive Blast**; HUD Tooltip ต้องระบุ Target Type ของ Q/W/E/R และ Ability Feedback ต้องแจ้ง `target required`, `out of range` หรือ `invalid ground` โดยไม่ใช้ Mana/Cooldown เมื่อ Reject

### 6.6 Talent Tree System (ใหม่ — เติม Spec ที่ Section 7.2 อ้างถึงแต่ยังไม่มีรายละเอียด)

ระบบ Talent เป็นแบบ **เลือก 1 ใน 2 ต่อจุดปลดล็อก** (Binary Choice) ถาวรตลอดแมตช์ ใช้ชุดเดียวกันทุกฮีโร่ (ไม่แยกต่อฮีโร่ เพื่อลด scope):

| ปลดล็อกที่ Level | ตัวเลือก A | ตัวเลือก B |
|---|---|---|
| Level 4 | +75 Max HP | +10% Cooldown Reduction |
| Level 8 | +15% Attack Speed | +15% ดาเมจจากสกิลทั้งหมด |
| Level 12 | +20% Move Speed | -10% ดาเมจที่ได้รับจากทุกแหล่ง |

กด `T` เพื่อเปิดหน้าต่างเลือก Talent เมื่อถึง Level ที่ปลดล็อก (มีจุดแจ้งเตือนบน HUD เมื่อเลือกได้)

### 6.7 Attribute Bonus System (ใหม่ — เติม Spec ที่ Section 7.2 อ้างถึงแต่ยังไม่มีรายละเอียด)

ทุกครั้งที่ Level Up ได้รับ **Attribute Point 1 แต้ม** (แยกจาก Skill Point) นำไปลงในสถิติได้อย่างอิสระผ่านหน้าต่าง `Ctrl+U`:

| ตัวเลือกลงแต้ม | ผลต่อ 1 แต้ม |
|---|---|
| Vitality | +2% ของ Max HP ปัจจุบัน |
| Focus | +2% ของ Max Mana ปัจจุบัน |
| Armor | +1 Armor |
| Resolve | +1 Magic Resist |

ลงแต้มสะสมได้ ไม่จำกัดจำนวนแต้มต่อหมวด (จำกัดแค่จำนวนแต้มรวมตาม Level ปัจจุบัน)

---

## 7. Controls & Camera — **PC-first สำหรับ 1.0.0** (ปรับปรุงล่าสุด)

**สถานะ:** 1.0.0 พัฒนาและปล่อยบน **PC เท่านั้น**เดิม Mobile Control Scheme ถูกเลื่อนไปเป็น **Version 1.1.0 (Mobile Port)** หลังจากพิสูจน์ Core Loop สำเร็จบน PC แล้ว เหตุผล: ทีมพัฒนาคนเดียว มือใหม่ Unity — ลดความเสี่ยง debug 2 แพลตฟอร์มพร้อมกันตั้งแต่ยังไม่ชำนาญ

Input Abstraction Layer (Section 1.1) ยังคงออกแบบไว้ตั้งแต่ต้นเหมือนเดิม เพื่อให้ตอน Port มือถือใน 1.1.0 ไม่ต้องรื้อ Simulation Core เลย — เพิ่มแค่ "Touch Input Adapter" ตัวใหม่ที่แปลงเป็น `{moveVector, castIntent, aimVector}` แบบเดียวกับที่ PC Adapter ทำอยู่แล้ว

### 7.1 กล้อง
- มุมกล้องคงที่ Top-down Perspective: pitch 60 องศา, vertical FOV 46 องศา และ presentation yaw -45 องศา ทำให้แนว Blue → Red พาดจากซ้ายล่างไปขวาบนบนหน้าจอแบบ Inspire MOBA โดยพิกัดสนามและ Simulation Core ยังคงแกนเดิม
- ระยะกล้องตามแนวแกนมองเริ่มต้น 24 เมตร และ Zoom ได้ 18-32 เมตร (scroll wheel) เพื่อให้เห็นพื้นที่ต่อสู้มากขึ้นโดยยังอ่านตัวละครกับเอฟเฟกต์ได้ชัด
- กล้องมีสองสถานะ: FREE สำหรับ Edge Pan/เลื่อนดูแผนที่ และ LOCKED สำหรับ follow ตัวละครแบบ soft-lerp พร้อม look-ahead
- กด Y หรือปุ่มสถานะบน HUD เพื่อสลับ FREE/LOCKED; กด Space สั้นเพื่อ Focus และกดค้างเพื่อ Lock ชั่วคราว
- Edge Pan ต้องแปลงทิศจาก screen-space ผ่านแกนระนาบของกล้อง เพื่อให้เลื่อนตามขอบจอได้ตรงทิศหลังใช้ presentation yaw

### 7.2 PC Control Scheme (Scope จริงของ 1.0.0)
- **เคลื่อนที่:** Right-click (Click-to-move) มาตรฐาน Inspire MOBA
- **โจมตี:** Left-click ที่ศัตรู หรือกด A แล้ว click (Attack-move)
- **สกิล (4 สกิลเต็มรูปแบบ สไตล์ Inspire MOBA):**
  - **Q:** Skill 1
  - **W:** Skill 2
  - **E:** Skill 3
  - **R:** Ultimate
  - **Ctrl + Q / W / E / R:** อัปเกรดระดับสกิลด่วน (Quick Skill Level-Up)
  - **Ctrl + U:** อัปเกรด Attribute Bonus (+Stats)
  - Aim ด้วยตำแหน่งเมาส์บนโลก (world-space mouse cursor)
  - **1–6:** ใช้งาน Active Items ในช่อง Inventory (6 ช่อง)
  - **P:** สลับเปิด/ปิดร้านค้า (เมื่ออยู่ใน Fountain Zone ตาม Section 5.1)
  - **T:** เปิดหน้าต่าง Talent Tree (เลเวล 4, 8, 12)
  - **F1:** เปิด/ปิด Hero Profile ที่แสดง Model, Role, Lore, Passive และรายละเอียด Q/W/E/R
- อินพุตเมาส์ที่อยู่บน HUD/Modal ต้องถูก consume โดย Presentation และห้ามทะลุไปเป็นคำสั่งเลือกเป้าหมายหรือเดิน

### 7.3 Mobile Control Scheme (เลื่อนไป Version 1.1.0 — เก็บ Spec ไว้ล่วงหน้า)
- **เคลื่อนที่:** Virtual Joystick มุมซ้ายล่าง
- **สกิล:** ปุ่มสกิลมุมขวาล่าง — `SKILLSHOT_LINE`/`GROUND_TARGET_AOE` ใช้ drag-to-aim, `SELF_CAST` แตะครั้งเดียวจบ และ `SINGLE_TARGET` ต้องแตะเลือกศัตรูที่มีชีวิตและอยู่ในระยะ; หากไม่มีเป้าหมายที่ถูกต้องต้อง Reject โดยไม่เสีย Mana/Cooldown ตาม Section 6.5.3

---

## 8. UI/HUD Scope สำหรับ 1.0.0 (ใหม่)

- HP/Mana Bar เหนือหัวตัวละคร (World-space billboard ตามที่ระบุในเอกสารต้นฉบับ)
- HP ศัตรูมองเห็นได้เฉพาะเมื่ออยู่ในระยะ Vision (ไม่เห็นตลอดเวลา — สอดคล้องกับกลไก Fog of War/Brush)
- แถบ Ability พร้อม Cooldown radial-fill indicator
- ตัวเลข Damage Popup ลอยขึ้นเมื่อโดนตี/โดนสกิล โดยแสดงค่าดาเมจจริงหลังการลดทอนเพียงหนึ่งครั้งต่อ hit; Presentation ต้องซ่อน popup เมื่อผู้โจมตีและเป้าหมายเป็นครีปทั้งคู่ แต่ยังแสดงดาเมจที่ฮีโร่หรือสิ่งปลูกสร้างเป็นผู้โจมตี และดาเมจที่ฮีโร่/สิ่งปลูกสร้างได้รับ
- แถบ Gold/Level/EXP มุมบนหน้าจอ
- Kill Feed แบบเรียบง่าย (ข้อความ "You / Enemy destroyed [Tower]" หรือ "You / Enemy has been slain")
- Kill Feed ของ Hero Elimination และ Tower Destruction ต้องแสดงจำนวนทองที่ได้รับ ส่วน Last Hit ครีปใช้เหรียญ world-space เพื่อไม่ทำให้ Feed เต็มจากเหตุการณ์ถี่
- Kill Feed อยู่กึ่งกลางใต้ Top Bar, จำกัดไม่เกิน 3 รายการและต้องล้างข้อความหมดเมื่อครบเวลา เพื่อไม่ทับ Mini Map; Ability Feedback ไม่ถูกเพิ่มเข้ารายการนี้
- Low-HP Vignette Warning เมื่อ HP ต่ำกว่า 25%
- **Mini Map มุมซ้ายบน:** แสดงขอบสนาม, เลน, Fountain, Tower, Nexus, ครีป และฮีโร่ โดยตำแหน่งศัตรูต้องเคารพกฎ Vision/Brush ตาม Section 3.1 และ Section 8
- Mini Map ต้องมีปุ่มย่อ/ขยาย, คลิกซ้ายเพื่อเลื่อนกล้อง และคลิกขวาเพื่อส่งคำสั่งเดินไปยังตำแหน่งที่แปลงกลับเป็น world-space ภายใน Arena Bounds
- แผง Hero & Bot Controls ต้องย่อ/ขยายได้ และมีปุ่ม/ข้อความสถานะกล้อง FREE/LOCKED
- Hover Q/W/E/R ต้องแสดง Tooltip ที่มีชื่อสกิล, Mana Cost, Base Cooldown และคำอธิบายผลเต็ม
- กด F1 ต้องเปิด Hero Profile ตาม Section 7.2 โดยมีภาพ Model แบบ render สด, Lore, Role, Passive และรายละเอียด Q/W/E/R

---

## 9. FSM AI Bot — ปรับให้เข้ากับแมพเลนเดียว

คง State Machine เดิมทั้งหมด (`STATE_IDLE`, `STATE_LANE_PUSHING`, `STATE_ATTACKING_HERO`, `STATE_RETREATING`) และ Difficulty Tier (Easy/Medium/Hard) ตาม Section 3 ของเอกสารต้นฉบับ **ไม่มีการเปลี่ยนแปลง** — เพราะ Bot ถูกออกแบบมาสำหรับ "เลนเดียว" อยู่แล้วโดยธรรมชาติ (เดิมมี logic หาเลนที่ใกล้ที่สุด ซึ่งในกรณีเลนเดียวจะลดความซับซ้อนลงจริง ไม่ต้องเพิ่มงาน)

---

## 10. Development Lifecycle — ปรับ Phase-01 ให้ตรง Scope ใหม่

### 🛠️ Phase-00: Product Constitution
คงเดิม + เพิ่ม: Lock Engine เป็น Unity 3D URP, ตั้งค่า Input Abstraction Layer เป็นรากฐานแรกก่อนเขียนระบบอื่น

### 🛠️ Phase-01: Core Mechanics & 1v1 Duel (Version 1.0.0)
**Deliverables ที่ปรับปรุงแล้ว:**
- Simulation Engine (Transform Vectors, 3D Hitboxes) — คงเดิม
- Input Abstraction Layer รองรับทั้ง PC และ Touch พร้อมกัน (ใหม่)
- FSM Bot 3 Difficulty Tier บนแมพเลนเดียว
- ระบบ Leveling/EXP ตาม Section 2.2
- ระบบเศรษฐกิจ (Gold/EXP sources) ตาม Section 4
- ระบบร้านค้า/ไอเทม 8 รายการตาม Section 5
- ระบบสกิลครบ 16 ความสามารถ (4 ฮีโร่ x 4 สกิล) พร้อมค่า Cooldown/Mana
- โครงสร้างแมพ: 2 Tower + Nexus ต่อข้าง, Bush 2 จุด, Fountain 2 ฝั่ง
- UI/HUD ตาม Section 8

### 🛠️ Phase-02 ถึง Phase-05
คงตามเอกสารต้นฉบับ — เมื่อถึงเวลาต้องพิจารณาเพิ่ม **5v5 mode, 3-เลน, ป่า/Neutral Monster** เข้าไปใน scope ของเวอร์ชันเหล่านั้น ซึ่งสถาปัตยกรรมที่วางไว้ใน 1.0.0 ทั้งหมด (Economy, Item, Ability Taxonomy) ถูกออกแบบให้ขยายรองรับได้โดยไม่ต้องรื้อใหม่

---

## 11. Acceptance Criteria — Phase-01 (1.0.0) ถือว่า "เสร็จ" เมื่อ

1. ผู้เล่นเริ่มแมตช์ 1v1 กับ AI Bot ได้ครบ 3 ระดับความยาก (Easy/Medium/Hard) ตั้งแต่ Spawn จนถึงหน้าจอชนะ/แพ้
2. ฮีโร่ทั้ง 4 ตัวมีสกิลครบ **4 ท่า (Q/W/E/R) + Passive** ทำงานถูกต้องตามค่าที่ระบุใน Section 6.1-6.4
3. ระบบร้านค้าซื้อ/ขายไอเทมได้ สถิติเปลี่ยนแปลงถูกต้องแบบ real-time
4. **ระบบ Talent Tree (Section 6.6) เลือกได้ที่ Level 4/8/12 และ Attribute Bonus (Section 6.7) ลงแต้มได้ทุกครั้งที่ Level Up ผลลัพธ์ตรงตามสเปก**
5. **Tower Escalation System (Section 3.2) เกิด Cannon Minion และ Super Creep ถูกต้องเมื่อทำลายป้อมแต่ละ Tier**
6. Input ทำงานถูกต้องทั้งบน PC build ขั้นต่ำ 1 เครื่อง (Mobile เลื่อนไป 1.1.0)
7. รักษาเฟรมเรตอย่างน้อย 30 FPS บนสเปกขั้นต่ำ (ตาม Section device_specs_config เดิม) และ 60 FPS บนสเปกแนะนำ
8. แมตช์ทดสอบต่อเนื่อง 20 นาทีไม่มี Critical Crash

---

## 12. รายการที่เลื่อนออกจาก 1.0.0 อย่างชัดเจน (Deferred Scope)

- **Mobile Port (Touch Controls)** — เลื่อนไป Version 1.1.0 หลัง 1.0.0 พิสูจน์ตัวบน PC สำเร็จ
- Mode 5v5 และแผนที่ 3 เลน
- Jungle Camp / Neutral Monster
- ระบบ Deny
- Item Recipe/Crafting ต่อกัน
- Networking/Matchmaking (Cloudflare/Supabase) — ยังคงอยู่ใน Phase-02/03 ตามเดิม
- Monetization (ยังไม่ได้คุยในรอบนี้ — แนะนำให้หยิบมาคุยก่อนเริ่ม Phase-03)
- Pick/Ban Phase (ไม่จำเป็นสำหรับ 1v1 กับ AI)

### 12.1 Ranked 5v5 Economy & Vision — Future Specification (ยังไม่อนุมัติให้ลง 1v1)

รายการต่อไปนี้เป็นข้อกำหนดเบื้องต้นสำหรับ **Ranked 5v5 เท่านั้น** และต้องผ่าน Design/Balance Review แยกก่อน implementation:

- **Secret Shop:** อยู่ในพื้นที่เสี่ยงบนแผนที่ Ranked 5v5; สินค้าหมวด Secret Shop ซื้อจาก Fountain/Base Shop ไม่ได้ และไม่ปรากฏใน 5v5 Normal
- **Ward System:** มีไอเทมตรวจการณ์แบบมองเห็นพื้นที่และแบบตรวจจับสิ่งพรางตัว กำหนดจำนวนคงคลัง, cooldown การเติม, ระยะ vision, อายุ และกฎการทำลายแยกจาก 1v1
- **Tree-consume Healing:** Consumable สำหรับใช้กับต้นไม้ที่ถูกต้องตามชนิดในแผนที่ เพื่อฟื้น HP แบบต่อเนื่อง; การใช้ต้องยกเลิกเมื่อรับความเสียหายตามค่าที่ Balance Review อนุมัติ
- **Gold Buyback:** ผู้เล่นที่ตายใน Ranked 5v5 สามารถจ่ายทองเพื่อเกิดใหม่ได้เมื่อผ่านสูตรราคาและ cooldown; ระบบต้องมี confirmation, แสดงราคาก่อนซื้อ และบันทึก telemetry เพื่อป้องกันการกดผิด
- ระบบทั้งสี่ต้องอยู่หลัง Mode Rule Set/feature flag ห้ามทำให้ catalog, economy หรือ respawn ของ Version 1.0.0 แบบ 1v1 เปลี่ยนตามโดยอัตโนมัติ

---

*เอกสารนี้เป็น Living Document — ค่าตัวเลขทั้งหมด (Cooldown, Mana, Gold, Item Price) เป็น Draft เริ่มต้นที่ออกแบบจากหลัก MOBA มาตรฐาน ต้องปรับจริงหลัง Playtest รอบแรกของ Phase-01*
