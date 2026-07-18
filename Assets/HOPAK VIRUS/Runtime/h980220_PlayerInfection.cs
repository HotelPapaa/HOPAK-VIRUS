using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class h980220_PlayerInfection : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [SerializeField] private int maxInfection = 3;
    [SerializeField] private float invulnerabilitySeconds = 1f;
    [SerializeField] private float knockbackDistance = 1.5f;

    [Header("Visuals")]
    [SerializeField] private Renderer[] bodyRenderers = Array.Empty<Renderer>();
    [SerializeField] private Color infectedColor = new Color(0.5f, 0f, 0.75f, 1f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Image[] hudIndicators = Array.Empty<Image>();

    private CharacterController characterController;
    private float invulnerableUntil = float.NegativeInfinity;
    private bool cureEnabled = true;

    public event Action Cured;

    public int RemainingInfection { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ResetInfection();
    }

    public bool TryReceiveCure(Vector3 sourcePosition)
    {
        return ReceiveCureAtTime(sourcePosition, Time.time);
    }

    public bool ReceiveCureAtTime(Vector3 sourcePosition, float now)
    {
        if (!cureEnabled || RemainingInfection <= 0 || now < invulnerableUntil)
            return false;

        invulnerableUntil = now + invulnerabilitySeconds;
        RemainingInfection--;

        if (characterController == null)
            characterController = GetComponent<CharacterController>();

        Vector3 away = transform.position - sourcePosition;
        away.y = 0f;
        if (characterController != null && away.sqrMagnitude > 0.001f)
            characterController.Move(away.normalized * knockbackDistance);

        RefreshVisuals();
        if (RemainingInfection == 0)
            Cured?.Invoke();

        return true;
    }

    public void ResetInfection()
    {
        RemainingInfection = maxInfection;
        invulnerableUntil = float.NegativeInfinity;
        RefreshVisuals();
    }

    public void SetCureEnabled(bool enabled)
    {
        cureEnabled = enabled;
    }

    private void RefreshVisuals()
    {
        float ratio = maxInfection <= 0 ? 0f : (float)RemainingInfection / maxInfection;
        Color bodyColor = Color.Lerp(normalColor, infectedColor, ratio);
        var properties = new MaterialPropertyBlock();

        foreach (Renderer bodyRenderer in bodyRenderers)
        {
            if (bodyRenderer == null)
                continue;

            bodyRenderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, bodyColor);
            properties.SetColor(ColorId, bodyColor);
            bodyRenderer.SetPropertyBlock(properties);
            properties.Clear();
        }

        for (int i = 0; i < hudIndicators.Length; i++)
        {
            if (hudIndicators[i] != null)
                hudIndicators[i].gameObject.SetActive(i < RemainingInfection);
        }
    }

    private void OnValidate()
    {
        maxInfection = Mathf.Max(1, maxInfection);
        invulnerabilitySeconds = Mathf.Max(0f, invulnerabilitySeconds);
        knockbackDistance = Mathf.Max(0f, knockbackDistance);
    }
}
