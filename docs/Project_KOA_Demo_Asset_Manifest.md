# Project KOA — Demo Asset Manifest

อัปเดตเมื่อ: 2026-09-09

## ขอบเขตการนำเข้า

- แหล่งไฟล์: `D:\Assets_Games`
- วิธีดำเนินการ: **คัดลอก** เฉพาะชุดที่เลือกเข้าโปรเจกต์ ไม่ย้ายและไม่ลบ ZIP/โฟลเดอร์ต้นฉบับ
- ปลายทางหลัก: `Assets/ThirdParty`
- ไฟล์ที่ Project KOA สร้างเพิ่ม: Material, Animator Controller และ Prefab อยู่ใน `Assets/Presentation/Art` และ `Assets/Resources/KOA/Demo`
- สถาปัตยกรรม: Asset ถูกเรียกผ่าน Presentation เท่านั้นตาม Requirement Section 1.1; Simulation Core ไม่อ้างอิงโมเดล, Material หรือ Unity prefab
- รอบนี้ทำเฉพาะ Unity Import/Compile และ validation; **ไม่ได้สั่ง Build**

## ชุดที่เลือกใช้ใน Demo

| ผู้สร้าง/แพ็ก | License จากไฟล์ที่แนบ | ไฟล์ที่เลือก | การใช้งานปัจจุบัน |
|---|---|---|---|
| Quaternius — Universal Base Characters Standard | CC0 1.0 | Male/Female full-body FBX และ texture | ฐานตัวละครและ Material สำรอง |
| Quaternius — Modular Character Outfits Fantasy Standard | CC0 1.0 | Male/Female Ranger และ Peasant | เก็บเป็นชุดทดลองสำหรับรอบประกอบตัวละคร; ยังไม่ใช้เป็นตัวหลักเพราะโมเดลชุดไม่รวมร่างกายครบทุกส่วน |
| Quaternius — Universal Animation Library 1/2 | CC0 1.0 | `UAL1_Standard.fbx`, `UAL2_Standard.fbx` | Neutral idle, walk, attack, cast และ death; เลือกเวอร์ชันไม่มี root motion เพราะ Core เป็นเจ้าของตำแหน่ง |
| Quaternius — Bestiary Dungeon Monsters Standard | QAL v1.0 | Imp และ Puglin พร้อม texture | ภาพตัวแทน Gravitor และครีปทั้ง 4 ประเภท |
| Quaternius — Stylized Nature MegaKit Standard | CC0 1.0 | FBX (Unity) ชุดฟรี 68 โมเดลและ texture | พุ่มไม้ ต้นไม้ สน และก้อนหินในสนาม Demo |
| Quaternius — Medieval Village MegaKit Standard | CC0 1.0 | Uneven Brick, Rock Trim และ Terrain Noise texture ความละเอียดสูง | พื้นสะพานหิน หน้าผาและวัสดุขอบเหว |
| Quaternius — ชุดโครงสร้างจากโฟลเดอร์ Ultimate Fantasy RTS | CC0 1.0 ตาม `License.txt` ที่แนบ | WatchTower, Temple, Wonder และ WallTower รวม 8 FBX | Outer Tower, Inner Tower, Nexus และ Fountain |
| Kenney — Fantasy UI Borders | CC0 1.0 | ใช้ `Double/Border/panel-border-000.png` จากชุด PNG | กรอบหลักของ Mini Map, Top Bar, Hero Panel, Action Console, Control Panel และ Shop; เก็บไฟล์ runtime ที่ `Assets/Resources/KOA/UI/panel-frame-double.png` |
| Kenney — Input Prompts 1.5A | CC0 1.0 | Prompt ที่ใช้กับปุ่มควบคุม KOA 18 ภาพ | เตรียมไว้สำหรับหน้าคู่มือ/คำใบ้ปุ่ม; ยังไม่ผูก runtime |
| Kenney — Particle Pack 1.1 | CC0 1.0 | 14 PNG แบบ transparent ที่คัดใช้จาก 80 ภาพ | Slash, projectile, impact, magic glyph, aura, flame, dirt, smoke และ gravity vortex ของฮีโร่ ป้อม และครีป |
| Kenney — RPG Audio | CC0 1.0 | 7 OGG จากชุด 50 เสียง | เสียงฟัน กระแทก กลไก และฝีเท้า; ผูกกับจังหวะ combat/locomotion |
| Kenney — Interface Sounds | CC0 1.0 | 8 OGG ที่คัดใช้ | เสียงเปิด/ปิด/ย่อ/ขยาย, toggle, confirmation และ error ของ HUD |

