using UnityEngine;

public class GameProgress : MonoBehaviour
{
    public int storyStep = 0;
    public bool hasInspectedRoom = false;

    [HideInInspector]
    public int roomInspectedFrame = -1;
}