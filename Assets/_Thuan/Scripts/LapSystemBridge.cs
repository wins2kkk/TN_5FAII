using UnityEngine;

public class LapSystemBridge : MonoBehaviour
{
    private LapSystem lapSystem;

    void Start()
    {
        lapSystem = GetComponent<LapSystem>();
    }

    void Update()
    {
        if (lapSystem == null) return;

        // Nếu panel kết quả đã bật => cuộc đua kết thúc
        if (lapSystem.resultPanel != null && lapSystem.resultPanel.activeSelf)
        {
            bool playerWon = lapSystem.resultText != null && lapSystem.resultText.text.Contains("Chiến thắng");

            if (RaceManager.Instance != null && !RaceManager.Instance.RaceOver)
            {
                RaceManager.Instance.CompleteMission(playerWon,
                    playerWon ? "LapSystem: Bạn đã thắng!" : "LapSystem: Bạn thua!");
            }

            // Chỉ báo một lần
            enabled = false;
        }
    }
}
