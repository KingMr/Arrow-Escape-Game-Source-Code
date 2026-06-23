using UnityEngine;
using UnityEditor;
using Data;
using System.Collections.Generic;

namespace EditorTools
{
    public class LevelImporterWindow : EditorWindow
    {
        private string jsonText = "";
        private Vector2 scrollPos;
        private LevelData previewLevel;
        private List<string> validationErrors = new List<string>();
        private bool isValid = false;
        private string lastFileName = "ImportedLevel";
        private bool showRawJson = false;

        private const float CellSize = 36f;
        private const float Padding = 20f;

        [System.Serializable]
        private class JsonLevelData
        {
            public JsonVector2Int gridDimensions;
            public int cols;
            public int rows;
            public int maxMoves;
            public float defaultCameraSize;
            public float minZoom = 2;
            public float maxZoom = 30;
            public int gameWinMode;
            public JsonArrow[] arrows;
        }

        [System.Serializable]
        private class JsonArrow
        {
            public JsonVector2Int[] occupiedPositions;
            public JsonColor arrowColor;
        }

        [System.Serializable]
        private class JsonVector2Int
        {
            public int x;
            public int y;
        }

        [System.Serializable]
        private class JsonColor
        {
            public float r = 1;
            public float g = 0;
            public float b = 0;
            public float a = 1;
        }

        [MenuItem("Window/Arrow Puzzle Level Importer")]
        public static void ShowWindow()
        {
            GetWindow<LevelImporterWindow>("Level Importer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Level JSON Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load JSON File", GUILayout.Height(30)))
            {
                LoadJsonFile();
            }
            if (GUILayout.Button("Clear", GUILayout.Height(30)))
            {
                jsonText = "";
                previewLevel = null;
                validationErrors.Clear();
                isValid = false;
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            showRawJson = EditorGUILayout.Foldout(showRawJson, "Raw JSON", true);
            if (showRawJson)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(150));
                jsonText = EditorGUILayout.TextArea(jsonText, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Parse & Validate", GUILayout.Height(30)))
            {
                ParseAndValidate();
            }

            EditorGUILayout.Space();

            if (validationErrors.Count > 0)
            {
                var msg = string.Join("\n", validationErrors);
                EditorGUILayout.HelpBox(msg, MessageType.Error);
            }
            else if (isValid && previewLevel != null)
            {
                EditorGUILayout.HelpBox("Level is valid! Scroll down to save.", MessageType.Info);
            }

            if (previewLevel != null)
            {
                EditorGUILayout.Space();
                DrawLevelPreview();
                DrawSaveSection();
            }
        }

        private void LoadJsonFile()
        {
            string path = EditorUtility.OpenFilePanel("Select Level JSON", "", "json");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                jsonText = System.IO.File.ReadAllText(path);
                lastFileName = System.IO.Path.GetFileNameWithoutExtension(path);
                ParseAndValidate();
            }
            catch (System.Exception e)
            {
                validationErrors.Clear();
                validationErrors.Add("Error reading file: " + e.Message);
                isValid = false;
            }
        }

        private void ParseAndValidate()
        {
            validationErrors.Clear();
            previewLevel = null;
            isValid = false;

            if (string.IsNullOrWhiteSpace(jsonText))
            {
                validationErrors.Add("No JSON data provided.");
                return;
            }

            try
            {
                previewLevel = ScriptableObject.CreateInstance<LevelData>();
                ParseJsonIntoLevel(jsonText, previewLevel);
                RunValidation(previewLevel);
                isValid = validationErrors.Count == 0;
            }
            catch (System.Exception e)
            {
                validationErrors.Clear();
                validationErrors.Add("Parse error: " + e.Message);
                isValid = false;
            }

            Repaint();
        }

