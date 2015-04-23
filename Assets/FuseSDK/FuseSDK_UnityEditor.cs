#if UNITY_EDITOR
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using FuseMisc;
using FuseSDKDotNET;

public partial class FuseSDK
{
	public static bool debugOutput = true;

	#region Initialization

	private void Awake()
	{
		GameObject go = GameObject.Find("FuseSDK");
		if(go != null && go != gameObject && go.GetComponent<FuseSDK>() != null)
		{
			UnityEngine.Object.Destroy(gameObject);
			return;
		}
		DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID
		AppID = AndroidAppID;
#else
		AppID = iOSAppID;
#endif
		_debugOutput = logging;
		_registerForPush = registerForPushNotifications;

		RegisterEvents();
	}

	void Start()
	{
		if(!string.IsNullOrEmpty(AppID) && StartAutomatically)
		{
			_StartSession(AppID);
		}
	}
	#endregion

	#region FuseSDK.NET Event Registration
	private void RegisterEvents()
	{
		//Session
		FuseDotNet.SessionStartReceived += OnSessionStartReceived;
		FuseDotNet.SessionLoginError += e => OnSessionLoginError((int)e);

		//Game configuration
		FuseDotNet.GameConfigurationReceived += OnGameConfigurationReceived;

		//Social
		FuseDotNet.AccountLoginComplete += (type, id) => OnAccountLoginComplete((int)type, id);
		FuseDotNet.AccountLoginError += (e, id) => OnAccountLoginError(id, (int)FuseError.UNDEFINED);

		//Notifications
		FuseDotNet.NotificationAction += OnNotificationAction;
		FuseDotNet.NotificationWillClose += OnNotificationWillClose;

		//Friends
		FuseDotNet.FriendAdded += (id, e) => OnFriendAdded(id, (int)e);
		FuseDotNet.FriendRemoved += (id, e) => OnFriendRemoved(id, (int)e);
		FuseDotNet.FriendAccepted += (id, e) => OnFriendAccepted(id, (int)e);
		FuseDotNet.FriendRejected += (id, e) => OnFriendRejected(id, (int)e);
		FuseDotNet.FriendsMigrated += (id, e) => OnFriendsMigrated(id, (int)e);
		FuseDotNet.FriendsListUpdated += fl => OnFriendsListUpdated(fl.Cast<Friend>().ToList());
		FuseDotNet.FriendsListError += e => OnFriendsListError((int)e);

		//IAP
		FuseDotNet.PurchaseVerification += OnPurchaseVerification;

		//Ads
		FuseDotNet.AdAvailabilityResponse += (b, e) => OnAdAvailabilityResponse(b ? 1 : 0, (int)e);
		FuseDotNet.AdWillClose += OnAdWillClose;
		FuseDotNet.RewardedAdCompletedWithObject += r => OnRewardedAdCompleted(r);
		FuseDotNet.IAPOfferAcceptedWithObject += o => OnIAPOfferAccepted(o);
		FuseDotNet.VirtualGoodsOfferAcceptedWithObject += o => OnVirtualGoodsOfferAccepted(o);

		//Misc
		FuseDotNet.TimeUpdated += OnTimeUpdated;
	}
	#endregion

	#region Session Creation

	/// <summary>Initiate communication with the Fuse system. By default, you do not need to call this function.</summary>
	/// <remarks>
	/// The StartSession function is used to bootstrap all communications with the Fuse system.
	/// When a session has been established with the Fuse system <see cref="FuseSDK.SessionStartReceived"/> is called.
	/// If a session fails to start <see cref=" FuseSDK.SessionLoginError"/> is called.
	/// You should wait for <see cref="FuseSDK.SessionStartReceived"/> before calling any other FuseSDK functions.
	/// StartSession will be called automatically if the option is checked on the FuseSDK object (on by default).
	/// When called automatically, StartSession is called from the script's <c>Start()</c> function.
	/// You should be subscribed to the FuseSDK events before this function is called.
	/// The best place to do so would be from your script's <c>Awake()</c> function.
	/// </remarks>
	public static void StartSession()
	{
		_StartSession(AppID);
	}

