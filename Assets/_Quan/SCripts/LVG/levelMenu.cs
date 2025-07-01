using UnityEngine;
using UnityEngine.UI;

public class levelMenu : MonoBehaviour
{
    public Button[] buttons; // Gán các nút Level1 -> Level4 vào

    private void Awake()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            string levelName = "Level" + (i + 1);
            bool isUnlocked = PlayerPrefs.GetInt(levelName, levelName == "Level1" ? 1 : 0) == 1;
            buttons[i].interactable = isUnlocked;
        }
    }
}
