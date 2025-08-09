using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;

public class MapUnlockManager : MonoBehaviour
{
    public static MapUnlockManager Instance;

    public List<int> unlockedMaps = new List<int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Gọi khi đăng nhập thành công
    public void LoadUnlockedMaps()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), OnDataReceived, OnError);
    }

    private void OnDataReceived(GetUserDataResult result)
    {
        if (result.Data != null && result.Data.ContainsKey("UnlockedMaps"))
        {
            string mapsData = result.Data["UnlockedMaps"].Value;
            unlockedMaps.Clear();
            foreach (string mapId in mapsData.Split(','))
            {
                if (int.TryParse(mapId, out int id))
                    unlockedMaps.Add(id);
            }
        }
        else
        {
            unlockedMaps.Clear();
            unlockedMaps.Add(1); // Mặc định mở map 1
        }

        Debug.Log("📜 Maps unlocked: " + string.Join(",", unlockedMaps));
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("❌ Load map error: " + error.GenerateErrorReport());
    }

    public bool IsMapUnlocked(int mapId)
    {
        return unlockedMaps.Contains(mapId);
    }

    public void UnlockMap(int mapId)
    {
        if (!unlockedMaps.Contains(mapId))
        {
            unlockedMaps.Add(mapId);
            SaveUnlockedMaps();
        }
    }

    private void SaveUnlockedMaps()
    {
        string mapsData = string.Join(",", unlockedMaps);
        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
            {
                { "UnlockedMaps", mapsData }
            }
        };
        PlayFabClientAPI.UpdateUserData(request,
            result => Debug.Log("✅ Saved unlocked maps: " + mapsData),
            OnError);
    }
}