หมายเหตุ QAL: ใช้ในงานส่วนตัว การศึกษา และเชิงพาณิชย์ได้โดยไม่ต้องให้เครดิต แต่ห้ามนำ Asset ไปขายหรือแจกต่อในลักษณะ asset/asset pack แยกเดี่ยว รายละเอียดฉบับเต็มเก็บไว้ที่ `Assets/ThirdParty/Quaternius/Monsters/Dungeon/License_Standard.txt`

## Mapping ที่เห็นเมื่อกด Play

| ระบบ KOA | Demo visual |
|---|---|
| Vorkas | Superhero Male full-body + Melee animation controller |
| Zenthis | Superhero Female full-body + Caster animation controller |
| Korvax | Superhero Male full-body สีเข้ม + Ranged animation controller |
| Gravitor | Puglin |
| Melee / Super minion | Imp คนละขนาด |
| Ranged / Cannon minion | Puglin คนละขนาด |
| Outer Tower | Temple First Age Level 3 |
| Inner Tower | Temple Second Age Level 3 |
| Nexus | Wonder First Age Level 3 |
| Fountain | Temple Second Age Level 3 |
| Bush / forest / cliff edge | Bush, Common/Pine/Twisted/Dead Tree, Grass, Fern, Mushroom และ Rock จาก Nature MegaKit |
| ฮีโร่ทั้ง 4 | เกราะ เสื้อคลุม/หมวก และอาวุธเฉพาะตัวที่ผูกกับ Humanoid hand/chest/head bones |

ฮีโร่และครีปเหล่านี้เป็น **ภาพแทนสำหรับ Demo** เพื่อประเมินภาพรวม ไม่ใช่ character identity ฉบับ final เนื่องจากชุดฟรีมี archetype จำกัดและยังไม่มีโมเดลที่ออกแบบเฉพาะ Project KOA

## การตั้งค่า Import/Presentation

