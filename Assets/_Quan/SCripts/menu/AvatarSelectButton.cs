using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelectButton : MonoBehaviour
{
    public string avatarName;
    public AccountSettingsPanel settingsPanel;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            settingsPanel.OnAvatarSelected(avatarName);
        });
    }
}
