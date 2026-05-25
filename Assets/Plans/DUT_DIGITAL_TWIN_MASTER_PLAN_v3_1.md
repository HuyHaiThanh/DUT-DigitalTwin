# DUT DIGITAL TWIN — MASTER PLAN v3
# Đại học Bách Khoa Đà Nẵng — Operations Center
# Unity 2022.3 LTS · UI Toolkit · URP

> Tài liệu này là nguồn duy nhất của sự thật (single source of truth).
> Đọc toàn bộ trước khi implement bất kỳ script, UXML, hay USS nào.

---

## MỤC LỤC

1. Tầm nhìn & Triết lý thiết kế
2. Người dùng & Use cases
3. Kiến trúc hệ thống
4. Data Model — 6 lớp dữ liệu
5. Scene Hierarchy
6. Layer System — trái tim của Digital Twin
7. Interaction Flow — từng bước chi tiết
8. UI Architecture — 3 màn hình
9. Panel State Machine
10. Component Catalog — từng script
11. Thứ tự triển khai — 5 Sprint
12. Mock Data spec
13. Design Tokens

---

## 1. TẦM NHÌN & TRIẾT LÝ THIẾT KẾ

### Digital Twin là gì (trong context này)

Digital Twin KHÔNG phải:
- App lịch học có nhúng model 3D trang trí
- Dashboard thống kê bình thường với background 3D

Digital Twin LÀ:
- **Model 3D phản ánh trạng thái vật lý THỰC của campus tại thời điểm hiện tại**
- Mỗi tòa nhà trong 3D = một "vật thể sống" có trạng thái đa chiều
- Người dùng tương tác với không gian 3D để hiểu campus, không phải đọc bảng số liệu

### Triết lý thiết kế

```
Nguyên tắc 1 — SPATIAL FIRST
  Model 3D là giao diện chính, không phải background.
  Mọi insight đều có thể được đọc từ model 3D trước khi nhìn vào panel.

Nguyên tắc 2 — LAYERED REALITY
  Campus có nhiều "lớp thực tế" chồng lên nhau:
  lịch học, mật độ người, hạ tầng, thiết bị, sự kiện, bảo trì.
  Người dùng chọn nhìn qua "kính" nào.

Nguyên tắc 3 — CONTEXT PANEL
  Panel bên phải không bao giờ hiện thông tin cố định.
  Nó luôn phản ánh context hiện tại: đang xem gì, layer nào active, tòa nào được chọn.

Nguyên tắc 4 — ALERT-DRIVEN
  Hệ thống chủ động báo khi có bất thường.
  Người dùng không cần chủ động tìm — bất thường tự nổi lên.
```

### Cảm giác khi dùng

```
Sáng 7:30, quản lý mở app:
→ Nhìn model 3D: các tòa dần đổi màu theo lớp học bắt đầu
→ Một tòa nhà có icon ⚡ nháy — click vào: điều hòa tầng 3 gặp sự cố
→ Khu F đỏ rực — hover: 3 lớp học đang chạy, 127 người, máy chiếu F302 lỗi
→ Tắt layer lịch học, bật layer mật độ: thấy ngay đâu đông nhất
→ Một alert popup: "Phòng C401 không có người nhưng điều hòa vẫn chạy"
```

---

## 2. NGƯỜI DÙNG & USE CASES

### Primary User: Quản lý điều hành

**Profile**: Ban Giám hiệu, Phòng Quản trị Thiết bị, Phòng Đào tạo
**Màn hình**: 1920×1080, ngồi tại văn phòng hoặc chiếu trong cuộc họp
**Mục tiêu**: Nhìn vào 1 màn hình → hiểu ngay campus đang ở trạng thái gì

**Use cases chính**:

| ID | Use Case | Trigger | Expected Outcome |
|----|----------|---------|-----------------|
| UC1 | Giám sát tổng quan campus | Mở app | Thấy ngay số phòng đang dùng, alert nổi bật, sự kiện hôm nay |
| UC2 | Kiểm tra tòa nhà cụ thể | Click tòa nhà | Camera fly-to, panel hiện 6 chiều thông tin |
| UC3 | Phát hiện bất thường | Alert notification | Biết ngay vị trí, loại sự cố, mức độ nghiêm trọng |
| UC4 | So sánh mật độ sử dụng | Bật layer Mật độ | Model 3D đổi màu heatmap, thấy đâu đông/vắng |
| UC5 | Kiểm tra hạ tầng | Bật layer Hạ tầng | Thấy tòa nào đang tiêu thụ điện/nước bất thường |
| UC6 | Xem lịch sự kiện | Màn hình Operations | Timeline 7 ngày, conflict detection |
| UC7 | Quản lý bảo trì | Tab Bảo trì trong panel | Danh sách ticket, lịch định kỳ |
| UC8 | Export báo cáo | Dashboard | Xuất PDF báo cáo sử dụng tuần/tháng |

### Secondary Users

- **Kỹ thuật viên**: Xem tab Thiết bị + Bảo trì, báo sự cố mới
- **Phòng đào tạo**: Xem lịch học, tìm phòng trống, booking
- **Bảo vệ/An ninh**: Layer An toàn, kiểm soát ra vào (tương lai)

---

## 3. KIẾN TRÚC HỆ THỐNG

### Tổng quan

```
┌─────────────────────────────────────────────────────────────┐
│                    DUT DIGITAL TWIN                          │
├─────────────────────────────────────────────────────────────┤
│  DATA LAYER                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐  │
│  │ BuildingData │  │ MockDataPro- │  │  ApiManager      │  │
│  │ Store.asset  │  │ vider.cs     │  │  (future)        │  │
│  │ (ScriptObj)  │  │              │  │                  │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘  │
│         └─────────────────┴──────────────────┘             │
│                           │                                  │
│  LOGIC LAYER              ▼                                  │
│  ┌────────────┐  ┌────────────────┐  ┌──────────────────┐  │
│  │LayerManager│  │ UIManager      │  │ AlertManager     │  │
│  │.cs         │  │ .cs            │  │ .cs              │  │
│  └────────────┘  └────────────────┘  └──────────────────┘  │
│         │                │                    │              │
│  3D LAYER                │            UI LAYER              │
│  ┌────────────┐          │         ┌──────────────────────┐ │
│  │BuildingObj │          │         │ UI Toolkit           │ │
│  │ect.cs      │          │         │ DUT_Main.uxml        │ │
│  │WorldSpace  │          └────────►│ DUT_Dashboard.uxml   │ │
│  │Label.cs    │                    │ DUT_Schedule.uxml    │ │
│  │CameraCtrl  │                    │ DUT_Main.uss         │ │
│  │.cs         │                    └──────────────────────┘ │
│  └────────────┘                                              │
└─────────────────────────────────────────────────────────────┘
```

### Tech Stack

