using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int currentRoomIndex = -1;

    public h980220_GameState State { get; private set; } = h980220_GameState.Title;

    internal Action<int> SceneLoader { get; set; } =
        buildIndex => SceneManager.LoadScene(buildIndex);

    internal void Awake()
    {
        State = h980220_GameState.Title;
        currentRoomIndex = -1;

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
        SetActive(titlePanel, true);
        SetActive(hudPanel, false);
        SetActive(resultPanel, false);
    }

    private void Update()
    {
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
        SetCurrentRoom(0);
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
            roomText.text = $"ROOM {currentRoomIndex + 1}/{rooms.Length}";
    }

    public void Lose()
    {
        if (State != h980220_GameState.Playing)
            return;

        State = h980220_GameState.Cured;
        Finish("CURED...");
    }

    private void Win()
    {
        if (State != h980220_GameState.Playing)
            return;

        State = h980220_GameState.Won;
        if (followCamera != null)
            followCamera.SetVictoryView();
        Finish("HOPAK VIRUS SPREAD COMPLETE");
    }

    private void HandleRoomCompleted(int index)
    {
        if (State != h980220_GameState.Playing || index != currentRoomIndex)
            return;

        if (currentRoomIndex == rooms.Length - 1)
            Win();
        else
            SetCurrentRoom(currentRoomIndex + 1);
    }

    private void Finish(string message)
    {
        SetPlayerInput(false);
        foreach (h980220_RoomController room in rooms)
        {
            if (room != null)
                room.SetCombatEnabled(false);
        }

        SetActive(hudPanel, false);
        SetActive(resultPanel, true);
        if (resultText != null)
            resultText.text = message;
    }

    private void SetPlayerInput(bool enabled)
    {
        if (playerRhythmController != null)
            playerRhythmController.SetInputEnabled(enabled);
        if (playerCombat != null)
            playerCombat.SetInputEnabled(enabled);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void OnDestroy()
    {
        if (playerInfection != null)
            playerInfection.Cured -= Lose;

        foreach (h980220_RoomController room in rooms)
        {
            if (room != null)
                room.Completed -= HandleRoomCompleted;
        }
    }
}
