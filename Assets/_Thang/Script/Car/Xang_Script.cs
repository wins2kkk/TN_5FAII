using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Xang_Script : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel;
    public float drainRateMoving = 2f;
    public float boostMultiplier = 2f;
    public Slider fuelSlider;

    [Header("Low Fuel Warning")]
    public Image fuelImage;
    public float lowFuelThreshold = 20f;
    public Color warningColor = Color.red;
    public float flashSpeed = 4f;

    [Header("No Fuel Panel")]
    public GameObject noFuelPanel;        // Panel hiện khi hết xăng
    public Button refuelButton;           // Nút đổ đầy xăng trong panel

    private Rigidbody rb;
    private Car_script carScript;
    private Color originalColor;


    //am_thanh_an_xang
    // audio_nangluong
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip energyPickupSound;
    //an thanh het xag
    [Header("Low Fuel Audio")]
    public AudioClip lowFuelWarningSound;   // Âm thanh cảnh báo gần hết xăng
    public float lowFuelSoundCooldown = 5f; // Thời gian giữa mỗi lần phát âm thanh
    private float lastLowFuelSoundTime;
    


    // Thay đổi từ private thành public để CayXang có thể truy cập
    [HideInInspector] public bool isOutOfFuel = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        carScript = GetComponent<Car_script>();
        currentFuel = maxFuel;

        if (fuelSlider != null)
        {
            fuelSlider.maxValue = maxFuel;
            fuelSlider.value = currentFuel;
        }

        if (fuelImage != null)
        {
            originalColor = fuelImage.color;
        }

        if (noFuelPanel != null)
            noFuelPanel.SetActive(false);

        // Sửa nút thành xem quảng cáo
        if (refuelButton != null)
            refuelButton.onClick.AddListener(ShowRefuelAd);
    }

    void Update()
    {
        bool isMoving = rb.velocity.magnitude > 0.1f;

        if (!isOutOfFuel && currentFuel > 0f && isMoving)
        {
            float drainMultiplier = carScript != null && carScript.control == Car_script.ControlMode.Keyboard &&
                                    Input.GetKey(KeyCode.LeftShift) ? boostMultiplier : 1f;

            currentFuel -= drainRateMoving * drainMultiplier * Time.deltaTime;
            currentFuel = Mathf.Max(0, currentFuel);
        }

        if (fuelSlider != null)
        {
            fuelSlider.value = currentFuel;
        }

        HandleLowFuelWarning();
        HandleOutOfFuel();
    }

    void HandleLowFuelWarning()
    {
        if (fuelImage == null) return;

        if (currentFuel <= lowFuelThreshold)
        {
            // Hiệu ứng nhấp nháy
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed));
            fuelImage.color = Color.Lerp(originalColor, warningColor, alpha);

            // Phát âm thanh cảnh báo nếu cooldown đã hết
            if (audioSource != null && lowFuelWarningSound != null && Time.time - lastLowFuelSoundTime >= lowFuelSoundCooldown)
            {
                audioSource.volume = AudioManager.Instance != null ? AudioManager.Instance.effectsVolume : 1f;
                audioSource.PlayOneShot(lowFuelWarningSound);
                lastLowFuelSoundTime = Time.time;
            }
        }
        else
        {
            fuelImage.color = originalColor;
        }
    }


    void HandleOutOfFuel()
    {
        if (currentFuel <= 0f && !isOutOfFuel)
        {
            isOutOfFuel = true;

            if (carScript != null)
                carScript.maximumMotorTorque = 0f;

            if (noFuelPanel != null)
                noFuelPanel.SetActive(true);
        }
    }

    public void Refuel(int cost = 50)
    {
        bool hasEnough = CoinManager.Instance.HasEnoughCoins(cost);

        if (hasEnough)
        {
            CoinManager.Instance.SpendCoins(cost);
            Debug.Log($"✅ Đã trừ {cost} coin để đổ xăng.");
        }
        else
        {
            Debug.Log("⚠️ Không đủ coin, đổ xăng miễn phí.");
        }

        currentFuel = maxFuel;
        isOutOfFuel = false;

        if (carScript != null)
            carScript.maximumMotorTorque = 1500f;

        if (noFuelPanel != null)
            noFuelPanel.SetActive(false);

        if (fuelSlider != null)
            fuelSlider.value = currentFuel;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Xang"))
        {
            Debug.Log("Đã nhặt bình xăng!");

            currentFuel = maxFuel;
            isOutOfFuel = false;

            if (carScript != null)
                carScript.maximumMotorTorque = 1500f;

            if (fuelSlider != null)
                fuelSlider.value = currentFuel;

            if (fuelImage != null)
                fuelImage.color = originalColor;

            if (noFuelPanel != null)
                noFuelPanel.SetActive(false);
            if (audioSource != null && energyPickupSound != null && AudioManager.Instance != null)
            {
                audioSource.volume = AudioManager.Instance.effectsVolume;
                audioSource.PlayOneShot(energyPickupSound);
            }
            other.gameObject.SetActive(false);
            StartCoroutine(RespawnFuel(other.gameObject, 60f));

        }
    }
    private IEnumerator RespawnFuel(GameObject fuelObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        fuelObject.SetActive(true);
        Debug.Log("⛽ Bình xăng đã xuất hiện lại!");
    }
    private void ShowRefuelAd()
    {
        if (AdsManager.Instance != null)
        {
            // Khi xem xong quảng cáo sẽ gọi Refuel
            AdsManager.Instance.OnRewardedAdWatched += OnAdWatched_Refuel;
            AdsManager.Instance.ShowRewardedAd();
        }
    }
    private void OnAdWatched_Refuel()
    {
        Refuel(0); // Đổ đầy xăng, không trừ coin
        Debug.Log("✅ Xem xong quảng cáo - đã đổ đầy xăng!");

        // Hủy đăng ký để tránh gọi nhiều lần
        if (AdsManager.Instance != null)
            AdsManager.Instance.OnRewardedAdWatched -= OnAdWatched_Refuel;
    }
}