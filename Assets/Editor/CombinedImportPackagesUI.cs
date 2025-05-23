using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;

// Self-contained definition of ImportPackageInfo
public class ImportPackageInfo
{
    public string menuPath;         // Menu location or package label.
    public string[] sources;        // Array of URLs or local file paths.
    public string[] tempFileNames;  // For remote downloads (orders must match sources).
    public string thumbnailPath;    // Optional thumbnail path or URL.
    public bool nsfw = false;       // New field for NSFW status.
    // New fields:
    public string[] tags;           // Optional tags (separated by "|").
    public string description;      // Optional package description.
}

public class CombinedImportPackagesUI : EditorWindow
{
    private const string requiredVersion = "2.2";
    private bool versionMismatch;
    private string csvVersion;
    private string searchQuery = "";
    private List<ImportPackageInfo> packages = new List<ImportPackageInfo>();
    private Dictionary<string, Texture> thumbnailCache = new Dictionary<string, Texture>();
    private float thumbnailSize = 64f; // Will be loaded from preferences.
    private Vector2 scrollPos = Vector2.zero;
    private bool silent = false; // Default off.
    // Replace hideNSFW with displayNSFW (default true: display NSFW content).
    private bool displayNSFW = false; // Default off.
    // URL to the remote CSV asset index
    private string csvUrl = "https://docs.google.com/spreadsheets/d/e/2PACX-1vQ9DFygPufn-GrgX3E1INioJqraWDtnVWizD3-0swDmI19adULccvKDJ6A2o6wbhdJPXTTuoDYcgpiR/pub?gid=603996451&single=true&output=csv";
    // Local CSV override path relative to the Assets folder.
    private string localCsvRelativePath = "Assets/Editor/assets.csv";
    // Static default icon for comparison.
    private static Texture defaultIcon; // Removed immediate initialization.
    private bool versionIsOlder;
    private int selectedTagIndex = 0; // New field for tag dropdown selection
    private string manualUrl = ""; // New field for manual URL input

    // Helper method to scale a texture.
    private static Texture2D ScaleTexture(Texture source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    // Helper method to scale a texture and convert it to monochrome.
    private static Texture2D ScaleAndMakeMonoTexture(Texture source, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        result.Apply();
        // Convert to grayscale.
        Color[] pixels = result.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            float gray = pixels[i].grayscale;
            pixels[i] = new Color(gray, gray, gray, pixels[i].a);
        }
        result.SetPixels(pixels);
        result.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
    
    [MenuItem("Window/BenjiThatFoxGuy/Asset Library")]
    public static void ShowWindow()
    {
        CombinedImportPackagesUI window = GetWindow<CombinedImportPackagesUI>("Asset Library");
        // Use the internal box/prefab icon scaled down to 16x16 and convert it to mono.
        Texture monoIcon = ScaleAndMakeMonoTexture(EditorGUIUtility.IconContent("Prefab Icon").image, 16, 16);
        window.titleContent = new GUIContent("Asset Library", monoIcon);
    }

    private void OnEnable()
    {
        // Initialize defaultIcon lazily.
        if (defaultIcon == null)
        {
            defaultIcon = EditorGUIUtility.IconContent("UnityLogo").image;
        }
        silent = EditorPrefs.GetBool("CombinedImportPackagesUI_Silent", false);
        displayNSFW = EditorPrefs.GetBool("CombinedImportPackagesUI_DisplayNSFW", false);
        searchQuery = EditorPrefs.GetString("CombinedImportPackagesUI_SearchQuery", "");
        thumbnailSize = EditorPrefs.GetFloat("CombinedImportPackagesUI_ThumbnailSize", 64f);
        
        // Always check for the local CSV override first.
        string localCsvPath = Path.Combine(Application.dataPath, localCsvRelativePath.Substring("Editor/".Length));
        if (File.Exists(localCsvPath))
        {
            string csvText = File.ReadAllText(localCsvPath);
            ParseCSV(csvText);
            Repaint();
        }
        else
        {
            // If no local file exists, load the remote CSV.
            LoadAssetIndexFromCSV(csvUrl);
        }
    }

    // Force UI update when the panel gains focus.
    private void OnFocus()
    {
        Repaint();
    }

    private async void LoadAssetIndexFromCSV(string url)
    {
        int progressId = UnityEditor.Progress.Start("Load CSV", $"Downloading CSV from {url}");
        Debug.Log("Attempting to download CSV from URL: " + url);
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            var asyncOp = req.SendWebRequest();
            while (!asyncOp.isDone)
            {
                UnityEditor.Progress.Report(progressId, req.downloadProgress, "Downloading...");
                await Task.Delay(50);
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                UnityEditor.Progress.Finish(progressId, UnityEditor.Progress.Status.Succeeded);
                int updateCheckProgressId = UnityEditor.Progress.Start("Update Check", "Checking version...");
                string csvText = req.downloadHandler.text;
                ParseCSV(csvText);
                UnityEditor.Progress.Finish(updateCheckProgressId, UnityEditor.Progress.Status.Succeeded);
                Repaint();
            }
            else
            {
                UnityEditor.Progress.SetDescription(progressId, req.error);
                UnityEditor.Progress.Finish(progressId, UnityEditor.Progress.Status.Failed);
                Debug.LogError("Failed to download CSV from " + url + ": " + req.error);
            }
        }
    }

