using UnityEngine;
using UnityEditor;
using Data;
using System.Collections.Generic;

namespace EditorTools
{
    public class LevelGeneratorWindow : EditorWindow
    {
        private enum Difficulty { Easy, Medium, Hard, Expert }

        private Difficulty selectedDifficulty = Difficulty.Easy;
        private LevelData generatedLevelData;
        private string levelName = "GeneratedLevel";

        private const float CellSize = 36f;
        private const float Padding = 20f;
        private Vector2 scrollPosition;
        private float zoomScale = 1.0f;

        [System.Serializable]
        private class DifficultyParams
        {
            public int gridXMin = 5;
            public int gridXMax = 7;
            public int gridYMin = 5;
            public int gridYMax = 7;
            public int arrowCountMin = 2;
            public int arrowCountMax = 4;
            public int arrowLenMin = 3;
            public int arrowLenMax = 5;
            public int maxTurns = 1;
            public int maxMovesOffsetMin = 2;
            public int maxMovesOffsetMax = 4;
        }

        private Dictionary<Difficulty, DifficultyParams> diffParams;
        private bool showParams = false;
        private bool maximizeFill = true;

        [MenuItem("Window/Arrow Puzzle Level Generator")]
        public static void ShowWindow()
        {
            GetWindow<LevelGeneratorWindow>("Level Generator");
        }

        private void OnEnable()
        {
            diffParams = new Dictionary<Difficulty, DifficultyParams>
            {
                { Difficulty.Easy, new DifficultyParams
                    {
                        gridXMin = 5, gridXMax = 7,
                        gridYMin = 5, gridYMax = 7,
                        arrowCountMin = 2, arrowCountMax = 4,
                        arrowLenMin = 3, arrowLenMax = 5,
                        maxTurns = 1,
                        maxMovesOffsetMin = 2, maxMovesOffsetMax = 4
                    }
                },
                { Difficulty.Medium, new DifficultyParams
                    {
                        gridXMin = 8, gridXMax = 11,
                        gridYMin = 8, gridYMax = 11,
                        arrowCountMin = 5, arrowCountMax = 9,
                        arrowLenMin = 4, arrowLenMax = 8,
                        maxTurns = 2,
                        maxMovesOffsetMin = 2, maxMovesOffsetMax = 5
                    }
                },
                { Difficulty.Hard, new DifficultyParams
                    {
                        gridXMin = 12, gridXMax = 16,
                        gridYMin = 12, gridYMax = 16,
                        arrowCountMin = 10, arrowCountMax = 17,
                        arrowLenMin = 5, arrowLenMax = 12,
                        maxTurns = 4,
                        maxMovesOffsetMin = 3, maxMovesOffsetMax = 6
                    }
                },
                { Difficulty.Expert, new DifficultyParams
                    {
                        gridXMin = 16, gridXMax = 22,
                        gridYMin = 16, gridYMax = 22,
                        arrowCountMin = 15, arrowCountMax = 25,
                        arrowLenMin = 7, arrowLenMax = 18,
                        maxTurns = 6,
                        maxMovesOffsetMin = 4, maxMovesOffsetMax = 8
                    }
                }
            };
        }