	private static void _StartSession(string gameId)
	{
		if(_sessionStarted)
		{
			Debug.LogWarning("FuseSDK: Duplicate StartSession call. Ignoring.");
			return;
		}

		if(string.IsNullOrEmpty(gameId))
		{
			Debug.LogError("FuseSDK: Null or empty App ID. Make sure your App ID is entered in the FuseSDK prefab");
			return;
		}

		_sessionStarted = true;
		FuseLog("StartSession(" + gameId + ") with push notifications " + (_registerForPush ? "enabled" : "disabled"));
		FuseDotNet.StartSession(gameId);
	}
	#endregion

	#region Analytics Event

	[Obsolete("Registering events is deprecated and will be removed from future releases.")]
	public static bool RegisterEvent(string name, string paramName, string paramValue, Hashtable variables)
	{
		FuseLog("RegisterEvent(" + name + "," + paramName + "," + paramValue + ", [variables])");
		return false;
	}

	[Obsolete("Registering events is deprecated and will be removed from future releases.")]
	public static bool RegisterEvent(string name, string paramName, string paramValue, string variableName, double variableValue)
	{
		FuseLog("RegisterEvent(" + name + "," + paramName + "," + paramValue + "," + variableName + "," + variableValue + ")");
		return false;
	}

	#endregion

	#region In-App Purchase Logging

	/// <summary>Records an Android in-app purchase in the Fuse system when the price and currency are known.</summary>
	/// <remarks>
	/// If your app is using the Prime31 or Unibill plugin you do not need to call these functions manually.
	/// Simply make sure the correct options are checked on the FuseSDK object.
	/// </remarks>
	/// <param name="purchaseState">The type of transaction being recorded.</param>
	/// <param name="purchaseToken">The purchase token provided by Google Play.</param>
	/// <param name="productId">The product id of the item being purchased.</param>
	/// <param name="orderId">The order id of this transaction.</param>
	/// <param name="purchaseTime">Time when purchase was made.</param>
	/// <param name="developerPayload">Developer payload provided by Google Play.</param>
	/// <param name="price">The price of the purchased item.</param>
	/// <param name="currency">The currency string identifier.</param>
	public static void RegisterAndroidInAppPurchase(IAPState purchaseState, string purchaseToken, string productId, string orderId, DateTime purchaseTime, string developerPayload, double price, string currency)
	{
		//OnPurchaseVerification(verified, transactionId, originalTransactionId);
	}

	/// <summary>Records an Android in-app purchase in the Fuse system when the price and currency are known.</summary>
	/// <remarks>
	/// If your app is using the Prime31 or Unibill plugin you do not need to call these functions manually.
	/// Simply make sure the correct options are checked on the FuseSDK object.
	/// </remarks>
	/// <param name="purchaseState">The type of transaction being recorded.</param>
	/// <param name="purchaseToken">The purchase token provided by Google Play.</param>
	/// <param name="productId">The product id of the item being purchased.</param>
	/// <param name="orderId">The order id of this transaction.</param>
	/// <param name="purchaseTime">Unix timestamp when purchase was made.</param>
	/// <param name="developerPayload">Developer payload provided by Google Play.</param>
	/// <param name="price">The price of the purchased item.</param>
	/// <param name="currency">The currency string identifier.</param>
	public static void RegisterAndroidInAppPurchase(IAPState purchaseState, string purchaseToken, string productId, string orderId, long purchaseTime, string developerPayload, double price, string currency)
	{
		//OnPurchaseVerification(verified, transactionId, originalTransactionId);
	}

	/// <summary>Register the price and currency that a user is using to make iOS in-app purchases.</summary>
	/// <remarks>
	/// After receiving the list of in-app purchases from Apple, this method can be called to record the localized item information.
	/// If your app is using the Prime31 or Unibill plugin you do not need to call this function manually.
	/// Simply make sure the correct options are checked on the FuseSDK object.
	/// </remarks>
	/// <param name="products">An array of Products that are available to purchase.</param>
	public static void RegisterIOSInAppPurchaseList(Product[] products)
	{
	}

