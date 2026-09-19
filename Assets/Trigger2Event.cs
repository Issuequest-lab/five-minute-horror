using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Trigger2Event : MonoBehaviour
{
    [Header("旧怪異オブジェクト（初期非表示維持用）")]
    public GameObject shadow2;

    [Header("旅館の出口")]
    public Transform exitPoint;
    public float exitDistance = 2f;

    [Header("事件後の群衆")]
    [Min(20)] public int crowdCount = 24;
    public float arrivalInterval = 0.25f;
    public float arrivalMoveSeconds = 2.5f;
    public float investigationSeconds = 8f;
    public float departureInterval = 0.65f;
    public float departureMoveSeconds = 3f;

    [Header("墓碑")]
    public Transform graveSpawnPoint;
    public float graveDelaySeconds = 5f;
    public float graveOutsideDistance = 4f;

    [Header("終了演出")]
    public float inscriptionSeconds = 4f;
    public float fadeSeconds = 1f;

    GameProgress progress;
    Transform player;
    readonly List<GameObject> crowd = new List<GameObject>();

    bool started;
    bool graveReady;
    bool ending;
    bool cleared;
    string eventMessage = "";
    float fadeAlpha;
    GameObject graveRoot;

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

        // 浴槽の子供を確認した後、室内Trigger接触なしで事件後シーケンスを開始する。
        if (!started && progress.storyStep == GameProgress.SecondEventReady)
        {
            started = true;
            StartCoroutine(IncidentAftermathSequence());
        }

        if (!graveReady || ending || player == null)
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
        if (p != null)
            player = p.transform;
    }

    IEnumerator IncidentAftermathSequence()
    {
        progress.AdvanceTo(GameProgress.SecondEvent);

        yield return StartCoroutine(CrowdArrivalSequence());
        yield return StartCoroutine(CrowdDepartureSequence());

        progress.AdvanceTo(GameProgress.InnQuiet);

        yield return new WaitForSeconds(graveDelaySeconds);

        CreateGrave();
        progress.AdvanceTo(GameProgress.GraveCreated);
        graveReady = true;
    }

    IEnumerator CrowdArrivalSequence()
    {
        progress.AdvanceTo(GameProgress.CrowdGathering);

        int count = Mathf.Max(20, crowdCount);
        Vector3 entrance = GetEntrancePosition();

        for (int i = 0; i < count; i++)
        {
            GameObject actor = CreateCrowdMember(i, entrance);
            crowd.Add(actor);

            Vector3 target = GetCrowdPosition(i, count);
            StartCoroutine(MoveActor(actor, target, arrivalMoveSeconds, false));

            yield return new WaitForSeconds(arrivalInterval);
        }

        yield return new WaitForSeconds(arrivalMoveSeconds);

        progress.AdvanceTo(GameProgress.SceneInvestigation);
        yield return new WaitForSeconds(investigationSeconds);
    }

    IEnumerator CrowdDepartureSequence()
    {
        progress.AdvanceTo(GameProgress.CrowdLeaving);

        Vector3 entrance = GetEntrancePosition();
        Vector3 outward = entrance - transform.position;
        outward.y = 0f;

        if (outward.sqrMagnitude < 0.01f)
            outward = transform.forward;
        else
            outward.Normalize();

        for (int i = 0; i < crowd.Count; i++)
        {
            GameObject actor = crowd[i];
            if (actor != null)
            {
                Vector3 outside = entrance + outward * (2.5f + (i % 3) * 0.7f);
                StartCoroutine(MoveActor(actor, outside, departureMoveSeconds, true));
            }

            // 一斉消滅ではなく、1人ずつ玄関へ向かわせる。
            yield return new WaitForSeconds(departureInterval);
        }

        yield return new WaitForSeconds(departureMoveSeconds + 0.25f);
        crowd.Clear();
    }

    GameObject CreateCrowdMember(int index, Vector3 entrance)
    {
        GameObject actor = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        actor.name = index < 6
            ? $"Police_{index + 1:00}"
            : $"Onlooker_{index - 5:00}";

        actor.transform.position = entrance + new Vector3(
            ((index % 4) - 1.5f) * 0.35f,
            1f,
            -((index % 3) * 0.2f));
        actor.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

        Collider actorCollider = actor.GetComponent<Collider>();
        if (actorCollider != null)
            actorCollider.enabled = false;

        return actor;
    }

    Vector3 GetCrowdPosition(int index, int count)
    {
        float angle = (360f / count) * index * Mathf.Deg2Rad;
        float radius = 2.6f + (index % 4) * 0.65f;

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            1f,
            Mathf.Sin(angle) * radius);

        return transform.position + offset;
    }

    IEnumerator MoveActor(GameObject actor, Vector3 destination, float seconds, bool destroyAtEnd)
    {
        if (actor == null)
            yield break;

        Vector3 start = actor.transform.position;
        float duration = Mathf.Max(0.05f, seconds);
        float elapsed = 0f;

        while (elapsed < duration && actor != null)
        {
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            actor.transform.position = Vector3.Lerp(start, destination, t);

            Vector3 direction = destination - actor.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                actor.transform.forward = direction.normalized;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (actor == null)
            yield break;

        actor.transform.position = destination;

        if (destroyAtEnd)
            Destroy(actor);
    }

    Vector3 GetEntrancePosition()
    {
        if (exitPoint != null)
            return exitPoint.position;

        return transform.position + transform.forward * 10f;
    }

    void CreateGrave()
    {
        Vector3 position;
        Quaternion rotation = Quaternion.identity;

        if (graveSpawnPoint != null)
        {
            position = graveSpawnPoint.position;
            rotation = graveSpawnPoint.rotation;
        }
        else
        {
            Vector3 entrance = GetEntrancePosition();
            Vector3 outward = entrance - transform.position;
            outward.y = 0f;

            if (outward.sqrMagnitude < 0.01f)
                outward = transform.forward;
            else
                outward.Normalize();

            position = entrance + outward * graveOutsideDistance;
            rotation = Quaternion.LookRotation(-outward, Vector3.up);
        }

        graveRoot = new GameObject("Grave_待叶想");
        graveRoot.transform.SetPositionAndRotation(position, rotation);

        CreateGravePart("Base", new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.3f, 0.9f));
        CreateGravePart("Stone", new Vector3(0f, 1.05f, 0f), new Vector3(0.9f, 1.6f, 0.35f));
    }

    void CreateGravePart(string partName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = partName;
        part.transform.SetParent(graveRoot.transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
            partCollider.enabled = false;
    }

    bool IsNearGrave()
    {
        if (graveRoot == null || player == null)
            return false;

        Vector3 difference = player.position - graveRoot.transform.position;
        difference.y = 0f;

        return difference.sqrMagnitude <= exitDistance * exitDistance;
    }

    IEnumerator GraveEndingSequence()
    {
        ending = true;
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
        if (graveReady && graveRoot != null)
        {
            GUIStyle guide = new GUIStyle(GUI.skin.box);
            guide.alignment = TextAnchor.MiddleCenter;
            guide.fontSize = 24;
            guide.fontStyle = FontStyle.Bold;
            guide.wordWrap = true;
            guide.normal.textColor = Color.white;

            string text = IsNearGrave()
                ? "E：墓碑を調べる"
                : "外に、見覚えのない墓碑がある。";

            float width = Mathf.Min(700f, Screen.width - 30f);
            GUI.Box(new Rect((Screen.width - width) / 2f, 20f, width, 70f), text, guide);
        }

        if (ending && eventMessage != "")
        {
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
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, cleared ? 1f : fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
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
