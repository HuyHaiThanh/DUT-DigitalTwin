using System;
using System.Collections.Generic;
using UnityEngine;
using DUT.Data;

namespace DUT.Data
{
    [CreateAssetMenu(fileName = "BuildingDataStore", menuName = "DUT/BuildingDataStore")]
    public class BuildingDataStore : ScriptableObject
    {
        public List<BuildingInfo> AllBuildings = new List<BuildingInfo>();
        Dictionary<string, BuildingLiveData> _liveData = new Dictionary<string, BuildingLiveData>();

        public string SelectedBuildingId { get; private set; }
        public string SelectedKhu        { get; private set; }

        public event Action<string> OnBuildingSelected;
        public event Action<string> OnBuildingLiveDataUpdated;
        public event Action         OnSelectionCleared;

        public BuildingInfo     GetInfo(string id) => AllBuildings.Find(b => b.building_id == id);
        public BuildingLiveData GetLiveData(string id) => _liveData.TryGetValue(id, out var d) ? d : null;

        public void UpdateLiveData(BuildingLiveData data)
        {
            if (data == null) return;
            _liveData[data.building_id] = data;
            OnBuildingLiveDataUpdated?.Invoke(data.building_id);
        }

        public void SelectBuilding(string id)
        {
            SelectedBuildingId = id;
            SelectedKhu = GetInfo(id)?.ten_khu;
            OnBuildingSelected?.Invoke(id);
        }

        public void SelectKhu(string khu)
        {
            SelectedKhu = khu;
            SelectedBuildingId = null;
            OnBuildingSelected?.Invoke(null);
        }

        public void ClearSelection()
        {
            SelectedBuildingId = null;
            SelectedKhu = null;
            OnSelectionCleared?.Invoke();
        }

        public List<BuildingInfo> GetBuildingsInKhu(string khu) => AllBuildings.FindAll(b => b.ten_khu == khu);
        public BuildingStatus GetStatus(string id) => GetLiveData(id)?.schedule.status ?? BuildingStatus.Unknown;

        public int CountByStatus(BuildingStatus s)
        {
            int c = 0;
            foreach (var b in AllBuildings) if (GetStatus(b.building_id) == s) c++;
            return c;
        }

        public List<AlertData> GetAllAlerts()
        {
            var list = new List<AlertData>();
            foreach (var b in AllBuildings)
            {
                var live = GetLiveData(b.building_id);
                if (live == null) continue;
                if (live.infrastructure.has_alert)
                    list.Add(new AlertData { alert_id=$"infra_{b.building_id}", building_id=b.building_id, layer=AlertLayer.Infrastructure, severity=AlertSeverity.Warning, title=live.infrastructure.electric.alert_msg, timestamp=live.timestamp });
                foreach (var err in live.equipment.errors)
                    list.Add(new AlertData { alert_id=$"equip_{b.building_id}_{err.device_name}", building_id=b.building_id, layer=AlertLayer.Equipment, severity=err.severity, title=$"{err.device_name} — {err.error_type}", description=err.location, timestamp=err.reported_at });
            }
            return list;
        }

        void OnEnable() => _liveData ??= new Dictionary<string, BuildingLiveData>();
    }
}