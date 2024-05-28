using Steamworks;
using Zx.Steamworks.Managers;


namespace Zx.Steamworks
{
    public readonly struct SteamRoomUserUpdate
    {
        private readonly LobbyChatUpdate_t data;
        public CSteamID Room => (CSteamID) data.m_ulSteamIDLobby;
        public EChatMemberStateChange Change => (EChatMemberStateChange) data.m_rgfChatMemberStateChange;
        public CSteamID Player => (CSteamID) data.m_ulSteamIDUserChanged;
        public string PlayerName => SteamPlayerManager.GetName(Player);

        public SteamRoomUserUpdate(LobbyChatUpdate_t data)
        {
            this.data = data;
        }
    }
}