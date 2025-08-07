using UnityEngine;

namespace Gley.DailyRewards.Internal
{
    public class CalendarExample : MonoBehaviour
    {
        void Start()
        {
            // Đăng ký sự kiện khi người chơi nhấn vào phần thưởng
            Gley.DailyRewards.API.Calendar.AddClickListener(CalendarButtonClicked);
            Gley.DailyRewards.API.Calendar.SetValueFormatter(FormatValue);
        }

        // Hàm định dạng hiển thị giá trị phần thưởng (ví dụ: 1.000 thay vì 1000)
        private string FormatValue(int aValue)
        {
            string formattedText = aValue.ToString();
            int db = 0;
            for (int i = aValue.ToString().Length; i > 1; i--)
            {
                db++;
                if (db % 3 == 0)
                {
                    formattedText = formattedText.Insert(i - 1, ".");
                }
            }
            return formattedText;
        }

        /// <summary>
        /// Hàm được gọi khi người chơi nhấn vào một phần thưởng trong lịch
        /// </summary>
        private void CalendarButtonClicked(int dayNumber, int rewardValue, Sprite rewardSprite)
        {
            Debug.Log("Nhận thưởng ngày " + dayNumber + ": " + rewardValue + " coins");

            // Gọi CoinManager để cộng coin và cập nhật UI
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(rewardValue);
            }
        }

        /// <summary>
        /// Hiện giao diện Daily Reward
        /// </summary>
        public void ShowCalendar()
        {
            Gley.DailyRewards.API.Calendar.Show();
        }

        /// <summary>
        /// Reset lại lịch thưởng (dùng để test)
        /// </summary>
        public void ResetCalendar()
        {
            Gley.DailyRewards.API.Calendar.Reset();
        }
    }
}

//using UnityEngine;
//using UnityEngine.UI;
//using PlayFab;
//using PlayFab.ClientModels;

//namespace Gley.DailyRewards.Internal
//{
//    public class CalendarExample : MonoBehaviour
//    {
//        [Header("PlayFab Settings")]
//        public string playFabTitleId = "YOUR_PLAYFAB_TITLE_ID";

//        [Header("UI References (Optional)")]
//        public Text statusText;
//        public Button calendarButton;

//        void Start()
//        {
//            // Setup PlayFab nếu chưa login
//            SetupPlayFab();

//            // Đăng ký sự kiện khi người chơi nhấn vào phần thưởng
//            Gley.DailyRewards.API.Calendar.AddClickListener(CalendarButtonClicked);
//            Gley.DailyRewards.API.Calendar.SetValueFormatter(FormatValue);
//        }

//        void SetupPlayFab()
//        {
//            // Set title ID nếu có
//            if (!string.IsNullOrEmpty(playFabTitleId))
//            {
//                PlayFabSettings.staticSettings.TitleId = playFabTitleId;
//            }

//            // Nếu chưa login thì login anonymous
//            if (!PlayFabClientAPI.IsClientLoggedIn())
//            {
//                LoginToPlayFab();
//            }
//            else
//            {
//                UpdateStatus("PlayFab đã kết nối");
//            }
//        }

//        void LoginToPlayFab()
//        {
//            UpdateStatus("Đang kết nối PlayFab...");

//            var request = new LoginWithCustomIDRequest
//            {
//                CustomId = SystemInfo.deviceUniqueIdentifier,
//                CreateAccount = true
//            };

//            PlayFabClientAPI.LoginWithCustomID(request,
//                result => {
//                    Debug.Log("PlayFab login successful!");
//                    UpdateStatus("PlayFab đã kết nối");
//                },
//                error => {
//                    Debug.LogError($"PlayFab login failed: {error.GenerateErrorReport()}");
//                    UpdateStatus("Lỗi kết nối PlayFab: " + error.ErrorMessage);
//                });
//        }

