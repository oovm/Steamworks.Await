using System;
using System.Text;
using Steamworks;
using Zx.Steamworks.Managers;

namespace Zx.Steamworks
{
    public readonly struct SteamRoomChat
    {
        public readonly CSteamID room;
        public readonly CSteamID user;
        public string userName => SteamFriends.GetFriendPersonaName(user);
        public readonly EChatEntryType type;
        public readonly string message;

        public SteamRoomChat(LobbyChatMsg_t data)
        {
            room = (CSteamID) data.m_ulSteamIDLobby;
            var messageBuffer = new byte[4096];
            var bytesRead = SteamMatchmaking.GetLobbyChatEntry(
                room,
                (int) data.m_iChatID,
                out user,
                messageBuffer,
                messageBuffer.Length,
                out type
            );
            message = bytesRead > 0 ? Encoding.UTF8.GetString(messageBuffer, 0, bytesRead - 1) : "";
        }
    }
}