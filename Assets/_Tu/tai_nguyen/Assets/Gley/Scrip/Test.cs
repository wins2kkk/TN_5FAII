using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Test : MonoBehaviour
{
    public Text rewardText;
    int rewardValue;

    void Start()
    {
        Gley.DailyRewards.API.Calendar.AddClickListener(CalendarButtonClicked);
    }

    private void CalendarButtonClicked(int dayNumber, int reward, Sprite rewardSprite)
    {
        UnityEngine.Debug.Log($"Click {dayNumber} = {reward}");

        rewardValue += reward;
        rewardText.text = rewardValue.ToString();
    }

    public void ShowCalendar()
    {
        Gley.DailyRewards.API.Calendar.Show();
    }
}