        private void OnGUI()
        {
            GUILayout.Label("Level Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            selectedDifficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", selectedDifficulty);

            EditorGUILayout.Space();

            showParams = EditorGUILayout.Foldout(showParams, $"Adjust {selectedDifficulty} Parameters", true);
            if (showParams)
            {
                DifficultyParams p = diffParams[selectedDifficulty];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
                DrawIntRange("X", ref p.gridXMin, ref p.gridXMax, 3, 30);
                DrawIntRange("Y", ref p.gridYMin, ref p.gridYMax, 3, 30);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Arrow Count", EditorStyles.boldLabel);
                DrawIntRange("Count", ref p.arrowCountMin, ref p.arrowCountMax, 1, 30);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Arrow Length (cells)", EditorStyles.boldLabel);
                DrawIntRange("Length", ref p.arrowLenMin, ref p.arrowLenMax, 2, 20);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Max Turns", EditorStyles.boldLabel);
                p.maxTurns = EditorGUILayout.IntSlider(p.maxTurns, 0, 8);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Max Moves Offset (arrows + offset)", EditorStyles.boldLabel);
                DrawIntRange("Offset", ref p.maxMovesOffsetMin, ref p.maxMovesOffsetMax, 0, 15);

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            maximizeFill = EditorGUILayout.Toggle("Maximize Grid Fill", maximizeFill);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate Level", GUILayout.Height(40)))
            {
                GenerateLevel();
            }

            EditorGUILayout.Space();

            if (generatedLevelData == null)
            {
                EditorGUILayout.HelpBox("Click 'Generate Level' to create a new level.", MessageType.Info);
                return;
            }

            DrawLevelInfo();
            DrawGridPreview();
            DrawSaveButton();
        }

        private void DrawLevelInfo()
        {
            GUILayout.Label("Generated Level Info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Grid Size", $"{generatedLevelData.gridDimensions.x} x {generatedLevelData.gridDimensions.y}");
            EditorGUILayout.LabelField("Arrows", generatedLevelData.arrows.Count.ToString());
            EditorGUILayout.LabelField("Max Moves", generatedLevelData.maxMoves.ToString());

            EditorGUILayout.Space();
            GUILayout.Label("Preview (scroll to zoom):", EditorStyles.boldLabel);
            HandleZoomInput();
        }

        private void HandleZoomInput()
        {
            Event e = Event.current;
            if (e.type == EventType.ScrollWheel && e.control)
            {
                float zoomDelta = -e.delta.y * 0.05f;
                zoomScale = Mathf.Clamp(zoomScale + zoomDelta, 0.3f, 3.0f);
                e.Use();
                Repaint();
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Zoom:");
            zoomScale = EditorGUILayout.Slider(zoomScale, 0.3f, 3.0f);
            GUILayout.EndHorizontal();
        }

        private void DrawGridPreview()
        {
            float effectiveCellSize = CellSize * zoomScale;
            float gridWidth = generatedLevelData.gridDimensions.x * effectiveCellSize;
            float gridHeight = generatedLevelData.gridDimensions.y * effectiveCellSize;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            Rect canvasRect = GUILayoutUtility.GetRect(gridWidth + Padding * 2, gridHeight + Padding * 2);
            Rect gridRect = new Rect(Padding, Padding, gridWidth, gridHeight);

            EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.15f));

            for (int x = 0; x <= generatedLevelData.gridDimensions.x; x++)
            {
                Rect lineRect = new Rect(gridRect.x + x * effectiveCellSize, gridRect.y, 1, gridRect.height);
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
            }
            for (int y = 0; y <= generatedLevelData.gridDimensions.y; y++)
            {
                Rect lineRect = new Rect(gridRect.x, gridRect.y + y * effectiveCellSize, gridRect.width, 1);
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
            }

            for (int i = 0; i < generatedLevelData.arrows.Count; i++)
            {
                DrawArrowVisual(generatedLevelData.arrows[i], gridRect, effectiveCellSize, i);
            }

            GUILayout.EndScrollView();
        }

