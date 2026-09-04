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

**Stat Growth ต่อ Level (ค่าคงที่ ไม่ใช้ระบบ primary attribute แบบ DOTA เพื่อความง่ายใน 1.0.0):**

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

### 3.1 ขนาดและโครงสร้าง
- **รูปแบบ:** เลนเดียว แนวยาว (Corridor) เชื่อม Fountain ทั้งสองฝั่ง
- **ขนาดสนามโดยประมาณ:** ยาว 70 เมตร x กว้าง 20 เมตร (1 Grid Unit = 1 เมตร ตามที่กำหนดไว้เดิม)
- **Fountain Zone:** รัศมี 8 เมตร ที่ปลายแมพแต่ละฝั่ง มี HP/Mana regen สูงมาก (heal 20% Max HP/sec) และ invulnerability เมื่ออยู่ในโซน
- **Bush/พุ่มหญ้า:** วางไว้ 2 จุด สมมาตรกันบริเวณกึ่งกลางเลน (ไม่ใช่ป่าเต็มรูปแบบ) ใช้กลไก Fog of War brush เดิมจาก section 7.2 ของเอกสารต้นฉบับ (เปิดเผยตำแหน่งเมื่อโจมตีจากในพุ่ม 3.0 วินาที)
- **ไม่มี Jungle Camp / Neutral Monster** ใน 1.0.0 ตามที่ตัดสินใจไว้ — ออกแบบ layout ให้เหลือพื้นที่ว่างสองข้างเลนไว้เผื่อใส่ป่าใน Phase หลัง (รักษาความเป็นไปได้ในการขยาย)

### 3.2 Creep Spawner (ปรับจาก Section 7.1 เดิม)
- **ความถี่:** ทุก 25.0 วินาที (ลดจาก 30 วิเดิม เพราะมีเลนเดียว ต้องการจังหวะฟาร์มที่กระชับขึ้น)
- **รูปแบบเวฟ:** 2 Melee Minion + 1 Ranged Minion ต่อเวฟ (คงเดิม)
- **Evolution Scaling:** คงสูตรเดิม +10% Max HP / +5% AD ทุก 3 นาที

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

---

## 6. Ability System — เติมค่าตัวเลขที่ขาด (Cooldown / Mana Cost)

*หมายเหตุ: Effect ของสกิลทั้งหมดคงตามคำบรรยายเดิมใน Section 10 ของเอกสารต้นฉบับ ที่นี่เติมเฉพาะค่าตัวเลขที่ขาดหายไป*

### 6.1 Vorkas
| Ability | Cooldown | Mana Cost |
|---|---|---|
| Passive: Anti-Energy Aura | — (Passive) | — |
| Iron Cleave | 8.0s | 60 |
| Vanguard's Will | 14.0s | 80 |
| Ultimate: Rebellion Impact | 90s (ลดเหลือ 70s ที่ Level 3 ของสกิล) | 100 |

### 6.2 Zenthis
| Ability | Cooldown | Mana Cost |
|---|---|---|
| Passive: Chrono Stasis | Internal CD 45s (ป้องกันการใช้ถี่เกินไป) | — |
| Sacred Hourglass | 12.0s | 90 |
| Aura of Eternity | 16.0s | 70 |
| Ultimate: Grand Rewind | 120s | 150 |

### 6.3 Korvax
| Ability | Cooldown | Mana Cost |
|---|---|---|
| Passive: Momentum Piercer | — (Passive) | — |
| Heavy Bolt | 10.0s | 60 |
| Hunter's Focus | 18.0s | 50 |
| Ultimate: Ballista Overdrive | 100s | 120 |

### 6.4 Gravitor
| Ability | Cooldown | Mana Cost |
|---|---|---|
| Passive: Antigravity Shield | Internal CD 12s | — |
| Magnetic Pull | 12.0s | 70 |
| Repulsion Zone | 14.0s | 80 |
| Ultimate: Gravity Kore Collapse | 110s | 150 |

### 6.5 Ability Target Type Taxonomy (ใหม่ — จัดหมวดให้เป็นระบบ)
เพื่อให้ implement แบบ data-driven ได้ ทุกสกิลต้องจัดอยู่ใน 1 ใน 4 ประเภทนี้:
1. **`SKILLSHOT_LINE`** — ยิงเป็นเส้นตรงตามทิศ aimVector (เช่น Iron Cleave, Heavy Bolt)
2. **`GROUND_TARGET_AOE`** — เลือกพิกัดบนพื้น เกิด effect เป็นวงกลม (เช่น Sacred Hourglass, Rebellion Impact, Gravity Kore Collapse)
3. **`SINGLE_TARGET`** — ล็อกเป้าหมายเดี่ยว (เช่น Magnetic Pull)
4. **`SELF_CAST`** — ใช้กับตัวเองทันที ไม่ต้อง aim (เช่น Vanguard's Will, Hunter's Focus, Aura of Eternity ที่ผูกกับตัวเอง)

---

## 7. Controls & Camera — **PC-first สำหรับ 1.0.0** (ปรับปรุงล่าสุด)

