using UnityEngine;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;

[System.Serializable]
public class MapData
{
    public string mapName;    // Tên Level, ví dụ "Level_1"
    public GameObject mapButton; // Nút bấm trong UI
    public bool isUnlocked;   // Trạng thái mở
}

public class MapUnlockManager : MonoBehaviour
{
    public List<MapData> maps = new List<MapData>();
    private bool isPlayFabReady = false;

    void Start()
    {
        // Thiết lập trạng thái mặc định trước
        SetDefaultMapStates();
        // Sau đó load từ PlayFab
        LoadUnlockedMapsFromPlayFab();
    }

    void SetDefaultMapStates()
    {
        foreach (var map in maps)
        {
            if (map.mapName == "Level_1" || map.mapName == "Level 1")
            {
                // Level 1 luôn mở
                map.isUnlocked = true;
                if (map.mapButton != null)
                    map.mapButton.SetActive(true);
                Debug.Log($"✅ {map.mapName} đã được mở mặc định");
            }
            else
            {
                // Các level khác mặc định đóng
                map.isUnlocked = false;
                if (map.mapButton != null)
                    map.mapButton.SetActive(false);
            }
        }
    }

    public void UnlockMap(string mapName)
    {
        Debug.Log($"🔍 Đang cố gắng mở khóa: {mapName}");

        MapData map = maps.Find(m => m.mapName == mapName);
        if (map != null && !map.isUnlocked)
        {
            // MỞ NGAY LẬP TỨC - ưu tiên UI trước
            map.isUnlocked = true;
            if (map.mapButton != null)
            {
                map.mapButton.SetActive(true);
                Debug.Log($"🔓 ĐÃ MỞ NGAY: {mapName} - Button hiện thị!");
            }
            else
            {
                Debug.LogWarning($"⚠ Map button null cho {mapName}");
            }

            // Lưu lên PlayFab sau (không chặn UI)
            SaveUnlockedMapsToPlayFab();
        }
        else if (map == null)
        {
            Debug.LogError($"❌ KHÔNG TÌM THẤY MAP: {mapName}");
            DebugAllMaps();
        }
        else
        {
            Debug.Log($"ℹ Map {mapName} đã được mở từ trước");
        }
    }

    void SaveUnlockedMapsToPlayFab()
    {
        Dictionary<string, string> data = new Dictionary<string, string>();
        foreach (var map in maps)
        {
            data[map.mapName] = map.isUnlocked ? "1" : "0";
        }

        PlayFabClientAPI.UpdateUserData(new UpdateUserDataRequest
        {
            Data = data
        }, result => {
            Debug.Log("✅ Đã lưu trạng thái map lên PlayFab.");
        }, error => {
            Debug.LogError("❌ Lỗi lưu PlayFab: " + error.GenerateErrorReport());
        });
    }

