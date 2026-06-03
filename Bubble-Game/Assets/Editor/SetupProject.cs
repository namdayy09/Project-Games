using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Events;

using BubbleShooterPro.Core;
using BubbleShooterPro.Managers;
using BubbleShooterPro.UI;
using BubbleShooterPro.Utils;

namespace BubbleShooterPro.Editor
{
    public class SetupProject : EditorWindow
    {
        [MenuItem("Bubble Shooter/Build Project Scenes & Prefabs")]
        public static void BuildAll()
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Không được chạy Build Project Scenes & Prefabs khi Unity đang Play. Hãy bấm Stop trước.");
                return;
            }
            Debug.Log("[SetupProject] Bắt đầu khởi tạo dự án Bubble Shooter Pro...");

            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Scenes");

            EnsureTag("Bubble");
            EnsureTag("Wall");
            EnsureTag("Ceiling");

            // Layer không bắt buộc. Nếu anh đã tạo Layer Bubble thì tool sẽ tự gán.
            // Nếu chưa tạo Layer Bubble thì game vẫn compile/chạy, chỉ không gán layer đó.

            GameObject bubblePrefab = CreateBubblePrefab();
            GameObject levelButtonPrefab = CreateLevelButtonPrefab();
            CreateEmptyEffectsPrefabs();

            BuildMainMenuScene(levelButtonPrefab);
            BuildGameScene(bubblePrefab);

            SetupBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("=========================================================================");
            Debug.Log("[SetupProject] THÀNH CÔNG! Đã tạo Prefabs, MainMenuScene và GameScene.");
            Debug.Log("[SetupProject] Bây giờ mở Assets/Scenes/MainMenuScene.unity rồi bấm Play.");
            Debug.Log("=========================================================================");
        }

        #region Folder / Tag

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureTag(string tagName)
        {
            SerializedObject tagManager =
                new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty tag = tagsProp.GetArrayElementAtIndex(i);

                if (tag.stringValue == tagName)
                {
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;

            tagManager.ApplyModifiedProperties();

            Debug.Log("[SetupProject] Đã tạo Tag: " + tagName);
        }

        private static bool TagExists(string tagName)
        {
            try
            {
                GameObject temp = new GameObject("TagTest");
                temp.tag = tagName;
                DestroyImmediate(temp);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Prefabs

        private static GameObject CreateBubblePrefab()
        {
            string prefabPath = "Assets/Prefabs/Bubble.prefab";

            GameObject go = new GameObject("Bubble");
            go.transform.localScale = new Vector3(2.2f, 2.2f, 1f);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetDefaultSprite();
            spriteRenderer.color = Color.white;

            PhysicsMaterial2D bubbleMat = new PhysicsMaterial2D("BubbleBouncy");
            bubbleMat.bounciness = 1f;
            bubbleMat.friction = 0f;

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;
            collider.sharedMaterial = bubbleMat;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.useFullKinematicContacts = true;
            rb.sharedMaterial = bubbleMat;

            go.AddComponent<Bubble>();

            if (TagExists("Bubble"))
            {
                go.tag = "Bubble";
            }

            int bubbleLayer = LayerMask.NameToLayer("Bubble");
            if (bubbleLayer != -1)
            {
                go.layer = bubbleLayer;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);

            return prefab;
        }

        private static GameObject CreateLevelButtonPrefab()
        {
            string prefabPath = "Assets/Prefabs/LevelButton.prefab";

            GameObject go = new GameObject("LevelButton", typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(110f, 110f);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.35f, 0.65f, 1f);

            Button button = go.AddComponent<Button>();
            go.AddComponent<ButtonSound>();

            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.15f, 0.35f, 0.65f, 1f);
            colors.highlightedColor = new Color(0.25f, 0.55f, 0.95f, 1f);
            colors.pressedColor = new Color(0.1f, 0.25f, 0.45f, 1f);
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.6f);
            button.colors = colors;

            GameObject textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);

            TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = "1";
            text.fontSize = 36f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);

            return prefab;
        }

        private static void CreateEmptyEffectsPrefabs()
        {
            CreateEmptyPrefabIfMissing("Assets/Prefabs/PopEffect.prefab", "PopEffect");
            CreateEmptyPrefabIfMissing("Assets/Prefabs/PopupText.prefab", "PopupText");
        }

        private static void CreateEmptyPrefabIfMissing(string path, string objectName)
        {
            if (File.Exists(path)) return;

            GameObject go = new GameObject(objectName);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        #endregion

        #region MainMenuScene

        private static void BuildMainMenuScene(GameObject levelButtonPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "MainMenuScene";

            SetupCamera();

            Light light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                DestroyImmediate(light.gameObject);
            }

            GameObject canvasGo = CreateCanvas();
            CreateEventSystem();

            GameObject menuManagerGo = new GameObject("MenuManager");
            MainMenuManager mainMenuManager = menuManagerGo.AddComponent<MainMenuManager>();

            GameObject mainMenuPanel = CreatePanel("MainMenuPanel", canvasGo.transform);
            GameObject levelSelectPanel = CreatePanel("LevelSelectPanel", canvasGo.transform);
            GameObject settingsPanel = CreatePanel("SettingsPanel", canvasGo.transform);
            GameObject leaderboardPanel = CreatePanel("LeaderboardPanel", canvasGo.transform);

            // Main Menu
            CreateText("TitleText", mainMenuPanel.transform, "BUBBLE SHOOTER PRO", new Vector2(0f, 290f), 70f);

            Button playButton = CreateButton("PlayButton", mainMenuPanel.transform, "PLAY GAME", new Vector2(0f, 120f), new Vector2(340f, 70f));
            Button levelSelectButton = CreateButton("LevelSelectButton", mainMenuPanel.transform, "LEVEL SELECT", new Vector2(0f, 25f), new Vector2(340f, 65f));
            Button settingsButton = CreateButton("SettingsButton", mainMenuPanel.transform, "SETTINGS", new Vector2(0f, -65f), new Vector2(340f, 65f));
            Button leaderboardButton = CreateButton("LeaderboardButton", mainMenuPanel.transform, "LEADERBOARD", new Vector2(0f, -155f), new Vector2(340f, 65f));
            Button quitButton = CreateButton("QuitButton", mainMenuPanel.transform, "QUIT", new Vector2(0f, -245f), new Vector2(340f, 65f));

            // Level Select Panel
            CreateText("TitleText", levelSelectPanel.transform, "CHỌN LEVEL", new Vector2(0f, 310f), 60f);

            GameObject levelButtonContainer = new GameObject("LevelButtonContainer", typeof(RectTransform));
            levelButtonContainer.transform.SetParent(levelSelectPanel.transform, false);

            RectTransform levelContainerRect = levelButtonContainer.GetComponent<RectTransform>();
            levelContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            levelContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelContainerRect.anchoredPosition = new Vector2(0f, 30f);
            levelContainerRect.sizeDelta = new Vector2(850f, 350f);

            GridLayoutGroup gridLayout = levelButtonContainer.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(110f, 110f);
            gridLayout.spacing = new Vector2(25f, 25f);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;

            LevelSelectManager levelSelectManager = levelSelectPanel.AddComponent<LevelSelectManager>();
            levelSelectManager.buttonContainer = levelButtonContainer.transform;
            levelSelectManager.buttonPrefab = levelButtonPrefab;

            Button backLevelButton = CreateButton("BackButton", levelSelectPanel.transform, "BACK", new Vector2(0f, -310f), new Vector2(240f, 60f));

            // Settings Panel
            CreateText("TitleText", settingsPanel.transform, "CÀI ĐẶT", new Vector2(0f, 270f), 60f);

            CreateText("BgmLabel", settingsPanel.transform, "BGM", new Vector2(-160f, 120f), 34f);
            Toggle bgmToggle = CreateToggle("BgmToggle", settingsPanel.transform, new Vector2(130f, 120f));

            CreateText("SfxLabel", settingsPanel.transform, "SFX", new Vector2(-160f, 20f), 34f);
            Toggle sfxToggle = CreateToggle("SfxToggle", settingsPanel.transform, new Vector2(130f, 20f));

            Button resetButton = CreateButton("ResetProgressButton", settingsPanel.transform, "RESET PROGRESS", new Vector2(0f, -100f), new Vector2(300f, 60f));
            Button backSettingsButton = CreateButton("BackButton", settingsPanel.transform, "BACK", new Vector2(0f, -220f), new Vector2(240f, 60f));

            SettingsManager settingsManager = settingsPanel.AddComponent<SettingsManager>();
            settingsManager.bgmToggle = bgmToggle;
            settingsManager.sfxToggle = sfxToggle;
            settingsManager.resetProgressButton = resetButton;
            settingsManager.backButton = backSettingsButton;

            // Leaderboard Panel
            CreateText("TitleText", leaderboardPanel.transform, "BẢNG XẾP HẠNG", new Vector2(0f, 310f), 60f);

            TextMeshProUGUI leaderboardText =
                CreateText("LeaderboardText", leaderboardPanel.transform, "Chưa có dữ liệu", new Vector2(0f, 20f), 32f);

            RectTransform leaderboardRect = leaderboardText.GetComponent<RectTransform>();
            leaderboardRect.sizeDelta = new Vector2(850f, 430f);

            Button backLeaderboardButton = CreateButton("BackButton", leaderboardPanel.transform, "BACK", new Vector2(0f, -310f), new Vector2(240f, 60f));

            LeaderboardManager leaderboardManager = leaderboardPanel.AddComponent<LeaderboardManager>();
            leaderboardManager.leaderboardText = leaderboardText;
            leaderboardManager.backButton = backLeaderboardButton;

            // Assign MainMenuManager references
            mainMenuManager.mainMenuPanel = mainMenuPanel;
            mainMenuManager.levelSelectPanel = levelSelectPanel;
            mainMenuManager.settingsPanel = settingsPanel;
            mainMenuManager.leaderboardPanel = leaderboardPanel;
            mainMenuManager.bgmToggle = bgmToggle;
            mainMenuManager.sfxToggle = sfxToggle;
            mainMenuManager.leaderboardText = leaderboardText;

            // Button events
            UnityEventTools.AddPersistentListener(playButton.onClick, mainMenuManager.OnPlayClicked);
            UnityEventTools.AddPersistentListener(levelSelectButton.onClick, mainMenuManager.OnLevelSelectClicked);
            UnityEventTools.AddPersistentListener(settingsButton.onClick, mainMenuManager.OnSettingsClicked);
            UnityEventTools.AddPersistentListener(leaderboardButton.onClick, mainMenuManager.OnLeaderboardClicked);
            UnityEventTools.AddPersistentListener(quitButton.onClick, mainMenuManager.OnQuitClicked);

            UnityEventTools.AddPersistentListener(backLevelButton.onClick, mainMenuManager.OnBackClicked);
            UnityEventTools.AddPersistentListener(backSettingsButton.onClick, mainMenuManager.OnBackClicked);
            UnityEventTools.AddPersistentListener(backLeaderboardButton.onClick, mainMenuManager.OnBackClicked);

            mainMenuPanel.SetActive(true);
            levelSelectPanel.SetActive(false);
            settingsPanel.SetActive(false);
            leaderboardPanel.SetActive(false);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenuScene.unity");
        }

        #endregion

        #region GameScene

        private static void BuildGameScene(GameObject bubblePrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "GameScene";

            SetupCamera();

            Light light = Object.FindFirstObjectByType<Light>();
            if (light != null)
            {
                DestroyImmediate(light.gameObject);
            }

            PhysicsMaterial2D wallMaterial = new PhysicsMaterial2D("WallBouncy");
            wallMaterial.bounciness = 1f;
            wallMaterial.friction = 0f;

            GameObject playField = new GameObject("PlayFieldBackground");
            playField.transform.position = new Vector3(0f, 0f, 1f);
            playField.transform.localScale = new Vector3(7.6f, 8.8f, 1f);

            SpriteRenderer playFieldRenderer = playField.AddComponent<SpriteRenderer>();
            playFieldRenderer.sprite = GetDefaultSprite();
            playFieldRenderer.color = new Color(0.08f, 0.14f, 0.22f, 1f);
            playFieldRenderer.sortingOrder = -10;

            CreateWall("LeftWall", new Vector3(-4.0f, 0f, 0f), new Vector2(0.5f, 12f), "Wall", wallMaterial);
            CreateWall("RightWall", new Vector3(4.0f, 0f, 0f), new Vector2(0.5f, 12f), "Wall", wallMaterial);
            CreateWall("Ceiling", new Vector3(0f, 4.45f, 0f), new Vector2(10f, 0.5f), "Ceiling", wallMaterial);

            // Managers
            GameObject managersGo = new GameObject("Managers");
            managersGo.AddComponent<SaveManager>();
            managersGo.AddComponent<AudioManager>();
            managersGo.AddComponent<ScoreManager>();
            managersGo.AddComponent<LevelManager>();
            managersGo.AddComponent<GameManager>();

            // BubbleGrid
            GameObject gridGo = new GameObject("BubbleGrid");
            BubbleGrid bubbleGrid = gridGo.AddComponent<BubbleGrid>();

            bubbleGrid.bubblePrefab = bubblePrefab;

            // Kích thước lưới
            bubbleGrid.cols = 8;
            bubbleGrid.maxRows = 8;

            // Cho bóng gần nhau hơn, không bị rải rác
            bubbleGrid.bubbleRadius = 0.32f;

            // Đưa lưới lên trên và căn giữa màn hình
            bubbleGrid.gridOrigin = new Vector2(-2.3f, 3.55f);
            //  gridGo.AddComponent<GridTester>();

            // Shooter
            GameObject shooterGo = new GameObject("Shooter");
            shooterGo.transform.position = new Vector3(0f, -3.85f, 0f);

            BubbleLauncher launcher = shooterGo.AddComponent<BubbleLauncher>();
            BubbleShooter shooter = shooterGo.AddComponent<BubbleShooter>();

            launcher.bubblePrefab = bubblePrefab;

            GameObject pivotGo = new GameObject("GunPivot");
            pivotGo.transform.SetParent(shooterGo.transform, false);

            GameObject gunVisual = new GameObject("GunVisual");
            gunVisual.transform.SetParent(pivotGo.transform, false);
            gunVisual.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            gunVisual.transform.localScale = new Vector3(0.18f, 1.0f, 1f);

            SpriteRenderer gunRenderer = gunVisual.AddComponent<SpriteRenderer>();
            gunRenderer.sprite = GetDefaultSprite();
            gunRenderer.color = Color.white;

            shooter.pivotVisual = pivotGo.transform;

            GameObject launchPointGo = new GameObject("LaunchPoint");
            launchPointGo.transform.SetParent(shooterGo.transform, false);
            launchPointGo.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            launcher.launchPoint = launchPointGo.transform;

            GameObject previewPointGo = new GameObject("PreviewPoint");
            previewPointGo.transform.SetParent(shooterGo.transform, false);
            previewPointGo.transform.localPosition = new Vector3(-1.4f, 0f, 0f);
            launcher.previewPoint = previewPointGo.transform;

            GameObject trajectoryGo = new GameObject("Trajectory");
            trajectoryGo.transform.SetParent(shooterGo.transform, false);

            TrajectoryPreview trajectoryPreview = trajectoryGo.AddComponent<TrajectoryPreview>();
            LineRenderer lineRenderer = trajectoryGo.GetComponent<LineRenderer>();

            lineRenderer.startWidth = 0.08f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.positionCount = 0;

            Material lineMat = new Material(Shader.Find("Sprites/Default"));
            lineMat.color = new Color(1f, 1f, 1f, 0.45f);
            lineRenderer.sharedMaterial = lineMat;

            trajectoryPreview.collisionMask = LayerMask.GetMask("Default", "Bubble");
            if (trajectoryPreview.collisionMask == 0)
            {
                trajectoryPreview.collisionMask = LayerMask.GetMask("Default");
            }

            shooter.trajectoryPreview = trajectoryPreview;

            // Canvas Gameplay
            GameObject canvasGo = CreateCanvas();
            UIManager uiManager = canvasGo.AddComponent<UIManager>();
            CreateEventSystem();

            GameObject hudPanel = CreateHudPanel("HUDPanel", canvasGo.transform);
            GameObject pausePanel = CreatePanel("PausePanel", canvasGo.transform);
            GameObject victoryPanel = CreatePanel("VictoryPanel", canvasGo.transform);
            GameObject gameOverPanel = CreatePanel("GameOverPanel", canvasGo.transform);

            // HUD
            uiManager.scoreText = CreateText("ScoreText", hudPanel.transform, "SCORE: 0", new Vector2(-760f, 480f), 32f);
            uiManager.highScoreText = CreateText("HighScoreText", hudPanel.transform, "HIGH: 0", new Vector2(-760f, 430f), 26f);
            uiManager.levelText = CreateText("LevelText", hudPanel.transform, "LEVEL: 1", new Vector2(0f, 480f), 36f);
            uiManager.shotsText = CreateText("ShotsText", hudPanel.transform, "BALLS: 25", new Vector2(720f, 480f), 32f);

            GameObject comboGroup = new GameObject("ComboGroup", typeof(RectTransform));
            comboGroup.transform.SetParent(hudPanel.transform, false);
            uiManager.comboText = CreateText("ComboText", comboGroup.transform, "COMBO x2", new Vector2(0f, 420f), 30f);
            uiManager.comboGroup = comboGroup;

            Button pauseButton = CreateButton("PauseButton", hudPanel.transform, "PAUSE", new Vector2(880f, 480f), new Vector2(130f, 52f));

            // Pause Panel
            CreateText("TitleText", pausePanel.transform, "TẠM DỪNG", new Vector2(0f, 210f), 64f);
            uiManager.resumeButton = CreateButton("ResumeButton", pausePanel.transform, "RESUME", new Vector2(0f, 70f), new Vector2(280f, 65f));
            uiManager.pauseRestartButton = CreateButton("RestartButton", pausePanel.transform, "RESTART", new Vector2(0f, -30f), new Vector2(280f, 65f));
            uiManager.pauseMenuButton = CreateButton("MainMenuButton", pausePanel.transform, "MAIN MENU", new Vector2(0f, -130f), new Vector2(280f, 65f));
            uiManager.pausePanel = pausePanel;

            // Victory Panel
            CreateText("TitleText", victoryPanel.transform, "CHIẾN THẮNG!", new Vector2(0f, 260f), 64f);
            uiManager.victoryScoreText = CreateText("FinalScoreText", victoryPanel.transform, "Score: 0", new Vector2(0f, 150f), 36f);
            uiManager.victoryHighScoreText = CreateText("HighScoreText", victoryPanel.transform, "High: 0", new Vector2(0f, 90f), 30f);
            uiManager.victoryStarsText = CreateText("StarText", victoryPanel.transform, "Stars: 3", new Vector2(0f, 35f), 30f);
            uiManager.nextLevelButton = CreateButton("NextLevelButton", victoryPanel.transform, "NEXT LEVEL", new Vector2(0f, -70f), new Vector2(280f, 65f));
            uiManager.victoryReplayButton = CreateButton("ReplayButton", victoryPanel.transform, "REPLAY", new Vector2(-160f, -170f), new Vector2(180f, 58f));
            uiManager.victoryMenuButton = CreateButton("MainMenuButton", victoryPanel.transform, "MENU", new Vector2(160f, -170f), new Vector2(180f, 58f));
            uiManager.victoryPanel = victoryPanel;

            // GameOver Panel
            CreateText("TitleText", gameOverPanel.transform, "GAME OVER", new Vector2(0f, 220f), 70f);
            uiManager.gameOverScoreText = CreateText("FinalScoreText", gameOverPanel.transform, "Score: 0", new Vector2(0f, 100f), 38f);
            uiManager.gameOverReplayButton = CreateButton("ReplayButton", gameOverPanel.transform, "REPLAY", new Vector2(0f, -20f), new Vector2(280f, 65f));
            uiManager.gameOverMenuButton = CreateButton("MainMenuButton", gameOverPanel.transform, "MAIN MENU", new Vector2(0f, -120f), new Vector2(280f, 65f));
            uiManager.gameOverPanel = gameOverPanel;

            hudPanel.SetActive(true);
            pausePanel.SetActive(false);
            victoryPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            UnityEventTools.AddPersistentListener(pauseButton.onClick, uiManager.PauseGame);

            UnityEventTools.AddPersistentListener(uiManager.resumeButton.onClick, uiManager.ResumeGame);
            UnityEventTools.AddPersistentListener(uiManager.pauseRestartButton.onClick, uiManager.RestartGame);
            UnityEventTools.AddPersistentListener(uiManager.pauseMenuButton.onClick, uiManager.GoToMainMenu);

            UnityEventTools.AddPersistentListener(uiManager.nextLevelButton.onClick, uiManager.NextLevel);
            UnityEventTools.AddPersistentListener(uiManager.victoryReplayButton.onClick, uiManager.RestartGame);
            UnityEventTools.AddPersistentListener(uiManager.victoryMenuButton.onClick, uiManager.GoToMainMenu);

            UnityEventTools.AddPersistentListener(uiManager.gameOverReplayButton.onClick, uiManager.RestartGame);
            UnityEventTools.AddPersistentListener(uiManager.gameOverMenuButton.onClick, uiManager.GoToMainMenu);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        }

        private static void CreateWall(string name, Vector3 position, Vector2 size, string tag, PhysicsMaterial2D material)
        {
            GameObject wall = new GameObject(name);
            wall.transform.position = position;

            if (TagExists(tag))
            {
                wall.tag = tag;
            }

            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.sharedMaterial = material;

            SpriteRenderer sr = wall.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();

            if (tag == "Ceiling")
            {
                sr.color = new Color(0.35f, 0.75f, 1f, 0.85f);
            }
            else
            {
                sr.color = new Color(0.25f, 0.55f, 0.9f, 0.85f);
            }

            wall.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.sortingOrder = -5;
        }

        #endregion

        #region UI Helpers

        private static GameObject CreateCanvas()
        {
            GameObject canvasGo = new GameObject("Canvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            return canvasGo;
        }

        private static void CreateEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private static void SetupCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                GameObject cameraGo = new GameObject("Main Camera");
                camera = cameraGo.AddComponent<Camera>();
                cameraGo.tag = "MainCamera";
            }

            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.04f, 0.08f, 0.14f, 1f);
        }

        

        private static GameObject CreateHudPanel(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.04f, 0.055f, 0.09f, 0.95f);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string value, Vector2 position, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(720f, 80f);

            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);

            Button button = go.AddComponent<Button>();

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            TextMeshProUGUI text = CreateText(name + "_Label", go.transform, label, Vector2.zero, 25f);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static Toggle CreateToggle(string name, Transform parent, Vector2 position)
        {
            GameObject toggleGo = new GameObject(name, typeof(RectTransform));
            toggleGo.transform.SetParent(parent, false);

            Toggle toggle = toggleGo.AddComponent<Toggle>();

            RectTransform toggleRect = toggleGo.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.5f, 0.5f);
            toggleRect.anchorMax = new Vector2(0.5f, 0.5f);
            toggleRect.anchoredPosition = position;
            toggleRect.sizeDelta = new Vector2(48f, 48f);

            GameObject background = new GameObject("Background", typeof(RectTransform));
            background.transform.SetParent(toggleGo.transform, false);

            Image bgImage = background.AddComponent<Image>();
            bgImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(48f, 48f);

            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform));
            checkmark.transform.SetParent(background.transform, false);

            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.85f, 0.35f, 1f);

            RectTransform checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.sizeDelta = new Vector2(32f, 32f);

            toggle.targetGraphic = bgImage;
            toggle.graphic = checkImage;
            toggle.isOn = true;

            return toggle;
        }

        private static Sprite GetDefaultSprite()
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dx = x - 16;
                    float dy = y - 16;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    pixels[y * 32 + x] = distance <= 15 ? Color.white : new Color(1, 1, 1, 0);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
        }

        #endregion

        #region Build Settings

        private static void SetupBuildSettings()
        {
            string mainMenuPath = "Assets/Scenes/MainMenuScene.unity";
            string gameScenePath = "Assets/Scenes/GameScene.unity";

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(mainMenuPath, true),
                new EditorBuildSettingsScene(gameScenePath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();

            // API đúng cho Unity 6.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        #endregion
    }
}