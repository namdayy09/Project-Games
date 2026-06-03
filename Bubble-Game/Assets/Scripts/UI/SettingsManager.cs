using UnityEngine;
using UnityEngine.UI;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.UI
{
    public class SettingsManager : MonoBehaviour
    {
        [Header("Toggles Âm Thanh")]
        public Toggle bgmToggle;
        public Toggle sfxToggle;

        [Header("Nút Chức Năng")]
        public Button resetProgressButton;
        public Button backButton;

        private void Start()
        {
            // Đồng bộ trạng thái Toggle với dữ liệu từ SaveManager
            if (SaveManager.Instance != null)
            {
                if (bgmToggle != null) bgmToggle.isOn = SaveManager.Instance.IsBgmOn;
                if (sfxToggle != null) sfxToggle.isOn = SaveManager.Instance.IsSfxOn;
            }

            // Gán sự kiện thay đổi giá trị
            if (bgmToggle != null) bgmToggle.onValueChanged.AddListener(OnBgmToggled);
            if (sfxToggle != null) sfxToggle.onValueChanged.AddListener(OnSfxToggled);

            if (resetProgressButton != null) resetProgressButton.onClick.AddListener(ResetProgress);
        }

        private void OnBgmToggled(bool value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetBgmOn(value);
            }
        }

        private void OnSfxToggled(bool value)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetSfxOn(value);
            }
        }

        /// <summary>
        /// Xóa sạch dữ liệu và khôi phục cài đặt gốc.
        /// </summary>
        public void ResetProgress()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ResetProgress();
                
                // Cập nhật lại UI sau khi reset
                if (bgmToggle != null) bgmToggle.isOn = SaveManager.Instance.IsBgmOn;
                if (sfxToggle != null) sfxToggle.isOn = SaveManager.Instance.IsSfxOn;
                
                Debug.Log("Tiến trình game đã được reset về mặc định.");
            }
            
            // Phát âm thanh dội nhẹ khi ấn reset
            PlayClickSound();
        }

        private void PlayClickSound()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.bounceClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.bounceClip);
            }
        }
    }
}
