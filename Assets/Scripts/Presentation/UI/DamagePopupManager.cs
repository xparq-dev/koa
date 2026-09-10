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
        private AudioSource _rewardAudioSource;
        private AudioClip _rewardAudioClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _rewardAudioSource = gameObject.AddComponent<AudioSource>();
            _rewardAudioSource.playOnAwake = false;
            _rewardAudioSource.spatialBlend = 0f;
            _rewardAudioSource.volume = 0.5f;
            _rewardAudioClip = Resources.Load<AudioClip>("KOA/Audio/confirmation_002");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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

        /// <summary>
        /// แสดงเหรียญและจำนวนทอง ณ จุดที่ยืนยัน Last Hit หรือ Objective reward
        /// </summary>
        public void ShowGoldReward(Vector3 worldPosition, int amount)
        {
            if (amount <= 0) return;

            Vector3 spawnPos = worldPosition + new Vector3(
                Random.Range(-0.18f, 0.18f),
                Random.Range(2.0f, 2.3f),
                Random.Range(-0.18f, 0.18f));

            GameObject popupGo = new GameObject("GoldRewardPopup");
            popupGo.transform.position = spawnPos;
            popupGo.AddComponent<DamagePopup>().SetupGold(amount);

            if (_rewardAudioClip != null && _rewardAudioSource != null)
            {
                _rewardAudioSource.pitch = Random.Range(1.02f, 1.12f);
                _rewardAudioSource.PlayOneShot(_rewardAudioClip, 0.5f);
            }
        }
    }
}
