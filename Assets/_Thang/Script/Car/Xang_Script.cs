using UnityEngine;
using UnityEngine.UI;

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

        if (refuelButton != null)
            refuelButton.onClick.AddListener(() => Refuel(50));

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
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed));
            fuelImage.color = Color.Lerp(originalColor, warningColor, alpha);
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

}