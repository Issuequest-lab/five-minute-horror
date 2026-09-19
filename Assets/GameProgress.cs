using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public const int Start = 0;
    public const int FirstEvent = 1;
    public const int SecondEventReady = 2;
    public const int SecondEvent = 3;
    public const int CrowdGathering = 4;
    public const int SceneInvestigation = 5;
    public const int CrowdLeaving = 6;
    public const int InnQuiet = 7;
    public const int GraveCreated = 8;
    public const int GraveInspected = 9;
    public const int Cleared = 10;

    public int storyStep = Start;
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
