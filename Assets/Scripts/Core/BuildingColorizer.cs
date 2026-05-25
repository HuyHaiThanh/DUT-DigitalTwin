using System.Collections;
using UnityEngine;
using DUT.Data;

namespace DUT.Core
{
    public class BuildingColorizer : MonoBehaviour
    {
        [HideInInspector] public string buildingId;
        [HideInInspector] public BuildingDataStore store;

        public static Material MatEmpty;
        public static Material MatOccupied;
        public static Material MatUpcoming;
        public static Material MatSelected;
        public static Material MatDefault;

        Renderer[] _renderers;
        bool _isSelected;
        BuildingStatus _currentStatus = BuildingStatus.Unknown;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        void Start()
        {
            if (store == null) store = FindFirstObjectByType<BuildingDataStore>();
            if (store != null)
            {
                store.OnBuildingSelected        += OnSelected;
                store.OnBuildingLiveDataUpdated += OnDataUpdated;
                store.OnSelectionCleared        += OnCleared;
            }
            StartCoroutine(WaitAndRefresh());
        }

        void OnDestroy()
        {
            if (store == null) return;
            store.OnBuildingSelected        -= OnSelected;
            store.OnBuildingLiveDataUpdated -= OnDataUpdated;
            store.OnSelectionCleared        -= OnCleared;
        }

        void OnSelected(string id)   { _isSelected = (id == buildingId); Refresh(); }
        void OnDataUpdated(string id) { if (id == buildingId) Refresh(); }
        void OnCleared()              { _isSelected = false; Refresh(); }

        IEnumerator WaitAndRefresh()
        {
            float timeout = 10f, t = 0;
            while (t < timeout)
            {
                if (store?.GetLiveData(buildingId) != null) break;
                t += Time.deltaTime;
                yield return null;
            }
            Refresh();
        }

        public void Refresh()
        {
            if (_renderers == null) _renderers = GetComponentsInChildren<Renderer>(true);
            Material mat = GetMat();
            if (mat == null) return;
            foreach (var rend in _renderers)
            {
                if (rend == null) continue;
                // Fill ALL material slots với cùng 1 material
                var slots = rend.sharedMaterials;
                var filled = new Material[slots.Length];
                for (int i = 0; i < filled.Length; i++) filled[i] = mat;
                rend.materials = filled;
            }
        }

        Material GetMat()
        {
            if (_isSelected)        return MatSelected ?? MatDefault;
            var live = store?.GetLiveData(buildingId);
            if (live == null)       return MatDefault;
            return live.schedule.status switch
            {
                BuildingStatus.Occupied => MatOccupied ?? MatDefault,
                BuildingStatus.Empty    => MatEmpty    ?? MatDefault,
                BuildingStatus.Upcoming => MatUpcoming ?? MatDefault,
                _                       => MatDefault
            };
        }
    }
}