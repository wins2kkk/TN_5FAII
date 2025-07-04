using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ChapNhanButton : MonoBehaviour
{
    public static ChapNhanButton Instance;

    [Header("UI References - Kéo thả trong Inspector")]
    public GameObject panel;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        // Nếu chưa gán từ Inspector thì thử tìm theo tên (không khuyến khích)
        if (panel == null || messageText == null || confirmButton == null || cancelButton == null)
        {
            FindUIElements();
        }

        if (panel != null)
            panel.SetActive(false);
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

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        if (panel == null || messageText == null || confirmButton == null || cancelButton == null)
        {
            FindUIElements();
            if (panel == null) return;
        }

        panel.SetActive(true);
        messageText.text = message;

        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();

        onConfirmAction = onConfirm;
        onCancelAction = onCancel;

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
            panel.SetActive(false);
    }
}
