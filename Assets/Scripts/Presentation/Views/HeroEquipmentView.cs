using System.Collections.Generic;
using KOA.Core.Entities;
using UnityEngine;
using UnityEngine.Rendering;

namespace KOA.Presentation.Views
{
    /// <summary>
    /// Presentation-only equipment pass: เพิ่มเกราะ เสื้อคลุม และอาวุธที่ติดตาม Humanoid bones
    /// โดยไม่เพิ่มข้อมูลอุปกรณ์เข้า Simulation Core ตาม Section 1.1
    /// </summary>
    public sealed class HeroEquipmentView : MonoBehaviour
    {
        private readonly List<Material> _materials = new List<Material>();
        private Material _primary;
        private Material _secondary;
        private Material _metal;
        private Material _energy;
        private Animator _animator;

        public void Bind(HeroBase3D hero)
        {
            if (hero == null) return;
            _animator = GetComponentInChildren<Animator>(true);
            CreatePalette(hero.DisplayName, hero.TeamId);
            CreateArmor(hero.DisplayName);
            CreateWeapon(hero.DisplayName);
        }

        private void CreatePalette(string heroName, int teamId)
        {
            Color primary;
            Color secondary;
            Color energy;
            switch (heroName)
            {
                case "Zenthis":
                    primary = new Color(0.24f, 0.16f, 0.38f);
                    secondary = new Color(0.78f, 0.62f, 0.20f);
                    energy = new Color(0.80f, 0.42f, 1f);
                    break;
                case "Korvax":
                    primary = new Color(0.10f, 0.20f, 0.16f);
                    secondary = new Color(0.45f, 0.28f, 0.13f);
                    energy = new Color(0.20f, 1f, 0.58f);
                    break;
                case "Gravitor":
                    primary = new Color(0.18f, 0.12f, 0.24f);
                    secondary = new Color(0.34f, 0.38f, 0.42f);
                    energy = new Color(0.72f, 0.20f, 1f);
                    break;
                default:
                    primary = new Color(0.16f, 0.22f, 0.32f);
                    secondary = new Color(0.48f, 0.13f, 0.08f);
                    energy = new Color(0.18f, 0.65f, 1f);
                    break;
            }

            if (teamId == 1) primary = Color.Lerp(primary, new Color(0.34f, 0.08f, 0.07f), 0.25f);
            _primary = CreateLitMaterial(primary, 0.32f, 0f);
            _secondary = CreateLitMaterial(secondary, 0.22f, 0f);
            _metal = CreateLitMaterial(new Color(0.34f, 0.38f, 0.42f), 0.72f, 0.65f);
            _energy = CreateEmissiveMaterial(energy, 3.2f);
        }

        private void CreateArmor(string heroName)
        {
            Transform chest = GetBoneOrFallback(HumanBodyBones.Chest, new Vector3(0f, 1.35f, 0f));
            Transform head = GetBoneOrFallback(HumanBodyBones.Head, new Vector3(0f, 1.78f, 0f));
            GameObject armorRoot = CreateWorldAlignedAttachment(chest, "HeroArmor");

            CreatePart(armorRoot.transform, "Breastplate", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.015f), new Vector3(0.48f, 0.42f, 0.22f), _primary);
            CreatePart(armorRoot.transform, "ChestEmblem", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.14f), new Vector3(0.18f, 0.22f, 0.035f), _energy);
            CreatePart(armorRoot.transform, "Belt", PrimitiveType.Cube, new Vector3(0f, -0.28f, 0f), new Vector3(0.52f, 0.10f, 0.24f), _secondary);
            CreatePart(armorRoot.transform, "Shoulder_L", PrimitiveType.Sphere, new Vector3(-0.32f, 0.13f, 0f), new Vector3(0.24f, 0.17f, 0.26f), _metal);
            CreatePart(armorRoot.transform, "Shoulder_R", PrimitiveType.Sphere, new Vector3(0.32f, 0.13f, 0f), new Vector3(0.24f, 0.17f, 0.26f), _metal);

