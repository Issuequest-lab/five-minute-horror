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
            return "数日後／宿泊客B：旅館に到着した。周囲を確認しよう。";
        }

        switch (progress.storyStep)
        {
            case GameProgress.Start:
                if (Time.unscaledTime - startTime < introSeconds)
                {
                    return "宿泊客A。旅館の中を確認しよう。\nWASD：移動　マウス：視点操作";
                }
                return "宿泊客A：浴槽のある部屋を確認しよう。";

            case GameProgress.FirstEvent:
                return "浴槽で子供の遺体を発見した。警察へ連絡しなければ……。";

            case GameProgress.SecondEventReady:
                return "通報後の状況へ移る。";

            case GameProgress.GraveInspected:
                return "宿泊客B：墓碑に刻まれた文字を確認している。";

            default:
                return "";
        }
    }
}
