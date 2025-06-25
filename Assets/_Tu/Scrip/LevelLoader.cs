using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelLoader : MonoBehaviour
{
    public GameObject loadingScreen;
    public Slider Slider;
    public TextMeshProUGUI ProgressText;

    public float loadingDuration = 4f; // tổng thời gian trượt kéo dài

    public void LoadLevel(int sceneIndex)
    {
        StartCoroutine(LoadAsynchronously(sceneIndex));
    }

    IEnumerator LoadAsynchronously(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        operation.allowSceneActivation = false;

        loadingScreen.SetActive(true);

        float elapsedTime = 0f;
        float progress = 0f;

        while (elapsedTime < loadingDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / loadingDuration);

            // 🎯 EASE OUT EXPO: đầu nhanh, cuối chậm rõ ràng
            float easedProgress = EaseOutExpo(t);

            float actualProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float displayProgress = Mathf.Min(easedProgress, actualProgress);

            Slider.value = displayProgress;
            ProgressText.text = (displayProgress * 100f).ToString("F0") + "%";

            yield return null;
        }

        // đợi load hoàn tất
        while (operation.progress < 0.9f)
        {
            yield return null;
        }

        Slider.value = 1f;
        ProgressText.text = "100%";

        yield return new WaitForSeconds(0.5f);
        operation.allowSceneActivation = true;
    }

    // 🔥 Hàm Easing mạnh mẽ hơn
    float EaseOutExpo(float t)
    {
        return (t >= 1f) ? 1f : 1f - Mathf.Pow(2f, -10f * t);
    }
}