    void LoadUnlockedMapsFromPlayFab()
    {
        PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
        {
            Debug.Log("📥 Đang load dữ liệu từ PlayFab...");
            isPlayFabReady = true;

            foreach (var map in maps)
            {
                if (map.mapName == "Level_1" || map.mapName == "Level 1")
                {
                    // Level 1 luôn mở, không cần check PlayFab
                    map.isUnlocked = true;
                }
                else
                {
                    // Các level khác check từ PlayFab
                    if (result.Data.ContainsKey(map.mapName))
                    {
                        bool wasUnlocked = result.Data[map.mapName].Value == "1";
                        if (wasUnlocked && !map.isUnlocked)
                        {
                            // Nếu PlayFab có data mở nhưng local chưa mở
                            map.isUnlocked = true;
                        }
                    }
                    else
                    {
                        map.isUnlocked = false; // Mặc định đóng nếu chưa có data
                    }
                }

                // Cập nhật UI
                if (map.mapButton != null)
                    map.mapButton.SetActive(map.isUnlocked);
                Debug.Log($"📍 {map.mapName}: {(map.isUnlocked ? "MỞ" : "KHÓA")}");
            }
        }, error =>
        {
            Debug.LogError("❌ Lỗi tải dữ liệu PlayFab: " + error.GenerateErrorReport());
            Debug.Log("⚠ Sử dụng trạng thái mặc định");
            isPlayFabReady = true;
        });
    }

    public void CheckAndUnlockByReward(int cupReward, string currentScene)
    {
        Debug.Log($"🏆 === KIỂM TRA MỞ KHÓA THEO PHẦN THƯỞNG ===");
        Debug.Log($"🏆 Cúp nhận được: {cupReward}");
        Debug.Log($"🎯 Scene hiện tại: {currentScene}");

        // Nếu đạt winReward (thường là 5 cúp) thì mở level tiếp theo
        if (cupReward >= 5) // winReward
        {
            Debug.Log($"🔓 ĐẠT ĐƯỢC {cupReward} CÚP - ĐỦ ĐIỀU KIỆN MỞ LEVEL MỚI!");
            UnlockNextMap(currentScene);
        }
        else
        {
            Debug.Log($"⏳ Chỉ nhận được {cupReward} cúp - chưa đủ để mở level mới (cần 5 cúp)");
        }

        Debug.Log($"🏆 === KẾT THÚC KIỂM TRA ===");
    }

    public void CheckAndUnlockByTotalCups()
    {
        Debug.Log($"🏆 === KIỂM TRA MỞ KHÓA THEO TỔNG SỐ CÚP ===");

        if (TrophyManager.Instance == null)
        {
            Debug.LogError("❌ TrophyManager không tồn tại!");
            return;
        }

        // Sử dụng field currentCupCount từ TrophyManager
        int totalCups = TrophyManager.Instance.currentCupCount;
        Debug.Log($"🏆 Tổng số cúp hiện tại: {totalCups}");

        // Kiểm tra từng level để mở khóa dựa trên số cúp
        CheckUnlockLevel("Level_2", "Level 2", 5, totalCups);   // Cần 5 cúp để mở Level_2
        CheckUnlockLevel("Level_3", "Level 3", 10, totalCups);  // Cần 10 cúp để mở Level_3  
        CheckUnlockLevel("Level_4", "Level 4", 15, totalCups);  // Cần 15 cúp để mở Level_4
        CheckUnlockLevel("Level_5", "Level 5", 20, totalCups);  // Cần 20 cúp để mở Level_5
                                                                // Có thể thêm nhiều level khác...

        Debug.Log($"🏆 === KẾT THÚC KIỂM TRA ===");
    }

    private void CheckUnlockLevel(string levelName1, string levelName2, int requiredCups, int currentCups)
    {
        MapData map = maps.Find(m => m.mapName == levelName1 || m.mapName == levelName2);
        if (map != null && !map.isUnlocked && currentCups >= requiredCups)
        {
            Debug.Log($"🔓 ĐẠT ĐỦ {requiredCups} CÚP! Mở khóa: {map.mapName}");
            UnlockMap(map.mapName);
        }
        else if (map != null && currentCups < requiredCups)
        {
            Debug.Log($"⏳ {map.mapName} cần {requiredCups} cúp (hiện có {currentCups})");
        }
    }

    public void UnlockNextMap(string currentMap)
    {
        Debug.Log($"🎯 === BẮT ĐẦU MỞ MAP TIẾP THEO ===");
        Debug.Log($"🎯 Map hiện tại: {currentMap}");

        // Xử lý nhiều format tên scene khác nhau
        string levelNumber = currentMap.Replace("Level_", "").Replace("Level ", "").Replace("Level", "").Trim();
        Debug.Log($"🔢 Level number parsed: '{levelNumber}'");

        if (int.TryParse(levelNumber, out int currentLevelNum))
        {
            int nextLevelNum = currentLevelNum + 1;
            Debug.Log($"➡ Level tiếp theo: {nextLevelNum}");

            // Thử tất cả các format tên có thể
            string[] possibleNames = {
                $"Level_{nextLevelNum}",
                $"Level {nextLevelNum}",
                $"Level{nextLevelNum}"
            };

            MapData nextMap = null;
            foreach (string possibleName in possibleNames)
            {
                nextMap = maps.Find(m => m.mapName == possibleName);
                if (nextMap != null)
                {
                    Debug.Log($"✅ Tìm thấy map: {possibleName}");
                    break;
                }
            }

            if (nextMap != null)
            {
                if (!nextMap.isUnlocked)
                {
                    Debug.Log($"🔓 ĐANG MỞ KHÓA: {nextMap.mapName}");
                    UnlockMap(nextMap.mapName);
                }
                else
                {
                    Debug.Log($"ℹ Map {nextMap.mapName} đã được mở từ trước");
                }
            }
            else
            {
                Debug.LogWarning($"⚠ KHÔNG TÌM THẤY MAP TIẾP THEO. Đã thử:");
                foreach (string name in possibleNames)
                {
                    Debug.LogWarning($"   - {name}");
                }
                DebugAllMaps();
            }
        }
        else
        {
            Debug.LogError($"❌ Không thể parse level number từ: '{currentMap}'");
        }

        Debug.Log($"🎯 === KẾT THÚC MỞ MAP TIẾP THEO ===");
    }

    // Debug helper methods
    void DebugAllMaps()
    {
        Debug.Log("=== TẤT CẢ MAPS TRONG DANH SÁCH ===");
        for (int i = 0; i < maps.Count; i++)
        {
            var map = maps[i];
            Debug.Log($"[{i}] Name: '{map.mapName}' | Unlocked: {map.isUnlocked} | Button: {(map.mapButton != null ? map.mapButton.name : "NULL")}");
        }
    }

    [ContextMenu("Debug Map States")]
    public void DebugMapStates()
    {
        DebugAllMaps();
    }

    // Method để test kiểm tra cúp theo phần thưởng
    [ContextMenu("Test Check Win Reward")]
    void TestCheckWinReward()
    {
        CheckAndUnlockByReward(5, "Level_1"); // Giả lập nhận 5 cúp ở Level_1
    }

    // Method để test kiểm tra tổng cúp
    [ContextMenu("Test Check Total Cups")]
    void TestCheckTotalCups()
    {
        CheckAndUnlockByTotalCups();
    }

    // Method để reset về trạng thái ban đầu (chỉ Level_1 mở)
    [ContextMenu("Reset All Maps")]
    public void ResetAllMaps()
    {
        foreach (var map in maps)
        {
            if (map.mapName == "Level_1" || map.mapName == "Level 1")
            {
                map.isUnlocked = true;
                if (map.mapButton != null)
                    map.mapButton.SetActive(true);
            }
            else
            {
                map.isUnlocked = false;
                if (map.mapButton != null)
                    map.mapButton.SetActive(false);
            }
        }
        SaveUnlockedMapsToPlayFab();
        Debug.Log("🔄 Đã reset tất cả maps");
    }

    // Method để force mở tất cả maps (để test)
    [ContextMenu("Unlock All Maps")]
    public void UnlockAllMaps()
    {
        foreach (var map in maps)
        {
            map.isUnlocked = true;
            if (map.mapButton != null)
                map.mapButton.SetActive(true);
        }
        SaveUnlockedMapsToPlayFab();
        Debug.Log("🔓 Đã mở tất cả maps");
    }
}