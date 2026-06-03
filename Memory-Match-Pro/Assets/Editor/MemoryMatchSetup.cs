#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using MemoryMatchPro;

/// <summary>
/// Editor utility: tạo toàn bộ scenes, prefabs và ScriptableObjects cho Memory Match Pro.
/// Chạy từ menu: Tools > Memory Match Pro > Setup All
/// </summary>
public static class MemoryMatchSetup
{
    // ==================== Paths ====================
    private const string SCENE_PATH_MAIN   = "Assets/Scenes/MainMenuScene.unity";
    private const string SCENE_PATH_MODE   = "Assets/Scenes/ModeSelectScene.unity";
    private const string SCENE_PATH_SELECT = "Assets/Scenes/LevelSelectScene.unity";
    private const string SCENE_PATH_GAME   = "Assets/Scenes/GameScene.unity";
    private const string PREFAB_PATH_CARD  = "Assets/Prefabs/CardPrefab.prefab";
    private const string PREFAB_PATH_BTN   = "Assets/Prefabs/LevelButtonPrefab.prefab";
    private const string SO_PATH_LEVELS    = "Assets/ScriptableObjects/Levels/";
    private const string SO_PATH_MODES     = "Assets/ScriptableObjects/Modes/";

    // Reference resolution cho Canvas
    private const float REF_WIDTH  = 1080f;
    private const float REF_HEIGHT = 1920f;

    // Color palette
    private static Color ColorBg      = new Color(0.08f, 0.10f, 0.18f);  // dark navy
    private static Color ColorPrimary = new Color(0.25f, 0.60f, 1.00f);  // blue
    private static Color ColorAccent  = new Color(1.00f, 0.75f, 0.10f);  // gold
    private static Color ColorPanel   = new Color(0.12f, 0.16f, 0.28f);  // darker navy
    private static Color ColorText    = new Color(0.95f, 0.97f, 1.00f);  // near white
    private static Color ColorGreen   = new Color(0.20f, 0.80f, 0.45f);
    private static Color ColorRed     = new Color(0.90f, 0.28f, 0.28f);
    private static Color ColorCard    = new Color(0.18f, 0.26f, 0.50f);  // card back color

    // ==================== Main Entry ====================

