using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class h980220_RoomController : MonoBehaviour
{
    private readonly List<h980220_EnemyController> enemies = new List<h980220_EnemyController>();
    private int roomIndex;
    private Transform exitDoor;
    private bool completed;

    public event Action<int> Completed;

    public int RemainingEnemies { get; private set; }

    public void Initialize(
        int index,
        IEnumerable<h980220_EnemyController> roomEnemies,
        Transform roomExitDoor)
    {
        if (completed)
            return;

        UnsubscribeEnemies();
        roomIndex = index;
        exitDoor = roomExitDoor;

        var uniqueEnemies = new HashSet<h980220_EnemyController>();
        if (roomEnemies != null)
        {
            foreach (h980220_EnemyController enemy in roomEnemies)
            {
                if (enemy == null || !uniqueEnemies.Add(enemy))
                    continue;

                enemies.Add(enemy);
                if (!enemy.IsInfected)
                    enemy.Infected += HandleEnemyInfected;
            }
        }

        RemainingEnemies = 0;
        foreach (h980220_EnemyController enemy in enemies)
        {
            if (!enemy.IsInfected)
                RemainingEnemies++;
        }

        TryComplete();
    }

    public void SetCombatEnabled(bool enabled)
    {
        foreach (h980220_EnemyController enemy in enemies)
        {
            if (enemy != null)
                enemy.SetCombatEnabled(enabled);
        }
    }

    private void HandleEnemyInfected(h980220_EnemyController enemy)
    {
        enemy.Infected -= HandleEnemyInfected;
        RemainingEnemies = Mathf.Max(0, RemainingEnemies - 1);
        TryComplete();
    }

    private void TryComplete()
    {
        if (completed || RemainingEnemies != 0)
            return;

        completed = true;
        if (exitDoor != null)
            exitDoor.position += Vector3.up * 4f;

        Completed?.Invoke(roomIndex);
    }

    private void OnDestroy()
    {
        UnsubscribeEnemies();
    }

    private void UnsubscribeEnemies()
    {
        foreach (h980220_EnemyController enemy in enemies)
        {
            if (enemy != null)
                enemy.Infected -= HandleEnemyInfected;
        }

        enemies.Clear();
    }
}