| Thành phần | Lựa chọn | Lý do |
|------------|----------|-------|
| Unity | 2022.3 LTS | Stable, LTS support |
| Render pipeline | URP | Nhẹ hơn HDRP, đủ cho demo |
| UI System | **UI Toolkit** | render_ui preview được qua MCP |
| Data serialization | Newtonsoft.Json | Mạnh hơn JsonUtility |
| HTTP | UnityWebRequest | Built-in |
| Animation | DOTween (nếu cần) | Smooth tweens |
| Namespace | DUT.* | DUT.Data, DUT.UI, DUT.Core |

---

## 4. DATA MODEL — 6 LỚP DỮ LIỆU

### 4.1 BuildingInfo (static — load một lần)

```csharp
namespace DUT.Data
{
    [Serializable]
    public class BuildingInfo
    {
        // Identity
        public string building_id;      // "Toa_A_HanhChinh"
        public string ten_khu;          // "A"
        public string ten_day_du;       // "Tòa nhà A"
        public int    so_tang;          // 4
        public string chuc_nang_str;    // "Khu hành chính"
        public BuildingFunction chuc_nang;

        // Physical
        public Vector3 world_position;  // từ scene
        public float   dien_tich_m2;    // m² sàn
        public int     so_phong;        // tổng số phòng/lab

        // Capacity
        public int suc_chua_toi_da;     // người tối đa
    }
}
```

### 4.2 BuildingLiveData (dynamic — cập nhật mỗi 30s)

```csharp
[Serializable]
public class BuildingLiveData
{
    public string building_id;
    public string timestamp;            // ISO 8601

    // Layer 1: Lịch học
    public ScheduleData schedule;

    // Layer 2: Mật độ
    public OccupancyData occupancy;

    // Layer 3: Hạ tầng
    public InfrastructureData infrastructure;

    // Layer 4: Thiết bị
    public EquipmentData equipment;

    // Layer 5: Sự kiện
    public List<EventData> events;

    // Layer 6: Bảo trì
    public MaintenanceData maintenance;
}
```

### 4.3 ScheduleData (Layer 1)

```csharp
[Serializable]
public class ScheduleData
{
    public BuildingStatus status;       // Empty/Occupied/Upcoming
    public ClassInfo current_class;
    public ClassInfo next_class;
    public List<ClassInfo> today_classes; // toàn bộ lịch hôm nay
    public float occupancy_rate;        // 0.0–1.0 tỷ lệ phòng đang dùng
}

[Serializable]
public class ClassInfo
{
    public string class_name;
    public string class_code;
    public string group;
    public string lecturer;
    public string time_start;           // "07:30"
    public string time_end;             // "09:30"
    public int    student_count;
    public int    student_capacity;
    public string room_id;              // phòng cụ thể (nếu có)
}
```

### 4.4 OccupancyData (Layer 2)

```csharp
[Serializable]
public class OccupancyData
{
    public int   current_count;         // số người hiện tại
    public int   max_capacity;          // sức chứa tối đa
    public float density_ratio;         // current/max, 0.0–1.0
    public OccupancyLevel level;        // Empty/Low/Medium/High/Overcrowded
    public List<HourlyCount> hourly_today; // biểu đồ theo giờ
}

public enum OccupancyLevel { Empty, Low, Medium, High, Overcrowded }

[Serializable]
public class HourlyCount
{
    public int hour;                    // 7–21
    public int count;
}
```

### 4.5 InfrastructureData (Layer 3)

```csharp
[Serializable]
public class InfrastructureData
{
    public ElectricData  electric;
    public WaterData     water;
    public ClimateData   climate;
    public bool          has_alert;     // true nếu bất kỳ subsystem bất thường
}

[Serializable]
public class ElectricData
{
    public float current_kw;
    public float avg_kw_same_hour;      // trung bình giờ này mọi ngày
    public float daily_kwh;             // đã dùng hôm nay
    public bool  is_abnormal;           // > 120% trung bình
    public string alert_message;        // nếu is_abnormal
}

[Serializable]
public class WaterData
{
    public float current_lph;           // lít/giờ
    public bool  is_abnormal;
    public string alert_message;
}

[Serializable]
public class ClimateData
{
    public float temperature_c;
    public float humidity_pct;
    public int   ac_total;              // tổng số điều hòa
    public int   ac_active;             // đang chạy
    public int   ac_error;              // bị lỗi
    public bool  is_abnormal;
}
```

### 4.6 EquipmentData (Layer 4)

```csharp
[Serializable]
public class EquipmentData
{
    public int total_devices;
    public int active_devices;
    public int error_devices;
    public int idle_devices;
    public List<DeviceGroup> groups;    // nhóm theo loại
    public bool has_critical_error;
}

[Serializable]
public class DeviceGroup
{
    public string type;                 // "Máy chiếu", "Máy tính", "Thiết bị lab"
    public int total;
    public int working;
    public int error;
    public List<DeviceError> errors;    // danh sách thiết bị lỗi cụ thể
}

[Serializable]
public class DeviceError
{
    public string device_id;
    public string device_name;
    public string location;             // "Phòng A301 - Máy chiếu"
    public string error_type;           // "Không kết nối", "Quá nhiệt"
    public string reported_at;
    public AlertSeverity severity;
}
```

### 4.7 EventData (Layer 5)

```csharp
[Serializable]
public class EventData
{
    public string event_id;
    public string event_name;           // "Bảo vệ luận văn K20"
    public string event_type;           // "exam", "defense", "conference", "ceremony"
    public string date;                 // "2025-05-14"
    public string time_start;
    public string time_end;
    public string location;             // tòa nhà + phòng
    public int    attendee_count;
    public bool   is_today;
    public bool   is_recurring;
}
```

### 4.8 MaintenanceData (Layer 6)

```csharp
[Serializable]
public class MaintenanceData
{
    public bool has_active_work;        // đang có công việc đang làm
    public List<MaintenanceTicket> active_tickets;
    public List<MaintenanceTicket> upcoming_tickets;
    public MaintenanceStatus overall_status;
}

public enum MaintenanceStatus
{
    Normal,         // không có gì
    Scheduled,      // có lịch sắp tới
    InProgress,     // đang thực hiện (không ảnh hưởng hoạt động)
    Disrupted,      // đang ảnh hưởng hoạt động (thang máy, điện, v.v.)
    Critical        // sự cố nghiêm trọng
}

[Serializable]
public class MaintenanceTicket
{
    public string ticket_id;
    public string title;                // "Sửa thang máy tầng 3"
    public string description;
    public string assigned_to;
    public string started_at;
    public string expected_done;
    public MaintenanceStatus status;
    public AlertSeverity severity;
    public string affected_area;        // "Tầng 3, 4 — Tòa A"
}
```

### 4.9 AlertData (cross-cutting)

