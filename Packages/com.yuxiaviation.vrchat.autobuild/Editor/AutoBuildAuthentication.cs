﻿using System;
using JetBrains.Annotations;
using OtpNet;
using UnityEditor;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public static class AutoBuildAuthentication
    {
        internal static void LoginWithCookiesOrCredentials(string username, string password, string totpKey, string authCookie, string twoFactorAuthCookie, Action onLogin)
        {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            // Try cookie-based login first if both cookies are provided
            if (!string.IsNullOrEmpty(authCookie))
            {
                if (!string.IsNullOrEmpty(twoFactorAuthCookie))
                {
                    Debug.Log("Trying login with auth and 2FA cookies...");
                    ApiCredentials.Set(username, username, "vrchat", authCookie, twoFactorAuthCookie);
                }
                else
                {
                    Debug.Log("Trying login with auth cookie...");
                    ApiCredentials.Set(username, username, "vrchat", authCookie);
                }

                // Test if login is valid by fetching current user
                APIUser.InitialFetchCurrentUser(_ =>
                {
                    Debug.Log("Cookie login successful");
                    onLogin();
                }, model =>
                {
                    Debug.LogWarning("Cookie login failed, falling back to username/password login");
                    Login(username, password, totpKey, onLogin);
                });
                return;
            }

            // Fallback to username/password/TOTP login
            Login(username, password, totpKey, onLogin);
        }

        internal static void Login(string username, string password, string totpKey, Action onLogin)
        {
            API.SetOnlineMode(true);
            ApiCredentials.Load();

            APIUser.Logout();
            APIUser.Login(username, password,
                model =>
                {
                    var user = APIUser.CurrentUser;

                    if (!model.Cookies.TryGetValue("auth", out var authCookie))
                    {
                        Debug.Log("No auth cookie found");
                    }

                    if (!model.Cookies.TryGetValue("twoFactorAuth", out var twoFactorAuthCookie))
                    {
                        Debug.Log("No 2FA cookie found");
                    }

                    if (twoFactorAuthCookie != null && authCookie != null)
                        ApiCredentials.Set(user.username, username, "vrchat", authCookie, twoFactorAuthCookie);
                    else if (authCookie != null)
                        ApiCredentials.Set(user.username, username, "vrchat", authCookie);

                    Debug.Log($"Logged in as: [{user.id}] {user.displayName}");

                    onLogin();
                },
                model => { Debug.LogError($"Failed to login: {model.Error}"); },
                model =>
                {
                    if (model.Cookies.TryGetValue("auth", out var authCookie))
                    {
                        ApiCredentials.Set(username, username, "vrchat", authCookie);
                    }

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

                            Login(username, password, totpKey, onLogin);
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
