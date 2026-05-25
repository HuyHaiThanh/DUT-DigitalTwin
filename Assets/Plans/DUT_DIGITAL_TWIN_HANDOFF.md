# DUT Digital Twin — Tài liệu Bàn giao Chi tiết

> **Mục đích:** Tài liệu này dành cho agent/developer tiếp theo tiếp quản dự án.
> Bao gồm kiến trúc đầy đủ, trạng thái hiện tại, các bug đã biết, và roadmap tiếp theo.

---

## 1. Tổng quan Dự án

**Tên:** DUT Digital Twin — Bản đồ số 3D Trường Đại học Bách Khoa Đà Nẵng

**Mục tiêu:** Hệ thống giám sát campus real-time trong Unity 3D, hiển thị trạng thái 65 tòa nhà trên 22 khu theo 6 layers dữ liệu (Lịch học, Mật độ, Hạ tầng, Thiết bị, Sự kiện, Bảo trì).

**Stack:**
- Engine: Unity 2022.3 LTS (URP — Universal Render Pipeline)
- UI: Unity UI Toolkit (UXML + USS)
- Data scraping: Python (BeautifulSoup4)
- Real data source: `cb.dut.udn.vn` (ASP.NET WebForms, yêu cầu login)
- Project path (Windows): `D:\Documents\Unity\DUT_DigitalTwin\`

---

## 2. Cấu trúc File

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── SceneBootstrapper.cs       ← Khởi tạo toàn bộ scene khi Play
│   │   ├── BuildingObject.cs          ← Gắn trên mỗi tòa nhà, xử lý click/hover
│   │   ├── CameraController.cs        ← Orbit + Pan + Zoom + FlyTo
│   │   ├── LayerManager.cs            ← Singleton: quản lý 6 layers, tính màu icons
│   │   ├── StatusDotManager.cs        ← Sphere billboards per khu (22 icons)
│   │   ├── BuildingColorizer.cs       ← Đổi material per building theo status
│   │   └── BuildingHighlighter.cs     ← MaterialPropertyBlock tint (giữ texture gốc)
│   ├── Data/
│   │   ├── DUTModels.cs               ← Tất cả model classes + enums
│   │   ├── BuildingDataStore.cs       ← ScriptableObject: store + events
│   │   ├── MockDataProvider.cs        ← Sinh mock data cho 5/6 layers
│   │   └── RealDataProvider.cs        ← Load schedule_data.json → ScheduleData layer
│   └── UI/
│       ├── UIManager.cs               ← Panel sidebar: 6 tabs, BuildPanel_Building
│       ├── ScreenManager.cs           ← Navigation: Campus3D ↔ Dashboard ↔ Schedule
│       └── DUTColors.cs               ← Color constants + DUTTime.Now (UTC+7)
├── UI/
│   ├── DUT_Main.uxml                  ← Campus 3D screen (topbar + sidebar + viewport)
│   ├── DUT_Dashboard.uxml             ← Dashboard screen (stat cards, chart, heatmap)
│   ├── DUT_Schedule.uxml              ← Schedule screen (weekly grid)
│   └── DUT_Main.uss                   ← Toàn bộ styles (dark theme)
├── Resources/
│   └── BuildingDataStore.asset        ← ScriptableObject instance
└── StreamingAssets/
    └── schedule_data.json             ← Real schedule data từ cb.dut (4 entries test)

Python scripts (ngoài Unity):
├── parse_schedule_html.py             ← Parse HTML → JSON (offline mode)
├── scrape_cb_dut.py                   ← Live scraper (cần auth cookies)
├── verify_building_mapping.py         ← Cross-reference room codes ↔ Unity buildings
└── schedule_data.json                 ← Sample output
```

---

## 3. Scene Hierarchy

