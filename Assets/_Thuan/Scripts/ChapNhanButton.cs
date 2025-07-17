using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Collections;
public class ChapNhanButton : MonoBehaviour
{
    public static ChapNhanButton Instance;

    [Header("UI References - Kéo thả trong Inspector")]
    public GameObject panel;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public Ease animationEase = Ease.OutBack;

    [Header("Button Effect Settings")]
    public float buttonHoverScale = 1.1f;
    public float buttonPressScale = 0.95f;
    public float buttonEffectDuration = 0.15f;

    private Action onConfirmAction;
    private Action onCancelAction;
    private Vector3 originalPanelPosition;
    private Vector3 hiddenPanelPosition;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
        {
            panel.SetActive(false);
            originalPanelPosition = panel.transform.position;
            hiddenPanelPosition = originalPanelPosition + new Vector3(0, Screen.height, 0);
        }
    }

    private void Start()
    {
        if (panel == null || messageText == null || confirmButton == null || cancelButton == null)
        {
            FindUIElements();
        }

        if (panel != null)
        {
            panel.SetActive(false);
            SetupButtonEffects();
        }
    }

    private void FindUIElements()
    {
        panel = GameObject.Find("ChapNhanPanel");
        messageText = GameObject.Find("ChapNhanMessage")?.GetComponent<TextMeshProUGUI>();
        confirmButton = GameObject.Find("ConfirmButton")?.GetComponent<Button>();
        cancelButton = GameObject.Find("CancelButton")?.GetComponent<Button>();

        if (panel == null) Debug.LogError("Không tìm thấy 'ChapNhanPanel'");
        if (messageText == null) Debug.LogError("Không tìm thấy 'ChapNhanMessage'");
        if (confirmButton == null) Debug.LogError("Không tìm thấy 'ConfirmButton'");
        if (cancelButton == null) Debug.LogError("Không tìm thấy 'CancelButton'");
    }

    private void SetupButtonEffects()
    {
        if (confirmButton != null)
        {
            EventTrigger triggerConfirm = confirmButton.gameObject.GetComponent<EventTrigger>() ?? confirmButton.gameObject.AddComponent<EventTrigger>();
            triggerConfirm.triggers.Clear();

            EventTrigger.Entry hoverEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEnter.callback.AddListener((data) => StartCoroutine(ButtonHoverEffect(confirmButton, true)));
            triggerConfirm.triggers.Add(hoverEnter);

            EventTrigger.Entry hoverExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            hoverExit.callback.AddListener((data) => StartCoroutine(ButtonHoverEffect(confirmButton, false)));
            triggerConfirm.triggers.Add(hoverExit);

            EventTrigger.Entry pressDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pressDown.callback.AddListener((data) => StartCoroutine(ButtonPressEffect(confirmButton, true)));
            triggerConfirm.triggers.Add(pressDown);

            EventTrigger.Entry pressUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pressUp.callback.AddListener((data) => StartCoroutine(ButtonPressEffect(confirmButton, false)));
            triggerConfirm.triggers.Add(pressUp);
        }

        if (cancelButton != null)
        {
            EventTrigger triggerCancel = cancelButton.gameObject.GetComponent<EventTrigger>() ?? cancelButton.gameObject.AddComponent<EventTrigger>();
            triggerCancel.triggers.Clear();

            EventTrigger.Entry hoverEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hoverEnter.callback.AddListener((data) => StartCoroutine(ButtonHoverEffect(cancelButton, true)));
            triggerCancel.triggers.Add(hoverEnter);

            EventTrigger.Entry hoverExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            hoverExit.callback.AddListener((data) => StartCoroutine(ButtonHoverEffect(cancelButton, false)));
            triggerCancel.triggers.Add(hoverExit);

            EventTrigger.Entry pressDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pressDown.callback.AddListener((data) => StartCoroutine(ButtonPressEffect(cancelButton, true)));
            triggerCancel.triggers.Add(pressDown);

            EventTrigger.Entry pressUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pressUp.callback.AddListener((data) => StartCoroutine(ButtonPressEffect(cancelButton, false)));
            triggerCancel.triggers.Add(pressUp);
        }
    }

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        if (panel == null || messageText == null || confirmButton == null || cancelButton == null)
        {
            FindUIElements();
            if (panel == null) return;
            SetupButtonEffects();
        }

        panel.SetActive(true);
        panel.transform.position = hiddenPanelPosition;
        messageText.text = message;

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        onConfirmAction = onConfirm;
        onCancelAction = onCancel;

        // Hiệu ứng trượt từ trên xuống
        panel.transform.DOMove(originalPanelPosition, animationDuration).SetEase(animationEase);

        confirmButton.onClick.AddListener(() =>
        {
            onConfirmAction?.Invoke();
            Hide();
        });

        cancelButton.onClick.AddListener(() =>
        {
            onCancelAction?.Invoke();
            Hide();
        });
    }

    public void Hide()
    {
        if (panel != null)
        {
            // Hiệu ứng trượt lên trên
            panel.transform.DOMove(hiddenPanelPosition, animationDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => panel.SetActive(false));
        }
    }

    private IEnumerator ButtonHoverEffect(Button button, bool isHovering)
    {
        if (button == null) yield break;

        Transform buttonTransform = button.transform;
        float targetScale = isHovering ? buttonHoverScale : 1f;

        yield return buttonTransform.DOScale(targetScale, buttonEffectDuration)
            .SetEase(Ease.OutQuad)
            .WaitForCompletion();
    }

    private IEnumerator ButtonPressEffect(Button button, bool isPressed)
    {
        if (button == null) yield break;

        Transform buttonTransform = button.transform;
        float targetScale = isPressed ? buttonPressScale : (buttonTransform.localScale.x == buttonHoverScale ? buttonHoverScale : 1f);

        yield return buttonTransform.DOScale(targetScale, buttonEffectDuration * 0.5f)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        if (!isPressed && buttonTransform.localScale.x != buttonHoverScale)
        {
            yield return buttonTransform.DOScale(1f, buttonEffectDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .WaitForCompletion();
        }
    }
}