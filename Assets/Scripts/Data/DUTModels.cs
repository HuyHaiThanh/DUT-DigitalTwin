using System;
using System.Collections.Generic;
using UnityEngine;

namespace DUT.Data
{
    public enum BuildingStatus   { Unknown, Empty, Occupied, Upcoming }
    public enum BuildingFunction { GiangDuong, HanhChinh, ThiNghiem, TienIch, GDTC, KyTucXa, HoiTruong }
    public enum OccupancyLevel   { Empty, Low, Medium, High, Overcrowded }
    public enum MaintenanceStatus{ Normal, Scheduled, InProgress, Disrupted, Critical }
    public enum AlertSeverity    { Info, Warning, Critical }
    public enum AlertLayer       { Schedule, Occupancy, Infrastructure, Equipment, Event, Maintenance }
    public enum LayerId          { Schedule, Occupancy, Infrastructure, Equipment, Events, Maintenance }

    [Serializable]
    public class BuildingInfo
    {
        public string building_id;
        public string ten_khu;
        public string ten_day_du;
        public string ten_ngan;
        public int    so_tang;
        public string chuc_nang_str;
        public BuildingFunction chuc_nang;
        public Vector3 world_position;
        public Vector3 bounds_size;
        public int    so_phong;
        public int    suc_chua_toi_da;
    }

    [Serializable]
    public class ScheduleData
    {
        public BuildingStatus  status;
        public ClassInfo       current_class;
        public ClassInfo       next_class;
        public List<ClassInfo> today_classes = new List<ClassInfo>();
        public float           occupancy_rate;
    }

    [Serializable]
    public class ClassInfo
    {
        public string class_name;
        public string class_code;
        public string group;
        public string lecturer;
        public string time_start;
        public string time_end;
        public int    student_count;
        public int    student_capacity;
        public string room_id;
        public float  progress;
    }

    [Serializable]
    public class OccupancyData
    {
        public int            current_count;
        public int            max_capacity;
        public float          density_ratio;
        public OccupancyLevel level;
        public string         trend;
        public List<HourlyCount> hourly_today = new List<HourlyCount>();
    }

    [Serializable]
    public class HourlyCount { public int hour; public int count; }

    [Serializable]
    public class InfrastructureData
    {
        public ElectricData electric = new ElectricData();
        public WaterData    water    = new WaterData();
        public ClimateData  climate  = new ClimateData();
        public bool         has_alert;
    }

    [Serializable] public class ElectricData
    {
        public float current_kw; public float avg_kw; public float daily_kwh;
        public bool  is_abnormal; public string alert_msg;
    }
    [Serializable] public class WaterData
    {
        public float current_lph; public bool is_abnormal; public string alert_msg;
    }
    [Serializable] public class ClimateData
    {
        public float temperature_c; public float humidity_pct;
        public int   ac_total; public int ac_active; public int ac_error;
        public bool  is_abnormal;
    }

    [Serializable]
    public class EquipmentData
    {
        public int  total_devices; public int active_devices; public int error_devices;
        public bool has_critical_error;
        public List<DeviceError> errors = new List<DeviceError>();
    }

    [Serializable] public class DeviceError
    {
        public string device_name; public string location;
        public string error_type; public string reported_at;
        public AlertSeverity severity;
    }

    [Serializable]
    public class EventData
    {
        public string event_name; public string event_type;
        public string date; public string time_start; public string time_end;
        public string location; public int attendee_count; public bool is_today;
    }

    [Serializable]
    public class MaintenanceData
    {
        public bool              has_active_work;
        public MaintenanceStatus overall_status;
        public List<MaintenanceTicket> active_tickets   = new List<MaintenanceTicket>();
        public List<MaintenanceTicket> upcoming_tickets = new List<MaintenanceTicket>();
    }

    [Serializable] public class MaintenanceTicket
    {
        public string title; public string description;
        public string started_at; public string expected_done;
        public MaintenanceStatus status; public AlertSeverity severity;
        public string affected_area;
    }

    [Serializable]
    public class AlertData
    {
        public string alert_id; public string building_id;
        public AlertLayer layer; public AlertSeverity severity;
        public string title; public string description;
        public string timestamp; public bool is_acknowledged;
        public string action_required;
    }

    [Serializable]
    public class BuildingLiveData
    {
        public string building_id;
        public string timestamp;
        public ScheduleData       schedule       = new ScheduleData();
        public OccupancyData      occupancy      = new OccupancyData();
        public InfrastructureData infrastructure = new InfrastructureData();
        public EquipmentData      equipment      = new EquipmentData();
        public List<EventData>    events         = new List<EventData>();
        public MaintenanceData    maintenance    = new MaintenanceData();
    }
}