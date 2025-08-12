using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class LoadingManager : MonoBehaviour
{
    [Header("Loading UI Elements")]
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;
    public GameObject loadingPanel;

    [Header("Loading Settings")]
    public float minimumLoadTime = 2f;

    [Header("Text Animation Settings")]
    public float bounceHeight = 10f;
    public float bounceSpeed = 2f;
    public float letterDelay = 0.1f;

    private string baseLoadingText = "Loading...";
    private Coroutine textAnimationCoroutine;

    void Start()
    {
        string sceneToLoad = PlayerPrefs.GetString("SceneToLoad", "");
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            StartCoroutine(LoadSceneAsync(sceneToLoad));
            PlayerPrefs.DeleteKey("SceneToLoad");
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Reset slider
        if (loadingSlider != null) loadingSlider.value = 0f;

        // Bắt đầu animation text
        if (loadingText != null)
        {
            textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        }

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

            // Cập nhật slider
            if (loadingSlider != null) loadingSlider.value = finalProgress;

            // Khi loading hoàn tất và đã đủ thời gian tối thiểu
            if (asyncOperation.progress >= 0.9f && timer >= minimumLoadTime)
            {
                // Dừng animation text
                if (textAnimationCoroutine != null)
                {
                    StopCoroutine(textAnimationCoroutine);
                }

                // Hoàn thành thanh loading
                if (loadingSlider != null) loadingSlider.value = 1f;
                if (loadingText != null) loadingText.text = "Complete!";

                yield return new WaitForSeconds(0.5f);

                // Kích hoạt scene mới
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private IEnumerator AnimateLoadingText()
    {
        if (loadingText == null) yield break;

        while (true)
        {
            // Tạo hiệu ứng bounce cho từng chữ cái
            for (int i = 0; i < baseLoadingText.Length; i++)
            {
                string animatedText = "";

                for (int j = 0; j < baseLoadingText.Length; j++)
                {
                    if (j == i)
                    {
                        // Chữ cái đang bounce - thêm màu và style
                        animatedText += "<color=yellow><size=120%>" + baseLoadingText[j] + "</size></color>";
                    }
                    else
                    {
                        animatedText += baseLoadingText[j];
                    }
                }

                loadingText.text = animatedText;
                yield return new WaitForSeconds(letterDelay);
            }

            // Thêm hiệu ứng dots animation
            yield return StartCoroutine(AnimateDots());
        }
    }

    private IEnumerator AnimateDots()
    {
        string[] dotAnimations = { ".", "..", "...", "" };

        for (int i = 0; i < dotAnimations.Length; i++)
        {
            loadingText.text = baseLoadingText + dotAnimations[i];
            yield return new WaitForSeconds(0.3f);
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

    public void Loadlevel_1()
    {
        PlayerPrefs.SetString("SceneToLoad", "Level_1");
        SceneManager.LoadScene("Loading");
    }
    public void Loadlevel_2()
    {
        PlayerPrefs.SetString("SceneToLoad", "Level_2");
        SceneManager.LoadScene("Loading");
    }
    public void Loadlevel_3()
    {
        PlayerPrefs.SetString("SceneToLoad", "Level_3");
        SceneManager.LoadScene("Loading");
    }
}