	/// <summary>Records an iOS in-app purchase in the Fuse system when the price and currency are known.</summary>
	/// <remarks>
	/// If your app is using the Prime31 or Unibill plugin you do not need to call these functions manually.
	/// Simply make sure the correct options are checked on the FuseSDK object.
	/// </remarks>
	/// <param name="productId">The product ID of the purchased item.</param>
	/// <param name="transactionId">The transaction ID of the purchase supplied by Apple.</param>
	/// <param name="transactionReceipt">The data payload associated with the purchase, supplied by Apple.</param>
	/// <param name="transactionState">The transaction state of the purchase.</param>
	public static void RegisterIOSInAppPurchase(string productId, string transactionId, byte[] transactionReceipt, IAPState transactionState)
	{
		//OnPurchaseVerification(verified, transactionId, originalTransactionId);
	}

	/// <summary>Records an iOS in-app purchase in the Fuse system made using the Unibill plugin.</summary>
	/// <remarks>
	/// This is an internal function, it does not need to be called manually
	/// You should make sure Unibill iOS tracking is enabled on the FuseSDK object.
	/// </remarks>
	/// <param name="productID">The product ID of the purchased item.</param>
	/// <param name="receipt">The transaction receipt provided by Unibill.</param>
	public static void RegisterUnibillPurchase(string productID, byte[] receipt)
	{
		//OnPurchaseVerification(verified, transactionId, originalTransactionId);
	}
	#endregion

	#region Fuse Ads

	/// <summary>Used to check if an ad was loaded in a particular zone.</summary>
	/// <param name="zoneId">The zone id to check.</param>
	/// <returns>True if an ad is loaded and ready to be shown.</returns>
	public static bool IsAdAvailableForZoneID(string zoneId)
	{
		FuseLog("IsAdAvailableForZoneID");
		return FuseDotNet.IsAdAvailableForZoneID(zoneId);
	}

	/// <summary>Used to check whether or not a zone id has rewarded video content in it.</summary>
	/// <param name="zoneId">The zone id to check.</param>
	/// <returns>True if the zone has a reward configured.</returns>
	public static bool ZoneHasRewarded(string zoneId)
	{
		FuseLog("ZoneHasRewarded");
		return FuseDotNet.ZoneHasRewarded(zoneId);
	}

	/// <summary>Used to check whether or not a zone id has an IAP offer in it.</summary>
	/// <param name="zoneId">The zone id to check.</param>
	/// <returns>True if the zone has an IAP offer configured.</returns>
	public static bool ZoneHasIAPOffer(string zoneId)
	{
		FuseLog("ZoneHasIAPOffer");
		return FuseDotNet.ZoneHasIAPOffer(zoneId);
	}

	/// <summary>Used to check whether or not a zone id has a virtual good offer in it.</summary>
	/// <param name="zoneId">The zone id to check.</param>
	/// <returns>True if the zone has a virtual good offer configured.</returns>
	public static bool ZoneHasVirtualGoodsOffer(string zoneId)
	{
		FuseLog("ZoneHasVirtualGoodsOffer");
		return FuseDotNet.ZoneHasVirtualGoodsOffer(zoneId);
	}

	/// <summary>Get the rewarded that a user will receive for watching a video in the given zone.</summary>
	/// <remarks>If a zone is configured to show rewarded videos, this will return an object containing those details.</remarks>
	/// <param name="zoneId">The zone id to check.</param>
	/// <returns>A struct with the reward information.</returns>
	public static RewardedInfo GetRewardedInfoForZone(string zoneId)
	{
		FuseLog("GetRewardedInfoForZone");
		return FuseDotNet.GetRewardedInfoForZone(zoneId);
	}

