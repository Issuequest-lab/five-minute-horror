using System.Collections.Generic;
using UnityEngine;

public class InnExtensionBuilder : MonoBehaviour
{
    public static InnExtensionBuilder Instance { get; private set; }

    public Vector3 DoorOutsidePoint { get; private set; }
    public Vector3 CorridorPoint { get; private set; }
    public Vector3 EntrancePoint { get; private set; }
    public Vector3 OutsidePoint { get; private set; }
    public Vector3 GravePoint { get; private set; }

    public Vector3 Outward { get; private set; }
    public Vector3 Side { get; private set; }

    const float CorridorLength = 8f;
    const float CorridorWidth = 3.2f;
    const float HallLength = 4.5f;
    const float HallWidth = 5.5f;
    const float WallHeight = 3f;

    GameProgress progress;
    Transform door;
    Collider doorCollider;
    Renderer doorRenderer;
    GameObject extensionRoot;
    bool built;

    public static InnExtensionBuilder EnsureBuilt()
    {
        if (Instance != null)
        {
            Instance.BuildIfNeeded();
            return Instance;
        }

        InnExtensionBuilder existing = FindAnyObjectByType<InnExtensionBuilder>();
        if (existing != null)
        {
            Instance = existing;
            existing.BuildIfNeeded();
            return existing;
        }

        GameObject root = new GameObject("Inn_Extension_Runtime");
        InnExtensionBuilder created = root.AddComponent<InnExtensionBuilder>();
        created.BuildIfNeeded();
        return created;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildIfNeeded();
    }

    void Update()
    {
        if (!built)
            BuildIfNeeded();

        if (progress == null)
            progress = FindAnyObjectByType<GameProgress>();

        if (progress == null)
            return;

        // 事件発生前だけ扉を閉じておく。警察到着以降は通路を開放する。
        bool doorShouldBeOpen = progress.storyStep >= GameProgress.SecondEventReady;

        if (doorCollider != null)
            doorCollider.enabled = !doorShouldBeOpen;

        if (doorRenderer != null)
            doorRenderer.enabled = !doorShouldBeOpen;
    }

    public void BuildIfNeeded()
    {
        if (built)
            return;

        GameObject doorObject = GameObject.Find("Door");
        if (doorObject == null)
            return;

        door = doorObject.transform;
        doorCollider = doorObject.GetComponent<Collider>();
        doorRenderer = doorObject.GetComponent<Renderer>();
        progress = FindAnyObjectByType<GameProgress>();

        Vector3 roomCenter = Vector3.zero;
        GameObject floor = GameObject.Find("Plane");
        if (floor != null)
            roomCenter = floor.transform.position;

        Vector3 direction = door.position - roomCenter;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.25f)
        {
            direction = door.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.forward;

        // 部屋の外方向をX/Zどちらかの軸に揃え、ブロックアウトを崩れにくくする。
        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
            Outward = new Vector3(Mathf.Sign(direction.x), 0f, 0f);
        else
            Outward = new Vector3(0f, 0f, Mathf.Sign(direction.z));

        if (Outward == Vector3.zero)
            Outward = Vector3.forward;

        Side = Vector3.Cross(Vector3.up, Outward).normalized;

        float floorY = floor != null ? floor.transform.position.y : 0f;
        Vector3 doorGround = door.position;
        doorGround.y = floorY;

        DoorOutsidePoint = SetY(doorGround + Outward * 1.2f, floorY + 1f);
        CorridorPoint = SetY(doorGround + Outward * (CorridorLength * 0.55f), floorY + 1f);
        EntrancePoint = SetY(doorGround + Outward * (CorridorLength + HallLength * 0.5f), floorY + 1f);
        OutsidePoint = SetY(doorGround + Outward * (CorridorLength + HallLength + 4f), floorY + 1f);
        GravePoint = SetY(doorGround + Outward * (CorridorLength + HallLength + 5.2f) + Side * 2.2f, floorY);

        extensionRoot = new GameObject("Inn_Extension_Blockout");
        extensionRoot.transform.SetParent(transform, false);

        BuildCorridor(doorGround, floorY);
        BuildEntranceHall(doorGround, floorY);
        BuildOutside(doorGround, floorY);

        built = true;
    }