        private void DrawArrowVisual(ArrowDefinition arrow, Rect gridRect, float cellSize, int index)
        {
            if (arrow.occupiedPositions == null || arrow.occupiedPositions.Count == 0) return;

            Handles.color = arrow.arrowColor;
            for (int i = 0; i < arrow.occupiedPositions.Count; i++)
            {
                Vector2Int pos = arrow.occupiedPositions[i];
                float guiX = gridRect.x + pos.x * cellSize;
                float guiY = gridRect.y + (generatedLevelData.gridDimensions.y - 1 - pos.y) * cellSize;

                Rect cellRect = new Rect(guiX + 2, guiY + 2, cellSize - 4, cellSize - 4);
                EditorGUI.DrawRect(cellRect, arrow.arrowColor);

                if (i > 0)
                {
                    Vector2Int prevPos = arrow.occupiedPositions[i - 1];
                    float pGuiX = gridRect.x + prevPos.x * cellSize;
                    float pGuiY = gridRect.y + (generatedLevelData.gridDimensions.y - 1 - prevPos.y) * cellSize;

                    float minX = Mathf.Min(guiX, pGuiX) + 2;
                    float maxX = Mathf.Max(guiX + cellSize, pGuiX + cellSize) - 2;
                    float minY = Mathf.Min(guiY, pGuiY) + 2;
                    float maxY = Mathf.Max(guiY + cellSize, pGuiY + cellSize) - 2;

                    EditorGUI.DrawRect(new Rect(minX, minY, maxX - minX, maxY - minY), arrow.arrowColor);
                }
            }

            {
                Vector2Int headPos = arrow.occupiedPositions[0];
                float guiX = gridRect.x + headPos.x * cellSize;
                float guiY = gridRect.y + (generatedLevelData.gridDimensions.y - 1 - headPos.y) * cellSize;

                Vector2Int dir = Vector2Int.up;
                if (arrow.occupiedPositions.Count > 1)
                {
                    dir = arrow.occupiedPositions[0] - arrow.occupiedPositions[1];
                }

                Handles.color = Color.white;
                Vector3 center = new Vector3(guiX + cellSize / 2f, guiY + cellSize / 2f, 0);
                float size = cellSize * 0.28f;
                Vector3 p1, p2, p3;

                if (dir.x > 0)
                {
                    p1 = center + new Vector3(size, 0, 0);
                    p2 = center + new Vector3(-size, size, 0);
                    p3 = center + new Vector3(-size, -size, 0);
                }
                else if (dir.x < 0)
                {
                    p1 = center + new Vector3(-size, 0, 0);
                    p2 = center + new Vector3(size, size, 0);
                    p3 = center + new Vector3(size, -size, 0);
                }
                else if (dir.y < 0)
                {
                    p1 = center + new Vector3(0, size, 0);
                    p2 = center + new Vector3(-size, -size, 0);
                    p3 = center + new Vector3(size, -size, 0);
                }
                else
                {
                    p1 = center + new Vector3(0, -size, 0);
                    p2 = center + new Vector3(-size, size, 0);
                    p3 = center + new Vector3(size, size, 0);
                }

                Handles.DrawAAConvexPolygon(new Vector3[] { p1, p2, p3 });
            }

            {
                Vector2Int headPos = arrow.occupiedPositions[0];
                float hGuiX = gridRect.x + headPos.x * cellSize + cellSize / 2f - 8;
                float hGuiY = gridRect.y + (generatedLevelData.gridDimensions.y - 1 - headPos.y) * cellSize - 2;
                Handles.Label(new Vector3(hGuiX, hGuiY, 0), $"{index + 1}");
            }
        }

        private void DrawSaveButton()
        {
            EditorGUILayout.Space();
            levelName = EditorGUILayout.TextField("Level Name", levelName);

            if (GUILayout.Button("Save Level Asset", GUILayout.Height(35)))
            {
                SaveLevelData();
            }
        }

        private void GenerateLevel()
        {
            generatedLevelData = ScriptableObject.CreateInstance<LevelData>();

            DifficultyParams p = diffParams[selectedDifficulty];

            int gridX = Random.Range(p.gridXMin, p.gridXMax + 1);
            int gridY = Random.Range(p.gridYMin, p.gridYMax + 1);
            int numArrows = Random.Range(p.arrowCountMin, p.arrowCountMax + 1);

            generatedLevelData.gridDimensions = new Vector2Int(gridX, gridY);
            generatedLevelData.defaultCameraSize = Mathf.Max(gridX, gridY) * 1.8f;
            generatedLevelData.minZoom = 5;
            generatedLevelData.maxZoom = 30;
            generatedLevelData.gameWinMode = Core.LevelManager.GameWinMode.Moves;

            int maxRetries = 20;
            bool success = false;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                gridOccupancy = new bool[gridX * gridY];
                generatedLevelData.arrows.Clear();

                if (GenerateArrows(gridX, gridY, numArrows, p))
                {
                    mainArrowCount = generatedLevelData.arrows.Count;
                    generatedLevelData.maxMoves = numArrows + Random.Range(p.maxMovesOffsetMin, p.maxMovesOffsetMax + 1);

                    if (!HasHeadToHead() && !HasDeadlock(gridX, gridY))
                    {
                        if (maximizeFill)
                        {
                            FillRemainingCells(gridX, gridY);
                            if (HasHeadToHead() || HasDeadlock(gridX, gridY))
                            {
                                RevertFillArrows(gridX);
                            }
                        }
                        success = true;
                        break;
                    }
                }
            }