```
[Scene: SampleScene]
├── [Khu A] ... [Khu CT]       ← 22 khu, mỗi khu chứa N tòa nhà
│   └── "Tòa nhà X - Khu Y, Z tầng, chức năng"
│       ├── [mesh renderers]
│       └── _ColliderProxy      ← BoxCollider theo world bounds
│           └── BuildingObject  ← buildingId, khu, store ref
├── [Main Camera]               ← CameraController (panSpeed=0.05)
├── [CameraPivot]               ← Pivot cho orbit camera (pos: 370,0,320)
├── [Managers]                  ← SceneBootstrapper, StatusDotManager
│                                  + (runtime add) MockDataProvider,
│                                  + (runtime add) RealDataProvider,
│                                  + (runtime add) LayerManager,
│                                  + (runtime add) ScreenManager
├── [DUT_UI]                    ← UIDocument (DUT_Main.uxml) + UIManager
├── [DUT_Dashboard]             ← UIDocument (DUT_Dashboard.uxml) — inactive default
├── [DUT_Schedule]              ← UIDocument (DUT_Schedule.uxml) — inactive default
├── [UI_Root]                   ← UIDocument (DUT_Main.uxml) — inactive, legacy
└── [WorldSpaceLabels]          ← Container cho sphere icons (StatusDotManager)
```

**Quan trọng:** `[Managers]` chỉ có `SceneBootstrapper` và `StatusDotManager` được serialize. Các components còn lại (`MockDataProvider`, `RealDataProvider`, `LayerManager`, `ScreenManager`) được **AddComponent runtime trong `SceneBootstrapper.SetupDataProvider()`** nên KHÔNG save được vào scene file — đây là design có chủ ý.

---

## 4. Data Flow

```
Play Mode Start
    ↓
SceneBootstrapper.Awake()
    ├── DisableSketchUpCameras()
    ├── SetupCamera()           → AddComponent CameraController
    ├── SetupBuildings()        → Parse scene hierarchy → AllBuildings[65]
    │                           → Tạo BuildingInfo với ID: "B_{khu}_{index}"
    │                           → Tạo _ColliderProxy + BuildingObject per tòa
    └── SetupDataProvider()
        ├── AddComponent MockDataProvider   → GenerateAllData() → 65 BuildingLiveData
        ├── AddComponent RealDataProvider   → Co_Load() schedule_data.json
        │                                  → ParseJson() → ApplyToStore()
        │                                  → Override ScheduleData layer mỗi 5 phút
        ├── AddComponent LayerManager
        └── AddComponent ScreenManager
            └── Co_WireScreenManager()     → Wire 3 UIDocuments sau 1 frame

User Click tòa nhà
    ↓
CameraController.TrySelect()
    → Raycast → hit _ColliderProxy
    → BuildingObject.buildingId
    → store.SelectBuilding(id)
    → store.OnBuildingSelected.Invoke(id)
    ↓
UIManager.OnBuildingSelected(id)
    → BuildPanel_Building(id)
    → Đọc store.GetLiveData(id)
    → Update UXML elements (header, banner, tabs, timeline)
    ↓
StatusDotManager.OnSelected(id)
    → Hiện selected icon trên tòa
CameraController.FlyTo(info.world_position, dist)
    → Smooth camera animation
```

---

## 5. Chi tiết Từng Script

### 5.1 `SceneBootstrapper.cs`

**Purpose:** Single entry point khởi tạo toàn bộ scene.

**Key method `SetupBuildings()`:**
- Tìm root GOs có tên bắt đầu `"[Khu "`, parse child tòa nhà
- Parse tên theo format: `"Tòa nhà X - Khu Y, Z tầng, chức năng"`
- Tạo `building_id = "B_{khu}_{index}"` (ví dụ: `"B_E_9"`, `"B_GDTC_38"`)
- Tính bounds từ tất cả Renderers → tạo `_ColliderProxy` BoxCollider
- Tính `so_phong = so_tang * 8`, `suc_chua_toi_da` từ footprint area

**Key method `SetupDataProvider()`:**
- Gọi `GetComponent<X>() ?? AddComponent<X>()` cho 4 providers
- `Co_WireScreenManager()`: coroutine delay 1 frame để wire UIDocuments

### 5.2 `BuildingDataStore.cs` (ScriptableObject)

**Pattern:** Centralized event bus + data store.

