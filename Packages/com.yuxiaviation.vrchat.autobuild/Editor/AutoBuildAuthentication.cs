using System;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public static class AutoBuildAuthentication
    {
        internal static void Login(string authCookie, string twoFactorAuthCookie, Action onLogin)
        {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            APIUser.Logout();

            if (!string.IsNullOrEmpty(authCookie) && !string.IsNullOrEmpty(twoFactorAuthCookie))
                ApiCredentials.Set("vrchat", "vrchat", "vrchat", authCookie, twoFactorAuthCookie);
            else if (!string.IsNullOrEmpty(authCookie))
                ApiCredentials.Set("vrchat", "vrchat", "vrchat", authCookie);
            else
            {
                Debug.LogError("Authentication cookies are required.");
                EditorApplication.Exit(1);
                return;
            }

            APIUser.InitialFetchCurrentUser(
                _ =>
                {
                    var user = APIUser.CurrentUser;
                    Debug.Log($"Logged in as: [{user.id}] {user.displayName}");
                    onLogin();
                },
                model =>
                {
                    Debug.LogError($"Failed to fetch current user: {model?.Error ?? "Unknown error"}");
                    EditorApplication.Exit(1);
                });
        }
    }
}
