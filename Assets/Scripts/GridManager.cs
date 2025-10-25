using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class GridManager
{
    public GameObject cellPrefab;
    public GridLayoutGroup parent;
    public float originalParentWidth = 2260f;
    public float originalParentHeight = 1343.3f;
    public Vector2 originalCellSize;
    public Vector2 originalSpacing;
    public int gridRow = 4;
    public int gridColumn = 4;
    public int maxAttempts = 1000;
    private Cell[,] cells;
    public List<int> enableCellIds = new List<int>();
    /*public List<int> disableCellIds = new List<int>();*/
    public List<int> showCellIdList = new List<int>();
    private List<Vector2Int> availablePositions = null;
    public bool showQuestionWordPosition = false;
    public bool isMCType = false;
    public int gridCount = 0;
    public Texture[] cell_object_Textures;
    public string currentHiddenWrongWord = "";


    public Cell[,] CreateGrid()
    {
        if (this.parent != null)
        {
            RectTransform parentRect = this.parent.GetComponent<RectTransform>();
            float widthRatio = parentRect.rect.width / originalParentWidth;
            float heightRatio = parentRect.rect.height / originalParentHeight;

            LogController.Instance.debug($"Parent Rect Width: {parentRect.rect.width}, Height: {parentRect.rect.height}");
            // You can use the smaller ratio to maintain aspect, or apply each independently
            float scaleRatio = Mathf.Min(widthRatio, heightRatio);
            LogController.Instance.debug($"scaleRatio: {scaleRatio}");
            // Update cell size and spacing based on the ratio
            parent.cellSize = originalCellSize * scaleRatio;
            parent.spacing = originalSpacing * scaleRatio;
        }

        if (LoaderConfig.Instance != null && LoaderConfig.Instance.apiManager.IsLogined && LoaderConfig.Instance.gameSetup.object_item_images.Count > 0)
        {
            this.cell_object_Textures = LoaderConfig.Instance.gameSetup.object_item_images.ToArray();
        }

        this.cells = new Cell[this.gridRow, this.gridColumn];
        if (this.parent != null) this.parent.constraintCount = this.gridColumn;
        this.availablePositions = new List<Vector2Int>();
        for (int i = 0; i < this.gridRow; i++)
        {
            for (int j = 0; j < this.gridColumn; j++)
            {
                this.createCell(i, j);
            }
        }
        return this.cells;
    }

    public void ResetAllCellsToCenter()
    {
        if (this.parent != null)
            this.parent.enabled = false;

        for (int i = 0; i < this.gridRow; i++)
        {
            for (int j = 0; j < this.gridColumn; j++)
            {
                var cell = this.cells[i, j];
                if (cell != null)
                {
                    var rect = cell.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        Vector3 worldPos = rect.TransformPoint(rect.rect.center);

                        rect.anchorMin = new Vector2(0.5f, 0.5f);
                        rect.anchorMax = new Vector2(0.5f, 0.5f);
                        rect.pivot = new Vector2(0.5f, 0.5f);

                        var parentRect = this.parent.GetComponent<RectTransform>();
                        Canvas canvas = this.parent.GetComponentInParent<Canvas>();
                        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

                        Vector2 localPoint;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                            parentRect,
                            RectTransformUtility.WorldToScreenPoint(cam, worldPos),
                            cam,
                            out localPoint
                        );
                        rect.anchoredPosition = localPoint;
                    }
                }
            }
        }
    }

    private void createCell(int rowId, int columnId)
    {
        GameObject cellObject = GameObject.Instantiate(cellPrefab, this.parent != null ? this.parent.transform : null);
        cellObject.name = "Cell_" + this.gridCount;
        Cell cell = cellObject.GetComponent<Cell>();
        if(this.cell_object_Textures != null) {
            if(this.cell_object_Textures.Length > 0) cell.cellTextures = this.cell_object_Textures;
        }
        cell.SetTextContent("");
        cell.row = rowId;
        cell.col = columnId;
        this.cells[rowId, columnId] = cell;
        this.cells[rowId, columnId].cellId = this.gridCount;
        this.availablePositions.Add(new Vector2Int(rowId, columnId));
        this.gridCount += 1;
    }

    public List<int> GenerateUniqueRandomIntegers(int count, int minValue, int maxValue)
    {
        // fallback: enableCellIds empty/null 則用全部 cell
        List<int> candidateCellIds;
        if (enableCellIds == null || enableCellIds.Count == 0)
        {
            candidateCellIds = Enumerable.Range(0, this.gridRow * this.gridColumn).ToList();
        }
        else
        {
            candidateCellIds = new List<int>(enableCellIds);
        }

        if (count > candidateCellIds.Count)
            throw new ArgumentException("Count cannot be greater than the number of enabled cells.");

        HashSet<int> enableSet = new HashSet<int>(candidateCellIds);

        bool[,] blocked = new bool[this.gridRow, this.gridColumn];
        List<int> result = new List<int>();
        System.Random random = new System.Random();

        for (int attempt = 0; attempt < this.maxAttempts && result.Count < count; attempt++)
        {
            // 收集所有可用 candidates
            List<int> candidates = new List<int>();
            foreach (int cellId in enableSet)
            {
                int row = cellId / this.gridColumn;
                int col = cellId % this.gridColumn;
                if (!blocked[row, col])
                    candidates.Add(cellId);
            }

            if (candidates.Count == 0)
                break;

            int selId = candidates[random.Next(candidates.Count)];
            int rowSel = selId / this.gridColumn;
            int colSel = selId % this.gridColumn;
            result.Add(selId);

            // Block this cell and all 8 neighbors
            for (int dr = -1; dr <= 1; dr++)
                for (int dc = -1; dc <= 1; dc++)
                {
                    int nr = rowSel + dr;
                    int nc = colSel + dc;
                    if (nr >= 0 && nr < this.gridRow && nc >= 0 && nc < this.gridColumn)
                        blocked[nr, nc] = true;
                }
        }

        if (result.Count < count)
            throw new InvalidOperationException("Unable to generate enough unique IDs with the given constraints.");

        // Debug check for adjacency
        for (int i = 0; i < result.Count; i++)
        {
            int idA = result[i];
            int rowA = idA / this.gridColumn;
            int colA = idA % this.gridColumn;
            for (int j = i + 1; j < result.Count; j++)
            {
                int idB = result[j];
                int rowB = idB / this.gridColumn;
                int colB = idB % this.gridColumn;
                if (Mathf.Abs(rowA - rowB) <= 1 && Mathf.Abs(colA - colB) <= 1)
                {
                    LogController.Instance.debugError($"鄰近cell: {idA}({rowA},{colA}) 和 {idB}({rowB},{colB})");
                }
            }
        }

        return result;
    }

    char[] ShuffleStringToCharArray(string input)
    {
        char[] letters = input.ToCharArray();
        System.Random random = new System.Random();
        letters = letters.OrderBy(x => random.Next()).ToArray();

        return letters;
    }
    public void UpdateGridWithWord(string[] newMultipleWords=null, string newWord=null, Action onCompleted = null)
    {
       this.PlaceWordInGrid(newMultipleWords, newWord, ()=>
       {
           onCompleted?.Invoke();
       });
    }

    public void setAllCellsStatus(bool status = false)
    {
        foreach (var cell in cells)
        {
            if (!cell.isSelected)
            {
                cell.setCellDebugStatus(status);
                cell.setCellStatus(status);
            }
        }
    }

    public void RemoveOneWrongMCOption(string[] multipleWords, string correctWord, Action onCompleted = null)
    {
        this.currentHiddenWrongWord = "";
        if (!isMCType || multipleWords == null || multipleWords.Length <= 1)
            return;

        // Find indices of wrong answers
        List<int> wrongIndices = new List<int>();
        for (int i = 0; i < multipleWords.Length; i++)
        {
            if (!string.Equals(multipleWords[i], correctWord, StringComparison.Ordinal))
                wrongIndices.Add(i);
        }

        if (wrongIndices.Count == 0)
            return;

        // Randomly pick one wrong answer to hide
        System.Random rnd = new System.Random();
        int hideIdx = wrongIndices[rnd.Next(wrongIndices.Count)];
        int cellId = this.showCellIdList[hideIdx];
        int row = cellId / this.gridColumn;
        int col = cellId % this.gridColumn;
        this.currentHiddenWrongWord = this.cells[row, col].content.text.ToLower();
        this.cells[row, col].SetTextStatus(false, 0f);
        onCompleted?.Invoke();
    }


    void PlaceWordInGrid(string[] multipleWords = null, string spellWord = null, Action onCompleted = null)
    {
        char[] letters = null;
        if (multipleWords != null && multipleWords.Length > 0)
        {
            this.isMCType = true;
        }

        if (!string.IsNullOrEmpty(spellWord))
        {
            letters = this.ShuffleStringToCharArray(spellWord);
            this.isMCType = false;
        }

        for (int i = 0; i < this.gridRow; i++)
        {
            for (int j = 0; j < this.gridColumn; j++)
            {
                this.cells[i, j].SetTextContent("");
                var rect = this.cells[i, j].GetComponent<RectTransform>();
                if (rect != null)
                    rect.localRotation = Quaternion.identity;
            }
        }

        this.showCellIdList = this.GenerateUniqueRandomIntegers(this.isMCType ? multipleWords.Length : letters.Length,
                                                                0,
                                                                cells.Length);
        System.Random rnd = new System.Random();
        for (int i = 0; i < this.showCellIdList.Count; i++)
        {
            int cellId = this.showCellIdList[i];
            int row = cellId / this.gridColumn;
            int col = cellId % this.gridColumn;
            this.cells[row, col].SetTextContent(this.isMCType ? multipleWords[i] : letters[i].ToString());

            var rect = this.cells[row, col].GetComponent<RectTransform>();
            if (rect != null)
            {
                float angle = (float)(rnd.NextDouble() * 60.0 - 30.0); // -30~30
                rect.localRotation = Quaternion.Euler(0, 0, angle);
            }
        }

        onCompleted?.Invoke();
    }



}
