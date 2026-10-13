using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrashRush.EditorTools
{
    public sealed class VoxToLegoWindow : EditorWindow
    {
        private const string WindowTitle = "VOX to LEGO";
        private const string DefaultModelFolder = "Assets/Voxel Models";
        private const string BaseColorProperty = "_BaseColor";
        private const string ColorProperty = "_Color";
        private const int LargeModelThreshold = 10000;
        private const int ProgressUpdateInterval = 128;

        [SerializeField] private DefaultAsset _voxFile;
        [SerializeField] private GameObject _brickPrefab;
        [SerializeField] private Material _materialTemplate;

        [SerializeField] private Vector3 _spacing = Vector3.one * 0.1f;
        [SerializeField] private float _brickScale = 1f;
        [SerializeField] private Vector3 _pivotOffset;

        [SerializeField] private bool _surfaceOnly;
        [SerializeField] private bool _preparePhysics;

        private VoxBrickReader.Model _model;
        private string _error;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/Trash Rush/VOX to LEGO")]
        public static void Open()
        {
            var window = GetWindow<VoxToLegoWindow>(WindowTitle);
            window.minSize = new Vector2(430f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_voxFile == null)
            {
                _voxFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultModelFolder + "/Pineapple.vox");
            }

            ReadModel();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawSourceFields();
            EditorGUILayout.Space(12f);
            DrawLayoutFields();
            EditorGUILayout.Space(12f);
            DrawGenerationOptions();
            EditorGUILayout.Space(12f);
            DrawModelStatus();
            DrawGenerateButton();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSourceFields()
        {
            EditorGUILayout.LabelField("Source Assets", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _voxFile = (DefaultAsset)EditorGUILayout.ObjectField(
                new GUIContent("VOX File", "A single-model MagicaVoxel .vox file exported from Goxel."),
                _voxFile, typeof(DefaultAsset), false);

            if (EditorGUI.EndChangeCheck())
            {
                ReadModel();
            }

            _brickPrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Brick Prefab", "An active prefab root containing at least one Renderer."),
                _brickPrefab, typeof(GameObject), false);

            _materialTemplate = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Color Material (Optional)", "Uses the prefab material when empty. Must support _BaseColor or _Color."),
                _materialTemplate, typeof(Material), false);

            EditorGUILayout.HelpBox(
                "VOX colors replace every Renderer material slot. For textured bricks, select a plain white URP/Lit material.",
                MessageType.None);
        }

        private void DrawLayoutFields()
        {
            EditorGUILayout.LabelField("Brick Layout", EditorStyles.boldLabel);

            _spacing = EditorGUILayout.Vector3Field(
                new GUIContent("Brick Spacing", "Distance between brick centers along Unity X, Y, and Z. All values must be positive."),
                _spacing);
            _brickScale = EditorGUILayout.FloatField(
                new GUIContent("Brick Scale", "Positive multiplier applied to the source prefab's local scale."),
                _brickScale);
            _pivotOffset = EditorGUILayout.Vector3Field(
                new GUIContent("Pivot Offset", "Position adjustment applied to each brick in model-local space."),
                _pivotOffset);

            EditorGUILayout.HelpBox(
                "The model is centered on X/Z, with its lowest voxel center at Y = 0. VOX Z maps to Unity Y.",
                MessageType.Info);
        }

        private void DrawGenerationOptions()
        {
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);

            _surfaceOnly = EditorGUILayout.Toggle(
                new GUIContent("Surface Only", "Generate only voxels with at least one exposed face."),
                _surfaceOnly);
            _preparePhysics = EditorGUILayout.Toggle(
                new GUIContent("Prepare Physics", "Add a kinematic Rigidbody when a brick has none. The prefab must provide suitable colliders."),
                _preparePhysics);

            if (_preparePhysics)
            {
                EditorGUILayout.HelpBox(
                    "Rigidbodies remain kinematic with gravity disabled. Detach a hit brick and enable its physics in your slash logic.",
                    MessageType.Info);
            }
        }

        private void DrawModelStatus()
        {
            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            if (_model != null)
            {
                EditorGUILayout.LabelField(
                    $"{_model.Width} x {_model.Depth} x {_model.Height} - {_model.Cells.Length:N0} voxels");

                if (_model.Cells.Length > LargeModelThreshold)
                {
                    EditorGUILayout.HelpBox(
                        "This model creates many separate objects. Enable Surface Only to reduce the object count, or disable it to include interior bricks.",
                        MessageType.Info);
                }
            }

            if (_brickPrefab != null && !IsValidBrickPrefab())
            {
                EditorGUILayout.HelpBox("Select an active prefab root containing a Renderer.", MessageType.Error);
            }

            if (!HasValidLayout())
            {
                EditorGUILayout.HelpBox(
                    "Spacing and scale must be positive and finite. Pivot offset values must be finite.",
                    MessageType.Error);
            }
        }

        private void DrawGenerateButton()
        {
            bool canGenerate = _model != null && IsValidBrickPrefab() && HasValidLayout();

            using (new EditorGUI.DisabledScope(!canGenerate))
            {
                if (GUILayout.Button("Create LEGO Model Prefab", GUILayout.Height(32f)))
                {
                    Generate();
                }
            }
        }

        private bool IsValidBrickPrefab()
        {
            return _brickPrefab != null
                && PrefabUtility.IsPartOfPrefabAsset(_brickPrefab)
                && _brickPrefab.transform.parent == null
                && _brickPrefab.activeSelf
                && _brickPrefab.GetComponentInChildren<Renderer>(true) != null;
        }

        private bool HasValidLayout()
        {
            return IsFinite(_spacing)
                && IsFinite(_pivotOffset)
                && IsFinite(_brickScale)
                && _spacing.x > 0f
                && _spacing.y > 0f
                && _spacing.z > 0f
                && _brickScale > 0f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void ReadModel()
        {
            _model = null;
            _error = null;

            if (_voxFile == null)
            {
                return;
            }

            try
            {
                string sourcePath = AssetDatabase.GetAssetPath(_voxFile);

                if (!string.Equals(Path.GetExtension(sourcePath), ".vox", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Select a MagicaVoxel .vox file.");
                }

                _model = VoxBrickReader.Read(sourcePath);
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }

        private void Generate()
        {
            // Include source edits made while the window was open.
            ReadModel();

            if (_model == null)
            {
                return;
            }

            string prefabPath = EditorUtility.SaveFilePanelInProject(
                "Save LEGO Prefab", _voxFile.name + "_LEGO", "prefab",
                "Choose a location for the generated model.", DefaultModelFolder);

            if (string.IsNullOrEmpty(prefabPath))
            {
                return;
            }

            prefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
            string parentFolder = Path.GetDirectoryName(prefabPath).Replace('\\', '/');
            string modelName = Path.GetFileNameWithoutExtension(prefabPath);
            string materialFolder = AssetDatabase.GenerateUniqueAssetPath(parentFolder + "/" + modelName + "_Materials");
            GameObject modelRoot = null;
            bool materialFolderCreated = false;

            try
            {
                List<VoxBrickReader.Cell> cells = GetCellsToGenerate();
                Vector3 origin = CalculateModelOrigin(cells);
                Material template = ResolveMaterialTemplate();
                Shader shader = ResolveShader(template);

                string folderGuid = AssetDatabase.CreateFolder(parentFolder, Path.GetFileName(materialFolder));
                materialFolderCreated = !string.IsNullOrEmpty(folderGuid);

                if (!materialFolderCreated)
                {
                    throw new IOException("Could not create the material folder.");
                }

                var materials = new Dictionary<byte, Material>();
                modelRoot = new GameObject(modelName);

                for (int index = 0; index < cells.Count; index++)
                {
                    UpdateGenerationProgress(index, cells.Count);

                    VoxBrickReader.Cell cell = cells[index];
                    Material material = GetOrCreateMaterial(cell.Color, materials, template, shader, materialFolder);
                    CreateBrick(cell, modelRoot.transform, origin, material);
                }

                GameObject prefab = SaveModelPrefab(modelRoot, prefabPath);
                _error = null;
                Debug.Log(
                    $"Created LEGO model: {prefabPath} ({cells.Count:N0} bricks, {materials.Count} colors).",
                    prefab);
            }
            catch (Exception exception)
            {
                _error = exception.Message;

                // Both paths are unique to this generation operation.
                AssetDatabase.DeleteAsset(prefabPath);

                if (materialFolderCreated)
                {
                    AssetDatabase.DeleteAsset(materialFolder);
                }

                if (!(exception is OperationCanceledException))
                {
                    Debug.LogException(exception);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();

                if (modelRoot != null)
                {
                    DestroyImmediate(modelRoot);
                }
            }
        }

        private Material ResolveMaterialTemplate()
        {
            Material template = _materialTemplate != null
                ? _materialTemplate
                : _brickPrefab.GetComponentInChildren<Renderer>(true).sharedMaterial;

            if (template != null && !template.HasProperty(BaseColorProperty) && !template.HasProperty(ColorProperty))
            {
                throw new InvalidOperationException("The color material must support _BaseColor or _Color.");
            }

            return template;
        }

        private static Shader ResolveShader(Material template)
        {
            Shader shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null)
            {
                throw new InvalidOperationException("Select a color material with a valid shader.");
            }

            return shader;
        }

        private static void UpdateGenerationProgress(int index, int cellCount)
        {
            if (index % ProgressUpdateInterval != 0)
            {
                return;
            }

            bool canceled = EditorUtility.DisplayCancelableProgressBar(
                WindowTitle, $"{index:N0} / {cellCount:N0} bricks", (float)index / cellCount);

            if (canceled)
            {
                throw new OperationCanceledException("Generation canceled.");
            }
        }

        private Material GetOrCreateMaterial(
            byte colorIndex,
            Dictionary<byte, Material> materials,
            Material template,
            Shader shader,
            string materialFolder)
        {
            if (materials.TryGetValue(colorIndex, out Material material))
            {
                return material;
            }

            material = template != null ? new Material(template) : new Material(shader);
            byte[] rgba = _model.Palette[colorIndex];
            Color color = new Color32(rgba[0], rgba[1], rgba[2], 255);

            if (material.HasProperty(BaseColorProperty))
            {
                material.SetColor(BaseColorProperty, color);
            }

            if (material.HasProperty(ColorProperty))
            {
                material.SetColor(ColorProperty, color);
            }

            material.enableInstancing = true;
            AssetDatabase.CreateAsset(material, materialFolder + $"/Color_{colorIndex:D3}.mat");
            materials.Add(colorIndex, material);

            return material;
        }

        private void CreateBrick(VoxBrickReader.Cell cell, Transform parent, Vector3 origin, Material material)
        {
            var brick = (GameObject)PrefabUtility.InstantiatePrefab(_brickPrefab, parent);
            brick.name = $"Brick_{cell.X}_{cell.Y}_{cell.Z}";

            if (brick.GetComponent<TrashRush.Game.VoxelBrick>() == null)
            {
                brick.AddComponent<TrashRush.Game.VoxelBrick>();
            }

            Transform brickTransform = brick.transform;
            brickTransform.localPosition = Vector3.Scale(ToUnityPosition(cell) - origin, _spacing) + _pivotOffset;
            brickTransform.localRotation = _brickPrefab.transform.localRotation;
            brickTransform.localScale = _brickPrefab.transform.localScale * _brickScale;

            ApplyBrickMaterial(brick, material);
            PrepareBrickPhysics(brick);
        }

        private static void ApplyBrickMaterial(GameObject brick, Material material)
        {
            foreach (Renderer renderer in brick.GetComponentsInChildren<Renderer>(true))
            {
                var materialSlots = new Material[Math.Max(1, renderer.sharedMaterials.Length)];

                for (int slotIndex = 0; slotIndex < materialSlots.Length; slotIndex++)
                {
                    materialSlots[slotIndex] = material;
                }

                renderer.sharedMaterials = materialSlots;
            }
        }

        private void PrepareBrickPhysics(GameObject brick)
        {
            if (_preparePhysics && brick.GetComponentInChildren<Rigidbody>(true) == null)
            {
                brick.AddComponent<Rigidbody>();
            }

            // Existing prefab bodies must remain at rest even when Prepare Physics is disabled.
            foreach (Rigidbody body in brick.GetComponentsInChildren<Rigidbody>(true))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        private static GameObject SaveModelPrefab(GameObject modelRoot, string prefabPath)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(modelRoot, prefabPath, out bool success);

            if (!success || prefab == null)
            {
                throw new IOException("Could not save the prefab.");
            }

            AssetDatabase.SaveAssets();
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);

            return prefab;
        }

        private static Vector3 ToUnityPosition(VoxBrickReader.Cell cell)
        {
            // VOX uses Z as its vertical axis; Unity uses Y.
            return new Vector3(cell.X, cell.Z, cell.Y);
        }

        private static Vector3 CalculateModelOrigin(List<VoxBrickReader.Cell> cells)
        {
            var minimum = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var maximum = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            foreach (VoxBrickReader.Cell cell in cells)
            {
                Vector3 position = ToUnityPosition(cell);
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            return new Vector3((minimum.x + maximum.x) * 0.5f, minimum.y, (minimum.z + maximum.z) * 0.5f);
        }

        private List<VoxBrickReader.Cell> GetCellsToGenerate()
        {
            if (!_surfaceOnly)
            {
                return new List<VoxBrickReader.Cell>(_model.Cells);
            }

            var occupiedPositions = new HashSet<int>();

            foreach (VoxBrickReader.Cell cell in _model.Cells)
            {
                occupiedPositions.Add(cell.Key);
            }

            var surfaceCells = new List<VoxBrickReader.Cell>();

            foreach (VoxBrickReader.Cell cell in _model.Cells)
            {
                if (IsSurfaceCell(cell, occupiedPositions))
                {
                    surfaceCells.Add(cell);
                }
            }

            return surfaceCells;
        }

        private static bool IsSurfaceCell(VoxBrickReader.Cell cell, HashSet<int> occupiedPositions)
        {
            const int maximumCoordinate = byte.MaxValue;
            const int xStep = 1;
            const int yStep = 1 << 8;
            const int zStep = 1 << 16;

            // Check boundaries before packed-key arithmetic to prevent coordinate wrapping.
            return cell.X == 0 || cell.X == maximumCoordinate
                || cell.Y == 0 || cell.Y == maximumCoordinate
                || cell.Z == 0 || cell.Z == maximumCoordinate
                || !occupiedPositions.Contains(cell.Key - xStep)
                || !occupiedPositions.Contains(cell.Key + xStep)
                || !occupiedPositions.Contains(cell.Key - yStep)
                || !occupiedPositions.Contains(cell.Key + yStep)
                || !occupiedPositions.Contains(cell.Key - zStep)
                || !occupiedPositions.Contains(cell.Key + zStep);
        }
    }
}