**สถานะ:** 1.0.0 พัฒนาและปล่อยบน **PC เท่านั้น**เดิม Mobile Control Scheme ถูกเลื่อนไปเป็น **Version 1.1.0 (Mobile Port)** หลังจากพิสูจน์ Core Loop สำเร็จบน PC แล้ว เหตุผล: ทีมพัฒนาคนเดียว มือใหม่ Unity — ลดความเสี่ยง debug 2 แพลตฟอร์มพร้อมกันตั้งแต่ยังไม่ชำนาญ

Input Abstraction Layer (Section 1.1) ยังคงออกแบบไว้ตั้งแต่ต้นเหมือนเดิม เพื่อให้ตอน Port มือถือใน 1.1.0 ไม่ต้องรื้อ Simulation Core เลย — เพิ่มแค่ "Touch Input Adapter" ตัวใหม่ที่แปลงเป็น `{moveVector, castIntent, aimVector}` แบบเดียวกับที่ PC Adapter ทำอยู่แล้ว

### 7.1 กล้อง
- มุมกล้องคงที่ Top-down Isometric 50 องศา
- ระยะ Zoom: 8-14 เมตรจากพื้น (scroll wheel)
- กล้อง follow ตัวละครผู้เล่นแบบ soft-lerp พร้อม look-ahead เล็กน้อยตามทิศทางเคลื่อนที่

### 7.2 PC Control Scheme (Scope จริงของ 1.0.0)
- **เคลื่อนที่:** Right-click (Click-to-move) แบบ DOTA มาตรฐาน
- **โจมตี:** Left-click ที่ศัตรู หรือกด A แล้ว click (Attack-move)
- **สกิล:** Q / W / E สำหรับ Skill 1/2/Ultimate, aim ด้วยตำแหน่งเมาส์บนโลก (world-space)

### 7.3 Mobile Control Scheme (เลื่อนไป Version 1.1.0 — เก็บ Spec ไว้ล่วงหน้า)
- **เคลื่อนที่:** Virtual Joystick มุมซ้ายล่าง
- **สกิล:** ปุ่มสกิลมุมขวาล่าง — สกิลประเภท `SKILLSHOT_LINE`/`GROUND_TARGET_AOE` ใช้ drag-to-aim, สกิลประเภท `SELF_CAST`/`SINGLE_TARGET` แตะครั้งเดียวจบ (auto-target ศัตรูที่ใกล้ที่สุดในระยะ)

---

## 8. UI/HUD Scope สำหรับ 1.0.0 (ใหม่)

- HP/Mana Bar เหนือหัวตัวละคร (World-space billboard ตามที่ระบุในเอกสารต้นฉบับ)
- HP ศัตรูมองเห็นได้เฉพาะเมื่ออยู่ในระยะ Vision (ไม่เห็นตลอดเวลา — สอดคล้องกับกลไก Fog of War/Brush)
- แถบ Ability พร้อม Cooldown radial-fill indicator
- ตัวเลข Damage Popup ลอยขึ้นเมื่อโดนตี/โดนสกิล
- แถบ Gold/Level/EXP มุมบนหน้าจอ
- Kill Feed แบบเรียบง่าย (ข้อความ "You / Enemy destroyed [Tower]" หรือ "You / Enemy has been slain")
- Low-HP Vignette Warning เมื่อ HP ต่ำกว่า 25%
- **ไม่ต้องมี Minimap** ใน 1.0.0 (แผนที่เลนเดียวมองเห็นได้ทั้งหมดในมุมกล้องอยู่แล้ว)

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
2. ฮีโร่ทั้ง 4 ตัวมีสกิลครบ 4 ท่าทำงานถูกต้องตามค่าที่ระบุใน Section 6
3. ระบบร้านค้าซื้อ/ขายไอเทมได้ สถิติเปลี่ยนแปลงถูกต้องแบบ real-time
4. Input ทำงานถูกต้องทั้งบน PC build และ Android build ขั้นต่ำ 1 เครื่อง
5. รักษาเฟรมเรตอย่างน้อย 30 FPS บนสเปกขั้นต่ำ (ตาม Section device_specs_config เดิม) และ 60 FPS บนสเปกแนะนำ
6. แมตช์ทดสอบต่อเนื่อง 20 นาทีไม่มี Critical Crash

---

## 12. รายการที่เลื่อนออกจาก 1.0.0 อย่างชัดเจน (Deferred Scope)

- **Mobile Port (Touch Controls)** — เลื่อนไป Version 1.1.0 หลัง 1.0.0 พิสูจน์ตัวบน PC สำเร็จ
- Mode 5v5 และแผนที่ 3 เลน
- Jungle Camp / Neutral Monster
- ระบบ Deny
- Item Recipe/Crafting ต่อกัน
- Networking/Matchmaking (Cloudflare/Supabase) — ยังคงอยู่ใน Phase-02/03 ตามเดิม
- Monetization (ยังไม่ได้คุยในรอบนี้ — แนะนำให้หยิบมาคุยก่อนเริ่ม Phase-03)
- Minimap
- Pick/Ban Phase (ไม่จำเป็นสำหรับ 1v1 กับ AI)

---

*เอกสารนี้เป็น Living Document — ค่าตัวเลขทั้งหมด (Cooldown, Mana, Gold, Item Price) เป็น Draft เริ่มต้นที่ออกแบบจากหลัก MOBA มาตรฐาน ต้องปรับจริงหลัง Playtest รอบแรกของ Phase-01*