	/// <summary>Displays an ad, rewarded video, or offer for a given zone. Zone contents can be configured via the Fuse Dashboard.</summary>
	/// <remarks>
	/// This attempts to display an ad if one is available.  If no ad is immediately ready to display
	/// this waits up to 3 seconds for an ad to load and displays immediately if loading finishes.
	/// <see cref="FuseSDK.AdWillClose"/> is always called at some point after calling this method,
	/// either when the ad is closed, or when no ads are loaded before timing out.
	/// <br/>
	/// If this attempts to display a rewarded ad, by default, an alert will be shown offering the reward
	/// to the user, as well as one after the video confirming that they earned the reward.
	/// The alerts can be disabled by using the <c>options</c> parameter with the keys
	/// <see cref="FuseMisc.Constants.RewardedAdOptionKey_ShowPreRoll"/> and <see cref="FuseMisc.Constants.RewardedAdOptionKey_ShowPostRoll"/>
	/// using the value <c>false.ToString()</c>.
	/// <br/>
	/// Similarly, the button text on the alerts can be set using the keys <see cref="FuseMisc.Constants.FuseRewardedOptionKey_PreRollYesButtonText"/>,
	/// <see cref="FuseMisc.Constants.FuseRewardedOptionKey_PreRollNoButtonText"/>, and <see cref="FuseMisc.Constants.FuseRewardedOptionKey_PostRollContinueButtonText"/>.
	/// <br/>
	/// If the user should receive a reward, <see cref="FuseSDK.RewardedAdCompletedWithObject"/> will be called.
	/// If the user has accepted an offer, <see cref="FuseSDK.IAPOfferAcceptedWithObject"/> or <see cref="FuseSDK.VirtualGoodsOfferAcceptedWithObject"/>
	/// will be called.
	/// </remarks>
	/// <example>
	/// //Simple usage. Show the next item in the zone
	/// FuseSDK.ShowAdForZoneID("myZone");
	/// <br/>
	/// //Show an ad, disabling the pre roll alert if it is a rewarded video.
	/// var ops = new Dictionary&lt;string, string&gt; {{Constants.RewardedAdOptionKey_ShowPreRoll , false.ToString()}};
	/// FuseSDK.ShowAdForZoneID("rewardedZone2", ops);
	/// <br/>
	/// //Show an ad, using custom button text if it is a rewarded video.
	/// var ops = new Dictionary&lt;string, string&gt;
	/// {
	///		{Constants.RewardedOptionKey_PreRollYesButtonText , "Yes please!"},
	///		{Constants.RewardedOptionKey_PreRollNoButtonText , "No way!"},
	///		{Constants.RewardedOptionKey_PostRollContinueButtonText , "SWEET!"},
	/// };
	/// FuseSDK.ShowAdForZoneID("rewardedZone2", ops);
	/// </example>
	/// <param name="zoneId">The zone id to show the ad from.</param>
	/// <param name="options">Optional parameter. Configuration options for showing an ad. See <see cref="FuseMisc.Constants"/></param>
	public static void ShowAdForZoneID(String zoneId, Dictionary<string, string> options = null)
	{
		FuseLog("ShowAdForZoneID");
		FuseDotNet.ShowAdForZoneID(zoneId, options);
	}

	/// <summary>Used to fetch an ad, offer, or rewarded video from the given zone and prepare it to be shown.</summary>
	/// <remarks>
	/// This method is optional and is used to help ensure that an ad is shown in a timely manner.
	/// If the zone is not ready to display an ad, this will start loading an appropriate ad but will not show
	/// it until <see cref="FuseSDK.ShowAdForZoneID"/> is called.
	/// If an ad is ready to be displayed, it does nothing.
	/// <see cref="FuseSDK.AdAvailabilityResponse"/> is always called after this function.
	/// <br/>
	/// Downloading an ad could take several seconds, depending on the size of the ad and connection speed.
	/// For best results, you should allot ample time to the ad to finish loading before attempting to show it.
	/// For example: when a game has a level completed screen and wants to show an ad when the
	/// user chooses to start the next level, you should call PreloadAdForZoneID as soon as the level is completed
	/// and ShowAdForZoneID when the next level button is pressed.
	/// </remarks>
	/// <param name="zoneId">The zone id to fetch the ad from.</param>
	public static void PreloadAdForZoneID(string zoneId)
	{
		FuseLog("PreloadAdForZoneID");
		FuseDotNet.PreloadAdForZoneID(zoneId);
	}

