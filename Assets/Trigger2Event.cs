using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Trigger2Event : MonoBehaviour
{
    [Header("旧怪異オブジェクト（初期非表示維持用）")]
    public GameObject shadow2;

    [Header("サウンドノベル風演出")]
    public float panelInputDelay = 0.8f;

    [Header("墓碑")]
    public float graveInteractDistance = 2.2f;

    [Header("連打イベント")]
    public int mashTargetPresses = 30;
    public float mashDuration = 8.5f;

    [Header("終了演出")]
    public float fadeSeconds = 1f;

    GameProgress progress;
    Transform player;
    Rigidbody playerBody;
    RigidbodyConstraints savedConstraints;
    readonly List<MonoBehaviour> disabledPlayerBehaviours = new List<MonoBehaviour>();

    bool started;
    bool interludeActive;
    int storyPanel;
    float panelShownAt;

    bool guestBActive;
    bool graveReady;

    bool postStoryActive;
    int postStage;
    float postStageShownAt;

    bool chantMashActive;
    int mashCount;
    float mashTimeRemaining;
    float mashFailureFlashUntil;

    bool ending;
    bool cleared;
    string eventMessage = "";
    float fadeAlpha;

    GameObject aftermathRoot;
    GameObject graveRoot;
    Vector3 guestBSpawn;
    Quaternion guestBRotation;

    void Awake()
    {
        progress = FindAnyObjectByType<GameProgress>();

        if (shadow2 != null)
            shadow2.SetActive(false);

        Collider indoorTrigger = GetComponent<Collider>();
        if (indoorTrigger != null)
            indoorTrigger.enabled = false;

        FindPlayer();
    }

    void Update()
    {
        if (progress == null)
            return;

        FindPlayer();

        if (!started && progress.storyStep >= GameProgress.SecondEventReady)
        {
            started = true;
            BeginStoryInterlude();
        }

        if (interludeActive)
        {
            HandleOpeningInterludeInput();
            return;
        }

        if (postStoryActive)
        {
            HandlePostStoryInput();
            return;
        }

        if (chantMashActive)
        {
            HandleChantMash();
            return;
        }

        if (!guestBActive || !graveReady || ending || player == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            IsNearGrave())
        {
            graveReady = false;
            progress.AdvanceTo(GameProgress.GraveInspected);
            BeginPostGraveStory();
        }
    }

    void FindPlayer()
    {
        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return;

        player = playerObject.transform;
        playerBody = playerObject.GetComponent<Rigidbody>();
    }

    // ---------------------------------------------------------------------
    // 前半：事件当日 → 警察 → 事情聴取 → 数日後 → 宿泊客B
    // ---------------------------------------------------------------------

    void BeginStoryInterlude()
    {
        interludeActive = true;
        storyPanel = 0;
        panelShownAt = Time.unscaledTime;

        progress.isStoryInterlude = true;
        progress.AdvanceTo(GameProgress.PoliceArrival);

        FreezePlayerControls();
        UnlockCursor();
    }

    void HandleOpeningInterludeInput()
    {
        if (!NextPressed() || Time.unscaledTime - panelShownAt < panelInputDelay)
            return;

        storyPanel++;
        panelShownAt = Time.unscaledTime;

        switch (storyPanel)
        {
            case 1:
                progress.AdvanceTo(GameProgress.PoliceQuestioning);
                break;
            case 2:
                progress.AdvanceTo(GameProgress.InvestigationComplete);
                break;
            case 3:
                progress.AdvanceTo(GameProgress.DaysLater);
                break;
            case 4:
                progress.AdvanceTo(GameProgress.GuestBStart);
                break;
            default:
                CompleteOpeningInterlude();
                break;
        }
    }

    void CompleteOpeningInterlude()
    {
        BuildAftermathExterior();

        progress.currentGuest = GameProgress.GuestB;
        progress.isStoryInterlude = false;
        progress.AdvanceTo(GameProgress.GraveCreated);

        SwitchToGuestB();

        interludeActive = false;
        guestBActive = true;
        graveReady = true;
    }

    // ---------------------------------------------------------------------
    // 後半：墓碑 → 宿泊 → 噂 → 就寝 → のっぺらぼう → 地獄
    // ---------------------------------------------------------------------

    void BeginPostGraveStory()
    {
        postStoryActive = true;
        postStage = 0;
        postStageShownAt = Time.unscaledTime;
        progress.isStoryInterlude = true;

        FreezePlayerControls();
        UnlockCursor();
    }

    void HandlePostStoryInput()
    {
        if (!NextPressed() || Time.unscaledTime - postStageShownAt < panelInputDelay)
            return;

        postStageShownAt = Time.unscaledTime;

        switch (postStage)
        {
            case 0:
                postStage = 1;
                break;
            case 1:
                progress.AdvanceTo(GameProgress.CheckIn);
                postStage = 2;
                break;
            case 2:
                progress.AdvanceTo(GameProgress.RumorTold);
                postStage = 3;
                break;
            case 3:
                progress.AdvanceTo(GameProgress.Bedtime);
                postStage = 4;
                break;
            case 4:
                progress.AdvanceTo(GameProgress.GhostAppears);
                postStage = 5;
                break;
            case 5:
                progress.AdvanceTo(GameProgress.DraggedToHell);
                postStage = 6;
                break;
            default:
                BeginChantMash();
                break;
        }
    }

    void BeginChantMash()
    {
        postStoryActive = false;
        chantMashActive = true;
        mashCount = 0;
        mashTimeRemaining = Mathf.Max(3f, mashDuration);
        progress.AdvanceTo(GameProgress.Chanting);
    }

    void HandleChantMash()
    {
        mashTimeRemaining -= Time.unscaledDeltaTime;

        if (MashPressed())
            mashCount++;

        if (mashCount >= Mathf.Max(3, mashTargetPresses))
        {
            chantMashActive = false;
            StartCoroutine(ReliefAndTruthSequence());
            return;
        }

        if (mashTimeRemaining <= 0f)
        {
            mashCount = 0;
            mashTimeRemaining = Mathf.Max(3f, mashDuration);
            mashFailureFlashUntil = Time.unscaledTime + 0.9f;
        }
    }

    IEnumerator ReliefAndTruthSequence()
    {
        ending = true;
        progress.AdvanceTo(GameProgress.FalseRelief);

        eventMessage = "……消えた。\n助かった……。";
        yield return new WaitForSecondsRealtime(2.6f);

        eventMessage = "静寂が戻る。\nもう、終わったはずだった。";
        yield return new WaitForSecondsRealtime(2.1f);

        progress.AdvanceTo(GameProgress.TruthReveal);
        eventMessage = "『待つ叶う想』――";
        yield return new WaitForSecondsRealtime(1.6f);

        eventMessage = "それは、真っ赤な嘘だった。";
        yield return new WaitForSecondsRealtime(3.4f);

        eventMessage = "";

        float duration = Mathf.Max(0.05f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            fadeAlpha = Mathf.Clamp01(elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        fadeAlpha = 1f;
        progress.isStoryInterlude = false;
        progress.AdvanceTo(GameProgress.Cleared);
        cleared = true;
        Time.timeScale = 0f;
    }

    // ---------------------------------------------------------------------
    // プレイヤー切替・操作制御
    // ---------------------------------------------------------------------

    void FreezePlayerControls()
    {
        if (player == null)
            return;

        disabledPlayerBehaviours.Clear();

        MonoBehaviour[] behaviours = player.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour != null && behaviour.enabled)
            {
                behaviour.enabled = false;
                disabledPlayerBehaviours.Add(behaviour);
            }
        }

        if (playerBody != null)
        {
            savedConstraints = playerBody.constraints;
            playerBody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    void RestorePlayerControls()
    {
        if (playerBody != null)
            playerBody.constraints = savedConstraints;

        foreach (MonoBehaviour behaviour in disabledPlayerBehaviours)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        disabledPlayerBehaviours.Clear();
    }

    void SwitchToGuestB()
    {
        if (player == null)
            return;

        player.SetPositionAndRotation(guestBSpawn, guestBRotation);

        Camera playerCamera = player.GetComponentInChildren<Camera>(true);
        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.identity;

        Physics.SyncTransforms();
        RestorePlayerControls();
        LockCursor();
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------------------------------------------------------------------
    // 数日後の旅館前・墓碑
    // ---------------------------------------------------------------------

    void BuildAftermathExterior()
    {
        if (aftermathRoot != null)
            return;

        float floorY = 0f;
        GameObject originalFloor = GameObject.Find("Plane");
        if (originalFloor != null)
            floorY = originalFloor.transform.position.y;

        Vector3 anchor = new Vector3(40f, floorY, 0f);
        aftermathRoot = new GameObject("Aftermath_Exterior");

        CreateBlock("Exterior_Ground", anchor + new Vector3(0f, -0.1f, 0f),
            new Vector3(18f, 0.2f, 14f), new Color(0.18f, 0.18f, 0.17f));

        CreateBlock("Inn_Left", anchor + new Vector3(-3.3f, 1.6f, 5f),
            new Vector3(3.4f, 3.2f, 0.45f), new Color(0.18f, 0.12f, 0.09f));
        CreateBlock("Inn_Right", anchor + new Vector3(3.3f, 1.6f, 5f),
            new Vector3(3.4f, 3.2f, 0.45f), new Color(0.18f, 0.12f, 0.09f));
        CreateBlock("Inn_EntranceTop", anchor + new Vector3(0f, 2.75f, 5f),
            new Vector3(3.2f, 0.9f, 0.45f), new Color(0.14f, 0.09f, 0.07f));
        CreateBlock("Inn_Roof", anchor + new Vector3(0f, 3.45f, 5f),
            new Vector3(10.8f, 0.35f, 1.6f), new Color(0.08f, 0.08f, 0.08f));
        CreateBlock("Inn_Door", anchor + new Vector3(0f, 1.1f, 4.78f),
            new Vector3(2.4f, 2.2f, 0.12f), new Color(0.09f, 0.08f, 0.07f));

        CreateLantern(anchor + new Vector3(-1.7f, 1.7f, 4.5f));
        CreateLantern(anchor + new Vector3(1.7f, 1.7f, 4.5f));

        guestBSpawn = anchor + new Vector3(0f, 1.05f, -4.5f);
        guestBRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        CreateGrave(anchor + new Vector3(2.7f, 0f, 0.7f));
    }

    void CreateLantern(Vector3 position)
    {
        GameObject lantern = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lantern.name = "Entrance_Lantern";
        lantern.transform.SetParent(aftermathRoot.transform, true);
        lantern.transform.position = position;
        lantern.transform.localScale = new Vector3(0.42f, 0.55f, 0.42f);

        Renderer renderer = lantern.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = new Color(0.75f, 0.58f, 0.34f);

        Collider collider = lantern.GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
    }

    GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(aftermathRoot.transform, true);
        block.transform.position = position;
        block.transform.localScale = scale;

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        return block;
    }

    void CreateGrave(Vector3 position)
    {
        graveRoot = new GameObject("Grave_待叶想");
        graveRoot.transform.SetParent(aftermathRoot.transform, true);
        graveRoot.transform.position = position;
        graveRoot.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);

        GameObject basePart = CreateGravePart("Base",
            new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.3f, 0.9f));
        GameObject stonePart = CreateGravePart("Stone",
            new Vector3(0f, 1.05f, 0f), new Vector3(0.9f, 1.6f, 0.35f));

        SetPartColor(basePart, new Color(0.18f, 0.18f, 0.2f));
        SetPartColor(stonePart, new Color(0.22f, 0.22f, 0.24f));
    }

    GameObject CreateGravePart(string partName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(graveRoot.transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            partCollider.enabled = false;

        return part;
    }

    void SetPartColor(GameObject part, Color color)
    {
        Renderer renderer = part != null ? part.GetComponent<Renderer>() : null;
        if (renderer != null)
            renderer.material.color = color;
    }

    bool IsNearGrave()
    {
        if (graveRoot == null || player == null)
            return false;

        Vector3 difference = player.position - graveRoot.transform.position;
        difference.y = 0f;
        return difference.sqrMagnitude <= graveInteractDistance * graveInteractDistance;
    }

    // ---------------------------------------------------------------------
    // GUI
    // ---------------------------------------------------------------------

    void OnGUI()
    {
        if (interludeActive)
        {
            DrawOpeningInterlude();
            return;
        }

        if (postStoryActive)
        {
            DrawPostStory();
            return;
        }

        if (chantMashActive)
        {
            DrawChantMash();
            return;
        }

        if (guestBActive && graveReady && graveRoot != null)
            DrawGraveGuide();

        if (ending && eventMessage != "")
            DrawEndingMessage();

        if (fadeAlpha > 0f || cleared)
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height),
                new Color(0f, 0f, 0f, cleared ? 1f : fadeAlpha));
        }

        if (cleared)
            DrawClearScreen();
    }

    void DrawGraveGuide()
    {
        GUIStyle guide = new GUIStyle(GUI.skin.box);
        guide.alignment = TextAnchor.MiddleCenter;
        guide.fontSize = 24;
        guide.fontStyle = FontStyle.Bold;
        guide.wordWrap = true;
        guide.normal.textColor = Color.white;

        string text = IsNearGrave()
            ? "E：墓碑を調べる"
            : "数日後 ― 宿泊客B。旅館の周囲を確認しよう。";

        float width = Mathf.Min(700f, Screen.width - 30f);
        GUI.Box(new Rect((Screen.width - width) / 2f, 20f, width, 70f), text, guide);
    }

    void DrawEndingMessage()
    {
        Color background = progress != null && progress.storyStep >= GameProgress.TruthReveal
            ? new Color(0.24f, 0f, 0f, 0.94f)
            : new Color(0f, 0f, 0f, 0.90f);

        DrawRect(new Rect(0, 0, Screen.width, Screen.height), background);

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 30;
        style.fontStyle = FontStyle.Bold;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(Screen.width * 0.12f, Screen.height * 0.39f,
            Screen.width * 0.76f, Screen.height * 0.20f), eventMessage, style);
    }

    void DrawClearScreen()
    {
        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 34;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUI.Label(new Rect(0, Screen.height * 0.33f, Screen.width, 65f), "CLEAR", title);

        GUIStyle message = new GUIStyle(GUI.skin.label);
        message.alignment = TextAnchor.MiddleCenter;
        message.fontSize = 21;
        message.normal.textColor = Color.white;

        GUI.Label(new Rect(0, Screen.height * 0.44f, Screen.width, 60f),
            "『待つ叶う想』は、救いの言葉ではなかった。", message);

        if (GUI.Button(new Rect(Screen.width / 2f - 90f,
            Screen.height * 0.60f, 180f, 45f), "ゲームを終了"))
        {
            QuitGame();
        }
    }

    void DrawOpeningInterlude()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), Color.black);
        Rect art = MainArtRect();
        DrawRect(art, new Color(0.055f, 0.055f, 0.065f));
        DrawOpeningArtwork(art);
        DrawStoryText(GetOpeningTitle(), GetOpeningCaption(), "E / Space / Enter：次へ");
    }

    void DrawPostStory()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), Color.black);
        Rect art = MainArtRect();
        DrawRect(art, new Color(0.045f, 0.04f, 0.05f));
        DrawPostArtwork(art);

        string action = postStage == 1
            ? "E / Space / Enter：宿泊手続きをする"
            : postStage == 6
                ? "E / Space / Enter：抗う"
                : "E / Space / Enter：次へ";

        DrawStoryText(GetPostTitle(), GetPostCaption(), action);
    }

    void DrawChantMash()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.12f, 0f, 0f));
        Rect art = MainArtRect();
        DrawRect(art, new Color(0.035f, 0f, 0f));
        DrawGhostAtBedside(art, true);
        DrawHellHands(art);

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 34;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.05f, Screen.width, 50f),
            "地獄へ引きずり込まれる――！", title);

        int target = Mathf.Max(3, mashTargetPresses);
        float ratio = Mathf.Clamp01((float)mashCount / target);

        Rect barBackground = new Rect(Screen.width * 0.18f, Screen.height * 0.72f,
            Screen.width * 0.64f, 34f);
        DrawRect(barBackground, new Color(0.12f, 0.12f, 0.12f));
        DrawRect(new Rect(barBackground.x, barBackground.y,
            barBackground.width * ratio, barBackground.height),
            new Color(0.72f, 0.08f, 0.08f));

        int third = Mathf.Max(1, Mathf.CeilToInt(target / 3f));
        int chants = Mathf.Clamp(mashCount / third, 0, 3);
        string chantText = "";
        for (int i = 0; i < chants; i++)
            chantText += (i == 0 ? "" : "\n") + "待つ叶う想";

        GUIStyle chantStyle = new GUIStyle(GUI.skin.label);
        chantStyle.alignment = TextAnchor.MiddleCenter;
        chantStyle.fontSize = 31;
        chantStyle.fontStyle = FontStyle.Bold;
        chantStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width * 0.12f, Screen.height * 0.47f,
            Screen.width * 0.76f, Screen.height * 0.20f), chantText, chantStyle);

        GUIStyle prompt = new GUIStyle(GUI.skin.box);
        prompt.alignment = TextAnchor.MiddleCenter;
        prompt.fontSize = 24;
        prompt.fontStyle = FontStyle.Bold;
        prompt.wordWrap = true;
        prompt.normal.textColor = Color.white;

        string warning = Time.unscaledTime < mashFailureFlashUntil
            ? "まだ終わっていない！　もっと連打しろ！"
            : "E / Space / Enter / 左クリックを連打！\n『待つ叶う想』を三度唱えろ！";

        GUI.Box(new Rect(Screen.width * 0.16f, Screen.height * 0.79f,
            Screen.width * 0.68f, 82f), warning, prompt);

        GUIStyle timer = new GUIStyle(GUI.skin.label);
        timer.alignment = TextAnchor.MiddleCenter;
        timer.fontSize = 18;
        timer.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.90f, Screen.width, 30f),
            $"残り {Mathf.CeilToInt(mashTimeRemaining)} 秒", timer);
    }

    Rect MainArtRect()
    {
        float artWidth = Mathf.Min(900f, Screen.width * 0.82f);
        float artHeight = Screen.height * 0.55f;
        return new Rect((Screen.width - artWidth) * 0.5f,
            Screen.height * 0.08f, artWidth, artHeight);
    }

    void DrawStoryText(string titleText, string captionText, string actionText)
    {
        Rect art = MainArtRect();

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 25;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(0, art.y - 5f, Screen.width, 42f), titleText, title);

        GUIStyle caption = new GUIStyle(GUI.skin.box);
        caption.alignment = TextAnchor.MiddleCenter;
        caption.fontSize = 23;
        caption.wordWrap = true;
        caption.normal.textColor = Color.white;
        GUI.Box(new Rect(Screen.width * 0.10f, Screen.height * 0.68f,
            Screen.width * 0.80f, Screen.height * 0.17f), captionText, caption);

        GUIStyle next = new GUIStyle(GUI.skin.label);
        next.alignment = TextAnchor.MiddleCenter;
        next.fontSize = 17;
        next.normal.textColor = new Color(0.78f, 0.78f, 0.78f);
        GUI.Label(new Rect(0, Screen.height * 0.87f, Screen.width, 35f), actionText, next);
    }

    // ---------------------------------------------------------------------
    // 簡易シルエット絵
    // ---------------------------------------------------------------------

    void DrawOpeningArtwork(Rect art)
    {
        switch (storyPanel)
        {
            case 0:
                DrawInnSilhouette(art);
                DrawRect(new Rect(art.x + art.width * 0.12f, art.y + art.height * 0.74f,
                    art.width * 0.12f, art.height * 0.045f), new Color(0.15f, 0.35f, 0.8f));
                DrawRect(new Rect(art.x + art.width * 0.24f, art.y + art.height * 0.74f,
                    art.width * 0.12f, art.height * 0.045f), new Color(0.75f, 0.12f, 0.12f));
                for (int i = 0; i < 8; i++)
                    DrawPerson(art, 0.10f + i * 0.105f, 0.70f, 0.055f, 0.22f);
                break;

            case 1:
                DrawReception(art);
                DrawPerson(art, 0.30f, 0.72f, 0.09f, 0.30f);
                DrawPerson(art, 0.62f, 0.72f, 0.09f, 0.30f);
                break;

            case 2:
                DrawInnSilhouette(art);
                DrawLargeText(art, "03:40", 38, new Color(0.7f, 0.7f, 0.72f));
                break;

            case 3:
                DrawLargeText(art, "―― 数日後 ――", 52, Color.white);
                break;

            default:
                DrawInnSilhouette(art);
                DrawPerson(art, 0.43f, 0.72f, 0.10f, 0.34f);
                break;
        }
    }

    void DrawPostArtwork(Rect art)
    {
        switch (postStage)
        {
            case 0:
                DrawRect(new Rect(art.x + art.width * 0.45f, art.y + art.height * 0.22f,
                    art.width * 0.10f, art.height * 0.52f), new Color(0.22f, 0.22f, 0.24f));
                DrawRect(new Rect(art.x + art.width * 0.40f, art.y + art.height * 0.70f,
                    art.width * 0.20f, art.height * 0.07f), new Color(0.16f, 0.16f, 0.18f));
                DrawLargeText(new Rect(art.x, art.y + art.height * 0.30f,
                    art.width, art.height * 0.25f), "待叶想", 46, Color.white);
                break;

            case 1:
                DrawReception(art);
                break;

            case 2:
            case 3:
                DrawReception(art);
                DrawPerson(art, 0.25f, 0.72f, 0.09f, 0.31f);
                DrawPerson(art, 0.64f, 0.72f, 0.09f, 0.31f);
                break;

            case 4:
                DrawBed(art);
                break;

            case 5:
                DrawBed(art);
                DrawGhostAtBedside(art, false);
                break;

            default:
                DrawBed(art);
                DrawGhostAtBedside(art, true);
                DrawHellHands(art);
                break;
        }
    }

    void DrawReception(Rect art)
    {
        DrawRect(new Rect(art.x + art.width * 0.12f, art.y + art.height * 0.58f,
            art.width * 0.76f, art.height * 0.18f), new Color(0.20f, 0.14f, 0.10f));
        DrawRect(new Rect(art.x + art.width * 0.18f, art.y + art.height * 0.32f,
            art.width * 0.64f, art.height * 0.06f), new Color(0.12f, 0.09f, 0.07f));
    }

    void DrawBed(Rect art)
    {
        DrawRect(new Rect(art.x + art.width * 0.16f, art.y + art.height * 0.55f,
            art.width * 0.58f, art.height * 0.23f), new Color(0.16f, 0.14f, 0.15f));
        DrawRect(new Rect(art.x + art.width * 0.17f, art.y + art.height * 0.48f,
            art.width * 0.18f, art.height * 0.10f), new Color(0.42f, 0.40f, 0.40f));
    }

    void DrawGhostAtBedside(Rect art, bool threatening)
    {
        Color bodyColor = threatening
            ? new Color(0.03f, 0f, 0f)
            : new Color(0.04f, 0.04f, 0.045f);

        DrawRect(new Rect(art.x + art.width * 0.72f, art.y + art.height * 0.34f,
            art.width * 0.10f, art.height * 0.42f), bodyColor);

        DrawRect(new Rect(art.x + art.width * 0.715f, art.y + art.height * 0.20f,
            art.width * 0.11f, art.height * 0.16f), new Color(0.78f, 0.76f, 0.72f));
    }

    void DrawHellHands(Rect art)
    {
        Color handColor = new Color(0.22f, 0.01f, 0.01f);
        for (int i = 0; i < 5; i++)
        {
            float x = 0.18f + i * 0.15f;
            DrawRect(new Rect(art.x + art.width * x, art.y + art.height * 0.76f,
                art.width * 0.055f, art.height * 0.22f), handColor);
        }
    }

    void DrawInnSilhouette(Rect art)
    {
        DrawRect(new Rect(art.x + art.width * 0.16f, art.y + art.height * 0.25f,
            art.width * 0.68f, art.height * 0.45f), new Color(0.14f, 0.12f, 0.12f));
        DrawRect(new Rect(art.x + art.width * 0.10f, art.y + art.height * 0.20f,
            art.width * 0.80f, art.height * 0.10f), new Color(0.08f, 0.08f, 0.09f));
        DrawRect(new Rect(art.x + art.width * 0.43f, art.y + art.height * 0.46f,
            art.width * 0.14f, art.height * 0.24f), new Color(0.035f, 0.035f, 0.04f));
    }

    void DrawPerson(Rect art, float x, float bottom, float width, float height)
    {
        Rect body = new Rect(
            art.x + art.width * x,
            art.y + art.height * (bottom - height),
            art.width * width,
            art.height * height);

        DrawRect(body, new Color(0.055f, 0.055f, 0.06f));

        float headSize = art.width * width * 0.72f;
        DrawRect(new Rect(body.x + body.width * 0.14f,
            body.y - headSize * 0.82f, headSize, headSize),
            new Color(0.07f, 0.07f, 0.075f));
    }

    void DrawLargeText(Rect rect, string text, int fontSize, Color color)
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    // ---------------------------------------------------------------------
    // テキスト
    // ---------------------------------------------------------------------

    string GetOpeningTitle()
    {
        switch (storyPanel)
        {
            case 0: return "事件当日";
            case 1: return "事情聴取";
            case 2: return "現場検証終了";
            case 3: return "時間経過";
            default: return "別の宿泊客";
        }
    }

    string GetOpeningCaption()
    {
        switch (storyPanel)
        {
            case 0:
                return "通報から間もなく、警察と救急が到着した。\n旅館の周囲には野次馬まで集まり、現場は騒然となった。";
            case 1:
                return "遺体を発見した宿泊客Aを含め、\nその場にいた宿泊客たちは一人ずつ事情を聞かれた。";
            case 2:
                return "現場検証が終わったのは夜明け前だった。\n人々は旅館を離れ、事件当日の時間はここで途切れる。";
            case 3:
                return "――数日後。\n旅館の外には、事件当日には無かった墓碑が建てられていた。";
            default:
                return "事件を知らない別の宿泊客Bが、この旅館を訪れた。\nここから操作する人物は、遺体を発見した宿泊客Aとは別人である。";
        }
    }

    string GetPostTitle()
    {
        switch (postStage)
        {
            case 0: return "見覚えのない墓碑";
            case 1: return "旅館の受付";
            case 2: return "宿に残る噂";
            case 3: return "待叶想";
            case 4: return "その夜";
            case 5: return "枕元";
            default: return "逃げられない";
        }
    }

    string GetPostCaption()
    {
        switch (postStage)
        {
            case 0:
                return "墓碑には『待叶想』と刻まれている。\n読み方も意味も分からない。宿の者なら何か知っているかもしれない。";
            case 1:
                return "宿泊客Bは旅館へ戻り、受付に宿泊を申し出た。\n名前を書き、部屋の鍵を受け取る。";
            case 2:
                return "墓碑のことを尋ねると、受付の人間は一瞬だけ黙った。\nそして、この宿に昔から残る噂を話し始めた。";
            case 3:
                return "『待叶想』――“待つ・叶う・想”。\n夜、枕元に顔のない子供が立ったら、その言葉を三度唱えれば連れていかれずに済む。\n……この宿では、そう言い伝えられている。";
            case 4:
                return "その夜。\n噂を聞いた宿泊客Bは、用意された部屋で眠りについた。";
            case 5:
                return "夜中、気配で目が覚めた。\n枕元には――顔のない子供が立っていた。";
            default:
                return "身体が動かない。布団の下が底のない闇へ沈んでいく。\n子供の手が、宿泊客Bを地獄へ引きずり込もうとしている。\n噂の言葉を唱えるしかない――！";
        }
    }

    // ---------------------------------------------------------------------
    // 入力・共通描画
    // ---------------------------------------------------------------------

    bool NextPressed()
    {
        if (Keyboard.current == null)
            return false;

        return Keyboard.current.eKey.wasPressedThisFrame ||
               Keyboard.current.spaceKey.wasPressedThisFrame ||
               Keyboard.current.enterKey.wasPressedThisFrame;
    }

    bool MashPressed()
    {
        bool keyboardPressed = Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame);

        bool mousePressed = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        return keyboardPressed || mousePressed;
    }

    void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (cleared)
            Time.timeScale = 1f;
    }
}