        private void ParseJsonIntoLevel(string json, LevelData level)
        {
            var parsed = JsonUtility.FromJson<JsonLevelData>(json);
            if (parsed == null)
                throw new System.Exception("Could not parse JSON. Check format.");

            int cols = 5, rows = 5;
            if (parsed.gridDimensions != null)
            {
                cols = parsed.gridDimensions.x;
                rows = parsed.gridDimensions.y;
            }
            if (parsed.cols > 0) cols = parsed.cols;
            if (parsed.rows > 0) rows = parsed.rows;

            level.gridDimensions = new Vector2Int(cols, rows);
            level.defaultCameraSize = parsed.defaultCameraSize > 0 ? parsed.defaultCameraSize : Mathf.Max(cols, rows) * 4f;
            level.minZoom = parsed.minZoom > 0 ? parsed.minZoom : 2;
            level.maxZoom = parsed.maxZoom > 0 ? parsed.maxZoom : Mathf.Max(cols, rows) * 6f;
            level.maxMoves = parsed.maxMoves;
            level.gameWinMode = (Core.LevelManager.GameWinMode)parsed.gameWinMode;

            if (parsed.arrows == null || parsed.arrows.Length == 0)
                throw new System.Exception("No arrows found in JSON.");

            foreach (var jsonArrow in parsed.arrows)
            {
                if (jsonArrow.occupiedPositions == null || jsonArrow.occupiedPositions.Length < 2)
                    throw new System.Exception("Arrow must have at least 2 positions.");

                var def = new ArrowDefinition();
                def.occupiedPositions = new List<Vector2Int>();
                def.arrowColor = Color.red;

                foreach (var p in jsonArrow.occupiedPositions)
                {
                    def.occupiedPositions.Add(new Vector2Int(p.x, p.y));
                }

                if (jsonArrow.arrowColor != null)
                {
                    def.arrowColor = new Color(
                        jsonArrow.arrowColor.r,
                        jsonArrow.arrowColor.g,
                        jsonArrow.arrowColor.b,
                        jsonArrow.arrowColor.a
                    );
                }

                level.arrows.Add(def);
            }

            if (level.maxMoves <= 0)
                level.maxMoves = level.arrows.Count + 3;
        }

        private void RunValidation(LevelData level)
        {
            var arrows = level.arrows;
            int cols = level.gridDimensions.x;
            int rows = level.gridDimensions.y;

            if (arrows.Count < 1)
            {
                validationErrors.Add("Level must have at least 1 arrow.");
                return;
            }

            // Per-arrow checks
            for (int i = 0; i < arrows.Count; i++)
            {
                var a = arrows[i];
                if (a.occupiedPositions.Count < 2)
                    validationErrors.Add($"Arrow {i+1}: only {a.occupiedPositions.Count} cell(s), minimum 2 required.");

                var seen = new HashSet<Vector2Int>();
                foreach (var p in a.occupiedPositions)
                {
                    if (seen.Contains(p))
                        validationErrors.Add($"Arrow {i+1}: duplicate position ({p.x},{p.y}).");
                    seen.Add(p);

                    if (p.x < 0 || p.x >= cols || p.y < 0 || p.y >= rows)
                        validationErrors.Add($"Arrow {i+1}: position ({p.x},{p.y}) is out of bounds ({cols}x{rows}).");
                }

                for (int j = 1; j < a.occupiedPositions.Count; j++)
                {
                    var prev = a.occupiedPositions[j-1];
                    var curr = a.occupiedPositions[j];
                    int dist = Mathf.Abs(prev.x - curr.x) + Mathf.Abs(prev.y - curr.y);
                    if (dist != 1)
                        validationErrors.Add($"Arrow {i+1}: gap between ({prev.x},{prev.y}) and ({curr.x},{curr.y}).");
                }
            }

            // Rule 2: Overlap between arrows
            var allCells = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < arrows.Count; i++)
            {
                foreach (var p in arrows[i].occupiedPositions)
                {
                    if (allCells.ContainsKey(p))
                    {
                        string msg = $"Overlap at ({p.x},{p.y}) between arrows {allCells[p]+1} & {i+1}.";
                        if (!validationErrors.Contains(msg))
                            validationErrors.Add(msg);
                    }
                    else
                    {
                        allCells[p] = i;
                    }
                }
            }

            // Rule 4: No self-block — check full path against original body
            // The game's CheckPathClear inspects ALL occupied cells at once (before clearing tail),
            // so we check the entire path from head+dir against the arrow's current positions.
            for (int i = 0; i < arrows.Count; i++)
            {
                var a = arrows[i];
                if (a.occupiedPositions.Count < 2) continue;
                var head = a.occupiedPositions[0];
                var dir = head - a.occupiedPositions[1];
                if (dir == Vector2Int.zero) continue;

                var bodySet = new HashSet<Vector2Int>(a.occupiedPositions);
                Vector2Int check = head + dir;
                int step = 0;
                while (check.x >= 0 && check.x < cols && check.y >= 0 && check.y < rows)
                {
                    if (bodySet.Contains(check))
                    {
                        var where = step == 0 ? "" : $" after {step} move(s)";
                        validationErrors.Add($"Arrow {i+1}: self-block{where} at ({check.x},{check.y}).");
                        break;
                    }
                    check += dir;
                    step++;
                }
            }

