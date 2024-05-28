using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Steamworks;
using UnityEngine;

namespace Zx.Steamworks.Managers
{
    public class SteamLobbyManager
    {
        private static readonly Lazy<SteamLobbyManager> lazy = new(() => new SteamLobbyManager());
        public static SteamLobbyManager Instance => lazy.Value;

        /// <summary>
        /// Current Room
        /// </summary>
        public CSteamID? Room;

        /// <summary>
        /// Current Room Mode
        /// </summary>
        public ELobbyType Mode = ELobbyType.k_ELobbyTypePublic;

        public event Action<SteamRoomChat>? OnReceiveChat;
        public event Action<GameLobbyJoinRequested_t>? OnPlayerJoin;
        public event Action<LobbyEnter_t>? OnEnterRoom;

        /// <summary>
        /// Return room id
        /// </summary>
        public event Action<CSteamID>? OnLeaveRoom;

        /// <summary>
        /// Return room data update event
        /// </summary>
        public event Action<LobbyDataUpdate_t>? OnRoomUpdate;

        public event Action<SteamRoomUserUpdate>? OnUserUpdate;

        public SteamLobbyManager()
        {
            Callback<LobbyChatMsg_t>.Create(e => { Instance.OnReceiveChat?.Invoke(new SteamRoomChat(e)); });
            Callback<GameLobbyJoinRequested_t>.Create(e => { Instance.OnPlayerJoin?.Invoke(e); });
            Callback<LobbyChatUpdate_t>.Create(e => { Instance.OnUserUpdate?.Invoke(new SteamRoomUserUpdate(e)); });
            Callback<LobbyEnter_t>.Create(e => { Instance.OnEnterRoom?.Invoke(e); });
            Callback<LobbyDataUpdate_t>.Create(e => { Instance.OnRoomUpdate?.Invoke(e); });
        }

        public static async IAsyncEnumerable<CSteamID> RequestRoomList()
        {
            var count = await CountRooms();
            for (uint i = 0; i < count; i++)
            {
                yield return GetRoom(i);
            }
        }

        public static async Task<uint> CountRooms()
        {
            var source = new TaskCompletionSource<LobbyMatchList_t>();
            var result = CallResult<LobbyMatchList_t>.Create((param, io_failure) =>
            {
                if (io_failure)
                {
                    source.SetException(new Exception("Failed to create lobby"));
                }
                else
                {
                    source.SetResult(param);
                }
            });
            result.Set(SteamMatchmaking.RequestLobbyList());
            return (await source.Task).m_nLobbiesMatching;
        }

        public static CSteamID GetRoom(uint index)
        {
            return SteamMatchmaking.GetLobbyByIndex((int) index);
        }


        public static string GetRoomData(CSteamID lobby, string key)
        {
            return SteamMatchmaking.GetLobbyData(lobby, key);
        }

        public string GetRoomData(string key)
        {
            return Room == null ? "" : GetRoomData(Room.Value, key);
        }

        public bool SetRoomData(string key, string value)
        {
            return Room != null && SteamMatchmaking.SetLobbyData(Room.Value, key, value);
        }

        public static bool SetRoomData(CSteamID lobby, string key, string value)
        {
            return SteamMatchmaking.SetLobbyData(lobby, key, value);
        }
        
        public async Task<LobbyCreated_t> CreateLobby(int maxMembers)
        {
            var source = new TaskCompletionSource<LobbyCreated_t>();
            var result = CallResult<LobbyCreated_t>.Create((param, io_failure) =>
            {
                if (io_failure)
                {
                    source.SetException(new Exception("Failed to create lobby"));
                }
                else
                {
                    source.SetResult(param);
                }
            });
            result.Set(SteamMatchmaking.CreateLobby(Mode, maxMembers));
            var created = await source.Task;
            Room = (CSteamID) created.m_ulSteamIDLobby;
            return created;
        }

        public bool CopyLobbyID()
        {
            if (Room == null)
            {
                return false;
            }
            else
            {
                GUIUtility.systemCopyBuffer = Room.Value.m_SteamID.ToString();
                return true;
            }
        }

        public static CSteamID? ParseLobbyID(string text)
        {
            if (ulong.TryParse(text, out var id))
            {
                return (CSteamID) id;
            }

            return null;
        }

        public bool InviteUserToRoom(CSteamID invitee)
        {
            return Room != null && SteamMatchmaking.InviteUserToLobby(Room.Value, invitee);
        }

        public async Task<LobbyEnter_t> JoinRoom(CSteamID room)
        {
            var source = new TaskCompletionSource<LobbyEnter_t>();
            var result = CallResult<LobbyEnter_t>.Create((param, io_failure) =>
            {
                if (io_failure)
                {
                    source.SetException(new Exception("Failed to create lobby"));
                }
                else
                {
                    source.SetResult(param);
                }
            });
            result.Set(SteamMatchmaking.JoinLobby(room));
            var joined = await source.Task;
            Room = (CSteamID) joined.m_ulSteamIDLobby;
            return joined;
        }

        public bool SendChat(string message)
        {
            if (Room == null) return false;
            var messageBytes = Encoding.UTF8.GetBytes(message + "\0");
            return SteamMatchmaking.SendLobbyChatMsg(Room.Value, messageBytes, messageBytes.Length);
        }


        public void LeaveRoom()
        {
            if (Room != null)
            {
                OnLeaveRoom?.Invoke(Room.Value);
                SteamMatchmaking.LeaveLobby(Room.Value);
            }
        }

        public Callback<LobbyInvite_t> LobbyInvited = Callback<LobbyInvite_t>.Create(e =>
        {
            Debug.Log($"受到 {e.m_ulSteamIDUser} 的邀请, 房间为 {e.m_ulSteamIDLobby}");
        });
    }
}