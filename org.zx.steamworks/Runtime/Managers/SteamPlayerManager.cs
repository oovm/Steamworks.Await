using System;
using Steamworks;
using UnityEngine;

namespace Zx.Steamworks.Managers
{
    public class SteamPlayerManager
    {
        private static readonly Lazy<SteamPlayerManager> lazy = new(() => new SteamPlayerManager());
        public static SteamPlayerManager Instance => lazy.Value;

        public static CSteamID GetID()
        {
            return SteamUser.GetSteamID();
        }

        /// <summary>
        /// Query the current user's name
        /// </summary>
        /// <returns></returns>
        public static string GetName()
        {
            return SteamFriends.GetPersonaName();
        }

        /// <summary>
        /// Query the given user's name
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetName(ulong player)
        {
            return SteamFriends.GetFriendPersonaName((CSteamID) player);
        }

        /// <summary>
        /// Query the given user's name
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static string GetName(CSteamID player)
        {
            return SteamFriends.GetFriendPersonaName(player);
        }


        public static Texture2D? GetAvatarTextureSmall(CSteamID player)
        {
            return GetSteamTexture(SteamFriends.GetSmallFriendAvatar(player));
        }

        public static Texture2D? GetAvatarTextureMedium(CSteamID player)
        {
            return GetSteamTexture(SteamFriends.GetMediumFriendAvatar(player));
        }

        public static Texture2D? GetAvatarTextureLarge(CSteamID player)
        {
            return GetSteamTexture(SteamFriends.GetLargeFriendAvatar(player));
        }

        public static Sprite? GetAvatarSpriteLarge(CSteamID player)
        {
            var texture = GetAvatarTextureLarge(player);
            return Sprite.Create(texture, new Rect(0, 0, 184, 184), Vector2.zero);
        }


        private static Texture2D? GetSteamTexture(int imageID)
        {
            if (SteamUtils.GetImageSize(imageID, out var width, out var height))
            {
                var Image = new byte[width * height * 4];
                if (SteamUtils.GetImageRGBA(imageID, Image, (int) (width * height * 4)))
                {
                    var ret = new Texture2D((int) width, (int) height, TextureFormat.RGBA32, false, true);
                    ret.LoadRawTextureData(Image);
                    ret.Apply();
                    return ret;
                }
            }

            return null;
        }
    }
}