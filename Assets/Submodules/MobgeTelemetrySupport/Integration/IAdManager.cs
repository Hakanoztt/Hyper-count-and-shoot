using System;

namespace Mobge.Telemetry {

	public interface IAdManager {

		public bool IsAdsActive { get; set; }

		public void ShowInterstitial(Action<InterstitialState> onInterstitialStateChange = null, string eventName = null);
		public bool ShowRewarded(Action onRewardedAdStart, Action<RewardedResult> onRewardedAdFinish, string eventName = null);
	}

	public enum InterstitialState {
		Opened,
		Closed,
		NotEvenShown
	}

	public enum RewardedResult {
		Failed,
		Skipped,
		Finished
	}
}
