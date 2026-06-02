using EchoRun.Core;
using UnityEngine;
using UnityEngine.Advertisements;

namespace EchoRun.Ads
{
    public sealed class AdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
     {
        [Header("Game IDs")]
        [SerializeField] private string androidGameId = "YOUR_ANDROID_GAME_ID";
        [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID";

        [Header("Rewarded Ad Unit IDs")]
        [SerializeField] private string androidRewardedId = "Rewarded_Android";
        [SerializeField] private string iosRewardedId = "Rewarded_iOS";

        [Header("Settings")]
        [SerializeField] private bool testMode = true;

        private string _gameId;
        private string _rewardedId;
        private bool _rewardedLoaded;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

#if UNITY_IOS
            _gameId = iosGameId;
            _rewardedId = iosRewardedId;
#else
            _gameId = androidGameId;
            _rewardedId = androidRewardedId;
#endif

            Advertisement.Initialize(_gameId, testMode, this);
        }

        private void OnEnable()
        {
            GameSignals.ContinueRunRequested += ShowRewardedAd;
        }

        private void OnDisable()
        {
            GameSignals.ContinueRunRequested -= ShowRewardedAd;
        }

        private void LoadRewardedAd()
        {
            Advertisement.Load(_rewardedId, this);
        }

        private void ShowRewardedAd()
        {
            if (!_rewardedLoaded)
            {
                Debug.Log("Rewarded ad is not ready yet.");
                return;
            }

            _rewardedLoaded = false;
            Advertisement.Show(_rewardedId, this);
        }

        public void OnInitializationComplete()
        {
            Debug.Log("Unity Ads initialized.");
            LoadRewardedAd();
        }

        public void OnInitializationFailed(UnityAdsInitializationError error, string message)
        {
            Debug.LogWarning($"Unity Ads initialization failed: {error} - {message}");
        }

        public void OnUnityAdsAdLoaded(string placementId)
        {
            if (placementId == _rewardedId)
            {
                Debug.Log("Rewarded ad loaded.");
                _rewardedLoaded = true;
            }
        }

        public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
        {
            Debug.LogWarning($"Rewarded ad failed to load {placementId}: {error} - {message}");
        }

        public void OnUnityAdsShowStart(string placementId)
        {
            Debug.Log("Rewarded ad started.");
        }

        public void OnUnityAdsShowClick(string placementId)
        {
            Debug.Log("Rewarded ad clicked.");
        }

        public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
        {
            LoadRewardedAd();

            if (placementId != _rewardedId)
                return;

            if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
            {
                Debug.Log("Rewarded ad completed. Continue granted.");
                GameSignals.RaiseContinueRunGranted();
            }
            else
            {
                Debug.Log("Rewarded ad skipped or not completed.");
            }
        }

        public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
        {
            Debug.LogWarning($"Rewarded ad failed to show {placementId}: {error} - {message}");
            LoadRewardedAd();
        }
    }
}