```csharp
// Events quan trọng
public event Action<string> OnBuildingSelected;        // khi user click building
public event Action<string> OnBuildingLiveDataUpdated; // khi data update
public event Action         OnSelectionCleared;         // khi deselect

// Dictionary runtime (không serialize — reset khi Play)
Dictionary<string, BuildingLiveData> _liveData;
```

**Lưu ý:** Vì là ScriptableObject, `_liveData` reset về empty mỗi lần `OnEnable()` (Play Mode start). Tất cả subscribers cần unsubscribe trong `OnDestroy`/`OnDisable` để tránh memory leak.

### 5.3 `DUTModels.cs`

**Enums:**
- `BuildingStatus`: Unknown, Empty, Occupied, Upcoming
- `BuildingFunction`: GiangDuong, HanhChinh, ThiNghiem, TienIch, GDTC, KyTucXa, HoiTruong
- `LayerId`: Schedule, Occupancy, Infrastructure, Equipment, Events, Maintenance
- `OccupancyLevel`: Empty, Low, Medium, High, Overcrowded
- `AlertSeverity`: Info, Warning, Critical

**Key data classes:**
- `BuildingInfo`: metadata tĩnh (tên, khu, vị trí, bounds, chức năng)
- `BuildingLiveData`: runtime data (6 layers)
- `ClassInfo`: thông tin 1 lớp học — `room_id` encode `"thu=X;tiet=Y-Z;phong=ABC"`
- `AlertData`: alert item với severity, layer, title

### 5.4 `MockDataProvider.cs`

**Purpose:** Sinh realistic mock data cho TẤT CẢ 6 layers.

**Behavior:**
- `Start()` → `GenerateAllData()` → 65 `BuildingLiveData`
- `Co_Flicker()`: cứ 60 giây random thay đổi 20% buildings
- `Co_RefreshColors()`: sau 2 frames gọi `BuildingColorizer.Refresh()`

**Mock data generation:**
- Schedule: random 0-4 lớp/ngày, `room_id = "{khu}{roomnum}"` (KHÔNG encode `thu=X`)
- Occupancy: random density_ratio, hourly_today
- Infrastructure: random kW, temperature, AC count với 15% chance alert
- Equipment: random errors với 20% chance có lỗi
- Events: 30% chance có event hôm nay

**⚠️ Known issue:** MockDataProvider `room_id` không encode `thu=X;tiet=Y-Z` nên `ScreenManager.GetClassesForDay()` dùng fallback (trả all `today_classes` không filter). Nếu cần filter theo ngày thực cho mock data cần thêm `thu=X` encoding.

### 5.5 `RealDataProvider.cs`

**Purpose:** Load `schedule_data.json` từ `StreamingAssets` → override ScheduleData layer.

**Flow:**
```
Co_Load() → đọc file (File.ReadAllText hoặc UnityWebRequest tuỳ platform)
→ ParseJson(json)
    → GetCurrentWeek() → tính tuần hiện tại từ ngày 1/8 của năm học
    → Filter entries theo week ranges (ví dụ: "22-27;31-44")
    → ResolveBuildingId(phong) → map room code → building_id
    → SlotToClassInfo() → tạo ClassInfo với room_id="thu=X;tiet=Y-Z;phong=ABC"
→ ApplyToStore()
    → Với mỗi building trong store:
        → Lọc today_classes theo dutToday (T2=2..T7=7,CN=8)
        → Set current_class, next_class, status
        → store.UpdateLiveData(live)
```

**Room → Building mapping:**
```
Prefix  → Khu Unity
GDTC    → GDTC (B_GDTC_38..41)
E1.xxx  → E (B_E_9, match "E1" trong ten_ngan)
E2.xxx  → E (B_E_10, match "E2" trong ten_ngan)
F208    → F (B_F_11..)
M202    → G (Khu M = Khu G trong Unity — xưởng thực hành)
K105    → K (B_K_24..30)
XP      → K
P2,P3   → F
```

**`GetCurrentWeek()` logic:**
```csharp
// Năm học bắt đầu 1/8, tuần 1 = tuần đầu tháng 8
int year = now.Month >= 8 ? now.Year : now.Year - 1;
var semStart = new DateTime(year, 8, 1);
// Lùi về thứ 2 gần nhất
semStart = semStart.AddDays(-(dow - 1));
int week = (int)((now - semStart).TotalDays / 7) + 1;
```

