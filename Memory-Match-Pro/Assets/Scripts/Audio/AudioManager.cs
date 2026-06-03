using UnityEngine;

namespace MemoryMatchPro
{
    /// <summary>
    /// Singleton quản lý âm thanh toàn cục.
    /// DontDestroyOnLoad – tồn tại xuyên suốt game.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip flipClip;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip wrongClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;

        private bool _soundEnabled = true;
        private bool _musicEnabled = true;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tạo AudioSource nếu chưa có
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0.5f;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.volume = 1f;
            }

            // Đọc settings từ SaveManager
            _soundEnabled = SaveManager.IsSoundEnabled();
            _musicEnabled = SaveManager.IsMusicEnabled();

            // Bắt đầu phát nhạc nền
            PlayBackgroundMusic();
        }

        // ==================== Music ====================

        public void PlayBackgroundMusic()
        {
            if (musicSource == null || backgroundMusic == null) return;

            musicSource.clip = backgroundMusic;
            musicSource.mute = !_musicEnabled;

            if (!musicSource.isPlaying)
                musicSource.Play();
        }

        public void SetMusicEnabled(bool enabled)
        {
            _musicEnabled = enabled;
            SaveManager.SetMusicEnabled(enabled);

            if (musicSource != null)
                musicSource.mute = !enabled;
        }

        public bool IsMusicEnabled() => _musicEnabled;

        // ==================== SFX ====================

        public void SetSoundEnabled(bool enabled)
        {
            _soundEnabled = enabled;
            SaveManager.SetSoundEnabled(enabled);
        }

        public bool IsSoundEnabled() => _soundEnabled;

        private void PlaySFX(AudioClip clip)
        {
            if (!_soundEnabled || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip);
        }

        public void PlayButtonClick() => PlaySFX(buttonClickClip);
        public void PlayFlip()        => PlaySFX(flipClip);
        public void PlayMatch()       => PlaySFX(matchClip);
        public void PlayWrong()       => PlaySFX(wrongClip);
        public void PlayWin()         => PlaySFX(winClip);
        public void PlayLose()        => PlaySFX(loseClip);

        // ==================== Volume ====================

        public void SetMusicVolume(float volume)
        {
            if (musicSource != null)
                musicSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null)
                sfxSource.volume = Mathf.Clamp01(volume);
        }
    }
}
