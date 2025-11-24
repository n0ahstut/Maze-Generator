using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField]
    public MazeCell _mazeCellPrefab;

    [SerializeField]
    public GameObject _goalPrefab; 

    [SerializeField]
    public GameObject _npcPrefab; 

    [SerializeField]
    public GameObject _pathTilePrefab; 

    [SerializeField]
    public int _mazeWidth;

    [SerializeField]
    public int _mazeDepth;

    private MazeCell[,] _mazeGrid;
    private GameObject _goalObject;
    private MazeNPC _npcController;

    IEnumerator Start()
    {
        _mazeGrid = new MazeCell[_mazeWidth, _mazeDepth];

        for (int x = 0; x < _mazeWidth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity);
            }
        }

        yield return GenerateMaze(null, _mazeGrid[0, 0]);

        // Place goal after maze generation is complete
        PlaceGoal();

        // Spawn and start NPC
        SpawnNPC();
    }

    private void PlaceGoal()
    {

        // Place goal at the opposite corner from start (farthest point)
        int goalX = _mazeWidth - 1;
        int goalZ = _mazeDepth - 1;

        Vector3 goalPosition = new Vector3(goalX, 0.5f, goalZ); // Slightly elevated
        _goalObject = Instantiate(_goalPrefab, goalPosition, Quaternion.identity);

    }

    private void SpawnNPC()
    {
        // Spawn NPC at start position (0, 0)
        Vector3 npcStartPosition = new Vector3(0, 0.5f, 0);
        GameObject npcObject = Instantiate(_npcPrefab, npcStartPosition, Quaternion.identity);

        _npcController = npcObject.GetComponent<MazeNPC>();
        if (_npcController == null)
        {
            _npcController = npcObject.AddComponent<MazeNPC>();
        }

        // Initialize NPC 
        Vector2Int goalPos = new Vector2Int(_mazeWidth - 1, _mazeDepth - 1);
        _npcController.Initialize(this, goalPos, _pathTilePrefab);

       

        // Start pathfinding after delay
        StartCoroutine(StartNPCPathfinding());

    }

    private IEnumerator StartNPCPathfinding()
    {
        yield return new WaitForSeconds(0.5f);
        _npcController.StartPathfinding();
    }

    private IEnumerator GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        currentCell.Visit();
        ClearWalls(previousCell, currentCell);

        yield return new WaitForSeconds(0.05f);

        MazeCell nextCell;
        do
        {
            nextCell = GetNextUnvisitedCell(currentCell);
            if (nextCell != null)
            {
                yield return GenerateMaze(currentCell, nextCell);
            }
        } while (nextCell != null);
    }

    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell);
        return unvisitedCells.OrderBy(_ => Random.Range(1, 10)).FirstOrDefault();
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.position.x;
        int z = (int)currentCell.transform.position.z;

        if (x + 1 < _mazeWidth)
        {
            var cellToRight = _mazeGrid[x + 1, z];
            if (cellToRight.IsVisited == false)
            {
                yield return cellToRight;
            }
        }

        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];
            if (cellToLeft.IsVisited == false)
            {
                yield return cellToLeft;
            }
        }

        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];
            if (cellToFront.IsVisited == false)
            {
                yield return cellToFront;
            }
        }

        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];
            if (cellToBack.IsVisited == false)
            {
                yield return cellToBack;
            }
        }
    }

    private void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        if (previousCell.transform.position.x < currentCell.transform.position.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            return;
        }

        if (previousCell.transform.position.x > currentCell.transform.position.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
            return;
        }

        if (previousCell.transform.position.z < currentCell.transform.position.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
        }

        if (previousCell.transform.position.z > currentCell.transform.position.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
        }
    }

    // Public method to access maze cells for pathfinding
    public MazeCell GetMazeCell(int x, int z)
    {
        if (x >= 0 && x < _mazeWidth && z >= 0 && z < _mazeDepth)
        {
            return _mazeGrid[x, z];
        }
        return null;
    }
}