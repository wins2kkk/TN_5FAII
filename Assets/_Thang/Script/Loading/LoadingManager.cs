using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class LoadingManager : MonoBehaviour
{
    [Header("Loading UI Elements")]
    public Slider loadingSlider;
    public Text loadingText;
    public GameObject loadingPanel;

    [Header("Loading Settings")]
    public float minimumLoadTime = 2f;

    void Start()
    {
        string sceneToLoad = PlayerPrefs.GetString("SceneToLoad", "");
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(LoadSceneAsync(sceneToLoad));
            PlayerPrefs.DeleteKey("SceneToLoad"); // Xóa sau khi dùng
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Reset slider
        if (loadingSlider != null)
            loadingSlider.value = 0f;

        // Bắt đầu load scene
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        float timer = 0f;

        while (!asyncOperation.isDone)
        {
            timer += Time.deltaTime;

            // Tính toán progress
            float progress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            float timeProgress = timer / minimumLoadTime;
            float finalProgress = Mathf.Min(progress, timeProgress);

            // Cập nhật UI
            if (loadingSlider != null)
                loadingSlider.value = finalProgress;

            if (loadingText != null)
                loadingText.text = "Loading... " + Mathf.RoundToInt(finalProgress * 100) + "%";

            // Khi loading hoàn tất và đã đủ thời gian tối thiểu
            if (asyncOperation.progress >= 0.9f && timer >= minimumLoadTime)
            {
                // Hoàn thành thanh loading
                if (loadingSlider != null)
                    loadingSlider.value = 1f;

                if (loadingText != null)
                    loadingText.text = "Loading... 100%";

                yield return new WaitForSeconds(0.5f);

                // Kích hoạt scene mới
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    public void LoadGarageCar1()
    {
        PlayerPrefs.SetString("SceneToLoad", "Menu");
        SceneManager.LoadScene("Loading");
    }

    public void LoadThanh_pho()
    {
        PlayerPrefs.SetString("SceneToLoad", "Thanh_Pho2");
        SceneManager.LoadScene("Loading");
    }
    public void LoadRaceLap()
    {
        PlayerPrefs.SetString("SceneToLoad", "ChayLap");
        SceneManager.LoadScene("Loading");
    }
}

// Script để gọi loading từ các scene khác


