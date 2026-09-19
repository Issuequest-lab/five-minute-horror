using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class Trigger2Event : MonoBehaviour
{
    public GameObject shadow2;
    public float shadowSeconds = 5f;

    public Transform exitPoint;
    public float exitDistance = 2f;

    public float doorOpenSeconds = 1f;
    public float fadeSeconds = 1f;

    GameProgress progress;
    Transform player;

    bool started;
    bool unlocked;
    bool exiting;
    bool cleared;
    float fadeAlpha;

    void Awake()
    {
        progress = FindAnyObjectByType<GameProgress>();

        if (shadow2 != null)
            shadow2.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || started)
            return;

        if (progress == null || progress.storyStep != 2)
            return;

        if (shadow2 == null)
        {
            Debug.LogError("怪異2のShadow_2が未設定です。");
            return;
        }

        player = other.attachedRigidbody != null
            ? other.attachedRigidbody.transform
            : other.transform;

        started = true;
        progress.storyStep = 3;

        StartCoroutine(SecondEvent());
    }

    IEnumerator SecondEvent()
    {
        shadow2.SetActive(true);

        yield return new WaitForSeconds(shadowSeconds);

        shadow2.SetActive(false);

        progress.storyStep = 4;
        unlocked = true;
    }

    void Update()
    {
        if (!unlocked || exiting || cleared || player == null)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame &&
            IsNearExit())
        {
            StartCoroutine(ExitSequence());
        }
    }

    bool IsNearExit()
    {
        Transform point = exitPoint != null ? exitPoint : transform;

        Vector3 difference = player.position - point.position;
        difference.y = 0f;

        return difference.sqrMagnitude <= exitDistance * exitDistance;
    }

    IEnumerator ExitSequence()
    {
        exiting = true;

        // 脱出演出中はPlayerを動かさない
        PlayerMove movement = player.GetComponent<PlayerMove>();
        if (movement != null)
            movement.enabled = false;

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.constraints = RigidbodyConstraints.FreezeAll;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Exit Pointに設定したDoorを開く
        if (exitPoint != null)
        {
            Transform door = exitPoint;

            // Cubeの左端を蝶番として回転させる
            Vector3 hinge = door.TransformPoint(
                new Vector3(-0.5f, 0f, 0f));

            Vector3 startPosition = door.position;
            Quaternion startRotation = door.rotation;

            // 開く途中でPlayerと物理的にぶつからないようにする
            Collider doorCollider = door.GetComponent<Collider>();
            if (doorCollider != null)
                doorCollider.enabled = false;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, doorOpenSeconds);

            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(
                    0f, 1f, Mathf.Clamp01(elapsed / duration));

                Quaternion turn = Quaternion.AngleAxis(
                    90f * t, Vector3.up);

                door.SetPositionAndRotation(
                    hinge + turn * (startPosition - hinge),
                    turn * startRotation);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Quaternion finalTurn =
                Quaternion.AngleAxis(90f, Vector3.up);

            door.SetPositionAndRotation(
                hinge + finalTurn * (startPosition - hinge),
                finalTurn * startRotation);
        }

        // 扉が開いたあと、画面を暗転
        float fadeElapsed = 0f;
        float fadeDuration = Mathf.Max(0.01f, fadeSeconds);

        while (fadeElapsed < fadeDuration)
        {
            fadeAlpha = Mathf.Clamp01(
                fadeElapsed / fadeDuration);

            fadeElapsed += Time.deltaTime;
            yield return null;
        }

        fadeAlpha = 1f;

        progress.storyStep = 5;
        cleared = true;
        exiting = false;

        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (!unlocked && !exiting && !cleared)
            return;

        if (!exiting && !cleared)
        {
            GUI.Box(
                new Rect(10, 10, Screen.width - 20, 45),
                IsNearExit()
                    ? "出口が開いた　Eキーで脱出"
                    : "出口が開いた");
            return;
        }

        // 暗転
        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f,
            cleared ? 1f : fadeAlpha);

        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture);

        GUI.color = previousColor;

        if (!cleared)
            return;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 32;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.35f, Screen.width, 60),
            "CLEAR", title);

        GUIStyle message = new GUIStyle(GUI.skin.label);
        message.alignment = TextAnchor.MiddleCenter;
        message.fontSize = 20;
        message.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.45f, Screen.width, 50),
            "脱出成功", message);

        if (GUI.Button(
            new Rect(Screen.width / 2f - 90f,
                     Screen.height * 0.60f, 180f, 45f),
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