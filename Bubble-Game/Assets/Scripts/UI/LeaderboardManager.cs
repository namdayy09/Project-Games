using UnityEngine;
using TMPro;
using BubbleShooterPro.Managers;
using UnityEngine.UI;

namespace BubbleShooterPro.UI
{
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("Thành Phần Hiển Thị")]
        public TextMeshProUGUI leaderboardText;
        public Button backButton;

        private void OnEnable()
        {
            DisplayLeaderboard();
        }

        /// <summary>
        /// Phân tích dữ liệu và vẽ danh sách điểm cao.
        /// </summary>
        public void DisplayLeaderboard()
        {
            if (leaderboardText == null) return;

            if (SaveManager.Instance == null)
            {
                leaderboardText.text = "Không tìm thấy dữ liệu SaveManager.";
                return;
            }

            var entries = SaveManager.Instance.GetLeaderboard();
            if (entries == null || entries.Count == 0)
            {
                leaderboardText.text = "BẢNG XẾP HẠNG TRỐNG\nHãy hoàn thành các level để ghi danh!";
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("TOP 5 ĐIỂM CAO");
            sb.AppendLine("====================");

            for (int i = 0; i < entries.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {entries[i].playerName} - {entries[i].score} Pts ({entries[i].date})");
            }

            leaderboardText.text = sb.ToString();
        }
    }
}
