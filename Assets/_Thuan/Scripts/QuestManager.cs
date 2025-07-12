using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    [Header("UI References - Sẽ được tự động tìm lại")]
    public GameObject PanelQuest;
    public GameObject QuestlogoPanel;
    public GameObject PanelSucces;
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
    public AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Button Effect Settings")]
    public float buttonScaleEffect = 1.1f;
    public float buttonPressScale = 0.95f;
    public float buttonEffectDuration = 0.15f;
    public Color buttonHoverColor = new Color(1f, 1f, 1f, 0.9f);
    public Color buttonPressColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Quest Data - Được giữ lại")]
    private QuestData currentQuest;
    private float timeRemaining;
    private bool questActive = false;
    private bool isAnimating = false;

    // Lưu vị trí ban đầu của QuestlogoPanel
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
        SetupButtonEffects(); // Thêm setup hiệu ứng button
    }

    private IEnumerator DelayedSetup()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.2f);

        FindUIReferences();
        SetupUI();
        SetupAnimationPositions();
        SetupButtonEffects(); // Thêm setup hiệu ứng button
    }

    // 🎨 BUTTON EFFECTS - Hiệu ứng đẹp cho các nút bấm
    // 🎨 BUTTON EFFECTS - Hiệu ứng đẹp cho các nút bấm (FIXED VERSION)
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

        // Lưu lại scale ban đầu của button
        button.transform.localScale = Vector3.one;

        // Thêm EventTrigger để xử lý các sự kiện hover và press
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Clear existing triggers
        eventTrigger.triggers.Clear();

        // Hover Enter Effect
        EventTrigger.Entry hoverEnter = new EventTrigger.Entry();
        hoverEnter.eventID = EventTriggerType.PointerEnter;
        hoverEnter.callback.AddListener((data) => { StartCoroutine(ButtonHoverEffect(button, true)); });
        eventTrigger.triggers.Add(hoverEnter);

        // Hover Exit Effect
        EventTrigger.Entry hoverExit = new EventTrigger.Entry();
        hoverExit.eventID = EventTriggerType.PointerExit;
        hoverExit.callback.AddListener((data) => { StartCoroutine(ButtonHoverEffect(button, false)); });
        eventTrigger.triggers.Add(hoverExit);

        // Press Down Effect
        EventTrigger.Entry pressDown = new EventTrigger.Entry();
        pressDown.eventID = EventTriggerType.PointerDown;
        pressDown.callback.AddListener((data) => { StartCoroutine(ButtonPressEffect(button, true)); });
        eventTrigger.triggers.Add(pressDown);

        // Press Up Effect
        EventTrigger.Entry pressUp = new EventTrigger.Entry();
        pressUp.eventID = EventTriggerType.PointerUp;
        pressUp.callback.AddListener((data) => { StartCoroutine(ButtonPressEffect(button, false)); });
        eventTrigger.triggers.Add(pressUp);

        // Click Effect với âm thanh và rung
        EventTrigger.Entry click = new EventTrigger.Entry();
        click.eventID = EventTriggerType.PointerClick;
        click.callback.AddListener((data) => { StartCoroutine(ButtonClickEffect(button, buttonName)); });
        eventTrigger.triggers.Add(click);

        Debug.Log($"✅ Button effects setup for: {buttonName}");
    }

    // FIXED: Hiệu ứng hover với scale cố định
    private IEnumerator ButtonHoverEffect(Button button, bool isHovering)
    {
        if (button == null) yield break;

        Image buttonImage = button.GetComponent<Image>();
        Transform buttonTransform = button.transform;

        // FIXED: Đảm bảo scale luôn bắt đầu từ 1f
        float startScale = 1f;
        float targetScale = isHovering ? buttonScaleEffect : 1f;

        Color startColor = buttonImage != null ? buttonImage.color : Color.white;
        Color targetColor = isHovering ? buttonHoverColor : Color.white;

        float elapsedTime = 0f;
        float duration = buttonEffectDuration;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // Smooth scale animation
            float currentScale = Mathf.Lerp(startScale, targetScale, progress);
            buttonTransform.localScale = Vector3.one * currentScale;

            // Smooth color transition
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(startColor, targetColor, progress);
            }

            yield return null;
        }

        // Ensure final values
        buttonTransform.localScale = Vector3.one * targetScale;
        if (buttonImage != null)
        {
            buttonImage.color = targetColor;
        }
    }

    // FIXED: Hiệu ứng press với logic cải tiến
    private IEnumerator ButtonPressEffect(Button button, bool isPressed)
    {
        if (button == null) yield break;

        Transform buttonTransform = button.transform;
        Image buttonImage = button.GetComponent<Image>();

        // FIXED: Logic scale rõ ràng hơn
        float startScale = buttonTransform.localScale.x;
        float targetScale;

        if (isPressed)
        {
            targetScale = buttonPressScale; // Thu nhỏ khi nhấn
        }
        else
        {
            // Khi nhả ra, về lại trạng thái hover (nếu đang hover) hoặc normal
            targetScale = buttonScaleEffect; // Giả sử đang hover
        }

        Color startColor = buttonImage != null ? buttonImage.color : Color.white;
        Color targetColor = isPressed ? buttonPressColor : buttonHoverColor;

        float elapsedTime = 0f;
        float duration = buttonEffectDuration * 0.5f; // Faster press effect

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            float currentScale = Mathf.Lerp(startScale, targetScale, progress);
            buttonTransform.localScale = Vector3.one * currentScale;

            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(startColor, targetColor, progress);
            }

            yield return null;
        }

        buttonTransform.localScale = Vector3.one * targetScale;
        if (buttonImage != null)
        {
            buttonImage.color = targetColor;
        }
    }

    // FIXED: Hiệu ứng click với reset về trạng thái bình thường
    private IEnumerator ButtonClickEffect(Button button, string buttonName)
    {
        if (button == null) yield break;

        // Tạo hiệu ứng rung nhẹ
#if UNITY_ANDROID || UNITY_IOS
    Handheld.Vibrate();
#endif

        // Tạo hiệu ứng bounce
        Transform buttonTransform = button.transform;
        Vector3 normalScale = Vector3.one; // FIXED: Về scale bình thường sau click

        // Bounce effect
        float bounceScale = buttonScaleEffect * 1.2f;
        float bounceTime = 0.1f;

        // Scale up quickly
        float elapsedTime = 0f;
        while (elapsedTime < bounceTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / bounceTime;
            float currentScale = Mathf.Lerp(1f, bounceScale, progress);
            buttonTransform.localScale = Vector3.one * currentScale;
            yield return null;
        }

        // Scale back down to normal (not hover state)
        elapsedTime = 0f;
        while (elapsedTime < bounceTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / bounceTime;
            float currentScale = Mathf.Lerp(bounceScale, 1f, progress);
            buttonTransform.localScale = Vector3.one * currentScale;
            yield return null;
        }

        // FIXED: Đảm bảo về scale bình thường
        buttonTransform.localScale = normalScale;

        // Reset màu về bình thường
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.white;
        }

        // Tạo hiệu ứng particle hoặc glow (optional)
        CreateClickParticles(button.transform.position);

        Debug.Log($"🎉 Button clicked with effects: {buttonName}");
    }

    // THÊM: Phương thức reset button về trạng thái bình thường
    public void ResetButtonToNormal(Button button)
    {
        if (button == null) return;

        button.transform.localScale = Vector3.one;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = Color.white;
        }
    }

    // THÊM: Reset tất cả button về trạng thái bình thường
    public void ResetAllButtonsToNormal()
    {
        ResetButtonToNormal(acceptButton);
        ResetButtonToNormal(declineButton);
        ResetButtonToNormal(openQuestButton);
        ResetButtonToNormal(HuyNV);
    } 

    // Tạo hiệu ứng particle khi click (optional)
    private void CreateClickParticles(Vector3 position)
    {
        // Tạo hiệu ứng đơn giản với GameObject tạm thời
        GameObject effect = new GameObject("ClickEffect");
        effect.transform.position = position;

        // Thêm hiệu ứng đơn giản
        StartCoroutine(SimpleClickEffect(effect));
    }

    private IEnumerator SimpleClickEffect(GameObject effect)
    {
        // Tạo hiệu ứng đơn giản - có thể thay thế bằng particle system
        float duration = 0.5f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            // Hiệu ứng có thể được mở rộng ở đây
            yield return null;
        }

        Destroy(effect);
    }

    // Hiệu ứng đặc biệt cho nút Accept
    private IEnumerator AcceptButtonSpecialEffect()
    {
        if (acceptButton == null) yield break;

        // Tạo hiệu ứng đặc biệt cho nút Accept
        Image buttonImage = acceptButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            Color specialColor = Color.green;

            float effectDuration = 0.3f;
            float elapsedTime = 0f;

            // Glow effect
            while (elapsedTime < effectDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.PingPong(elapsedTime * 4f, 1f);
                buttonImage.color = Color.Lerp(originalColor, specialColor, progress * 0.3f);
                yield return null;
            }

            buttonImage.color = originalColor;
        }
    }

    // Hiệu ứng đặc biệt cho nút Decline
    private IEnumerator DeclineButtonSpecialEffect()
    {
        if (declineButton == null) yield break;

        Image buttonImage = declineButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color originalColor = buttonImage.color;
            Color specialColor = Color.red;

            float effectDuration = 0.3f;
            float elapsedTime = 0f;

            while (elapsedTime < effectDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.PingPong(elapsedTime * 4f, 1f);
                buttonImage.color = Color.Lerp(originalColor, specialColor, progress * 0.3f);
                yield return null;
            }

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
                // Lưu vị trí ban đầu
                originalPanelPosition = panelRect.anchoredPosition;

                // Tạo vị trí ẩn (trên màn hình) - sử dụng Canvas height thay vì Screen height
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
        yield return new WaitForSeconds(0.2f); // Đợi hiệu ứng hoàn thành
        AcceptQuest();
    }

    private IEnumerator DelayedDeclineQuest()
    {
        yield return new WaitForSeconds(0.2f); // Đợi hiệu ứng hoàn thành
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
            PanelQuest.SetActive(true);
    }

    public void HideQuestPopup()
    {
        if (PanelQuest != null)
            PanelQuest.SetActive(false);
    }

    private void AcceptQuest()
    {
        HideQuestLogoPanelWithAnimation();
        StartQuest();
    }

    // 🎨 ANIMATION METHODS - Phương thức tạo hiệu ứng đẹp
    public void ShowQuestLogoPanelWithAnimation()
    {
        if (isAnimating || QuestlogoPanel == null) return;

        StartCoroutine(ShowPanelAnimation());
    }

    public void HideQuestLogoPanelWithAnimation()
    {
        if (isAnimating || QuestlogoPanel == null) return;

        StartCoroutine(HidePanelAnimation());
    }

    private IEnumerator ShowPanelAnimation()
    {
        isAnimating = true;

        RectTransform panelRect = QuestlogoPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        // Kích hoạt panel và đặt ở vị trí ẩn
        QuestlogoPanel.SetActive(true);
        panelRect.anchoredPosition = hiddenPanelPosition;

        // Tạo hiệu ứng fade in cho alpha
        CanvasGroup canvasGroup = QuestlogoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = QuestlogoPanel.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;

        // Animation slide xuống và fade in
        float elapsedTime = 0f;
        Vector3 startPos = hiddenPanelPosition;
        Vector3 endPos = originalPanelPosition;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            // Sử dụng animation curve để tạo chuyển động mượt mà
            float easedProgress = animationCurve.Evaluate(progress);

            // Lerp vị trí và alpha
            panelRect.anchoredPosition = Vector3.Lerp(startPos, endPos, easedProgress);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, easedProgress);

            yield return null;
        }

        // Đảm bảo đặt đúng vị trí cuối
        panelRect.anchoredPosition = endPos;
        canvasGroup.alpha = 1f;

        isAnimating = false;
        Debug.Log("✅ Quest logo panel showed with animation");
    }

    private IEnumerator HidePanelAnimation()
    {
        isAnimating = true;

        RectTransform panelRect = QuestlogoPanel.GetComponent<RectTransform>();
        if (panelRect == null) yield break;

        CanvasGroup canvasGroup = QuestlogoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = QuestlogoPanel.AddComponent<CanvasGroup>();
        }

        // Animation slide lên và fade out
        float elapsedTime = 0f;
        Vector3 startPos = originalPanelPosition;
        Vector3 endPos = hiddenPanelPosition;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;

            float easedProgress = animationCurve.Evaluate(progress);

            panelRect.anchoredPosition = Vector3.Lerp(startPos, endPos, easedProgress);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, easedProgress);

            yield return null;
        }

        // Ẩn panel
        QuestlogoPanel.SetActive(false);
        panelRect.anchoredPosition = originalPanelPosition;
        canvasGroup.alpha = 1f;

        isAnimating = false;
        Debug.Log("✅ Quest logo panel hidden with animation");
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

    // 🎨 Phương thức này thay thế cho AcpQuestlogo cũ
    public void AcpQuestlogo()
    {
        if (QuestlogoPanel == null || PanelQuest == null)
        {
            RefreshUI();
            return;
        }

        // Sử dụng animation thay vì SetActive trực tiếp
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
        {
            CoinManager.Instance.AddCoins(currentQuest.coinReward);

            if (successRewardText != null)
            {
                successRewardText.text = "Nhiệm vụ hoàn thành bạn đã nhận được " + currentQuest.coinReward + " coin!";
            }

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
            HuyNV.gameObject.SetActive(false);

        WaypointManager.Instance?.RemoveWaypoint();

        if (PanelSucces != null)
            PanelSucces.SetActive(true);

        if (faileText != null)
        {
            faileText.text = reason;
            faileText.gameObject.SetActive(true);
        }

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
            PanelSucces.SetActive(false);

        if (successRewardText != null)
            successRewardText.text = "";

        if (faileText != null)
        {
            faileText.text = "";
            faileText.gameObject.SetActive(false);
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