            // Rule 3: Head-to-head
            for (int i = 0; i < arrows.Count; i++)
            {
                var a = arrows[i];
                if (a.occupiedPositions.Count < 2) continue;
                var hA = a.occupiedPositions[0];
                var dA = hA - a.occupiedPositions[1];
                var nA = hA + dA;

                for (int j = i + 1; j < arrows.Count; j++)
                {
                    var b = arrows[j];
                    if (b.occupiedPositions.Count < 2) continue;
                    var hB = b.occupiedPositions[0];
                    var dB = hB - b.occupiedPositions[1];
                    var nB = hB + dB;

                    if (nA == hB && nB == hA)
                        validationErrors.Add($"Head-to-head: Arrow {i+1} and Arrow {j+1} point at each other.");
                }
            }

            // Rule 5: Deadlock check - check full path (not just first cell)
            var blockSet = new Dictionary<int, HashSet<int>>();
            bool[] canMove = new bool[arrows.Count];

            for (int i = 0; i < arrows.Count; i++)
            {
                var a = arrows[i];
                if (a.occupiedPositions.Count < 2) { canMove[i] = true; continue; }
                var head = a.occupiedPositions[0];
                var dir = head - a.occupiedPositions[1];
                if (dir == Vector2Int.zero) { canMove[i] = true; continue; }

                Vector2Int check = head + dir;
                var blockers = new HashSet<int>();
                while (check.x >= 0 && check.x < cols && check.y >= 0 && check.y < rows)
                {
                    for (int j = 0; j < arrows.Count; j++)
                    {
                        if (i == j) continue;
                        if (arrows[j].occupiedPositions.Contains(check))
                            blockers.Add(j);
                    }
                    check += dir;
                }

                blockSet[i] = blockers;
            }

            // Iteratively remove arrows that become clear as their blockers are resolved
            bool changed = true;
            while (changed)
            {
                changed = false;
                var newlyClear = new List<int>();
                for (int i = 0; i < arrows.Count; i++)
                {
                    if (canMove[i]) continue;
                    if (!blockSet.ContainsKey(i) || blockSet[i].Count == 0)
                    {
                        newlyClear.Add(i);
                    }
                }
                if (newlyClear.Count > 0)
                {
                    changed = true;
                    foreach (int i in newlyClear)
                    {
                        canMove[i] = true;
                        foreach (var kv in blockSet)
                            kv.Value.Remove(i);
                    }
                }
            }

            var clearList = new List<int>();
            bool hasDeadlock = false;
            for (int i = 0; i < arrows.Count; i++)
            {
                if (canMove[i])
                    clearList.Add(i);
                else
                {
                    validationErrors.Add($"Deadlock: Arrow {i+1} is blocked and cannot be resolved.");
                    hasDeadlock = true;
                }
            }

