using UnityEngine;
using UnityEngine.UI;

public class GameHistory : MonoBehaviour
{
    public Text historyText;
    private string history = "";

    public void AddHistory(int score)
    {
        history += "Điểm: " + score + "\n";
        historyText.text = history;
        historyText.gameObject.SetActive(true);
    }
}
