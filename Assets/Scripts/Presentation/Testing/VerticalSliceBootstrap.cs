using KOA.Presentation.Camera;
using KOA.Presentation.Input;
using KOA.Presentation.UI;
using KOA.Presentation.Views;
using UnityEngine;

namespace KOA.Presentation.Testing
{
    /// <summary>
    /// Auto Bootstrapper สำหรับทดสอบ Vertical Slice ใน Phase 1 & 2
    /// สร้าง Ground Plane, แสง, ฮีโร่ Vorkas, Dummy Target, UI Health Bar, และกล้อง ให้อัตโนมัติเมื่อกด Play
    /// </summary>
    public class VerticalSliceBootstrap : MonoBehaviour
    {
        [Header("Bootstrap Options")]
        [SerializeField] private bool autoBuildOnStart = true;

        private void Start()
        {
            if (autoBuildOnStart)
            {
                BuildArena();
            }
        }

        public void BuildArena()
        {
            Debug.Log("[Bootstrap] Initializing KOA Vertical Slice Test Arena...");

            // 1. ตรวจสอบหรือสร้าง Directional Light
            if (FindObjectOfType<Light>() == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                Light dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // 2. สร้าง Ground Plane ขนาด 70x20m (ตาม Section 3.1)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DuelArena_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(2.0f, 1.0f, 7.0f); // Plane มาตรฐานคือ 10x10 -> 20m x 70m
            Renderer groundRenderer = ground.GetComponent<Renderer>();
            if (groundRenderer != null)
            {
                groundRenderer.material.color = new Color(0.22f, 0.25f, 0.24f); // สีพื้นเลนหม่น
            }

            // 3. สร้าง DamagePopupManager
            if (FindObjectOfType<DamagePopupManager>() == null)
            {
                GameObject managerGo = new GameObject("DamagePopupManager");
                managerGo.AddComponent<DamagePopupManager>();
            }

            // 4. สร้าง Dummy Target (Capsule สีแดง)
            GameObject dummyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummyGo.name = "DummyTarget";
            dummyGo.transform.position = new Vector3(0, 1.0f, 8.0f);

            GameObject dummyBarGo = CreateHealthBarVisual(dummyGo.transform, "DummyHealthBar", Color.red);
            WorldSpaceHealthBar dummyHealthBar = dummyBarGo.GetComponent<WorldSpaceHealthBar>();

            DummyTargetView dummyView = dummyGo.AddComponent<DummyTargetView>();

            // 5. สร้าง Hero Vorkas (Capsule สีฟ้า/เทาเข้ม)
            GameObject heroGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            heroGo.name = "Hero_Vorkas";
            heroGo.transform.position = new Vector3(0, 1.0f, 0);
            Renderer heroRenderer = heroGo.GetComponent<Renderer>();
            if (heroRenderer != null)
            {
                heroRenderer.material.color = new Color(0.15f, 0.45f, 0.85f);
            }

            // เพิ่ม VFX LineRenderer สำหรับ Iron Cleave
            LineRenderer lineRenderer = heroGo.AddComponent<LineRenderer>();
            lineRenderer.startWidth = 1.5f;
            lineRenderer.endWidth = 1.5f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = new Color(0.3f, 0.85f, 1.0f, 0.7f);
            lineRenderer.endColor = new Color(0.3f, 0.85f, 1.0f, 0.1f);
            lineRenderer.enabled = false;

            // เพิ่ม PCInputAdapter
            PCInputAdapter inputAdapter = heroGo.AddComponent<PCInputAdapter>();

            // เพิ่ม UI Health Bar
            GameObject heroBarGo = CreateHealthBarVisual(heroGo.transform, "HeroHealthBar", Color.green);
            WorldSpaceHealthBar heroHealthBar = heroBarGo.GetComponent<WorldSpaceHealthBar>();

            // เพิ่ม HeroView
            HeroView heroView = heroGo.AddComponent<HeroView>();

            // 6. ตั้งค่ากล้อง Main Camera พร้อม TopDownCameraController (Section 7.1)
            UnityEngine.Camera mainCam = UnityEngine.Camera.main;
            if (mainCam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                mainCam = camGo.AddComponent<UnityEngine.Camera>();
                camGo.tag = "MainCamera";
            }

            TopDownCameraController camController = mainCam.GetComponent<TopDownCameraController>();
            if (camController == null)
            {
                camController = mainCam.gameObject.AddComponent<TopDownCameraController>();
            }
            camController.SetTarget(heroGo.transform);

            Debug.Log("[Bootstrap] Duel Arena Ready! Click Right-Mouse to Move, Left-Mouse/A to Attack, Q for Iron Cleave.");
        }

        private GameObject CreateHealthBarVisual(Transform parentTarget, string barName, Color barColor)
        {
            GameObject barRoot = new GameObject(barName);
            WorldSpaceHealthBar wsHealthBar = barRoot.AddComponent<WorldSpaceHealthBar>();
            wsHealthBar.BindTarget(parentTarget);

            // สร้างพื้นหลังสีดำของหลอดเลือด
            GameObject bgQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bgQuad.name = "Background";
            bgQuad.transform.SetParent(barRoot.transform);
            bgQuad.transform.localPosition = Vector3.zero;
            bgQuad.transform.localScale = new Vector3(1.2f, 0.18f, 1f);
            Destroy(bgQuad.GetComponent<Collider>());
            Renderer bgRenderer = bgQuad.GetComponent<Renderer>();
            bgRenderer.material = new Material(Shader.Find("Sprites/Default"));
            bgRenderer.material.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // สร้างหลอดเลือดสี
            GameObject fillQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fillQuad.name = "Fill";
            fillQuad.transform.SetParent(barRoot.transform);
            fillQuad.transform.localPosition = new Vector3(0, 0, -0.01f);
            fillQuad.transform.localScale = new Vector3(1.16f, 0.14f, 1f);
            Destroy(fillQuad.GetComponent<Collider>());
            Renderer fillRenderer = fillQuad.GetComponent<Renderer>();
            fillRenderer.material = new Material(Shader.Find("Sprites/Default"));
            fillRenderer.material.color = barColor;

            return barRoot;
        }
    }
}
