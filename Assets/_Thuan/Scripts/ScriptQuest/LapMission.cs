    using UnityEngine;
    using UnityEngine.SceneManagement;
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;

    public class LapMission : MonoBehaviour
    {
        [Header("Mission Settings")]
        public int requiredLaps = 3;
        public float timeLimit = 90f;

        [Header("UI Elements")]
        public TextMeshProUGUI lapText;
        public TextMeshProUGUI countdownText;
        public GameObject countdownPanel;

        [Header("Checkpoint Order")]
        public List<string> checkpointOrder = new List<string> { "Check1", "Check2", "Check3" };

        [Header("Objects")]
        public GameObject startBarrier;

        private int currentLap = 0;
        private int currentCheckpointIndex = 0;
        private float timer = 0f;
        private bool missionActive = false;

        private Transform player;
        private Transform lastCheckpointTransform;

        void Start()
        {
            // 🔍 Tìm Player tự động qua tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("🚫 Không tìm thấy Player có tag 'Player'");
            }

            if (PlayerPrefs.GetInt("LapMission_Active", 0) == 1)
            {
                LoadMissionSettings();
                StartCoroutine(StartMissionWithCountdown());
            }

            UpdateLapDisplay();
        }

        void LoadMissionSettings()
        {
            requiredLaps = PlayerPrefs.GetInt("LapMission_Laps", 3);
            timeLimit = PlayerPrefs.GetFloat("LapMission_Time", 90f);
            PlayerPrefs.SetInt("LapMission_Active", 0);
        }

        IEnumerator StartMissionWithCountdown()
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(true);

            for (int i = 3; i > 0; i--)
            {
                if (countdownText != null)
                {
                    countdownText.text = i.ToString();
                    countdownText.fontSize = 100f;
                }

                yield return new WaitForSeconds(1f);
            }

            if (countdownText != null)
            {
                countdownText.text = "GO!";
                countdownText.fontSize = 120f;
            }

            yield return new WaitForSeconds(0.5f);

            if (countdownPanel != null)
                countdownPanel.SetActive(false);
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);

            if (startBarrier != null)
                startBarrier.SetActive(false);

            StartMission();
        }

        public void StartMission()
        {
            currentLap = 0;
            currentCheckpointIndex = 0;
            timer = 0f;
            missionActive = true;

            if (CheckpointPool.Instance != null)
                CheckpointPool.Instance.ShowAllCheckpoints();

            UpdateLapDisplay();
            Debug.Log("🏁 Bắt đầu nhiệm vụ đua!");
        }

        void Update()
        {
            if (!missionActive || player == null) return;

            timer += Time.deltaTime;

            if (QuestManager.instance != null && QuestManager.instance.timerText != null)
            {
                float timeLeft = Mathf.Max(0f, timeLimit - timer);
                int minutes = Mathf.FloorToInt(timeLeft / 60f);
                int seconds = Mathf.FloorToInt(timeLeft % 60f);
                QuestManager.instance.timerText.text = $"Thời gian còn lại: {minutes:00}:{seconds:00}";
                QuestManager.instance.timerText.gameObject.SetActive(true);
            }

            if (timer > timeLimit)
            {
                FailMission();
            }

            // ⚠️ Kiểm tra rơi khỏi map
            if (player.position.y < -100f)
            {
                Debug.Log("⚠️ Người chơi rơi khỏi map");
                ReturnToLastCheckpoint();
            }
        }

        public void OnCheckpointHit(string checkpointName)
        {
            if (!missionActive) return;

            string expected = checkpointOrder[currentCheckpointIndex];
            if (checkpointName != expected)
            {
                Debug.Log($"❌ Sai checkpoint. Cần: {expected}, Nhận: {checkpointName}");
                return;
            }

            Debug.Log($"✅ Checkpoint đúng: {checkpointName}");

            if (CheckpointPool.Instance != null)
            {
                CheckpointPool.Instance.HideCheckpoint(checkpointName);

                // 🔁 Ghi nhớ checkpoint cuối cùng
                var checkpoint = CheckpointPool.Instance.checkpoints.Find(c => c.name == checkpointName);
                if (checkpoint != null)
                    lastCheckpointTransform = checkpoint.transform;
            }

            currentCheckpointIndex++;

            if (currentCheckpointIndex >= checkpointOrder.Count)
            {
                CompleteLap();
            }
        }

        void CompleteLap()
        {
            currentLap++;
            currentCheckpointIndex = 0;
            UpdateLapDisplay();

            Debug.Log($"🏁 Lap {currentLap}/{requiredLaps} hoàn thành");

            if (currentLap >= requiredLaps)
                CompleteMission();
            else
                StartCoroutine(PrepareNextLap());
        }

        IEnumerator PrepareNextLap()
        {
            yield return new WaitForSeconds(0.5f);
            if (CheckpointPool.Instance != null)
                CheckpointPool.Instance.ShowAllCheckpoints();

            Debug.Log($"🔄 Chuẩn bị lap {currentLap + 1}");
        }

        void UpdateLapDisplay()
        {
            if (lapText != null)
                lapText.text = $"Lap: {currentLap}/{requiredLaps}";
        }

        void CompleteMission()
        {
            missionActive = false;

            Debug.Log("🎉 Đua hoàn thành!");

            if (QuestManager.instance != null)
            {
                QuestManager.instance.CompleteQuest();
                if (QuestManager.instance.timerText != null)
                    QuestManager.instance.timerText.gameObject.SetActive(false);
            }

            StartCoroutine(ReturnToMenu());
        }

        void FailMission()
        {
        missionActive = false;
        Debug.Log("❌ Thất bại - Hết thời gian!");

        if (QuestManager.instance != null)
        {
            Debug.Log("cc");
            QuestManager.instance.FailQuest("Thất bại - Hết giờ!");
            if (QuestManager.instance.timerText != null)
                QuestManager.instance.timerText.gameObject.SetActive(false);
        }

        StartCoroutine(ReturnToMenu());
    }

        IEnumerator ReturnToMenu()
        {
            yield return new WaitForSeconds(7f);
            PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
            SceneManager.LoadScene("Loading"); // ← không cần LoadingManager
        }

        public void ReturnToLastCheckpoint()
        {
            if (lastCheckpointTransform != null && player != null)
            {
                player.position = lastCheckpointTransform.position + Vector3.up * 2f;
                player.rotation = lastCheckpointTransform.rotation;

                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Debug.Log("🔁 Quay lại checkpoint gần nhất");
            }
        }
    }
