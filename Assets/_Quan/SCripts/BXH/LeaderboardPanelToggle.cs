using UnityEngine;

public class LeaderboardPanelToggle : MonoBehaviour
{
    public GameObject leaderboardPanel;

    public void ShowLeaderboard()
    {
        leaderboardPanel.SetActive(true);
    }

    public void HideLeaderboard()
    {
        leaderboardPanel.SetActive(false);
    }
}
