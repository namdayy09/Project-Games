using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Player player;
    [SerializeField] private Spawner spawner;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject gameOver;

    // Text hiển thị tiêu đề và nội dung lịch sử
    [SerializeField] private Text historyTitle;
    [SerializeField] private Text historyContent;

    // Nút làm mới
    [SerializeField] private Button refreshButton;

    public int score { get; private set; } = 0;
    private List<int> historyScores = new List<int>();

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        Pause();

        // Đặt tiêu đề cố định
        if (historyTitle != null)
        {
            historyTitle.text = "Lịch Sử Chơi";
        }

        // Đọc dữ liệu đã lưu
        LoadHistory();
        ShowTopScores();

        // Gắn sự kiện cho nút làm mới
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(ClearHistory);
        }
    }

    public void Pause()
    {
        Time.timeScale = 0f;
        player.enabled = false;
    }

    public void Play()
    {
        score = 0;
        scoreText.text = score.ToString();

        playButton.SetActive(false);
        gameOver.SetActive(false);

        Time.timeScale = 1f;
        player.enabled = true;

        Pipes[] pipes = FindObjectsOfType<Pipes>();
        for (int i = 0; i < pipes.Length; i++)
        {
            Destroy(pipes[i].gameObject);
        }
    }

    public void GameOver()
    {
        playButton.SetActive(true);
        gameOver.SetActive(true);

        // Thêm điểm vào danh sách
        historyScores.Add(score);

        // Lưu lại vào PlayerPrefs
        SaveHistory();

        // Hiển thị top 3
        ShowTopScores();

        Pause();
    }

    public void IncreaseScore()
    {
        score++;
        scoreText.text = score.ToString();
    }

    private void ShowTopScores()
    {
        var topScores = historyScores.OrderByDescending(s => s).Take(3).ToList();

        if (historyContent != null)
        {
            historyContent.text = "";
            for (int i = 0; i < topScores.Count; i++)
            {
                historyContent.text += (i + 1) + ". " + topScores[i] + "\n";
            }
            historyContent.gameObject.SetActive(true);
        }
    }

    private void SaveHistory()
    {
        PlayerPrefs.SetInt("HistoryCount", historyScores.Count);
        for (int i = 0; i < historyScores.Count; i++)
        {
            PlayerPrefs.SetInt("HistoryScore_" + i, historyScores[i]);
        }
        PlayerPrefs.Save();
    }

    private void LoadHistory()
    {
        historyScores.Clear();
        int count = PlayerPrefs.GetInt("HistoryCount", 0);
        for (int i = 0; i < count; i++)
        {
            int score = PlayerPrefs.GetInt("HistoryScore_" + i, 0);
            historyScores.Add(score);
        }
    }

    // Hàm làm mới lịch sử
    public void ClearHistory()
    {
        historyScores.Clear();
        PlayerPrefs.DeleteKey("HistoryCount");

        int i = 0;
        while (PlayerPrefs.HasKey("HistoryScore_" + i))
        {
            PlayerPrefs.DeleteKey("HistoryScore_" + i);
            i++;
        }

        PlayerPrefs.Save();
        ShowTopScores();
    }
}
