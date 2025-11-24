using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// A* Node class to represent each cell in pathfinding
public class AStarNode
{
    public Vector2Int Position;
    public AStarNode Parent;
    public float GCost; // Distance from start
    public float HCost; // Heuristic distance to goal
    public float FCost => GCost + HCost; // Total cost

    public AStarNode(Vector2Int position)
    {
        Position = position;
    }
}

// NPC Controller with A* 
public class MazeNPC : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed = 2f;

    [SerializeField]
    private GameObject _pathTilePrefab;

    [SerializeField]
    private MazeGenerator _mazeGenerator;

    private Vector2Int _goalPosition;
    private List<Vector2Int> _path;
    private int _currentPathIndex = 0;
    private bool _isMoving = false;
    private List<GameObject> _pathTiles = new List<GameObject>();

    public void Initialize(MazeGenerator mazeGenerator, Vector2Int goalPosition, GameObject pathTilePrefab = null)
    {
        _mazeGenerator = mazeGenerator;
        _goalPosition = goalPosition;
        _pathTilePrefab = pathTilePrefab;

    }

    public void StartPathfinding()
    {
        Vector2Int startPos = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.z)
        );

        _path = FindPath(startPos, _goalPosition);

        if (_path != null && _path.Count > 0)
        {
            Debug.Log($"Path found with {_path.Count} steps!");
            VisualizePath();
            StartCoroutine(FollowPath());
        }
        else
        {
            Debug.LogError("No path found to goal!");
        }
    }

    private void VisualizePath()
    {


        // Clear any existing path tiles
        foreach (GameObject tile in _pathTiles)
        {
            Destroy(tile);
        }
        _pathTiles.Clear();


        // Create path tiles for each position in the path
        for (int i = 0; i < _path.Count; i++)
        {
            Vector2Int pos = _path[i];
            Vector3 tilePosition = new Vector3(pos.x, 0.1f, pos.y); // Increased height
            GameObject pathTile = Instantiate(_pathTilePrefab, tilePosition, Quaternion.Euler(90, 0, 0));
            pathTile.name = $"PathTile_{i}";
            _pathTiles.Add(pathTile);
        }

        Debug.Log($"Visualized path with {_pathTiles.Count} tiles");
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        List<AStarNode> openSet = new List<AStarNode>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        AStarNode startNode = new AStarNode(start);
        startNode.GCost = 0;
        startNode.HCost = GetHeuristic(start, goal);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            // Get node with lowest F cost
            AStarNode currentNode = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();

            if (currentNode.Position == goal)
            {
                return ReconstructPath(currentNode);
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);

            // Check all neighbors
            foreach (Vector2Int neighbor in GetWalkableNeighbors(currentNode.Position))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                float tentativeGCost = currentNode.GCost + 1;

                AStarNode neighborNode = openSet.FirstOrDefault(n => n.Position == neighbor);

                if (neighborNode == null)
                {
                    neighborNode = new AStarNode(neighbor);
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = GetHeuristic(neighbor, goal);
                    neighborNode.Parent = currentNode;
                    openSet.Add(neighborNode);
                }
                else if (tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.Parent = currentNode;
                }
            }
        }

        return null; // No path found
    }

    private List<Vector2Int> GetWalkableNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        MazeCell currentCell = _mazeGenerator.GetMazeCell(position.x, position.y);

        if (currentCell == null)
            return neighbors;

        // Check each direction if wall is cleared
        if (!currentCell.HasRightWall() && position.x + 1 < _mazeGenerator._mazeWidth)
            neighbors.Add(new Vector2Int(position.x + 1, position.y));

        if (!currentCell.HasLeftWall() && position.x - 1 >= 0)
            neighbors.Add(new Vector2Int(position.x - 1, position.y));

        if (!currentCell.HasFrontWall() && position.y + 1 < _mazeGenerator._mazeDepth)
            neighbors.Add(new Vector2Int(position.x, position.y + 1));

        if (!currentCell.HasBackWall() && position.y - 1 >= 0)
            neighbors.Add(new Vector2Int(position.x, position.y - 1));

        return neighbors;
    }

    private float GetHeuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private List<Vector2Int> ReconstructPath(AStarNode endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        AStarNode currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }

        path.Reverse();
        return path;
    }

    private IEnumerator FollowPath()
    {
        _isMoving = true;

        for (int i = 1; i < _path.Count; i++)
        {
            Vector3 targetPosition = new Vector3(_path[i].x, transform.position.y, _path[i].y);

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, _moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = targetPosition;
        }

        _isMoving = false;
        Debug.Log("NPC reached the goal!");
    }
}