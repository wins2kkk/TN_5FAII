using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class SpiningManager : MonoBehaviour
{
    public GameObject wheelPanel;
    public Button spinButton;
    public Button adButton;
    public Button freeSpinButton; // Nút nhận lượt quay miễn phí
    public TMP_Text winText;
    public TMP_Text countdownText; // Text hiển thị thời gian còn lại
    

    [Header("Danh sách phần thưởng (coins)")]
    public int[] PrizeCoins = { 300, 100, 500, 100, 100, 200, 100, 200 };
    [HideInInspector] public string[] PrizeName;

    [Header("Số phần thưởng")]
    public int section = 8;

    [Header("Thời gian cooldown (giây)")]
    public int cooldownSeconds = 10;

    private int randVal;
    private float timeInterval;
    private bool isSpinning; // Đổi tên để rõ ràng hơn
    private int finalAngle;
    private float totalAngle;

    private DateTime nextSpinTime;
    private DateTime nextAdTime;
    private int freeSpinCount = 0;

    const string SPIN_KEY = "NextSpinTime";
    const string AD_KEY = "NextAdTime";
    const string FREE_SPIN_KEY = "FreeSpinCount";

    private void Start()
    {
        isSpinning = false;
        totalAngle = 360f / section;

        // Tự tạo PrizeName từ PrizeCoins
        PrizeName = new string[PrizeCoins.Length];
        for (int i = 0; i < PrizeCoins.Length; i++)
        {
            PrizeName[i] = PrizeCoins[i].ToString();
        }

        LoadCooldownTimes();
        UpdateUI();
        winText.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateUI();
    }

    public void OpenWheelPanel()
    {
        wheelPanel.SetActive(true);
    }

    public void CloseWheelPanel()
    {
        wheelPanel.SetActive(false);
    }

    public void OnSpinButtonClicked()
    {
        // Kiểm tra xem có đang quay không
        if (isSpinning) return;

        if (CanSpin())
        {
            StartCoroutine(Spin());
        }
        else
        {
            Debug.Log("Chưa đến thời gian quay lại!");
        }
    }

    public void OnAdButtonClicked()
    {
        if (CanWatchAd())
        {
            Debug.Log("Đã xem quảng cáo!");

            freeSpinCount++;
            PlayerPrefs.SetInt(FREE_SPIN_KEY, freeSpinCount);

            nextAdTime = DateTime.Now.AddHours(2);
            PlayerPrefs.SetString(AD_KEY, nextAdTime.ToBinary().ToString());
            PlayerPrefs.Save();

            UpdateUI();
        }
        else
        {
            Debug.Log("Chưa đủ thời gian để xem quảng cáo tiếp.");
        }
    }

    public void OnFreeSpinButtonClicked()
    {
        // Nút để nhận lượt quay miễn phí (có thể là reward video, daily bonus, etc.)
        freeSpinCount++;
        PlayerPrefs.SetInt(FREE_SPIN_KEY, freeSpinCount);
        PlayerPrefs.Save();

        Debug.Log("Đã nhận 1 lượt quay miễn phí!");
        UpdateUI();
    }

    private bool CanSpin()
    {
        return DateTime.Now >= nextSpinTime || freeSpinCount > 0;
    }

    private bool CanWatchAd()
    {
        return DateTime.Now >= nextAdTime;
    }

    private IEnumerator Spin()
    {
        isSpinning = true;
        winText.gameObject.SetActive(false);
        UpdateUI();

        randVal = UnityEngine.Random.Range(200, 300);
        timeInterval = 0.0001f * Time.deltaTime * 2;

        for (int i = 0; i < randVal; i++)
        {
            transform.Rotate(0, 0, (totalAngle / 2));

            if (i > Mathf.RoundToInt(randVal * 0.2f))
                timeInterval = 0.5f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.5f))
                timeInterval = 1f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.7f))
                timeInterval = 1.5f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.8f))
                timeInterval = 2f * Time.deltaTime;
            if (i > Mathf.RoundToInt(randVal * 0.9f))
                timeInterval = 2.5f * Time.deltaTime;

            yield return new WaitForSeconds(timeInterval);
        }

        if (Mathf.RoundToInt(transform.eulerAngles.z) % totalAngle != 0)
            transform.Rotate(0, 0, totalAngle / 2);

        finalAngle = Mathf.RoundToInt(transform.eulerAngles.z);
        int prizeIndex = (int)(finalAngle / totalAngle) % section;

        int prizeCoin = PrizeCoins[prizeIndex];
        winText.text = $"Bạn nhận được: {prizeCoin} coins";
        winText.gameObject.SetActive(true);
        CoinManager.Instance.AddCoins(prizeCoin);

        // Xử lý cooldown và free spin
        if (freeSpinCount > 0)
        {
            freeSpinCount--;
            PlayerPrefs.SetInt(FREE_SPIN_KEY, freeSpinCount);
        }
        else
        {
            nextSpinTime = DateTime.Now.AddSeconds(cooldownSeconds);
            PlayerPrefs.SetString(SPIN_KEY, nextSpinTime.ToBinary().ToString());
        }

        PlayerPrefs.Save();

        // Kết thúc quay
        isSpinning = false;

        // Hiển thị kết quả 3 giây
        yield return new WaitForSeconds(3f);
        winText.gameObject.SetActive(false);

        // Cập nhật UI
        UpdateUI();
    }

    private void LoadCooldownTimes()
    {
        // Load thời gian quay tiếp theo
        if (PlayerPrefs.HasKey(SPIN_KEY))
        {
            try
            {
                nextSpinTime = DateTime.FromBinary(Convert.ToInt64(PlayerPrefs.GetString(SPIN_KEY)));
            }
            catch (System.Exception)
            {
                nextSpinTime = DateTime.MinValue;
            }
        }
        else
        {
            nextSpinTime = DateTime.MinValue;
        }

        // Load thời gian xem quảng cáo tiếp theo
        if (PlayerPrefs.HasKey(AD_KEY))
        {
            try
            {
                nextAdTime = DateTime.FromBinary(Convert.ToInt64(PlayerPrefs.GetString(AD_KEY)));
            }
            catch (System.Exception)
            {
                nextAdTime = DateTime.MinValue;
            }
        }
        else
        {
            nextAdTime = DateTime.MinValue;
        }

        // Load số lượt quay miễn phí
        if (PlayerPrefs.HasKey(FREE_SPIN_KEY))
        {
            freeSpinCount = PlayerPrefs.GetInt(FREE_SPIN_KEY);
        }
        else
        {
            freeSpinCount = 0;
        }
    }

    private void UpdateUI()
    {
        // Nút quay: luôn hiện nhưng chỉ có thể bấm khi không đang quay và có thể quay
        spinButton.gameObject.SetActive(true);
        spinButton.interactable = !isSpinning && CanSpin();

        // Nút quảng cáo
        adButton.interactable = CanWatchAd();

        // Nút nhận lượt quay miễn phí (luôn có thể bấm)
        if (freeSpinButton != null)
            freeSpinButton.interactable = true;

        // Cập nhật text countdown
        if (countdownText != null)
        {
            if (freeSpinCount > 0)
            {
                countdownText.text = $"Lượt quay miễn phí: {freeSpinCount}";
            }
            else if (DateTime.Now >= nextSpinTime)
            {
                countdownText.text = "Quay Thôi Nào!";
            }
            else
            {
                TimeSpan timeLeft = nextSpinTime - DateTime.Now;
                countdownText.text = $"Còn lại: {timeLeft.TotalSeconds:F0}s";
            }
        }

        

        // Thay đổi màu nút dựa trên trạng thái
        if (spinButton.interactable)
        {
            spinButton.GetComponent<Image>().color = Color.white;
        }
        else
        {
            spinButton.GetComponent<Image>().color = Color.gray;
        }
    }


}