            if (!success)
            {
                generatedLevelData.arrows.Clear();
            }

            EditorUtility.SetDirty(generatedLevelData);
            Repaint();
        }

        private bool[] gridOccupancy;
        private int mainArrowCount;

        private bool GenerateArrows(int gridX, int gridY, int numArrows, DifficultyParams p)
        {
            Color[] palette = new Color[]
            {
                Color.red,
                new Color(0.20f, 1f, 0f),
                Color.blue,
                new Color(1f, 0.69f, 0f),
                new Color(0.50f, 0f, 1f),
                new Color(1f, 0.41f, 0.71f),
                new Color(0f, 0.80f, 0.80f),
                Color.yellow,
                new Color(0.30f, 0.70f, 1f),
                new Color(0.80f, 0.40f, 0.20f),
            };

            int placedArrows = 0;
            int maxAttempts = 100;

            for (int a = 0; a < numArrows; a++)
            {
                bool placed = false;
                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    List<Vector2Int> arrow = GenerateSingleArrow(gridX, gridY, p);
                    if (arrow != null && arrow.Count >= 2 && !SelfBlocks(arrow, gridX, gridY))
                    {
                        generatedLevelData.arrows.Add(new ArrowDefinition
                        {
                            occupiedPositions = arrow,
                            arrowColor = palette[placedArrows % palette.Length]
                        });
                        placedArrows++;
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    for (int i = generatedLevelData.arrows.Count - 1; i >= 0; i--)
                    {
                        ClearArrowOccupancy(generatedLevelData.arrows[i], gridX);
                    }
                    generatedLevelData.arrows.Clear();
                    return false;
                }
            }

            return generatedLevelData.arrows.Count >= 2;
        }

        private void ClearArrowOccupancy(ArrowDefinition arrow, int gridX)
        {
            if (arrow.occupiedPositions == null) return;
            foreach (var pos in arrow.occupiedPositions)
            {
                gridOccupancy[pos.y * gridX + pos.x] = false;
            }
        }

        private List<Vector2Int> GenerateSingleArrow(int gridX, int gridY, DifficultyParams p)
        {
            List<Vector2Int> emptyCells = new List<Vector2Int>();
            for (int x = 0; x < gridX; x++)
                for (int y = 0; y < gridY; y++)
                    if (!gridOccupancy[y * gridX + x])
                        emptyCells.Add(new Vector2Int(x, y));

            if (emptyCells.Count == 0) return null;

            Vector2Int head = emptyCells[Random.Range(0, emptyCells.Count)];

            int targetLength = Random.Range(p.arrowLenMin, p.arrowLenMax + 1);
            int maxTurns = p.maxTurns;

            List<Vector2Int> positions = new List<Vector2Int> { head };
            gridOccupancy[head.y * gridX + head.x] = true;

            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            List<Vector2Int> validDirs = new List<Vector2Int>();
            foreach (var d in directions)
            {
                Vector2Int next = head + d;
                if (IsInGrid(next, gridX, gridY) && !gridOccupancy[next.y * gridX + next.x])
                    validDirs.Add(d);
            }
            if (validDirs.Count == 0) return null;

            Vector2Int dir = validDirs[Random.Range(0, validDirs.Count)];
            int turns = 0;

            while (positions.Count < targetLength)
            {
                Vector2Int next = positions[positions.Count - 1] + dir;

                if (!IsInGrid(next, gridX, gridY) || gridOccupancy[next.y * gridX + next.x])
                {
                    if (turns >= maxTurns) break;

                    List<Vector2Int> turnDirs = new List<Vector2Int>();
                    foreach (var d in directions)
                    {
                        if (d == dir || d == -dir) continue;
                        Vector2Int turnNext = positions[positions.Count - 1] + d;
                        if (IsInGrid(turnNext, gridX, gridY) && !gridOccupancy[turnNext.y * gridX + turnNext.x])
                            turnDirs.Add(d);
                    }

                    if (turnDirs.Count == 0) break;

                    dir = turnDirs[Random.Range(0, turnDirs.Count)];
                    turns++;
                    continue;
                }

                positions.Add(next);
                gridOccupancy[next.y * gridX + next.x] = true;
            }

            return positions;
        }

