using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Rendering; // Thêm DOTween
using System.Linq; // Để dùng FirstOrDefault
public class RaceCountdown : MonoBehaviour
{
    public TextMeshProUGUI[] countdownTexts;
    public float countdownTime = 3f;

    private Car_script[] playerCar;
    private OppentCar[] oppentCars;
    private OppentCarWaypoint[] waypoints;
    [Header("Countdown Audio")]
    public AudioSource audioSource;
    public AudioClip countdownClip; // File 3-2-1-GO

    void Awake()
    {
        // Tìm tất cả Car_script kể cả bị disable
        playerCar = Resources.FindObjectsOfTypeAll<Car_script>();

        // Tìm tất cả OppentCar kể cả bị disable
        oppentCars = Resources.FindObjectsOfTypeAll<OppentCar>();

        // Tìm tất cả OppentCarWaypoint kể cả bị disable
        waypoints = Resources.FindObjectsOfTypeAll<OppentCarWaypoint>();

        // Tìm các text countdown kể cả bị disable
        if (countdownTexts == null || countdownTexts.Length == 0)
        {
            countdownTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .Where(t => t != null && t.name == "CountdownText")
                .ToArray();
        }

        StartCoroutine(StartCountdownRoutine());
    }
    IEnumerator StartCountdownRoutine()
    {
        DisableScript();

        // Phát âm thanh toàn bộ đếm 3-2-1-GO
        if (audioSource && countdownClip)
            audioSource.PlayOneShot(countdownClip);

        float currentTime = countdownTime;
        while (currentTime > 0)
        {
            UpdateCountdown(currentTime);
            yield return new WaitForSeconds(0.7f);
            currentTime--;
        }

        EnableScript();
        UpdateCountdown("GO!");
        yield return new WaitForSeconds(1f);
        SetCountdownTextActive(false);
    }


    void DisableScript()
    {
        foreach (OppentCar oppentCar in oppentCars)
        {
            oppentCar.enabled = false;
        }
        foreach (OppentCarWaypoint waypoint in waypoints)
        {
            waypoint.enabled = false;
        }

       foreach (Car_script car in playerCar)
        {
            car.enabled = false;
        }
    }

    void EnableScript()
    {
        foreach (OppentCar oppentCar in oppentCars)
        {
            oppentCar.enabled = true;
        }
        foreach (OppentCarWaypoint waypoint in waypoints)
        {
            waypoint.enabled = true;
        }

        foreach (Car_script car in playerCar)
        {
            car.enabled = true;
        }
    }

    void UpdateCountdown(string text)
    {
        foreach (TextMeshProUGUI countdownText in countdownTexts)
        {
            countdownText.text = text;
            PlayScaleEffect(countdownText);
        }
    }

    void UpdateCountdown(float time)
    {
        foreach (TextMeshProUGUI countdownText in countdownTexts)
        {
            countdownText.text = time.ToString("0");
            PlayScaleEffect(countdownText);
        }
    }

    void SetCountdownTextActive(bool isActive)
    {
        foreach (TextMeshProUGUI countdownText in countdownTexts)
        {
            countdownText.gameObject.SetActive(isActive);
        }
    }

    void PlayScaleEffect(TextMeshProUGUI text)
    {
        Transform t = text.transform;

        // Dừng animation cũ nếu đang chạy
        t.DOKill();

        // Reset scale
        t.localScale = Vector3.one;

        // Sequence mượt
        Sequence s = DOTween.Sequence();
        s.Append(t.DOScale(Vector3.one * 1.5f, 0.25f).SetEase(Ease.OutQuad));
        s.Append(t.DOScale(Vector3.one, 0.25f).SetEase(Ease.InQuad));
    }

}
