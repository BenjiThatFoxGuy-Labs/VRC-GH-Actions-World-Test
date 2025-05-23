#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Threading.Tasks;
using Discord;
using Debug = UnityEngine.Debug;
using System.Diagnostics;

[InitializeOnLoad]
public static class UERP
{
    private const string applicationId = "846826015904497714";
    internal const string prefKey = "UERPRichPresenceEnabled";

    private static Discord.Discord discord;
    private static long startTimestamp;
    private static bool playMode = false;

    static UERP()
    {
        if (EditorPrefs.GetBool(prefKey, false))
            DelayStart();
    }

    internal static void EnablePresence()
    {
        EditorPrefs.SetBool(prefKey, true);
        DelayStart();
        Debug.Log("[UERP] Discord Rich Presence Enabled.");
    }

    internal static void DisablePresence()
    {
        EditorPrefs.SetBool(prefKey, false);
        Shutdown();
        Debug.Log("[UERP] Discord Rich Presence Disabled.");
    }

    public static async void DelayStart(int delay = 1000)
    {
        await Task.Delay(delay);
        if (!EditorPrefs.GetBool(prefKey, false))
            return;

        if (DiscordRunning())
            Init();
    }

    public static void Init()
    {
        if (discord != null)
            return;

        try
        {
            discord = new Discord.Discord(long.Parse(applicationId), (long)CreateFlags.Default);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UERP] Failed to initialize Discord: {e}");
            return;
        }

        TimeSpan timeSpan = TimeSpan.FromMilliseconds(EditorAnalyticsSessionInfo.elapsedTime);
        startTimestamp = DateTimeOffset.Now.Subtract(timeSpan).ToUnixTimeSeconds();

        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += PlayModeChanged;
        UpdateActivity();
    }

    private static void Update()
    {
        discord?.RunCallbacks();
    }

    private static void PlayModeChanged(PlayModeStateChange state)
    {
        bool isPlaying = EditorApplication.isPlaying;
        if (isPlaying != playMode)
        {
            playMode = isPlaying;
            UpdateActivity();
        }
    }

    public static void UpdateActivity()
    {
        if (discord == null)
        {
            Init();
            return;
        }

        var activity = new Activity
        {
            State = EditorSceneManager.GetActiveScene().name + " scene",
            Details = Application.productName,
            Timestamps = { Start = startTimestamp },
            Assets =
            {
                LargeImage = "unity-icon",
                LargeText = "Unity " + Application.unityVersion,
                SmallImage = playMode ? "play-mode" : "edit-mode",
                SmallText = playMode ? "Play mode" : "Edit mode",
            },
        };

        discord.GetActivityManager().UpdateActivity(activity, result =>
        {
            if (result != Result.Ok)
                Debug.LogError($"[UERP] Discord RPC error: {result}");
        });
    }

    private static bool DiscordRunning()
    {
        return Process.GetProcessesByName("Discord").Length > 0
            || Process.GetProcessesByName("DiscordPTB").Length > 0
            || Process.GetProcessesByName("DiscordCanary").Length > 0;
    }

    private static void Shutdown()
    {
        if (discord != null)
        {
            try
            {
                discord.GetActivityManager().ClearActivity(result => { });
                discord.Dispose();
            }
            catch { }
            finally
            {
                discord = null;
            }
        }

        EditorApplication.update -= Update;
        EditorApplication.playModeStateChanged -= PlayModeChanged;
    }
}

public class UERPSettingsWindow : EditorWindow
{
    private bool enabled;

    [MenuItem("Window/BenjiThatFoxGuy/Unity Editor Rich Presence")]
    public static void ShowWindow()
    {
        var window = GetWindow<UERPSettingsWindow>("Rich Presence Settings");
        window.minSize = new Vector2(300, 100);
    }

    private void OnEnable()
    {
        enabled = EditorPrefs.GetBool(UERP.prefKey, false);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Unity Editor Rich Presence", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        enabled = EditorGUILayout.Toggle("Enable Rich Presence", enabled);
        if (EditorGUI.EndChangeCheck())
        {
            if (enabled)
                UERP.EnablePresence();
            else
                UERP.DisablePresence();
        }
    }
}
#endif
