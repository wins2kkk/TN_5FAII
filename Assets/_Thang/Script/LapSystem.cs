using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;

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
    public TextMeshProUGUI coinRewardText;

    public TextMeshProUGUI top1Text;
    public TextMeshProUGUI top2Text;
    public TextMeshProUGUI top3Text;

    [Header("Reward Settings")]
    public int winReward = 5;
    public int loseReward = 1;

    [Header("Coin Reward Settings")]
    public int winCoinReward = 20;
    public int loseCoinReward = 5;

    [Header("Checkpoint Settings")]
    public TextMeshProUGUI checkpointText;
    public int checkpointsRequiredPerLap = 4;
    private int playerCheckpointCount = 0;

    private List<RacerResult> results = new List<RacerResult>();
    private List<string> botNames = new List<string> { "Bot Alpha", "Bot Beta", "Bot Gamma", "Bot Delta", "Bot Zeta" };
    private bool hasPlayerFinished = false;

    private void Awake()
    {
        // Nghe sự kiện load scene để tìm lại UI
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnEnable()
    {
        // Đăng ký sự kiện khi scene mới load xong
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnDisable()
    {
        // Hủy đăng ký khi script bị disable/destroy
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 Đang tìm lại UI trong scene {scene.name}");
        StartCoroutine(DelayedFindUI());
    }

    private IEnumerator DelayedFindUI()
    {
        yield return new WaitForEndOfFrame();
        FindUIReferences();
    }

    private void Start()
    {
        FindUIReferences();
        UpdateLapUI();
        StartCoroutine(UpdateTimer());
    }

    private void FindUIReferences()
    {
        // Tìm cả object đang ẩn bằng Resources.FindObjectsOfTypeAll
        if (lapText == null)
            lapText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "LapText");

        if (timerText == null)
            timerText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "TimerText");

        if (resultPanel == null)
            resultPanel = Resources.FindObjectsOfTypeAll<GameObject>()
                .FirstOrDefault(g => g.name == "ResultPanel");

        if (resultText == null)
            resultText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "ResultText");

        if (finalTimeText == null)
            finalTimeText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "FinalTimeText");

        if (cupRewardText == null)
            cupRewardText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "CupRewardText");

        if (coinRewardText == null)
            coinRewardText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "CoinRewardText");

        if (top1Text == null)
            top1Text = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "Top1Text");

        if (top2Text == null)
            top2Text = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "Top2Text");

        if (top3Text == null)
            top3Text = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "Top3Text");

        if (checkpointText == null)
            checkpointText = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .FirstOrDefault(t => t.name == "CheckpointText");
    }


    public void PlayerPassedCheckpoint()
    {
        playerCheckpointCount++;
        if (checkpointText != null)
        {
            checkpointText.text = $"Checkpoint: {playerCheckpointCount}/{checkpointsRequiredPerLap}";
        }
    }

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

                results.Add(new RacerResult(botName, Time.timeSinceLevelLoad));

                if (hasPlayerFinished)
                {
                    UpdateResultPanel();
                }
            }
        }

        // PLAYER về đích
        if (playerCar != null && currentLap < maxLap)
        {
            if (playerCheckpointCount >= checkpointsRequiredPerLap)
            {
                currentLap++;
                playerCheckpointCount = 0;
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

        int cupReward = playerWon ? winReward : loseReward;
        int coinReward = playerWon ? winCoinReward : loseCoinReward;

        if (cupRewardText != null)
            cupRewardText.text = $"+{cupReward} Cup";

        if (coinRewardText != null)
            coinRewardText.text = $"+{coinReward} Coin";

        if (TrophyManager.Instance != null)
        {
            TrophyManager.Instance.AddCup(cupReward);
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinReward);
            Debug.Log($"🎉 Đã cộng {coinReward} Coin cho người chơi ({(playerWon ? "thắng" : "thua")})");
        }
        RacerResult playerResult = results.FirstOrDefault(r => r.name == "You");
        if (playerResult != null)
        {
            finalTimeText.text = "Your Time: " + FormatTime(playerResult.finishTime);
        }

        UpdateResultPanel();
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
            EndMission();
        }
    }
}