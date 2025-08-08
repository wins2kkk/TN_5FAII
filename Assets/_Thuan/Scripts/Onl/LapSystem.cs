using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

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
    public TextMeshProUGUI cupRewardText;

    public TextMeshProUGUI top1Text;
    public TextMeshProUGUI top2Text;
    public TextMeshProUGUI top3Text;

    [Header("Reward Settings")]
    public int winReward = 5;
    public int loseReward = 1;

    private List<RacerResult> results = new List<RacerResult>();
    private List<string> botNames = new List<string> { "Bot Alpha", "Bot Beta", "Bot Gamma", "Bot Delta", "Bot Zeta" };
    private bool hasPlayerFinished = false;
    //private Checkpointwin checkpointManager;
    public TextMeshProUGUI checkpointText;
    [Header("Checkpoint Settings")]
    public int checkpointsRequiredPerLap = 4; // Số checkpoint cần qua để tính 1 lap
    private int playerCheckpointCount = 0;

    public void PlayerPassedCheckpoint()
    {
        playerCheckpointCount++;
        if (checkpointText != null)
        {
            checkpointText.text = $"Checkpoint: {playerCheckpointCount}/{checkpointsRequiredPerLap}";
        }
    }

    private void Start()
    {
        //checkpointManager = FindObjectOfType<Checkpointwin>();
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
    }
    //private void Update()
    //{
    //    if (checkpointManager != null && checkpointText != null)
    //    {
    //        checkpointText.text = $"Checkpoint: {checkpointManager.CheckpointsPassedCount}/5";
    //    }
    //}
    private void OnTriggerEnter(Collider other)
    {
        OppentCar opponentCar = other.GetComponent<OppentCar>();
        Car_script playerCar = other.GetComponent<Car_script>();

        // BOT về đích
        if (opponentCar != null && !opponentCar.hasFinished)
        {
            opponentCar.IncreaseLap();

            if (opponentCar.currentLap >= maxLap)
            {
                opponentCar.hasFinished = true;

                string botName = opponentCar.botName;
                if (string.IsNullOrEmpty(botName))
                {
                    botName = GetRandomBotName();
                    opponentCar.botName = botName;
                }

                // ✅ Ghi đúng thời điểm bot về đích
                results.Add(new RacerResult(botName, Time.timeSinceLevelLoad));

                // ✅ Nếu player đã về đích => cập nhật UI
                if (hasPlayerFinished)
                {
                    UpdateResultPanel();
                }
            }
        }

        // PLAYER về đích
        if (playerCar != null && currentLap < maxLap)
        {
            // ✅ Chỉ tăng lap nếu đã qua đủ checkpoint
            if (playerCheckpointCount >= checkpointsRequiredPerLap)
            {
                currentLap++;
                playerCheckpointCount = 0; // Reset cho lap mới
                UpdateLapUI();

                if (currentLap >= maxLap && !hasPlayerFinished)
                {
                    hasPlayerFinished = true;
                    results.Add(new RacerResult("You", Time.timeSinceLevelLoad));
                    EndMission();
                }
            }
            else
            {
                Debug.Log("⚠ Chưa qua đủ checkpoint, không tính lap!");
            }
        }

    }
    private void EndMission()
    {
        raceEnded = true;

        results = results.OrderBy(r => r.finishTime).ToList();

        bool playerWon = results[0].name == "You";
        resultText.text = playerWon ? "Victory!" : "Defeat!";
        cupRewardText.text = playerWon ? $" +{winReward} Cup" : $" +{loseReward} Cup";

        if (TrophyManager.Instance != null)
        {
            TrophyManager.Instance.AddCup(playerWon ? winReward : loseReward);
        }

        RacerResult playerResult = results.FirstOrDefault(r => r.name == "You");
        if (playerResult != null)
        {
            finalTimeText.text = "Your Time: " + FormatTime(playerResult.finishTime);
        }

        UpdateResultPanel(); // 👈 ban đầu có thể chỉ có "You"
        resultPanel.SetActive(true);
    }

    private void UpdateResultPanel()
    {
        results = results.OrderBy(r => r.finishTime).ToList();

        if (top1Text != null && results.Count > 0)
            top1Text.text = $"1st. {results[0].name} - {FormatTime(results[0].finishTime)}";

        if (top2Text != null && results.Count > 1)
            top2Text.text = $"2nd {results[1].name} - {FormatTime(results[1].finishTime)}";

        if (top3Text != null && results.Count > 2)
            top3Text.text = $"3th {results[2].name} - {FormatTime(results[2].finishTime)}";
    }

    private void UpdateLapUI()
    {
        if (lapText != null)
        {
            lapText.text = "Lap: " + currentLap + "/" + maxLap;
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

    private string GetRandomBotName()
    {
        if (botNames.Count == 0) return "Bot";
        int index = Random.Range(0, botNames.Count);
        string name = botNames[index];
        botNames.RemoveAt(index);
        return name;
    }

    public void RestartRace()
    {
        raceEnded = false;
        currentLap = 0;
        raceTime = 0f;
        resultPanel.SetActive(false);
        results.Clear();
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
        Debug.Log("🔄 Đã restart race");
    }

    class RacerResult
    {
        public string name;
        public float finishTime;

        public RacerResult(string name, float time)
        {
            this.name = name;
            this.finishTime = time;
        }
    }
    public void PlayerPassedFinishLine()
    {
        if (raceEnded || currentLap >= maxLap) return;

        currentLap++;
        UpdateLapUI();

        Debug.Log($"✅ Lap {currentLap} hoàn thành!");

        if (currentLap >= maxLap)
        {
            hasPlayerFinished = true;
            results.Add(new RacerResult("You", Time.timeSinceLevelLoad));
            EndMission(); // Hiện kết quả
        }
    }

}