	/// <summary>Displays the "More Games" screen.</summary>
	/// <remarks>"More Games" can be used to showcase your own games or all games within the Fuse network.</remarks>
	public static void DisplayMoreGames()
	{
		FuseLog("DisplayMoreGames");
		FuseDotNet.DisplayMoreGames();
	}

	#endregion

	#region Notifications

	/// <summary>Display and in-game Fuse notification.</summary>
	/// <remarks>
	/// The Fuse notification system can be used to deliver textual system notifications to your users,
	/// promoting features of your application or promoting another application.
	/// <br/>
	/// In addition, the Fuse system automatically configures notifications to rate your application
	/// as well as upgrade your application when a new version is released.
	/// It is best to call this method early in the application flow of your game, preferably on your main menu.
	/// <br/>
	/// Optionally, an action can be assigned to the closing of the dialog to notify the application that an internal action should be taken.
	/// In this case, <see cref="FuseSDK.NotificationAction"/> would be called when the dialog is closing (only if the affirmative button is pressed).
	/// </remarks>
	public static void DisplayNotifications()
	{
		FuseLog("DisplayNotifications()");
		FuseDotNet.DisplayNotifications();
	}

	/// <summary>Check if a Fuse notification is available.</summary>
	/// <returns>True if a pending notification is available.</returns>
	public static bool IsNotificationAvailable()
	{
		FuseLog("IsNotificationAvailable()");
		return FuseDotNet.IsNotificationAvailable();
	}
	#endregion

	#region User Info

	/// <summary>Registers a gender for the user.</summary>
	/// <param name="gender">The user's gendar.</param>
	public static void RegisterGender(Gender gender)
	{
		FuseLog("RegisterGender()");
		FuseDotNet.RegisterGender((FuseSDKDotNET.Util.Gender)(int)gender);
	}

	/// <summary>Registers am age for the user.</summary>
	/// <param name="age">The user's age.</param>
	public static void RegisterAge(int age)
	{
		FuseLog("RegisterAge()");
		FuseDotNet.RegisterAge(age);
	}

	/// <summary>Registers a birthday for the user.</summary>
	/// <param name="year">The user's birthday year.</param>
	/// <param name="month">The user's birthday month.</param>
	/// <param name="day">The user's birthday day.</param>
	public static void RegisterBirthday(int year, int month, int day)
	{
		FuseLog("RegisterBirthday()");
		FuseDotNet.RegisterBirthday(year, month, day);
	}

	/// <summary>Registers the current level for the user.</summary>
	/// <param name="level">The user's current level.</param>
	public static void RegisterLevel(int level)
	{
		FuseLog("RegisterLevel()");
		FuseDotNet.RegisterLevel(level);
	}

	/// <summary>Register a change in the current balances of the user's in-app currencies.</summary>
	/// <param name="currencyType">A value between 1 and 4, representing up to four different in-app resources.</param>
	/// <param name="balance">The updated balance of the user.</param>
	public static void RegisterCurrency(int currencyType, int balance)
	{
		FuseLog("RegisterCurrency()");
		FuseDotNet.RegisterCurrency(currencyType, balance);
	}
	#endregion

	#region Account Login

	/// <summary>Returns the public Fuse ID for the logged in user.</summary>
	/// <remarks>
	/// After a user has registered a login for one of the supported services (i.e. Facebook, Twitter, etc),
	/// a 9-digit 'Fuse ID' is generated that uniquely identifies the user.
	/// This ID can be passed between users as a public ID for the Fuse system so that users can interact
	/// (i.e. invite as friends, etc.) without exposing confidential account information.
	/// </remarks>
	/// <returns>The 9-digit Fuse ID. This ID is strictly comprised of integers, it is NOT SAFE to cast this value to an int/long.</returns>
	public static string GetFuseId()
	{
		FuseLog("GetFuseId()");
		return FuseDotNet.GetFuseId();
	}

	/// <summary>Get the original account alias of the user used to log in to the Fuse system.</summary>
	/// <returns>The original account alias.</returns>
	public static string GetOriginalAccountAlias()
	{
		FuseLog("GetOriginalAccountAlias()");
		return FuseDotNet.GetOriginalAccountAlias();
	}

