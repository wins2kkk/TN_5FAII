using System;
using UnityEngine;

namespace Gley.DailyRewards.Internal
{
    /// <summary>
    /// Used for save
    /// </summary>
    public class TimeMethods
    {
        /// <summary>
        /// Subtract from the current time old time 
        /// </summary>
        /// <param name="oldTime">time to subtract</param>
        /// <returns></returns>
        public static TimeSpan SubtractTime(DateTime oldTime)
        {
            return DateTime.Now.Subtract(oldTime);
        }


        /// <summary>
        /// Load saved time
        /// </summary>
        /// <param name="saveName"></param>
        /// <returns></returns>
        public static DateTime LoadTime(string saveName)
        {
            if (!PlayerPrefs.HasKey(saveName))
            {
                SaveTime(saveName);
                return DateTime.Now;
            }
            else
            {
                long temp = Convert.ToInt64(PlayerPrefs.GetString(saveName));
                return DateTime.FromBinary(temp);
            }
        }

        public static void ResetTime(string saveName)
        {
            if (PlayerPrefs.HasKey(saveName))
            {
                PlayerPrefs.DeleteKey(saveName);
            }
        }


        /// <summary>
        /// Save current time
        /// </summary>
        /// <param name="saveName"></param>
        public static void SaveTime(string saveName)
        {
            PlayerPrefs.SetString(saveName, DateTime.Now.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Save the remaining time
        /// </summary>
        /// <param name="saveName"></param>
        /// <param name="remainingTime"></param>
        internal static void SaveTime(string saveName, TimeSpan remainingTime)
        {
            PlayerPrefs.SetString(saveName, DateTime.Now.Subtract(remainingTime).ToBinary().ToString());
            PlayerPrefs.Save();
        }


        /// <summary>
        /// Load current day
        /// </summary>
        /// <param name="saveName"></param>
        /// <returns></returns>
        public static int LoadDay(string saveName)
        {
            if (!PlayerPrefs.HasKey(saveName))
            {
                SaveDay(saveName, 0);
                return 0;
            }
            else
            {
                return PlayerPrefs.GetInt(saveName);
            }
        }


        public static void ResetDay(string saveName)
        {
            SaveDay(saveName, 0);
        }

        /// <summary>
        /// Save current day
        /// </summary>
        /// <param name="saveName"></param>
        /// <param name="currentDay"></param>
        public static void SaveDay(string saveName, int currentDay)
        {
            PlayerPrefs.SetInt(saveName, currentDay);
            PlayerPrefs.Save();
        }


        /// <summary>
        /// Check if the current save name exists
        /// </summary>
        /// <param name="saveName"></param>
        /// <returns></returns>
        public static bool SaveExists(string saveName)
        {
            return PlayerPrefs.HasKey(saveName);
        }


        /// <summary>
        /// Ads the timer to the current time so that the current button becomes available to click
        /// </summary>
        /// <param name="saveName"></param>
        /// <param name="openTime"></param>
        public static void MakeButtonAvailable(string saveName, TimeSpan openTime)
        {
            DateTime timeToSave = DateTime.Now.Subtract(openTime);
            PlayerPrefs.SetString(saveName, timeToSave.ToBinary().ToString());
            PlayerPrefs.Save();
        }
    }
}


//using System;
//using UnityEngine;
//using PlayFab;
//using PlayFab.ClientModels;

//namespace Gley.DailyRewards.Internal
//{
//    /// <summary>
//    /// Used for save - Updated với PlayFab server time sync
//    /// </summary>
//    public class TimeMethods
//    {
//        // Server time sync variables
//        private static DateTime? serverTime = null;
//        private static DateTime lastSyncTime;
//        private static bool isSyncing = false;

//        /// <summary>
//        /// Initialize và sync server time từ PlayFab
//        /// </summary>
//        public static void Initialize(System.Action onComplete = null, System.Action<string> onError = null)
//        {
//            SyncServerTime(onComplete, onError);
//        }

//        /// <summary>
//        /// Sync server time from PlayFab
//        /// </summary>
//        public static void SyncServerTime(System.Action onComplete = null, System.Action<string> onError = null)
//        {
//            if (isSyncing)
//            {
//                onComplete?.Invoke();
//                return;
//            }

//            isSyncing = true;

//            if (!PlayFabClientAPI.IsClientLoggedIn())
//            {
//                Debug.LogWarning("PlayFab not logged in, using local time");
//                isSyncing = false;
//                onComplete?.Invoke();
//                return;
//            }

//            var request = new GetTimeRequest();

//            PlayFabClientAPI.GetTime(request,
//                result => {
//                    serverTime = result.Time;
//                    lastSyncTime = DateTime.Now;
//                    isSyncing = false;

//                    Debug.Log($"Server time synced: {serverTime}");
//                    onComplete?.Invoke();
//                },
//                error => {
//                    Debug.LogWarning($"Failed to sync server time, using local time: {error.GenerateErrorReport()}");
//                    isSyncing = false;
//                    onComplete?.Invoke(); // Vẫn continue với local time
//                });
//        }

//        /// <summary>
//        /// Get current server time (hoặc local time nếu chưa sync được)
//        /// </summary>
//        public static DateTime GetCurrentTime()
//        {
//            if (serverTime.HasValue)
//            {
//                // Tính toán thời gian server hiện tại dựa trên offset
//                TimeSpan elapsed = DateTime.Now - lastSyncTime;
//                return serverTime.Value.Add(elapsed);
//            }

//            // Fallback to local time nếu chưa sync được
//            return DateTime.Now;
//        }

//        /// <summary>
//        /// Subtract from the current time old time 
//        /// </summary>
//        /// <param name="oldTime">time to subtract</param>
//        /// <returns></returns>
//        public static TimeSpan SubtractTime(DateTime oldTime)
//        {
//            return GetCurrentTime().Subtract(oldTime);
//        }

//        /// <summary>
//        /// Load saved time
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <returns></returns>
//        public static DateTime LoadTime(string saveName)
//        {
//            if (!PlayerPrefs.HasKey(saveName))
//            {
//                SaveTime(saveName);
//                return GetCurrentTime();
//            }
//            else
//            {
//                long temp = Convert.ToInt64(PlayerPrefs.GetString(saveName));
//                return DateTime.FromBinary(temp);
//            }
//        }

//        public static void ResetTime(string saveName)
//        {
//            if (PlayerPrefs.HasKey(saveName))
//            {
//                PlayerPrefs.DeleteKey(saveName);
//            }
//        }

//        /// <summary>
//        /// Save current time (sử dụng server time)
//        /// </summary>
//        /// <param name="saveName"></param>
//        public static void SaveTime(string saveName)
//        {
//            PlayerPrefs.SetString(saveName, GetCurrentTime().ToBinary().ToString());
//            PlayerPrefs.Save();
//        }

//        /// <summary>
//        /// Save the remaining time (sử dụng server time)
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <param name="remainingTime"></param>
//        internal static void SaveTime(string saveName, TimeSpan remainingTime)
//        {
//            PlayerPrefs.SetString(saveName, GetCurrentTime().Subtract(remainingTime).ToBinary().ToString());
//            PlayerPrefs.Save();
//        }

//        /// <summary>
//        /// Load current day
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <returns></returns>
//        public static int LoadDay(string saveName)
//        {
//            if (!PlayerPrefs.HasKey(saveName))
//            {
//                SaveDay(saveName, 0);
//                return 0;
//            }
//            else
//            {
//                return PlayerPrefs.GetInt(saveName);
//            }
//        }

//        public static void ResetDay(string saveName)
//        {
//            SaveDay(saveName, 0);
//        }

//        /// <summary>
//        /// Save current day
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <param name="currentDay"></param>
//        public static void SaveDay(string saveName, int currentDay)
//        {
//            PlayerPrefs.SetInt(saveName, currentDay);
//            PlayerPrefs.Save();
//        }

//        /// <summary>
//        /// Check if the current save name exists
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <returns></returns>
//        public static bool SaveExists(string saveName)
//        {
//            return PlayerPrefs.HasKey(saveName);
//        }

//        /// <summary>
//        /// Make button available using server time
//        /// </summary>
//        /// <param name="saveName"></param>
//        /// <param name="openTime"></param>
//        public static void MakeButtonAvailable(string saveName, TimeSpan openTime)
//        {
//            DateTime timeToSave = GetCurrentTime().Subtract(openTime);
//            PlayerPrefs.SetString(saveName, timeToSave.ToBinary().ToString());
//            PlayerPrefs.Save();
//        }

//        /// <summary>
//        /// Check if server time is synced
//        /// </summary>
//        /// <returns></returns>
//        public static bool IsServerTimeSynced()
//        {
//            return serverTime.HasValue;
//        }

//        /// <summary>
//        /// Get server time offset
//        /// </summary>
//        /// <returns></returns>
//        public static TimeSpan GetServerOffset()
//        {
//            if (serverTime.HasValue)
//            {
//                return serverTime.Value - lastSyncTime;
//            }
//            return TimeSpan.Zero;
//        }

//        /// <summary>
//        /// Force resync server time
//        /// </summary>
//        public static void ForceResync(System.Action onComplete = null)
//        {
//            serverTime = null;
//            SyncServerTime(onComplete);
//        }

//        /// <summary>
//        /// Auto resync nếu đã quá lâu (30 phút)
//        /// </summary>
//        public static void AutoResyncIfNeeded()
//        {
//            if (serverTime.HasValue && DateTime.Now.Subtract(lastSyncTime).TotalMinutes > 30)
//            {
//                ForceResync();
//            }
//        }
//    }
//}