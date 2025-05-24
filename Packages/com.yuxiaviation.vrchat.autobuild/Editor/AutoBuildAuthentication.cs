using System;
using JetBrains.Annotations;
using OtpNet;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public static class AutoBuildAuthentication
    {
        internal static void Login(string username, string password, string totpKey, string authCookie, string twoFactorAuthCookie, Action onLogin)
        {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            APIUser.Logout();
            APIUser.Login(username, password,
                model =>
                {
                    var user = APIUser.CurrentUser;

                    if (!string.IsNullOrEmpty(authCookie) && !string.IsNullOrEmpty(twoFactorAuthCookie))
                        ApiCredentials.Set(user.username, username, "vrchat", authCookie, twoFactorAuthCookie);
                    else if (!string.IsNullOrEmpty(authCookie))
                        ApiCredentials.Set(user.username, username, "vrchat", authCookie);

                    Debug.Log($"Logged in as: [{user.id}] {user.displayName}");

                    onLogin();
                },
                model => { Debug.LogError($"Failed to login: {model.Error}"); },
                model =>
                {
                    if (model.Model is not API2FA api2Fa)
                    {
                        Debug.LogError("Failed to get 2FA model");
                        EditorApplication.Exit(1);
                        return;
                    }

                    Debug.Log($"Support 2FA: {string.Join(", ", api2Fa.requiresTwoFactorAuth)}");

                    if (!api2Fa.TimeBasedOneTimePasswordSupported())
                    {
                        Debug.LogError("Only TOTP is supported");
                        EditorApplication.Exit(1);
                        return;
                    }

                    var totpKeyBytes = Base32Encoding.ToBytes(totpKey);
                    var totp = new Totp(totpKeyBytes);
                    var totpCode = totp.ComputeTotp();

                    APIUser.VerifyTwoFactorAuthCode(totpCode, API2FA.TIME_BASED_ONE_TIME_PASSWORD_AUTHENTICATION,
                        username, password,
                        _ =>
                        {
                            Debug.Log("2FA code verified");

                            Login(username, password, totpKey, authCookie, twoFactorAuthCookie, onLogin);
                        },
                        verifyModel =>
                        {
                            Debug.LogError($"Failed to verify 2FA code: {verifyModel.Error}");
                            EditorApplication.Exit(1);
                        });
                });
        }
    }
}
