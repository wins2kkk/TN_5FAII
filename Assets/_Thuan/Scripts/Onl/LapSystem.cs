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

    [Header("Checkpoint Tracking")]
    private int playerCheckpoints = 0;
    public int requiredCheckpointsPerLap = 5;

    [Header("UI")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI timerText;
    public GameObject resultPanel;
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI cupRewardText;
    public TextMeshProUGUI raceInfoText; // 👉 hiển thị tên và thời gian từng người

    [Header("Reward Settings")]
    public int winReward = 5;
    public int loseReward = 1;

    [Header("Opponent")]
    public string[] botNames = { "Bot_Alex", "Bot_Jin", "Bot_Mike", "Bot_Sara" };
    private string opponentName;
    private float opponentFinishTime = 0f;
    private float playerFinishTime = 0f;

    private void Start()
    {
        opponentName = botNames[Random.Range(0, botNames.Length)];
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (raceEnded) return;

        if (other.CompareTag("Checkpoint"))
        {
            playerCheckpoints++;
        }

        OppentCar opponentCar = other.GetComponent<OppentCar>();
        Car_script playerCar = other.GetComponent<Car_script>();

        if (opponentCar != null)
        {
            opponentCar.currentCheckpoints++;
            if (opponentCar.currentCheckpoints >= requiredCheckpointsPerLap)
            {
                opponentCar.currentCheckpoints = 0;
                opponentCar.currentLap++;
                CheckRaceCompletion(opponentCar);
            }
        }

        if (playerCar != null && other.CompareTag("Finish"))
        {
            if (playerCheckpoints >= requiredCheckpointsPerLap)
            {
                playerCheckpoints = 0;
                currentLap++;
                UpdateLapUI();
                CheckRaceCompletion(playerCar);
            }
        }
    }

    private void CheckRaceCompletion(OppentCar opponentCar)
    {
        if (opponentCar.currentLap >= maxLap)
        {
            opponentFinishTime = Time.timeSinceLevelLoad;
            EndMission(false); // player thua
        }
    }

    private void CheckRaceCompletion(Car_script playerCar)
    {
        if (currentLap >= maxLap)
        {
            playerFinishTime = raceTime;
            EndMission(true); // player thắng
        }
    }

    private void EndMission(bool success)
    {
        if (raceEnded) return;
        raceEnded = true;

        int rewardCup = success ? winReward : loseReward;

        if (success)
        {
            resultText.text = "Victory!";
            TrophyManager.Instance?.AddCup(winReward);
            playerFinishTime = raceTime;
        }
        else
        {
            resultText.text = "Defeat!";
            TrophyManager.Instance?.AddCup(loseReward);
            opponentFinishTime = Time.timeSinceLevelLoad;
        }

        // Hiển thị thông tin người thắng
        if (raceInfoText != null)
        {
            if (success)
            {
                raceInfoText.text = $"1. You - {FormatTime(playerFinishTime)}\n2. {opponentName} - {FormatTime(opponentFinishTime)}";
            }
            else
            {
                raceInfoText.text = $"1. {opponentName} - {FormatTime(opponentFinishTime)}\n2. You - {FormatTime(playerFinishTime)}";
            }
        }

        if (cupRewardText != null)
        {
            cupRewardText.text = $"🏆 +{rewardCup} Cup";
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
        }

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
            lapText.text = $"Lap: {currentLap}/{maxLap}";
        }
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
        playerCheckpoints = 0;
        raceTime = 0f;
        resultPanel.SetActive(false);
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
        Debug.Log("🔄 Đã restart race");
    }
}