**⚠️ Known issue:** `schedule_data.json` hiện tại chỉ có 4 entries test (T3, T4, T5 hôm nay). Cần download HTML thực từ `cb.dut.udn.vn/PageLopHPKH.aspx` và parse lại.

### 5.6 `LayerManager.cs`

**Purpose:** Singleton quản lý active layer, tính màu cho StatusDotManager.

**Color rules per layer:**
| Layer | Empty | Medium | High/Occupied |
|-------|-------|--------|---------------|
| Schedule | 🟢 Green | - | 🔴 Red / 🟡 Yellow |
| Occupancy | 🟢 <20% | 🔵 20-50% | 🟡 50-80% / 🔴 >80% |
| Infrastructure | ⬜ No alert | 🟡 Alert | 🔴 >1.5x avg kW |
| Equipment | ⬜ No error | 🟡 Minor | 🔴 Critical |
| Events | ⬜ None | 🔵 Event | 🟣 >50 attendees |
| Maintenance | ⬜ Normal | 🔵 Scheduled | 🟡 InProgress / 🔴 Critical |

**Event:** `OnPrimaryLayerChanged` → `StatusDotManager.RefreshAll()` re-tính màu icons.

### 5.7 `StatusDotManager.cs`

**Purpose:** Hiển thị 22 sphere icons (1 per khu) billboard hướng về camera.

- Vị trí: `center của khu + 8 units lên trên maxY`
- Scale: `10f` units
- Màu: dominant color của buildings trong khu theo active layer
- `_selectedIcon`: sphere xanh dương riêng đặt trên tòa được chọn

**Events subscribed:**
- `store.OnBuildingLiveDataUpdated` → `TryCreate()` rồi `RefreshAll()`
- `store.OnBuildingSelected` → `OnSelected()` → di chuyển `_selectedIcon`
- `LayerManager.OnPrimaryLayerChanged` → `RefreshAll()`

### 5.8 `CameraController.cs`

**Controls:**
- **Orbit:** RMB drag
- **Pan:** MMB drag hoặc LMB drag (nếu kéo > 5px)
- **Zoom:** Scroll wheel
- **Click select:** LMB click (< 5px movement) → Raycast → `TrySelect()`
- **Keyboard:** WASD/Arrow keys pan, F = overview, Escape = deselect + overview
- **FlyTo:** smooth coroutine lerp 0.8s
- **Sidebar detection:** `OverSidebar()` — block click nếu x > Screen.width - 390px

**panSpeed:** Field public, serialize Inspector. Giá trị hiện tại: `0.05f` (giảm từ 0.1 → 0.05). **Cần save scene sau Stop Play** để giữ giá trị này.

### 5.9 `UIManager.cs`

**State machine:**
```
PanelState.NoSelection    → BuildPanel_Overview() → status banner tổng quan
PanelState.BuildingSelected → BuildPanel_Building(id) → 6 tabs
```

**Lifecycle quan trọng:**
```csharp
OnEnable()  → Instance=this + SubscribeEvents() + Co_RebindUI()
OnDisable() → Instance=null + UnsubscribeEvents()
```

**Tại sao cần `OnEnable` re-subscribe:** `DUT_UI` GO bị `SetActive(false/true)` bởi `ScreenManager` khi chuyển màn hình. Mỗi lần `SetActive(true)` → `OnEnable()` chạy → re-subscribe events.

**Co_RebindUI():** Delay 1 frame → `_root = _doc.rootVisualElement` → `BindNav()` → rebuild panel.

**BuildPanel_Building(id):**
1. Update UXML elements (`building-badge`, `building-name`, `building-meta`)
2. Status banner: count ongoing classes (GetClassesByTime với nowMin)
3. Rebuild tab bar với 6 Button elements
4. Inject tab content (`Tab_Schedule`, `Tab_Occupancy`, etc.)
5. Append `BuildTimeline(live)`
6. Append back button