```csharp
[Serializable]
public class AlertData
{
    public string alert_id;
    public string building_id;
    public AlertLayer layer;            // Infrastructure, Equipment, v.v.
    public AlertSeverity severity;      // Info, Warning, Critical
    public string title;                // "Máy chiếu A301 không kết nối"
    public string description;
    public string timestamp;
    public bool   is_acknowledged;
    public string action_required;      // hướng dẫn xử lý
}

public enum AlertLayer
{
    Schedule, Occupancy, Infrastructure,
    Equipment, Event, Maintenance, Security
}

public enum AlertSeverity { Info, Warning, Critical }
```

---

## 5. SCENE HIERARCHY

```
SampleScene
├── Campus (root model)
│   ├── Buildings
│   │   ├── Building_A          ← có BuildingObject.cs
│   │   │   └── Khu_A_group     ← mesh thực tế
│   │   ├── Building_B
│   │   │   └── ...
│   │   └── ... (12 buildings)
│   ├── Landmarks
│   ├── Workshops
│   ├── KyTucXa
│   └── Environment
│
├── CameraRig               ← GameObject rỗng làm pivot
│   └── Main Camera         ← có CameraController.cs
│
├── WorldSpaceLabels        ← container cho tất cả labels 3D
│   ├── Label_Toa_A         ← có WorldSpaceLabel.cs
│   ├── Label_Toa_B
│   └── ...
│
├── Managers                ← GameObject rỗng chứa các Manager
│   ├── LayerManager        ← có LayerManager.cs
│   ├── AlertManager        ← có AlertManager.cs
│   ├── UIManager           ← có UIManager.cs
│   └── DataProvider        ← có MockDataProvider.cs (sau này đổi sang ApiManager)
│
├── DUT_UI                  ← UIDocument (UI Toolkit)
│   └── [UIDocument component] → DUT_Main.uxml
│
└── Lighting
    └── Directional Light
```

---

## 6. LAYER SYSTEM — TRÁI TIM CỦA DIGITAL TWIN

### 6.1 Các Layer

| Layer ID | Tên hiển thị | Icon | Màu model khi active | Data source |
|----------|-------------|------|---------------------|-------------|
| SCHEDULE | Lịch học | 📚 | Đỏ/Xanh/Vàng theo status | ScheduleData |
| OCCUPANCY | Mật độ | 👥 | Gradient xanh→cam→đỏ theo density | OccupancyData |
| INFRASTRUCTURE | Hạ tầng | ⚡ | Xám bình thường, cam/đỏ khi bất thường | InfrastructureData |
| EQUIPMENT | Thiết bị | 🖥 | Xám bình thường, đỏ khi có lỗi | EquipmentData |
| EVENTS | Sự kiện | 📅 | Tím khi có sự kiện đặc biệt | EventData |
| MAINTENANCE | Bảo trì | 🔧 | Cam khi đang thi công, đỏ khi disrupted | MaintenanceData |

### 6.2 LayerManager.cs — Spec chi tiết

```csharp
namespace DUT.Core
{
    public class LayerManager : MonoBehaviour
    {
        // State
        private HashSet<LayerId> _activeLayers;
        private LayerId _primaryLayer;   // layer nào đang "drive" màu model

        // Events
        public event Action<LayerId, bool> OnLayerToggled;
        public event Action<LayerId>       OnPrimaryLayerChanged;

        // API
        public void ToggleLayer(LayerId layer);
        public void SetPrimaryLayer(LayerId layer);
        public bool IsActive(LayerId layer);
        public Color GetBuildingColor(string buildingId); // tính màu theo primary layer
    }
}
```

### 6.3 Logic tính màu tòa nhà theo layer

```
PRIMARY LAYER = SCHEDULE:
  Occupied  → rgb(239, 68, 68)    đỏ
  Empty     → rgb(34, 197, 94)    xanh lá
  Upcoming  → rgb(245, 158, 11)   vàng
  Unknown   → rgb(61, 88, 112)    xám

PRIMARY LAYER = OCCUPANCY:
  density_ratio 0.0–0.2  → rgb(34, 197, 94)     xanh lá (vắng)
  density_ratio 0.2–0.5  → rgb(46, 143, 232)    xanh dương (bình thường)
  density_ratio 0.5–0.8  → rgb(245, 158, 11)    vàng cam (đông)
  density_ratio 0.8–1.0  → rgb(239, 68, 68)     đỏ (đông)
  density_ratio > 1.0    → rgb(168, 85, 247)     tím (quá tải)

PRIMARY LAYER = INFRASTRUCTURE:
  has_alert = false       → rgb(61, 88, 112)    xám mặc định
  has_alert = true,
    severity = Warning    → rgb(245, 158, 11)    cam
    severity = Critical   → rgb(239, 68, 68)     đỏ nháy

PRIMARY LAYER = EQUIPMENT:
  error_devices = 0       → rgb(61, 88, 112)    xám
  error_devices > 0,
    has_critical = false  → rgb(245, 158, 11)    cam
    has_critical = true   → rgb(239, 68, 68)     đỏ

PRIMARY LAYER = EVENTS:
  Không có sự kiện hôm nay → xám
  Có sự kiện thường        → rgb(46, 143, 232)   xanh
  Có sự kiện lớn (>50 người) → rgb(168, 85, 247) tím

PRIMARY LAYER = MAINTENANCE:
  Normal     → xám
  Scheduled  → rgb(46, 143, 232)   xanh nhạt
  InProgress → rgb(245, 158, 11)   cam
  Disrupted  → rgb(239, 68, 68)    đỏ
  Critical   → đỏ nháy (pulse animation)
```

### 6.4 Material System

```
Mỗi BuildingObject có 3 bộ material:
  _matDefault   → trạng thái khi không có layer nào active
  _matHighlight → đang hover
  _matSelected  → đang được chọn (outline trắng + màu layer)

Cách áp dụng màu:
  Không dùng Material.color thay đổi mỗi frame
  Thay vào đó: dùng MaterialPropertyBlock để set _BaseColor
  → Hiệu năng tốt hơn, không tạo instance mới
```

---

## 7. INTERACTION FLOW — CHI TIẾT TỪNG BƯỚC

### 7.1 Default State (không chọn gì)

```
Camera: vị trí overview nhìn toàn campus
Model 3D: màu theo primary layer đang active
Panel phải: Campus Overview (xem mục 9.1)
WorldSpace labels: ẩn (hoặc hiện dot nhỏ theo zoom level)
```

### 7.2 Hover tòa nhà

```
Trigger: Mouse enter collider của BuildingObject

Actions:
1. Material → _matHighlight (viền sáng nhẹ)
2. WorldSpaceLabel → hiện tooltip nhỏ:
   ┌─────────────────────┐
   │ Tòa nhà A           │
   │ ● 3 lớp đang chạy  │
   │ 👥 127 người        │
   └─────────────────────┘
3. Cursor → change to pointer

KHÔNG:
- KHÔNG move camera
- KHÔNG thay đổi panel
- KHÔNG show full info
```

### 7.3 Click tòa nhà

