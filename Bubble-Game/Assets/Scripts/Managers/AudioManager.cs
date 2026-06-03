using UnityEngine;

namespace BubbleShooterPro.Managers
{
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips Mẫu (Gán trong Inspector)")]
        public AudioClip shootClip;
        public AudioClip bounceClip;
        public AudioClip popClip;
        public AudioClip winClip;
        public AudioClip loseClip;
        public AudioClip bgmClip;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                SetupAudioSources();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ApplySettings();
            
            // Tự động phát nhạc nền nếu được gán và cài đặt nhạc nền đang bật
            if (bgmClip != null)
            {
                PlayMusic(bgmClip);
            }
        }

        private void SetupAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.volume = 0.5f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = 0.8f;
            }
        }

        /// <summary>
        /// Đồng bộ âm lượng/mute từ SaveManager vào AudioSource.
        /// </summary>
        public void ApplySettings()
        {
            bgmSource.mute = !SaveManager.Instance.IsBgmOn;
            sfxSource.mute = !SaveManager.Instance.IsSfxOn;
        }

        /// <summary>
        /// Phát nhạc nền lặp đi lặp lại.
        /// </summary>
        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;
            
            bgmSource.clip = clip;
            if (!bgmSource.mute)
            {
                bgmSource.Play();
            }
        }

        /// <summary>
        /// Phát âm thanh hiệu ứng một lần.
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxSource.mute) return;
            sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Bật/Tắt nhạc nền.
        /// </summary>
        public void SetBgmOn(bool isOn)
        {
            SaveManager.Instance.IsBgmOn = isOn;
            bgmSource.mute = !isOn;
            if (isOn && !bgmSource.isPlaying && bgmClip != null)
            {
                bgmSource.Play();
            }
            else if (!isOn && bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }
        }

        /// <summary>
        /// Bật/Tắt âm thanh hiệu ứng.
        /// </summary>
        public void SetSfxOn(bool isOn)
        {
            SaveManager.Instance.IsSfxOn = isOn;
            sfxSource.mute = !isOn;
        }
    }
}
