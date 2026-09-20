using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GuestRoom3DSequence : MonoBehaviour
{
    enum RoomStage
    {
        Waiting,
        InspectBath,
        EerieMoment,
        GoToFuton,
        Sleeping,
        Ghost,
        Mashing,
        Ending,
        Cleared
    }

    GameProgress progress;
    Trigger2Event triggerEvent;
    GameObject player;
    Rigidbody playerBody;
    PlayerMove playerMove;
    MouseLook mouseLook;

    GameObject roomRoot;
    Transform bathPoint;
    Transform futonPoint;
    GameObject eerieShadow;

    RoomStage stage = RoomStage.Waiting;
    bool started;
    float stageStartedAt;
    float screenDarkAlpha;

    int mashCount;
    float mashTimeRemaining;

    string centerMessage = "";
    bool showGhost;
    bool cleared;

    const float InteractDistance = 2.25f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GuestRoom3DSequence>() != null)
            return;

        GameObject root = new GameObject("GuestRoom3DSequence");
        root.AddComponent<GuestRoom3DSequence>();
    }

    void Update()
    {
        ResolveReferences();

        if (!started &&
            progress != null &&
            progress.currentGuest == GameProgress.GuestB &&
            progress.storyStep == GameProgress.Bedtime)
        {
            StartGuestRoom();
        }

        if (!started || player == null)
            return;

        switch (stage)
        {
            case RoomStage.InspectBath:
                if (Near(bathPoint) && PressedE())
                    StartCoroutine(EerieBathMoment());
                break;

            case RoomStage.GoToFuton:
                if (Near(futonPoint) && PressedE())
                    StartCoroutine(SleepSequence());
                break;

            case RoomStage.Ghost:
                if (NextPressed())
                    BeginMash();
                break;

            case RoomStage.Mashing:
                HandleMash();
                break;
        }
    }

    void ResolveReferences()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (triggerEvent == null)
            triggerEvent = FindAnyObjectByType<Trigger2Event>();

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerBody = player.GetComponent<Rigidbody>();
                playerMove = player.GetComponent<PlayerMove>();
                mouseLook = player.GetComponentInChildren<MouseLook>(true);
            }
        }
    }

    void StartGuestRoom()
    {
        started = true;

        // ここから先は従来の2D就寝処理ではなく、3D宿泊部屋シーケンスが担当する。
        if (triggerEvent != null)
            triggerEvent.enabled = false;

        progress.isStoryInterlude = false;

        BuildGuestRoom();
        MovePlayerIntoRoom();
        EnablePlayerControl();

        stage = RoomStage.InspectBath;
        stageStartedAt = Time.unscaledTime;
        centerMessage = "案内された部屋に入った。";
        StartCoroutine(ClearCenterMessageAfter(2.0f));
    }

    void BuildGuestRoom()
    {
        if (roomRoot != null)
            return;

        Vector3 anchor = new Vector3(80f, 0f, 0f);
        roomRoot = new GameObject("GuestRoom_MonthsLater");

        CreateBlock("Floor", anchor + new Vector3(0f, -0.1f, 0f),
            new Vector3(8f, 0.2f, 10f), new Color(0.23f, 0.19f, 0.15f));
        CreateBlock("Wall_Back", anchor + new Vector3(0f, 1.5f, 5f),
            new Vector3(8f, 3f, 0.2f), new Color(0.33f, 0.29f, 0.24f));
        CreateBlock("Wall_Left", anchor + new Vector3(-4f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 10f), new Color(0.33f, 0.29f, 0.24f));
        CreateBlock("Wall_Right", anchor + new Vector3(4f, 1.5f, 0f),
            new Vector3(0.2f, 3f, 10f), new Color(0.33f, 0.29f, 0.24f));
        CreateBlock("Wall_Front_Left", anchor + new Vector3(-2.5f, 1.5f, -5f),
            new Vector3(3f, 3f, 0.2f), new Color(0.33f, 0.29f, 0.24f));
        CreateBlock("Wall_Front_Right", anchor + new Vector3(2.5f, 1.5f, -5f),
            new Vector3(3f, 3f, 0.2f), new Color(0.33f, 0.29f, 0.24f));

        // 浴槽。数ヶ月後なので遺体はない。
        Vector3 bathCenter = anchor + new Vector3(2.25f, 0.35f, 2.7f);
        CreateBlock("Bath_Base", bathCenter, new Vector3(2.4f, 0.7f, 1.55f),
            new Color(0.66f, 0.68f, 0.67f));
        CreateBlock("Bath_Inner", bathCenter + new Vector3(0f, 0.35f, 0f),
            new Vector3(1.75f, 0.12f, 1.0f), new Color(0.18f, 0.24f, 0.25f));

        GameObject bathMarker = new GameObject("Bath_InspectPoint");
        bathMarker.transform.SetParent(roomRoot.transform, true);
        bathMarker.transform.position = anchor + new Vector3(2.25f, 1f, 1.5f);
        bathPoint = bathMarker.transform;

        // 布団。
        CreateBlock("Futon", anchor + new Vector3(-1.35f, 0.09f, 1.25f),
            new Vector3(2.0f, 0.18f, 3.5f), new Color(0.50f, 0.46f, 0.42f));
        CreateBlock("Pillow", anchor + new Vector3(-1.35f, 0.23f, 2.35f),
            new Vector3(1.35f, 0.22f, 0.65f), new Color(0.72f, 0.70f, 0.66f));

        GameObject futonMarker = new GameObject("Futon_SleepPoint");
        futonMarker.transform.SetParent(roomRoot.transform, true);
        futonMarker.transform.position = anchor + new Vector3(-1.35f, 1f, 0.2f);
        futonPoint = futonMarker.transform;

        // 違和感の瞬間だけ見える人影。
        eerieShadow = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        eerieShadow.name = "Eerie_Shadow";
        eerieShadow.transform.SetParent(roomRoot.transform, true);
        eerieShadow.transform.position = anchor + new Vector3(-3.0f, 1.0f, 4.25f);
        eerieShadow.transform.localScale = new Vector3(0.6f, 1.0f, 0.6f);
        Renderer shadowRenderer = eerieShadow.GetComponent<Renderer>();
        if (shadowRenderer != null)
            shadowRenderer.material.color = new Color(0.02f, 0.02f, 0.025f);
        Collider shadowCollider = eerieShadow.GetComponent<Collider>();
        if (shadowCollider != null)
            shadowCollider.enabled = false;
        eerieShadow.SetActive(false);
    }

    void MovePlayerIntoRoom()
    {
        Vector3 spawn = new Vector3(80f, 1.05f, -3.6f);
        player.transform.SetPositionAndRotation(spawn,
            Quaternion.LookRotation(Vector3.forward, Vector3.up));

        Camera camera = player.GetComponentInChildren<Camera>(true);
        if (camera != null)
            camera.transform.localRotation = Quaternion.identity;

        Physics.SyncTransforms();
    }

    void EnablePlayerControl()
    {
        if (playerBody != null)
        {
            playerBody.isKinematic = false;
            playerBody.useGravity = true;
            playerBody.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (playerMove != null)
            playerMove.enabled = true;

        if (mouseLook != null)
            mouseLook.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void DisablePlayerControl()
    {
        if (playerMove != null)
            playerMove.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            playerBody.constraints = RigidbodyConstraints.FreezeAll;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    IEnumerator EerieBathMoment()
    {
        stage = RoomStage.EerieMoment;
        stageStartedAt = Time.unscaledTime;

        if (eerieShadow != null)
            eerieShadow.SetActive(true);

        screenDarkAlpha = 0.28f;
        centerMessage = "……今、一瞬だけ寒気がした。";

        yield return new WaitForSecondsRealtime(0.28f);

        if (eerieShadow != null)
            eerieShadow.SetActive(false);

        screenDarkAlpha = 0.10f;
        yield return new WaitForSecondsRealtime(1.35f);

        screenDarkAlpha = 0f;
        centerMessage = "……気のせいだ。もう寝よう。";
        stage = RoomStage.GoToFuton;
        stageStartedAt = Time.unscaledTime;

        yield return new WaitForSecondsRealtime(1.8f);
        if (stage == RoomStage.GoToFuton)
            centerMessage = "";
    }

    IEnumerator SleepSequence()
    {
        stage = RoomStage.Sleeping;
        progress.isStoryInterlude = true;
        DisablePlayerControl();

        centerMessage = "布団に入った。";
        screenDarkAlpha = 0.25f;
        yield return new WaitForSecondsRealtime(1.2f);

        centerMessage = "部屋の灯りを消す。";
        screenDarkAlpha = 0.65f;
        yield return new WaitForSecondsRealtime(1.4f);

        centerMessage = "……目を閉じた。";
        screenDarkAlpha = 1f;
        yield return new WaitForSecondsRealtime(1.8f);

        progress.AdvanceTo(GameProgress.GhostAppears);
        centerMessage = "夜中、気配で目が覚めた。\n枕元には――顔のない子供が立っていた。";
        screenDarkAlpha = 0.82f;
        showGhost = true;
        stage = RoomStage.Ghost;
        stageStartedAt = Time.unscaledTime;
    }

    void BeginMash()
    {
        progress.AdvanceTo(GameProgress.DraggedToHell);
        progress.AdvanceTo(GameProgress.Chanting);

        showGhost = true;
        centerMessage = "身体が動かない。\n闇の底へ引きずり込まれる――！";
        screenDarkAlpha = 0.72f;

        mashCount = 0;
        mashTimeRemaining = triggerEvent != null
            ? Mathf.Max(3f, triggerEvent.mashDuration)
            : 8.5f;

        stage = RoomStage.Mashing;
    }

    void HandleMash()
    {
        mashTimeRemaining -= Time.unscaledDeltaTime;

        if (MashPressed())
            mashCount++;

        int target = triggerEvent != null
            ? Mathf.Max(3, triggerEvent.mashTargetPresses)
            : 30;

        if (mashCount >= target)
        {
            stage = RoomStage.Ending;
            StartCoroutine(ReliefAndTruth());
            return;
        }

        if (mashTimeRemaining <= 0f)
        {
            // GAME OVER表示は ChantFailureGameOver に任せる。
            stage = RoomStage.Ending;
            enabled = false;
        }
    }

    IEnumerator ReliefAndTruth()
    {
        progress.AdvanceTo(GameProgress.FalseRelief);
        showGhost = false;
        centerMessage = "……消えた。\n助かった……。";
        screenDarkAlpha = 0.88f;
        yield return new WaitForSecondsRealtime(2.4f);

        centerMessage = "静寂が戻る。\nもう、終わった――はずだった。";
        yield return new WaitForSecondsRealtime(2.0f);

        progress.AdvanceTo(GameProgress.TruthReveal);
        centerMessage = "『待つ叶う想』――\nそれは、真っ赤な嘘だった。";
        screenDarkAlpha = 0.96f;
        yield return new WaitForSecondsRealtime(3.5f);

        progress.AdvanceTo(GameProgress.Cleared);
        progress.isStoryInterlude = false;
        centerMessage = "";
        screenDarkAlpha = 1f;
        cleared = true;
        stage = RoomStage.Cleared;
        Time.timeScale = 0f;
    }

    IEnumerator ClearCenterMessageAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (stage == RoomStage.InspectBath)
            centerMessage = "";
    }

    bool Near(Transform target)
    {
        if (target == null || player == null)
            return false;

        Vector3 delta = player.transform.position - target.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= InteractDistance * InteractDistance;
    }

    bool PressedE()
    {
        return Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;
    }

    bool NextPressed()
    {
        return Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame);
    }

    bool MashPressed()
    {
        bool keyboard = Keyboard.current != null &&
            (Keyboard.current.eKey.wasPressedThisFrame ||
             Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame);

        bool mouse = Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame;

        return keyboard || mouse;
    }

    GameObject CreateBlock(string objectName, Vector3 position, Vector3 scale, Color color)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(roomRoot.transform, true);
        block.transform.position = position;
        block.transform.localScale = scale;

        Renderer renderer = block.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.color = color;

        return block;
    }

    void OnGUI()
    {
        if (!started)
            return;

        if (screenDarkAlpha > 0f)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, screenDarkAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        if (stage == RoomStage.InspectBath)
            DrawPrompt(Near(bathPoint) ? "E：浴槽を調べる" : "部屋の中を確認しよう。浴槽がある。 ");
        else if (stage == RoomStage.GoToFuton)
            DrawPrompt(Near(futonPoint) ? "E：布団に入る" : "もう寝よう。布団へ向かおう。 ");

        if (stage == RoomStage.Ghost)
        {
            DrawGhostSilhouette();
            DrawPrompt("E / Space / Enter：抗う");
        }

        if (stage == RoomStage.Mashing)
            DrawMashUI();

        if (!string.IsNullOrEmpty(centerMessage))
            DrawCenterMessage(centerMessage);

        if (cleared)
            DrawClearScreen();
    }

    void DrawPrompt(string text)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 23;
        style.fontStyle = FontStyle.Bold;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        float width = Mathf.Min(700f, Screen.width - 30f);
        GUI.Box(new Rect((Screen.width - width) * 0.5f, 20f, width, 68f), text, style);
    }

    void DrawCenterMessage(string text)
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 28;
        style.fontStyle = FontStyle.Bold;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        GUI.Box(new Rect(Screen.width * 0.14f, Screen.height * 0.40f,
            Screen.width * 0.72f, 110f), text, style);
    }

    void DrawGhostSilhouette()
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.04f, 0.04f, 0.045f, 1f);
        GUI.DrawTexture(new Rect(Screen.width * 0.66f, Screen.height * 0.22f,
            Screen.width * 0.10f, Screen.height * 0.46f), Texture2D.whiteTexture);

        GUI.color = new Color(0.78f, 0.76f, 0.72f, 1f);
        GUI.DrawTexture(new Rect(Screen.width * 0.65f, Screen.height * 0.14f,
            Screen.width * 0.12f, Screen.height * 0.16f), Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void DrawMashUI()
    {
        int target = triggerEvent != null
            ? Mathf.Max(3, triggerEvent.mashTargetPresses)
            : 30;

        float ratio = Mathf.Clamp01((float)mashCount / target);

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 32;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 60f),
            "地獄へ引きずり込まれる――！", title);

        GUIStyle prompt = new GUIStyle(GUI.skin.box);
        prompt.alignment = TextAnchor.MiddleCenter;
        prompt.fontSize = 24;
        prompt.fontStyle = FontStyle.Bold;
        prompt.normal.textColor = Color.white;
        GUI.Box(new Rect(Screen.width * 0.16f, Screen.height * 0.72f,
            Screen.width * 0.68f, 82f),
            "E / Space / Enter / 左クリックを連打！\n『待つ叶う想』を三度唱えろ！", prompt);

        Color previous = GUI.color;
        GUI.color = new Color(0.12f, 0.12f, 0.12f, 1f);
        GUI.DrawTexture(new Rect(Screen.width * 0.18f, Screen.height * 0.65f,
            Screen.width * 0.64f, 32f), Texture2D.whiteTexture);
        GUI.color = new Color(0.72f, 0.08f, 0.08f, 1f);
        GUI.DrawTexture(new Rect(Screen.width * 0.18f, Screen.height * 0.65f,
            Screen.width * 0.64f * ratio, 32f), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle timer = new GUIStyle(GUI.skin.label);
        timer.alignment = TextAnchor.MiddleCenter;
        timer.fontSize = 19;
        timer.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.84f, Screen.width, 35f),
            $"残り {Mathf.CeilToInt(mashTimeRemaining)} 秒", timer);
    }

    void DrawClearScreen()
    {
        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 42;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, 70f), "CLEAR", title);

        GUIStyle body = new GUIStyle(GUI.skin.label);
        body.alignment = TextAnchor.MiddleCenter;
        body.fontSize = 22;
        body.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 60f),
            "『待つ叶う想』は、救いの言葉ではなかった。", body);

        if (GUI.Button(new Rect(Screen.width / 2f - 90f,
            Screen.height * 0.60f, 180f, 45f), "ゲームを終了"))
        {
#if UNITY_EDITOR
            Time.timeScale = 1f;
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void OnDestroy()
    {
        if (cleared)
            Time.timeScale = 1f;
    }
}