- โมเดลตัวละครและ animation ตั้งเป็น Humanoid เพื่อ retarget ไปยัง Animator Controller แยกตามบทบาท
- ปิด root motion; ตำแหน่งและการหมุนยังซิงค์จาก Simulation Core ตาม Section 1.1
- Texture UI ตั้งเป็น Sprite; normal map และ linear texture ตั้งตามชนิด; texture อื่นจำกัดขนาดสูงสุด 2048 และบีบอัดคุณภาพสูง
- สร้าง URP Lit Material ของ Project KOA และ remap material ที่ฝังมากับ FBX
- สร้าง controller `KOA_HeroMelee`, `KOA_HeroCaster`, `KOA_HeroRanged` และ `KOA_Monster` พร้อม parameter `MoveSpeed`, `LocomotionRate`, `Attack`, `Cast`, `Dead`
- เลือกคลิป Idle แบบชื่อ exact เพื่อป้องกัน `Crouch_Idle` ถูกเลือกแทน `Idle_Loop`; ความเร็วคลิปเดินสัมพันธ์กับความเร็วจาก Simulation Core
- Presentation ใช้ textured particle/projectile จาก Core success event สำหรับ Basic Attack และ Q/W/E/R ครบทั้ง 4 ฮีโร่ รวมถึง Tower/Minion attack; ground glyph คงไว้เฉพาะการอ่านพื้นที่ตาม Requirement Section 6.1–6.5 โดยไม่ย้าย logic เข้า Presentation
- Generated humanoid controller ใช้ neutral `Idle_Loop` และ `Walk_Loop`; ท่าถือเวท/เล็งอาวุธทำงานเฉพาะ state Attack/Cast และกลับ neutral idle หลังจบ
- Source archive checksum: Particle Pack ZIP SHA-256 `B631D4B07F7002549FDCF155F01141AD482F79F3440E4E301EED49CE5F1D8958`; RPG Audio ZIP SHA-256 `3AE398AD63E293F9C450BDA22D5D81C3AF69C74DF66FC1400F33C012C0BBC231`
- สนาม Demo เป็นสะพานป่าหินเหนือเหว มีพื้นอิฐเก่า/ดิน/มอส, หน้าผาลึก, โครงค้ำสะพาน, กำแพงแตก, หมอก, เตาไฟ และพืชหนาแน่น โดยพื้นที่เล่นจริงยังคง 130x26 เมตรตาม Section 3.1
- ขยาย URP Lit Material remap ให้ Tower, Nexus, Fountain, พุ่มไม้, ต้นไม้หลายชนิด, หญ้า, เฟิร์น, เห็ด และหิน เพื่อไม่ให้แสดงเป็นสีชมพูจาก shader ที่ไม่รองรับ URP
- เพิ่ม Arena surface materials 5 รายการ พร้อม normal map ของ Uneven Brick/Rock Trim, anisotropic filtering, shadow คุณภาพสูง และ atmospheric fog
- เพิ่ม `HeroEquipmentView` ฝั่ง Presentation เพื่อประกอบ Breastplate, Belt, Shoulder, Hood/Helmet/Cloak และอาวุธ Vorkas Sword+Shield, Zenthis Staff, Korvax Crossbow, Gravitor Gravity Maul โดยไม่แตะ Simulation Core
- แก้ Tower/Nexus ให้แสดง FBX จริงเป็นค่าเริ่มต้น และใช้ primitive silhouette เฉพาะเมื่อ prefab สูญหาย เพื่อไม่ให้โครงสำรองบังโมเดล
- เพิ่มโลกชั้นล่างใต้สะพาน: lower-valley forest, river, waterfall, pool, cliff fog และกลุ่มเมฆที่เคลื่อนช้า เพื่อสื่อระดับความสูงจากมุมกล้องด้านบน
- แก้จากวิดีโอ review วันที่ 2026-09-09: เปลี่ยนน้ำตกกล่องสีสว่างเป็นริบบิ้นโปร่งใสหลายชั้น, เปลี่ยนเมฆทรงกลมทึบเป็นแผ่นขอบนุ่มนอกพื้นที่เล่น, ลดก้อนหินริมหน้าผา และยกป่าชั้นล่างให้มองเห็นง่ายขึ้น
- แยกระดับพื้นหญ้า/พื้นเลน/โครงสะพานให้ห่างกัน และถอดวงรีคราบดิน/เส้นรอยแตกแบบ primitive ที่รบกวนภาพ เพื่อลด z-fighting และ placeholder noise
- แก้ตัวสร้าง prefab ให้ใช้ `Visual` wrapper เก็บสเกล FBX อย่างแน่นอน; ก่อนแก้ Tower renderer สูงเพียงประมาณ 0.04 เมตรแม้ collider ถูกต้อง
- ปรับสัดส่วน Demo visual ของ Outer Tower / Inner Tower / Nexus เป็น 3.6 / 4.3 / 5.4 เมตร ลดฐานวงกลมและ beacon พร้อมจัด health bar ให้สัมพันธ์กับความสูงใหม่
- หัน facade ของโครงสร้างเข้าหามุมกล้องมาตรฐาน เพราะโมเดลฟรีชุดที่เลือกมีด้านหลังเปิดและมืดกว่าด้านหน้า
- ตั้งระยะกล้องเริ่มต้นเป็น 13.5 เมตรภายในช่วง 8-14 เมตรตาม Requirement Section 7.1 เพื่อเห็นขอบสะพานและหุบเหวได้กว้างขึ้น
- Runtime โหลด prefab จาก `Resources/KOA/Demo` และย้อนกลับไปใช้ primitive เดิมได้หาก Asset ใดหาย
- ไม่ได้นำ third-party script, sample scene หรือ ProjectSettings จากแพ็กเข้ามา

