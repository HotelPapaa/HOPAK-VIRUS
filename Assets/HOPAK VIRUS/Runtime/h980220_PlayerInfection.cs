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
    private h980220_PlayerCombat playerCombat;

    public event Action Cured;

    public int RemainingInfection { get; private set; }
    public int MaximumInfection => maxInfection;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCombat = GetComponent<h980220_PlayerCombat>();
        ResetInfection();
    }

    public bool TryReceiveCure(Vector3 sourcePosition)
    {
        return ReceiveCureAtTime(sourcePosition, Time.time);
    }

    public bool ReceiveCureAtTime(Vector3 sourcePosition, float now)
    {
        if (playerCombat == null)
            playerCombat = GetComponent<h980220_PlayerCombat>();
        if (!cureEnabled || RemainingInfection <= 0 || now < invulnerableUntil ||
            (playerCombat != null && playerCombat.IsDashing))
            return false;

        invulnerableUntil = now + invulnerabilitySeconds;
        if (TrySacrificeJunior())
            return true;

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

    public void ReceiveFatalContact()
    {
        if (playerCombat == null)
            playerCombat = GetComponent<h980220_PlayerCombat>();
        if (!cureEnabled || RemainingInfection <= 0 ||
            (playerCombat != null && playerCombat.IsDashing))
            return;

        if (TrySacrificeJunior())
        {
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            return;
        }

        RemainingInfection = 0;
        RefreshVisuals();
        Cured?.Invoke();
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

    public void HealOne()
    {
        RemainingInfection = Mathf.Min(maxInfection, RemainingInfection + 1);
        RefreshVisuals();
    }

    public void IncreaseMaximumInfection()
    {
        maxInfection++;
        AddHudIndicator();
        RefreshVisuals();
    }

    private bool TrySacrificeJunior()
    {
        h980220_HopakJunior[] juniors =
            GetComponentsInChildren<h980220_HopakJunior>(true);
        for (int i = juniors.Length - 1; i >= 0; i--)
        {
            h980220_HopakJunior junior = juniors[i];
            if (junior == null || !junior.gameObject.activeInHierarchy)
                continue;
            junior.gameObject.SetActive(false);
            Destroy(junior.gameObject);
            return true;
        }
        return false;
    }

    private void AddHudIndicator()
    {
        if (hudIndicators == null || hudIndicators.Length == 0 ||
            hudIndicators[hudIndicators.Length - 1] == null)
            return;

        Image source = hudIndicators[hudIndicators.Length - 1];
        Image added = Instantiate(source, source.transform.parent);
        added.name = $"Infection {maxInfection}";
        RectTransform addedRect = added.rectTransform;
        if (hudIndicators.Length >= 2 && hudIndicators[hudIndicators.Length - 2] != null)
        {
            Vector2 spacing = source.rectTransform.anchoredPosition -
                              hudIndicators[hudIndicators.Length - 2].rectTransform.anchoredPosition;
            addedRect.anchoredPosition = source.rectTransform.anchoredPosition + spacing;
        }
        else
        {
            addedRect.anchoredPosition += new Vector2(36f, 0f);
        }

        Array.Resize(ref hudIndicators, hudIndicators.Length + 1);
        hudIndicators[hudIndicators.Length - 1] = added;
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