	/// <summary>Get the original account ID used to log in to the Fuse system.</summary>
	/// <remarks>This is different from the FuseID.</remarks>
	/// <returns>The original parameter used to create the user account session.</returns>
	public static string GetOriginalAccountId()
	{
		FuseLog("GetOriginalAccountId()");
		return FuseDotNet.GetOriginalAccountId();
	}

	/// <summary>Get the original account type used to log in to the Fuse system.</summary>
	/// <returns>The type of account used to create the user account session.</returns>
	public static AccountType GetOriginalAccountType()
	{
		FuseLog("GetOriginalAccountType()");
		return (AccountType)(int)FuseDotNet.GetOriginalAccountType();
	}

	/// <summary>Register a GameCenter account.</summary>
	/// <remarks>Uniquely track a user across devices by passing GameCenter login information of a user.</remarks>
	public static void GameCenterLogin()
	{
		FuseDotNet.GameCenterLogin();
	}

	/// <summary>Register a Facebook account.</summary>
	/// <remarks>Uniquely track a user across devices by passing Facebook login information of a user.</remarks>
	/// <param name="facebookId">This is the account id of the user signed in to Facebook (e.g. 122611572)</param>
	/// <param name="name">The first and last name of the user (i.e. "Jon Jovi"). Can be <c>string.Empty</c> if unknown.</param>
	/// <param name="accessToken">This is the access token generated if a user signs in to a facebook app on the device. Can be <c>string.Empty</c> if unknown.</param>
	public static void FacebookLogin(string facebookId, string name, string accessToken)
	{
		FuseDotNet.FacebookLogin(facebookId, name, accessToken);
	}

	/// <summary>Register a Twitter account.</summary>
	/// <remarks>Uniquely track a user across devices by passing Twitter login information of a user.</remarks>
	/// <param name="twitterId">This is the account id of the user signed in to Twitter.</param>
	/// <param name="alias">The alias of the user.</param>
	public static void TwitterLogin(string twitterId, string alias)
	{
		FuseDotNet.TwitterLogin(twitterId, alias);
	}

	/// <summary>Register a Fuse account.</summary>
	/// <remarks>Uniquely track a user across devices by passing Fuse login information of a user.</remarks>
	/// <param name="fuseId">This is the account id of the user signed in to Fuse</param>
	/// <param name="alias">The alias of the user.</param>
	public static void FuseLogin(string fuseId, string alias)
	{
		FuseDotNet.FuseLogin(fuseId, alias);
	}

	/// <summary>Register an Email account.</summary>
	/// <remarks>Uniquely track a user across devices by passing Email login information of a user.</remarks>
	/// <param name="email">This is the email address of the user.</param>
	/// <param name="alias">The alias of the user.</param>
	public static void EmailLogin(string email, string alias)
	{
		FuseDotNet.EmailLogin(email, alias);
	}

	/// <summary>Register a device identifier.</summary>
	/// <remarks>Uniquely track a user based on their device identifier.</remarks>
	/// <param name="alias">The alias of the user.</param>
	public static void DeviceLogin(string alias)
	{
		FuseDotNet.DeviceLogin(alias);
	}

	/// <summary>Register a Google Play account.</summary>
	/// <remarks>Uniquely track a user across devices by passing Google Play login information of a user.</remarks>
	/// <param name="alias">This is the token of the user signed in to Google Play Games</param>
	/// <param name="token">The alias of the user</param>
	public static void GooglePlayLogin(string alias, string token)
	{
		FuseDotNet.GooglePlayLogin(alias, token);
	}
	#endregion

	#region Miscellaneous

	/// <summary>Get the number of times the user has opened the game.</summary>
	/// <returns>Number of times the user has opened the game.</returns>
	public static int GamesPlayed()
	{
		FuseLog("GamesPlayed()");
		return FuseDotNet.GamesPlayed();
	}

	/// <summary>Get the version of the Fuse SDK included in the game.</summary>
	/// <returns>The version of the Fuse SDK.</returns>
	public static string LibraryVersion()
	{
		FuseLog("LibraryVersion()");
		return FuseDotNet.LibraryVersion();
	}

