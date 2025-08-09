using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;
using DG.Tweening;

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

    private void Awake()
    {
        FindReferences();
        StartCoroutine(StartCountdownRoutine());
    }

    private void FindReferences()
    {
        // Tìm Car_script kể cả object ẩn
        playerCar = Resources.FindObjectsOfTypeAll<Car_script>();

        // Tìm OppentCar kể cả object ẩn
        oppentCars = Resources.FindObjectsOfTypeAll<OppentCar>();

        // Tìm OppentCarWaypoint kể cả object ẩn
        waypoints = Resources.FindObjectsOfTypeAll<OppentCarWaypoint>();

        // Tìm countdownTexts theo tên giống LapSystem
        if (countdownTexts == null || countdownTexts.Length == 0)
        {
            var found = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()
                .Where(t => t != null && t.name == "CountdownText")
                .ToArray();
            countdownTexts = found;
        }
    }

    private IEnumerator StartCountdownRoutine()
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

    private void DisableScript()
    {
        foreach (var oppentCar in oppentCars)
            oppentCar.enabled = false;

        foreach (var waypoint in waypoints)
            waypoint.enabled = false;

        foreach (var car in playerCar)
            car.enabled = false;
    }

    private void EnableScript()
    {
        foreach (var oppentCar in oppentCars)
            oppentCar.enabled = true;

        foreach (var waypoint in waypoints)
            waypoint.enabled = true;

        foreach (var car in playerCar)
            car.enabled = true;
    }

    private void UpdateCountdown(string text)
    {
        foreach (var countdownText in countdownTexts)
        {
            countdownText.text = text;
            PlayScaleEffect(countdownText);
        }
    }

    private void UpdateCountdown(float time)
    {
        foreach (var countdownText in countdownTexts)
        {
            countdownText.text = time.ToString("0");
            PlayScaleEffect(countdownText);
        }
    }

    private void SetCountdownTextActive(bool isActive)
    {
        foreach (var countdownText in countdownTexts)
        {
            countdownText.gameObject.SetActive(isActive);
        }
    }

    private void PlayScaleEffect(TextMeshProUGUI text)
    {
        Transform t = text.transform;
        t.DOKill();
        t.localScale = Vector3.one;

        Sequence s = DOTween.Sequence();
        s.Append(t.DOScale(Vector3.one * 1.5f, 0.25f).SetEase(Ease.OutQuad));
        s.Append(t.DOScale(Vector3.one, 0.25f).SetEase(Ease.InQuad));
    }
}
