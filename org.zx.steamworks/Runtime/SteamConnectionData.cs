using Steamworks;

namespace Zx.Steamworks
{
    internal class SteamConnectionData
    {
        public SteamConnectionData(CSteamID steamId)
        {
            id = steamId;
        }

        public CSteamID id;
        public HSteamNetConnection connection;
    }
}