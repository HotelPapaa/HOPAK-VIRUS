using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class h980220_LevelUpSystem : MonoBehaviour
{
    private enum Upgrade
    {
        DashUnlock,
        JumpUnlock,
        Heal,
        HopakJunior,
        BodyGrowth,
        DashPower,
        JumpPower,
        MaximumInfection
    }

    private static readonly int[] Thresholds = { 10, 22, 36, 52, 70, 90, 112, 136 };

    private h980220_EndlessWorldController world;
    private h980220_PlayerRhythmController rhythm;
    private h980220_PlayerCombat combat;
    private h980220_PlayerInfection infection;
    private GameObject popup;
    private Text infectionText;
    private Image levelGaugeFill;
    private RectTransform levelGaugeFillRect;
    private Text titleText;
    private readonly Button[] choiceButtons = new Button[3];
    private readonly Text[] choiceTexts = new Text[3];
    private readonly Upgrade[] offeredChoices = new Upgrade[3];
    private int infectedCount;
    private int completedLevels;
    private int juniorLevel;
    private int growthLevel;
    private int dashPowerLevel;
    private int jumpPowerLevel;
    private int maximumInfectionLevel;
    private float savedTimeScale = 1f;
    private bool choosing;

    public void Initialize(h980220_EndlessWorldController endlessWorld,
        h980220_PlayerRhythmController playerRhythm,
        h980220_PlayerCombat playerCombat,
        h980220_PlayerInfection playerInfection, Transform hudRoot)
    {
        Shutdown();
        world = endlessWorld;
        rhythm = playerRhythm;
        combat = playerCombat;
        infection = playerInfection;
        if (world != null)
            world.EnemySpawned += RegisterEnemy;
        BuildUi(hudRoot);
        RefreshCounter();
    }

    public void Shutdown()
    {
        if (world != null)
            world.EnemySpawned -= RegisterEnemy;
        if (choosing)
            ResumeGame();
    }

    private void Update()
    {
        RefreshCounter();
        if (!choosing)
            return;
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            Select(offeredChoices[0]);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            Select(offeredChoices[1]);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            Select(offeredChoices[2]);
    }

    private void RegisterEnemy(h980220_EnemyController enemy)
    {
        if (enemy == null)
            return;
        enemy.Infected -= HandleEnemyInfected;
        enemy.Infected += HandleEnemyInfected;
    }

    private void HandleEnemyInfected(h980220_EnemyController enemy)
    {
        if (enemy != null)
            enemy.Infected -= HandleEnemyInfected;
        infectedCount++;
        RefreshCounter();
        TryOpenLevelUp();
    }

    private void TryOpenLevelUp()
    {
        if (choosing || completedLevels >= Thresholds.Length ||
            infectedCount < Thresholds[completedLevels])
            return;
        ShowChoices();
    }

    private void ShowChoices()
    {
        List<Upgrade> pool = BuildPool();
        if (pool.Count < choiceButtons.Length)
            return;

        choosing = true;
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        rhythm?.SetLevelUpPaused(true);
        combat?.SetLevelUpPaused(true);
        popup.SetActive(true);
        popup.transform.SetAsLastSibling();
        titleText.text = $"바이러스 레벨 {completedLevels + 1}";

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            Upgrade selected = pool[index];
            pool.RemoveAt(index);
            SetChoice(i, selected);
        }
    }

    private void SetChoice(int index, Upgrade selected)
    {
        offeredChoices[index] = selected;
        choiceTexts[index].text = $"[{index + 1}]\n{Label(selected)}";
        choiceButtons[index].onClick.RemoveAllListeners();
        choiceButtons[index].onClick.AddListener(() => Select(selected));
    }

    private List<Upgrade> BuildPool()
    {
        var pool = new List<Upgrade> { Upgrade.Heal };
        if (combat != null && !combat.IsDashUnlocked)
            pool.Add(Upgrade.DashUnlock);
        if (combat != null && !combat.IsJumpUnlocked)
            pool.Add(Upgrade.JumpUnlock);
        if (juniorLevel < 2)
            pool.Add(Upgrade.HopakJunior);
        if (growthLevel < 3)
            pool.Add(Upgrade.BodyGrowth);
        if (combat != null && combat.IsDashUnlocked && dashPowerLevel < 3)
            pool.Add(Upgrade.DashPower);
        if (combat != null && combat.IsJumpUnlocked && jumpPowerLevel < 3)
            pool.Add(Upgrade.JumpPower);
        if (maximumInfectionLevel < 3)
            pool.Add(Upgrade.MaximumInfection);
        return pool;
    }

    private void Select(Upgrade selected)
    {
        if (!choosing)
            return;
        Apply(selected);
        completedLevels++;
        ResumeGame();
        RefreshCounter();
        TryOpenLevelUp();
    }

    private void Apply(Upgrade selected)
    {
        switch (selected)
        {
            case Upgrade.DashUnlock:
                combat?.UnlockDash();
                break;
            case Upgrade.JumpUnlock:
                combat?.UnlockJump();
                break;
            case Upgrade.Heal:
                infection?.HealOne();
                break;
            case Upgrade.HopakJunior:
                if (CreateJunior(juniorLevel + 1))
                    juniorLevel++;
                break;
            case Upgrade.BodyGrowth:
                growthLevel++;
                if (rhythm != null)
                    rhythm.transform.localScale *= 1.2f;
                break;
            case Upgrade.DashPower:
                dashPowerLevel++;
                combat?.UpgradeDash();
                break;
            case Upgrade.JumpPower:
                jumpPowerLevel++;
                combat?.UpgradeJump();
                break;
            case Upgrade.MaximumInfection:
                maximumInfectionLevel++;
                infection?.IncreaseMaximumInfection();
                break;
        }
    }

    private bool CreateJunior(int slot)
    {
        if (rhythm == null || slot < 1 || slot > 2)
            return false;

        string juniorName = $"h980220_HopakJr_{slot}";
        Transform existing = rhythm.transform.Find(juniorName);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.localPosition = new Vector3(slot == 1 ? -2.2f : 2.2f, 0f, -0.25f);
            return true;
        }

        float side = slot == 1 ? -1f : 1f;
        var juniorObject = new GameObject(juniorName);
        h980220_HopakJunior junior = juniorObject.AddComponent<h980220_HopakJunior>();
        junior.Initialize(rhythm, side);
        return true;
    }

    private void ResumeGame()
    {
        choosing = false;
        if (popup != null)
            popup.SetActive(false);
        Time.timeScale = savedTimeScale;
        rhythm?.SetLevelUpPaused(false);
        combat?.SetLevelUpPaused(false);
    }

    private void RefreshCounter()
    {
        if (infectionText == null)
            return;
        int level = completedLevels + 1;
        int lowerThreshold = completedLevels == 0 ? 0 : Thresholds[completedLevels - 1];
        int upperThreshold = completedLevels < Thresholds.Length
            ? Thresholds[completedLevels] : lowerThreshold;
        float gauge = completedLevels >= Thresholds.Length
            ? 1f
            : Mathf.InverseLerp(lowerThreshold, upperThreshold, infectedCount);
        if (levelGaugeFill != null)
        {
            levelGaugeFill.fillAmount = gauge;
            levelGaugeFillRect.anchorMax = new Vector2(gauge, 1f);
        }

        string levelProgress = completedLevels < Thresholds.Length
            ? $"{infectedCount - lowerThreshold}/{upperThreshold - lowerThreshold}"
            : "최대";
        string dash = AbilityStatus("대시", combat != null && combat.IsDashUnlocked,
            combat == null ? 0 : combat.CurrentDashCharges,
            combat == null ? 0 : combat.MaximumDashCharges,
            combat == null ? 0f : combat.DashChargeRemaining);
        string jump = AbilityStatus("점프", combat != null && combat.IsJumpUnlocked,
            combat == null ? 0 : combat.CurrentJumpCharges,
            combat == null ? 0 : combat.MaximumJumpCharges,
            combat == null ? 0f : combat.JumpChargeRemaining);
        int livingJuniors = rhythm == null
            ? 0 : rhythm.GetComponentsInChildren<h980220_HopakJunior>(false).Length;
        infectionText.text =
            $"플레이어 레벨 {level}  [{levelProgress}]\n\n" +
            $"최대 리듬: {(rhythm == null ? 0 : rhythm.MaximumSuccessStreak)}\n" +
            $"현재 리듬: {(rhythm == null ? 0 : rhythm.SuccessStreak)}\n" +
            $"감염자 수: {infectedCount}\n" +
            $"호팍 주니어: {livingJuniors}명 | 피격 대신 희생\n{dash}\n{jump}";
    }

    private static string AbilityStatus(string skillName, bool unlocked,
        int currentCharges, int maximumCharges, float remainingSeconds)
    {
        return unlocked
            ? $"{skillName}: {currentCharges}/{maximumCharges} | 충전 {remainingSeconds:0.0}초"
            : $"{skillName}: 잠김";
    }

    private string Label(Upgrade upgrade)
    {
        float dashCooldown = combat == null ? 10f : combat.DashChargeSeconds;
        float jumpCooldown = combat == null ? 4f : combat.JumpChargeSeconds;
        switch (upgrade)
        {
            case Upgrade.DashUnlock:
                return $"대시 해금\nS로 대시합니다\n대시 중 무적 / 경찰 처치 가능\n재충전 시간: {dashCooldown:0.#}초";
            case Upgrade.JumpUnlock:
                return $"점프 해금\nSPACE로 현재 속도를 유지하며 높이 뜁니다\n재충전 시간: {jumpCooldown:0.#}초";
            case Upgrade.Heal:
                return "바이러스 회복\n체력을 1칸 회복합니다 최대 체력을 넘을 수 없습니다.";
            case Upgrade.HopakJunior: return "호팍 주니어\n절반 크기의 동료를 생성합니다\n피격 시 체력 대신 사라집니다";
            case Upgrade.BodyGrowth: return "거대 호팍\n플레이어 크기가 20% 증가합니다";
            case Upgrade.DashPower:
                return $"대시 강화\n최대 사용 횟수가 1 증가합니다\n재충전 시간: {dashCooldown:0.#}초";
            case Upgrade.JumpPower:
                return $"점프 강화\n점프 높이가 18% 증가합니다\n재충전 시간: {jumpCooldown:0.#}초";
            default:
                return "최대 바이러스 +1\n최대 체력칸만 1 증가합니다. 회복은 되지 않습니다.";
        }
    }

    private void BuildUi(Transform hudRoot)
    {
        Canvas canvas = hudRoot == null ? FindFirstObjectByType<Canvas>() :
            hudRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Font font = Font.CreateDynamicFontFromOSFont(
            new[] { "Malgun Gothic", "Arial" }, 24);
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject counterObject = new GameObject("h980220_InfectionCounter",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        counterObject.transform.SetParent(hudRoot == null ? canvas.transform : hudRoot, false);
        infectionText = counterObject.GetComponent<Text>();
        infectionText.font = font;
        infectionText.fontSize = 20;
        infectionText.alignment = TextAnchor.UpperRight;
        infectionText.color = new Color(1f, 0.86f, 0.12f, 1f);
        infectionText.fontStyle = FontStyle.Bold;
        Outline statusOutline = counterObject.AddComponent<Outline>();
        statusOutline.effectColor = new Color(0.025f, 0.035f, 0.09f, 1f);
        statusOutline.effectDistance = new Vector2(2f, -2f);
        statusOutline.useGraphicAlpha = true;
        RectTransform counterRect = infectionText.rectTransform;
        counterRect.anchorMin = new Vector2(1f, 1f);
        counterRect.anchorMax = new Vector2(1f, 1f);
        counterRect.pivot = new Vector2(1f, 1f);
        counterRect.anchoredPosition = new Vector2(-24f, -72f);
        counterRect.sizeDelta = new Vector2(420f, 270f);
        CreateLevelGauge(hudRoot == null ? canvas.transform : hudRoot);

        popup = new GameObject("h980220_LevelUpPopup", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        popup.transform.SetParent(canvas.transform, false);
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.offsetMin = Vector2.zero;
        popupRect.offsetMax = Vector2.zero;
        popup.GetComponent<Image>().color = new Color(0.025f, 0.015f, 0.06f, 0.94f);

        titleText = CreateText(popup.transform, font, "바이러스 레벨", 42,
            new Vector2(0f, 210f), new Vector2(700f, 70f));
        titleText.color = new Color(0.78f, 0.25f, 1f, 1f);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            float x = (i - 1) * 270f;
            choiceButtons[i] = CreateButton(popup.transform, font,
                new Vector2(x, -10f), out choiceTexts[i]);
        }
        popup.SetActive(false);
    }

    private void CreateLevelGauge(Transform parent)
    {
        GameObject backgroundObject = new GameObject("h980220_LevelGauge",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(parent, false);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(1f, 1f);
        backgroundRect.anchorMax = new Vector2(1f, 1f);
        backgroundRect.pivot = new Vector2(1f, 1f);
        backgroundRect.anchoredPosition = new Vector2(-24f, -108f);
        backgroundRect.sizeDelta = new Vector2(300f, 14f);
        backgroundObject.GetComponent<Image>().color = new Color(0.12f, 0.08f, 0.16f, 0.9f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        fillObject.transform.SetParent(backgroundObject.transform, false);
        levelGaugeFillRect = fillObject.GetComponent<RectTransform>();
        levelGaugeFillRect.anchorMin = Vector2.zero;
        levelGaugeFillRect.anchorMax = new Vector2(0f, 1f);
        levelGaugeFillRect.offsetMin = new Vector2(0f, 2f);
        levelGaugeFillRect.offsetMax = new Vector2(0f, -2f);
        levelGaugeFill = fillObject.GetComponent<Image>();
        levelGaugeFill.color = new Color(0.72f, 0.16f, 0.95f, 1f);
        levelGaugeFill.fillAmount = 0f;
    }

    private static Text CreateText(Transform parent, Font font, string value,
        int size, Vector2 position, Vector2 dimensions)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        return text;
    }

    private static Button CreateButton(Transform parent, Font font,
        Vector2 position, out Text label)
    {
        GameObject buttonObject = new GameObject("Upgrade Choice", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(240f, 280f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.22f, 0.08f, 0.34f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.55f, 0.16f, 0.75f, 1f);
        colors.pressedColor = new Color(0.75f, 0.25f, 0.95f, 1f);
        button.colors = colors;
        label = CreateText(buttonObject.transform, font, string.Empty, 22,
            Vector2.zero, new Vector2(215f, 250f));
        return button;
    }

    private void OnDestroy()
    {
        Shutdown();
    }
}
