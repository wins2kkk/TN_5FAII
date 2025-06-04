using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class levelMenu : MonoBehaviour
{
    public Button[] buttons;
    //   public GameObject LevelButtons;
    private void Awake()
    {
        //ButtonsToArray();

        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }
        for (int i = 0; i < unlockedLevel; i++)
        {
            buttons[i].interactable = true;
        }


    }

    public void OpenLevel(int levelId)
    {
        string levelName = "Level" + levelId;
        SceneManager.LoadScene(levelName);
    }

    //void ButtonsToArray()
    //{
    //    int childCount = LevelButtons.transform.childCount;
    //    buttons = new Button[childCount];
    //    for (int i = 0; i < childCount; i++)
    //    {

    //        buttons[i] = LevelButtons.transform.GetChild(i).gameObject.GetComponent<Button>();
    //    }
    //}
}