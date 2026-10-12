using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrashRush.EditorTools
{
    public sealed class VoxToLegoWindow : EditorWindow
    {
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

        [MenuItem("Tools/Trash Rush/VOX to LEGO")]
        public static void Open()
        {
            var window = GetWindow<VoxToLegoWindow>("VOX → LEGO");
            window.minSize = new Vector2(430, 480);
            window.Show();
        }

        private void OnEnable()
        {
            if (_voxFile == null)
                _voxFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Voxel Models/Pineapple.vox");
            ReadModel();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Voxel modelini LEGO parçalarına dönüştür", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _voxFile = (DefaultAsset)EditorGUILayout.ObjectField("VOX dosyası", _voxFile, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) ReadModel();
            _brickPrefab = (GameObject)EditorGUILayout.ObjectField("LEGO parça prefabı", _brickPrefab, typeof(GameObject), false);
            _materialTemplate = (Material)EditorGUILayout.ObjectField("Renk materyali (isteğe bağlı)", _materialTemplate, typeof(Material), false);
            EditorGUILayout.Space();
            _spacing = EditorGUILayout.Vector3Field("Parça aralığı (Unity X/Y/Z)", _spacing);
            _brickScale = EditorGUILayout.FloatField("Parça ölçek çarpanı", _brickScale);
            _pivotOffset = EditorGUILayout.Vector3Field("Parçanın pivot düzeltmesi", _pivotOffset);
            _surfaceOnly = EditorGUILayout.Toggle("Yalnızca dış yüzey", _surfaceOnly);
            _preparePhysics = EditorGUILayout.Toggle("Kinematic Rigidbody hazırla", _preparePhysics);

            EditorGUILayout.HelpBox("LEGO prefabını yukarıdaki alana sürükle. Aralık, parçaların merkezleri arasındaki mesafedir; ölçek çarpanı prefabın mevcut ölçeğini çarpar. En alt voxel merkezi Y=0'a, model X/Z merkezine yerleştirilir.", MessageType.Info);
            EditorGUILayout.HelpBox("Renkler VOX dosyasından alınır. Bütün Renderer materyal yuvaları bu renklerle değiştirilir. Dokulu parça kullanıyorsan düz beyaz bir URP/Lit materyalini renk materyali alanına ekle.", MessageType.None);
            if (_preparePhysics)
                EditorGUILayout.HelpBox("Her parçada kinematic Rigidbody hazırlanır; simülasyon başlamaz. Slash kodunda vurulan parçayı ayırıp isKinematic=false ve useGravity=true yapabilirsin. Prefabında uygun Collider bulunmalı.", MessageType.Info);
            if (!string.IsNullOrEmpty(_error)) EditorGUILayout.HelpBox(_error, MessageType.Error);
            if (_model != null)
                EditorGUILayout.LabelField($"{_model.Width} × {_model.Depth} × {_model.Height} — {_model.Cells.Length:N0} voxel");
            if (_model != null && _model.Cells.Length > 10000)
                EditorGUILayout.HelpBox("Bu model çok sayıda ayrı obje üretir. İlk denemede yalnızca dış yüzeyi oluşturmak obje sayısını azaltır; iç parçaların da bulunması için bu seçeneği kapat.", MessageType.Info);

            bool validPrefab = _brickPrefab != null && PrefabUtility.IsPartOfPrefabAsset(_brickPrefab)
                && _brickPrefab.transform.parent == null && _brickPrefab.activeSelf
                && _brickPrefab.GetComponentInChildren<Renderer>(true) != null;
            if (_brickPrefab != null && !validPrefab)
                EditorGUILayout.HelpBox("Aktif, Renderer içeren bir prefabın kök objesini seç.", MessageType.Error);
            using (new EditorGUI.DisabledScope(_model == null || !validPrefab || _spacing.x <= 0 || _spacing.y <= 0 || _spacing.z <= 0 || _brickScale <= 0
                || !Finite(_spacing) || !Finite(_pivotOffset) || float.IsInfinity(_brickScale) || float.IsNaN(_brickScale)))
            {
                if (GUILayout.Button("LEGO model prefabını oluştur", GUILayout.Height(32))) Generate();
            }
        }

        private static bool Finite(Vector3 v) =>
            !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z)
            && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

        private void ReadModel()
        {
            _model = null;
            _error = null;
            if (_voxFile == null) return;
            try
            {
                string path = AssetDatabase.GetAssetPath(_voxFile);
                if (!string.Equals(Path.GetExtension(path), ".vox", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("MagicaVoxel .vox dosyası seç.");
                _model = VoxBrickReader.Read(path);
            }
            catch (Exception e) { _error = e.Message; }
        }

        private void Generate()
        {
            // Re-read so edits made in Goxel are included even if the window stayed open.
            ReadModel();
            if (_model == null) return;
            string path = EditorUtility.SaveFilePanelInProject("LEGO prefabını kaydet", _voxFile.name + "_LEGO", "prefab", "Oluşturulacak modelin konumunu seç.", "Assets/Voxel Models");
            if (string.IsNullOrEmpty(path)) return;
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string folder = Path.GetDirectoryName(path).Replace('\\', '/');
            string materialFolder = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + Path.GetFileNameWithoutExtension(path) + "_Materials");
            GameObject root = null;
            var createdAssets = new List<string>();
            try
            {
                var cells = GetCells();
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var c in cells)
                {
                    var p = new Vector3(c.X, c.Z, c.Y);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                Vector3 origin = new Vector3((min.x + max.x) / 2f, min.y, (min.z + max.z) / 2f);
                Material template = _materialTemplate;
                if (template == null) template = _brickPrefab.GetComponentInChildren<Renderer>(true).sharedMaterial;
                Shader shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("Bir renk materyali seç.");
                if (template != null && !template.HasProperty("_BaseColor") && !template.HasProperty("_Color"))
                    throw new InvalidOperationException("Renk materyali _BaseColor veya _Color özelliğini içermeli.");
                AssetDatabase.CreateFolder(folder, Path.GetFileName(materialFolder));
                createdAssets.Add(materialFolder);
                var materials = new Dictionary<byte, Material>();
                root = new GameObject(Path.GetFileNameWithoutExtension(path));
                for (int i = 0; i < cells.Count; i++)
                {
                    if (i % 128 == 0 && EditorUtility.DisplayCancelableProgressBar("VOX → LEGO", $"{i:N0} / {cells.Count:N0} parça", (float)i / cells.Count))
                        throw new OperationCanceledException("Oluşturma iptal edildi.");
                    var cell = cells[i];
                    if (!materials.TryGetValue(cell.Color, out var material))
                    {
                        material = template != null ? new Material(template) : new Material(shader);
                        var rgba = _model.Palette[cell.Color];
                        Color color = new Color32(rgba[0], rgba[1], rgba[2], 255);
                        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                        material.enableInstancing = true;
                        string materialPath = materialFolder + $"/Color_{cell.Color:D3}.mat";
                        AssetDatabase.CreateAsset(material, materialPath);
                        materials.Add(cell.Color, material);
                    }
                    var brick = (GameObject)PrefabUtility.InstantiatePrefab(_brickPrefab, root.transform);
                    brick.name = $"Brick_{cell.X}_{cell.Y}_{cell.Z}";
                    if (brick.GetComponent<TrashRush.Game.VoxelBrick>() == null)
                        brick.AddComponent<TrashRush.Game.VoxelBrick>();
                    brick.transform.localPosition = Vector3.Scale(new Vector3(cell.X, cell.Z, cell.Y) - origin, _spacing) + _pivotOffset;
                    brick.transform.localRotation = _brickPrefab.transform.localRotation;
                    brick.transform.localScale = _brickPrefab.transform.localScale * _brickScale;
                    foreach (var renderer in brick.GetComponentsInChildren<Renderer>(true))
                    {
                        var slots = new Material[Math.Max(1, renderer.sharedMaterials.Length)];
                        for (int m = 0; m < slots.Length; m++) slots[m] = material;
                        renderer.sharedMaterials = slots;
                    }
                    if (_preparePhysics)
                    {
                        if (brick.GetComponent<Rigidbody>() == null && brick.GetComponentInChildren<Rigidbody>(true) == null)
                            brick.AddComponent<Rigidbody>();
                    }
                    // Any bodies supplied by the source prefab must also stay at rest.
                    foreach (var body in brick.GetComponentsInChildren<Rigidbody>(true))
                    {
                        body.isKinematic = true;
                        body.useGravity = false;
                    }
                }
                var result = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || result == null) throw new IOException("Prefab kaydedilemedi.");
                AssetDatabase.SaveAssets();
                Selection.activeObject = result;
                EditorGUIUtility.PingObject(result);
                _error = null;
                Debug.Log($"LEGO modeli oluşturuldu: {path} ({cells.Count:N0} parça, {materials.Count} renk).", result);
            }
            catch (Exception e)
            {
                _error = e.Message;
                // Only remove this operation's newly created assets on failure/cancel.
                AssetDatabase.DeleteAsset(path);
                foreach (string asset in createdAssets) AssetDatabase.DeleteAsset(asset);
                if (!(e is OperationCanceledException)) Debug.LogException(e);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (root != null) DestroyImmediate(root);
            }
        }

        private List<VoxBrickReader.Cell> GetCells()
        {
            if (!_surfaceOnly) return new List<VoxBrickReader.Cell>(_model.Cells);
            var occupied = new HashSet<int>();
            foreach (var c in _model.Cells) occupied.Add(c.Key);
            var result = new List<VoxBrickReader.Cell>();
            foreach (var c in _model.Cells)
            {
                if (c.X == 0 || c.Y == 0 || c.Z == 0 || c.X == 255 || c.Y == 255 || c.Z == 255
                    || !occupied.Contains(c.Key - 1) || !occupied.Contains(c.Key + 1)
                    || !occupied.Contains(c.Key - 256) || !occupied.Contains(c.Key + 256)
                    || !occupied.Contains(c.Key - 65536) || !occupied.Contains(c.Key + 65536)) result.Add(c);
            }
            return result;
        }
    }
}
