using UnityEngine;

public class HorrorGuide : MonoBehaviour
{
    public GameProgress progress;
    public float introSeconds = 7f;

    float startTime;

    void Awake()
    {
        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        startTime = Time.unscaledTime;
    }

    void OnGUI()
    {
        if (progress == null || progress.isStoryInterlude)
            return;

        string message = GetSituationMessage();
        if (message == "")
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 22;
        style.wordWrap = true;
        style.normal.textColor = Color.white;

        float width = Mathf.Min(720f, Screen.width - 24f);

        GUI.Box(
            new Rect(
                (Screen.width - width) / 2f,
                Screen.height - 110f,
                width,
                80f),
            message,
            style);
    }

    string GetSituationMessage()
    {
        if (progress.currentGuest == GameProgress.GuestB &&
            progress.storyStep >= GameProgress.GraveCreated &&
            progress.storyStep < GameProgress.GraveInspected)
        {
            return "女性客：旅館に到着した。周囲を確認しよう。";
        }

        switch (progress.storyStep)
        {
            case GameProgress.Start:
                if (Time.unscaledTime - startTime < introSeconds)
                {
                    return "男性客として旅館を訪れている。\nWASD：移動　マウス：視点操作";
                }
                return "部屋の中を確認しよう。";

            case GameProgress.FirstEvent:
                return "少年の遺体を発見した。警察へ連絡しなければ……。";

            case GameProgress.SecondEventReady:
                return "通報後の状況へ移る。";

            default:
                return "";
        }
    }
}
