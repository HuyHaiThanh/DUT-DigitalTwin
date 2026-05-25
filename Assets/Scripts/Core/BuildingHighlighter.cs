using UnityEngine;
using DUT.Data;

namespace DUT.Core
{
    /// <summary>
    /// Đổi màu tint tòa nhà theo trạng thái dùng MaterialPropertyBlock.
    /// KHÔNG tạo material instance mới — texture gốc giữ nguyên.
    /// Thay thế status dot sphere.
    /// </summary>
    public class BuildingHighlighter : MonoBehaviour
    {
        [HideInInspector] public string buildingId;
        [HideInInspector] public BuildingDataStore store;

        Renderer[]          _renderers;
        MaterialPropertyBlock _mpb;

        // Màu tint theo trạng thái (alpha thấp để giữ texture gốc)
        static readonly Color COL_EMPTY    = new Color(0.20f, 0.85f, 0.45f, 0.45f);  // xanh lá
        static readonly Color COL_OCCUPIED = new Color(0.95f, 0.25f, 0.25f, 0.45f);  // đỏ
        static readonly Color COL_UPCOMING = new Color(1.00f, 0.65f, 0.05f, 0.45f);  // vàng
        static readonly Color COL_SELECTED = new Color(0.20f, 0.65f, 1.00f, 0.65f);  // xanh dương
        static readonly Color COL_DEFAULT  = new Color(1.00f, 1.00f, 1.00f, 0.00f);  // transparent = màu gốc

        bool _isSelected;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
        }

        void Start()
        {
            if (store == null) store = FindFirstObjectByType<BuildingDataStore>();
            if (store != null)
            {
                store.OnBuildingSelected        += OnSelectionChanged;
                store.OnBuildingLiveDataUpdated += OnDataUpdated;
                store.OnSelectionCleared        += OnCleared;
            }
            Refresh();
        }

        void OnDestroy()
        {
            if (store == null) return;
            store.OnBuildingSelected        -= OnSelectionChanged;
            store.OnBuildingLiveDataUpdated -= OnDataUpdated;
            store.OnSelectionCleared        -= OnCleared;
        }

        void OnSelectionChanged(string id)
        {
            _isSelected = (id == buildingId);
            Refresh();
        }

        void OnDataUpdated(string id)
        {
            if (id == buildingId) Refresh();
        }

        void OnCleared()
        {
            _isSelected = false;
            Refresh();
        }

        public void Refresh()
        {
            if (_mpb == null)       _mpb       = new MaterialPropertyBlock();
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>();
            Color tint = GetTint();
            // Dùng _EmissionColor để overlay màu lên texture gốc
            // Intensity thấp = nhìn thấy tint nhẹ, texture vẫn rõ
            Color emission = new Color(tint.r * tint.a, tint.g * tint.a, tint.b * tint.a, 1f);
            foreach (var rend in _renderers)
            {
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", emission);
                rend.SetPropertyBlock(_mpb);
            }
        }

        Color GetTint()
        {
            if (_isSelected) return COL_SELECTED;
            if (store == null) return COL_DEFAULT;

            var live = store.GetLiveData(buildingId);
            if (live == null) return COL_DEFAULT;

            return live.schedule.status switch
            {
                BuildingStatus.Occupied => COL_OCCUPIED,
                BuildingStatus.Empty    => COL_EMPTY,
                BuildingStatus.Upcoming => COL_UPCOMING,
                _                       => COL_DEFAULT
            };
        }

        // Reset về màu gốc
        public void ResetColor()
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>();
            foreach (var rend in _renderers)
            {
                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", Color.black);
                rend.SetPropertyBlock(_mpb);
            }
        }
    }
}