**`Tab_Schedule` logic:** Phân nhóm `today_classes` thành:
- Ongoing: `nowMin >= start && nowMin < end`
- Upcoming: `start > nowMin && start - nowMin <= 90` (trong 90 phút)
- Rest: tất cả còn lại

**`room_id` encoding** (từ RealDataProvider):
- Format: `"thu=3;tiet=1-4;phong=GDTC"`
- `ExtractPhong()`: regex `phong=(\S+)`, fallback nếu không có `=` thì dùng cả string

### 5.10 `ScreenManager.cs`

**Navigation:**
```csharp
ShowScreen(Screen.Campus3D)   → DUT_UI.SetActive(true)
ShowScreen(Screen.Dashboard)  → DUT_Dashboard.SetActive(true)
ShowScreen(Screen.Schedule)   → DUT_Schedule.SetActive(true)
```

**Sau khi activate → `Co_ActivateScreen()`:**
1. `yield return null` (1 frame để UIDocument ready)
2. `BindNav(root)` — register click handlers cho nav + layer buttons
3. Populate content (Dashboard hoặc Schedule)

**Co_PopulateDashboard():**
- Stat cards: đếm buildings theo BuildingStatus
- Bar chart: class count per hour (7h-17h)
- Donut: count per BuildingFunction
- Top list: buildings có nhiều classes nhất hôm nay
- Heatmap: khu × hour (8 slots)

**Co_PopulateScheduleGrid():**
- Select top 7 buildings có nhiều classes ngày được chọn
- Fallback nếu 0: dùng all `today_classes` (mock data)
- Headers: `sched-col-name` (ten_ngan) + `sched-col-khu`
- Time rows: 5 slots (07:00, 10:20, 12:30, 15:00, 17:30)
- Cell matching: so sánh tiet range vs slot range

### 5.11 `DUTColors.cs`

**`DUTTime.Now`:**
```csharp
// Giờ UTC+7 (Việt Nam) — LUÔN dùng thay DateTime.Now
public static System.DateTime Now =>
    System.TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, _vn);
```
**Quan trọng:** Server/editor có thể chạy UTC+0. Tất cả code dùng `DUT.UI.DUTTime.Now` thay vì `DateTime.Now`.

---

## 6. UI Structure (UXML)

### DUT_Main.uxml (Campus 3D)
```
root
└── topbar
    ├── logo ("Digital Twin — Bách Khoa ĐN")
    ├── nav [nav-3d, nav-dashboard, nav-schedule]
    ├── layer-toggles [layer-schedule..layer-maint] (6 buttons)
    └── topbar-right [live-dot, datetime-label]
└── main-content
    ├── viewport (3D scene area)
    └── sidebar
        ├── search-bar [search-input]
        ├── sidebar-tabs [tab-info, tab-schedule] ← rebuilt by UIManager
        └── sidebar-body
            ├── building-header [building-badge, building-name, building-meta]
            ├── status-banner [status-dot, status-label, status-desc]
            └── [tab content — injected by UIManager]
```

### DUT_Dashboard.uxml
```
topbar (identical, with nav names + layer-toggles)
body
├── dash-stat-cards (4 cards: "dash-stat-card" class)
│   └── dash-stat__num + dash-stat__sub
├── chart-row
│   ├── bar-chart (bar-col > bar-fill)
│   └── donut-chart (donut-pct, donut-legend-val ×4)
├── top-list (top-list-row ×5: top-list__name, top-list__bar-fill, top-list__pct)
└── heatmap (heatmap-row ×7: heatmap-cell ×8, class hm-0..hm-4)
```

### DUT_Schedule.uxml
```
topbar (identical)
left-panel (filter: sched-day-btn ×5, TRẠNG THÁI checkboxes, CHỨC NĂNG checkboxes)
schedule-grid
├── header-row (sched-col-header > sched-col-name + sched-col-khu)
└── time-rows ×5 (sched-time-row > sched-time-label + sched-cell ×7)
    └── sched-cell__course + sched-cell__info
    └── classes: sched-cell--occupied / --upcoming / --empty
```

---

