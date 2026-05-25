# DUT DIGITAL TWIN — MASTER PLAN v5
# Đại học Bách Khoa Đà Nẵng — Operations Center
# Unity 2022.3 LTS · URP · UI Toolkit
# Cập nhật: 2026-05-24 — Trước Sprint 2

> **Tài liệu handoff cho agent tiếp theo.**
> Sprint 1 HOÀN THÀNH. Đọc mục 3 (Known Bugs) trước khi động vào bất cứ thứ gì.

---

## 1. TỔNG QUAN

**Mục tiêu:** Digital Twin campus DUT Đà Nẵng — giao diện Operations Center cho Ban Giám hiệu và Phòng Quản trị.

**Triết lý:** Model 3D là giao diện chính. 6 data layers chồng nhau. Click tòa nhà → camera fly → panel chi tiết. Alert-driven.

**Tech stack:** Unity 2022.3 LTS, URP, UI Toolkit (UXML/USS), C# namespaces: `DUT.Data`, `DUT.Core`, `DUT.UI`.

---

## 2. TRẠNG THÁI SPRINT 1

### ✅ Hoàn thành
- 65 buildings tự động setup từ 22 `[Khu X]` groups
- `BuildingObject.cs` — identity + ColliderProxy (xem bug #1)
- `CameraController.cs` — orbit/pan/zoom/fly-to/WASD/click
- `StatusDotManager.cs` — **1 icon per KHU** (22 khu), màu dominant status
- `BuildingDataStore.cs` — ScriptableObject + event system
- `MockDataProvider.cs` — 6 data layers, update 60s
- `UIManager.cs` — 3 panel states, 6 tabs, rate-limited rebuild
- `DUT_Main.uxml` + `DUT_Main.uss` — Campus Live screen
- 0 compile errors, scene saved

### ❌ Chưa làm (Sprint 2+)
- `LayerManager.cs` — toggle 6 layers → icon màu thay đổi
- Operations Dashboard logic
- Schedule screen logic
- Real data từ cb.dut.udn.vn

---

## 3. KNOWN BUGS & WORKAROUNDS (ĐỌC TRƯỚC)

### Bug #1: Collider Z=0 — ColliderProxy (ĐÃ FIX)
Model SketchUp rotation=(270,180,0) scale=0.03. BoxCollider trực tiếp trên building cho size.z=0.

**Fix hiện tại:** `_ColliderProxy` child GO ở world space:
```
proxyT.SetParent(null)           // detach
proxyT.position = bounds.center  // world pos
proxyT.rotation = Quaternion.identity
proxyT.localScale = bounds.size  // world size
BoxCollider size = Vector3.one   // size=1 * scale = bounds đúng
proxyT.SetParent(parent, true)   // re-attach
// + copy BuildingObject lên proxy để raycast GetComponentInParent tìm được
```

### Bug #2: `_store` null trong CameraController (ĐÃ FIX)
`FindFirstObjectByType<BuildingDataStore>()` không tìm được ScriptableObject.
**Fix:** `Resources.Load<BuildingDataStore>("BuildingDataStore")` trong `Start()`.

### Bug #3: panSpeed bị override bởi serialized value (ĐÃ FIX)
Script default không có tác dụng nếu Inspector đã lưu giá trị cũ.
**Giá trị hiện tại:** `panSpeed=0.1` (set trực tiếp trên component, saved trong scene).
**Nếu cần thay đổi:** Dùng `ctrl.panSpeed = X; EditorUtility.SetDirty(cam.gameObject); SaveScene()` trong Editor mode — KHÔNG chỉ sửa script.

### Bug #4: StatusDotManager icons không tạo (ĐÃ FIX)
Coroutine chờ sai condition. Fix: trigger từ `OnBuildingLiveDataUpdated` event lần đầu.

### Bug #5: SketchUp camera che Main Camera (ĐÃ FIX)
`skp_camera_Last_Saved_SketchUp_View` depth=0 render đè lên Main Camera.
Fix: `DisableSketchUpCameras()` trong `SceneBootstrapper.Awake()`.

### Bug #6: render_ui tool bị stuck (CHƯA FIX)
`WaitForEndOfFrame` không resolve khi UIManager rebuild panel liên tục.
**Workaround:** Dùng `manage_camera screenshot capture_source=game_view` để verify model 3D. Verify UI trong Unity Editor Game View thực tế.

### Note về đổi màu tòa nhà
Đã thử BuildingColorizer (swap materials) nhưng **không khả thi** vì:
- 1314+ renderers per tòa nhà
- URP/Lit: bị shadow → không đồng đều
- URP/Unlit: mất texture hoàn toàn
- `sharedMaterials` bị reset khi Stop play

→ **Quyết định cuối: dùng icon per khu** (StatusDotManager). Nếu muốn đổi màu tòa nhà trong tương lai cần URP ScriptableRendererFeature custom.

---

## 4. FILE STRUCTURE

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── BuildingObject.cs        ← identity + EnsureCollider (fallback)
│   │   ├── BuildingColorizer.cs     ← DEPRECATED, không dùng
│   │   ├── BuildingHighlighter.cs   ← DEPRECATED, không dùng
│   │   ├── CameraController.cs      ← orbit/pan/zoom/fly/click
│   │   ├── SceneBootstrapper.cs     ← auto-setup khi Awake()
│   │   └── StatusDotManager.cs      ← icon per khu (22 icons + 1 selected)
│   ├── Data/
│   │   ├── DUTModels.cs             ← tất cả data classes + enums
│   │   ├── BuildingDataStore.cs     ← ScriptableObject + events
│   │   └── MockDataProvider.cs      ← sinh mock data 6 layers, 60s interval
│   └── UI/
│       ├── UIManager.cs             ← panel state machine, 6 tabs
│       └── DUTColors.cs             ← color constants
├── UI/
│   ├── DUT_Main.uxml                ← Campus Live layout
│   ├── DUT_Main.uss                 ← tất cả styles
│   ├── DUT_Dashboard.uxml           ← chưa wire
│   ├── DUT_Schedule.uxml            ← chưa wire
│   └── DUT_PanelSettings.asset      ← clearColor=false (transparent bg)
├── Resources/
│   ├── BuildingDataStore.asset      ← ScriptableObject instance
│   └── BuildingStatus/              ← legacy materials (không dùng)
└── Models/
    └── DUT_MODELS.fbx               ← SketchUp model, scale=0.03, rot=(270,180,0)
```

---

## 5. SCENE HIERARCHY

```
SampleScene
├── Directional Light          rotation=(45,45,0), intensity=1.2
├── [Khu A] ... [Khu CT]       22 khu groups (A,B,C,D,E,F,G,H,I,K,S,V,TV,XE,GDTC,KTX,N,CTT,UTCN,ISTAR,KHO,CT)
│   └── Tòa nhà X...           child: BuildingObject, _ColliderProxy
│       └── _ColliderProxy     world-space BoxCollider + BuildingObject copy
├── DUT_MODELS                 852 children (cây/đường/xe — KHÔNG có BuildingObject)
├── EventSystem
├── Main Camera                CameraController
│                              panSpeed=0.1, orbitSpeed=180
│                              overview: dist=420, pitch=28°, yaw=45°
│                              pivot → CameraPivot(370,0,320)
├── CameraPivot                position=(370,0,320)
├── DUT_UI                     UIDocument→DUT_Main.uxml, UIManager
├── DUT_Dashboard (disabled)
├── DUT_Schedule (disabled)
├── Managers                   SceneBootstrapper, StatusDotManager, MockDataProvider
└── WorldSpaceLabels           22 khu icons + 1 selected icon (runtime-created)
```

---

## 6. DATA MODEL

### Enums (DUTModels.cs)
```csharp
enum BuildingStatus    { Unknown, Empty, Occupied, Upcoming }
enum BuildingFunction  { GiangDuong, HanhChinh, ThiNghiem, TienIch, GDTC, KyTucXa, HoiTruong }
enum OccupancyLevel    { Empty, Low, Medium, High, Overcrowded }
enum MaintenanceStatus { Normal, Scheduled, InProgress, Disrupted, Critical }
enum AlertSeverity     { Info, Warning, Critical }
enum AlertLayer        { Schedule, Occupancy, Infrastructure, Equipment, Event, Maintenance }
enum LayerId           { Schedule, Occupancy, Infrastructure, Equipment, Events, Maintenance }
```

### BuildingInfo (static)
```csharp
string building_id;     // "B_A_0", "B_F_12" — format: B_{khu}_{index}
string ten_khu;         // "A", "F", "KTX"
string ten_ngan;        // "Tòa nhà A"
string ten_day_du;      // tên đầy đủ từ SketchUp child name
int    so_tang;
string chuc_nang_str;
BuildingFunction chuc_nang;
Vector3 world_position; // bounds.center
Vector3 bounds_size;    // world bounds size
int    so_phong;        // so_tang * 8
int    suc_chua_toi_da; // Clamp(x*z*0.5, 50, 1000)
```

### BuildingLiveData (dynamic)
```csharp
string building_id;
string timestamp;
ScheduleData       schedule;        // Layer 1: Lịch học
OccupancyData      occupancy;       // Layer 2: Mật độ
InfrastructureData infrastructure;  // Layer 3: Hạ tầng
EquipmentData      equipment;       // Layer 4: Thiết bị
List<EventData>    events;          // Layer 5: Sự kiện
MaintenanceData    maintenance;     // Layer 6: Bảo trì
```

---

## 7. SCRIPTS API

### BuildingDataStore (ScriptableObject)
**Load:** `Resources.Load<BuildingDataStore>("BuildingDataStore")`
**KHÔNG dùng** `FindFirstObjectByType<BuildingDataStore>()` — không tìm được ScriptableObject.

```csharp
// State
List<BuildingInfo> AllBuildings;
string SelectedBuildingId { get; }
string SelectedKhu { get; }

// Events
event Action<string> OnBuildingSelected;       // fires khi SelectBuilding()
event Action<string> OnBuildingLiveDataUpdated; // fires khi UpdateLiveData()
event Action         OnSelectionCleared;        // fires khi ClearSelection()

// API
BuildingInfo     GetInfo(string id);
BuildingLiveData GetLiveData(string id);
void             UpdateLiveData(BuildingLiveData data);
void             SelectBuilding(string id);
void             ClearSelection();
BuildingStatus   GetStatus(string id);
int              CountByStatus(BuildingStatus s);
List<BuildingInfo> GetBuildingsInKhu(string khu);
List<AlertData>  GetAllAlerts(); // auto-detect từ infrastructure + equipment
```

### CameraController
**Attach:** Main Camera. **Serialized values (quan trọng):**
```
panSpeed    = 0.1    (dist * panSpeed per pixel — đã set trực tiếp trên component)
orbitSpeed  = 180
minDist     = 25, maxDist = 1200
_ovDist=420, _ovPitch=28°, _ovYaw=45°
pivot → CameraPivot (370, 0, 320)
```

**Input:**
| Action | Trigger |
|--------|---------|
| Orbit | RMB giữ + kéo |
| Pan | LMB giữ + kéo >5px, hoặc MMB |
| Zoom | Scroll (tỷ lệ theo dist * 0.4) |
| Click select | LMB release, drag <5px, ngoài sidebar |
| WASD/Arrows | Pan bàn phím |
| F | ReturnToOverview |
| Escape | Deselect + ReturnToOverview |

**OverSidebar() zones:**
- x > Screen.width - 390 → sidebar
- y > Screen.height - 55 → topbar
- y < 90 && x < 220 → bottom-left legend

**Public API:**
```csharp
void FlyTo(Vector3 pos, float dist = -1);   // fly-to building
void ReturnToOverview();                     // về overview default
```

**TrySelect():** `Resources.Load<BuildingDataStore>()` → Raycast → `GetComponentInParent<BuildingObject>()` → `store.SelectBuilding()`.

### SceneBootstrapper
**Attach:** Managers GO. Runs in `Awake()` — trước mọi `Start()`.

```
Awake():
1. DisableSketchUpCameras() — tắt skp_camera, đặt Main Camera depth=0
2. store = Resources.Load<BuildingDataStore>("BuildingDataStore")
3. SetupCamera() — add CameraController nếu chưa có
4. SetupBuildings():
   - Iterate [Khu X] children
   - Parse tên: "Tòa nhà A - Khu A, 4 tầng, khu hành chính"
   - ID = "B_{khu}_{store.AllBuildings.Count}"
   - Add BuildingObject
   - SetupColliderProxy() — _ColliderProxy child world-space
5. SetupDataProvider() — add MockDataProvider
```

**SetupColliderProxy():**
```csharp
// Xóa collider trên parent
// Tạo/tìm "_ColliderProxy" child:
proxyT.SetParent(null, false);
proxyT.position   = wb.center;
proxyT.rotation   = Quaternion.identity;
proxyT.localScale = wb.size;
proxyT.SetParent(parent.transform, true);
var bc = proxyT.AddComponent<BoxCollider>();
bc.size = Vector3.one; bc.center = Vector3.zero;
// Copy BuildingObject lên proxy
```

### StatusDotManager
**Attach:** Managers GO. **WorldSpaceLabels** container ở scene root.

```
Logic:
- 1 Sphere icon per khu (22 khu) — radius 10f, URP/Unlit
- Màu = dominant status trong khu:
    all empty    → COL_EMPTY   (0.13, 0.77, 0.37) xanh lá
    all occupied → COL_OCCUPIED (0.94, 0.27, 0.27) đỏ
    dominant occ → COL_OCCUPIED
    dominant emp → COL_EMPTY
    otherwise   → COL_MIXED   (0.90, 0.85, 0.20) vàng nhạt
- 1 icon selected (hidden mặc định) → visible khi SelectBuilding()
  màu COL_SELECTED (0.18, 0.56, 0.91) xanh dương, scale=8f
- Icons tạo khi OnBuildingLiveDataUpdated fire lần đầu (data ready)
- LateUpdate: billboard (LookAt camera)
```

### UIManager
**Attach:** DUT_UI GO. **sidebar-body** là container UIManager inject content.

**Panel states:**
```
NoSelection → BuildPanel_Overview():
  - MiniStats: Occupied/Empty/Upcoming/Total
  - Alert list (top 4)
  - Hint text

BuildingSelected → BuildPanel_Building(id):
  - Header: badge + tên + meta
  - StatusBanner: màu + text theo status
  - TabBar: 6 tabs
  - Tab content (switch _activeTab 0-5)
  - Back button → ClearSelection()
```

**6 Tabs:**
| Index | Tên | Content |
|-------|-----|---------|
| 0 | Lịch học | current_class, next_class, today_classes |
| 1 | Mật độ | density_ratio, level, progress bar |
| 2 | Hạ tầng | Điện/Nước/Nhiệt/AC rows |
| 3 | Thiết bị | stats + error cards |
| 4 | Sự kiện | event cards |
| 5 | Bảo trì | maintenance tickets |

**Rate limiting:** OnDataUpdated rebuild tối đa 1 lần / 2 giây.

---

## 8. CAMERA SYSTEM

### Overview defaults (hardcoded trong Start())
```
pivot    = (370, 0, 320)   // CameraPivot GO
dist     = 420
pitch    = 28°
yaw      = 45°             // nhìn từ NE về SW
```

### Fly-to khi SelectBuilding
```csharp
float dist = Clamp(Max(bounds.x, bounds.z) * 1.8, 60, 200);
_cam.FlyTo(info.world_position, dist);
// 0.8s SmoothStep easing
```

### Directional Light
```
rotation = (45, 45, 0)  // cùng hướng camera yaw=45
intensity = 1.2
```

---

## 9. UI SYSTEM

### Layout (DUT_Main.uxml)
```
root (transparent — clearColor=false)
├── topbar (52px)
│   ├── logo
│   ├── nav (Campus 3D | Dashboard | Lịch học)
│   └── datetime + LIVE badge
└── main-content (flex-row)
    ├── viewport (flex-grow: 7, transparent)
    │   ├── search-bar (overlay top-left)
    │   ├── vp-buttons (+/-/R)
    │   ├── legend (bottom-left)
    │   └── ministats (bottom-left above legend)
    └── sidebar (380px)
        ├── sidebar__tabs
        └── sidebar-body    ← UIManager inject vào đây
```

### Quan trọng với UI Toolkit
- `PanelSettings.clearColor = false` → model 3D thấy xuyên qua UI
- `DUT_Dashboard`, `DUT_Schedule` phải `SetActive(false)` trước Play
- Không dùng `:last-child` (không hỗ trợ trong Unity UI Toolkit)
- `cursor: default` trên Button để tránh warning
- `sidebar-body` (name attr) là injection point của UIManager

---

## 10. SPRINT 2 — LAYERMANAGER (VIỆC TIẾP THEO)

### Mục tiêu
Toggle 6 data layers → icon màu per khu thay đổi theo layer đang active.

### LayerManager.cs — Cần tạo mới tại `Assets/Scripts/Core/LayerManager.cs`

```csharp
namespace DUT.Core
{
    public class LayerManager : MonoBehaviour
    {
        public static LayerManager Instance;
        public BuildingDataStore store;

        LayerId _primaryLayer = LayerId.Schedule;

        public event Action<LayerId> OnPrimaryLayerChanged;

        public LayerId PrimaryLayer => _primaryLayer;

        public void SetPrimaryLayer(LayerId layer)
        {
            _primaryLayer = layer;
            OnPrimaryLayerChanged?.Invoke(layer);
        }

        // Tính màu cho 1 building theo layer đang active
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
    }
}
```

### Color rules per layer

```
SCHEDULE:
  Occupied → đỏ    (0.94, 0.27, 0.27)
  Empty    → xanh  (0.13, 0.77, 0.37)
  Upcoming → vàng  (0.96, 0.62, 0.04)

OCCUPANCY (density_ratio):
  < 0.2  → xanh lá   (vắng)
  < 0.5  → xanh dương (bình thường)
  < 0.8  → vàng      (đông)
  >= 0.8 → đỏ        (quá đông)

INFRASTRUCTURE:
  has_alert=false → xám (0.48, 0.60, 0.72)
  warning         → vàng cam
  critical        → đỏ

EQUIPMENT:
  errors=0        → xám
  has_critical=F  → vàng
  has_critical=T  → đỏ

EVENTS:
  no events       → xám
  events today    → xanh dương
  large (>50 pax) → tím (0.66, 0.33, 0.98)

MAINTENANCE:
  Normal          → xám
  Scheduled       → xanh dương nhạt
  InProgress      → vàng cam
  Disrupted/Crit  → đỏ
```

### Cập nhật StatusDotManager cho LayerManager

```csharp
// Trong StatusDotManager.Start():
if (LayerManager.Instance != null)
    LayerManager.Instance.OnPrimaryLayerChanged += _ => RefreshAll();

// GetKhuColor() mới — dùng LayerManager thay vì hardcode Schedule:
Color GetKhuColor(string khu)
{
    if (LayerManager.Instance == null)
        return GetKhuScheduleColor(khu); // fallback cũ

    // Tính dominant color theo layer hiện tại
    var colors = new List<Color>();
    foreach (var b in store.AllBuildings)
    {
        if (b.ten_khu != khu) continue;
        colors.Add(LayerManager.Instance.GetBuildingColor(b.building_id));
    }
    // return dominant color (most frequent)
}
```

### Layer toggle buttons — thêm vào DUT_Main.uxml (topbar)
```xml
<ui:VisualElement name="layer-toggles" class="layer-toggles">
    <ui:Button name="layer-schedule"  class="layer-btn layer-btn--active" text="📚 Lịch học" />
    <ui:Button name="layer-occupancy" class="layer-btn" text="👥 Mật độ" />
    <ui:Button name="layer-infra"     class="layer-btn" text="⚡ Hạ tầng" />
    <ui:Button name="layer-equipment" class="layer-btn" text="🖥 Thiết bị" />
    <ui:Button name="layer-events"    class="layer-btn" text="📅 Sự kiện" />
    <ui:Button name="layer-maint"     class="layer-btn" text="🔧 Bảo trì" />
</ui:VisualElement>
```

### Thứ tự implement Sprint 2
```
1. Tạo Assets/Scripts/Core/LayerManager.cs
2. Gắn LayerManager vào Managers GO, assign store
3. Cập nhật StatusDotManager.GetKhuColor() dùng LayerManager
4. Thêm layer-toggles vào DUT_Main.uxml + style trong DUT_Main.uss
5. Bind buttons trong UIManager.BindNav()
6. Test: click từng layer → khu icon đổi màu đúng
```

---

## 11. DESIGN TOKENS

### Colors (USS)
```css
--bg-app:       rgb(11, 21, 32)      /* #0B1520 */
--bg-topbar:    rgb(8, 16, 26)
--bg-sidebar:   rgb(13, 24, 38)
--bg-card:      rgb(16, 30, 46)
--border:       rgb(26, 48, 80)
--text-primary: rgb(232, 240, 248)
--text-muted:   rgb(122, 154, 184)
--accent:       rgb(46, 143, 232)
--status-occ:   rgb(239, 68, 68)
--status-empty: rgb(34, 197, 94)
--status-soon:  rgb(245, 158, 11)
```

### Icon Colors (C#)
```csharp
COL_EMPTY    = (0.13, 0.77, 0.37)  // xanh lá
COL_OCCUPIED = (0.94, 0.27, 0.27)  // đỏ
COL_UPCOMING = (0.96, 0.62, 0.04)  // vàng cam
COL_SELECTED = (0.18, 0.56, 0.91)  // xanh dương (selected building)
COL_MIXED    = (0.90, 0.85, 0.20)  // vàng nhạt (khu hỗn hợp)
```

---

## 12. CAMPUS GEOGRAPHY

```
World bounds: center≈(347,20,395), size≈588×749 (X×Z)
Camera pivot: (370, 0, 320)
Model scale: 0.03, rotation: (270,~180,0)
22 khu: A,B,C,D,E,F,G,H,I,K,S,V,TV,XE,GDTC,KTX,N,CTT,UTCN,ISTAR,KHO,CT
65 buildings total
Building ID format: B_{khu}_{index_in_AllBuildings}
```

---

## 13. CHECKLIST TRƯỚC KHI BẮT ĐẦU SPRINT 2

```
□ 0 compile errors: read_console types=["error"]
□ Play mode verify:
  □ Log "[Boot] 65 buildings setup" xuất hiện
  □ 22 icons hiện trong WorldSpaceLabels sau ~2s
  □ Click tòa nhà → UIManager panel hiện đúng (tab Lịch học)
  □ Camera pan chậm vừa phải (panSpeed=0.1)
  □ Kéo chuột trái >5px → pan, click nhanh → select
□ Tạo LayerManager.cs theo spec mục 10
□ Gắn vào Managers GO
□ Test từng layer button → icon màu đổi đúng
□ Verify UIManager 6 tabs vẫn hoạt động sau Sprint 2
```

---

*v5.0 | 2026-05-24 | Sprint 1: DONE | Sprint 2: LayerManager*