```
Trigger: Mouse click trên collider

Actions (theo thứ tự):
1. BuildingObject.SetSelected(true)
   → Material → _matSelected
   → Deselect tòa nhà cũ nếu có

2. CameraController.FlyTo(building.transform.position)
   → Smooth tween 0.8s
   → Target position: nhìn thẳng vào facade chính của tòa nhà
   → Target distance: dựa trên bounds size (lớn hơn → lùi xa hơn)
   → Giữ góc nhìn isometric (pitch ~35°)

3. UIManager.SelectBuilding(buildingId)
   → Panel state → BUILDING_SELECTED
   → Load data từ BuildingDataStore
   → Animate panel slide-in từ phải

4. WorldSpaceLabel.ShowFull(buildingId)
   → Expand tooltip thành label đầy đủ
   → Vị trí: phía trên tòa nhà, luôn quay về camera
```

### 7.4 Click empty space (deselect)

```
Trigger: Click vào area không phải BuildingObject

Actions:
1. Deselect tòa nhà hiện tại (nếu có)
2. CameraController.ReturnToOverview()
   → Tween về vị trí overview mặc định, 0.6s
3. UIManager.ClearSelection()
   → Panel state → NO_SELECTION (Campus Overview)
4. WorldSpaceLabel → ẩn full view, về dot nhỏ
```

### 7.5 Click chip Khu (filter)

```
Trigger: Click chip "A", "B", v.v. trong panel

Actions:
1. LayerManager: highlight tất cả tòa nhà thuộc khu đó
2. Camera: pan nhẹ để framing khu đó vào center
3. Panel state → KHU_SELECTED (xem mục 9.3)
4. Các tòa ngoài khu: dim (alpha giảm)
```

### 7.6 Toggle Layer

```
Trigger: Click layer button trong topbar

Actions:
1. LayerManager.ToggleLayer(layerId)
2. Model 3D: tất cả tòa nhà re-evaluate màu dựa trên layer mới
   → Không tween màu (instant change để dễ đọc)
3. WorldSpaceLabel: update icon/badge theo layer
4. Nếu có tòa nhà được chọn: panel cập nhật tab tương ứng
5. Legend panel (bottom-left): cập nhật legend theo layer active
```

### 7.7 Camera Controls

```
Chuột phải giữ + drag   → Orbit xung quanh pivot
Scroll wheel            → Zoom in/out (giới hạn min/max)
Chuột giữa giữ + drag  → Pan
Double click tòa nhà   → Zoom sát hơn (FocusClose)
Phím F                  → Frame lại toàn campus
Phím Escape             → Deselect, về overview

Zoom levels:
  OVERVIEW  (distance > 400m): Không hiện WorldSpaceLabel
  CAMPUS    (200–400m):        Hiện dot nhỏ + tên khu
  BUILDING  (80–200m):         Hiện tên tòa + status icon
  CLOSE     (< 80m):           Hiện full tooltip
```

---

## 8. UI ARCHITECTURE — 3 MÀN HÌNH

### 8.1 Layout tổng quan

```
Tất cả 3 màn hình dùng chung:
- Topbar (52px, fixed)
- Layout 65/35 (viewport 3D / panel phải)

3D Viewport (65%):
  Không có UI element cứng bên trong
  Chỉ có WorldSpaceLabel (được render bởi 3D Camera, không phải UI)
  Overlay buttons (search, zoom, layer toggles) dùng position: absolute

Panel phải (35% = ~672px ở 1920):
  Thay đổi nội dung theo screen và selection state
```

### 8.2 Màn hình 1 — CAMPUS LIVE

```
Topbar:
  [Logo] [Campus Live ●] [Operations] [Schedule]
  [Layer toggles: 📚 👥 ⚡ 🖥 📅 🔧]  ← cái mới
  [Alert bell 🔔 (3)] [Live · 08:42]

Viewport overlays (position: absolute):
  Top-left:   Search bar
  Top-right:  Layer toggles (if not in topbar)
  Bottom-left: Legend (thay đổi theo active layer)
  Bottom-left (above legend): MiniStats nếu không có gì được chọn

Panel phải:
  → Thay đổi theo selection state (xem mục 9)
```

### 8.3 Màn hình 2 — OPERATIONS DASHBOARD

```
Không có model 3D (hoặc thu nhỏ thành thumbnail)
Full screen dashboard:

Layout:
  ┌─────────────────────────────────────────────────────┐
  │ TOPBAR                                              │
  ├───────┬─────────────────────────────┬───────────────┤
  │ LEFT  │       CENTER                │ RIGHT         │
  │ Filter│  Alert Feed (live)          │ Quick Actions │
  │ panel │  ─────────────────          │               │
  │       │  Resource Overview          │ Upcoming      │
  │       │  (Điện/Nước/Thiết bị)       │ Events (7d)  │
  │       │  ─────────────────          │               │
  │       │  Utilization Charts         │ Maintenance   │
  │       │  (Bar + Heatmap)            │ Queue         │
  └───────┴─────────────────────────────┴───────────────┘

Alert Feed (component quan trọng nhất):
  Hiển thị real-time alerts từ AlertManager
  Mỗi alert có: severity badge, building, message, time, [Xem]
  Màu theo severity: Info=xanh, Warning=vàng, Critical=đỏ nháy

Resource Overview:
  3 card: Điện (kWh hôm nay), Nước (m³), Thiết bị lỗi (n/tổng)
  Trending: so sánh với ngày hôm qua / trung bình tuần

Utilization Charts:
  Bar chart: % phòng có lịch theo từng giờ trong ngày
  Heatmap: Tòa × Giờ (đã có, giữ lại)
  Top 5: tòa nhà sử dụng nhiều nhất hôm nay
```

### 8.4 Màn hình 3 — SCHEDULE & BOOKING

```
Giữ nguyên Schedule screen hiện tại, thêm:
  - Tab "Đặt phòng" bên cạnh timeline
  - Conflict detection: highlight đỏ nếu 2 sự kiện trùng phòng
  - "Tìm phòng trống": nhập tiêu chí → gợi ý phòng phù hợp
  - Nút "Export lịch" → PDF/Excel
```

---

## 9. PANEL STATE MACHINE

### 9.1 State: NO_SELECTION — Campus Overview

```
Trigger: App vừa mở, hoặc click empty space

Nội dung panel:
┌─────────────────────────────┐
│ CAMPUS LIVE                 │
│ Thứ 3 · 14/05/2025 · 08:42 │
├─────────────────────────────┤
│ TỔNG QUAN                   │
│  47  31  12  90             │
│ lớp trg trp sắp tổng        │
├─────────────────────────────┤
│ ALERTS (3)         [Xem tất]│
│ ⚠ Máy chiếu A301 lỗi  08:30│
│ ⚠ Điều hòa F302 cao  08:15 │
│ ℹ Thang máy A bảo trì       │
├─────────────────────────────┤
│ SỰ KIỆN HÔM NAY             │
│ 📅 14:00 Bảo vệ LV K20 · A  │
│ 📅 16:00 Họp Hội đồng · F  │
├─────────────────────────────┤
│ LAYER ACTIVE                │
│ [📚 Lịch học ●] [hiện tại] │
│ Chọn layer để xem campus    │
│ theo chiều dữ liệu khác     │
└─────────────────────────────┘
```

