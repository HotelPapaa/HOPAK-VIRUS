using UnityEngine;

public sealed class h980220_PlayerCombat : MonoBehaviour
{
    [SerializeField] private h980220_Projectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float fireCooldown = 0.5f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private float projectileRange = 4f;

    private bool inputEnabled = true;
    private float nextFireTime = float.NegativeInfinity;

    private void Update()
    {
        if (inputEnabled && Input.GetKeyDown(KeyCode.Space))
            Fire();
    }

    internal bool ProcessInputAtTime(bool firePressed, float now)
    {
        return inputEnabled && firePressed && FireAtTime(now);
    }

    public bool Fire()
    {
        return FireAtTime(Time.time);
    }

    internal bool FireAtTime(float now)
    {
        if (!inputEnabled || projectilePrefab == null || now < nextFireTime)
            return false;

        Transform origin = firePoint != null ? firePoint : transform;
        h980220_Projectile projectile = Instantiate(
            projectilePrefab, origin.position, origin.rotation);
        projectile.Initialize(
            h980220_ProjectileKind.Virus,
            origin.forward,
            projectileSpeed,
            projectileRange);
        nextFireTime = now + fireCooldown;
        return true;
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void OnValidate()
    {
        fireCooldown = Mathf.Max(0f, fireCooldown);
        projectileSpeed = Mathf.Max(0f, projectileSpeed);
        projectileRange = Mathf.Max(0f, projectileRange);
    }
}