	/// <summary>Returns whether the application is connected to the internet.</summary>
	/// <returns>True if the app has internet access.</returns>
	public static bool Connected()
	{
		FuseLog("Connected()");
		return FuseDotNet.Connected();
	}

	/// <summary>Requests the UTC time from the server.</summary>
	/// <remarks>
	/// To help determine the psuedo-accurate real-world time (i.e. not device time), this method can be called to get the UTC time from the Fuse servers.
	/// The returned value is only psuedo-accurate in that it does not account for request time and delays.
	/// It is the time on the server when the request was received but not the time when the value returns to the device.
	/// This is generally used to prevent time exploits in games where such situations could occur (by a user changing their device time).
	/// <br/>
	/// When the time is retrieved <see cref="FuseSDK.TimeUpdated"/> will be called.
	/// </remarks>
	public static void UTCTimeFromServer()
	{
		FuseLog("TimeFromServer()");
		FuseDotNet.UTCTimeFromServer();
	}

	/// <summary>Internal function used to log messages to the console.</summary>
	/// <param name="str">Message to log.</param>
	public static void FuseLog(string str)
	{
		if(_debugOutput)
		{
			Debug.Log("FuseSDK: " + str);
		}
	}

	#endregion

	#region Data Opt In/Out

	/// <summary>Opts a user in to data being collected by the SDK.</summary>
	public static void EnableData()
	{
		FuseLog("EnableData()");
		FuseDotNet.EnableData();
	}

	/// <summary>Opts a user out of data being collected by the SDK.</summary>
	public static void DisableData()
	{
		FuseLog("DisableData()");
		FuseDotNet.DisableData();
	}

	/// <summary>Returns whether data is being collected by the SDK..</summary>
	/// <returns>True if data is being collected.</returns>
	public static bool DataEnabled()
	{
		FuseLog("DataEnabled()");
		return FuseDotNet.DataEnabled();
	}
	#endregion

	#region Friend List

	/// <summary>Get a the user's friends list.</summary>
	/// <remarks>
	/// Once a user has signed in with one of the supported account services, they will have
	/// a friends list which is retrievable from the server. This list is composed of any friends that
	/// they have invited.
	/// This call is asynchronous, <see cref="FuseSDK.FriendsListUpdated"/> will be called with the result, when the list is retrieved.
	/// <see cref="FriendsListError"/> is called if an error occurs while fetching the list.
	/// </remarks>
	public static void UpdateFriendsListFromServer()
	{
		FuseLog("UpdateFriendsListFromServer()");
		FuseDotNet.UpdateFriendsListFromServer();
	}

	/// <summary>Returns the local friends list of the logged in user.</summary>
	/// <remarks>This method merely returns the local copy of the friends list. The local version of the list can differ from the server.</remarks>
	/// <returns>The local friends list of the logged in user.</returns>
	public static List<Friend> GetFriendsList()
	{
		FuseLog("GetFriendsList()");
		return FuseDotNet.GetFriendsList().Cast<Friend>().ToList();
	}

	/// <summary>This method is used to invite (add) a friend to the user's friends list.</summary>
	/// <remarks>
	/// A friend is not added right away to the inviting user's list.
	/// Instead, there is a mechanism whereby the invited user needs to agree to the invite before both users are shown in each others list.
	/// <see cref="FuseSDK.FriendAccepted"/> or <see cref="FuseSDK.FriendRejected"/> are called depending on the outcome.
	/// </remarks>
	/// <param name="fuseId">The Fuse ID of the player to add.</param>
	public static void AddFriend(string fuseId)
	{
		FuseLog("AddFriend(" + fuseId + ")");
		FuseDotNet.AddFriend(fuseId);
	}

	/// <summary>This method is used to delete a friend from the user's friends list.</summary>
	/// <remarks>
	/// Once a friend is removed by a user, both the target and source user will not show in each other's friends list.
	/// When complete, <see cref="FuseSDK.FriendRemoved"/> will be called.
	/// </remarks>
	/// <param name="fuseId">The Fuse ID of the player to remove.</param>
	public static void RemoveFriend(string fuseId)
	{
		FuseLog("RemoveFriend(" + fuseId + ")");
		FuseDotNet.RemoveFriend(fuseId);
	}

