using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MemoryMatchPro
{
    /// <summary>Main Menu UI – Play chuyển sang ModeSelect</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button     settingsCloseButton;
        [SerializeField] private SettingsUI settingsUI;

        [Header("Title Animation")]
        [SerializeField] private RectTransform titleRect;
        [SerializeField] private float bobAmplitude = 8f;
        [SerializeField] private float bobSpeed     = 1.5f;

        private Vector3 _titleBase;
        private float   _time;

        private void Awake()
        {
            playButton?.onClick.AddListener(OnPlayClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            exitButton?.onClick.AddListener(OnExitClicked);
            settingsCloseButton?.onClick.AddListener(OnSettingsClose);

            settingsPanel?.SetActive(false);

            if (titleRect != null) _titleBase = titleRect.anchoredPosition;
        }

        private void Update()
        {
            if (titleRect == null) return;
            _time += Time.deltaTime * bobSpeed;
            float y = Mathf.Sin(_time) * bobAmplitude;
            titleRect.anchoredPosition = _titleBase + new Vector3(0, y, 0);
        }

        private void OnPlayClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            // Chuyển sang Mode Select (không phải Level Select trực tiếp)
            LevelManager.Instance?.LoadModeSelect();
        }

        private void OnSettingsClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            settingsPanel?.SetActive(true);
            settingsUI?.RefreshUI();
        }

        private void OnSettingsClose()
        {
            AudioManager.Instance?.PlayButtonClick();
            settingsPanel?.SetActive(false);
        }

        private void OnExitClicked()
        {
            AudioManager.Instance?.PlayButtonClick();
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