## 7. Data Sources

### 7.1 Real Data (Schedule Layer)

**URL:** `https://cb.dut.udn.vn/PageLopHPKH.aspx`

**Auth:** ASP.NET WebForms — yêu cầu login session cookies (trả 403 nếu không có).

**Workflow scraping:**
```bash
# 1. Đăng nhập vào cb.dut.udn.vn bằng browser
# 2. Save page as HTML: PageLopHPKH.aspx → cb_dut_schedule.html
# 3. Parse:
python parse_schedule_html.py cb_dut_schedule.html --output schedule_data.json
# 4. Copy vào Unity:
cp schedule_data.json "D:/Documents/Unity/DUT_DigitalTwin/Assets/StreamingAssets/"
```

**JSON format output:**
```json
{
  "scraped_at": "2026-05-25T07:00:00",
  "source": "cb.dut.udn.vn/PageLopHPKH.aspx",
  "count": 450,
  "data": [
    {
      "ma_lop": "013002125202504",
      "ten_lop": "B25-GDTC2-TD-04",
      "giang_vien": "Khoa G.dục thể chất",
      "so_tin_chi": "0",
      "slsv": 44,
      "tuan": { "ranges": [[26,27],[31,44]], "raw": "26-27;31-44" },
      "thoi_khoa_bieu": [{ "thu": 3, "tiet": "1-4", "phong": "GDTC" }]
    }
  ]
}
```

**Room → Building mapping table:**
| Room prefix | Unity khu | Ví dụ |
|-------------|-----------|-------|
| E1. | Khu E (B_E_9 — Tòa nhà E1) | E1.101 |
| E2. | Khu E (B_E_10 — Tòa nhà E2) | E2.406 |
| F | Khu F | F208 |
| H | Khu H | H201 |
| B | Khu B | B303 |
| K | Khu K | K103, K105 |
| M | Khu G (đặt tên khác trong Unity) | M201, M202 |
| GDTC | Khu GDTC | GDTC |
| XP | Khu K | XP |
| P | Khu F | P2, P3, P6, P7 |

### 7.2 Mock Data (5 Layers còn lại)

`MockDataProvider.cs` sinh ngẫu nhiên với seed cố định (42) để reproducible. Update 60 giây/lần với 20% buildings bị flicker trạng thái.

---

## 8. Trạng thái Hiện tại (Tháng 5/2026)

### ✅ Đã Hoàn thành

| Sprint | Feature | Status |
|--------|---------|--------|
| S1 | 3D campus model — 65 buildings, 22 khu | ✅ Done |
| S1 | CameraController (orbit/pan/zoom/fly) | ✅ Done |
| S1 | BuildingDataStore + DUTModels | ✅ Done |
| S1 | MockDataProvider (6 layers) | ✅ Done |
| S1 | BuildingObject + ColliderProxy click | ✅ Done |
| S1 | StatusDotManager (22 sphere icons) | ✅ Done |
| S2 | LayerManager (6 layers toggle) | ✅ Done |
| S2 | Layer toggle buttons trong topbar | ✅ Done |
| S2 | UIManager sidebar (6 tabs) | ✅ Done |
| S2 | RealDataProvider + schedule_data.json | ✅ Done |
| S2 | Python scraper (parse_schedule_html.py) | ✅ Done |
| S2 | Room→Building mapping (M=G, E1/E2 split) | ✅ Done |
| S3 | ScreenManager navigation (3 screens) | ✅ Done |
| S3 | Dashboard screen (stats, chart, heatmap) | ✅ Done |
| S3 | Schedule screen (weekly grid) | ✅ Done |
| S3 | DUTTime.Now (UTC+7 fix) | ✅ Done |
| S3 | UIManager panel update on click | ✅ Done (after rewrite) |

### ⚠️ Partially Done / Known Issues

1. **UIManager panel update** — Đã fix bằng `OnEnable` re-subscribe + `Co_RebindUI`. Cần test thực tế sau Stop/Play lại để xác nhận hoạt động ổn định.

2. **schedule_data.json chỉ có 4 entries test** — Chỉ có T3, T4, T5 data. Cần download HTML thực từ cb.dut để có full semester data.