### 9.2 State: BUILDING_SELECTED

```
Trigger: Click tòa nhà

Header cố định:
┌─────────────────────────────┐
│ [← Quay lại]  [KHU A]       │
│ Tòa nhà A                   │
│ 4 tầng · Khu hành chính     │
│ [Khu hành chính] [tag]      │
├─────────────────────────────┤
│ [Lịch] [Mật độ] [Hạ tầng]  │ ← 6 tabs
│ [Thiết bị] [Sự kiện] [Bảo trì]│
└─────────────────────────────┘

TAB: LỊCH HỌC (default khi select + layer Schedule)
  Status banner (màu theo status)
  Lớp hiện tại: tên, mã, giảng viên, giờ, sv/max, progress
  Timeline hôm nay: visual bar
  Lớp tiếp theo: giờ + tên
  [Xem lịch đầy đủ]

TAB: MẬT ĐỘ (active khi layer Occupancy bật)
  Số người: 127 / 200 (vòng tròn progress)
  Mức độ: [BÌNH THƯỜNG] badge
  Biểu đồ theo giờ hôm nay
  So sánh: "Đông hơn 15% so với T3 tuần trước"

TAB: HẠ TẦNG
  ⚡ Điện: 45 kW  (trung bình: 38 kW)  ⚠ Cao hơn 18%
     [Chart nhỏ 1h gần nhất]
  💧 Nước: 280 lít/h  Bình thường
  🌡 Nhiệt độ: 28.5°C  Độ ẩm: 72%
  ❄ Điều hòa: 8/12 hoạt động  (2 đang tắt lịch, 2 lỗi)

TAB: THIẾT BỊ
  Tổng: 47 thiết bị
  ✅ Hoạt động: 43    ❌ Lỗi: 2    😴 Không dùng: 2
  
  DANH SÁCH LỖI:
  ❌ Máy chiếu A301 — Không kết nối — Báo 08:30
     [Tạo ticket bảo trì]
  ⚠ Máy tính A412 — Quá nhiệt — Báo 07:45
     [Xem chi tiết]

TAB: SỰ KIỆN
  HÔM NAY:
  📅 14:00–16:30  Bảo vệ luận văn K20 · A401
     48 người · Hội đồng GS.TS Nguyễn A
  
  TUẦN NÀY:
  📅 T4 09:00  Hội thảo IoT · A201
  📅 T6 14:00  Họp Hội đồng Khoa

TAB: BẢO TRÌ
  ĐANG THỰC HIỆN:
  🔧 Sơn lại hành lang tầng 2     ← Không ảnh hưởng
     Từ 10/05 · Dự kiến xong 20/05
  
  ⚠ Thang máy tầng 3–4 dừng      ← Ảnh hưởng di chuyển
     Từ 07/05 · Đang chờ linh kiện
  
  LỊCH SẮP TỚI:
  📋 20/05  Kiểm tra PCCC định kỳ
  
  [+ Báo sự cố mới]
```

### 9.3 State: KHU_SELECTED

```
Trigger: Click chip khu (A, B, C...)

┌─────────────────────────────┐
│ [← Quay lại]                │
│ KHU A — 4 tòa nhà           │
├─────────────────────────────┤
│ DANH SÁCH:                  │
│                             │
│ ● Tòa nhà A  [Đang có lớp] │
│   47 kW · 127 người · 3 lớp│
│   ⚠ Máy chiếu A301 lỗi     │
│                    [Chi tiết]│
│ ─────────────────────────── │
│ ○ Lab A  [Trống]            │
│   12 kW · 8 người           │
│                    [Chi tiết]│
│ ─────────────────────────── │
│ ◔ PTN A  [Sắp có lớp 09:30]│
│   18 kW · 45 người          │
│                    [Chi tiết]│
├─────────────────────────────┤
│ TỔNG KHU A:                 │
│ 65 kW · 180 người · 1 alert │
└─────────────────────────────┘
```

### 9.4 State: ALERT_DETAIL

```
Trigger: Click vào một alert cụ thể

┌─────────────────────────────┐
│ [← Quay lại]  ⚠ WARNING     │
│ Máy chiếu A301 không kết nối│
├─────────────────────────────┤
│ TÒA NHÀ: A · Phòng A301    │
│ PHÁT HIỆN: 08:30 hôm nay   │
│ TRẠNG THÁI: Chưa xử lý     │
├─────────────────────────────┤
│ MÔ TẢ:                      │
│ Máy chiếu Epson EB-X51      │
│ Số series: EP-2024-0123     │
│ Mất kết nối HDMI từ 08:28  │
├─────────────────────────────┤
│ HƯỚNG XỬ LÝ:               │
│ 1. Kiểm tra kết nối HDMI   │
│ 2. Khởi động lại máy chiếu │
│ 3. Nếu không được → tạo    │
│    ticket kỹ thuật          │
├─────────────────────────────┤
│ [Đánh dấu đã xử lý]        │
│ [Tạo ticket bảo trì]        │
│ [Giao cho kỹ thuật viên]    │
└─────────────────────────────┘
```

---

## 10. COMPONENT CATALOG

### 10.1 BuildingObject.cs

```csharp
// Attach vào: mỗi tòa nhà (building_id group)
// Dependencies: BuildingDataStore, LayerManager

public class BuildingObject : MonoBehaviour
{
    // Inspector fields
    [SerializeField] string _buildingId;    // "Toa_A_HanhChinh"
    [SerializeField] BuildingDataStore _store;
    [SerializeField] LayerManager _layerManager;
    [SerializeField] Material _matDefault, _matHover, _matSelected;

    // State
    bool _isSelected;
    bool _isHovered;
    Renderer[] _renderers;
    MaterialPropertyBlock _propBlock;

    // Public API
    public string BuildingId => _buildingId;
    public void SetSelected(bool value);
    public void SetHovered(bool value);
    public void RefreshColor();         // gọi khi layer thay đổi

    // Events (Unity Messages)
    void OnMouseEnter();                // SetHovered(true)
    void OnMouseExit();                 // SetHovered(false)
    void OnMouseDown();                 // fire BuildingDataStore.SelectBuilding()

    // Internal
    void ApplyMaterial();               // dùng MaterialPropertyBlock
    Color GetCurrentColor();            // query LayerManager
}
```

### 10.2 CameraController.cs

