using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class h980220_PlayModeVirusReceiver : MonoBehaviour, h980220_IVirusHitReceiver
{
    public int HitCount { get; private set; }

    public void ReceiveVirusHit()
    {
        HitCount++;
    }
}

public sealed class h980220_ProjectilePhysicsTests
{
    [UnityTest]
    public IEnumerator MovingProjectileTriggersReceiverAndDestroysItself()
    {
        var target = new GameObject("Task4 PlayMode Virus Target");
        h980220_PlayModeVirusReceiver receiver = target.AddComponent<h980220_PlayModeVirusReceiver>();
        target.AddComponent<BoxCollider>();
        target.transform.position = Vector3.zero;

        var projectileObject = new GameObject("Task4 PlayMode Virus Projectile");
        projectileObject.transform.position = Vector3.back * 2f;
        h980220_Projectile projectile = projectileObject.AddComponent<h980220_Projectile>();
        projectile.Initialize(h980220_ProjectileKind.Virus, Vector3.forward, 0.01f, 100f);

        yield return new WaitForFixedUpdate();
        projectile.transform.position = target.transform.position;
        Physics.SyncTransforms();
        yield return new WaitForFixedUpdate();
        yield return null;

        int hitCount = receiver.HitCount;
        bool projectileDestroyed = projectile == null;
        Object.Destroy(target);
        if (projectile != null)
            Object.Destroy(projectile.gameObject);
        yield return null;

        Assert.That(hitCount, Is.EqualTo(1));
        Assert.That(projectileDestroyed, Is.True);
    }
}
