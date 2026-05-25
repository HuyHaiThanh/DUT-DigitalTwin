using System.Collections.Generic;
using UnityEngine;
using DUT.Data;

namespace DUT.Core
{
    /// <summary>
    /// Hiện 1 icon per KHU (không phải per tòa nhà) để tránh loãng.
    /// Icon là Circle/Quad billboard, màu theo trạng thái phổ biến nhất trong khu.
    /// Khi click chọn tòa nhà cụ thể → icon xanh dương xuất hiện trên tòa đó.
    /// </summary>
    public class StatusDotManager : MonoBehaviour
    {
        public BuildingDataStore store;

        // Màu
        static readonly Color COL_EMPTY    = new Color(0.13f, 0.77f, 0.37f);
        static readonly Color COL_OCCUPIED = new Color(0.94f, 0.27f, 0.27f);
        static readonly Color COL_UPCOMING = new Color(0.96f, 0.62f, 0.04f);
        static readonly Color COL_SELECTED = new Color(0.18f, 0.56f, 0.91f);
        static readonly Color COL_MIXED    = new Color(0.90f, 0.85f, 0.20f);

        // Khu icon: khu → renderer
        readonly Dictionary<string, Renderer> _khuRends   = new Dictionary<string, Renderer>();
        // Khu center positions
        readonly Dictionary<string, Vector3>  _khuCenters = new Dictionary<string, Vector3>();
        // Selected building icon (riêng)
        GameObject _selectedIcon;

        Transform _root;
        Camera    _cam;
        Shader    _shader;
        bool      _ready;

        void Start()
        {
            if (store == null) store = FindFirstObjectByType<BuildingDataStore>();
            _cam    = Camera.main;
            _shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");

            _root = GameObject.Find("WorldSpaceLabels")?.transform
                  ?? new GameObject("WorldSpaceLabels").transform;

            // Xóa icons cũ
            var old = new List<GameObject>();
            foreach (Transform c in _root) old.Add(c.gameObject);
            foreach (var g in old) Destroy(g);

            if (store != null)
            {
                            if (LayerManager.Instance != null)
                LayerManager.Instance.OnPrimaryLayerChanged += _ => RefreshAll();
store.OnBuildingLiveDataUpdated += _ => { if (_ready) RefreshAll(); else TryCreate(); };
                store.OnBuildingSelected        += OnSelected;
                store.OnSelectionCleared        += OnCleared;
            }
            TryCreate();
        }

        void OnDestroy()
        {
            if (store == null) return;
            store.OnBuildingSelected        -= OnSelected;
            store.OnSelectionCleared        -= OnCleared;
        }

        void TryCreate()
        {
            if (_ready) return;
            if (store?.AllBuildings?.Count == 0) return;
            if (store.GetLiveData(store.AllBuildings[0].building_id) == null) return;
            CreateKhuIcons();
            _ready = true;
        }

        // ── Tạo icon per Khu ─────────────────────────────────────────────
        void CreateKhuIcons()
        {
            // Gom buildings theo khu
            var khuBuildings = new Dictionary<string, List<BuildingInfo>>();
            foreach (var b in store.AllBuildings)
            {
                if (!khuBuildings.ContainsKey(b.ten_khu)) khuBuildings[b.ten_khu] = new List<BuildingInfo>();
                khuBuildings[b.ten_khu].Add(b);
            }

            foreach (var kv in khuBuildings)
            {
                string khu = kv.Key;
                var buildings = kv.Value;

                // Tính center của khu
                Vector3 center = Vector3.zero;
                float maxY = 0;
                foreach (var b in buildings) {
                    center += b.world_position;
                    float top = b.world_position.y + b.bounds_size.y * 0.5f;
                    if (top > maxY) maxY = top;
                }
                center /= buildings.Count;
                Vector3 iconPos = new Vector3(center.x, maxY + 8f, center.z);
                _khuCenters[khu] = iconPos;

                // Tạo icon sphere
                var icon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                icon.name = $"KhuIcon_{khu}";
                Destroy(icon.GetComponent<Collider>());
                icon.transform.SetParent(_root);
                icon.transform.position = iconPos;
                icon.transform.localScale = Vector3.one * 10f;

                var mat = new Material(_shader);
                mat.color = COL_MIXED;
                icon.GetComponent<Renderer>().material = mat;
                _khuRends[khu] = icon.GetComponent<Renderer>();
            }

            // Selected building icon (ẩn mặc định)
            _selectedIcon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _selectedIcon.name = "SelectedBuildingIcon";
            Destroy(_selectedIcon.GetComponent<Collider>());
            _selectedIcon.transform.SetParent(_root);
            _selectedIcon.transform.localScale = Vector3.one * 8f;
            var selMat = new Material(_shader);
            selMat.color = COL_SELECTED;
            _selectedIcon.GetComponent<Renderer>().material = selMat;
            _selectedIcon.SetActive(false);

            RefreshAll();
        }

