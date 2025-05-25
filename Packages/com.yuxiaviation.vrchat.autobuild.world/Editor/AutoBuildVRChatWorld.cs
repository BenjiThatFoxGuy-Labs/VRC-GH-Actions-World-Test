using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;

namespace VRChatAerospaceUniversity.VRChatAutoBuild.Worlds {
    internal static class AutoBuildVRChatWorld {
        [PublicAPI]
        private static async void BuildWorld() {
            try {
                await AutoBuildBase.InitAutoBuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            AutoBuildLogger.BeginLogGroup("Build world");
            AutoBuildLogger.Log("Building world");
            try {
                await BuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            AutoBuildLogger.Log("World build complete");
            AutoBuildLogger.EndLogGroup();
            EditorApplication.Exit(0);
        }

        [PublicAPI]
        private static async void BuildAndUploadWorld() {
            AutoBuildArguments args;
            try {
                args = await AutoBuildBase.InitAutoBuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            var worldId = args.ContentId;

            if (string.IsNullOrEmpty(worldId)) {
                AutoBuildBase.ExitWithException(new Exception("World ID is null or empty."));
                return;
            }

            AutoBuildLogger.BeginLogGroup("Build and upload world");
            AutoBuildLogger.Log("Building world");

            try {
                await BuildAsync();
            }
            catch (Exception e) {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            AutoBuildLogger.Log("World build complete");

            AutoBuildLogger.Log($"Fetching world: {worldId}");
            VRCWorld world = null;
            try {
                world = await FetchWorldAsync(worldId);
                if (world == null)
                {
                    throw new Exception("Fetched world is null.");
                }
            }
            catch (Exception e)
            {
                AutoBuildBase.ExitWithException(e);
                return;
            }
            AutoBuildLogger.Log(
                $"Fetched world: [{world.ID}] {world.Name} by {world.AuthorName}\nDescription: {world.Description}");

            AutoBuildLogger.Log(
                $"Uploading world: [{world.ID}] {world.Name} by {world.AuthorName}\nDescription: {world.Description}");
            try
            {
                await UploadAsync(world);
            } catch (Exception e)
            {
                AutoBuildBase.ExitWithException(e);
                return;
            }

            AutoBuildLogger.Log("Upload complete");
            AutoBuildLogger.EndLogGroup();

            EditorApplication.Exit(0);
        }

        private static async Task<VRCWorld> FetchWorldAsync(string worldId) {
            if (string.IsNullOrEmpty(worldId))
                throw new ArgumentException("worldId must not be null or empty", nameof(worldId));
            try {
                // If VRCApi is problematic, consider wrapping this in a using for HttpClient if possible
                return await VRCApi.GetWorld(worldId, true);
            }
            catch (Exception e)
            {
                var ex = new Exception("Failed to fetch world", e);
                AutoBuildBase.ExitWithException(ex);

                throw ex;
            }
        }
    }
}
