using System;
using System.Collections.Generic;
using UnityEngine;
using DUT.Data;

namespace DUT.Core
{
    public class LayerManager : MonoBehaviour
    {
        public static LayerManager Instance { get; private set; }
        public BuildingDataStore store;

        LayerId _primaryLayer = LayerId.Schedule;
        public LayerId PrimaryLayer => _primaryLayer;

        public event Action<LayerId> OnPrimaryLayerChanged;

        static readonly Color COL_EMPTY    = new Color(0.13f, 0.77f, 0.37f);
        static readonly Color COL_OCCUPIED = new Color(0.94f, 0.27f, 0.27f);
        static readonly Color COL_UPCOMING = new Color(0.96f, 0.62f, 0.04f);
        static readonly Color COL_BLUE     = new Color(0.18f, 0.56f, 0.91f);
        static readonly Color COL_MUTED    = new Color(0.48f, 0.60f, 0.72f);
        static readonly Color COL_PURPLE   = new Color(0.66f, 0.33f, 0.98f);
        static readonly Color COL_DEFAULT  = new Color(0.25f, 0.35f, 0.45f);

        void Awake()
        {
            Instance = this;
            if (store == null) store = UnityEngine.Resources.Load<BuildingDataStore>("BuildingDataStore");
        }

        public void SetPrimaryLayer(LayerId layer)
        {
            _primaryLayer = layer;
            OnPrimaryLayerChanged?.Invoke(layer);
        }

        /// <summary>Tính màu icon cho 1 building theo layer đang active.</summary>
        public Color GetBuildingColor(string buildingId)
        {
            var live = store?.GetLiveData(buildingId);
            if (live == null) return COL_DEFAULT;
            return _primaryLayer switch
            {
                LayerId.Schedule       => ScheduleColor(live.schedule.status),
                LayerId.Occupancy      => OccupancyColor(live.occupancy.density_ratio),
                LayerId.Infrastructure => InfraColor(live.infrastructure),
                LayerId.Equipment      => EquipColor(live.equipment),
                LayerId.Events         => EventColor(live.events),
                LayerId.Maintenance    => MaintColor(live.maintenance),
                _                     => COL_DEFAULT
            };
        }

        // ── Color rules per layer (theo master plan §10) ──────────────────

        static Color ScheduleColor(BuildingStatus s) => s switch
        {
            BuildingStatus.Occupied => COL_OCCUPIED,
            BuildingStatus.Empty    => COL_EMPTY,
            BuildingStatus.Upcoming => COL_UPCOMING,
            _                       => COL_DEFAULT
        };

        static Color OccupancyColor(float ratio)
        {
            if (ratio < 0.2f) return COL_EMPTY;
            if (ratio < 0.5f) return COL_BLUE;
            if (ratio < 0.8f) return COL_UPCOMING;
            return COL_OCCUPIED;
        }

        static Color InfraColor(InfrastructureData infra)
        {
            if (!infra.has_alert)                    return COL_MUTED;
            if (infra.electric.is_abnormal &&
                infra.electric.current_kw > infra.electric.avg_kw * 1.5f)
                                                     return COL_OCCUPIED;
            return COL_UPCOMING;
        }

        static Color EquipColor(EquipmentData eq)
        {
            if (eq.error_devices == 0)  return COL_MUTED;
            if (eq.has_critical_error)  return COL_OCCUPIED;
            return COL_UPCOMING;
        }

        static Color EventColor(List<EventData> events)
        {
            if (events == null || events.Count == 0) return COL_MUTED;
            foreach (var ev in events)
                if (ev.is_today && ev.attendee_count > 50) return COL_PURPLE;
            return COL_BLUE;
        }

        static Color MaintColor(MaintenanceData m) => m.overall_status switch
        {
            MaintenanceStatus.Scheduled  => new Color(0.40f, 0.70f, 0.90f),
            MaintenanceStatus.InProgress => COL_UPCOMING,
            MaintenanceStatus.Disrupted  => COL_OCCUPIED,
            MaintenanceStatus.Critical   => COL_OCCUPIED,
            _                            => COL_MUTED
        };
    }
}