        private bool IsInGrid(Vector2Int pos, int gridX, int gridY)
        {
            return pos.x >= 0 && pos.x < gridX && pos.y >= 0 && pos.y < gridY;
        }

        private void DrawIntRange(string label, ref int minVal, ref int maxVal, int limitMin, int limitMax)
        {
            float fMin = minVal;
            float fMax = maxVal;
            EditorGUILayout.MinMaxSlider(label, ref fMin, ref fMax, limitMin, limitMax);
            minVal = Mathf.RoundToInt(fMin);
            maxVal = Mathf.RoundToInt(fMax);
            if (minVal < limitMin) minVal = limitMin;
            if (maxVal > limitMax) maxVal = limitMax;
            EditorGUILayout.LabelField($"  {minVal}  —  {maxVal}");
        }

        private bool SelfBlocks(List<Vector2Int> positions, int gridX, int gridY)
        {
            if (positions.Count < 2) return false;
            Vector2Int head = positions[0];
            Vector2Int dir = head - positions[1];

            if (dir == Vector2Int.zero) return true;

            Vector2Int check = head + dir;
            while (IsInGrid(check, gridX, gridY))
            {
                for (int i = 1; i < positions.Count; i++)
                {
                    if (positions[i] == check) return true;
                }
                check += dir;
            }
            return false;
        }

