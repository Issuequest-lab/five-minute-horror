using UnityEngine;
using UnityEngine.InputSystem;

public class ChantFailureGameOver : MonoBehaviour
{
    GameProgress progress;
    Trigger2Event triggerEvent;

    bool watching;
    bool gameOver;
    float timeRemaining;
    int pressCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<ChantFailureGameOver>() != null)
            return;

        GameObject root = new GameObject("ChantFailureGameOver");
        root.AddComponent<ChantFailureGameOver>();
    }

    void Update()
    {
        if (gameOver)
            return;

        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (triggerEvent == null)
            triggerEvent = FindAnyObjectByType<Trigger2Event>();

        if (progress == null || triggerEvent == null)
            return;

        if (!watching && progress.storyStep == GameProgress.Chanting)
        {
            watching = true;
            pressCount = 0;
            timeRemaining = Mathf.Max(3f, triggerEvent.mashDuration);
        }

        if (!watching)
            return;

        if (progress.storyStep != GameProgress.Chanting)
        {
            // 必要回数を達成し、本編側が次の状態へ進んだ。
            watching = false;
            return;
        }

        timeRemaining -= Time.unscaledDeltaTime;

        if (MashPressed())
            pressCount++;

        int target = Mathf.Max(3, triggerEvent.mashTargetPresses);
        if (pressCount >= target)
        {
            // 成功判定・安堵演出はTrigger2Event側に任せる。
            watching = false;
            return;
        }

        if (timeRemaining <= 0f)
            EnterGameOver();
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

    void EnterGameOver()
    {
        gameOver = true;
        watching = false;

        if (triggerEvent != null)
            triggerEvent.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (!gameOver)
            return;

        Color previous = GUI.color;
        GUI.color = new Color(0.16f, 0f, 0f, 1f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 46;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUI.Label(
            new Rect(0, Screen.height * 0.28f, Screen.width, 80f),
            "GAME OVER",
            title);

        GUIStyle message = new GUIStyle(GUI.skin.label);
        message.alignment = TextAnchor.MiddleCenter;
        message.fontSize = 25;
        message.wordWrap = true;
        message.normal.textColor = Color.white;

        GUI.Label(
            new Rect(Screen.width * 0.12f, Screen.height * 0.43f,
                Screen.width * 0.76f, 120f),
            "『待つ叶う想』を唱えきれなかった。\n女は、顔のない子供とともに闇の底へ引きずり込まれた。",
            message);

        GUIStyle sub = new GUIStyle(GUI.skin.label);
        sub.alignment = TextAnchor.MiddleCenter;
        sub.fontSize = 18;
        sub.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

        GUI.Label(
            new Rect(0, Screen.height * 0.68f, Screen.width, 45f),
            "Playを停止して、もう一度最初から確認してください。",
            sub);
    }

    void OnDestroy()
    {
        if (gameOver)
            Time.timeScale = 1f;
    }
}
