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