            if (clearList.Count == 0 && !hasDeadlock)
                validationErrors.Add("Deadlock: no arrow has a clear path to move.");
        }

        private void DrawSaveSection()
        {
            EditorGUILayout.Space();
            GUILayout.Label("Save Imported Level", EditorStyles.boldLabel);

            lastFileName = EditorGUILayout.TextField("Asset Name", lastFileName);

            GUI.enabled = isValid;
            if (GUILayout.Button("Save as Level Asset", GUILayout.Height(35)))
            {
                string path = EditorUtility.SaveFilePanelInProject("Save Level Data", lastFileName, "asset", "Save imported level data");
                if (!string.IsNullOrEmpty(path))
                {
                    var asset = ScriptableObject.CreateInstance<LevelData>();
                    asset.gridDimensions = previewLevel.gridDimensions;
                    asset.defaultCameraSize = previewLevel.defaultCameraSize;
                    asset.minZoom = previewLevel.minZoom;
                    asset.maxZoom = previewLevel.maxZoom;
                    asset.maxMoves = previewLevel.maxMoves > 0 ? previewLevel.maxMoves : previewLevel.arrows.Count + 3;
                    asset.gameWinMode = previewLevel.gameWinMode;

                    foreach (var arrow in previewLevel.arrows)
                    {
                        var def = new ArrowDefinition();
                        def.occupiedPositions = new List<Vector2Int>(arrow.occupiedPositions);
                        def.arrowColor = arrow.arrowColor;
                        asset.arrows.Add(def);
                    }

                    AssetDatabase.CreateAsset(asset, path);
                    AssetDatabase.SaveAssets();
                    EditorUtility.FocusProjectWindow();
                    Selection.activeObject = asset;
                    EditorUtility.DisplayDialog("Success", "Level saved to:\n" + path, "OK");
                }
            }
            GUI.enabled = true;
        }

        private void DrawLevelPreview()
        {
            var level = previewLevel;
            float cs = CellSize;
            float gw = level.gridDimensions.x * cs;
            float gh = level.gridDimensions.y * cs;

            GUILayout.Label($"Grid: {level.gridDimensions.x}\u00d7{level.gridDimensions.y}  |  Arrows: {level.arrows.Count}  |  Max Moves: {(level.maxMoves > 0 ? level.maxMoves.ToString() : (level.arrows.Count + 3).ToString())}");

            scrollPos = GUILayout.BeginScrollView(scrollPos);
            Rect gridRect = GUILayoutUtility.GetRect(gw + Padding * 2, gh + Padding * 2);
            gridRect = new Rect(Padding, Padding, gw, gh);

            EditorGUI.DrawRect(gridRect, new Color(0.15f, 0.15f, 0.15f));

            for (int x = 0; x <= level.gridDimensions.x; x++)
                EditorGUI.DrawRect(new Rect(gridRect.x + x * cs, gridRect.y, 1, gh), new Color(0.3f, 0.3f, 0.3f));
            for (int y = 0; y <= level.gridDimensions.y; y++)
                EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y + y * cs, gw, 1), new Color(0.3f, 0.3f, 0.3f));

            for (int i = 0; i < level.arrows.Count; i++)
                DrawArrowPreview(level.arrows[i], gridRect, cs, level.gridDimensions.y);

            GUILayout.EndScrollView();
        }

        private void DrawArrowPreview(ArrowDefinition arrow, Rect gridRect, float cs, int gridH)
        {
            if (arrow.occupiedPositions == null || arrow.occupiedPositions.Count == 0) return;

            var cols = arrow.occupiedPositions.Count;
            for (int i = 0; i < cols; i++)
            {
                var p = arrow.occupiedPositions[i];
                float gx = gridRect.x + p.x * cs;
                float gy = gridRect.y + (gridH - 1 - p.y) * cs;

                EditorGUI.DrawRect(new Rect(gx + 1, gy + 1, cs - 2, cs - 2), arrow.arrowColor);

                if (i > 0)
                {
                    var prev = arrow.occupiedPositions[i - 1];
                    float px = gridRect.x + prev.x * cs;
                    float py = gridRect.y + (gridH - 1 - prev.y) * cs;
                    float mx = Mathf.Min(gx, px) + 1;
                    float Mx = Mathf.Max(gx + cs, px + cs) - 1;
                    float my = Mathf.Min(gy, py) + 1;
                    float My = Mathf.Max(gy + cs, py + cs) - 1;
                    EditorGUI.DrawRect(new Rect(mx, my, Mx - mx, My - my), arrow.arrowColor);
                }
            }

            var head = arrow.occupiedPositions[0];
            float hx = gridRect.x + head.x * cs;
            float hy = gridRect.y + (gridH - 1 - head.y) * cs;

            var dir = Vector2Int.up;
            if (arrow.occupiedPositions.Count > 1)
                dir = arrow.occupiedPositions[0] - arrow.occupiedPositions[1];

            var cx = hx + cs / 2f;
            var cy = hy + cs / 2f;
            float s = cs * 0.28f;

            Vector3 p1, p2, p3;
            if (dir.x > 0)
            {
                p1 = new Vector3(cx + s, cy, 0);
                p2 = new Vector3(cx - s, cy + s, 0);
                p3 = new Vector3(cx - s, cy - s, 0);
            }
            else if (dir.x < 0)
            {
                p1 = new Vector3(cx - s, cy, 0);
                p2 = new Vector3(cx + s, cy + s, 0);
                p3 = new Vector3(cx + s, cy - s, 0);
            }
            else if (dir.y < 0)
            {
                p1 = new Vector3(cx, cy + s, 0);
                p2 = new Vector3(cx - s, cy - s, 0);
                p3 = new Vector3(cx + s, cy - s, 0);
            }
            else
            {
                p1 = new Vector3(cx, cy - s, 0);
                p2 = new Vector3(cx - s, cy + s, 0);
                p3 = new Vector3(cx + s, cy + s, 0);
            }

            Handles.DrawAAConvexPolygon(p1, p2, p3);
        }
    }
}