## ชุดที่ยังไม่ใช้

- โมเดล modular ส่วนใหญ่ของ `Medieval Village MegaKit[Standard]`: ยังไม่คัดลอกเพื่อคุมขนาดโปรเจกต์; ใช้เฉพาะ texture CC0 ที่จำเป็นกับสะพานและหน้าผา
- รูปแบบ OBJ, glTF, Blend และไฟล์ animation แบบ root motion: ไม่คัดลอก เพราะ Unity ใช้ FBX และ Decoupled Core ต้องควบคุม movement เอง
- โมเดล/สีที่อยู่นอก free Standard subset: ไม่ได้ใช้งาน
- `WallTowers_FirstAge`, `WallTowers_SecondAge` และ structure variant ฝั่ง Second Age บางชิ้นถูกเก็บใน ThirdParty ไว้เป็นตัวเลือก แต่ยังไม่ถูก instantiate ในสนามปัจจุบัน

## แหล่ง Asset ฟรีที่แนะนำสำหรับรอบถัดไป

- [Kenney](https://kenney.nl/assets): UI, VFX texture และเสียงจำนวนมากแบบ CC0; รอบนี้ใช้ Particle Pack, RPG Audio และ Interface Sounds
- [Quaternius](https://quaternius.com/): โมเดล/Animation 3D ฟรีแบบ CC0; เหมาะกับ Demo และ blockout คุณภาพสูง
- [Poly Haven](https://polyhaven.com/): PBR texture, HDRI และโมเดล 3D แบบ CC0; เหมาะกับหิน หน้าผา พื้น และแสงสภาพแวดล้อม
- [Unity Asset Store — Stylized Slash VFX](https://assetstore.unity.com/packages/vfx/particles/stylized-slash-vfx-200233): Free และรองรับ URP ตามหน้าสินค้า แต่ใช้ Standard Unity Asset Store EULA จึงต้องนำเข้าผ่านบัญชีของ Owner และห้ามแจกไฟล์ต้นฉบับแยก
- [Unity Asset Store — Free Game VFX Magic Circle URP](https://assetstore.unity.com/packages/vfx/particles/free-game-vfx-magic-circle-urp-344984): ชุดวงเวทสำหรับเปลี่ยน Demo particle ในรอบ art polish หลัง Owner กด Add to My Assets
- [Unity Asset Store — Area of Effect Spell FREE](https://assetstore.unity.com/packages/vfx/particles/spells/area-of-effect-spell-free-287652): ชุด AOE สำหรับทดแทน ground glyph เมื่อผ่านการตรวจ URP และ license แล้ว

หลักการคัดเลือก: เลือก CC0 ก่อน; ถ้าเป็น Asset Store ให้ Owner นำเข้าผ่าน My Assets แล้วจึงคัดเฉพาะส่วนที่ใช้ พร้อมบันทึกชื่อแพ็ก เวอร์ชัน URL และ License ใน Manifest นี้

## ข้อควรทำก่อน Release

1. เก็บ ZIP ต้นฉบับและไฟล์ License ไว้เป็นหลักฐานแหล่งที่มา
2. เปลี่ยน demo hero identity เป็นโมเดล/portrait/VFX ที่ออกแบบเฉพาะ Project KOA ก่อน final art lock
3. ตรวจความชัดของกรอบ UI และปรับระดับเสียง VFX/UI จากของที่ integrate แล้วบนความละเอียดจอเป้าหมายใน Phase 4
4. ตรวจ draw calls, texture memory, animation และ frame time ด้วย Unity Profiler บนเครื่องเป้าหมายตาม Roadmap Week 45
5. ทำ visual/playability review ใน Game View จริงก่อน Owner sign-off; automated import/compile ไม่สามารถยืนยันความสวยงามหรือ gameplay feel แทนคนได้
