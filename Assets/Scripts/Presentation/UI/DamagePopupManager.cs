using KOA.Data.Enums;
using UnityEngine;

namespace KOA.Presentation.UI
{
    /// <summary>
    /// ผู้จัดการสร้าง Damage Popup กลางใน Scene (Singleton) ตาม Section 8
    /// </summary>
    public class DamagePopupManager : MonoBehaviour
    {
        public static DamagePopupManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// แสดง Damage Popup ลอยขึ้น ณ ตำแหน่งที่กำหนด
        /// </summary>
        public void ShowDamage(Vector3 worldPosition, float damageAmount, DamageType damageType)
        {
            // สุ่มกระจายตำแหน่งเล็กน้อยเพื่อไม่ให้ตัวเลขทับกันเป๊ะๆ
            Vector3 spawnPos = worldPosition + new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(1.8f, 2.2f),
                Random.Range(-0.3f, 0.3f)
            );

            GameObject popupGo = new GameObject("DamagePopup");
            popupGo.transform.position = spawnPos;

            DamagePopup popup = popupGo.AddComponent<DamagePopup>();
            popup.Setup(damageAmount, damageType);
        }
    }
}