```csharp
// Attach vào: Main Camera (hoặc CameraRig)
// Dependencies: BuildingDataStore

public class CameraController : MonoBehaviour
{
    // Inspector fields
    [SerializeField] float orbitSpeed = 120f;
    [SerializeField] float zoomSpeed = 20f;
    [SerializeField] float minDist = 30f, maxDist = 600f;
    [SerializeField] float minPitch = 15f, maxPitch = 80f;
    [SerializeField] float flyDuration = 0.8f;

    // State
    float _yaw, _pitch, _distance;
    Vector3 _pivotPos;
    bool _isFlying;
    Coroutine _flyCoroutine;

    // Public API
    public void FlyTo(Vector3 targetPos, float targetDist = -1);
    public void ReturnToOverview();     // về vị trí mặc định
    public void FocusKhu(string khuId); // frame khu vào center
    public void SetOrbitEnabled(bool v); // tắt khi đang fly

    // Input handling
    void HandleOrbit();
    void HandleZoom();
    void HandlePan();
    void HandleKeyboard();

    // Fly animation
    IEnumerator Co_FlyTo(Vector3 pos, float dist);  // SmoothStep easing
}
```

### 10.3 WorldSpaceLabel.cs

```csharp
// Attach vào: mỗi WorldSpaceLabel GameObject
// Dependencies: BuildingDataStore, LayerManager

public class WorldSpaceLabel : MonoBehaviour
{
    [SerializeField] string _buildingId;
    [SerializeField] Canvas _canvas;        // World Space Canvas
    [SerializeField] RectTransform _rt;
    [SerializeField] TMP_Text _txtName, _txtStatus, _txtExtra;
    [SerializeField] Image _bgImage, _statusDot;
    [SerializeField] float _showDistance = 200f; // chỉ hiện khi camera < 200m

    // Public API
    public void SetBuildingId(string id);
    public void UpdateContent();        // refresh theo data + active layer
    public void SetMode(LabelMode mode); // Hidden, Dot, Mini, Full

    // Internal
    void Update();          // Billboard effect (quay về camera)
    void CheckVisibility(); // ẩn/hiện theo zoom level
    LabelContent BuildContent(); // query data theo active layer
}

public enum LabelMode { Hidden, Dot, Mini, Full }
```

### 10.4 LayerManager.cs

```csharp
// Attach vào: Managers GameObject (singleton)
// Dependencies: BuildingDataStore, tất cả BuildingObject

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    // State
    private Dictionary<LayerId, bool> _layerStates;
    private LayerId _primaryLayer = LayerId.Schedule;

    // Events
    public event Action<LayerId, bool>  OnLayerToggled;
    public event Action<LayerId>        OnPrimaryLayerChanged;
    public event Action                 OnLayersChanged; // broadcast đến tất cả BuildingObject

    // Public API
    public void ToggleLayer(LayerId layer);
    public void SetPrimaryLayer(LayerId layer);
    public bool IsActive(LayerId layer);
    public LayerId PrimaryLayer => _primaryLayer;
    public Color GetBuildingColor(string buildingId); // tính màu theo primary layer + data
    public string GetBuildingBadge(string buildingId); // "3 lớp · 127 người"
}
```

### 10.5 AlertManager.cs

```csharp
// Attach vào: Managers GameObject
// Dependencies: BuildingDataStore

public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance;

    // State
    private List<AlertData> _activeAlerts;
    private int _unreadCount;

    // Events
    public event Action<AlertData>  OnNewAlert;
    public event Action<AlertData>  OnAlertAcknowledged;
    public event Action<int>        OnUnreadCountChanged;

    // Public API
    public List<AlertData> GetAlerts(AlertSeverity? filter = null);
    public List<AlertData> GetAlertsForBuilding(string buildingId);
    public void Acknowledge(string alertId);
    public void AcknowledgeAll();
    public int UnreadCount => _unreadCount;

    // Internal (được gọi bởi MockDataProvider)
    public void IngestData(BuildingLiveData data); // tự sinh alert nếu phát hiện bất thường
    void DetectAnomalies(BuildingLiveData data);   // rule-based anomaly detection
}
```

### 10.6 UIManager.cs

```csharp
// Attach vào: DUT_UI GameObject
// Dependencies: BuildingDataStore, LayerManager, AlertManager

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    // Panel state machine
    private PanelState _currentState = PanelState.NoSelection;
    private string _selectedBuildingId;
    private string _selectedKhu;
    private PanelTab _activeTab = PanelTab.Schedule;

    // UI Toolkit references
    private UIDocument _doc;
    private VisualElement _root;
    // ... các element references

    // Public API
    public void SelectBuilding(string id);
    public void SelectKhu(string khu);
    public void ClearSelection();
    public void SwitchScreen(ScreenId screen);
    public void SwitchTab(PanelTab tab);
    public void ShowAlert(AlertData alert);

    // Internal panel builders
    void BuildPanelNoSelection();
    void BuildPanelBuilding(string id);
    void BuildPanelKhu(string khu);
    void BuildPanelAlertDetail(AlertData alert);

    // Tab builders
    void BuildTabSchedule(BuildingLiveData data);
    void BuildTabOccupancy(BuildingLiveData data);
    void BuildTabInfrastructure(BuildingLiveData data);
    void BuildTabEquipment(BuildingLiveData data);
    void BuildTabEvents(BuildingLiveData data);
    void BuildTabMaintenance(BuildingLiveData data);
}

public enum PanelState { NoSelection, BuildingSelected, KhuSelected, AlertDetail }
public enum ScreenId { CampusLive, Operations, Schedule }
public enum PanelTab { Schedule, Occupancy, Infrastructure, Equipment, Events, Maintenance }
```

### 10.7 MockDataProvider.cs

```csharp
// Attach vào: DataProvider GameObject
// Tự sinh BuildingLiveData đầy đủ 6 layers cho ~46 tòa nhà

public class MockDataProvider : MonoBehaviour
{
    [SerializeField] BuildingDataStore _store;
    [SerializeField] float _updateInterval = 30f;   // cập nhật mỗi 30s
    [SerializeField] float _flickerInterval = 60f;  // random thay đổi nhỏ

    void Start();
    void LoadBuildingInfos();                        // static data
    void LoadAllLiveData();                          // sinh live data lần đầu
    BuildingLiveData GenerateLiveData(string id);   // sinh cho 1 tòa
    ScheduleData      GenerateSchedule(string id);
    OccupancyData     GenerateOccupancy(string id);
    InfrastructureData GenerateInfra(string id);
    EquipmentData     GenerateEquipment(string id);
    List<EventData>   GenerateEvents(string id);
    MaintenanceData   GenerateMaintenance(string id);
    IEnumerator Co_FlickerData();                   // simulate real-time changes
}
```

### 10.8 BuildingDataStore.cs (ScriptableObject)

