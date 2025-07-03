using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ChapNhanButton : MonoBehaviour
{
    public static ChapNhanButton Instance;

    [Header("UI")]
    public GameObject panel;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    private Action onConfirmAction;
    private Action onCancelAction;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        panel.SetActive(false);
    }

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        panel.SetActive(true);
        messageText.text = message;

        // Xóa các listener cũ
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
        panel.SetActive(false);
    }
}