//        void UpdateStatus(string message)
//        {
//            if (statusText != null)
//            {
//                statusText.text = message;
//            }
//            Debug.Log(message);
//        }

//        // Hàm định dạng hiển thị giá trị phần thưởng (ví dụ: 1.000 thay vì 1000)
//        private string FormatValue(int aValue)
//        {
//            string formattedText = aValue.ToString();
//            int db = 0;
//            for (int i = aValue.ToString().Length; i > 1; i--)
//            {
//                db++;
//                if (db % 3 == 0)
//                {
//                    formattedText = formattedText.Insert(i - 1, ".");
//                }
//            }
//            return formattedText;
//        }

//        /// <summary>
//        /// Hàm được gọi khi người chơi nhấn vào một phần thưởng trong lịch
//        /// </summary>
//        private void CalendarButtonClicked(int dayNumber, int rewardValue, Sprite rewardSprite)
//        {
//            Debug.Log("Nhận thưởng ngày " + dayNumber + ": " + rewardValue + " coins");

//            // Gọi CoinManager để cộng coin và cập nhật UI
//            if (CoinManager.Instance != null)
//            {
//                CoinManager.Instance.AddCoins(rewardValue);
//            }

//            UpdateStatus($"Đã nhận thưởng ngày {dayNumber}: {rewardValue} coins");
//        }

//        /// <summary>
//        /// Hiện giao diện Daily Reward
//        /// </summary>
//        public void ShowCalendar()
//        {
//            if (!PlayFabClientAPI.IsClientLoggedIn())
//            {
//                UpdateStatus("Chưa kết nối PlayFab!");
//                LoginToPlayFab();
//                return;
//            }

//            Gley.DailyRewards.API.Calendar.Show();
//        }

//        /// <summary>
//        /// Reset lại lịch thưởng (dùng để test)
//        /// </summary>
//        public void ResetCalendar()
//        {
//            Gley.DailyRewards.API.Calendar.Reset();
//            UpdateStatus("Đã reset lịch thưởng!");
//        }

//        /// <summary>
//        /// Force resync server time (dùng để test)
//        /// </summary>
//        public void ResyncServerTime()
//        {
//            if (CalendarManager.Instance != null)
//            {
//                CalendarManager.Instance.ResyncServerTime();
//                UpdateStatus("Đang đồng bộ thời gian server...");
//            }
//        }

//        /// <summary>
//        /// Hiển thị thông tin debug
//        /// </summary>
//        public void ShowDebugInfo()
//        {
//            if (CalendarManager.Instance != null)
//            {
//                bool serverSynced = CalendarManager.Instance.IsServerTimeSynced();
//                int currentDay = CalendarManager.Instance.GetCurrentDay();
//                string remainingTime = CalendarManager.Instance.GetRemainingTime();
//                bool rewardAvailable = CalendarManager.Instance.TimeExpired();
//                var serverOffset = CalendarManager.Instance.GetServerOffset();

//                string debugInfo = $"Server Synced: {serverSynced}\n" +
//                                 $"Current Day: {currentDay}\n" +
//                                 $"Remaining Time: {remainingTime}\n" +
//                                 $"Reward Available: {rewardAvailable}\n" +
//                                 $"Server Offset: {serverOffset.TotalSeconds}s";

//                Debug.Log(debugInfo);
//                UpdateStatus("Debug info logged to console");
//            }
//        }

//        // Test controls bằng keyboard
//        void Update()
//        {
//            if (Input.GetKeyDown(KeyCode.Space))
//            {
//                ShowCalendar();
//            }

//            if (Input.GetKeyDown(KeyCode.R))
//            {
//                ResetCalendar();
//            }

//            if (Input.GetKeyDown(KeyCode.T))
//            {
//                ResyncServerTime();
//            }

//            if (Input.GetKeyDown(KeyCode.I))
//            {
//                ShowDebugInfo();
//            }
//        }
//    }
//}
