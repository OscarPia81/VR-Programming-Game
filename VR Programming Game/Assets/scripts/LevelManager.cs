using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Data")]
    public List<LevelData> levels;

    [Header("Prefabs")]
    public GameObject gridTileLightPrefab;
    public GameObject gridTileDarkPrefab;
    public GameObject starPrefab;

    [Header("Grid")]
    public Vector2Int gridSize;
    public Vector3 gridCenter = Vector3.zero;

    [Header("Settings")]
    public float cellSize = 1f;
    public float starHeight = 0.5f;

    public int nextStarIndex { get; private set; }

    private int currentLevelIndex;
    private GameObject gridParent;
    private ScreenController screen;
    private CodeManager codeManager;

    private void Awake()
    {
        Instance = this;
        screen = FindObjectOfType<ScreenController>();
        codeManager = FindObjectOfType<CodeManager>();
    }

    private void Start()
    {
        if (levels.Count > 0)
            LoadLevel(0);
    }

    public void CollectStar(Star star)
    {
        if (star.orderIndex != nextStarIndex)
        {
            screen?.UpdateText("Wrong order! Restarting...");
            StartCoroutine(RestartAfterDelay());
            return;
        }

        star.Collect();
        nextStarIndex++;

        screen?.UpdateText($"Star {nextStarIndex}/{CountStarsInLevel()} collected");

        if (nextStarIndex >= CountStarsInLevel())
        {
            screen?.UpdateText("Level Complete!");
            StartCoroutine(NextLevelAfterDelay(2f));
        }
    }

    private int CountStarsInLevel()
    {
        if (currentLevelIndex >= levels.Count) return 0;
        return levels[currentLevelIndex].starPositions.Length;
    }

    private IEnumerator NextLevelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentLevelIndex++;
        if (currentLevelIndex >= levels.Count)
        {
            screen?.UpdateText("All levels complete!");
            yield break;
        }
        LoadLevel(currentLevelIndex);
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(1.5f);
        LoadLevel(currentLevelIndex);
    }

    public void RestartLevel()
    {
        LoadLevel(currentLevelIndex);
    }

    private void LoadLevel(int index)
    {
        currentLevelIndex = index;
        nextStarIndex = 0;

        if (codeManager != null && codeManager.IsExecuting)
        {
            codeManager.StopExecution();
        }

        ClearLevel();

        var data = levels[index];

        var size = gridSize.x > 0 && gridSize.y > 0 ? gridSize : data.gridSize;

        GenerateGrid(size);
        SpawnStars(data, size);
        PlaceRobot(data, size);
    }

    private void ClearLevel()
    {
        if (gridParent != null) Destroy(gridParent);
        foreach (var star in FindObjectsOfType<Star>())
            Destroy(star.gameObject);
    }

    private void GenerateGrid(Vector2Int size)
    {
        gridParent = new GameObject("Grid");
        Vector3 origin = GridOrigin(size);

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                GameObject tilePrefab = (x + y) % 2 == 0 ? gridTileLightPrefab : gridTileDarkPrefab;
                Vector3 pos = origin + new Vector3(x * cellSize + cellSize * 0.5f, 0f, y * cellSize + cellSize * 0.5f);

                if (tilePrefab != null)
                {
                    var tile = Instantiate(tilePrefab, pos, Quaternion.Euler(90f, 0f, 0f), gridParent.transform);
                    tile.transform.localScale = Vector3.one * cellSize * 0.95f;
                }
            }
        }
    }

    private void SpawnStars(LevelData data, Vector2Int size)
    {
        Vector3 origin = GridOrigin(size);

        for (int i = 0; i < data.starPositions.Length; i++)
        {
            Vector3 pos = origin + new Vector3(data.starPositions[i].x * cellSize + cellSize * 0.5f, 0f, data.starPositions[i].y * cellSize + cellSize * 0.5f);
            if (starPrefab != null)
            {
                var star = Instantiate(starPrefab, new Vector3(pos.x, gridCenter.y + starHeight, pos.z), Quaternion.identity);
                var starComp = star.GetComponent<Star>();
                if (starComp != null) starComp.orderIndex = i;
            }
        }
    }

    private void PlaceRobot(LevelData data, Vector2Int size)
    {
        if (CodeManager.Robot == null) return;

        Vector3 origin = GridOrigin(size);
        Vector3 pos = origin + new Vector3(data.robotStart.x * cellSize + cellSize * 0.5f, 0f, data.robotStart.y * cellSize + cellSize * 0.5f);
        CodeManager.Robot.transform.position = new Vector3(pos.x, gridCenter.y, pos.z);

        float facingAngle = data.robotFacing switch
        {
            RobotDirection.North => 0f,
            RobotDirection.East => 90f,
            RobotDirection.South => 180f,
            RobotDirection.West => 270f,
            _ => 0f
        };
        CodeManager.Robot.transform.rotation = Quaternion.Euler(0f, facingAngle, 0f);
    }

    private Vector3 GridOrigin(Vector2Int size)
    {
        return gridCenter - new Vector3(size.x * cellSize * 0.5f, 0f, size.y * cellSize * 0.5f);
    }
}
