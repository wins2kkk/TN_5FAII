using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ChapNhanButton : MonoBehaviour
{
    public static ChapNhanButton Instance;

    [Header("UI References - Kéo thả trực tiếp trong Inspector")]
    public GameObject panel;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    public Action onConfirmAction;
    public Action onCancelAction;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Chỉ giữ gameObject này, không phải root
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Đảm bảo panel được ẩn khi khởi tạo
        if (panel != null)
            panel.SetActive(false);
    }

    private void Start()
    {
        // Chỉ tìm GameObject nếu chưa được gán trong Inspector
        if (panel == null)
        {
            FindUIElements();
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private void FindUIElements()
    {
        panel = GameObject.Find("ChapNhanPanel");
        if (panel == null)
        {
            Debug.LogError("Không tìm thấy panel tên 'ChapNhanPanel'");
            return;
        }

        messageText = GameObject.Find("ChapNhanMessage")?.GetComponent<TextMeshProUGUI>();
        confirmButton = GameObject.Find("ConfirmButton")?.GetComponent<Button>();
        cancelButton = GameObject.Find("CancelButton")?.GetComponent<Button>();

        if (messageText == null) Debug.LogError("Không tìm thấy ChapNhanMessage");
        if (confirmButton == null) Debug.LogError("Không tìm thấy ConfirmButton");
        if (cancelButton == null) Debug.LogError("Không tìm thấy CancelButton");
    }

    public void Show(string message, Action onConfirm, Action onCancel)
    {
        // Kiểm tra lại các references nếu bị null (do chuyển scene)
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

