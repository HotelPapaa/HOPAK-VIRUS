using UnityEngine;

public enum h980220_ProjectileKind
{
    Virus,
    Cure
}

public interface h980220_IVirusHitReceiver
{
    void ReceiveVirusHit();
}

public sealed class h980220_Projectile : MonoBehaviour
{
    private Vector3 startPosition;

    public h980220_ProjectileKind Kind { get; private set; }
    public Vector3 Direction { get; private set; }
    public float Speed { get; private set; }
    public float MaximumRange { get; private set; }
    public bool IsExpired { get; private set; }

    public void Initialize(h980220_ProjectileKind kind, Vector3 direction, float speed, float range)
    {
        Kind = kind;
        Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
        Speed = Mathf.Max(0f, speed);
        MaximumRange = Mathf.Max(0f, range);
        startPosition = transform.position;
        IsExpired = false;

        if (MaximumRange <= 0f)
            Expire();
    }

    private void Update()
    {
        Tick(Time.deltaTime);
    }

    internal void Tick(float deltaTime)
    {
        if (IsExpired || deltaTime <= 0f)
            return;

        float travelled = Vector3.Distance(startPosition, transform.position);
        float remaining = Mathf.Max(0f, MaximumRange - travelled);
        float distance = Mathf.Min(Speed * deltaTime, remaining);
        transform.position += Direction * distance;

        if (Vector3.Distance(startPosition, transform.position) >= MaximumRange)
            Expire();
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    internal void HandleCollision(Collider other)
    {
        if (IsExpired || other == null)
            return;

        if (Kind == h980220_ProjectileKind.Virus)
        {
            foreach (MonoBehaviour behaviour in other.GetComponentsInParent<MonoBehaviour>(true))
            {
                if (behaviour is h980220_IVirusHitReceiver receiver)
                {
                    receiver.ReceiveVirusHit();
                    Expire();
                    return;
                }
            }

            Expire();
            return;
        }

        h980220_PlayerInfection infection = other.GetComponentInParent<h980220_PlayerInfection>();
        if (infection != null)
            infection.TryReceiveCure(transform.position);

        Expire();
    }

    private void Expire()
    {
        if (IsExpired)
            return;

        IsExpired = true;
        gameObject.SetActive(false);
        if (Application.isPlaying)
            Destroy(gameObject);
    }
}