3. **panSpeed reset sau Play** — `panSpeed = 0.05f` set runtime trong Play Mode, chưa serialize vào scene. **Sau mỗi Stop Play phải Ctrl+S** để lưu. Nên set default trong script thành `0.05f` thay vì `0.008f`.

4. **Schedule screen header** — Hiện "Khu B", "Khu C" (UXML default) thay vì tên tòa thực khi `buildings.Count == 0`. Fallback đã có nhưng cần test với mock data running.

5. **MockDataProvider room_id** — Không encode `thu=X`, `ScreenManager.GetClassesForDay()` fallback trả all `today_classes`. Nếu cần schedule filter đúng ngày cho mock cần thêm encoding.

6. **Dashboard populate** — `Co_PopulateDashboard` dùng `Query<VisualElement>("", "class")` nhưng execute_code context không verify được từ outside — cần test thực tế trong Game view.

### ❌ Chưa Làm (Next Sprints)

---

## 9. Roadmap Tiếp theo

### Sprint 4: Alert System 🔔 (Ưu tiên cao nhất)

**Tạo `AlertManager.cs`:**
```csharp
// Assets/Scripts/Core/AlertManager.cs
// Tự động detect alerts từ BuildingLiveData mỗi 30 giây

Rules:
- Infrastructure: electric.current_kw > avg_kw * 1.5 → WARNING
- Infrastructure: temperature_c > 35 → WARNING
- Infrastructure: ac_error > 0 → INFO
- Equipment: error_devices > 0 && severity == Critical → CRITICAL
- Occupancy: density_ratio > 0.95 → WARNING
- Waste alert: status == Empty && electric.current_kw > 5 → INFO
```

**UI:**
- Alert bell icon trong topbar-right (badge count)
- Alert dropdown list khi click bell
- Alert toast notification khi alert mới xuất hiện
- Alert color coding: 🔴 Critical / 🟡 Warning / 🔵 Info

**UXML changes cần:**
```xml
<!-- Thêm vào DUT_Main.uxml topbar-right -->
<ui:Button name="alert-bell" class="alert-btn">
  <ui:Label name="alert-badge" class="alert-badge" text="3" />
</ui:Button>
<ui:VisualElement name="alert-dropdown" class="alert-dropdown" />
```

### Sprint 5: Building Search & Filter

**Search bar** (đã có `search-input` trong sidebar UXML):
- Bind `RegisterValueChangedCallback` → filter buildings theo tên
- Highlight matching buildings (StatusDot màu xanh)
- Click suggestion → `store.SelectBuilding(id)`

**Filter chips** (đã có "Khu A", "Khu B"... trong UXML):
- Bind chips → filter theo khu
- Multi-select: giữ Ctrl để chọn nhiều khu

### Sprint 6: Dashboard Analytics Enhancement

**Bar chart animation:** Animate bar heights khi data load
**Heatmap interaction:** Hover cell → tooltip với building list
**Time range picker:** Hôm nay / Tuần này / Tháng này

### Sprint 7: Real Data cho các Layers khác

**PubThietBi.aspx** (Equipment Layer):
- URL: `https://cb.dut.udn.vn/PubThietBi.aspx`
- ASP.NET POST request với ViewState
- Columns: Phòng học, Đèn, Bàn/ghế, Máy chiếu, Quạt, Điều hòa, Cửa

**Occupancy từ Camera/WiFi data** (tương lai xa):
- Cần API từ phía trường
- Hoặc ước tính từ số lớp × số SV

### Sprint 8: Polish & Performance

- Camera smooth deceleration
- Building selection animation (scale pulse)
- Panel slide-in animation
- LOD cho buildings xa camera
- Optimize `StatusDotManager.LateUpdate()` (hiện chạy mỗi frame)

---

## 10. Hướng dẫn Cho Agent Tiếp Theo

### Setup môi trường

1. **Unity 2022.3 LTS** với URP package
2. Clone/copy project vào local
3. Mở `Assets/Scenes/SampleScene.unity`
4. Press **Play** — scene tự bootstrap