```csharp
// ScriptableObject — dùng chung bởi tất cả scripts
// Tạo asset: Assets/Data/BuildingDataStore.asset

[CreateAssetMenu]
public class BuildingDataStore : ScriptableObject
{
    // Static data (load 1 lần)
    public List<BuildingInfo> AllBuildings;

    // Live data (cập nhật theo interval)
    private Dictionary<string, BuildingLiveData> _liveData;

    // Selection state
    public string SelectedBuildingId { get; private set; }
    public string SelectedKhu        { get; private set; }

    // Events
    public event Action<string>          OnBuildingSelected;
    public event Action<string>          OnBuildingLiveDataUpdated;
    public event Action                  OnAllDataRefreshed;

    // Public API
    public BuildingInfo     GetInfo(string id);
    public BuildingLiveData GetLiveData(string id);
    public void UpdateLiveData(BuildingLiveData data);
    public void SelectBuilding(string id);
    public void SelectKhu(string khu);
    public void ClearSelection();
    public List<BuildingInfo> GetBuildingsInKhu(string khu);
    public BuildingStatus    GetStatus(string id); // shortcut
    public int               CountByStatus(BuildingStatus s);
}
```

---

## 11. THỨ TỰ TRIỂN KHAI — 5 SPRINT

### Sprint 1 — Foundation & Interaction (1–2 tuần)

```
Mục tiêu: Click tòa nhà → camera fly → panel hiện thông tin

1.1  BuildingDataStore.cs          (ScriptableObject)
1.2  DUTModels.cs                  (toàn bộ 8 data classes)
1.3  MockDataProvider.cs           (sinh data 6 layers)
1.4  BuildingObject.cs             (click, hover, material)
     → Test: click tòa nhà → panel hiện tên + trạng thái
1.5  CameraController.cs           (orbit, fly-to)
     → Test: click → camera fly smooth đến tòa nhà
1.6  WorldSpaceLabel.cs            (billboard tooltip)
     → Test: hover → tooltip hiện trên tòa nhà
1.7  UIManager.cs (Panel state 1+2) (NoSelection + BuildingSelected)
     → Test: panel đổi nội dung khi chọn/bỏ chọn

Deliverable: Demo click tòa nhà → fly camera → panel context
```

### Sprint 2 — Layer System (1 tuần)

```
Mục tiêu: Toggle layer → model 3D thay đổi màu

2.1  LayerManager.cs               (toggle, primary layer, màu)
2.2  Material system               (MaterialPropertyBlock per building)
2.3  Layer toggle buttons          (UI Toolkit, topbar)
2.4  Legend panel                  (cập nhật theo active layer)
2.5  WorldSpaceLabel update        (icon theo layer)

Deliverable: Demo bật layer Mật độ → tòa nhà đổi màu gradient
```

### Sprint 3 — Panel 6 Tabs (1–2 tuần)

```
Mục tiêu: Panel bên phải đầy đủ 6 tabs với data thực

3.1  USS: styles cho 6 tabs
3.2  UIManager: BuildTabSchedule()
3.3  UIManager: BuildTabOccupancy()
3.4  UIManager: BuildTabInfrastructure()
3.5  UIManager: BuildTabEquipment()
3.6  UIManager: BuildTabEvents()
3.7  UIManager: BuildTabMaintenance()
3.8  Panel state 3: KhuSelected
3.9  Panel state 4: AlertDetail

Deliverable: Demo đầy đủ 6 tabs với mock data
```

### Sprint 4 — Alert System & Operations Dashboard (1 tuần)

```
Mục tiêu: AlertManager + màn hình Operations hoàn chỉnh

4.1  AlertManager.cs               (detect, store, acknowledge)
4.2  AlertBell UI                  (badge count, dropdown list)
4.3  Alert notification popup      (khi có alert mới)
4.4  Operations Dashboard UXML     (redesign từ bản cũ)
     → Alert Feed
     → Resource Overview (Điện/Nước/Thiết bị)
     → Upcoming Events 7 ngày
     → Maintenance Queue
4.5  Panel state 4: AlertDetail

Deliverable: Demo alert system + Operations Dashboard
```

### Sprint 5 — Polish & Integration (1 tuần)

```
Mục tiêu: End-to-end polished, sẵn sàng demo

5.1  Animation: building selection pulse
5.2  Animation: panel slide transitions
5.3  Animation: alert notification appear/dismiss
5.4  Schedule screen: cập nhật theo concept mới
5.5  Camera: keyboard shortcuts (F, Escape)
5.6  Performance: LOD cho WorldSpaceLabel
5.7  Mock data: làm cho believable hơn
     (data có tính logic, không random hoàn toàn)
5.8  Real-time simulation: Co_FlickerData hoạt động tốt
5.9  Build test: Windows Standalone

Deliverable: Bản demo hoàn chỉnh
```

---

## 12. MOCK DATA SPEC

### Phân loại tòa nhà theo mock behavior

```
NHÓM 1 — Hoạt động cao (7:30–17:00)
  Toa_A_HanhChinh, Toa_F_GiangDuong, Toa_C_KhoiA/B/C/D/E
  → Schedule: Occupied 60% thời gian
  → Occupancy: 60–80% capacity
  → Equipment: thỉnh thoảng có error nhỏ
  → Infrastructure: điện cao buổi sáng

NHÓM 2 — Hoạt động trung bình
  Toa_B, Toa_D, Toa_E1/E2, Toa_G
  → Schedule: Occupied 40% thời gian
  → Occupancy: 30–60% capacity

NHÓM 3 — Lab / PTN (hoạt động cả buổi)
  PTN_I_KhuTNDien, PTN_K_KhuTNCoKhi, Xuong_K_*, PTN_N_*
  → Schedule: Occupied 70% thời gian (giờ lab dài)
  → Equipment: máy móc nhiều, error rate cao hơn
  → Infrastructure: điện cao và ổn định

NHÓM 4 — Đặc biệt
  Toa_S_HanhChinh: ít lớp học, nhiều cuộc họp, thiết bị văn phòng
  Toa_K_PFIEV: lịch giờ khác (8:00–10:00, 10:00–12:00)
  Misc_ThuVien: không có lịch học, người ra vào liên tục

NHÓM 5 — Luôn có bảo trì
  1 tòa ngẫu nhiên mỗi ngày có MaintenanceTicket active
  Thứ 7: nhiều tòa bảo trì hơn (ít lớp học)
```

### Alert generation rules

```
Rule 1: Infrastructure Alert
  IF electric.current_kw > electric.avg_kw_same_hour * 1.2
  THEN create Alert(Warning, "Tiêu thụ điện cao bất thường")

Rule 2: Equipment Alert
  IF device.error_devices > 0 AND device.has_critical
  THEN create Alert(Critical, "Thiết bị {name} gặp sự cố")

Rule 3: Occupancy Alert
  IF occupancy.density_ratio > 1.0
  THEN create Alert(Critical, "Quá tải: {building} vượt sức chứa")

Rule 4: Climate Alert
  IF climate.temperature_c > 32 AND climate.ac_error > 0
  THEN create Alert(Warning, "Nhiệt độ cao, {n} điều hòa lỗi")

Rule 5: Wastage Alert
  IF schedule.status == Empty AND infrastructure.electric.current_kw > 10
  THEN create Alert(Info, "Điều hòa bật khi không có người")
```

