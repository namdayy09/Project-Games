using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using BubbleShooterPro.Data;
using BubbleShooterPro.Managers;

namespace BubbleShooterPro.UI
{
    public class LevelSelectManager : MonoBehaviour
    {
        [Header("Giao diện Nút Level")]
        public GameObject buttonPrefab;      // Prefab của nút chọn Level
        public Transform buttonContainer;    // Container chứa Grid Layout

        private List<LevelData> _allLevels = new List<LevelData>();

        private void Start()
        {
            RefreshButtons();
        }

        /// <summary>
        /// Xóa danh sách nút cũ và sinh lại danh sách nút Level động dựa trên tiến trình chơi.
        /// </summary>
        public void RefreshButtons()
        {
            if (buttonContainer == null) return;
            if (buttonPrefab == null) return;

            foreach (Transform child in buttonContainer)
            {
                Destroy(child.gameObject);
            }

            int unlockedLevel = 1;

            if (SaveManager.Instance != null)
            {
                unlockedLevel = SaveManager.Instance.UnlockedLevel;
            }

            int totalLevels = 5;

            for (int i = 1; i <= totalLevels; i++)
            {
                int levelIndex = i;

                GameObject btnGo = Instantiate(buttonPrefab, buttonContainer);
                btnGo.name = $"LevelButton_{levelIndex}";

                TextMeshProUGUI btnText = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    btnText.text = levelIndex.ToString();
                }

                Button button = btnGo.GetComponent<Button>();
                if (button != null)
                {
                    bool isUnlocked = levelIndex <= unlockedLevel;
                    button.interactable = isUnlocked;
                    button.onClick.AddListener(() => OnLevelSelected(levelIndex));
                }
            }
        }

        /// <summary>
        /// Kích hoạt tải màn chơi tương ứng khi chọn nút Level.
        /// </summary>
        private void OnLevelSelected(int levelIndex)
        {
            if (AudioManager.Instance != null && AudioManager.Instance.bounceClip != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.bounceClip);
            }

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetSelectedLevelIndex(levelIndex);
            }

            SceneManager.LoadScene("GameScene");
        }
    }
}
