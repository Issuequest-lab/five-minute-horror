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
        // サウンドノベル、噂、就寝、霊、連打中は専用UIだけを表示する。
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
            return "数日後／宿泊客B：事件を知らず旅館を訪れた。周囲を確認しよう。";
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

            default:
                return "";
        }
    }
}
