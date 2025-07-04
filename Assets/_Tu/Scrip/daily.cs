using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Daily : MonoBehaviour
{
    public int LastDate;

    public int Day_1;
    public int Day_2;

    public GameObject OFF_1;
    public GameObject ACTIVE_1;
    public GameObject CHECK_1;

    public GameObject OFF_2;
    public GameObject ACTIVE_2;
    public GameObject CHECK_2;

    void Start()
    {
        Day_1 = PlayerPrefs.GetInt("Day_1", 0);
        Day_2 = PlayerPrefs.GetInt("Day_2", 0);
        LastDate = PlayerPrefs.GetInt("LastDate", 0);

        Reward();

        if (LastDate != System.DateTime.Now.Day)
        {
            if (Day_1 == 0)
            {
                Day_1 = 1;
            }
            else if (Day_2 == 0)
            {
                Day_2 = 1;
            }

            Reward();
        }
    }

    public void Reward()
    {
        // Day 1
        if (Day_1 == 0)
        {
            OFF_1.SetActive(true);
            ACTIVE_1.SetActive(false);
            CHECK_1.SetActive(false);
        }
        if (Day_1 == 1)
        {
            OFF_1.SetActive(false);
            ACTIVE_1.SetActive(true);
            CHECK_1.SetActive(false);
        }
        if (Day_1 == 2)
        {
            OFF_1.SetActive(false);
            ACTIVE_1.SetActive(false);
            CHECK_1.SetActive(true);
        }

        // Day 2
        if (Day_2 == 0)
        {
            OFF_2.SetActive(true);
            ACTIVE_2.SetActive(false);
            CHECK_2.SetActive(false);
        }
        if (Day_2 == 1)
        {
            OFF_2.SetActive(false);
            ACTIVE_2.SetActive(true);
            CHECK_2.SetActive(false);
        }
        if (Day_2 == 2)
        {
            OFF_2.SetActive(false);
            ACTIVE_2.SetActive(false);
            CHECK_2.SetActive(true);
        }
    }

    public void GetReward_1()
    {
        LastDate = System.DateTime.Now.Day;
        PlayerPrefs.SetInt("LastDate", LastDate);

        print("Reward 1");

        Day_1 = 2;
        PlayerPrefs.SetInt("Day_1", 2);

        Reward();
    }

    public void GetReward_2()
    {
        LastDate = System.DateTime.Now.Day;
        PlayerPrefs.SetInt("LastDate", LastDate);

        print("Reward 2");

        Day_2 = 2;
        PlayerPrefs.SetInt("Day_2", 2);

        Reward();
    }
}