            if (heroName == "Zenthis" || heroName == "Korvax")
            {
                CreatePart(armorRoot.transform, "TravelCloak", PrimitiveType.Cube, new Vector3(0f, -0.18f, -0.16f), new Vector3(0.46f, 0.72f, 0.045f), _primary);
                GameObject hoodRoot = CreateWorldAlignedAttachment(head, "HeroHood");
                CreatePart(hoodRoot.transform, "Hood", PrimitiveType.Sphere, new Vector3(0f, 0.02f, -0.015f), new Vector3(0.36f, 0.34f, 0.38f), _primary);
                CreatePart(hoodRoot.transform, "HoodOpening", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0.19f), new Vector3(0.22f, 0.19f, 0.025f), _secondary);
            }
            else
            {
                GameObject helmetRoot = CreateWorldAlignedAttachment(head, "HeroHelmet");
                CreatePart(helmetRoot.transform, "Helmet", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.34f, 0.29f, 0.34f), _metal);
                CreatePart(helmetRoot.transform, "Visor", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.18f), new Vector3(0.30f, 0.10f, 0.035f), _primary);
                if (heroName == "Vorkas")
                    CreatePart(helmetRoot.transform, "Crest", PrimitiveType.Cube, new Vector3(0f, 0.25f, -0.02f), new Vector3(0.07f, 0.28f, 0.30f), _secondary);
            }
        }

        private void CreateWeapon(string heroName)
        {
            Transform rightHand = GetBoneOrFallback(HumanBodyBones.RightHand, new Vector3(0.46f, 1.05f, 0f));
            GameObject weaponRoot = CreateWorldAlignedAttachment(rightHand, $"{heroName}_Weapon");

            switch (heroName)
            {
                case "Zenthis":
                    CreateStaff(weaponRoot.transform);
                    break;
                case "Korvax":
                    CreateCrossbow(weaponRoot.transform);
                    break;
                case "Gravitor":
                    CreateGravityMaul(weaponRoot.transform);
                    break;
                default:
                    CreateSword(weaponRoot.transform);
                    CreateShield();
                    break;
            }
        }

        private void CreateSword(Transform root)
        {
            CreatePart(root, "Grip", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0f), new Vector3(0.055f, 0.18f, 0.055f), _secondary);
            CreatePart(root, "Guard", PrimitiveType.Cube, new Vector3(0f, 0.34f, 0f), new Vector3(0.42f, 0.075f, 0.10f), _metal);
            CreatePart(root, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.88f, 0f), new Vector3(0.095f, 1.02f, 0.045f), _metal);
            CreatePart(root, "Rune", PrimitiveType.Cube, new Vector3(0f, 0.84f, 0.05f), new Vector3(0.025f, 0.55f, 0.018f), _energy);
            CreatePart(root, "Pommel", PrimitiveType.Sphere, new Vector3(0f, -0.08f, 0f), Vector3.one * 0.13f, _energy);
        }

        private void CreateShield()
        {
            Transform leftHand = GetBoneOrFallback(HumanBodyBones.LeftHand, new Vector3(-0.46f, 1.05f, 0f));
            GameObject root = CreateWorldAlignedAttachment(leftHand, "Vorkas_Shield");
            GameObject shield = CreatePart(root.transform, "Shield", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.42f, 0.07f, 0.52f), _primary);
            shield.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePart(root.transform, "ShieldBoss", PrimitiveType.Sphere, new Vector3(0f, 0f, 0.085f), Vector3.one * 0.18f, _energy);
        }

        private void CreateStaff(Transform root)
        {
            CreatePart(root, "StaffShaft", PrimitiveType.Cylinder, new Vector3(0f, 0.70f, 0f), new Vector3(0.055f, 0.82f, 0.055f), _secondary);
            CreatePart(root, "StaffCrown", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0f), Vector3.one * 0.30f, _metal);
            CreatePart(root, "ChronoCore", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0f), Vector3.one * 0.19f, _energy);
            GameObject ring = CreatePart(root, "ChronoRing", PrimitiveType.Cylinder, new Vector3(0f, 1.55f, 0f), new Vector3(0.34f, 0.035f, 0.34f), _energy);
            if (ring != null) ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void CreateCrossbow(Transform root)
        {
            root.localRotation = Quaternion.Euler(72f, 0f, 0f);
            CreatePart(root, "CrossbowStock", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.32f), new Vector3(0.12f, 0.12f, 0.82f), _secondary);
            GameObject leftLimb = CreatePart(root, "CrossbowLimb_L", PrimitiveType.Cube, new Vector3(-0.28f, 0.05f, 0.58f), new Vector3(0.58f, 0.055f, 0.08f), _metal);
            GameObject rightLimb = CreatePart(root, "CrossbowLimb_R", PrimitiveType.Cube, new Vector3(0.28f, 0.05f, 0.58f), new Vector3(0.58f, 0.055f, 0.08f), _metal);
            leftLimb.transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
            rightLimb.transform.localRotation = Quaternion.Euler(0f, -18f, 0f);
            CreatePart(root, "CrossbowBolt", PrimitiveType.Cylinder, new Vector3(0f, 0.13f, 0.58f), new Vector3(0.022f, 0.50f, 0.022f), _energy).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        private void CreateGravityMaul(Transform root)
        {
            CreatePart(root, "MaulHandle", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0.07f, 0.70f, 0.07f), _secondary);
            CreatePart(root, "MaulHead", PrimitiveType.Cube, new Vector3(0f, 1.28f, 0f), new Vector3(0.68f, 0.38f, 0.38f), _metal);
            CreatePart(root, "GravityCore", PrimitiveType.Sphere, new Vector3(0f, 1.28f, 0f), Vector3.one * 0.25f, _energy);
            CreatePart(root, "MaulCap", PrimitiveType.Sphere, new Vector3(0f, -0.18f, 0f), Vector3.one * 0.16f, _energy);
        }

        private Transform GetBoneOrFallback(HumanBodyBones bone, Vector3 fallbackLocalPosition)
        {
            Transform target = _animator != null && _animator.isHuman ? _animator.GetBoneTransform(bone) : null;
            if (target != null) return target;

            GameObject fallback = new GameObject($"{bone}_EquipmentAnchor");
            fallback.transform.SetParent(transform, false);
            fallback.transform.localPosition = fallbackLocalPosition;
            return fallback.transform;
        }

        private GameObject CreateWorldAlignedAttachment(Transform parent, string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = parent.position;
            root.transform.rotation = transform.rotation;
            root.transform.SetParent(parent, true);
            return root;
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            Renderer renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            return part;
        }

        private Material CreateLitMaterial(Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            _materials.Add(material);
            return material;
        }

        private Material CreateEmissiveMaterial(Color color, float intensity)
        {
            Material material = CreateLitMaterial(color, 0.55f, 0.15f);
            material.SetColor("_EmissionColor", color * intensity);
            material.EnableKeyword("_EMISSION");
            return material;
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials)
            {
                if (material != null) Destroy(material);
            }
            _materials.Clear();
        }
    }
}