        private bool HasHeadToHead()
        {
            List<ArrowDefinition> arrows = generatedLevelData.arrows;
            for (int i = 0; i < arrows.Count; i++)
            {
                Vector2Int headA = arrows[i].occupiedPositions[0];
                Vector2Int dirA = arrows[i].occupiedPositions.Count > 1
                    ? headA - arrows[i].occupiedPositions[1] : Vector2Int.zero;

                for (int j = i + 1; j < arrows.Count; j++)
                {
                    Vector2Int headB = arrows[j].occupiedPositions[0];
                    Vector2Int dirB = arrows[j].occupiedPositions.Count > 1
                        ? headB - arrows[j].occupiedPositions[1] : Vector2Int.zero;

                    if (dirA == -dirB && AreOnSameAxis(headA, headB, dirA))
                    {
                        Vector2Int diff = headB - headA;
                        if ((dirA.x != 0 && diff.x * dirA.x > 0) ||
                            (dirA.y != 0 && diff.y * dirA.y > 0))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool AreOnSameAxis(Vector2Int a, Vector2Int b, Vector2Int dir)
        {
            if (dir.x != 0) return a.y == b.y;
            if (dir.y != 0) return a.x == b.x;
            return false;
        }

        private bool HasDeadlock(int gridX, int gridY)
        {
            List<ArrowDefinition> arrows = generatedLevelData.arrows;
            int n = arrows.Count;
            List<int>[] blockedBy = new List<int>[n];
            for (int i = 0; i < n; i++) blockedBy[i] = new List<int>();

            for (int i = 0; i < n; i++)
            {
                Vector2Int head = arrows[i].occupiedPositions[0];
                Vector2Int dir = arrows[i].occupiedPositions.Count > 1
                    ? head - arrows[i].occupiedPositions[1] : Vector2Int.zero;

                if (dir == Vector2Int.zero) continue;

                Vector2Int check = head + dir;
                while (IsInGrid(check, gridX, gridY))
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i == j) continue;
                        for (int k = 1; k < arrows[j].occupiedPositions.Count; k++)
                        {
                            if (arrows[j].occupiedPositions[k] == check)
                            {
                                if (!blockedBy[i].Contains(j)) blockedBy[i].Add(j);
                            }
                        }
                    }
                    check += dir;
                }
            }

            bool hasFreeArrow = false;
            for (int i = 0; i < n; i++)
            {
                if (blockedBy[i].Count == 0) { hasFreeArrow = true; break; }
            }
            if (!hasFreeArrow) return true;

            bool[] visited = new bool[n];
            bool[] inStack = new bool[n];
            for (int i = 0; i < n; i++)
            {
                if (HasCycle(i, blockedBy, visited, inStack)) return true;
            }

            return false;
        }

        private bool HasCycle(int node, List<int>[] graph, bool[] visited, bool[] inStack)
        {
            if (inStack[node]) return true;
            if (visited[node]) return false;
            visited[node] = true;
            inStack[node] = true;
            foreach (int neighbor in graph[node])
            {
                if (HasCycle(neighbor, graph, visited, inStack)) return true;
            }
            inStack[node] = false;
            return false;
        }

        private void FillRemainingCells(int gridX, int gridY)
        {
            Color[] fillColors = new Color[]
            {
                new Color(1f, 0.5f, 0f),
                new Color(0.3f, 0.7f, 1f),
                new Color(1f, 0.3f, 0.7f),
                new Color(0.7f, 0.7f, 0.7f),
            };

            int colorIdx = 0;
            int placed = 0;
            int maxFill = 50;

            for (int attempt = 0; attempt < maxFill; attempt++)
            {
                List<Vector2Int> emptyCells = new List<Vector2Int>();
                for (int x = 0; x < gridX; x++)
                    for (int y = 0; y < gridY; y++)
                        if (!gridOccupancy[y * gridX + x])
                            emptyCells.Add(new Vector2Int(x, y));

                if (emptyCells.Count < 2) break;

                Vector2Int start = emptyCells[Random.Range(0, emptyCells.Count)];
                List<Vector2Int> dirs = new List<Vector2Int>
                {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
                };

                Vector2Int chosenDir = Vector2Int.zero;
                foreach (var d in dirs)
                {
                    Vector2Int next = start + d;
                    if (IsInGrid(next, gridX, gridY) && !gridOccupancy[next.y * gridX + next.x])
                    {
                        chosenDir = d;
                        break;
                    }
                }
                if (chosenDir == Vector2Int.zero) continue;

                List<Vector2Int> fillPos = new List<Vector2Int> { start, start + chosenDir };
                gridOccupancy[start.y * gridX + start.x] = true;
                gridOccupancy[(start + chosenDir).y * gridX + (start + chosenDir).x] = true;

                Vector2Int[] perpDirs = new Vector2Int[]
                {
                    new Vector2Int(chosenDir.y, chosenDir.x),
                    new Vector2Int(-chosenDir.y, -chosenDir.x)
                };
                foreach (var pd in perpDirs)
                {
                    Vector2Int third = start + chosenDir + pd;
                    if (third != start && IsInGrid(third, gridX, gridY) && !gridOccupancy[third.y * gridX + third.x])
                    {
                        fillPos.Add(third);
                        gridOccupancy[third.y * gridX + third.x] = true;
                        break;
                    }
                }

                if (!SelfBlocks(fillPos, gridX, gridY))
                {
                    generatedLevelData.arrows.Add(new ArrowDefinition
                    {
                        occupiedPositions = fillPos,
                        arrowColor = fillColors[colorIdx % fillColors.Length]
                    });
                    colorIdx++;
                    placed++;
                }
                else
                {
                    foreach (var pos in fillPos)
                        gridOccupancy[pos.y * gridX + pos.x] = false;
                }
            }
        }

        private void RevertFillArrows(int gridX)
        {
            while (generatedLevelData.arrows.Count > mainArrowCount)
            {
                ArrowDefinition arrow = generatedLevelData.arrows[generatedLevelData.arrows.Count - 1];
                generatedLevelData.arrows.RemoveAt(generatedLevelData.arrows.Count - 1);
                if (arrow.occupiedPositions != null)
                {
                    foreach (var pos in arrow.occupiedPositions)
                        gridOccupancy[pos.y * gridX + pos.x] = false;
                }
            }
        }

        private void SaveLevelData()
        {
            if (generatedLevelData == null) return;

            string path = EditorUtility.SaveFilePanelInProject("Save Level Data", levelName, "asset", "Save generated level data");
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(generatedLevelData, path);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = generatedLevelData;
            }
        }
    }
}
