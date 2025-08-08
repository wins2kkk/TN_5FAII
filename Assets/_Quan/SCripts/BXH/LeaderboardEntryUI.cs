using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class LeaderboardEntryUI : MonoBehaviour
{
    public Image avatarImage;
    public TMP_Text nameText;
    public TMP_Text cupText;

    public Image cupIconImage; // thêm icon cúp

    public Sprite cupIconSprite; // sprite cúp mặc định


    public void SetEntry(Sprite avatar, string name, int cup)
    {
        avatarImage.sprite = avatar;
        nameText.text = name;
        cupText.text = cup.ToString();


        if (cupIconImage != null && cupIconSprite != null)
        {
            cupIconImage.sprite = cupIconSprite;
            cupIconImage.enabled = true;
        }

    }

    public void SetColorByRank(int rank)
    {
        Color color;

        switch (rank)
        {
            case 0:
                color = new Color32(255, 215, 0, 255); // Vàng
                break;
            case 1:
                color = new Color32(192, 192, 192, 255); // Bạc
                break;
            case 2:
                color = new Color32(205, 127, 50, 255); // Đồng
                break;
            default:
                color = Color.white;
                break;
        }

        nameText.color = color;
        cupText.color = color;

        if (cupIconImage != null)
        {
            cupIconImage.color = color;
        }
    }

    internal void SetEntry(int v1, string v2, int statValue, Sprite defaultAvatar)
    {
        throw new NotImplementedException();
    }
}
