using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;
using DG.Tweening;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI References - Sẽ được tự động tìm lại")]
    public GameObject PanelQuest;
    public GameObject QuestlogoPanel;
    public GameObject PanelSucces;
    public GameObject PanelFaile;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI questNametext;
    public Button acceptButton;
    public Button declineButton;
    public Button openQuestButton;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI successRewardText;
    public TextMeshProUGUI faileText;
    public Button HuyNV;

    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public Ease animationEase = Ease.InOutQuad;

    [Header("Button Effect Settings")]
    public float buttonScaleEffect = 1.1f;
    public float buttonPressScale = 0.95f;
    public float buttonEffectDuration = 0.15f;
    public float buttonPunchScale = 0.1f;

    [Header("Quest Data - Được giữ lại")]
    private QuestData currentQuest;
    private float timeRemaining;
    private bool questActive = false;
    private bool isAnimating = false;

    private Vector3 originalPanelPosition;
    private Vector3 hiddenPanelPosition;

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
        SetupAnimationPositions();
        SetupButtonEffects();
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.2f);

        FindUIReferences();
        SetupUI();
        SetupAnimationPositions();
        SetupButtonEffects();
    }

    private void SetupButtonEffects()
    {
        SetupButtonEffect(acceptButton, "Accept");
        SetupButtonEffect(declineButton, "Decline");
        SetupButtonEffect(openQuestButton, "OpenQuest");
        SetupButtonEffect(HuyNV, "HuyNV");
    }

    private void SetupButtonEffect(Button button, string buttonName)
    {
        if (button == null) return;

        button.transform.localScale = Vector3.one;

        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        eventTrigger.triggers.Clear();

        EventTrigger.Entry pressDown = new EventTrigger.Entry();
        pressDown.eventID = EventTriggerType.PointerDown;
        pressDown.callback.AddListener((data) => { StartCoroutine(ButtonPressEffect(button, true)); });
        eventTrigger.triggers.Add(pressDown);

        EventTrigger.Entry pressUp = new EventTrigger.Entry();
        pressUp.eventID = EventTriggerType.PointerUp;
        pressUp.callback.AddListener((data) => { StartCoroutine(ButtonPressEffect(button, false)); });
        eventTrigger.triggers.Add(pressUp);

        EventTrigger.Entry click = new EventTrigger.Entry();
        click.eventID = EventTriggerType.PointerClick;
        click.callback.AddListener((data) => { StartCoroutine(ButtonClickEffect(button, buttonName)); });
        eventTrigger.triggers.Add(click);

        Debug.Log($"✅ Button effects setup for: {buttonName}");
    }

    private IEnumerator ButtonPressEffect(Button button, bool isPressed)
    {
        if (button == null) yield break;

        Transform buttonTransform = button.transform;
        float targetScale = isPressed ? buttonPressScale : 1f;

        yield return buttonTransform.DOScale(targetScale, buttonEffectDuration * 0.5f)
            .SetEase(animationEase)
            .WaitForCompletion();
    }

    private IEnumerator ButtonClickEffect(Button button, string buttonName)
    {
        if (button == null) yield break;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif

        Transform buttonTransform = button.transform;

        yield return buttonTransform.DOPunchScale(Vector3.one * buttonPunchScale, buttonEffectDuration, 2, 0.5f)
            .SetEase(Ease.InOutSine)
            .WaitForCompletion();

        buttonTransform.localScale = Vector3.one;

        CreateClickParticles(button.transform.position);

        Debug.Log($"🎉 Button clicked with effects: {buttonName}");
    }

    public void ResetButtonToNormal(Button button)
    {
        if (button == null) return;

        button.transform.localScale = Vector3.one;
    }

    public void ResetAllButtonsToNormal()
    {
        ResetButtonToNormal(acceptButton);
        ResetButtonToNormal(declineButton);
        ResetButtonToNormal(openQuestButton);
        ResetButtonToNormal(HuyNV);
    }

    private void CreateClickParticles(Vector3 position)
    {
        GameObject effect = new GameObject("ClickEffect");
        effect.transform.position = position;
        StartCoroutine(SimpleClickEffect(effect));
    }

    private IEnumerator SimpleClickEffect(GameObject effect)
    {
        float duration = 0.5f;
        yield return new WaitForSeconds(duration);
        Destroy(effect);
    }

    private IEnumerator AcceptButtonSpecialEffect()
    {
        if (acceptButton == null) yield break;

        Image buttonImage = acceptButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            Color specialColor = Color.green;

            yield return buttonImage.DOColor(specialColor, 0.3f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            buttonImage.color = originalColor;
        }
    }

    private IEnumerator DeclineButtonSpecialEffect()
    {
        if (declineButton == null) yield break;

        Image buttonImage = declineButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            Color specialColor = Color.red;

            yield return buttonImage.DOColor(specialColor, 0.3f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .WaitForCompletion();

            buttonImage.color = originalColor;
        }
    }

    private void SetupAnimationPositions()
    {
        if (QuestlogoPanel != null)
        {
            RectTransform panelRect = QuestlogoPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                originalPanelPosition = panelRect.anchoredPosition;
                Canvas canvas = panelRect.GetComponentInParent<Canvas>();
                float canvasHeight = canvas != null ? canvas.GetComponent<RectTransform>().rect.height : Screen.height;

                hiddenPanelPosition = new Vector3(
                    originalPanelPosition.x,
                    originalPanelPosition.y + canvasHeight + panelRect.rect.height,
                    originalPanelPosition.z
                );

                Debug.Log($"✅ Animation positions set - Original: {originalPanelPosition}, Hidden: {hiddenPanelPosition}");
                Debug.Log($"Panel Rect: {panelRect.rect}, Canvas Height: {canvasHeight}");
            }
        }
    }

    private void FindUIReferences()
    {
        if (QuestlogoPanel == null)
        {
            QuestlogoPanel = GameObject.Find("QuestlogoPanel") ?? FindInactiveGameObject("QuestlogoPanel");
        }

        if (PanelQuest == null)
        {
            PanelQuest = GameObject.Find("PanelQuest") ?? FindInactiveGameObject("PanelQuest");
        }

        if (PanelSucces == null)
        {
            PanelSucces = GameObject.Find("PanelSucces") ?? FindInactiveGameObject("PanelSucces");
        }

        if (PanelFaile == null)
        {
            PanelFaile = GameObject.Find("PanelFaile") ?? FindInactiveGameObject("PanelFaile");
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

        if (PanelFaile != null)
            PanelFaile.SetActive(false);

        if (QuestlogoPanel != null)
            QuestlogoPanel.SetActive(false);

        if (timerText != null)
            timerText.gameObject.SetActive(false);

        if (HuyNV != null)
            HuyNV.gameObject.SetActive(false);

        SetupButton(acceptButton, () => {
            StartCoroutine(AcceptButtonSpecialEffect());
            StartCoroutine(DelayedAcceptQuest());
        }, "Accept");

        SetupButton(declineButton, () => {
            StartCoroutine(DeclineButtonSpecialEffect());
            StartCoroutine(DelayedDeclineQuest());
        }, "Decline");

        SetupButton(openQuestButton, AcpQuestlogo, "OpenQuest");
        SetupButton(HuyNV, HuyNhiemVU, "HuyNV");
    }

    private IEnumerator DelayedAcceptQuest()
    {
        yield return new WaitForSeconds(0.2f);
        AcceptQuest();
    }

    private IEnumerator DelayedDeclineQuest()
    {
        yield return new WaitForSeconds(0.2f);
        HideQuestLogoPanelWithAnimation();
    }

    private void SetupButton(Button button, System.Action callback, string buttonName)
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
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
        {
            PanelQuest.SetActive(true);
            CanvasGroup canvasGroup = PanelQuest.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = PanelQuest.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetEase(animationEase);
        }
    }

    public void HideQuestPopup()
    {
        if (PanelQuest != null)
        {
            CanvasGroup canvasGroup = PanelQuest.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, animationDuration)
                    .SetEase(animationEase)
                    .OnComplete(() => PanelQuest.SetActive(false));
            }
            else
            {
                PanelQuest.SetActive(false);
            }
        }
    }

    private void AcceptQuest()
    {
        HideQuestLogoPanelWithAnimation();
        StartQuest();
    }

    public void ShowQuestLogoPanelWithAnimation()
    {
        if (isAnimating || QuestlogoPanel == null) return;

        isAnimating = true;

        RectTransform panelRect = QuestlogoPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            isAnimating = false;
            return;
        }

        CanvasGroup canvasGroup = QuestlogoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = QuestlogoPanel.AddComponent<CanvasGroup>();
        }

        QuestlogoPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        panelRect.anchoredPosition = hiddenPanelPosition;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(1f, animationDuration).SetEase(animationEase))
               .Join(panelRect.DOAnchorPos(originalPanelPosition, animationDuration).SetEase(animationEase))
               .OnComplete(() => isAnimating = false);

        Debug.Log("✅ Quest logo panel showed with DOTween animation");
    }

    public void HideQuestLogoPanelWithAnimation()
    {
        if (isAnimating || QuestlogoPanel == null) return;

        isAnimating = true;

        RectTransform panelRect = QuestlogoPanel.GetComponent<RectTransform>();
        if (panelRect == null)
        {
            isAnimating = false;
            return;
        }

        CanvasGroup canvasGroup = QuestlogoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = QuestlogoPanel.AddComponent<CanvasGroup>();
        }

        Sequence sequence = DOTween.Sequence();
        sequence.Append(canvasGroup.DOFade(0f, animationDuration).SetEase(animationEase))
               .Join(panelRect.DOAnchorPos(hiddenPanelPosition, animationDuration).SetEase(animationEase))
               .OnComplete(() => {
                   QuestlogoPanel.SetActive(false);
                   panelRect.anchoredPosition = originalPanelPosition;
                   canvasGroup.alpha = 1f;
                   isAnimating = false;
                   Debug.Log("✅ Quest logo panel hidden with DOTween animation");
               });
    }

    private void StartQuest()
    {
        if (currentQuest == null) return;

        questActive = true;
        timeRemaining = currentQuest.timeLimit;

        if (timerText != null)
            timerText.gameObject.SetActive(true);
        if (HuyNV != null)
            HuyNV.gameObject.SetActive(true);

        switch (currentQuest.questType)
        {
            case QuestType.ParkCar:
                FindObjectOfType<ParkingMission>()?.StartMission();
                break;
            case QuestType.Delivery:
                FindObjectOfType<DeliveryQuest>()?.StartQuest();
                break;
            case QuestType.raceCity:
                FindObjectOfType<RaceCity>()?.StartMission();
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
            case QuestType.Taxi:
                FindObjectOfType<TaxiMission>()?.StartMission();
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
            FailQuest("Hết thời gian nhiệm vụ thất bại!");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            FailQuest("Bạn đã thất bại!");
        }
    }

    private void StartLapRaceQuest()
    {
        PlayerPrefs.SetInt("LapMission_Active", 1);
        PlayerPrefs.SetInt("LapMission_Laps", currentQuest.lapCount);
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

        ShowQuestLogoPanelWithAnimation();

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
            HuyNV.gameObject.SetActive(false);

        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

        if (successRewardText != null)
        {
            successRewardText.text = +currentQuest.coinReward + " coin!";
            successRewardText.gameObject.SetActive(true);
        }

        if (PanelSucces != null)
        {
            PanelSucces.SetActive(true);
            CanvasGroup canvasGroup = PanelSucces.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = PanelSucces.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetEase(Ease.OutSine);
        }

        if (PanelFaile != null)
            PanelFaile.SetActive(false);

        if (faileText != null)
            faileText.gameObject.SetActive(false);

        StartCoroutine(HideSuccessPanel());
    }

    public void FailQuest(string reason)
    {
        if (!questActive) return;

        questActive = false;

        Debug.Log("❌ Nhiệm vụ thất bại: " + reason);

        if (timerText != null)
            timerText.gameObject.SetActive(false);
        if (HuyNV != null)
            HuyNV.gameObject.SetActive(false);

        WaypointManager.Instance?.RemoveWaypoint();

        if (PanelFaile != null)
        {
            PanelFaile.SetActive(true);
            CanvasGroup canvasGroup = PanelFaile.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = PanelFaile.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, animationDuration).SetEase(Ease.OutSine);
        }

        if (PanelSucces != null)
            PanelSucces.SetActive(false);

        if (successRewardText != null)
            successRewardText.gameObject.SetActive(false);
        if (faileText != null)
            faileText.gameObject.SetActive(false);

        StartCoroutine(HideSuccessPanel());
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

        if (PanelSucces != null)
        {
            CanvasGroup canvasGroup = PanelSucces.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                yield return canvasGroup.DOFade(0f, animationDuration)
                    .SetEase(Ease.InSine)
                    .OnComplete(() => PanelSucces.SetActive(false))
                    .WaitForCompletion();
            }
            else
            {
                PanelSucces.SetActive(false);
            }
        }

        if (successRewardText != null)
            successRewardText.text = "";

        yield return new WaitForSeconds(2f);
        if (PanelFaile != null)
        {
            CanvasGroup canvasGroup = PanelFaile.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                yield return canvasGroup.DOFade(0f, animationDuration)
                    .SetEase(Ease.InSine)
                    .OnComplete(() => PanelFaile.SetActive(false))
                    .WaitForCompletion();
            }
            else
            {
                PanelFaile.SetActive(false);
            }
        }
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