using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum h980220_GameState
{
    Title,
    Playing,
    Won,
    Cured
}

public sealed class h980220_GameManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Text resultText;
    [SerializeField] private Text roomText;

    [Header("Player")]
    [SerializeField] private h980220_PlayerRhythmController playerRhythmController;
    [SerializeField] private h980220_PlayerCombat playerCombat;
    [SerializeField] private h980220_PlayerInfection playerInfection;
    [SerializeField] private h980220_FollowCamera followCamera;

    [Header("Rooms")]
    [SerializeField] private h980220_RoomController[] rooms = Array.Empty<h980220_RoomController>();

    [Header("Enemy Spawning")]
    [SerializeField] private h980220_EnemySpawnSettings enemySpawning =
        new h980220_EnemySpawnSettings();

    private int currentRoomIndex = -1;
    private readonly HashSet<int> completedRooms = new HashSet<int>();
    private h980220_EndlessWorldController endlessWorld;
    private h980220_LevelUpSystem levelUpSystem;
    private Font koreanFont;
    private GameObject stageNoticePanel;
    private Text stageNoticeText;
    private float stageNoticeUntil;

    public h980220_GameState State { get; private set; } = h980220_GameState.Title;

    internal Action<int> SceneLoader { get; set; } =
        buildIndex => SceneManager.LoadScene(buildIndex);

    internal void Awake()
    {
        koreanFont = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Arial" }, 28);
        endlessWorld = GetComponent<h980220_EndlessWorldController>();
        if (endlessWorld == null)
            endlessWorld = gameObject.AddComponent<h980220_EndlessWorldController>();
        endlessWorld.ConfigureSpawning(enemySpawning);
        endlessWorld.Initialize(
            playerRhythmController == null ? null : playerRhythmController.transform,
            rooms);
        endlessWorld.Survived += Win;
        endlessWorld.StageTwoStarted += HandleStageTwoStarted;
        levelUpSystem = GetComponent<h980220_LevelUpSystem>();
        if (levelUpSystem == null)
            levelUpSystem = gameObject.AddComponent<h980220_LevelUpSystem>();
        levelUpSystem.Initialize(endlessWorld, playerRhythmController, playerCombat,
            playerInfection, hudPanel == null ? null : hudPanel.transform);
        State = h980220_GameState.Title;
        currentRoomIndex = -1;
        completedRooms.Clear();

        if (playerInfection != null)
            playerInfection.Cured += Lose;

        foreach (h980220_RoomController room in rooms)
        {
            if (room == null)
                continue;

            room.Completed += HandleRoomCompleted;
            room.SetCombatEnabled(false);
        }

        SetPlayerInput(false);
        SetPlayerCureEnabled(false);
        SetActive(titlePanel, true);
        SetActive(hudPanel, false);
        SetActive(resultPanel, false);
        ApplyKoreanFont(titlePanel);
        ApplyKoreanFont(hudPanel);
        ApplyKoreanFont(resultPanel);
        ConfigureHudAppearance();
        RefreshControlGuide();
        RefreshResultGuide();
        BuildStageNotice();
        BuildResultButtons();
    }

    private void RefreshControlGuide()
    {
        if (titlePanel == null)
            return;

        foreach (Text label in titlePanel.GetComponentsInChildren<Text>(true))
        {
            if (label.text != null && label.text.Contains("HOPAK VIRUS"))
            {
                label.fontSize = 24;
                label.text = "HOPAK VIRUS\n\n" +
                             "승리 조건: 각 스테이지에서 2분 동안 살아남으십시오.\n\n" +
                             "A / D: 호팍 스텝    ← / →: 방향 전환\n" +
                             "접촉: 시민과 메딕 감염\n" +
                             "S: 대시 (강화 필요) / 경찰 처치\n" +
                             "SPACE: 점프 (강화 필요)\n\n" +
                             "시민: 플레이어에게서 도망칩니다\n" +
                             "메딕: 치료제를 발사합니다\n" +
                             "경찰: 접촉 시 체력이 1칸 감소합니다.\n\n" +
                             "ENTER: 게임 시작";
                break;
            }
        }
    }

    private void RefreshResultGuide()
    {
        if (resultPanel == null)
            return;
        foreach (Text label in resultPanel.GetComponentsInChildren<Text>(true))
        {
            if (label != resultText && label.text != null && label.text.Contains("R:"))
                label.text = "R: 다시 시작  |  ESC: 종료";
        }
    }

    private void ApplyKoreanFont(GameObject panel)
    {
        if (panel == null || koreanFont == null)
            return;
        foreach (Text label in panel.GetComponentsInChildren<Text>(true))
            label.font = koreanFont;
    }

    private void ConfigureHudAppearance()
    {
        if (roomText == null)
            return;

        roomText.color = new Color(1f, 0.86f, 0.12f, 1f);
        roomText.fontStyle = FontStyle.Bold;
        roomText.fontSize = 26;
        roomText.alignment = TextAnchor.UpperRight;
        roomText.horizontalOverflow = HorizontalWrapMode.Overflow;
        roomText.verticalOverflow = VerticalWrapMode.Truncate;
        roomText.raycastTarget = false;
        RectTransform rect = roomText.rectTransform;
        rect.anchoredPosition = new Vector2(-24f, -28f);
        rect.sizeDelta = new Vector2(620f, 48f);
        AddDarkOutline(roomText.gameObject);
    }

    private static void AddDarkOutline(GameObject target)
    {
        if (target == null)
            return;
        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.AddComponent<Outline>();
        outline.effectColor = new Color(0.025f, 0.035f, 0.09f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private void Update()
    {
        if (State == h980220_GameState.Playing && roomText != null && endlessWorld != null)
        {
            int remainingSeconds = Mathf.CeilToInt(endlessWorld.RemainingTime);
            roomText.text = $"스테이지 {endlessWorld.CurrentStage}/2  |  " +
                            $"생존 {remainingSeconds / 60:00}:{remainingSeconds % 60:00}";
        }

        if (stageNoticePanel != null && stageNoticePanel.activeSelf &&
            Time.unscaledTime >= stageNoticeUntil)
            stageNoticePanel.SetActive(false);

        if ((State == h980220_GameState.Won || State == h980220_GameState.Cured) &&
            Input.GetKeyDown(KeyCode.Escape))
            QuitGame();

        ProcessInput(
            Input.GetKeyDown(KeyCode.Return),
            Input.GetKeyDown(KeyCode.R));
    }

    internal void ProcessInput(bool startPressed, bool restartPressed)
    {
        if (State == h980220_GameState.Title && startPressed)
            StartGame();

        if ((State == h980220_GameState.Won || State == h980220_GameState.Cured) &&
            restartPressed)
        {
            SceneLoader?.Invoke(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void StartGame()
    {
        if (State != h980220_GameState.Title)
            return;

        State = h980220_GameState.Playing;
        SetActive(titlePanel, false);
        SetActive(hudPanel, true);
        SetActive(resultPanel, false);
        SetPlayerInput(true);
        SetPlayerCureEnabled(true);
        if (endlessWorld != null)
            endlessWorld.SetSimulationEnabled(true);
        if (roomText != null)
            roomText.text = "스테이지 1/2  |  생존 02:00";
    }

    public void SetCurrentRoom(int index)
    {
        if (State != h980220_GameState.Playing || index < 0 || index >= rooms.Length)
            return;

        currentRoomIndex = index;
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i] != null)
                rooms[i].SetCombatEnabled(i == currentRoomIndex);
        }

        if (roomText != null)
            roomText.text = $"구역 {currentRoomIndex + 1}/{rooms.Length}";
    }

    public void Lose()
    {
        if (State != h980220_GameState.Playing)
            return;

        State = h980220_GameState.Cured;
        Finish("치료되었습니다...");
    }

    private void Win()
    {
        if (State != h980220_GameState.Playing)
            return;

        State = h980220_GameState.Won;
        if (followCamera != null)
        {
            if (endlessWorld != null)
                followCamera.SetVictoryView(endlessWorld.ArenaCenter, endlessWorld.ArenaSize);
            else
                followCamera.SetVictoryView();
        }
        Finish("축하합니다! 보균에 성공했습니다!");
    }

    private void HandleStageTwoStarted()
    {
        if (State != h980220_GameState.Playing || stageNoticePanel == null)
            return;
        stageNoticeText.text = "STAGE 2\n실린더 장애물이 나타났습니다!\n적들의 속도가 증가합니다!";
        stageNoticePanel.SetActive(true);
        stageNoticePanel.transform.SetAsLastSibling();
        stageNoticeUntil = Time.unscaledTime + 3f;
    }

    private void HandleRoomCompleted(int index)
    {
        if (State != h980220_GameState.Playing || index < 0 || index >= rooms.Length ||
            !completedRooms.Add(index))
            return;

        AdvanceThroughCompletedRooms();
    }

    private void AdvanceThroughCompletedRooms()
    {
        while (State == h980220_GameState.Playing &&
               completedRooms.Contains(currentRoomIndex))
        {
            if (currentRoomIndex == rooms.Length - 1)
            {
                Win();
                return;
            }

            SetCurrentRoom(currentRoomIndex + 1);
        }
    }

    private void Finish(string message)
    {
        SetPlayerInput(false);
        SetPlayerCureEnabled(false);
        if (endlessWorld != null)
            endlessWorld.SetSimulationEnabled(false);
        foreach (h980220_RoomController room in rooms)
        {
            if (room != null)
                room.SetCombatEnabled(false);
        }

        SetActive(hudPanel, false);
        SetActive(stageNoticePanel, false);
        SetActive(resultPanel, true);
        if (resultText != null)
        {
            resultText.text = message;
            if (State == h980220_GameState.Won)
            {
                resultText.color = new Color(1f, 0.86f, 0.12f, 1f);
                resultText.fontStyle = FontStyle.Bold;
                AddDarkOutline(resultText.gameObject);
            }
        }
    }

    private void SetPlayerInput(bool enabled)
    {
        if (playerRhythmController != null)
            playerRhythmController.SetInputEnabled(enabled);
        if (playerCombat != null)
            playerCombat.SetInputEnabled(enabled);
    }

    private void SetPlayerCureEnabled(bool enabled)
    {
        if (playerInfection != null)
            playerInfection.SetCureEnabled(enabled);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneLoader?.Invoke(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        Type editorApplication = Type.GetType("UnityEditor.EditorApplication,UnityEditor");
        editorApplication?.GetProperty("isPlaying")?.SetValue(null, false);
#else
        Application.Quit();
#endif
    }

    private void BuildStageNotice()
    {
        Canvas canvas = hudPanel == null ? FindFirstObjectByType<Canvas>() :
            hudPanel.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        stageNoticePanel = new GameObject("h980220_StageNotice",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        stageNoticePanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = stageNoticePanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 150f);
        panelRect.sizeDelta = new Vector2(650f, 180f);
        Image background = stageNoticePanel.GetComponent<Image>();
        background.color = new Color(0.12f, 0.02f, 0.2f, 0.94f);
        background.raycastTarget = false;
        stageNoticeText = CreateRuntimeText(stageNoticePanel.transform,
            "STAGE 2", 34, Vector2.zero, new Vector2(620f, 160f));
        stageNoticePanel.SetActive(false);
    }

    private void BuildResultButtons()
    {
        if (resultPanel == null)
            return;
        EnsureEventSystem();
        CreateResultButton("다시 시작", new Vector2(-120f, -110f), RestartGame);
        CreateResultButton("종료", new Vector2(120f, -110f), QuitGame);
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        var eventSystemObject = new GameObject("h980220_EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private void CreateResultButton(string buttonLabel, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject($"h980220_{buttonLabel}",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(resultPanel.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(210f, 64f);
        buttonObject.GetComponent<Image>().color = new Color(0.35f, 0.08f, 0.5f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(action);
        CreateRuntimeText(buttonObject.transform, buttonLabel, 26,
            Vector2.zero, new Vector2(200f, 58f));
    }

    private Text CreateRuntimeText(Transform parent, string value, int fontSize,
        Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject("Text", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = koreanFont;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private void OnDestroy()
    {
        if (playerInfection != null)
            playerInfection.Cured -= Lose;
        if (endlessWorld != null)
        {
            endlessWorld.Survived -= Win;
            endlessWorld.StageTwoStarted -= HandleStageTwoStarted;
        }
        if (levelUpSystem != null)
            levelUpSystem.Shutdown();

        foreach (h980220_RoomController room in rooms)
        {
            if (room != null)
                room.Completed -= HandleRoomCompleted;
        }
    }

    private void OnValidate()
    {
        if (enemySpawning == null)
            enemySpawning = new h980220_EnemySpawnSettings();
        enemySpawning.Sanitize();
    }
}
