using UnityEngine;

public class GuestIdentityOverlay : MonoBehaviour
{
    GameProgress progress;
    GameObject player;
    bool femaleApplied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GuestIdentityOverlay>() != null)
            return;

        GameObject root = new GameObject("GuestIdentityOverlay");
        root.AddComponent<GuestIdentityOverlay>();
    }

    void Awake()
    {
        progress = FindAnyObjectByType<GameProgress>();
        player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
            player.name = "Player_MaleGuest";
    }

    void Update()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        if (!femaleApplied &&
            progress != null &&
            progress.currentGuest == GameProgress.GuestB)
        {
            femaleApplied = true;

            if (player != null)
                player.name = "Player_FemaleGuest";
        }
    }

    void OnGUI()
    {
        if (progress == null || !progress.isStoryInterlude)
            return;

        bool monthsLater = progress.storyStep == GameProgress.DaysLater;
        bool femaleArrival = progress.storyStep == GameProgress.GuestBStart;

        if (!monthsLater && !femaleArrival)
            return;

        // 既存の説明パネルより前面に出し、不自然な「別人です」という説明を見せない。
        GUI.depth = -1000;

        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle title = new GUIStyle(GUI.skin.label);
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 44;
        title.fontStyle = FontStyle.Bold;
        title.normal.textColor = Color.white;

        GUIStyle body = new GUIStyle(GUI.skin.label);
        body.alignment = TextAnchor.MiddleCenter;
        body.fontSize = 25;
        body.wordWrap = true;
        body.normal.textColor = Color.white;

        if (monthsLater)
        {
            GUI.Label(
                new Rect(0, Screen.height * 0.32f, Screen.width, 70f),
                "―― 数ヶ月後 ――",
                title);

            GUI.Label(
                new Rect(Screen.width * 0.16f, Screen.height * 0.48f,
                    Screen.width * 0.68f, 100f),
                "事件の記憶が薄れ始めた頃。\n旅館は再び客を迎えていた。",
                body);
            return;
        }

        // 女性客のシルエット。髪を示す輪郭を追加し、文章で「別人」と説明しなくても視覚で分ける。
        float cx = Screen.width * 0.5f;
        float top = Screen.height * 0.23f;

        DrawRect(new Rect(cx - 45f, top + 90f, 90f, 210f), new Color(0.10f, 0.10f, 0.11f));
        DrawRect(new Rect(cx - 50f, top + 30f, 100f, 100f), new Color(0.15f, 0.15f, 0.16f));
        DrawRect(new Rect(cx - 68f, top + 20f, 28f, 170f), new Color(0.06f, 0.06f, 0.07f));
        DrawRect(new Rect(cx + 40f, top + 20f, 28f, 170f), new Color(0.06f, 0.06f, 0.07f));

        GUI.Label(
            new Rect(0, Screen.height * 0.60f, Screen.width, 60f),
            "ひとりの女性客が旅館を訪れた。",
            title);

        GUI.Label(
            new Rect(Screen.width * 0.16f, Screen.height * 0.71f,
                Screen.width * 0.68f, 90f),
            "彼女は、ここで起きた事件を知らない。",
            body);
    }

    void DrawRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
