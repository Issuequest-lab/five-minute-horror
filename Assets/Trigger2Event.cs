using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Trigger2Event : MonoBehaviour
{
    [Header("旧怪異オブジェクト（初期非表示維持用）")]
    public GameObject shadow2;

    [Header("サウンドノベル風の事件後演出")]
    public float panelInputDelay = 0.8f;

    [Header("墓碑")]
    public float graveInteractDistance = 2.2f;

    [Header("終了演出")]
    public float inscriptionSeconds = 4f;
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

        // 旧仕様の「室内Triggerを踏むと進行」を無効化する。
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

        // 浴槽の遺体を確認した直後から、操作を止めて事件の時間経過を見せる。
        if (!started && progress.storyStep >= GameProgress.SecondEventReady)
        {
            started = true;
            BeginStoryInterlude();
        }

        if (interludeActive)
        {
            HandleInterludeInput();
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
            StartCoroutine(GraveEndingSequence());
        }
    }

    void FindPlayer()
    {
        if (player != null)
            return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null)
            return;

        player = p.transform;
        playerBody = p.GetComponent<Rigidbody>();
    }

    void BeginStoryInterlude()
    {
        interludeActive = true;
        storyPanel = 0;
        panelShownAt = Time.unscaledTime;

        progress.isStoryInterlude = true;
        progress.AdvanceTo(GameProgress.PoliceArrival);

        FreezePlayerControls();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void HandleInterludeInput()
    {
        if (Time.unscaledTime - panelShownAt < panelInputDelay)
            return;

        if (Keyboard.current == null)
            return;

        bool nextPressed =
            Keyboard.current.eKey.wasPressedThisFrame ||
            Keyboard.current.spaceKey.wasPressedThisFrame ||
            Keyboard.current.enterKey.wasPressedThisFrame;

        if (!nextPressed)
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
                CompleteStoryInterlude();
                break;
        }
    }

    void CompleteStoryInterlude()
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

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

        // 小さな旅館前だけを用意する。事件中の警察NPCや灰色の廊下は生成しない。
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

        CreateBlock("Fence_Left", anchor + new Vector3(-8f, 0.55f, 0f),
            new Vector3(0.25f, 1.1f, 14f), new Color(0.11f, 0.1f, 0.09f));
        CreateBlock("Fence_Right", anchor + new Vector3(8f, 0.55f, 0f),
            new Vector3(0.25f, 1.1f, 14f), new Color(0.11f, 0.1f, 0.09f));

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

    IEnumerator GraveEndingSequence()
    {
        ending = true;
        FreezePlayerControls();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        eventMessage = "墓碑には『待叶想』と刻まれている。";
        yield return new WaitForSeconds(inscriptionSeconds);

        eventMessage = "待つ。叶う。ソウ。";
        yield return new WaitForSeconds(inscriptionSeconds);

        eventMessage = "……真っ赤な嘘。";
        yield return new WaitForSeconds(inscriptionSeconds);

        eventMessage = "";

        float duration = Mathf.Max(0.05f, fadeSeconds);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            fadeAlpha = Mathf.Clamp01(elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        fadeAlpha = 1f;
        progress.AdvanceTo(GameProgress.Cleared);
        cleared = true;
        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (interludeActive)
        {
            DrawStoryInterlude();
            return;
        }

        if (guestBActive && graveReady && graveRoot != null)
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

        if (ending && eventMessage != "")
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.82f));

            GUIStyle eventStyle = new GUIStyle(GUI.skin.box);
            eventStyle.alignment = TextAnchor.MiddleCenter;
            eventStyle.fontSize = 26;
            eventStyle.wordWrap = true;
            eventStyle.normal.textColor = Color.white;

            GUI.Box(
                new Rect(Screen.width * 0.15f, Screen.height * 0.42f, Screen.width * 0.7f, 90f),
                eventMessage,
                eventStyle);
        }

        if (fadeAlpha > 0f || cleared)
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height),
                new Color(0f, 0f, 0f, cleared ? 1f : fadeAlpha));
        }

        if (!cleared)
            return;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 32;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.35f, Screen.width, 60f),
            "CLEAR",
            title);

        GUIStyle message = new GUIStyle(GUI.skin.label);
        message.alignment = TextAnchor.MiddleCenter;
        message.fontSize = 20;
        message.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.45f, Screen.width, 50f),
            "事件のあとに残ったものを見届けた。",
            message);

        if (GUI.Button(
            new Rect(Screen.width / 2f - 90f, Screen.height * 0.60f, 180f, 45f),
            "ゲームを終了"))
        {
            QuitGame();
        }
    }

    void DrawStoryInterlude()
    {
        DrawRect(new Rect(0, 0, Screen.width, Screen.height), Color.black);

        float artWidth = Mathf.Min(900f, Screen.width * 0.82f);
        float artHeight = Screen.height * 0.55f;
        Rect art = new Rect((Screen.width - artWidth) * 0.5f, Screen.height * 0.08f, artWidth, artHeight);

        DrawRect(art, new Color(0.055f, 0.055f, 0.065f));
        DrawPanelArtwork(art);

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 25;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUI.Label(new Rect(0, art.y - 5f, Screen.width, 42f), GetPanelTitle(), title);

        GUIStyle caption = new GUIStyle(GUI.skin.box);
        caption.alignment = TextAnchor.MiddleCenter;
        caption.fontSize = 24;
        caption.wordWrap = true;
        caption.normal.textColor = Color.white;

        Rect captionRect = new Rect(
            Screen.width * 0.10f,
            Screen.height * 0.68f,
            Screen.width * 0.80f,
            Screen.height * 0.17f);

        GUI.Box(captionRect, GetPanelCaption(), caption);

        GUIStyle next = new GUIStyle(GUI.skin.label);
        next.alignment = TextAnchor.MiddleCenter;
        next.fontSize = 17;
        next.normal.textColor = new Color(0.78f, 0.78f, 0.78f);

        string nextText = Time.unscaledTime - panelShownAt >= panelInputDelay
            ? "E / Space / Enter：次へ"
            : "";

        GUI.Label(new Rect(0, Screen.height * 0.87f, Screen.width, 35f), nextText, next);
    }

    void DrawPanelArtwork(Rect art)
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
                DrawRect(new Rect(art.x + art.width * 0.18f, art.y + art.height * 0.58f,
                    art.width * 0.64f, art.height * 0.10f), new Color(0.22f, 0.19f, 0.16f));
                DrawPerson(art, 0.30f, 0.54f, 0.09f, 0.30f);
                DrawPerson(art, 0.62f, 0.54f, 0.09f, 0.30f);
                break;

            case 2:
                DrawInnSilhouette(art);
                GUIStyle clock = new GUIStyle(GUI.skin.label);
                clock.alignment = TextAnchor.MiddleCenter;
                clock.fontSize = 38;
                clock.fontStyle = FontStyle.Bold;
                clock.normal.textColor = new Color(0.7f, 0.7f, 0.72f);
                GUI.Label(new Rect(art.x, art.y + art.height * 0.18f, art.width, 60f), "03:40", clock);
                break;

            case 3:
                GUIStyle days = new GUIStyle(GUI.skin.label);
                days.alignment = TextAnchor.MiddleCenter;
                days.fontSize = 52;
                days.fontStyle = FontStyle.Bold;
                days.normal.textColor = Color.white;
                GUI.Label(art, "―― 数日後 ――", days);
                break;

            default:
                DrawInnSilhouette(art);
                DrawPerson(art, 0.43f, 0.72f, 0.10f, 0.34f);
                DrawRect(new Rect(art.x + art.width * 0.70f, art.y + art.height * 0.62f,
                    art.width * 0.08f, art.height * 0.24f), new Color(0.28f, 0.28f, 0.3f));
                DrawRect(new Rect(art.x + art.width * 0.675f, art.y + art.height * 0.82f,
                    art.width * 0.13f, art.height * 0.05f), new Color(0.22f, 0.22f, 0.24f));
                break;
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
        DrawRect(new Rect(
            body.x + body.width * 0.14f,
            body.y - headSize * 0.82f,
            headSize,
            headSize), new Color(0.07f, 0.07f, 0.075f));
    }

    string GetPanelTitle()
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

    string GetPanelCaption()
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
                return "事件から数日が経った。\n旅館の外には、あの日には無かった墓碑が建てられていた。";

            default:
                return "事件を知らない別の宿泊客Bが、この旅館を訪れた。\nここから操作する人物は、遺体を発見した宿泊客Aとは別人である。";
        }
    }

    void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
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