    void BuildCorridor(Vector3 doorGround, float floorY)
    {
        Vector3 center = doorGround + Outward * (CorridorLength * 0.5f + 0.8f);
        center.y = floorY - 0.1f;

        CreateBlock(
            "Corridor_Floor",
            center,
            ScaleAlongAxes(CorridorWidth, 0.2f, CorridorLength + 1.6f));

        Vector3 wallCenter = doorGround + Outward * (CorridorLength * 0.5f + 0.8f);
        wallCenter.y = floorY + WallHeight * 0.5f;

        CreateBlock(
            "Corridor_Wall_Left",
            wallCenter + Side * (CorridorWidth * 0.5f),
            ScaleAlongAxes(0.2f, WallHeight, CorridorLength + 1.6f));

        CreateBlock(
            "Corridor_Wall_Right",
            wallCenter - Side * (CorridorWidth * 0.5f),
            ScaleAlongAxes(0.2f, WallHeight, CorridorLength + 1.6f));
    }

    void BuildEntranceHall(Vector3 doorGround, float floorY)
    {
        Vector3 center = doorGround + Outward * (CorridorLength + HallLength * 0.5f + 0.8f);
        center.y = floorY - 0.1f;

        CreateBlock(
            "EntranceHall_Floor",
            center,
            ScaleAlongAxes(HallWidth, 0.2f, HallLength));

        Vector3 wallCenter = center;
        wallCenter.y = floorY + WallHeight * 0.5f;

        CreateBlock(
            "EntranceHall_Wall_Left",
            wallCenter + Side * (HallWidth * 0.5f),
            ScaleAlongAxes(0.2f, WallHeight, HallLength));

        CreateBlock(
            "EntranceHall_Wall_Right",
            wallCenter - Side * (HallWidth * 0.5f),
            ScaleAlongAxes(0.2f, WallHeight, HallLength));
    }

    void BuildOutside(Vector3 doorGround, float floorY)
    {
        Vector3 outsideCenter = doorGround + Outward * (CorridorLength + HallLength + 5.5f);
        outsideCenter.y = floorY - 0.12f;

        CreateBlock(
            "Outside_Ground",
            outsideCenter,
            ScaleAlongAxes(12f, 0.2f, 11f));
    }

    GameObject CreateBlock(string blockName, Vector3 position, Vector3 scale)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = blockName;
        block.transform.SetParent(extensionRoot.transform, true);
        block.transform.position = position;
        block.transform.localScale = scale;
        return block;
    }

    Vector3 ScaleAlongAxes(float sideSize, float ySize, float outwardSize)
    {
        if (Mathf.Abs(Outward.x) > 0.5f)
            return new Vector3(outwardSize, ySize, sideSize);

        return new Vector3(sideSize, ySize, outwardSize);
    }

    Vector3 SetY(Vector3 value, float y)
    {
        value.y = y;
        return value;
    }

    public Vector3[] GetArrivalPath(Vector3 roomTarget)
    {
        Vector3 insideDoor = door != null
            ? SetY(door.position - Outward * 0.9f, roomTarget.y)
            : roomTarget;

        return new[]
        {
            SetY(OutsidePoint + Outward * 1.8f, roomTarget.y),
            SetY(EntrancePoint, roomTarget.y),
            SetY(CorridorPoint, roomTarget.y),
            insideDoor,
            roomTarget
        };
    }

    public Vector3[] GetDeparturePath(Vector3 currentPosition, int index)
    {
        float actorY = currentPosition.y;
        Vector3 insideDoor = door != null
            ? SetY(door.position - Outward * 0.9f, actorY)
            : currentPosition;

        Vector3 finalOutside = SetY(
            OutsidePoint + Outward * (2.5f + (index % 3) * 0.7f),
            actorY);

        return new[]
        {
            insideDoor,
            SetY(CorridorPoint, actorY),
            SetY(EntrancePoint, actorY),
            finalOutside
        };
    }
}
