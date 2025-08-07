using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using System.Data;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance;

    public Action OnRewardedAdWatched;

    BannerView _bannerView;
    private InterstitialAd _interstitialAd;
    private RewardedAd _rewardedAd;

    // Rewarded Ad Control Variables
    private bool isRewardedAdShowing = false;
    private float rewardedAdTimer = 0f;
    private const float REWARDED_AD_MIN_WATCH_TIME = 10f; // 10 giây
    private bool rewardEarned = false;
    private DateTime adStartTime;


    //
    [Header("UI")]
    public GameObject closeBannerButton; // Gán nút X trong Inspector
    public float autoShowDelay = 30f;  // Thời gian chờ để hiện lại banner (giây)
                                       //


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else 
        { 
            Instance = this;
        }
    }

    //
    private void Update()
    {
        // Đếm thời gian khi quảng cáo rewarded đang hiển thị
        if (isRewardedAdShowing)
        {
            rewardedAdTimer += Time.deltaTime;
            Debug.Log($"Ad timer: {rewardedAdTimer:F1}s / {REWARDED_AD_MIN_WATCH_TIME}s");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // This callback is called once the MobileAds SDK is initialized.
            LoadAd();
            LoadInterstitialAd();
            LoadRewardedAd();
        });
    }

    //QC banner
    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private string _adBannerUnitId = "ca-app-pub-7682460454478974/7328849115";
#elif UNITY_IPHONE
  private string _adBannerUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
    private string _adBannerUnitId = "unused";
#endif


//QC xen kẻ
    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
  private string _adInterstitialUnitId = "ca-app-pub-7682460454478974/6077462729";
#elif UNITY_IPHONE
  private string _adInterstitialUnitId = "ca-app-pub-3940256099942544/4411468910";
#else
    private string _adInterstitialUnitId = "unused";
#endif



    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
  private string _adRewardeUnitId = "ca-app-pub-7682460454478974/8178896358";
#elif UNITY_IPHONE
  private string _adRewardeUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _adRewardeUnitId = "unused";
