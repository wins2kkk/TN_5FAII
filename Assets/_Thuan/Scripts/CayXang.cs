using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CayXang : MonoBehaviour
{
    [Header("Cài đặt")]
    public float refillDuration = 5f; // thời gian để đổ đầy xăng
    public int fuelCost = 50;
    public string carTag = "Player";

    [Header("UI xác nhận")]
    public GameObject confirmPanel;
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI messageText; // "Đổ xăng với 50 coin?"

    private Transform carTransform;
    private Xang_Script fuelScript; // Sửa tên class từ Xang_Script
    private Coroutine refuelCoroutine;
    private bool hasShownPanel = false;
    private bool isRefueling = false;

    public TextMeshProUGUI refuelTimeText; // 👈 Text hiển thị thời gian đổ


    private void Start()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmRefuel);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancel);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasShownPanel && other.CompareTag(carTag))
        {
            carTransform = other.transform;
            fuelScript = carTransform.GetComponent<Xang_Script>(); // Sử dụng đúng tên class

            if (fuelScript == null)
            {
                Debug.LogWarning("🚫 Xe không có Xang_Script component");
                return;
            }

            ShowConfirmPanel();
        }
    }

    private void ShowConfirmPanel()
    {
        if (confirmPanel != null)
        {
            hasShownPanel = true;
            
            ChapNhanButton.Instance.Show( $"Đổ xăng với {fuelCost} coin?", OnConfirmRefuel, OnCancel);


            if (messageText != null)
                messageText.text = $"Đổ xăng với {fuelCost} coin?";
        }
    }

    private void OnConfirmRefuel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (fuelScript == null)
        {
            Debug.LogError("🚫 Không tìm thấy fuel script");
            return;
        }

        // Kiểm tra CoinManager tồn tại
        if (CoinManager.Instance == null)
        {
            Debug.LogError("🚫 CoinManager.Instance không tồn tại");
            return;
        }

        // Kiểm tra đủ coin
        if (!CoinManager.Instance.SpendCoins(fuelCost))
        {
            Debug.Log("🚫 Không đủ coin để đổ xăng");
            return;
        }

        // Dừng coroutine cũ nếu có
        if (refuelCoroutine != null)
            StopCoroutine(refuelCoroutine);

        refuelCoroutine = StartCoroutine(GradualRefuel());
    }

    private IEnumerator GradualRefuel()
    {
        isRefueling = true;
        float initialFuel = fuelScript.currentFuel;
        float targetFuel = fuelScript.maxFuel;
        float elapsed = 0f;

        Debug.Log($"🚗 Bắt đầu đổ xăng từ {initialFuel:F1} đến {targetFuel:F1}");

        while (elapsed < refillDuration && isRefueling)
        {
            elapsed += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsed / refillDuration);

            // Cập nhật fuel
            fuelScript.currentFuel = Mathf.Lerp(initialFuel, targetFuel, percent);

            // Cập nhật UI slider
            if (fuelScript.fuelSlider != null)
                fuelScript.fuelSlider.value = fuelScript.currentFuel;

            // Reset trạng thái hết xăng nếu có
            if (fuelScript.currentFuel > 0 && fuelScript.isOutOfFuel)
            {
                fuelScript.Refuel();
            }

            // 👇 Cập nhật UI thời gian còn lại
            if (refuelTimeText != null)
            {
                float timeLeft = refillDuration - elapsed;
                refuelTimeText.text = $"Đang đổ xăng: {timeLeft:F1}s";
                refuelTimeText.gameObject.SetActive(true); // đảm bảo nó hiện
            }

            yield return null;
        }


        // Đảm bảo fuel được set đúng giá trị cuối
        if (isRefueling)
        {
            fuelScript.currentFuel = targetFuel;
            if (fuelScript.fuelSlider != null)
                fuelScript.fuelSlider.value = targetFuel;
        }

        isRefueling = false;
        if (refuelTimeText != null)
            refuelTimeText.gameObject.SetActive(false); // ẩn sau khi xong

        Debug.Log("✅ Kết thúc quá trình đổ xăng");
    }

    private void OnCancel()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        Debug.Log("❌ Hủy đổ xăng");
        ResetState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(carTag))
        {
            if (isRefueling)
            {
                Debug.Log($"⛔ Người chơi rời cây xăng giữa chừng, đổ được {fuelScript.currentFuel:F1}");
            }

            // Dừng coroutine
            if (refuelCoroutine != null)
                StopCoroutine(refuelCoroutine);

            ResetState();
        }
    }

    private void ResetState()
    {
        isRefueling = false;
        hasShownPanel = false;
        carTransform = null;
        fuelScript = null;
        refuelCoroutine = null;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
        if (refuelTimeText != null)
        {
            refuelTimeText.text = "";
            refuelTimeText.gameObject.SetActive(false);
        }

    }
}