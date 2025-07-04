using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI References - Sẽ được tự động tìm lại")]
    public GameObject PanelQuest;
    public GameObject QuestlogoPanel;
    public GameObject PanelSucces; // 👈 Bỏ comment để hiển thị thông báo
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI questNametext;
    public Button acceptButton;
    public Button declineButton;
    public Button openQuestButton; // nút gắn AcpQuestlogo
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI successRewardText;
    public TextMeshProUGUI faileText;
    public Button HuyNV;


    [Header("Quest Data - Được giữ lại")]
    private QuestData currentQuest;
    private float timeRemaining;
    private bool questActive = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("✅ QuestManager created and registered for scene events");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(InitialSetup());
    }

    private IEnumerator InitialSetup()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);

        FindUIReferences();
        SetupUI();
        //Debug.Log("✅ Initial setup completed");
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.2f);

        FindUIReferences();
        SetupUI();
        //Debug.Log("✅ Delayed setup completed");
    }

    private void FindUIReferences()
    {
        //Debug.Log("🔍 Finding UI References...");

        if (QuestlogoPanel == null)
        {
            QuestlogoPanel = GameObject.Find("QuestlogoPanel") ?? FindInactiveGameObject("QuestlogoPanel");
        }

        if (PanelQuest == null)
        {
            PanelQuest = GameObject.Find("PanelQuest") ?? FindInactiveGameObject("PanelQuest");
        }

        // 👈 Tìm PanelSucces để hiển thị thông báo
        if (PanelSucces == null)
        {
            PanelSucces = GameObject.Find("PanelSucces") ?? FindInactiveGameObject("PanelSucces");
        }

        FindTextComponent(ref descriptionText, "DescriptionText", "Description Text", "Desc Text");
        FindTextComponent(ref questNametext, "QuestNameText", "Quest Name Text", "QuestName");
        FindTextComponent(ref timerText, "TimerText", "Timer Text", "Timer");
        FindTextComponent(ref successRewardText, "SuccessRewardText", "Success Reward Text", "RewardText");
        FindTextComponent(ref faileText, "faileText");

        FindButtonComponent(ref acceptButton, "AcceptButton", "Accept Button", "Accept");
        FindButtonComponent(ref declineButton, "DeclineButton", "Decline Button", "Decline");
        FindButtonComponent(ref openQuestButton, "OpenQuestButton", "QuestLogoBtn", "OpenQuest");
        FindButtonComponent(ref HuyNV, "HuyNV", "HuyNV", "HuyNV");

    }

    private GameObject FindInactiveGameObject(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.gameObject.name == name && t.gameObject.scene.isLoaded)
            {
                return t.gameObject;
            }
        }
        return null;
    }

    private void FindTextComponent(ref TextMeshProUGUI component, params string[] possibleNames)
    {
        if (component != null) return;

        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name) ?? FindInactiveGameObject(name);

            if (obj != null)
            {
                component = obj.GetComponent<TextMeshProUGUI>();
                if (component != null) break;
            }
        }
    }

    private void FindButtonComponent(ref Button component, params string[] possibleNames)
    {
        if (component != null) return;

        foreach (string name in possibleNames)
        {
            GameObject obj = GameObject.Find(name) ?? FindInactiveGameObject(name);

            if (obj != null)
            {
                component = obj.GetComponent<Button>();
                if (component != null) break;
            }
        }
    }

    private void SetupUI()
    {
        if (PanelQuest != null)
            PanelQuest.SetActive(false);

        if (PanelSucces != null)
            PanelSucces.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (HuyNV != null)
            HuyNV.gameObject.SetActive(false); // 👈 Ẩn nút hủy nhiệm vụ ban đầu

        SetupButton(acceptButton, AcceptQuest, "Accept");
        SetupButton(declineButton, () => {
            if (QuestlogoPanel != null)
                QuestlogoPanel.SetActive(false);
        }, "Decline");

        SetupButton(openQuestButton, AcpQuestlogo, "OpenQuest");
        SetupButton(HuyNV, HuyNhiemVU, "HuyNV");
    }

    private void SetupButton(Button button, System.Action callback, string buttonName)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
            //  Debug.Log($"✅ {buttonName} button configured");
        }
    }

    public void RefreshUI()
    {
        StartCoroutine(DelayedSetup());
    }
    private void HuyNhiemVU()
    {
        if (!questActive) return;

        FailQuest("Bạn đã từ bỏ nhiệm vụ!");
    }

    public void ShowQuestPopup(QuestData quest)
    {
        if (questActive) return;

        if (PanelQuest == null)
        {
            FindUIReferences();
            SetupUI();
        }

        currentQuest = quest;

        if (questNametext != null)
            questNametext.text = quest.questName;

        if (descriptionText != null)
            descriptionText.text = quest.description + "\nThưởng: " + quest.coinReward + " coin\nThời gian: " + quest.timeLimit + "s";

        if (PanelQuest != null)
            PanelQuest.SetActive(true);
    }

    public void HideQuestPopup()
    {
        if (PanelQuest != null)
            PanelQuest.SetActive(false);
    }

    private void AcceptQuest()
    {
        if (QuestlogoPanel != null)
            QuestlogoPanel.SetActive(false);
        StartQuest();
    }

    private void StartQuest()
    {
        if (currentQuest == null) return;

        questActive = true;
        timeRemaining = currentQuest.timeLimit;

        if (timerText != null)
            timerText.gameObject.SetActive(true);
        if (HuyNV != null)
            HuyNV.gameObject.SetActive(true); // 👈 Hiện nút hủy khi bắt đầu nhiệm vụ

        switch (currentQuest.questType)
        {
            case QuestType.ParkCar:
                FindObjectOfType<ParkingMission>()?.StartMission();
                break;
            case QuestType.Delivery:
                FindObjectOfType<DeliveryQuest>()?.StartQuest();
                break;
            case QuestType.ThuThapCoin:
                FindObjectOfType<ThuThapVatPham>()?.StartQuest();
                break;
            case QuestType.DoXang:
                FindObjectOfType<FuelMission>()?.StartMission();
                break;
            case QuestType.BanTocDo:
                FindObjectOfType<BanTocDo>()?.StartMission();
                break;
            case QuestType.duaxe:
                StartLapRaceQuest();
                break;
            case QuestType.DuaAI:
                StartDuaAI();
                break;



        }
    }

    private void Update()
    {
        if (!questActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"Thời gian còn lại: {minutes:00}:{seconds:00}";
        }
        if (timeRemaining < 10f)
            timerText.color = Color.red;
        else
            timerText.color = Color.white;


        if (timeRemaining <= 0)
        {
            // 👈 Gọi FailQuest thay vì xử lý trực tiếp
            FailQuest("Hết thời gian nhiệm vụ thất bại!");
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            FailQuest("Bạn đã thất bại!");
        }

    }
    private void StartLapRaceQuest()
    {
        // Lưu lại dữ liệu nhiệm vụ để scene đua dùng
        PlayerPrefs.SetInt("LapMission_Active", 1);
        PlayerPrefs.SetInt("LapMission_Laps", currentQuest.lapCount); // 👈 nếu có lapCount
        PlayerPrefs.SetFloat("LapMission_Time", currentQuest.timeLimit);
        PlayerPrefs.SetFloat("LapMission_Reward", currentQuest.coinReward);

        PlayerPrefs.SetString("LapMission_ReturnScene", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetString("SceneToLoad", "ChayLap");
        SceneManager.LoadScene("Loading");

    }
    private void StartDuaAI()
    {
        PlayerPrefs.SetInt("LapMission_Active", 1);
        PlayerPrefs.SetInt("LapMission_Laps", currentQuest.lapCount);
        PlayerPrefs.SetFloat("LapMission_Time", currentQuest.timeLimit);
        PlayerPrefs.SetFloat("LapMission_Reward", currentQuest.coinReward);
        PlayerPrefs.SetString("LapMission_ReturnScene", SceneManager.GetActiveScene().name);

        PlayerPrefs.SetString("SceneToLoad", "DuaAi");
        SceneManager.LoadScene("Loading");
    }

    public void AcpQuestlogo()
    {
        if (QuestlogoPanel == null || PanelQuest == null)
        {
            RefreshUI();
            return;
        }

        if (QuestlogoPanel != null)
            QuestlogoPanel.SetActive(true);

        if (PanelQuest != null)
            PanelQuest.SetActive(false);
    }

    public void CompleteQuest()
    {
        if (!questActive || currentQuest == null) return;

        questActive = false;

        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (HuyNV != null)
            HuyNV.gameObject.SetActive(false); // Ẩn nút hủy khi nhiệm vụ kết thúc


        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

            if (successRewardText != null)
            {
                successRewardText.text = "Nhiệm vụ hoàn thành bạn đã nhận được " + currentQuest.coinReward + " coin!";
            }

            // 👈 Hiển thị panel thành công
            if (PanelSucces != null)
                PanelSucces.SetActive(true);

            StartCoroutine(HideSuccessPanel());
        }

    }
    public void FailQuest(string reason)
    {
        if (!questActive) return;

        questActive = false;

        Debug.Log("❌ Nhiệm vụ thất bại: " + reason);

        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (HuyNV != null)
            HuyNV.gameObject.SetActive(false); // Ẩn nút hủy khi nhiệm vụ kết thúc

        WaypointManager.Instance?.RemoveWaypoint();

        if (faileText != null)
        {
            faileText.text = reason;
            faileText.gameObject.SetActive(true); // 👉 đảm bảo text hiện ra
            StartCoroutine(HideSuccessPanel());
        }

      
    }

    public bool IsQuestActive()
    {
        return questActive;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Debug.Log("🗑️ QuestManager destroyed and unregistered events");
    }

    private IEnumerator HideSuccessPanel()
    {
        yield return new WaitForSeconds(2f);

        // 👈 Ẩn panel sau 2 giây
        if (PanelSucces != null)
            PanelSucces.SetActive(false);

        if (successRewardText != null)
            successRewardText.text = "";

        if (faileText != null)
            faileText.text = "";

    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DelayedSetup());

        if (questActive && currentQuest != null)
        {
            string currentScene = scene.name;
            string returnScene = PlayerPrefs.GetString("LapMission_ReturnScene", "");

            if (!string.IsNullOrEmpty(returnScene) && currentScene != returnScene)
            {
                Debug.Log("❌ Đổi scene nên nhiệm vụ thất bại");
                FailQuest("Bạn đã rời khỏi khu vực nhiệm vụ!");
            }
        }
    }

}