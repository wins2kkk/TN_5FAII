using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class nhac : MonoBehaviour
{
    [Header("Chỉ phát trong scene này")]
    public string targetSceneName = "Menu";

    [Header("Slider điều chỉnh âm lượng (gán từ Inspector)")]
    public Slider volumeSlider;

    private AudioSource musicSource;

    void Start()
    {
        musicSource = GetComponent<AudioSource>();

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == targetSceneName)
        {
            musicSource.loop = true;
            musicSource.Play();

            // Gán giá trị ban đầu cho slider nếu có
            if (volumeSlider != null)
            {
                volumeSlider.value = musicSource.volume;
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
        }
        else
        {
            musicSource.Stop();
            Destroy(gameObject);
        }
    }

    public void SetVolume(float value)
    {
        musicSource.volume = value;
    }
}