---

## 13. DESIGN TOKENS

### Màu sắc (USS)

```css
/* Backgrounds */
--bg-app:      rgb(11, 21, 32)     /* #0B1520 */
--bg-topbar:   rgb(8, 16, 26)      /* #08101A */
--bg-sidebar:  rgb(13, 24, 38)     /* #0D1826 */
--bg-card:     rgb(16, 30, 46)     /* #101E2E */
--bg-hover:    rgb(22, 38, 56)     /* #162638 */

/* Borders */
--border:      rgb(26, 48, 80)     /* #1A3050 */
--border-light:rgb(30, 58, 95)     /* #1E3A5F */

/* Text */
--text-primary:   rgb(232, 240, 248)  /* #E8F0F8 */
--text-secondary: rgb(122, 154, 184)  /* #7A9AB8 */
--text-muted:     rgb(61, 88, 112)    /* #3D5870 */

/* Accent */
--accent:      rgb(26, 111, 191)   /* #1A6FBF */
--accent-light:rgb(46, 143, 232)   /* #2E8FE8 */
--accent-dim:  rgb(16, 37, 64)     /* #102540 */

/* Status */
--status-occupied: rgb(239, 68, 68)    /* #EF4444 */
--status-empty:    rgb(34, 197, 94)    /* #22C55E */
--status-upcoming: rgb(245, 158, 11)   /* #F59E0B */

/* Alert severity */
--alert-info:     rgb(46, 143, 232)   /* xanh */
--alert-warning:  rgb(245, 158, 11)   /* vàng */
--alert-critical: rgb(239, 68, 68)    /* đỏ */

/* Chức năng */
--func-giang-duong: rgb(46, 143, 232)   /* xanh dương */
--func-hanh-chinh:  rgb(168, 85, 247)   /* tím */
--func-thi-nghiem:  rgb(245, 158, 11)   /* vàng cam */
--func-tien-ich:    rgb(107, 114, 128)  /* xám */
--func-gdtc:        rgb(34, 197, 94)    /* xanh lá */
--func-ktx:         rgb(236, 72, 153)   /* hồng */
```

### Typography

```css
/* Font sizes */
--text-xs:   9px    /* labels, timestamps nhỏ */
--text-sm:   11px   /* meta, captions */
--text-base: 13px   /* body text */
--text-md:   14px   /* labels nổi bật */
--text-lg:   18px   /* tên tòa nhà */
--text-xl:   22px   /* số liệu stat */
--text-2xl:  28px   /* số liệu lớn */

/* Font weights */
--weight-normal: 400
--weight-bold:   700 (chỉ cho số liệu quan trọng)
```

### Spacing & Radius

```css
/* Spacing */
--space-xs: 4px
--space-sm: 8px
--space-md: 12px
--space-lg: 16px
--space-xl: 24px

/* Border radius */
--radius-sm: 4px   /* chip, badge */
--radius-md: 8px   /* button, input */
--radius-lg: 10px  /* card */
--radius-xl: 16px  /* panel */
```

---

## APPENDIX A — DANH SÁCH 46 TÒA NHÀ (Mock Data IDs)

```
GIẢNG ĐƯỜNG & HÀNH CHÍNH:
  Toa_A_HanhChinh, Toa_B_GiangDuong
  Toa_C_KhoiA, Toa_C_KhoiB, Toa_C_KhoiC, Toa_C_KhoiD, Toa_C_KhoiE
  Toa_D_GiangDuong, Toa_E1_GiangDuong, Toa_E2_GiangDuong
  Toa_F_GiangDuong, Toa_F_HoiTruong
  Toa_G_ThiNghiem, Toa_K_PFIEV, Toa_S_HanhChinh, Toa_V_VienCoKhi

THÍ NGHIỆM & XƯỞNG:
  PTN_I_KhuTNDien, PTN_I_NCDienTu, PTN_I_ThucHanhDien, Xuong_I_XuongDien
  PTN_K_KhuTNCoKhi, Xuong_K_DongLucCoKhi, TT_K_AVL_AMAST
  PTN_K_ThuNghiemDCOto, PTN_K_ThuNghiemDuc, PTN_K_GiaCongApLuc
  Xuong_N_XuongNhiet_1, Xuong_N_XuongNhiet_2, PTN_N_NhietDienLanh
  Xuong_UTCN_UomTao, PTN_CTT_CongTrinhThuy
  Xuong_G_CauDuong, Xuong_G_XayDungDDCN, Xuong_G_DieuKhac, Xuong_G_MoiTruong

TIỆN ÍCH:
  Misc_ThuVien, Misc_TTHoTroDN, Misc_NhaThiDau
  Misc_CangtinKhuA, Misc_NhaKho_1, Misc_NhaKho_2

THỂ THAO:
  San_BongDa, San_BongChuyen, TT_GDTC

KÝ TÚC XÁ:
  KTX_Nha1, KTX_Nha2, KTX_Nha3, KTX_Nha4, KTX_Nha5
```

---

## APPENDIX B — FILE STRUCTURE

```
Assets/
├── Scripts/
│   ├── Data/
│   │   ├── DUTModels.cs              ← tất cả classes, enums
│   │   ├── BuildingDataStore.cs      ← ScriptableObject
│   │   └── MockDataProvider.cs       ← sinh mock data
│   ├── Core/
│   │   ├── BuildingObject.cs         ← click, material, state
│   │   ├── CameraController.cs       ← orbit, fly-to
│   │   ├── WorldSpaceLabel.cs        ← billboard tooltip
│   │   ├── LayerManager.cs           ← layer system
│   │   └── AlertManager.cs           ← alert detection + storage
│   └── UI/
│       ├── UIManager.cs              ← panel state machine
│       ├── DUTColors.cs              ← color helper (static)
│       └── UIScreenshot.cs           ← capture utility (dev only)
├── UI/
│   ├── DUT_Main.uss                  ← tất cả styles
│   ├── DUT_Main.uxml                 ← Campus Live screen
│   ├── DUT_Dashboard.uxml            ← Operations screen
│   ├── DUT_Schedule.uxml             ← Schedule screen
│   └── DUT_PanelSettings.asset
├── Data/
│   └── BuildingDataStore.asset       ← instance của ScriptableObject
├── Materials/
│   ├── Building_Default.mat
│   ├── Building_Hover.mat
│   └── Building_Selected.mat
└── Models/
    └── (SketchUp model đã import)
```

---

*Tài liệu này được viết để bất kỳ developer nào (hoặc AI agent) có thể đọc và implement mà không cần hỏi thêm context.*
*Version: 3.0 | Cập nhật: 2025-05-22*
