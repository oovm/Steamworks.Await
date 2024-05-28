using Steamworks;
using UnityEngine;

//
// The SteamManager provides a base implementation of Steamworks.NET on which you can build upon.
// It handles the basics of starting up and shutting down the SteamAPI for use.
//
namespace Zx.Steamworks.Managers
{
    [DisallowMultipleComponent]
    public class SteamManager : MonoBehaviour
    {
        private static SteamManager? _instance;
        public static SteamManager Instance => _instance == null
            ? new GameObject(nameof(SteamManager)).AddComponent<SteamManager>()
            : _instance;
#if DISABLESTEAMWORKS
	    public static bool Initialized {
		    get {
			    return false;
		    }
	    }
#else
        private bool _initialized = false;
        public static bool _twice = false;
        public static bool Initialized => Instance._initialized;
#endif

#if !DISABLESTEAMWORKS

        public SteamAPIWarningMessageHook_t? m_SteamAPIWarningMessageHook;

        [AOT.MonoPInvokeCallback(typeof(SteamAPIWarningMessageHook_t))]
        protected static void SteamAPIDebugTextHook(int nSeverity, System.Text.StringBuilder pchDebugText)
        {
            Debug.LogWarning(pchDebugText);
        }

#if UNITY_2019_3_OR_NEWER
        // In case of disabled Domain Reload, reset static members before entering Play Mode.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitOnPlayMode()
        {
            _twice    = false;
            _instance = null;
        }
#endif

        protected virtual void Awake()
        {
            // Only one instance of SteamManager at a time!
            if (_instance != null)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_twice)
            {
                // This is almost always an error.
                // The most common case where this happens is when SteamManager gets destroyed because of Application.Quit(),
                // and then some Steamworks code in some other OnDestroy gets called afterwards, creating a new SteamManager.
                // You should never call Steamworks functions in OnDestroy, always prefer OnDisable if possible.
                throw new System.Exception("Tried to Initialize the SteamAPI twice in one session!");
            }

            // We want our SteamManager Instance to persist across scenes.
            DontDestroyOnLoad(gameObject);

            if (!Packsize.Test())
            {
                Debug.LogError(
                    "[Steamworks.NET] Packsize Test returned false, the wrong version of Steamworks.NET is being run in this platform.",
                    this);
            }

            if (!DllCheck.Test())
            {
                Debug.LogError(
                    "[Steamworks.NET] DllCheck Test returned false, One or more of the Steamworks binaries seems to be the wrong version.",
                    this);
            }

            try
            {
                // If Steam is not running or the game wasn't started through Steam, SteamAPI_RestartAppIfNecessary starts the
                // Steam client and also launches this game again if the User owns it. This can act as a rudimentary form of DRM.

                // Once you get a Steam AppID assigned by Valve, you need to replace AppId_t.Invalid with it and
                // remove steam_appid.txt from the game depot. eg: "(AppId_t)480" or "new AppId_t(480)".
                // See the Valve documentation for more information: https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
                if (SteamAPI.RestartAppIfNecessary(AppId_t.Invalid))
                {
                    Application.Quit();
                    return;
                }
            }
            catch (System.DllNotFoundException e)
            {
                // We catch this exception here, as it will be the first occurrence of it.
                Debug.LogError(
                    "[Steamworks.NET] Could not load [lib]steam_api.dll/so/dylib. It's likely not in the correct location. Refer to the README for more details.\n" +
                    e, this);

                Application.Quit();
                return;
            }

            // Initializes the Steamworks API.

            _initialized = SteamAPI.Init();
            if (_initialized)
            {
                _ = SteamLobbyManager.Instance;
                _ = SteamPlayerManager.Instance;
                Debug.Log($"Steam initialized, User: {SteamUser.GetSteamID()}, Name：{SteamFriends.GetPersonaName()}");
                _twice = true;
            }
            else
            {
                // If this returns false then this indicates one of the following conditions:
                // [*] The Steam client isn't running. A running Steam client is required to provide implementations of the various Steamworks interfaces.
                // [*] The Steam client couldn't determine the App ID of game. If you're running your application from the executable or debugger directly then you must have a [code-inline]steam_appid.txt[/code-inline] in your game directory next to the executable, with your app ID in it and nothing else. Steam will look for this file in the current working directory. If you are running your executable from a different directory you may need to relocate the [code-inline]steam_appid.txt[/code-inline] file.
                // [*] Your application is not running under the same OS user context as the Steam client, such as a different user or administration access level.
                // [*] Ensure that you own a license for the App ID on the currently active Steam account. Your game must show up in your Steam library.
                // [*] Your App ID is not completely set up, i.e. in Release State: Unavailable, or it's missing default packages.
                // Valve's documentation for this is located here:
                // https://partner.steamgames.com/doc/sdk/api#initialization_and_shutdown
                Debug.LogError("Steam Initialization Failed", this);
            }
        }

        // This should only ever get called on first load and after an Assembly reload, You should never Disable the Steamworks Manager yourself.
        protected virtual void OnEnable()
        {
            if (_instance == null)
            {
                _instance = this;
            }

            if (!_initialized)
            {
                return;
            }

            if (m_SteamAPIWarningMessageHook == null)
            {
                // Set up our callback to receive warning messages from Steam.
                // You must launch with "-debug_steamapi" in the launch args to receive warnings.
                m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t(SteamAPIDebugTextHook);
                SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);
            }
        }

        // OnApplicationQuit gets called too early to shutdown the SteamAPI.
        // Because the SteamManager should be persistent and never disabled or destroyed we can shutdown the SteamAPI here.
        // Thus it is not recommended to perform any Steamworks work in other OnDestroy functions as the order of execution can not be garenteed upon Shutdown. Prefer OnDisable().
        protected virtual void OnDestroy()
        {
            if (_instance != this)
            {
                return;
            }

            _instance = null;

            if (!_initialized)
            {
                return;
            }

            SteamAPI.Shutdown();
        }

        protected virtual void Update()
        {
            if (!_initialized)
            {
                return;
            }

            // Run Steam client callbacks
            SteamAPI.RunCallbacks();
        }
#endif
    }
}