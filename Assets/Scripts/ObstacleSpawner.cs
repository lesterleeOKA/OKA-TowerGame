using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject obstaclePrefab, submitPrefab;
    public Texture[] Obstacles;
    public int obstacleCount = 4;
    public Transform parent;
    public List<GameObject> obstacles = new List<GameObject>();
    public List<int> obstacleCellIds = new List<int>();
    public GameObject submitArea = null;

    private GameObject GetPooledObstacle(int index)
    {
        // If the obstacle already exists in the pool, reuse it
        if (index < obstacles.Count)
        {
            var obj = obstacles[index];
            if (!obj.activeSelf)
                obj.SetActive(true);
            return obj;
        }
        else
        {
            var obj = Instantiate(obstaclePrefab, parent);
            obj.SetActive(true);
            obstacles.Add(obj);
            return obj;
        }
    }

    public void GenerateObstacles(GridManager gridManager)
    {
        if (gridManager == null || obstaclePrefab == null) return;

        // Deactivate all pooled obstacles at the start
        foreach (var obj in obstacles)
            obj.SetActive(false);

        int gridRows = gridManager.gridRow;
        int gridCols = gridManager.gridColumn;

        // 1. Only use enableCellIds, exclude showCellIdList
        HashSet<int> forbidden = new HashSet<int>(gridManager.showCellIdList ?? new List<int>());
        List<int> availableCellIds = new List<int>();
        if (gridManager.enableCellIds != null && gridManager.enableCellIds.Count > 0)
        {
            foreach (var cellId in gridManager.enableCellIds)
            {
                if (!forbidden.Contains(cellId))
                    availableCellIds.Add(cellId);
            }
        }

        // 2. Find all cellIds directly adjacent to showCellIdList
        HashSet<int> nearShowCellIds = new HashSet<int>();
        int[] dr = { -1, 1, 0, 0 }; // up, down, left, right
        int[] dc = { 0, 0, -1, 1 };
        foreach (var showCellId in gridManager.showCellIdList)
        {
            int row = showCellId / gridCols;
            int col = showCellId % gridCols;
            for (int i = 0; i < 4; i++)
            {
                int nr = row + dr[i];
                int nc = col + dc[i];
                if (nr >= 0 && nr < gridRows && nc >= 0 && nc < gridCols)
                {
                    int neighborId = nr * gridCols + nc;
                    if (!forbidden.Contains(neighborId))
                        nearShowCellIds.Add(neighborId);
                }
            }
        }

        // 3. Filter availableCellIds to only include those near showCellIdList
        List<int> obstacleCandidateCellIds = new List<int>();
        foreach (var cellId in availableCellIds)
        {
            if (nearShowCellIds.Contains(cellId))
                obstacleCandidateCellIds.Add(cellId);
        }

        // 4. Shuffle obstacleCandidateCellIds to ensure randomness
        for (int i = obstacleCandidateCellIds.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            int temp = obstacleCandidateCellIds[i];
            obstacleCandidateCellIds[i] = obstacleCandidateCellIds[j];
            obstacleCandidateCellIds[j] = temp;
        }

        // 5. Assign obstacles using the pool
        obstacleCellIds.Clear();
        int placed = 0;

        int randomObstacleCount = Random.Range(2, this.obstacleCount);
        for (int idx = 0; idx < randomObstacleCount; idx++)
        {
            if (idx < obstacleCandidateCellIds.Count)
            {
                int cellId = obstacleCandidateCellIds[idx];
                int row = cellId / gridCols;
                int col = cellId % gridCols;

                var obj = GetPooledObstacle(idx);
                obj.name = $"Obstacle_{idx}_{cellId}";

                RectTransform objRect = obj.GetComponent<RectTransform>();
                var cell = GameController.Instance.grid[row, col];
                if (objRect != null && cell != null)
                {
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    objRect.anchorMin = cellRect.anchorMin;
                    objRect.anchorMax = cellRect.anchorMax;
                    objRect.pivot = cellRect.pivot;
                    objRect.localScale = new Vector3(
                        Random.Range(0.7f, 1f),
                        Random.Range(0.7f, 1f),
                        Random.Range(0.7f, 1f)
                    );
                    objRect.sizeDelta = cellRect.sizeDelta;
                    objRect.anchoredPosition = cellRect.anchoredPosition;
                    float angle = UnityEngine.Random.Range(-30f, 30f);
                    objRect.localRotation = Quaternion.Euler(0, 0, angle);

                    var obstacleComponent = obj.GetComponent<Obstacle>();
                    if (obstacleComponent != null)
                    {
                        obstacleComponent.cellId = cellId;
                        obstacleComponent.ResetPosition(objRect.anchoredPosition);
                    }
                }

                var rawImage = obj.GetComponent<RawImage>();
                if (rawImage != null && Obstacles != null && Obstacles.Length > 0)
                {
                    int texIdx = UnityEngine.Random.Range(0, Obstacles.Length);
                    rawImage.texture = Obstacles[texIdx];
                }

                obstacleCellIds.Add(cellId);
                placed++;
            }
            else
            {
                if (idx < obstacles.Count)
                    obstacles[idx].SetActive(false);
            }
        }

        // 6. Place submitArea at a random available cell not used by obstacles and not adjacent to obstacles/selected cells
        if (this.submitPrefab != null && availableCellIds.Count > 0)
        {
            this.submitArea = this.submitArea == null ? Instantiate(this.submitPrefab, parent) : this.submitArea;

            // Get all selected cellIds
            HashSet<int> selectedCellIds = new HashSet<int>();
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridCols; c++)
                {
                    var cell = GameController.Instance.grid[r, c];
                    if (cell != null && cell.isSelected)
                        selectedCellIds.Add(cell.row * gridCols + cell.col);
                }
            }

            HashSet<int> obstacleSet = new HashSet<int>(obstacleCellIds);

            // Helper to check if a cell is adjacent to forbidden cells
            bool HasNeighbor(HashSet<int> forbidden, int cellId)
            {
                int row = cellId / gridCols;
                int col = cellId % gridCols;
                int[] dr2 = { -1, 1, 0, 0 };
                int[] dc2 = { 0, 0, -1, 1 };
                for (int i = 0; i < 4; i++)
                {
                    int nr = row + dr2[i];
                    int nc = col + dc2[i];
                    if (nr >= 0 && nr < gridRows && nc >= 0 && nc < gridCols)
                    {
                        int neighborId = nr * gridCols + nc;
                        if (forbidden.Contains(neighborId))
                            return true;
                    }
                }
                return false;
            }

            List<int> submitCandidateCellIds = new List<int>();
            foreach (var cellId in availableCellIds)
            {
                if (obstacleSet.Contains(cellId)) continue;
                if (selectedCellIds.Contains(cellId)) continue;
                if (HasNeighbor(obstacleSet, cellId)) continue;
                if (HasNeighbor(selectedCellIds, cellId)) continue;
                submitCandidateCellIds.Add(cellId);
            }

            if (submitCandidateCellIds.Count > 0)
            {
                int chosenCellId = submitCandidateCellIds[UnityEngine.Random.Range(0, submitCandidateCellIds.Count)];
                int row = chosenCellId / gridCols;
                int col = chosenCellId % gridCols;
                var cell = GameController.Instance.grid[row, col];
                RectTransform submitRect = submitArea.GetComponent<RectTransform>();
                if (cell != null && submitRect != null)
                {
                    RectTransform cellRect = cell.GetComponent<RectTransform>();
                    submitRect.anchorMin = cellRect.anchorMin;
                    submitRect.anchorMax = cellRect.anchorMax;
                    submitRect.pivot = cellRect.pivot;
                    submitRect.localScale = Vector3.one;
                    submitRect.sizeDelta = cellRect.sizeDelta;
                    submitRect.anchoredPosition = cellRect.anchoredPosition;
                }
            }
            else
            {
                // No valid position for submitArea
                submitArea.SetActive(false);
            }
        }
    }
}
