using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using PlayFab;
using PlayFab.ClientModels;
using System;
using DG.Tweening; // Cần import DOTween (bạn có thể tải từ Package Manager hoặc Asset Store)

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
    public int winCoinReward = 100;
    public int loseCoinReward = 5;

    [Header("Checkpoint Settings")]
    public TextMeshProUGUI checkpointText;
    public int checkpointsRequiredPerLap = 4;
    private int playerCheckpointCount = 0;

    private List<RacerResult> results = new List<RacerResult>();
    private List<string> botNames = new List<string> { "Thuận", "Tư", "Khởi anh", "Quân", "Thắng" };
    private bool hasPlayerFinished = false;

    private Coroutine timerCoroutine;

    // Thêm biến để lưu maxUnlockedLevel hiện tại
    private int currentMaxUnlockedLevel = 1;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedInit());
    }
    private IEnumerator DelayedInit()
    {
        yield return new WaitForEndOfFrame();
        FindUIReferences();
        ResetRaceState();
        LoadCurrentMaxUnlockedLevel(); // Load level hiện tại từ PlayFab
    }

    // Thêm method để load maxUnlockedLevel hiện tại
    private void LoadCurrentMaxUnlockedLevel()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(),
            result =>
            {
                if (result.Data != null && result.Data.ContainsKey("MaxUnlockedLevel"))
                    currentMaxUnlockedLevel = Convert.ToInt32(result.Data["MaxUnlockedLevel"].Value);
                else
                    currentMaxUnlockedLevel = 1;
            },
            error =>
            {
                Debug.LogError("❌ Load Error: " + error.GenerateErrorReport());
                currentMaxUnlockedLevel = 1;
            });
    }

    private void ResetRaceState()
    {
        raceEnded = false;
        currentLap = 0;
        raceTime = 0f;
        playerCheckpointCount = 0;
        hasPlayerFinished = false;
        results.Clear();

        UpdateLapUI();
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(UpdateTimer());

        if (resultPanel != null)
            resultPanel.SetActive(false);

   
    }
    private void Start()
    {
        FindUIReferences();
        ResetRaceState();
        LoadCurrentMaxUnlockedLevel(); // Load level hiện tại
    }
    private void FindUIReferences()
    {
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
            checkpointText.text = $"Vị trí: {playerCheckpointCount}/{checkpointsRequiredPerLap}";
    }

    private void OnTriggerEnter(Collider other)
    {
        OppentCar opponentCar = other.GetComponent<OppentCar>();
        Car_script playerCar = other.GetComponent<Car_script>();

        // BOT
        if (opponentCar != null && !opponentCar.hasFinished)
        {
            opponentCar.IncreaseLap();
            if (opponentCar.currentLap >= maxLap)
            {
                opponentCar.hasFinished = true;
                string botName = string.IsNullOrEmpty(opponentCar.botName) ? GetRandomBotName() : opponentCar.botName;
                opponentCar.botName = botName;
                results.Add(new RacerResult(botName, Time.timeSinceLevelLoad));

                if (hasPlayerFinished) UpdateResultPanel();
            }
        }

        // PLAYER
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
                    results.Add(new RacerResult("Bạn", Time.timeSinceLevelLoad));
                    EndMission();
                }
            }
        }
    }

    private void EndMission()
    {
        raceEnded = true;
        results = results.OrderBy(r => r.finishTime).ToList();
        bool playerWon = results.Count > 0 && results[0].name == "Bạn";

        // Xác định level hiện tại
        string currentScene = SceneManager.GetActiveScene().name;
        int currentLevel = 1;
        if (currentScene.StartsWith("Level_"))
            int.TryParse(currentScene.Substring("Level_".Length), out currentLevel);

        // -----------------------
        // Logic thưởng
        int cupReward;
        int coinReward;

        if (playerWon && currentLevel == currentMaxUnlockedLevel)
        {
            // 👉 Thắng màn mới -> nhận thưởng set trong Inspector
            cupReward = winReward;
            coinReward = winCoinReward;
        }
        else
        {
            // 👉 Thua hoặc chơi lại màn cũ -> chỉ nhận 1 cúp, 100 xu
            cupReward = 1;
            coinReward = 100;
        }

        // -----------------------
        // Update UI
        resultText.text = playerWon ? "Chiến thắng!" : "Thất bại!";
        if (cupRewardText != null) cupRewardText.text = $"+{cupReward}";
        if (coinRewardText != null) coinRewardText.text = $"+{coinReward}";

        // Cộng thưởng
        TrophyManager.Instance?.AddCup(cupReward);
        CoinManager.Instance?.AddCoins(coinReward);

        // Hiển thị thời gian
        RacerResult playerResult = results.FirstOrDefault(r => r.name == "Bạn");
        if (playerResult != null)
            finalTimeText.text = "" + FormatTime(playerResult.finishTime);

        UpdateResultPanel();
        StartCoroutine(ShowResultPanelWithDelay(1.3f));

        // -----------------------
        // Nếu thắng màn mới -> mở khóa màn tiếp theo
        if (playerWon && currentLevel == currentMaxUnlockedLevel)
        {
            int newUnlockedLevel = currentMaxUnlockedLevel + 1;

            SaveUnlockedLevelToPlayFab(newUnlockedLevel);
            currentMaxUnlockedLevel = newUnlockedLevel;

            LevelUnlockSystem unlockSystem = FindObjectOfType<LevelUnlockSystem>();
            unlockSystem?.UnlockNextLevel(currentLevel);
        }
    }



    private void SaveUnlockedLevelToPlayFab(int newUnlockedLevel)
    {
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string> { { "MaxUnlockedLevel", newUnlockedLevel.ToString() } }
        };
        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log($"✅ Saved MaxUnlockedLevel {newUnlockedLevel} to PlayFab"),
            error => Debug.LogError("❌ Save Error: " + error.GenerateErrorReport()));
    }

    private void UpdateResultPanel()
    {
        results = results.OrderBy(r => r.finishTime).ToList();
        if (top1Text != null && results.Count > 0)
            top1Text.text = $"Top 1. {results[0].name} - {FormatTime(results[0].finishTime)}";
        if (top2Text != null && results.Count > 1)
            top2Text.text = $"Top 2. {results[1].name} - {FormatTime(results[1].finishTime)}";
        if (top3Text != null && results.Count > 2)
            top3Text.text = $"Top 3. {results[2].name} - {FormatTime(results[2].finishTime)}";
    }
    private void UpdateLapUI()
    {
        if (lapText != null) lapText.text = "Vòng đua: " + currentLap + "/" + maxLap;
    }
    private IEnumerator UpdateTimer()
    {
        while (!raceEnded)
        {
            raceTime += Time.deltaTime;
            if (timerText != null)
                timerText.text = FormatTime(raceTime);
            yield return null;
        }
    }
    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int milliseconds = Mathf.FloorToInt((time % 1f) * 100f);
        return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
    }
    private string GetRandomBotName()
    {
        if (botNames.Count == 0) return "Máy";
        int index = UnityEngine.Random.Range(0, botNames.Count);
        string name = botNames[index];
        botNames.RemoveAt(index);
        return name;
    }
    class RacerResult
    {
        public string name;
        public float finishTime;
        public RacerResult(string name, float time) { this.name = name; this.finishTime = time; }
    }
    private IEnumerator ShowResultPanelWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            // Reset scale để chuẩn bị hiệu ứng
            resultPanel.transform.localScale = Vector3.zero;

            // Hiệu ứng bật ra mượt (scale to 1 với "punch")
            resultPanel.transform.DOScale(1f, 0.5f).SetEase(Ease.OutBack);
        }
    }
}