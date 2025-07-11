using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;

namespace VRChatAerospaceUniversity.VRChatAutoBuild
{
    public abstract class AutoBuildBase
    {
        #region Arguments

        [PublicAPI]
        public static AutoBuildArguments GetArguments()
        {
            var commandLineArgs = Environment.GetCommandLineArgs().ToList();

            var contentId = GetArgument(commandLineArgs, "--vrchat-auto-build-content-id", "VRC_AUTO_BUILD_CONTENT_ID");
            var scenePath = GetArgument(commandLineArgs, "--vrchat-auto-build-scene-path", "VRC_AUTO_BUILD_SCENE_PATH");
            var username = GetArgument(commandLineArgs, "--vrchat-auto-build-username", "VRC_AUTO_BUILD_USERNAME");
            var password = GetArgument(commandLineArgs, "--vrchat-auto-build-password", "VRC_AUTO_BUILD_PASSWORD");
            var totpKey = GetArgument(commandLineArgs, "--vrchat-auto-build-totp-key", "VRC_AUTO_BUILD_TOTP_KEY");
            var authCookie = GetArgument(commandLineArgs, "--vrchat-auto-build-auth-cookie", "VRC_AUTO_BUILD_AUTH_COOKIE");
            var twoFactorAuthCookie = GetArgument(commandLineArgs, "--vrchat-auto-build-two-factor-auth-cookie", "VRC_AUTO_BUILD_TWO_FACTOR_AUTH_COOKIE");

            if (scenePath == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.ScenePath), "Scene path is required");
            }

            if (username == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.Username), "Username is required");
            }

            if (password == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.Password), "Password is required");
            }

            if (totpKey == null)
            {
                throw new ArgumentNullException(nameof(AutoBuildArguments.TotpKey), "TOTP key is required");
            }

            return new AutoBuildArguments
            {
                ContentId = contentId,
                ScenePath = scenePath,
                Username = username,
                Password = password,
                TotpKey = totpKey,
                AuthCookie = authCookie,
                TwoFactorAuthCookie = twoFactorAuthCookie
            };
        }

        [CanBeNull]
        private static string GetArgument(List<string> commandLineArgs, string commandLineArg,
            string environmentVariable)
        {
            var index = commandLineArgs.FindLastIndex(arg => arg == commandLineArg);

            var arg = index != -1 && commandLineArgs.Count > index + 1
                ? commandLineArgs[index + 1]
                : Environment.GetEnvironmentVariable(environmentVariable);

            return arg;
        }

        #endregion

        [PublicAPI]
        public static async Task<AutoBuildArguments> InitAutoBuildAsync()
        {
            AutoBuildLogger.BeginLogGroup("Auto build initialization");

            var args = GetArguments();

            AutoBuildLogger.Log("Application Cache Path: " + Application.temporaryCachePath);

            AutoBuildLogger.Log("Opening scene");
            EditorSceneManager.OpenScene(args.ScenePath, OpenSceneMode.Single);
            AutoBuildLogger.Log("Scene opened");

            try
            {
                await InitSDKOnlineModeAsync();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Online Mode", e);
            }

            try
            {
                await InitSDKAccount();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Account", e);
            }

            try
            {
                await InitSDKBuildersAsync();
            }
            catch (Exception e)
            {
                throw new Exception("Failed to initialize SDK Builders", e);
            }

            AutoBuildLogger.EndLogGroup();

            return args;
        }

        [PublicAPI]
        public static void ExitWithException(Exception e)
        {
            AutoBuildLogger.LogError("Failed to preform auto build");
            AutoBuildLogger.LogException(e);

            Exit(1);
        }

        [PublicAPI]
        public static async void Exit(int exitCode = 0)
        {
            AutoBuildLogger.EndAllLogGroups();

            if (_logOutWhenExit)
            {
                AutoBuildLogger.Log("Logging out");
                APIUser.Logout();

                // Wait for http request to finish
                await Task.Delay(5000);

                ApiCredentials.Clear();
                AutoBuildLogger.Log("Logged out");
            }

            AutoBuildLogger.Log("Exiting");
            EditorApplication.Exit(exitCode);
        }

        private static async Task InitSDKBuildersAsync()
        {
            var vrcSdkControlPanel = EditorWindow.GetWindow<VRCSdkControlPanel>();
            var showBuildersMethod =
                typeof(VRCSdkControlPanel).GetMethod("ShowBuilders", BindingFlags.Instance | BindingFlags.NonPublic);

            if (showBuildersMethod == null)
            {
                throw new Exception("Failed to get ShowBuilders method");
            }

            vrcSdkControlPanel.Show(true);
            showBuildersMethod.Invoke(vrcSdkControlPanel, null);

            // Because VRChat SDK initialize builder in ui schedule, we have to wait
            await Task.Delay(1000);
        }

        private static async Task InitSDKOnlineModeAsync()
        {
            AutoBuildLogger.BeginLogGroup("SDK Online Mode Initialization");
            AutoBuildLogger.Log("Initializing SDK Online Mode");

            API.SetOnlineMode(true);
            ApiCredentials.Load();

            var tcs = new TaskCompletionSource<bool>();

            ConfigManager.RemoteConfig.Init(() => { tcs.SetResult(true); }, () =>
            {
                AutoBuildLogger.LogError("Failed to initialize SDK: Failed to init remote config");
                tcs.SetException(new Exception("Failed to init remote config"));
            });

            await tcs.Task;

            AutoBuildLogger.Log("Initialized SDK Online Mode");
            AutoBuildLogger.EndLogGroup();
        }

        private static bool _logOutWhenExit;

        private static async Task InitSDKAccount()
        {
            AutoBuildLogger.BeginLogGroup("SDK Account Initialization");
            AutoBuildLogger.Log("Initializing SDK Account");

            var tcs = new TaskCompletionSource<bool>();

            if (ApiCredentials.IsLoaded())
            {
                APIUser.InitialFetchCurrentUser(_ =>
                {
                    AutoBuildLogger.Log($"Logged in as [{APIUser.CurrentUser.id}] {APIUser.CurrentUser.displayName}");

                    tcs.SetResult(true);
                }, model =>
                {
                    if (model == null)
                    {
                        AutoBuildLogger.LogError(
                            "Failed to initialize SDK Account: Failed to fetch current user: Unknown error (Model is null)");
                        tcs.SetException(new Exception("Failed to fetch current user: Unknown error (Model is null)"));
                        return;
                    }

                    AutoBuildLogger.LogError("Failed to initialize SDK Account: Failed to fetch current user: " + model.Error);
                    tcs.SetException(new Exception("Failed to fetch current user: " + model.Error));
                });
            }
            else
            {
                _logOutWhenExit = true;
                var args = GetArguments();
                AutoBuildAuthentication.LoginWithCookiesOrCredentials(
                    args.Username,
                    args.Password,
                    args.TotpKey,
                    args.AuthCookie,
                    args.TwoFactorAuthCookie,
                    () => { tcs.SetResult(true); });
            }

            await tcs.Task;

            AutoBuildLogger.Log("Initialized SDK Account");
            AutoBuildLogger.EndLogGroup();
        }
    }
}
