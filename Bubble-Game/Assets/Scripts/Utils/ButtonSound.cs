using UnityEngine;
using UnityEngine.UI;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.Utils
{
    [RequireComponent(typeof(Button))]
    public class ButtonSound : MonoBehaviour
    {
        private void Start()
        {
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(PlaySound);
            }
        }

        private void PlaySound()
        {
            if (AudioManager.Instance != null && AudioManager.Instance.bounceClip != null)
            {
                // Sử dụng âm thanh bounce làm âm click nút mặc định
                AudioManager.Instance.PlaySFX(AudioManager.Instance.bounceClip);
            }
        }
    }
}