    private void ParseCSV(string csvText)
    {
        packages.Clear();
        StringReader reader = new StringReader(csvText);
        string headerLine = reader.ReadLine();
        if (headerLine == null) return;
        string[] headerParts = headerLine.Split(',');
        csvVersion = headerParts[headerParts.Length - 1].Trim();
        
        float localVer = float.Parse(requiredVersion, CultureInfo.InvariantCulture);
        float remoteVer;
        if (!float.TryParse(csvVersion, NumberStyles.Float, CultureInfo.InvariantCulture, out remoteVer))
        {
            remoteVer = 0f;
        }
        versionIsOlder = (remoteVer < localVer);

        // Read the last column in the header line as the version.
        if (csvVersion != requiredVersion)
        {
            versionMismatch = true;
        }
        // Split CSV into lines - ignore the first header line.
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            // Expected order: menuPath,sources,tempFileNames,thumbnailPath,nsfw (optional),tags (optional),description (optional)
            string[] parts = line.Split(',');
            if (parts.Length < 3)
                continue;
            ImportPackageInfo info = new ImportPackageInfo();
            info.menuPath = parts[0].Trim(' ', '"');
            // Sources and tempFileNames: if multiple, separated by "|"
            info.sources = parts[1].Trim(' ', '"').Split('|');
            info.tempFileNames = parts[2].Trim(' ', '"').Split('|');
            info.thumbnailPath = (parts.Length >= 4) ? parts[3].Trim(' ', '"') : "";
            if (parts.Length >= 5)
            {
                string nsfwText = parts[4].Trim(' ', '"').ToLower();
                bool parsed;
                if (!bool.TryParse(nsfwText, out parsed))
                {
                    parsed = nsfwText == "1" || nsfwText == "yes";
                }
                info.nsfw = parsed;
            }
            if (parts.Length >= 6)
            {
                // Expect tags separated by "|"
                info.tags = parts[5].Trim(' ', '"').Split('|');
            }
            if (parts.Length >= 7)
            {
                info.description = parts[6].Trim(' ', '"');
            }
            packages.Add(info);
        }
    }

    private void OnGUI()
    {
        // Top bar: Refresh button, search field, and now a tag dropdown.
        EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(100)))
            {
                string localCsvPath = Path.Combine(Application.dataPath, localCsvRelativePath.Substring("Editor/".Length));
                if (File.Exists(localCsvPath))
                {
                    string csvText = File.ReadAllText(localCsvPath);
                    ParseCSV(csvText);
                    Repaint();
                    ShowWindow(); // Trigger a UI reload
                }
                else
                {
                    LoadAssetIndexFromCSV(csvUrl);
                }
            }
            GUIStyle searchStyle = GUI.skin.FindStyle("SearchTextField");
            searchQuery = EditorGUILayout.TextField(searchQuery, searchStyle, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();

        // Build the distinct tag list from packages.
        List<string> distinctTags = new List<string> { "All" };
        foreach (var pkg in packages)
        {
            if (pkg.tags != null)
            {
                foreach (var tag in pkg.tags)
                {
                    if (!distinctTags.Contains(tag))
                        distinctTags.Add(tag);
                }
            }
        }
        // Remove "All", sort, then put "All" back at the top.
        distinctTags.Remove("All");
        distinctTags.Sort();
        distinctTags.Insert(0, "All");

        selectedTagIndex = EditorGUILayout.Popup("Filter by Tag", selectedTagIndex, distinctTags.ToArray());
        
        // Slider for thumbnail size.
        thumbnailSize = EditorGUILayout.Slider("Thumbnail Size", thumbnailSize, 32f, 512f);
        EditorGUILayout.Space();

        // Wrap package listing in a scroll view.
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Filter packages: now also filter by tag if one is selected.
            List<ImportPackageInfo> filteredPackages = packages.FindAll(pkg =>
                (string.IsNullOrWhiteSpace(searchQuery)
                  || pkg.menuPath.ToLower().Contains(searchQuery.ToLower())
                  || (pkg.tags != null && System.Array.Exists(pkg.tags, t => t.ToLower().Contains(searchQuery.ToLower())))
                  || (!string.IsNullOrEmpty(pkg.description) && pkg.description.ToLower().Contains(searchQuery.ToLower())))
                && (displayNSFW || !pkg.nsfw)
                && (selectedTagIndex == 0 
                    || (pkg.tags != null && System.Array.Exists(pkg.tags, t => t.Equals(distinctTags[selectedTagIndex], StringComparison.OrdinalIgnoreCase))))
                && (versionIsOlder || pkg.menuPath.ToLower() != "update")
            );
            // If a search term is provided, sort filtered packages alphabetically.
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                filteredPackages.Sort((a, b) => a.menuPath.CompareTo(b.menuPath));
            }
            if(filteredPackages.Count == 0)
            {
                EditorGUILayout.LabelField("No packages found.");
            }
            else
            {
                if (thumbnailSize > 128)
                {
                    float availableWidth = position.width - 20;
                    int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (thumbnailSize + 10)));
                    int currentColumn = 0;
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    foreach (var pkg in filteredPackages)
                    {
                        DrawPackageGridCell(pkg);
                        currentColumn++;
                        if (currentColumn >= columns)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currentColumn = 0;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.BeginVertical();
                    foreach (var pkg in filteredPackages)
                    {
                        EditorGUILayout.BeginVertical("box");
                        DrawPackageListItem(pkg);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.EndVertical();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView(); // End scroll view

        // Bottom controls: silent and Display NSFW toggles side-by-side.
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
            silent = EditorGUILayout.ToggleLeft("Silent (Overwrite!)", silent);
            displayNSFW = EditorGUILayout.ToggleLeft("Display NSFW", displayNSFW);
        EditorGUILayout.EndHorizontal();
        
        // Save preferences.
        EditorPrefs.SetBool("CombinedImportPackagesUI_Silent", silent);
        EditorPrefs.SetBool("CombinedImportPackagesUI_DisplayNSFW", displayNSFW);
        EditorPrefs.SetString("CombinedImportPackagesUI_SearchQuery", searchQuery);
        EditorPrefs.SetFloat("CombinedImportPackagesUI_ThumbnailSize", thumbnailSize);

        // Manual Import UI
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        manualUrl = EditorGUILayout.TextField("Manual Import URL", manualUrl);
        if (GUILayout.Button("Import from URL"))
        {
            ImportManualURL(manualUrl);
        }
        EditorGUILayout.EndHorizontal();
        
        if (versionIsOlder)
        {
            EditorGUILayout.HelpBox(
                $"Your local version is {requiredVersion}, but Backend version is {csvVersion}. You can update from the listing or contact the dev (closed beta).",
                MessageType.Warning
            );
        }
        else
        {
            GUIStyle greenLabelStyle = new GUIStyle(EditorStyles.boldLabel);
            greenLabelStyle.normal.textColor = Color.green;
            EditorGUILayout.LabelField($"✓ Local version {csvVersion} is up to date or newer.", greenLabelStyle);
        }
    }

    private void DrawPackageListItem(ImportPackageInfo pkg)
    {
        Texture thumb = GetThumbnail(pkg.thumbnailPath);
        GUIStyle thumbStyle = new GUIStyle(GUI.skin.button)
        { 
            fixedWidth = thumbnailSize, 
            fixedHeight = thumbnailSize,
            normal = { background = thumb as Texture2D },
            alignment = TextAnchor.MiddleCenter 
        };
        // Instead of a clickable button that summons a thumbnail viewer, simply render the thumbnail.
        GUILayout.Label("", thumbStyle);
        EditorGUILayout.LabelField(pkg.menuPath, EditorStyles.boldLabel);
        // Display tags (if any).
        if (pkg.tags != null && pkg.tags.Length > 0)
        {
            EditorGUILayout.LabelField("Tags: " + string.Join(", ", pkg.tags));
        }
        // Display description (if any).
        if (!string.IsNullOrEmpty(pkg.description))
        {
            EditorGUILayout.LabelField("Description: " + pkg.description, EditorStyles.wordWrappedLabel);
        }
        GUIStyle importButtonStyle = new GUIStyle(GUI.skin.button);
        importButtonStyle.richText = true;
        string importButtonText = silent 
            ? "Import <color=red>(Overwrite!)</color>" 
            : "Import";
        if (GUILayout.Button(importButtonText, importButtonStyle))
        {
            ImportPackage(pkg);
        }
    }
    
    private void DrawPackageGridCell(ImportPackageInfo pkg)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(thumbnailSize + 20));
        Texture thumb = GetThumbnail(pkg.thumbnailPath);
        GUIStyle thumbStyle = new GUIStyle(GUI.skin.button)
        { 
            fixedWidth = thumbnailSize, 
            fixedHeight = thumbnailSize,
            normal = { background = thumb as Texture2D },
            alignment = TextAnchor.MiddleCenter 
        };
        // Render the thumbnail as a label only.
        GUILayout.Label("", thumbStyle);
        EditorGUILayout.LabelField(pkg.menuPath, EditorStyles.boldLabel);
        // Display tags (if any).
        if (pkg.tags != null && pkg.tags.Length > 0)
        {
            EditorGUILayout.LabelField("Tags: " + string.Join(", ", pkg.tags));
        }
        // Display description (if any).
        if (!string.IsNullOrEmpty(pkg.description))
        {
            EditorGUILayout.LabelField("Description: " + pkg.description, EditorStyles.wordWrappedLabel);
        }
        GUIStyle importButtonStyle = new GUIStyle(GUI.skin.button);
        importButtonStyle.richText = true;
        string importButtonText = silent 
            ? "Import <color=red>(Overwrite!)</color>" 
            : "Import";
        if (GUILayout.Button(importButtonText, importButtonStyle, GUILayout.Width(thumbnailSize)))
        {
            ImportPackage(pkg);
        }
        EditorGUILayout.EndVertical();
    }
    
    private Texture GetThumbnail(string thumbPath)
    {
        Texture thumb = null;
        if (!string.IsNullOrEmpty(thumbPath))
        {
            if(thumbPath.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                if(thumbnailCache.ContainsKey(thumbPath))
                {
                    thumb = thumbnailCache[thumbPath];
                }
                else
                {
                    DownloadThumbnail(thumbPath);
                }
            }
            else
            {
                thumb = AssetDatabase.LoadAssetAtPath<Texture2D>(thumbPath);
            }
        }
        if(thumb == null)
        {
            thumb = defaultIcon;
        }
        return thumb;
    }
    
    private async void DownloadThumbnail(string url)
    {
        int progressId = UnityEditor.Progress.Start("Thumbnail DL", $"Downloading thumb from {url}");
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            var asyncOp = req.SendWebRequest();
            while (!asyncOp.isDone)
            {
                UnityEditor.Progress.Report(progressId, req.downloadProgress, "Downloading...");
                await Task.Delay(50);
            }
            if (req.result == UnityWebRequest.Result.Success)
            {
                UnityEditor.Progress.Finish(progressId, UnityEditor.Progress.Status.Succeeded);
                Texture tex = DownloadHandlerTexture.GetContent(req);
                if (!thumbnailCache.ContainsKey(url))
                    thumbnailCache.Add(url, tex);
                Repaint();
            }
            else
            {
                UnityEditor.Progress.SetDescription(progressId, req.error);
                UnityEditor.Progress.Finish(progressId, UnityEditor.Progress.Status.Failed);
                Debug.LogError("Failed to download thumbnail: " + req.error);
            }
        }
    }

    private async void ImportPackage(ImportPackageInfo pkg)
    {
        int importProgressId = UnityEditor.Progress.Start("Importing Assets", $"Importing {pkg.menuPath} packages");
        // Iterate over all sources in the package entry
        for (int i = 0; i < pkg.sources.Length; i++)
        {
            float progress = i / (float)pkg.sources.Length;
            UnityEditor.Progress.Report(importProgressId, progress, $"Importing source {i + 1}/{pkg.sources.Length}");
            string source = pkg.sources[i];
            if (source.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                string tempPath = Path.Combine(Path.GetTempPath(), pkg.tempFileNames[i]);
                using (UnityWebRequest www = UnityWebRequest.Get(source))
                {
                    var asyncOp = www.SendWebRequest();
                    while (!asyncOp.isDone)
                        await Task.Delay(100);
                    if (www.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("Failed to download package: " + www.error);
                        continue;
                    }
                    File.WriteAllBytes(tempPath, www.downloadHandler.data);
                }
                // Use interactive import if silent is false; otherwise, silent.
                AssetDatabase.ImportPackage(tempPath, !silent);
                Debug.Log("Imported remote package from: " + source);
            }
            else
            {
                AssetDatabase.ImportPackage(source, !silent);
                Debug.Log("Imported local package from: " + source);
            }
        }
        UnityEditor.Progress.Finish(importProgressId, UnityEditor.Progress.Status.Succeeded);
    }

    private async void ImportManualURL(string url) // New method
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogError("Manual Import URL is empty.");
            return;
        }
        int importProgressId = UnityEditor.Progress.Start("Importing Assets", $"Importing from {url}");
        string[] urls = url.Contains("|") ? url.Split('|') : new string[] { url };
        foreach (string singleUrl in urls)
        {
            if (string.IsNullOrWhiteSpace(singleUrl))
                continue;
            string tempPath = Path.Combine(Path.GetTempPath(), "ManualImport.unitypackage");
            using (UnityWebRequest www = UnityWebRequest.Get(singleUrl))
            {
                var asyncOp = www.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    UnityEditor.Progress.Report(importProgressId, www.downloadProgress, "Downloading...");
                    await Task.Delay(50);
                }
                if (www.result != UnityWebRequest.Result.Success)
                {
                    UnityEditor.Progress.SetDescription(importProgressId, www.error);
                    UnityEditor.Progress.Finish(importProgressId, UnityEditor.Progress.Status.Failed);
                    Debug.LogError("Failed to download package: " + www.error);
                    return;
                }
                File.WriteAllBytes(tempPath, www.downloadHandler.data);
                AssetDatabase.ImportPackage(tempPath, !silent);
                Debug.Log("Imported remote package from: " + singleUrl);
            }
        }
        UnityEditor.Progress.Finish(importProgressId, UnityEditor.Progress.Status.Succeeded);
    }
}