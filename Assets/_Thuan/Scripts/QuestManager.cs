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
   // public GameObject PanelSucces;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI questNametext;
    public Button acceptButton;
    public Button declineButton;
    public Button openQuestButton; // nút gắn AcpQuestlogo
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI successRewardText;
    public TextMeshProUGUI faileText;

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
        Debug.Log("✅ Initial setup completed");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"🔄 Scene loaded: {scene.name}");
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.2f);

        FindUIReferences();
        SetupUI();
        Debug.Log("✅ Delayed setup completed");
    }

    private void FindUIReferences()
    {
        Debug.Log("🔍 Finding UI References...");

        if (QuestlogoPanel == null)
        {
            QuestlogoPanel = GameObject.Find("QuestlogoPanel") ?? FindInactiveGameObject("QuestlogoPanel");
        }

        if (PanelQuest == null)
        {
            PanelQuest = GameObject.Find("PanelQuest") ?? FindInactiveGameObject("PanelQuest");
        }

        //if (PanelSucces == null)
        //{
        //    PanelSucces = GameObject.Find("PanelSucces") ?? FindInactiveGameObject("PanelSucces");
        //}

        FindTextComponent(ref descriptionText, "DescriptionText", "Description Text", "Desc Text");
        FindTextComponent(ref questNametext, "QuestNameText", "Quest Name Text", "QuestName");
        FindTextComponent(ref timerText, "TimerText", "Timer Text", "Timer");
        FindTextComponent(ref successRewardText, "SuccessRewardText", "Success Reward Text", "RewardText");
        FindTextComponent(ref faileText, "faileText");

        FindButtonComponent(ref acceptButton, "AcceptButton", "Accept Button", "Accept");
        FindButtonComponent(ref declineButton, "DeclineButton", "Decline Button", "Decline");
        FindButtonComponent(ref openQuestButton, "OpenQuestButton", "QuestLogoBtn", "OpenQuest");


        Debug.Log($"UI References Found: " +
                  $"QuestPanel: {(PanelQuest != null ? "✅" : "❌")}, " +
                  $"Timer: {(timerText != null ? "✅" : "❌")}, " +
                  $"Accept: {(acceptButton != null ? "✅" : "❌")}");
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
        Debug.Log("⚙️ Setting up UI...");

        if (PanelQuest != null)
            PanelQuest.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        SetupButton(acceptButton, AcceptQuest, "Accept");
        SetupButton(declineButton, () => {
            if (QuestlogoPanel != null)
                QuestlogoPanel.SetActive(false);
        }, "Decline");
        SetupButton(openQuestButton, AcpQuestlogo, "OpenQuest");

        Debug.Log("✅ UI Setup completed");
    }

    private void SetupButton(Button button, System.Action callback, string buttonName)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
            Debug.Log($"✅ {buttonName} button configured");
        }
    }

    public void RefreshUI()
    {
        StartCoroutine(DelayedSetup());
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
            questNametext.text = quest.name;

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
        }
    }

    private void Update()
    {
        if (!questActive) return;

        timeRemaining -= Time.deltaTime;

        if (timerText != null)
            timerText.text = "Thời gian: " + Mathf.CeilToInt(timeRemaining) + "s";

        if (timeRemaining <= 0)
        {
            questActive = false;

            if (timerText != null)
                timerText.gameObject.SetActive(false);

            if (WaypointManager.Instance != null)
            {
                faileText.text = "Hết thời gian nhiệm vụ thất bại !";
            }
                WaypointManager.Instance.RemoveWaypoint();
            StartCoroutine(HideSuccessPanel());


            Debug.Log("❌ Hết thời gian làm nhiệm vụ!");
        }
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

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

            if (successRewardText != null)
            {
                successRewardText.text = "Nhiệm vụ hoàn thành bạn đã nhận được " + currentQuest.coinReward + " coin!";
            }
            StartCoroutine(HideSuccessPanel());

        }

        //if (PanelSucces != null)
        //{
        //    PanelSucces.SetActive(true);
        //    StartCoroutine(HideSuccessPanel());
        //}
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

        //if (PanelSucces != null)
        //    PanelSucces.SetActive(false);

        if (successRewardText != null)
            successRewardText.text = "";

        if (faileText != null)
            faileText.text = "";

    }

}
