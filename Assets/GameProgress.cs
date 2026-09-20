using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public const int Start = 0;
    public const int FirstEvent = 1;
    public const int SecondEventReady = 2;

    // 事件後はサウンドノベル風の場面転換で時間経過を見せる。
    public const int PoliceArrival = 3;
    public const int PoliceQuestioning = 4;
    public const int InvestigationComplete = 5;
    public const int DaysLater = 6;
    public const int GuestBStart = 7;
    public const int GraveCreated = 8;
    public const int GraveInspected = 9;

    // 宿泊客Bの後半。
    public const int CheckIn = 10;
    public const int RumorTold = 11;
    public const int Bedtime = 12;
    public const int GhostAppears = 13;
    public const int DraggedToHell = 14;
    public const int Chanting = 15;
    public const int FalseRelief = 16;
    public const int TruthReveal = 17;
    public const int Cleared = 18;

    // 旧スクリプト互換用エイリアス。
    public const int SecondEvent = PoliceArrival;
    public const int CrowdGathering = PoliceQuestioning;
    public const int SceneInvestigation = InvestigationComplete;
    public const int CrowdLeaving = DaysLater;
    public const int InnQuiet = GuestBStart;

    public const int GuestA = 0;
    public const int GuestB = 1;

    public int storyStep = Start;
    public int currentGuest = GuestA;
    public bool isStoryInterlude = false;
    public bool hasInspectedRoom = false;

    [HideInInspector]
    public int roomInspectedFrame = -1;

    public bool AdvanceTo(int nextStep)
    {
        if (nextStep <= storyStep)
            return false;

        storyStep = nextStep;
        return true;
    }
}
