using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "VRPG/Level Data")]
public class LevelData : ScriptableObject
{
    public Vector2Int gridSize = new(10, 10);
    public Vector2Int robotStart;
    public RobotDirection robotFacing = RobotDirection.North;
    public Vector2Int[] starPositions;
}

public enum RobotDirection { North, East, South, West }