#endif

    //--QC thưởng
    /// <summary>
    /// Loads the rewarded ad.
    /// </summary>
    public void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_adRewardeUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                _rewardedAd = ad;
                RegisterEventHandlers(_rewardedAd);

            });
    }
    //\
    public void ShowRewardedAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            // Reset timer và flags
            isRewardedAdShowing = true;
            rewardedAdTimer = 0f;
            rewardEarned = false; // Reset trạng thái nhận thưởng
            adStartTime = DateTime.Now;

            _rewardedAd.Show((Reward reward) =>
            {
                Debug.Log($"Rewarded ad rewarded the user. Type: {reward.Type}, amount: {reward.Amount}.");
                rewardEarned = true; // Đánh dấu đã nhận thưởng từ Google Ads
                Debug.Log("Reward callback được gọi - rewardEarned = true");
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready.");
        }
    }

    /* public void ShowRewardedAd()
     {
         const string rewardMsg = "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

         if (_rewardedAd != null && _rewardedAd.CanShowAd())
         {
             // Reset timer và flags
             isRewardedAdShowing = true;
             canCloseRewardedAd = false;
             rewardedAdTimer = 0f;
             rewardEarned = false;
             //

             _rewardedAd.Show((Reward reward) =>
             {
                 Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));

                 // Chỉ gọi sự kiện nếu đã xem đủ thời gian
                 if (canCloseRewardedAd)
                 {
                     OnRewardedAdWatched?.Invoke();
                 }
                 else
                 {
                     Debug.Log("Quảng cáo bị đóng quá sớm, không nhận phần thưởng");
                 }

                 //Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                 //// GỌI SỰ KIỆN khi xem xong quảng cáo
                 //OnRewardedAdWatched?.Invoke();
             });
         }
         else
         {
             Debug.LogWarning("Rewarded ad not ready.");
         }
     }*/
    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
            //
            isRewardedAdShowing = true;
            rewardedAdTimer = 0f;
            rewardEarned = false;
            adStartTime = DateTime.Now;
            Debug.Log("Ad opened - bắt đầu đếm thời gian");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            isRewardedAdShowing = false;
            double actualWatchTime = (DateTime.Now - adStartTime).TotalSeconds;

            Debug.Log($"Thời gian xem thực tế: {actualWatchTime:F1}s");
            Debug.Log($"Đã nhận reward: {rewardEarned}");

            // Chỉ cần kiểm tra có nhận được reward từ Google hay không
            // Nếu Google đã gọi reward callback thì có nghĩa là đã xem đủ thời gian theo quy định của Google
            if (rewardEarned)
            {
                OnRewardedAdWatched?.Invoke();
                Debug.Log("✓ Người chơi nhận được phần thưởng!");
            }
            else
            {
                Debug.Log("✗ Không nhận được reward từ Google Ads (có thể do tắt quá sớm)");
            }


            LoadRewardedAd();
            Debug.Log("Rewarded ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            isRewardedAdShowing = false;

            LoadRewardedAd();
            Debug.LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);
        };
    }
    ///QC thưởng---


    //----QC xen kẻ
    /// <summary>
    /// Loads the interstitial ad.
    /// </summary>
    public void LoadInterstitialAd()
    {
        // Clean up the old ad before loading a new one.
        if (_interstitialAd != null)
        {
            _interstitialAd.Destroy();
            _interstitialAd = null;
        }

        Debug.Log("Loading the interstitial ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        InterstitialAd.Load(_adInterstitialUnitId, adRequest,
            (InterstitialAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("interstitial ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                Debug.Log("Interstitial ad loaded with response : "
                          + ad.GetResponseInfo());

                _interstitialAd = ad;

                RegisterEventHandlers(_interstitialAd);
            });
    }
    /// <summary>
    /// Shows the interstitial ad.
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (_interstitialAd != null && _interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            _interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");
        }
    }

    private void RegisterEventHandlers(InterstitialAd interstitialAd)
    {
        // Raised when the ad is estimated to have earned money.
        interstitialAd.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        interstitialAd.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        interstitialAd.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            LoadInterstitialAd();
            Debug.Log("Interstitial ad full screen content closed.");
        };
        // Raised when the ad failed to open full screen content.
        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LoadInterstitialAd();
            Debug.LogError("Interstitial ad failed to open full screen content " +
                           "with error : " + error);
        };
    }
    ///QC xen kẻ---


    //---QC banner
    /// <summary>
    /// Creates a 320x50 banner view at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyAd();
        }

        // Create a 320x50 banner at top of the screen
        AdSize leaderboardSize = new AdSize(468, 60);
        _bannerView = new BannerView(_adBannerUnitId, leaderboardSize, AdPosition.Top);
        if (closeBannerButton != null)
            closeBannerButton.SetActive(true);
        //_bannerView = new BannerView(_adBannerUnitId, AdSize.Banner, AdPosition.Top);
        ListenToAdEvents();
    }
    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadAd()
    {
        // create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
        //
        if (closeBannerButton != null)
            closeBannerButton.SetActive(true);
    }

    /// <summary>
    /// listen to events the banner view may raise.
    /// </summary>
    private void ListenToAdEvents()
    {
        // Raised when an ad is loaded into the banner view.
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _bannerView.GetResponseInfo());
        };
        // Raised when an ad fails to load into the banner view.
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : "
                + error);

            LoadAd();
        };
        // Raised when the ad is estimated to have earned money.
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        _bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        _bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");

            LoadAd();
        };
    }

    /// <summary>
    /// Destroys the banner view.
    /// </summary>
    public void DestroyAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }
    public void HideBannerTemporarily()
    {
        if (_bannerView != null)
        {
            DestroyAd(); // Hủy banner thay vì chỉ Hide
            Debug.Log("Banner view destroyed by user");

            if (closeBannerButton != null)
                closeBannerButton.SetActive(false);

            StopAllCoroutines();
            StartCoroutine(ReShowBannerAfterDelay());
        }
    }

    private IEnumerator ReShowBannerAfterDelay()
    {
        yield return new WaitForSeconds(autoShowDelay);

        Debug.Log("Reloading banner after delay...");
        LoadAd(); // Tạo và load QC mới

        if (closeBannerButton != null)
            closeBannerButton.SetActive(true);
    }
}
///QC banner----