	/// <summary>This method is used to accept a friend request</summary>
	/// <remarks>
	/// The inviting of a friend is a two-step process.
	/// The first step is to actually invite the user (source user) using <see cref="FuseSDK.AddFriend"/>,
	/// and the second step is the acceptance by the target user using this method.
	/// When complete, <see cref="FuseSDK.FriendAdded"/> is called.
	/// </remarks>
	/// <param name="fuseId">The Fuse ID of the player to accept.</param>
	public static void AcceptFriend(string fuseId)
	{
		FuseLog("AcceptFriend(" + fuseId + ")");
		FuseDotNet.AcceptFriend(fuseId);
	}

	/// <summary>This method is used to reject a friend request</summary>
	/// <remarks>
	/// The inviting of a friend is a two-step process.
	/// The first step is to actually invite the user (source user) using <see cref="FuseSDK.AddFriend"/>
	/// This method is user to reject an add invitation.
	/// </remarks>
	/// <param name="fuseId">The Fuse ID of the player to reject.</param>
	public static void RejectFriend(string fuseId)
	{
		FuseLog("RejectFriend(" + fuseId + ")");
		FuseDotNet.RejectFriend(fuseId);
	}

	/// <summary></summary>
	/// <param name="fuseId"></param>
	public static void MigrateFriends(string fuseId)
	{
		FuseLog("MigrateFriends(" + fuseId + ")");
		FuseDotNet.MigrateFriends(fuseId);
	}
	#endregion

	#region User-to-User Push Notifications

	/// <summary>Send a push notification to another user.</summary>
	/// <remarks>
	/// Use this method to send a push notification to another user.
	/// The message is sent to each device that is logged in using the specified Fuse ID.
	/// This system is dependent upon both the sender and the recipient being logged in.
	/// This system would most likely be used in conjunction with another social tool, such as the friend's list,
	/// <see cref="FuseSDK.GetFriendsList"/> where a list of users and their associated Fuse IDs would be known.
	/// Messages can be no longer than 256 characters in length.
	/// </remarks>
	/// <param name="fuseId">The fuse ID where the message should be sent.</param>
	/// <param name="message">The message to send.</param>
	public static void UserPushNotification(string fuseId, string message)
	{
		FuseLog("UserPushNotification(" + fuseId + "," + message + ")");
		FuseDotNet.UserPushNotification(fuseId, message);
	}

	/// <summary>Send a push notification to a user's entire friends list.</summary>
	/// <remarks>Similar to UserPushNotification, this method sends the same message to each user in the source user's friends list.</remarks>
	/// <param name="message">The message to send.</param>
	public static void FriendsPushNotification(string message)
	{
		FuseLog("FriendsPushNotification(" + message + ")");
		FuseDotNet.FriendsPushNotification(message);
	}
	#endregion

	#region Game Configuration Data

	/// <summary>Returns a single server configuration value.</summary>
	/// <remarks>
	/// The Fuse Dashboard provides a method to store game configuration variables that are provided to the application on start.
	/// The values are received from the server at session start and will not change during a session.
	/// Values can be 256 characters in length and support UTF-8 characters.
	/// <br/>
	/// It is recommended that a default value be present on the device in case the user has not or never connects to the Internet.
	/// </remarks>
	/// <param name="key">The key of the value to retrieve.</param>
	/// <returns>A game configuration value for the specified key.</returns>
	public static string GetGameConfigurationValue(string key)
	{
		FuseLog("GetGameConfigurationValue(" + key + ")");
		return FuseDotNet.GetGameConfigurationValue(key);
	}

	/// <summary>Returns the entire server configuration value list.</summary>
	/// <remarks>The Fuse Dashboard provides a method to store game configuration variables that are provided to the application on start.</remarks>
	/// <returns>A dictionary containing all game configuration values.</returns>
	public static Dictionary<string, string> GetGameConfiguration()
	{
		FuseLog("GetGameConfiguration()");
		return FuseDotNet.GetGameConfiguration();
	}
	#endregion
}
#endif