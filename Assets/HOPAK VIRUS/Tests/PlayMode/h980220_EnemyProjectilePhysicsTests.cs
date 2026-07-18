using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class h980220_EnemyProjectilePhysicsTests
{
    [UnityTest]
    public IEnumerator FallbackShotSurvivesShooterAndCuresPlayerThroughPhysics()
    {
        var enemyObject = new GameObject("Task5 PlayMode Ranged Enemy");
        h980220_EnemyController enemy = enemyObject.AddComponent<h980220_EnemyController>();
        enemy.Configure(h980220_EnemyType.Ranged, 1);

        var playerObject = new GameObject("Task5 PlayMode Player");
        playerObject.transform.position = Vector3.forward * 3f;
        playerObject.AddComponent<BoxCollider>();
        h980220_PlayerInfection infection = playerObject.AddComponent<h980220_PlayerInfection>();
        infection.ResetInfection();

        var prefabObject = new GameObject("Task5 PlayMode Cure Prefab");
        prefabObject.transform.position = Vector3.right * 100f;
        h980220_Projectile prefab = prefabObject.AddComponent<h980220_Projectile>();

        SetPrivate(enemy, "player", playerObject.transform);
        SetPrivate(enemy, "cureProjectilePrefab", prefab);
        InvokeTick(enemy, 0f, 0f);
        h980220_Projectile projectile = FindSpawnedProjectile(prefab);
        Assert.That(projectile, Is.Not.Null);
        Assert.That(projectile.transform.position, Is.EqualTo(enemyObject.transform.position));

        prefabObject.SetActive(false);
        enemy.SetCombatEnabled(false);
        yield return new WaitForFixedUpdate();
        yield return null;
        bool survivedShooter = projectile != null && !projectile.IsExpired;

        for (int i = 0; i < 60 && infection.RemainingInfection == 3; i++)
        {
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        int remainingInfection = infection.RemainingInfection;
        Object.Destroy(enemyObject);
        Object.Destroy(playerObject);
        Object.Destroy(prefabObject);
        if (projectile != null)
            Object.Destroy(projectile.gameObject);
        yield return null;

        Assert.That(survivedShooter, Is.True);
        Assert.That(remainingInfection, Is.EqualTo(2));
    }

    private static h980220_Projectile FindSpawnedProjectile(h980220_Projectile prefab)
    {
        foreach (h980220_Projectile projectile in
                 Object.FindObjectsByType<h980220_Projectile>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (projectile != prefab)
                return projectile;
        }

        return null;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"Missing serialized field {fieldName}");
        field.SetValue(target, value);
    }

    private static void InvokeTick(h980220_EnemyController enemy, float now, float deltaTime)
    {
        MethodInfo tick = typeof(h980220_EnemyController).GetMethod(
            "Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(tick, Is.Not.Null, "Missing production Tick seam");
        tick.Invoke(enemy, new object[] { now, deltaTime });
    }
}