### Workflow phát triển

```
1. Dùng Unity MCP (unityMCP tools) để edit code trong Play Mode
2. execute_code để test logic trực tiếp
3. read_console để check compile errors
4. manage_scene để query hierarchy
5. Sau khi xong: Stop Play → Ctrl+S để save scene
```

### Quy ước đặt tên

- `building_id`: `"B_{ten_khu}_{index}"` — ví dụ `"B_E_9"`, `"B_GDTC_38"`
- `ten_khu`: chữ hoa, không dấu — `"E"`, `"GDTC"`, `"KTX"`
- Event handlers: `On{EventName}(args)` — ví dụ `OnBuildingSelected(string id)`
- Coroutines: `Co_{ActionName}()` — ví dụ `Co_PopulateDashboard()`
- Static helpers: `TryParseTime`, `ExtractPhong`, `DUTTime.Now`

### Pitfalls đã biết

1. **`DateTime.Now` → dùng `DUT.UI.DUTTime.Now`** — editor có thể chạy UTC+0
2. **`store.GetLiveData()` trả null** khi data chưa được MockDataProvider generate — luôn check null
3. **`style.padding` không tồn tại** trong Unity UI Toolkit — dùng `paddingTop/Left/Right/Bottom`
4. **`style.margin` không tồn tại** — dùng `marginTop/Left/Right/Bottom`
5. **Execute_code không support top-level `using`** — dùng fully-qualified names
6. **ScriptableObject `_liveData` reset khi Play** — đừng depend vào data persist giữa sessions
7. **`ScreenManager` và `LayerManager` không được serialize** — tạo lại mỗi Play via `SceneBootstrapper`
8. **UIManager `OnEnable` phải re-subscribe** vì `DUT_UI.SetActive(false/true)` bởi ScreenManager

### Test checklist sau mỗi thay đổi

- [ ] Console không có compile errors
- [ ] Play Mode: 65 buildings xuất hiện (Debug.Log "[Boot] 65 buildings setup")
- [ ] Click tòa nhà → panel sidebar update
- [ ] Click tòa khác → panel update lại
- [ ] Layer toggles → sphere icons đổi màu
- [ ] Nav Dashboard → screen switch, stat cards có số
- [ ] Nav Schedule → screen switch, grid hiện tòa nhà
- [ ] Nav Campus 3D → quay lại 3D view
- [ ] F key → camera về overview
- [ ] Escape → deselect building

### Update real data

```bash
# 1. Đăng nhập cb.dut.udn.vn
# 2. Mở PageLopHPKH.aspx, chọn học kỳ hiện tại
# 3. Save page as HTML
python parse_schedule_html.py page.html --output schedule_data.json

# Verify output
cat schedule_data.json | python -c "import json,sys; d=json.load(sys.stdin); print(f'{d[\"count\"]} entries, buildings: {set(r[\"phong\"] for e in d[\"data\"] for r in e[\"thoi_khoa_bieu\"])}')"

# Copy vào Unity
cp schedule_data.json "PATH/Assets/StreamingAssets/"
```

---

## 11. Danh sách Files Python

| File | Purpose |
|------|---------|
| `parse_schedule_html.py` | Offline parser: HTML → JSON |
| `scrape_cb_dut.py` | Live scraper (cần auth cookies) |
| `verify_building_mapping.py` | Cross-ref room codes ↔ Unity buildings |
| `test_scraper.py` | Unit tests cho parsing logic |
| `schedule_data.json` | Sample output (4 entries) |
| `room_to_building_mapping.json` | Mapping table đã verify |

---

## 12. Thống kê Dự án

| Metric | Value |
|--------|-------|
| Buildings | 65 |
| Khu groups | 22 |
| Data layers | 6 |
| C# scripts | 12 |
| UXML files | 3 (+ 1 legacy) |
| USS files | 1 (22KB) |
| Python scripts | 5 |
| Lines of C# | ~2,200 |

---

*Last updated: 2026-05-25 — Tháng 5/2026*
*Compiled by: Claude Sonnet 4.6 (DUT Digital Twin Agent)*
