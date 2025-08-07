using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LapSystem : MonoBehaviour
{
    public int maxLap = 3;
    private int currentLap = 0;
    private float raceTime = 0f;
    private bool raceEnded = false;

    [Header("UI")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI finalTimeText;
    public TextMeshProUGUI cupRewardText; // 👈 THÊM DÒNG NÀY

    [Header("Reward Settings")]
    public int winReward = 5; // Số cúp thưởng khi thắng
    public int loseReward = 1; // Số cúp thưởng khi thua (có thể để 0)

    private void Start()
    {
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceEnded) return;

        OppentCar opponentCar = other.GetComponent<OppentCar>();
        Car_script playerCar = other.GetComponent<Car_script>();

        if (opponentCar != null)
        {
            opponentCar.IncreaseLap();
            CheckRaceCompletion(opponentCar);
        }

        if (playerCar != null)
        {
            currentLap++;
            UpdateLapUI();
            CheckRaceCompletion(playerCar);
        }
    }

    private void CheckRaceCompletion(OppentCar opponentCar)
    {
        if (opponentCar.currentLap >= maxLap)
        {
            EndMission(false);
        }
    }

    private void CheckRaceCompletion(Car_script playerCar)
    {
        if (currentLap >= maxLap)
        {
            EndMission(true);
        }
    }

    private void EndMission(bool success)
    {
        raceEnded = true;

        int rewardCup = 0;

        if (success)
        {
            Debug.Log("🏁 Người chơi đã chiến thắng!");
            resultText.text = "Victory!";
            rewardCup = winReward;

            if (TrophyManager.Instance != null)
            {
                TrophyManager.Instance.AddCup(winReward);
                Debug.Log($"✅ Đã cộng {winReward} cúp cho người chơi (lưu vào UserData).");
            }
            else
            {
                Debug.LogWarning("⚠️ TrophyManager chưa được khởi tạo.");
            }
        }
        else
        {
            Debug.Log("❌ Người chơi đã thua.");
            resultText.text = "Defeat!";
            rewardCup = loseReward;

            if (loseReward > 0 && TrophyManager.Instance != null)
            {
                TrophyManager.Instance.AddCup(loseReward);
                Debug.Log($"🎖️ Đã cộng {loseReward} cúp an ủi.");
            }
        }

        finalTimeText.text = "Time: " + FormatTime(raceTime);

        // 👇 Hiển thị số cúp nhận được
        if (cupRewardText != null)
        {
            cupRewardText.text = $"🏆 +{rewardCup} Cup";
        }

        resultPanel.SetActive(true);
        StartCoroutine(HideResultPanelAfterDelay(5f));
    }

    private IEnumerator HideResultPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (resultPanel != null && resultPanel.activeInHierarchy)
        {
            resultPanel.SetActive(false);
        }
    }

    private void UpdateLapUI()
    {
        if (lapText != null)
        {
            lapText.text = "Lap: " + currentLap + "/" + maxLap;
        }
    }
    public int GetCurrentLap()
    {
        return currentLap;
    }

    private IEnumerator UpdateTimer()
    {
        while (!raceEnded)
        {
            raceTime += Time.deltaTime;
            if (timerText != null)
            {
                timerText.text = FormatTime(raceTime);
            }
            yield return null;
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    public void RestartRace()
    {
        raceEnded = false;
        currentLap = 0;
        raceTime = 0f;
        resultPanel.SetActive(false);
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
        Debug.Log("🔄 Đã restart race");
    }
}