        // ── Màu per Khu: dominant status ─────────────────────────────────
Color GetKhuColor(string khu)
        {
            // Dùng LayerManager nếu có
            if (LayerManager.Instance != null)
                return GetKhuColorFromLayer(khu);

            // Fallback: schedule dominant (cũ)
            int occ = 0, empty = 0, upcoming = 0;
            foreach (var b in store.AllBuildings)
            {
                if (b.ten_khu != khu) continue;
                var live = store.GetLiveData(b.building_id);
                if (live == null) continue;
                switch (live.schedule.status)
                {
                    case BuildingStatus.Occupied: occ++; break;
                    case BuildingStatus.Empty:    empty++; break;
                    case BuildingStatus.Upcoming: upcoming++; break;
                }
            }
            if (occ == 0 && upcoming == 0) return COL_EMPTY;
            if (empty == 0 && upcoming == 0) return COL_OCCUPIED;
            if (occ > empty && occ > upcoming) return COL_OCCUPIED;
            if (empty > occ && empty > upcoming) return COL_EMPTY;
            if (upcoming > occ) return COL_UPCOMING;
            return COL_MIXED;
        }

        Color GetKhuColorFromLayer(string khu)
        {
            // Gom màu từng building trong khu → dominant
            var counts = new System.Collections.Generic.Dictionary<Color, int>();
            foreach (var b in store.AllBuildings)
            {
                if (b.ten_khu != khu) continue;
                var col = LayerManager.Instance.GetBuildingColor(b.building_id);
                counts.TryGetValue(col, out int n);
                counts[col] = n + 1;
            }
            if (counts.Count == 0) return COL_MIXED;
            Color dominant = COL_MIXED;
            int max = 0;
            foreach (var kv in counts)
                if (kv.Value > max) { max = kv.Value; dominant = kv.Key; }
            return dominant;
        }

        void RefreshAll()
        {
            foreach (var kv in _khuRends)
                kv.Value.material.color = GetKhuColor(kv.Key);
        }

        void OnSelected(string id)
        {
            if (string.IsNullOrEmpty(id) || _selectedIcon == null) { OnCleared(); return; }
            var info = store?.GetInfo(id);
            if (info == null) { OnCleared(); return; }

            // Đặt selected icon lên tòa nhà được chọn
            float topY = info.world_position.y + info.bounds_size.y * 0.5f + 6f;
            _selectedIcon.transform.position = new Vector3(
                info.world_position.x, topY, info.world_position.z);
            _selectedIcon.SetActive(true);
        }

        void OnCleared()
        {
            if (_selectedIcon != null) _selectedIcon.SetActive(false);
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null || !_ready) return;
            foreach (var kv in _khuRends)
                if (kv.Value != null)
                    kv.Value.transform.LookAt(_cam.transform);
            if (_selectedIcon != null && _selectedIcon.activeSelf)
                _selectedIcon.transform.LookAt(_cam.transform);
        }
    }
}