    [MenuItem("Tools/Memory Match Pro/🚀 Setup All (Scenes + Prefabs + Data)")]
    public static void SetupAll()
    {
        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Ensuring folders...", 0.0f);
        EnsureFolders();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating Mode Data...", 0.15f);
        CreateAllModeConfigs();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating CardPrefab...", 0.3f);
        CreateCardPrefab();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating LevelButtonPrefab...", 0.45f);
        CreateLevelButtonPrefab();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating MainMenuScene...", 0.6f);
        CreateMainMenuScene();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating ModeSelectScene...", 0.7f);
        CreateModeSelectScene();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating LevelSelectScene...", 0.8f);
        CreateLevelSelectScene();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Creating GameScene...", 0.9f);
        CreateGameScene();

        EditorUtility.DisplayProgressBar("Memory Match Pro Setup", "Updating Build Settings...", 0.95f);
        UpdateBuildSettings();

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Memory Match Pro",
            "✅ Setup hoàn tất!\n\n" +
            "• 4 Scenes đã được tạo\n" +
            "• CardPrefab & LevelButtonPrefab đã được tạo\n" +
            "• GameModeConfig & LevelData ScriptableObjects đã được tạo\n" +
            "• Build settings đã được cập nhật\n\n" +
            "Mở MainMenuScene để bắt đầu test!",
            "OK");
    }

    [MenuItem("Tools/Memory Match Pro/📋 Create Mode Data Only")]
    public static void CreateModeDataOnly()
    {
        EnsureFolders();
        CreateAllModeConfigs();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Memory Match Pro/🎬 Rebuild ModeSelect Scene Only")]
    public static void RebuildModeSelectSceneOnly()
    {
        EnsureFolders();
        CreateModeSelectScene();
        UpdateBuildSettings();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Memory Match Pro/🎬 Rebuild All Scenes")]
    public static void RebuildAllScenes()
    {
        EnsureFolders();
        CreateMainMenuScene();
        CreateModeSelectScene();
        CreateLevelSelectScene();
        CreateGameScene();
        UpdateBuildSettings();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/Memory Match Pro/📋 Create ScriptableObjects Only")]
    public static void CreateSOOnly() { EnsureFolders(); CreateAllModeConfigs(); AssetDatabase.Refresh(); }

    [MenuItem("Tools/Memory Match Pro/🎴 Create Prefabs Only")]
    public static void CreatePrefabsOnly() { EnsureFolders(); CreateCardPrefab(); CreateLevelButtonPrefab(); AssetDatabase.Refresh(); }

    [MenuItem("Tools/Memory Match Pro/🎬 Create Scenes Only")]
    public static void CreateScenesOnly() { EnsureFolders(); CreateMainMenuScene(); CreateLevelSelectScene(); CreateGameScene(); UpdateBuildSettings(); AssetDatabase.Refresh(); }

    // ==================== Folder Setup ====================

    private static void EnsureFolders()
    {
        string[] dirs = {
            "Assets/Scenes",
            "Assets/Scripts/Core", "Assets/Scripts/Data", "Assets/Scripts/UI",
            "Assets/Scripts/Audio", "Assets/Scripts/Save",
            "Assets/Prefabs",
            "Assets/ScriptableObjects/Levels",
            "Assets/ScriptableObjects/Modes",
            "Assets/Sprites/Cards", "Assets/Sprites/UI",
            "Assets/Audio/Music", "Assets/Audio/SFX",
            "Assets/Editor"
        };
        foreach (var dir in dirs)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                string parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
        }
    }

    // ==================== ScriptableObjects ====================

    private static void CreateAllLevelData()
    {
        var levelConfigs = new (int id, string name, int rows, int cols, float time, int baseScore,
            int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[]
        {
            (1,  "Level 1",  2, 2, 60f,  500,  100, 20, 30, 8f,  5),
            (2,  "Level 2",  2, 4, 80f,  600,  120, 25, 35, 10f, 4),
            (3,  "Level 3",  3, 4, 100f, 700,  140, 30, 40, 12f, 4),
            (4,  "Level 4",  4, 4, 120f, 800,  160, 35, 45, 15f, 3),
            (5,  "Level 5",  4, 5, 150f, 900,  180, 40, 50, 18f, 3),  // 20 cards ✓
            (6,  "Level 6",  5, 6, 180f, 1000, 200, 45, 55, 20f, 3),  // 30 cards ✓
            (7,  "Level 7",  6, 6, 220f, 1100, 220, 50, 60, 22f, 2),
            (8,  "Level 8",  6, 8, 260f, 1200, 240, 55, 65, 25f, 2),
            (9,  "Level 9",  7, 8, 300f, 1300, 260, 60, 70, 28f, 2),  // 56 cards ✓
            (10, "Level 10", 8, 8, 360f, 1500, 300, 70, 80, 30f, 1),
        };

        foreach (var cfg in levelConfigs)
        {
            string path = SO_PATH_LEVELS + $"Level{cfg.id}.asset";

            // Kiểm tra đã tồn tại chưa
            LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
            {
                // Cập nhật nếu đã có
                existing.levelId      = cfg.id;
                existing.levelName    = cfg.name;
                existing.rows         = cfg.rows;
                existing.columns      = cfg.cols;
                existing.timeLimit    = cfg.time;
                existing.baseScore    = cfg.baseScore;
                existing.matchScore   = cfg.matchScore;
                existing.comboBonus   = cfg.comboBonus;
                existing.wrongPenalty = cfg.wrong;
                existing.hintCooldown = cfg.hintCd;
                existing.maxHints     = cfg.maxHints;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                // Tạo mới
                LevelData data = ScriptableObject.CreateInstance<LevelData>();
                data.levelId      = cfg.id;
                data.levelName    = cfg.name;
                data.rows         = cfg.rows;
                data.columns      = cfg.cols;
                data.timeLimit    = cfg.time;
                data.baseScore    = cfg.baseScore;
                data.matchScore   = cfg.matchScore;
                data.comboBonus   = cfg.comboBonus;
                data.wrongPenalty = cfg.wrong;
                data.hintCooldown = cfg.hintCd;
                data.maxHints     = cfg.maxHints;
                AssetDatabase.CreateAsset(data, path);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] ✅ 10 LevelData ScriptableObjects created/updated.");
    }

    // ==================== Card Prefab ====================

    private static GameObject CreateCardPrefab()
    {
        // Root
        GameObject root = new GameObject("CardPrefab");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(120f, 120f);

        // Canvas Group (để fade)
        root.AddComponent<CanvasGroup>();

        // Back Image (mặt sau - màu navy với pattern)
        GameObject backObj = new GameObject("BackImage");
        backObj.transform.SetParent(root.transform, false);
        Image backImg = backObj.AddComponent<Image>();
        backImg.color = ColorCard;
        RectTransform backRect = backObj.GetComponent<RectTransform>();
        SetRectFull(backRect);

        // Back pattern label "?"
        GameObject backLabel = new GameObject("BackLabel");
        backLabel.transform.SetParent(backObj.transform, false);
        TextMeshProUGUI backTMP = backLabel.AddComponent<TextMeshProUGUI>();
        backTMP.text = "?";
        backTMP.fontSize = 48f;
        backTMP.alignment = TextAlignmentOptions.Center;
        backTMP.color = new Color(0.5f, 0.7f, 1f, 0.6f);
        RectTransform backLabelRect = backLabel.GetComponent<RectTransform>();
        SetRectFull(backLabelRect);

        // Front Image (mặt trước - màu theo cardId)
        GameObject frontObj = new GameObject("FrontImage");
        frontObj.transform.SetParent(root.transform, false);
        Image frontImg = frontObj.AddComponent<Image>();
        frontImg.color = new Color(0.95f, 0.30f, 0.30f); // placeholder
        RectTransform frontRect = frontObj.GetComponent<RectTransform>();
        SetRectFull(frontRect);
        frontObj.SetActive(false); // ẩn ban đầu

        // Front Card Icon (label)
        GameObject frontLabel = new GameObject("FrontLabel");
        frontLabel.transform.SetParent(frontObj.transform, false);
        TextMeshProUGUI frontTMP = frontLabel.AddComponent<TextMeshProUGUI>();
        frontTMP.text = "★";
        frontTMP.fontSize = 52f;
        frontTMP.alignment = TextAlignmentOptions.Center;
        frontTMP.color = Color.white;
        RectTransform frontLabelRect = frontLabel.GetComponent<RectTransform>();
        SetRectFull(frontLabelRect);

        // Button (trên root)
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = backImg;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.85f, 0.90f, 1.00f);
        cb.pressedColor     = new Color(0.70f, 0.80f, 0.95f);
        btn.colors = cb;

        // Card script
        Card card = root.AddComponent<Card>();
        // Dùng SerializedObject để set private fields
        SerializedObject so = new SerializedObject(card);
        so.FindProperty("cardButton").objectReferenceValue  = btn;
        so.FindProperty("backImage").objectReferenceValue   = backImg;
        so.FindProperty("frontImage").objectReferenceValue  = frontImg;
        so.ApplyModifiedProperties();

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH_CARD);
        Object.DestroyImmediate(root);

        Debug.Log("[Setup] ✅ CardPrefab created.");
        return prefab;
    }

    // ==================== Level Button Prefab ====================

    private static GameObject CreateLevelButtonPrefab()
    {
        GameObject root = new GameObject("LevelButtonPrefab");
        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(200f, 200f);

        // Background image
        Image bgImg = root.AddComponent<Image>();
        bgImg.color = ColorPrimary;

        // Button
        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = ColorPrimary;
        cb.highlightedColor = new Color(0.40f, 0.75f, 1f);
        cb.pressedColor     = new Color(0.15f, 0.45f, 0.85f);
        cb.disabledColor    = new Color(0.35f, 0.35f, 0.45f);
        btn.colors = cb;

        // Level Number Text
        GameObject numObj = new GameObject("LevelNumberText");
        numObj.transform.SetParent(root.transform, false);
        TextMeshProUGUI numTMP = numObj.AddComponent<TextMeshProUGUI>();
        numTMP.text = "1";
        numTMP.fontSize = 52f;
        numTMP.fontStyle = FontStyles.Bold;
        numTMP.alignment = TextAlignmentOptions.Center;
        numTMP.color = Color.white;
        RectTransform numRect = numObj.GetComponent<RectTransform>();
        numRect.anchorMin = new Vector2(0, 0.35f);
        numRect.anchorMax = new Vector2(1, 0.85f);
        numRect.offsetMin = numRect.offsetMax = Vector2.zero;

        // Best Score Text
        GameObject scoreObj = new GameObject("BestScoreText");
        scoreObj.transform.SetParent(root.transform, false);
        TextMeshProUGUI scoreTMP = scoreObj.AddComponent<TextMeshProUGUI>();
        scoreTMP.text = "--";
        scoreTMP.fontSize = 22f;
        scoreTMP.alignment = TextAlignmentOptions.Center;
        scoreTMP.color = new Color(0.9f, 0.95f, 1f);
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 0.0f);
        scoreRect.anchorMax = new Vector2(1, 0.35f);
        scoreRect.offsetMin = scoreRect.offsetMax = Vector2.zero;

        // Stars (3 star images)
        GameObject starsParent = new GameObject("Stars");
        starsParent.transform.SetParent(root.transform, false);
        HorizontalLayoutGroup hlg = starsParent.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        RectTransform starsRect = starsParent.GetComponent<RectTransform>();
        starsRect.anchorMin = new Vector2(0.05f, 0.80f);
        starsRect.anchorMax = new Vector2(0.95f, 1.00f);
        starsRect.offsetMin = starsRect.offsetMax = Vector2.zero;

        Image[] starImgs = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject starObj = new GameObject($"Star{i + 1}");
            starObj.transform.SetParent(starsParent.transform, false);
            Image starImg = starObj.AddComponent<Image>();
            starImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f); // off
            LayoutElement le = starObj.AddComponent<LayoutElement>();
            le.preferredWidth  = 28f;
            le.preferredHeight = 28f;
            starImgs[i] = starImg;
        }

        // Lock Icon
        GameObject lockObj = new GameObject("LockIcon");
        lockObj.transform.SetParent(root.transform, false);
        Image lockImg = lockObj.AddComponent<Image>();
        lockImg.color = new Color(0f, 0f, 0f, 0.55f);
        RectTransform lockRect = lockObj.GetComponent<RectTransform>();
        SetRectFull(lockRect);
        lockObj.SetActive(false);

        // Lock text "🔒"
        GameObject lockTxt = new GameObject("LockText");
        lockTxt.transform.SetParent(lockObj.transform, false);
        TextMeshProUGUI lockTMP = lockTxt.AddComponent<TextMeshProUGUI>();
        lockTMP.text = "🔒";
        lockTMP.fontSize = 48f;
        lockTMP.alignment = TextAlignmentOptions.Center;
        lockTMP.color = Color.white;
        RectTransform lockTxtRect = lockTxt.GetComponent<RectTransform>();
        SetRectFull(lockTxtRect);

        // LevelButtonUI script
        LevelButtonUI btnUI = root.AddComponent<LevelButtonUI>();
        SerializedObject so = new SerializedObject(btnUI);
        so.FindProperty("button").objectReferenceValue          = btn;
        so.FindProperty("levelNumberText").objectReferenceValue = numTMP;
        so.FindProperty("bestScoreText").objectReferenceValue   = scoreTMP;
        so.FindProperty("lockIcon").objectReferenceValue        = lockObj;
        SerializedProperty starsProp = so.FindProperty("starImages");
        starsProp.arraySize = 3;
        for (int i = 0; i < 3; i++)
            starsProp.GetArrayElementAtIndex(i).objectReferenceValue = starImgs[i];
        so.ApplyModifiedProperties();

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH_BTN);
        Object.DestroyImmediate(root);

        Debug.Log("[Setup] ✅ LevelButtonPrefab created.");
        return prefab;
    }

    // ==================== Main Menu Scene ====================

    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags  = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorBg;
        cam.orthographic = true;
        cam.depth = -1;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 0, -10);

        // EventSystem
        CreateEventSystem();

        // AudioManager GameObject
        GameObject audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        // LevelManager GameObject
        GameObject lmGO = new GameObject("LevelManager");
        LevelManager lm = lmGO.AddComponent<LevelManager>();
        // Link all mode configs
        AssignAllModesToLevelManager(lm);

        // Canvas
        GameObject canvasGO = CreateCanvas("Canvas");
        Canvas canvas = canvasGO.GetComponent<Canvas>();

        // Background
        GameObject bg = CreateImage(canvasGO, "Background", ColorBg, true);
        SetRectFull(bg.GetComponent<RectTransform>());

        // Gradient overlay (decorative panel)
        GameObject gradPanel = CreatePanel(canvasGO, "GradientOverlay",
            new Color(0.15f, 0.25f, 0.55f, 0.45f));
        SetRectFull(gradPanel.GetComponent<RectTransform>());

        // Title
        GameObject titleGO = CreateTMPText(canvasGO, "TitleText",
            "MEMORY\nMATCH PRO", 90f, ColorAccent, FontStyles.Bold);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 480f);
        titleRect.sizeDelta = new Vector2(900f, 300f);

        // Subtitle
        GameObject subtitleGO = CreateTMPText(canvasGO, "SubtitleText",
            "Train Your Brain!", 38f, new Color(0.75f, 0.88f, 1f), FontStyles.Normal);
        RectTransform subRect = subtitleGO.GetComponent<RectTransform>();
        subRect.anchoredPosition = new Vector2(0, 330f);
        subRect.sizeDelta = new Vector2(700f, 80f);

        // Decorative card icons row
        GameObject decoRow = new GameObject("DecoRow");
        decoRow.transform.SetParent(canvasGO.transform, false);
        HorizontalLayoutGroup hlg = decoRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        RectTransform decoRect = decoRow.GetComponent<RectTransform>();
        decoRect.anchoredPosition = new Vector2(0, 200f);
        decoRect.sizeDelta = new Vector2(700f, 80f);
        string[] icons = {"🎴", "🃏", "🎯", "🃏", "🎴"};
        Color[] iconColors = {
            new Color(1f,0.3f,0.3f), new Color(0.3f,0.75f,0.4f),
            new Color(1f,0.85f,0.1f),
            new Color(0.3f,0.55f,1f), new Color(0.8f,0.35f,0.9f)
        };
        for (int i = 0; i < icons.Length; i++)
        {
            GameObject ico = CreateTMPText(decoRow, $"Icon{i}", icons[i], 42f, iconColors[i]);
            LayoutElement le = ico.AddComponent<LayoutElement>();
            le.preferredWidth = 70f; le.preferredHeight = 70f;
        }

        // Play Button
        GameObject playBtn = CreateButton(canvasGO, "PlayButton", "▶  PLAY",
            ColorPrimary, 52f);
        RectTransform playRect = playBtn.GetComponent<RectTransform>();
        playRect.anchoredPosition = new Vector2(0, 30f);
        playRect.sizeDelta = new Vector2(480f, 110f);

        // Settings Button
        GameObject settBtn = CreateButton(canvasGO, "SettingsButton", "⚙  SETTINGS",
            new Color(0.25f, 0.35f, 0.60f), 42f);
        RectTransform settRect = settBtn.GetComponent<RectTransform>();
        settRect.anchoredPosition = new Vector2(0, -110f);
        settRect.sizeDelta = new Vector2(480f, 95f);

        // Exit Button
        GameObject exitBtn = CreateButton(canvasGO, "ExitButton", "✕  EXIT",
            new Color(0.40f, 0.15f, 0.18f), 42f);
        RectTransform exitRect = exitBtn.GetComponent<RectTransform>();
        exitRect.anchoredPosition = new Vector2(0, -235f);
        exitRect.sizeDelta = new Vector2(480f, 95f);

        // Settings Panel (popup)
        GameObject settPanel = CreateSettingsPanel(canvasGO);
        settPanel.SetActive(false);

        // MainMenuUI script
        GameObject uiControllerGO = new GameObject("MainMenuUI");
        uiControllerGO.transform.SetParent(canvasGO.transform, false);
        MainMenuUI mainUI = uiControllerGO.AddComponent<MainMenuUI>();
        
        // SettingsUI script
        SettingsUI settUI = settPanel.GetComponent<SettingsUI>();
        Button closeBtn = settPanel.transform.Find("PanelBox/CloseButton")?.GetComponent<Button>(); // find from panel structure

        SerializedObject so = new SerializedObject(mainUI);
        so.FindProperty("playButton").objectReferenceValue     = playBtn.GetComponent<Button>();
        so.FindProperty("settingsButton").objectReferenceValue = settBtn.GetComponent<Button>();
        so.FindProperty("exitButton").objectReferenceValue     = exitBtn.GetComponent<Button>();
        so.FindProperty("settingsPanel").objectReferenceValue  = settPanel;
        so.FindProperty("titleRect").objectReferenceValue      = titleRect;
        if (closeBtn != null)
            so.FindProperty("settingsCloseButton").objectReferenceValue = closeBtn;
        if (settUI != null)
            so.FindProperty("settingsUI").objectReferenceValue = settUI;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_PATH_MAIN);
        Debug.Log("[Setup] ✅ MainMenuScene created.");
    }

    // ==================== Level Select Scene ====================

    private static void CreateLevelSelectScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        // LevelManager
        GameObject lmGO = new GameObject("LevelManager");
        LevelManager lm = lmGO.AddComponent<LevelManager>();
        AssignAllModesToLevelManager(lm);

        // AudioManager check
        GameObject audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        // Canvas
        GameObject canvasGO = CreateCanvas("Canvas");

        // Background
        GameObject bg = CreateImage(canvasGO, "Background", ColorBg, true);
        SetRectFull(bg.GetComponent<RectTransform>());

        // Decorative top strip
        GameObject topStrip = CreatePanel(canvasGO, "TopStrip", ColorPrimary);
        RectTransform tsRect = topStrip.GetComponent<RectTransform>();
        tsRect.anchorMin = new Vector2(0, 1);
        tsRect.anchorMax = new Vector2(1, 1);
        tsRect.pivot = new Vector2(0.5f, 1f);
        tsRect.offsetMin = new Vector2(0, -140f);
        tsRect.offsetMax = Vector2.zero;

        // Back Button
        GameObject backBtn = CreateButton(topStrip, "BackButton", "← Back", 
            new Color(0.15f, 0.25f, 0.50f), 36f);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 0);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 0.5f);
        backRect.offsetMin = new Vector2(20f, 10f);
        backRect.offsetMax = new Vector2(200f, -10f);

        // Header Text
        GameObject headerTxt = CreateTMPText(topStrip, "HeaderText",
            "SELECT LEVEL", 52f, Color.white, FontStyles.Bold);
        RectTransform headerRect = headerTxt.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = new Vector2(180f, 0);
        headerRect.offsetMax = new Vector2(-20f, 0);
        headerTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ScrollView
        GameObject scrollGO = CreateScrollView(canvasGO, "ScrollView");
        RectTransform scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(20f, 20f);
        scrollRect.offsetMax = new Vector2(-20f, -145f);

        // Find Content child of ScrollView
        Transform content = scrollGO.transform.Find("Viewport/Content");
        if (content == null)
        {
            Debug.LogError("[Setup] ScrollView Content not found!");
        }

        // LevelSelectUI script
        GameObject uiGO = new GameObject("LevelSelectUI");
        uiGO.transform.SetParent(canvasGO.transform, false);
        LevelSelectUI lvUI = uiGO.AddComponent<LevelSelectUI>();

        // Load prefab
        GameObject lvBtnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH_BTN);

        SerializedObject so = new SerializedObject(lvUI);
        so.FindProperty("levelButtonPrefab").objectReferenceValue = lvBtnPrefab;
        so.FindProperty("contentParent").objectReferenceValue     = content;
        so.FindProperty("backButton").objectReferenceValue        = backBtn.GetComponent<Button>();
        so.FindProperty("headerText").objectReferenceValue        = headerTxt.GetComponent<TextMeshProUGUI>();
        if (content != null)
        {
            GridLayoutGroup glg = content.GetComponent<GridLayoutGroup>();
            so.FindProperty("gridLayoutGroup").objectReferenceValue = glg;
        }
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_PATH_SELECT);
        Debug.Log("[Setup] ✅ LevelSelectScene created.");
    }

    // ==================== Game Scene ====================

    private static void CreateGameScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        // Managers
        GameObject lmGO = new GameObject("LevelManager");
        LevelManager lm = lmGO.AddComponent<LevelManager>();
        AssignAllModesToLevelManager(lm);

        GameObject audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        GameObject gmGO = new GameObject("GameManager");
        GameObject bmGO = new GameObject("BoardManager");

        // Canvas
        GameObject canvasGO = CreateCanvas("Canvas");

        // Background
        GameObject bg = CreateImage(canvasGO, "Background", ColorBg, true);
        SetRectFull(bg.GetComponent<RectTransform>());

        // ---- TOP PANEL ----
        GameObject topPanel = CreatePanel(canvasGO, "TopPanel", new Color(0.10f, 0.14f, 0.25f, 0.95f));
        RectTransform topRect = topPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.offsetMin = new Vector2(0, -200f);
        topRect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup topHLG = topPanel.AddComponent<HorizontalLayoutGroup>();
        topHLG.spacing = 0;
        topHLG.padding = new RectOffset(20, 20, 10, 10);
        topHLG.childAlignment = TextAnchor.MiddleCenter;
        topHLG.childForceExpandWidth = true;
        topHLG.childForceExpandHeight = true;

        // Timer cell
        GameObject timerCell = CreateStatCell(topPanel, "TimerCell", "Time", "01:00",
            ColorAccent, out TextMeshProUGUI timerLabel, out TextMeshProUGUI timerText);
        // Moves cell
        GameObject movesCell = CreateStatCell(topPanel, "MovesCell", "Moves", "0",
            ColorPrimary, out TextMeshProUGUI movesLabel, out TextMeshProUGUI movesText);
        // Score cell
        GameObject scoreCell = CreateStatCell(topPanel, "ScoreCell", "Score", "0",
            ColorGreen, out TextMeshProUGUI scoreLabel, out TextMeshProUGUI scoreText);
        // Combo cell
        GameObject comboCell = CreateStatCell(topPanel, "ComboCell", "Combo", "x0",
            new Color(0.9f, 0.4f, 1f), out TextMeshProUGUI comboLabel, out TextMeshProUGUI comboText);

        // ---- CARD GRID PANEL ----
        GameObject gridPanel = new GameObject("CardGridPanel");
        gridPanel.transform.SetParent(canvasGO.transform, false);
        gridPanel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.18f, 0.8f);
        RectTransform gridPanelRect = gridPanel.GetComponent<RectTransform>();
        gridPanelRect.anchorMin = new Vector2(0, 0.15f);
        gridPanelRect.anchorMax = new Vector2(1, 1);
        gridPanelRect.offsetMin = new Vector2(15f, 0);
        gridPanelRect.offsetMax = new Vector2(-15f, -205f);

        // Grid Layout Group
        GridLayoutGroup glg = gridPanel.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(120f, 120f);
        glg.spacing = new Vector2(10f, 10f);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.MiddleCenter;
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;

        // ---- BOTTOM PANEL ----
        GameObject bottomPanel = CreatePanel(canvasGO, "BottomPanel",
            new Color(0.10f, 0.14f, 0.25f, 0.95f));
        RectTransform bottomRect = bottomPanel.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = new Vector2(0, 150f);

        HorizontalLayoutGroup bottomHLG = bottomPanel.AddComponent<HorizontalLayoutGroup>();
        bottomHLG.spacing = 30f;
        bottomHLG.padding = new RectOffset(40, 40, 15, 15);
        bottomHLG.childAlignment = TextAnchor.MiddleCenter;
        bottomHLG.childForceExpandWidth = true;
        bottomHLG.childForceExpandHeight = false;

        // Hint Button
        GameObject hintBtnGO = CreateButton(bottomPanel, "HintButton", "Hint (5)",
            new Color(0.80f, 0.55f, 0.10f), 32f);
        RectTransform hintRect = hintBtnGO.GetComponent<RectTransform>();
        hintRect.sizeDelta = new Vector2(200f, 100f);
        LayoutElement hintLE = hintBtnGO.AddComponent<LayoutElement>();
        hintLE.preferredHeight = 100f;

        // Pause Button
        GameObject pauseBtnGO = CreateButton(bottomPanel, "PauseButton", "Pause",
            new Color(0.25f, 0.35f, 0.60f), 32f);
        RectTransform pauseRect = pauseBtnGO.GetComponent<RectTransform>();
        pauseRect.sizeDelta = new Vector2(200f, 100f);
        LayoutElement pauseLE = pauseBtnGO.AddComponent<LayoutElement>();
        pauseLE.preferredHeight = 100f;

        // ---- WIN PANEL ----
        GameObject winPanel = CreateModalPanel(canvasGO, "WinPanel",
            new Color(0.08f, 0.15f, 0.10f, 0.95f), "YOU WIN!", ColorAccent);
        winPanel.SetActive(false);

        // Win panel internals
        Transform winContent = winPanel.transform.Find("PanelContent");
        TextMeshProUGUI winScoreText = null, winTimeText = null, winMovesText = null, winComboText = null;
        Button nextLvlBtn = null, winReplayBtn = null, winMenuBtn = null;
        Image[] winStarImgs = null;

        if (winContent != null)
        {
            winScoreText = CreateTMPText(winContent.gameObject, "WinScoreText", "Score\n0", 38f, Color.white).GetComponent<TextMeshProUGUI>();
            winTimeText  = CreateTMPText(winContent.gameObject, "WinTimeText",  "Time\n0:00", 34f, new Color(0.85f, 0.95f, 1f)).GetComponent<TextMeshProUGUI>();
            winMovesText = CreateTMPText(winContent.gameObject, "WinMovesText", "Moves\n0",  34f, new Color(0.85f, 0.95f, 1f)).GetComponent<TextMeshProUGUI>();
            winComboText = CreateTMPText(winContent.gameObject, "WinComboText", "Combo\nx0", 34f, new Color(0.9f,0.75f,0.2f)).GetComponent<TextMeshProUGUI>();

            // Stars
            GameObject starsRow = new GameObject("StarsRow");
            starsRow.transform.SetParent(winContent.transform, false);
            HorizontalLayoutGroup starsHLG = starsRow.AddComponent<HorizontalLayoutGroup>();
            starsHLG.spacing = 15f; starsHLG.childAlignment = TextAnchor.MiddleCenter;
            starsHLG.childForceExpandWidth = false; starsHLG.childForceExpandHeight = false;
            RectTransform starsRowRect = starsRow.GetComponent<RectTransform>();
            starsRowRect.sizeDelta = new Vector2(400f, 60f);

            winStarImgs = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                GameObject s = CreateTMPText(starsRow, $"Star{i+1}", "★", 50f, new Color(0.3f,0.3f,0.3f,0.5f));
                winStarImgs[i] = s.GetComponent<Image>();
                LayoutElement le = s.AddComponent<LayoutElement>();
                le.preferredWidth = 60f; le.preferredHeight = 60f;
            }

            // Buttons row
            GameObject btnRow = new GameObject("WinButtonsRow");
            btnRow.transform.SetParent(winContent.transform, false);
            HorizontalLayoutGroup btnHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHLG.spacing = 20f; btnHLG.childAlignment = TextAnchor.MiddleCenter;
            btnHLG.childForceExpandWidth = false;
            RectTransform btnRowRect = btnRow.GetComponent<RectTransform>();
            btnRowRect.sizeDelta = new Vector2(860f, 100f);

            nextLvlBtn  = CreateButton(btnRow, "NextLevelBtn",  "▶ Next",   ColorGreen,   34f).GetComponent<Button>();
            winReplayBtn = CreateButton(btnRow, "WinReplayBtn", "↺ Replay", ColorPrimary, 34f).GetComponent<Button>();
            winMenuBtn   = CreateButton(btnRow, "WinMenuBtn",   "⌂ Menu",   ColorPanel,   34f).GetComponent<Button>();
            foreach (var b in new[]{nextLvlBtn, winReplayBtn, winMenuBtn})
            {
                LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 240f; le.preferredHeight = 90f;
            }
        }

        // ---- LOSE PANEL ----
        GameObject losePanel = CreateModalPanel(canvasGO, "LosePanel",
            new Color(0.18f, 0.06f, 0.06f, 0.95f), "⏱ TIME'S UP!", ColorRed);
        losePanel.SetActive(false);
        Button loseReplayBtn = null, loseMenuBtn = null;
        Transform loseContent = losePanel.transform.Find("PanelContent");
        if (loseContent != null)
        {
            CreateTMPText(loseContent.gameObject, "LoseMsg", "Better luck next time!", 36f, new Color(0.9f,0.7f,0.7f));
            GameObject lBtnRow = new GameObject("LoseButtonsRow");
            lBtnRow.transform.SetParent(loseContent.transform, false);
            HorizontalLayoutGroup lHLG = lBtnRow.AddComponent<HorizontalLayoutGroup>();
            lHLG.spacing = 25f; lHLG.childAlignment = TextAnchor.MiddleCenter;
            lHLG.childForceExpandWidth = false;
            RectTransform lBtnRect = lBtnRow.GetComponent<RectTransform>();
            lBtnRect.sizeDelta = new Vector2(620f, 100f);
            loseReplayBtn = CreateButton(lBtnRow, "LoseReplayBtn", "↺ Replay", ColorPrimary, 36f).GetComponent<Button>();
            loseMenuBtn   = CreateButton(lBtnRow, "LoseMenuBtn",   "⌂ Menu",   ColorPanel,   36f).GetComponent<Button>();
            foreach (var b in new[]{loseReplayBtn, loseMenuBtn})
            {
                LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 270f; le.preferredHeight = 90f;
            }
        }

        // ---- PAUSE PANEL ----
        GameObject pausePanel = CreateModalPanel(canvasGO, "PausePanel",
            new Color(0.05f, 0.08f, 0.18f, 0.95f), "⏸ PAUSED", ColorPrimary);
        pausePanel.SetActive(false);
        Button resumeBtn = null, pauseReplayBtn = null, pauseMenuBtn = null;
        Transform pauseContent = pausePanel.transform.Find("PanelContent");
        if (pauseContent != null)
        {
            GameObject pBtnRow = new GameObject("PauseButtonsRow");
            pBtnRow.transform.SetParent(pauseContent.transform, false);
            VerticalLayoutGroup pVLG = pBtnRow.AddComponent<VerticalLayoutGroup>();
            pVLG.spacing = 20f; pVLG.childAlignment = TextAnchor.MiddleCenter;
            pVLG.childForceExpandWidth = false;
            RectTransform pBtnRect = pBtnRow.GetComponent<RectTransform>();
            pBtnRect.sizeDelta = new Vector2(440f, 350f);
            resumeBtn      = CreateButton(pBtnRow, "ResumeBtn",     "▶ Resume",  ColorGreen,   40f).GetComponent<Button>();
            pauseReplayBtn = CreateButton(pBtnRow, "PauseReplayBtn","↺ Restart", ColorPrimary, 40f).GetComponent<Button>();
            pauseMenuBtn   = CreateButton(pBtnRow, "PauseMenuBtn",  "⌂ Menu",    ColorPanel,   40f).GetComponent<Button>();
            foreach (var b in new[]{resumeBtn, pauseReplayBtn, pauseMenuBtn})
            {
                LayoutElement le = b.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 400f; le.preferredHeight = 90f;
            }
        }

        // ---- BoardManager ----
        BoardManager bm = bmGO.AddComponent<BoardManager>();
        GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH_CARD);
        SerializedObject bmSO = new SerializedObject(bm);
        bmSO.FindProperty("cardPrefab").objectReferenceValue    = cardPrefab;
        bmSO.FindProperty("cardGrid").objectReferenceValue      = glg;
        bmSO.FindProperty("gridContainer").objectReferenceValue = gridPanel.GetComponent<RectTransform>();
        bmSO.ApplyModifiedProperties();

        // ---- GameUI ----
        GameObject gameUIGO = new GameObject("GameUI");
        gameUIGO.transform.SetParent(canvasGO.transform, false);
        GameUI gameUI = gameUIGO.AddComponent<GameUI>();
        SerializedObject guiSO = new SerializedObject(gameUI);
        guiSO.FindProperty("timeText").objectReferenceValue   = timerText;
        guiSO.FindProperty("movesText").objectReferenceValue  = movesText;
        guiSO.FindProperty("scoreText").objectReferenceValue  = scoreText;
        guiSO.FindProperty("comboText").objectReferenceValue  = comboText;
        guiSO.FindProperty("hintButton").objectReferenceValue = hintBtnGO.GetComponent<Button>();
        guiSO.FindProperty("hintButtonText").objectReferenceValue = hintBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        guiSO.FindProperty("pauseButton").objectReferenceValue = pauseBtnGO.GetComponent<Button>();
        guiSO.FindProperty("winPanel").objectReferenceValue   = winPanel;
        guiSO.FindProperty("losePanel").objectReferenceValue  = losePanel;
        guiSO.FindProperty("pausePanel").objectReferenceValue = pausePanel;
        // Win panel sub-elements
        if (winScoreText  != null) guiSO.FindProperty("winScoreText").objectReferenceValue  = winScoreText;
        if (winTimeText   != null) guiSO.FindProperty("winTimeText").objectReferenceValue   = winTimeText;
        if (winMovesText  != null) guiSO.FindProperty("winMovesText").objectReferenceValue  = winMovesText;
        if (winComboText  != null) guiSO.FindProperty("winComboText").objectReferenceValue  = winComboText;
        if (nextLvlBtn    != null) guiSO.FindProperty("nextLevelButton").objectReferenceValue  = nextLvlBtn;
        if (winReplayBtn  != null) guiSO.FindProperty("winReplayButton").objectReferenceValue  = winReplayBtn;
        if (winMenuBtn    != null) guiSO.FindProperty("winMenuButton").objectReferenceValue    = winMenuBtn;
        if (loseReplayBtn != null) guiSO.FindProperty("loseReplayButton").objectReferenceValue = loseReplayBtn;
        if (loseMenuBtn   != null) guiSO.FindProperty("loseMenuButton").objectReferenceValue   = loseMenuBtn;
        if (resumeBtn     != null) guiSO.FindProperty("resumeButton").objectReferenceValue     = resumeBtn;
        if (pauseReplayBtn!= null) guiSO.FindProperty("pauseReplayButton").objectReferenceValue= pauseReplayBtn;
        if (pauseMenuBtn  != null) guiSO.FindProperty("pauseMenuButton").objectReferenceValue  = pauseMenuBtn;
        if (winStarImgs != null && winStarImgs.Length > 0)
        {
            SerializedProperty starProp = guiSO.FindProperty("starImages");

            if (starProp != null && starProp.isArray)
            {
                starProp.arraySize = winStarImgs.Length;

                for (int i = 0; i < winStarImgs.Length; i++)
                {
                    starProp.GetArrayElementAtIndex(i).objectReferenceValue = winStarImgs[i];
                }
            }
            else
            {
                Debug.LogWarning("[Setup] GameUI không có field 'starImages' hoặc field này không phải array. Bỏ qua gán star images.");
            }
        }
        guiSO.ApplyModifiedProperties();

        // ---- GameManager ----
        GameManager gm = gmGO.AddComponent<GameManager>();
        SerializedObject gmSO = new SerializedObject(gm);
        gmSO.FindProperty("boardManager").objectReferenceValue = bm;
        gmSO.FindProperty("gameUI").objectReferenceValue       = gameUI;
        gmSO.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, SCENE_PATH_GAME);
        Debug.Log("[Setup] ✅ GameScene created.");
    }

    // ==================== Settings Panel (Popup) ====================

    private static GameObject CreateSettingsPanel(GameObject parent)
    {
        // Semi-transparent overlay
        GameObject overlay = CreatePanel(parent, "SettingsPanel", new Color(0f, 0f, 0f, 0.6f));
        SetRectFull(overlay.GetComponent<RectTransform>());

        // Panel content box
        GameObject panel = CreatePanel(overlay, "PanelBox", ColorPanel);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.05f, 0.20f);
        pr.anchorMax = new Vector2(0.95f, 0.80f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        // Title
        GameObject titleGO = CreateTMPText(panel, "SettingsTitle", "⚙ SETTINGS", 52f, ColorAccent, FontStyles.Bold);
        RectTransform titleR = titleGO.GetComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0, 0.82f);
        titleR.anchorMax = new Vector2(1, 1f);
        titleR.offsetMin = titleR.offsetMax = Vector2.zero;

        // Vertical layout for settings
        GameObject content = new GameObject("SettingsContent");
        content.transform.SetParent(panel.transform, false);
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 30f;
        vlg.padding = new RectOffset(60, 60, 20, 20);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        RectTransform contentR = content.GetComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0, 0.15f);
        contentR.anchorMax = new Vector2(1, 0.82f);
        contentR.offsetMin = contentR.offsetMax = Vector2.zero;

        // Sound Toggle
        Toggle soundToggle = CreateToggleRow(content, "SoundRow", "🔊 Sound Effects", out _);
        soundToggle.isOn = true;

        // Music Toggle
        Toggle musicToggle = CreateToggleRow(content, "MusicRow", "🎵 Background Music", out _);
        musicToggle.isOn = true;

        // Difficulty Dropdown
        GameObject diffRow = new GameObject("DifficultyRow");
        diffRow.transform.SetParent(content.transform, false);
        HorizontalLayoutGroup diffHLG = diffRow.AddComponent<HorizontalLayoutGroup>();
        diffHLG.spacing = 20f; diffHLG.childForceExpandWidth = false;
        diffHLG.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement diffLE = diffRow.AddComponent<LayoutElement>();
        diffLE.preferredHeight = 70f; diffLE.flexibleWidth = 1;

        GameObject diffLabel = CreateTMPText(diffRow, "DiffLabel", "🎯 Difficulty", 36f, ColorText);
        LayoutElement dlLE = diffLabel.AddComponent<LayoutElement>();
        dlLE.preferredWidth = 320f;

        GameObject dropObj = new GameObject("DifficultyDropdown");
        dropObj.transform.SetParent(diffRow.transform, false);
        TMP_Dropdown drop = dropObj.AddComponent<TMP_Dropdown>();
        Image dropImg = dropObj.AddComponent<Image>();
        dropImg.color = ColorPrimary;
        LayoutElement dropLE = dropObj.AddComponent<LayoutElement>();
        dropLE.preferredWidth = 260f; dropLE.preferredHeight = 65f;

        // Reset Progress Button
        GameObject resetBtn = CreateButton(content, "ResetProgressButton",
            "🗑 Reset Progress", ColorRed, 34f);
        LayoutElement resetLE = resetBtn.AddComponent<LayoutElement>();
        resetLE.preferredHeight = 80f;

        // Close Button
        GameObject closeBtn = CreateButton(panel, "CloseButton", "✕", ColorRed, 40f);
        RectTransform closeBtnR = closeBtn.GetComponent<RectTransform>();
        closeBtnR.anchorMin = new Vector2(1, 1);
        closeBtnR.anchorMax = new Vector2(1, 1);
        closeBtnR.pivot = new Vector2(1, 1);
        closeBtnR.anchoredPosition = new Vector2(-15f, -15f);
        closeBtnR.sizeDelta = new Vector2(70f, 70f);

        // SettingsUI script
        SettingsUI settUI = overlay.AddComponent<SettingsUI>();
        SerializedObject so = new SerializedObject(settUI);
        so.FindProperty("soundToggle").objectReferenceValue        = soundToggle;
        so.FindProperty("musicToggle").objectReferenceValue        = musicToggle;
        so.FindProperty("difficultyDropdown").objectReferenceValue = drop;
        so.FindProperty("resetProgressButton").objectReferenceValue = resetBtn.GetComponent<Button>();
        so.FindProperty("closeButton").objectReferenceValue        = closeBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        return overlay;
    }

    // ==================== Build Settings ====================

    private static void UpdateBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(SCENE_PATH_MAIN,   true),
            new EditorBuildSettingsScene(SCENE_PATH_MODE,   true),
            new EditorBuildSettingsScene(SCENE_PATH_SELECT, true),
            new EditorBuildSettingsScene(SCENE_PATH_GAME,   true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Setup] ✅ Build Settings updated with 4 scenes.");
    }

    // ==================== Helper: LevelManager data ====================

    private static void AssignAllModesToLevelManager(LevelManager lm)
    {
        string[] modes = { "Easy", "Normal", "Hard", "Expert" };
        List<GameModeConfig> modeConfigs = new List<GameModeConfig>();
        foreach (var m in modes)
        {
            string path = $"Assets/ScriptableObjects/Modes/Mode_{m}.asset";
            GameModeConfig config = AssetDatabase.LoadAssetAtPath<GameModeConfig>(path);
            if (config != null) modeConfigs.Add(config);
        }

        SerializedObject so = new SerializedObject(lm);
        SerializedProperty prop = so.FindProperty("allModes");
        if (prop != null)
        {
            prop.arraySize = modeConfigs.Count;
            for (int i = 0; i < modeConfigs.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = modeConfigs[i];
        }
        so.ApplyModifiedProperties();
    }

    // ==================== Mode creation details ====================

    private static void CreateAllModeConfigs()
    {
        var easyConfigs = new (int id, string name, int rows, int cols, float time, int baseScore,
            int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[]
        {
            (1, "Level 1", 2, 2, 90f,  500, 100, 20, 30, 8f,  5),
            (2, "Level 2", 2, 4, 100f, 600, 120, 25, 35, 10f, 5),
            (3, "Level 3", 3, 4, 130f, 700, 140, 30, 40, 12f, 5),
            (4, "Level 4", 4, 4, 160f, 800, 160, 35, 45, 15f, 4),
            (5, "Level 5", 4, 4, 180f, 850, 170, 35, 45, 15f, 4),
        };

        var normalConfigs = new (int id, string name, int rows, int cols, float time, int baseScore,
            int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[]
        {
            (1,  "Level 1",  2, 2, 60f,  500,  100, 20, 30, 8f,  5),
            (2,  "Level 2",  2, 4, 80f,  600,  120, 25, 35, 10f, 4),
            (3,  "Level 3",  3, 4, 100f, 700,  140, 30, 40, 12f, 4),
            (4,  "Level 4",  4, 4, 120f, 800,  160, 35, 45, 15f, 3),
            (5,  "Level 5",  4, 5, 150f, 900,  180, 40, 50, 18f, 3),
            (6,  "Level 6",  5, 6, 180f, 1000, 200, 45, 55, 20f, 3),
            (7,  "Level 7",  6, 6, 220f, 1100, 220, 50, 60, 22f, 2),
            (8,  "Level 8",  6, 8, 260f, 1200, 240, 55, 65, 25f, 2),
            (9,  "Level 9",  7, 8, 300f, 1300, 260, 60, 70, 28f, 2),
            (10, "Level 10", 8, 8, 360f, 1500, 300, 70, 80, 30f, 1),
        };

        var hardConfigs = new (int id, string name, int rows, int cols, float time, int baseScore,
            int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[]
        {
            (1,  "Level 1",  3, 4, 75f,  700,  140, 30, 40, 12f, 3),
            (2,  "Level 2",  4, 4, 90f,  800,  160, 35, 45, 15f, 3),
            (3,  "Level 3",  4, 5, 100f, 900,  180, 40, 50, 18f, 3),
            (4,  "Level 4",  4, 6, 110f, 1000, 200, 45, 55, 20f, 2),
            (5,  "Level 5",  5, 6, 125f, 1100, 220, 50, 60, 22f, 2),
            (6,  "Level 6",  6, 6, 145f, 1200, 240, 55, 65, 25f, 2),
            (7,  "Level 7",  6, 8, 175f, 1300, 260, 60, 70, 28f, 1),
            (8,  "Level 8",  7, 8, 200f, 1400, 280, 65, 75, 30f, 1),
            (9,  "Level 9",  8, 8, 230f, 1500, 300, 70, 80, 32f, 1),
            (10, "Level 10", 8, 8, 260f, 1600, 320, 75, 85, 35f, 1),
        };

        var expertConfigs = new (int id, string name, int rows, int cols, float time, int baseScore,
            int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[]
        {
            (1,  "Level 1",  3, 4, 56f,  700,  140, 30, 40, 12f, 1),
            (2,  "Level 2",  4, 4, 68f,  800,  160, 35, 45, 15f, 1),
            (3,  "Level 3",  4, 5, 75f,  900,  180, 40, 50, 18f, 1),
            (4,  "Level 4",  4, 6, 82f,  1000, 200, 45, 55, 20f, 0),
            (5,  "Level 5",  5, 6, 94f,  1100, 220, 50, 60, 22f, 0),
            (6,  "Level 6",  6, 6, 109f, 1200, 240, 55, 65, 25f, 0),
            (7,  "Level 7",  6, 8, 131f, 1300, 260, 60, 70, 28f, 0),
            (8,  "Level 8",  7, 8, 150f, 1400, 280, 65, 75, 30f, 0),
            (9,  "Level 9",  8, 8, 172f, 1500, 300, 70, 80, 32f, 0),
            (10, "Level 10", 8, 8, 195f, 1600, 320, 75, 85, 35f, 0),
        };

        var easyLevels   = CreateOrUpdateLevels(GameModeType.Easy, easyConfigs);
        var normalLevels = CreateOrUpdateLevels(GameModeType.Normal, normalConfigs);
        var hardLevels   = CreateOrUpdateLevels(GameModeType.Hard, hardConfigs);
        var expertLevels = CreateOrUpdateLevels(GameModeType.Expert, expertConfigs);

        CreateOrUpdateModeConfig(GameModeType.Easy,   "Easy",   "Relaxed memory matching with generous timers.", new Color(0.20f, 0.75f, 0.40f), true,  true,  easyLevels);
        CreateOrUpdateModeConfig(GameModeType.Normal, "Normal", "Standard challenge. Complete levels sequentially.", new Color(0.25f, 0.55f, 0.95f), false, true,  normalLevels);
        CreateOrUpdateModeConfig(GameModeType.Hard,   "Hard",   "Tough layouts and tighter time limits.",            new Color(0.85f, 0.45f, 0.15f), false, false, hardLevels);
        CreateOrUpdateModeConfig(GameModeType.Expert, "Expert", "Strict timers and very limited hints.",             new Color(0.70f, 0.15f, 0.80f), false, false, expertLevels);
    }

    private static List<LevelData> CreateOrUpdateLevels(GameModeType mode, (int id, string name, int rows, int cols, float time, int baseScore, int matchScore, int comboBonus, int wrong, float hintCd, int maxHints)[] configs)
    {
        List<LevelData> levelDataList = new List<LevelData>();
        foreach (var cfg in configs)
        {
            string modeName = mode.ToString();
            string path = SO_PATH_LEVELS + $"Level_{modeName}_{cfg.id}.asset";

            LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (existing != null)
            {
                existing.levelId      = cfg.id;
                existing.levelName    = cfg.name;
                existing.targetMode   = mode;
                existing.rows         = cfg.rows;
                existing.columns      = cfg.cols;
                existing.timeLimit    = cfg.time;
                existing.baseScore    = cfg.baseScore;
                existing.matchScore   = cfg.matchScore;
                existing.comboBonus   = cfg.comboBonus;
                existing.wrongPenalty = cfg.wrong;
                existing.hintCooldown = cfg.hintCd;
                existing.maxHints     = cfg.maxHints;
                EditorUtility.SetDirty(existing);
                levelDataList.Add(existing);
            }
            else
            {
                LevelData data = ScriptableObject.CreateInstance<LevelData>();
                data.levelId      = cfg.id;
                data.levelName    = cfg.name;
                data.targetMode   = mode;
                data.rows         = cfg.rows;
                data.columns      = cfg.cols;
                data.timeLimit    = cfg.time;
                data.baseScore    = cfg.baseScore;
                data.matchScore   = cfg.matchScore;
                data.comboBonus   = cfg.comboBonus;
                data.wrongPenalty = cfg.wrong;
                data.hintCooldown = cfg.hintCd;
                data.maxHints     = cfg.maxHints;
                AssetDatabase.CreateAsset(data, path);
                levelDataList.Add(data);
            }
        }
        AssetDatabase.SaveAssets();
        return levelDataList;
    }

    private static void CreateOrUpdateModeConfig(GameModeType type, string modeName, string desc, Color color, bool allUnlocked, bool alwaysUnlocked, List<LevelData> levels)
    {
        string path = SO_PATH_MODES + $"Mode_{modeName}.asset";
        GameModeConfig existing = AssetDatabase.LoadAssetAtPath<GameModeConfig>(path);
        if (existing != null)
        {
            existing.modeType = type;
            existing.modeName = modeName;
            existing.description = desc;
            existing.modeColor = color;
            existing.allLevelsUnlocked = allUnlocked;
            existing.alwaysUnlocked = alwaysUnlocked;
            existing.levels = levels;
            EditorUtility.SetDirty(existing);
        }
        else
        {
            GameModeConfig config = ScriptableObject.CreateInstance<GameModeConfig>();
            config.modeType = type;
            config.modeName = modeName;
            config.description = desc;
            config.modeColor = color;
            config.allLevelsUnlocked = allUnlocked;
            config.alwaysUnlocked = alwaysUnlocked;
            config.levels = levels;
            AssetDatabase.CreateAsset(config, path);
        }
        AssetDatabase.SaveAssets();
    }

    // ==================== Mode Select Scene Builder ====================

    private static void CreateModeSelectScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();

        // LevelManager
        GameObject lmGO = new GameObject("LevelManager");
        LevelManager lm = lmGO.AddComponent<LevelManager>();
        AssignAllModesToLevelManager(lm);

        // AudioManager
        GameObject audioGO = new GameObject("AudioManager");
        audioGO.AddComponent<AudioManager>();

        // Canvas
        GameObject canvasGO = CreateCanvas("Canvas");

        // Background
        GameObject bg = CreateImage(canvasGO, "Background", ColorBg, true);
        SetRectFull(bg.GetComponent<RectTransform>());

        // Top Strip
        GameObject topStrip = CreatePanel(canvasGO, "TopStrip", ColorPrimary);
        RectTransform tsRect = topStrip.GetComponent<RectTransform>();
        tsRect.anchorMin = new Vector2(0, 1);
        tsRect.anchorMax = new Vector2(1, 1);
        tsRect.pivot = new Vector2(0.5f, 1f);
        tsRect.offsetMin = new Vector2(0, -140f);
        tsRect.offsetMax = Vector2.zero;

        // Back Button
        GameObject backBtn = CreateButton(topStrip, "BackButton", "← Back", 
            new Color(0.15f, 0.25f, 0.50f), 36f);
        RectTransform backRect = backBtn.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 0);
        backRect.anchorMax = new Vector2(0, 1);
        backRect.pivot = new Vector2(0, 0.5f);
        backRect.offsetMin = new Vector2(20f, 10f);
        backRect.offsetMax = new Vector2(200f, -10f);

        // Header Text
        GameObject headerTxt = CreateTMPText(topStrip, "HeaderText",
            "SELECT MODE", 52f, Color.white, FontStyles.Bold);
        RectTransform headerRect = headerTxt.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 0);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = new Vector2(180f, 0);
        headerRect.offsetMax = new Vector2(-20f, 0);
        headerTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Info Text
        GameObject infoTextGO = CreateTMPText(canvasGO, "InfoText",
            "Choose your difficulty level", 34f, new Color(0.75f, 0.85f, 1.00f));
        RectTransform infoRect = infoTextGO.GetComponent<RectTransform>();
        infoRect.anchoredPosition = new Vector2(0, 480f);
        infoRect.sizeDelta = new Vector2(900f, 80f);

        // Buttons Container
        GameObject container = new GameObject("ButtonsContainer");
        container.transform.SetParent(canvasGO.transform, false);
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 35f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchoredPosition = new Vector2(0, -100f);
        containerRect.sizeDelta = new Vector2(650f, 900f);

        // Colors
        Color cEasy   = new Color(0.20f, 0.75f, 0.40f);
        Color cNormal = new Color(0.25f, 0.55f, 0.95f);
        Color cHard   = new Color(0.85f, 0.45f, 0.15f);
        Color cExpert = new Color(0.70f, 0.15f, 0.80f);

        // Create buttons
        GameObject btnEasy   = CreateModeButton(container, "EasyButton", "EASY", cEasy);
        GameObject btnNormal = CreateModeButton(container, "NormalButton", "NORMAL", cNormal);
        GameObject btnHard   = CreateModeButton(container, "HardButton", "HARD", cHard);
        GameObject btnExpert = CreateModeButton(container, "ExpertButton", "EXPERT", cExpert);

        // Lock Overlays
        GameObject hardLock   = CreateLockOverlay(btnHard);
        GameObject expertLock = CreateLockOverlay(btnExpert);

        // Mode Info Warning Text
        GameObject warningGO = CreateTMPText(canvasGO, "WarningText",
            "", 32f, ColorAccent);
        RectTransform warningRect = warningGO.GetComponent<RectTransform>();
        warningRect.anchoredPosition = new Vector2(0, -600f);
        warningRect.sizeDelta = new Vector2(900f, 100f);

        // UI Script Controller
        GameObject uiControllerGO = new GameObject("ModeSelectUI");
        uiControllerGO.transform.SetParent(canvasGO.transform, false);
        ModeSelectUI modeUI = uiControllerGO.AddComponent<ModeSelectUI>();

        SerializedObject so = new SerializedObject(modeUI);
        so.FindProperty("easyButton").objectReferenceValue   = btnEasy.GetComponent<Button>();
        so.FindProperty("normalButton").objectReferenceValue = btnNormal.GetComponent<Button>();
        so.FindProperty("hardButton").objectReferenceValue   = btnHard.GetComponent<Button>();
        so.FindProperty("expertButton").objectReferenceValue = btnExpert.GetComponent<Button>();

        so.FindProperty("easyLabel").objectReferenceValue    = btnEasy.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("normalLabel").objectReferenceValue  = btnNormal.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("hardLabel").objectReferenceValue    = btnHard.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("expertLabel").objectReferenceValue  = btnExpert.GetComponentInChildren<TextMeshProUGUI>();

        so.FindProperty("hardLockOverlay").objectReferenceValue   = hardLock;
        so.FindProperty("expertLockOverlay").objectReferenceValue = expertLock;

        so.FindProperty("modeInfoText").objectReferenceValue = warningGO.GetComponent<TextMeshProUGUI>();
        so.FindProperty("backButton").objectReferenceValue   = backBtn.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/ModeSelectScene.unity");
        Debug.Log("[Setup] ✅ ModeSelectScene created.");
    }

    private static GameObject CreateModeButton(GameObject parent, string name, string label, Color color)
    {
        GameObject go = CreateButton(parent, name, label, color, 42f);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 160f;
        return go;
    }

    private static GameObject CreateLockOverlay(GameObject buttonObj)
    {
        GameObject lockOverlay = CreatePanel(buttonObj, "LockOverlay", new Color(0.1f, 0.1f, 0.15f, 0.85f));
        SetRectFull(lockOverlay.GetComponent<RectTransform>());

        GameObject lockText = CreateTMPText(lockOverlay, "LockText", "🔒 LOCKED", 36f, Color.white, FontStyles.Bold);
        SetRectFull(lockText.GetComponent<RectTransform>());

        lockOverlay.SetActive(false);
        return lockOverlay;
    }

    // ==================== UI Factory Helpers ====================

    private static GameObject CreateCanvas(string name)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(REF_WIDTH, REF_HEIGHT);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static void CreateCamera()
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = ColorBg;
        cam.orthographic = true;
        cam.depth = -1;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = new Vector3(0, 0, -10);
    }

    private static void CreateEventSystem()
    {
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    private static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject CreateImage(GameObject parent, string name, Color color, bool raycastTarget = false)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = raycastTarget;
        return go;
    }

    private static GameObject CreateTMPText(GameObject parent, string name, string text,
        float fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        return go;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label,
        Color bgColor, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f));
        cb.pressedColor = new Color(
            Mathf.Max(bgColor.r - 0.15f, 0f),
            Mathf.Max(bgColor.g - 0.15f, 0f),
            Mathf.Max(bgColor.b - 0.15f, 0f));
        btn.colors = cb;

        // Label
        GameObject txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        TextMeshProUGUI tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
        RectTransform labelRect = txtGO.GetComponent<RectTransform>();
        SetRectFull(labelRect);

        return go;
    }

    private static Toggle CreateToggleRow(GameObject parent, string name, string label, out GameObject rowGO)
    {
        rowGO = new GameObject(name);
        rowGO.transform.SetParent(parent.transform, false);
        HorizontalLayoutGroup hlg = rowGO.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 25f; hlg.childForceExpandWidth = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        LayoutElement le = rowGO.AddComponent<LayoutElement>();
        le.preferredHeight = 65f; le.flexibleWidth = 1;

        // Label
        GameObject labelGO = CreateTMPText(rowGO, "Label", label, 36f, ColorText);
        labelGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        LayoutElement lLe = labelGO.AddComponent<LayoutElement>();
        lLe.preferredWidth = 340f;

        // Toggle
        GameObject togGO = new GameObject("Toggle");
        togGO.transform.SetParent(rowGO.transform, false);
        Toggle tog = togGO.AddComponent<Toggle>();
        Image bgImg = togGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.3f);
        LayoutElement togLE = togGO.AddComponent<LayoutElement>();
        togLE.preferredWidth = 70f; togLE.preferredHeight = 42f;

        GameObject checkGO = new GameObject("Checkmark");
        checkGO.transform.SetParent(togGO.transform, false);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = ColorGreen;
        RectTransform checkRect = checkGO.GetComponent<RectTransform>();
        SetRectFull(checkRect);
        checkRect.sizeDelta = new Vector2(-8f, -8f);

        tog.graphic = checkImg;
        tog.targetGraphic = bgImg;

        return tog;
    }

    private static GameObject CreateScrollView(GameObject parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        ScrollRect scrollRect = go.AddComponent<ScrollRect>();
        Image bgImg = go.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(go.transform, false);
        RectTransform vpRect = viewport.GetComponent<RectTransform>();
        if (vpRect == null) vpRect = viewport.AddComponent<RectTransform>();
        SetRectFull(vpRect);
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0.01f); // near-invisible for mask
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        if (contentRect == null) contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = contentRect.offsetMax = Vector2.zero;

        // Grid Layout Group on Content
        GridLayoutGroup glg = content.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(200f, 200f);
        glg.spacing = new Vector2(20f, 20f);
        glg.padding = new RectOffset(30, 30, 30, 30);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.constraintCount = 4;
        glg.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Link ScrollRect
        scrollRect.content  = contentRect;
        scrollRect.viewport = vpRect;
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 30f;

        return go;
    }

    private static GameObject CreateModalPanel(GameObject parent, string name, Color bgColor, string title, Color titleColor)
    {
        // Full-screen overlay
        GameObject overlay = CreatePanel(parent, name, new Color(0, 0, 0, 0.7f));
        SetRectFull(overlay.GetComponent<RectTransform>());

        // Content panel
        GameObject panel = CreatePanel(overlay, "PanelContent", bgColor);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.05f, 0.22f);
        pr.anchorMax = new Vector2(0.95f, 0.78f);
        pr.offsetMin = pr.offsetMax = Vector2.zero;

        // Title
        GameObject titleGO = CreateTMPText(panel, "PanelTitle", title, 64f, titleColor, FontStyles.Bold);
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.80f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

        // Divider
        GameObject divider = CreatePanel(panel, "Divider", new Color(titleColor.r, titleColor.g, titleColor.b, 0.3f));
        RectTransform divRect = divider.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.05f, 0.79f);
        divRect.anchorMax = new Vector2(0.95f, 0.80f);
        divRect.offsetMin = divRect.offsetMax = Vector2.zero;

        // Vertical layout for content
        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 20f;
        vlg.padding = new RectOffset(40, 40, 220, 30);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        return overlay;
    }

    private static GameObject CreateStatCell(GameObject parent, string name,
        string labelStr, string valueStr, Color color,
        out TextMeshProUGUI labelTMP, out TextMeshProUGUI valueTMP)
    {
        GameObject cell = new GameObject(name);
        cell.transform.SetParent(parent.transform, false);

        VerticalLayoutGroup vlg = cell.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 5f;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // Label
        GameObject labelGO = CreateTMPText(cell, "Label", labelStr, 24f, new Color(0.7f, 0.8f, 1f));
        labelTMP = labelGO.GetComponent<TextMeshProUGUI>();
        LayoutElement lLE = labelGO.AddComponent<LayoutElement>();
        lLE.preferredHeight = 35f;

        // Value
        GameObject valueGO = CreateTMPText(cell, "Value", valueStr, 42f, color, FontStyles.Bold);
        valueTMP = valueGO.GetComponent<TextMeshProUGUI>();
        LayoutElement vLE = valueGO.AddComponent<LayoutElement>();
        vLE.preferredHeight = 60f;

        return cell;
    }

    private static void SetRectFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
#endif
