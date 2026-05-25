# DUT Digital Twin — UI Design Brief
> Tài liệu này dành cho Claude (claude.ai) để hiểu toàn bộ dự án và thiết kế lại giao diện.

---

## 1. Tổng quan dự án

**Tên:** DUT Digital Twin — Digital Twin campus Đại học Bách Khoa Đà Nẵng  
**Mục tiêu:** Hệ thống giám sát campus real-time cho Ban Giám hiệu và Phòng Quản trị.  
**Engine:** Unity 6000.3.14f1, Universal Render Pipeline (URP)  
**UI Framework:** Unity UI Toolkit — UXML (cấu trúc) + USS (style, giống CSS)  
**Namespace:** `DUT.Data`, `DUT.Core`, `DUT.UI`

### Đối tượng sử dụng
Người quản trị campus (Ban Giám hiệu, Phòng Quản trị) — nhìn màn hình 1 lần phải thấy ngay "bức tranh toàn cảnh": phòng nào đang có lớp, khu nào bất thường, cảnh báo gì cần xử lý. **Không phải app lịch học có model 3D.**

---

## 2. Kiến trúc hệ thống

```
SceneBootstrapper (Awake)
  ├── BuildingDataStore (ScriptableObject) — event bus trung tâm
  ├── MockDataProvider — sinh mock data 6 layers, refresh mỗi 60s
  ├── RealDataProvider — load StreamingAssets/schedule_data.json (925 slots/tuần thật)
  ├── LayerManager — toggle 6 data layers, đổi màu status dots trên 3D
  ├── AlertManager — scan alerts mỗi 30s, bell badge + toast + dropdown
  ├── ScreenManager — navigation 3 màn hình (Campus3D / Dashboard / Schedule)
  └── UIManager — rebuild sidebar dynamically khi data thay đổi
```

### 65 tòa nhà, chia theo khu:
`A, B, C, D, E, F, H, Khu-vực-khác` (một số khu có tên đặc biệt)

---

## 3. Data Models (những gì UI có thể đọc)

### BuildingInfo (static, mỗi tòa nhà)
```
building_id, ten_khu, ten_ngan, ten_day_du
so_tang, chuc_nang (GiangDuong/HanhChinh/ThiNghiem/TienIch/GDTC/KyTucXa/HoiTruong)
so_phong, suc_chua_toi_da, world_position
```

### BuildingLiveData (real-time, cập nhật mỗi 30–60s)

**schedule** — Lịch học
```
status: Empty | Occupied | Upcoming
today_classes: List<ClassInfo>
  ├── class_name, class_code, group, lecturer
  ├── time_start, time_end (format "07:30")
  ├── student_count, student_capacity
  ├── room_id (chứa phòng dạng "phong=B201")
  └── progress (0.0–1.0, % buổi học đã qua)
```

**occupancy** — Mật độ người
```
current_count, max_capacity, density_ratio (0–1)
level: Empty | Low | Medium | High | Overcrowded
trend: string ("↑ tăng", "→ ổn định", ...)
hourly_today: List<{hour, count}>
```

**infrastructure** — Hạ tầng
```
electric: { current_kw, avg_kw, daily_kwh, is_abnormal }
water:    { current_lph, is_abnormal }
climate:  { temperature_c, humidity_pct, ac_total, ac_active, ac_error }
```

**equipment** — Thiết bị
```
total_devices, active_devices, error_devices, has_critical_error
errors: List<{ device_name, location, error_type, reported_at, severity }>
```

**events** — Sự kiện
```
List<{ event_name, event_type, time_start, time_end, location, attendee_count }>
```

**maintenance** — Bảo trì
```
has_active_work, overall_status
active_tickets: List<{ title, affected_area, started_at, expected_done, severity }>
```

### AlertData
```
alert_id, building_id, layer (Schedule/Occupancy/Infrastructure/Equipment/Event/Maintenance)
severity: Info | Warning | Critical
title, description, timestamp, is_acknowledged, action_required
```

### Campus-level aggregates (computed từ store)
```csharp
store.CountByStatus(BuildingStatus.Occupied)   // số tòa đang có lớp
store.CountByStatus(BuildingStatus.Empty)       // số tòa trống
store.CountByStatus(BuildingStatus.Upcoming)    // sắp có lớp (< 90 phút)
store.AllBuildings.Count                        // tổng 65 tòa
store.GetBuildingsInKhu(khu)                   // tòa theo khu
store.GetStatus(building_id)                   // status 1 tòa
AlertManager.Instance.Alerts                   // IReadOnlyList<AlertData>, sorted Critical→Warning→Info
DUTTime.Now                                    // DateTime UTC+7
```

---

## 4. Cấu trúc UI hiện tại

### Layout tổng thể
```
┌─────────────────────────────────────────────────────┐
│  TOPBAR (52px) — logo | nav | KPI chips | bell | time│
├─────────────────────────────────────────────────────┤
│  LAYER BAR (40px) — toggle 6 data layers             │
├───────────────────────────────┬─────────────────────┤
│                               │  SIDEBAR (380px)    │
│   VIEWPORT (flex-grow: 7)     │  ┌ building-header  │
│   (3D canvas — Unity renders  │  ├ status-banner    │
│    the campus model here,     │  └ ScrollView       │
│    UI is overlay on top)      │    └ sidebar-body   │
│                               │      (rebuilt dynamically)
│  ┌ viewport-overlay (top-left)│                     │
│  ├ legend     (bottom-left)   │                     │
│  └ ministats  (bottom-left)   │                     │
└───────────────────────────────┴─────────────────────┘
│  ALERT DROPDOWN (absolute, top-right overlay)        │
│  ALERT TOAST    (absolute, top-right overlay)        │
```

### Các màn hình (ScreenManager)
- **Campus3D** — màn hình chính (UXML trên) + 3D viewport
- **Dashboard** — analytics screen (file riêng: `DUT_Dashboard.uxml`)
- **Schedule** — lịch học grid (file riêng: `DUT_Schedule.uxml`)

### Sidebar states
- **Overview** (không chọn tòa nào): campus KPI cards + khu breakdown (progress bars) + alert summary + timeline
- **BuildingSelected** (click tòa): compact building card + khu breakdown + alert summary + nút deselect

---

## 5. File cần thiết kế lại

| File | Mô tả |
|------|-------|
| `Assets/UI/DUT_Main.uxml` | UXML chính — Campus3D screen |
| `Assets/UI/DUT_Main.uss`  | USS stylesheet chính |
| `Assets/Scripts/UI/UIManager.cs` | C# rebuild sidebar dynamically |

> File Dashboard và Schedule (`DUT_Dashboard.uxml`, `DUT_Schedule.uxml`) sẽ thiết kế sau.

---

## 6. USS — Các ràng buộc syntax (QUAN TRỌNG)

Unity UI Toolkit dùng USS gần giống CSS nhưng có nhiều khác biệt:

```css
/* ✅ ĐÚNG */
padding-top: 12px;
padding-left: 16px;
background-color: rgba(8, 16, 26, 0.9);
border-radius: 8px;           /* shorthand OK */
border-width: 1px;            /* shorthand OK */
flex-direction: row;
align-items: center;
justify-content: flex-end;
-unity-font-style: bold;      /* thay vì font-weight */
-unity-text-align: middle-center; /* thay vì text-align */
letter-spacing: 1px;
white-space: normal;
overflow: hidden;
cursor: default;

/* ❌ SAI — không tồn tại trong USS */
font-weight: bold;       /* dùng -unity-font-style: bold */
text-align: center;      /* dùng -unity-text-align: middle-center */
gap: 8px;                /* không có — dùng margin */
grid-template-columns:   /* không có — dùng flexbox */
display: grid;           /* không có */
:focus, :active          /* có giới hạn */
transition, animation    /* không hỗ trợ trong USS */
box-shadow               /* không hỗ trợ */
```

**Các giá trị `-unity-text-align` hợp lệ:**
`upper-left, middle-left, lower-left, upper-center, middle-center, lower-center, upper-right, middle-right, lower-right`

**Pseudo-classes hỗ trợ:** `:hover`, `:active`, `:disabled`, `:checked`, `:focus`

**Position:** `absolute` (tách khỏi flow) hoặc `relative` (default).  
Dùng `top/left/right/bottom` chỉ khi `position: absolute`.

---

## 7. UXML — Syntax elements chính

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
  <ui:Style src="DUT_Main.uss" />
  <ui:VisualElement name="root" class="root">
    <ui:Label text="Hello" />
    <ui:Button name="my-btn" text="Click me" />
    <ui:ScrollView vertical-scroller-visibility="Hidden"
                   horizontal-scroller-visibility="Hidden">
      <ui:VisualElement name="inner" />
    </ui:ScrollView>
  </ui:VisualElement>
</ui:UXML>
```

Các elements: `VisualElement`, `Label`, `Button`, `ScrollView`, `TextField`, `Toggle`, `Slider`, `ProgressBar`.

---

## 8. UIManager.cs — Pattern code (C#)

UIManager rebuild sidebar bằng code C# mỗi khi data thay đổi. Output là `VisualElement` tree được add vào `sidebar-body`.

```csharp
// Query elements từ UXML
var body = _root.Q<VisualElement>("sidebar-body");
var lbl  = _root.Q<Label>("building-name");
lbl.text = "Tên tòa nhà";

// Tạo element mới
var el = new VisualElement();
el.style.flexDirection = FlexDirection.Row;
el.style.paddingTop = 12;
el.style.backgroundColor = new StyleColor(new Color(0.05f, 0.12f, 0.20f));
el.style.borderTopLeftRadius = el.style.borderTopRightRadius = 8;
el.style.borderBottomLeftRadius = el.style.borderBottomRightRadius = 8;

// Border (shorthand helper SetBorder)
void SetBorder(VisualElement ve, Color col, float w, float r) { ... }

// Màu sắc constants đang dùng
static readonly Color C_OCCUPIED = new Color(0.94f, 0.27f, 0.27f); // đỏ
static readonly Color C_EMPTY    = new Color(0.13f, 0.77f, 0.37f); // xanh lá
static readonly Color C_UPCOMING = new Color(0.96f, 0.62f, 0.04f); // cam
static readonly Color C_BLUE     = new Color(0.18f, 0.56f, 0.91f); // xanh dương
static readonly Color C_MUTED    = new Color(0.48f, 0.60f, 0.72f); // xám xanh
static readonly Color C_DIM      = new Color(0.24f, 0.35f, 0.44f); // tối

// CSS class toggle
el.AddToClassList("my-class");
el.RemoveFromClassList("my-class");

// Event listener
btn.RegisterCallback<ClickEvent>(_ => store.SelectBuilding(id));
row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = ...);
```

---

## 9. Color Palette hiện tại

```
Background chính:  rgb(8, 16, 26)   — navy rất tối
Background sidebar: rgb(13, 24, 38)
Background card:    rgb(16, 30, 46)
Border:             rgb(26, 48, 80)  — border mặc định
Border hover:       rgb(46, 143, 232)

Text primary:       rgb(232, 240, 248) — gần trắng
Text secondary:     rgb(122, 154, 184) — xám xanh
Text dim:           rgb(61, 88, 112)   — mờ
Accent blue:        rgb(46, 143, 232)  — xanh chính

Status occupied:    rgb(239, 68, 68)   — đỏ
Status empty:       rgb(34, 197, 94)   — xanh lá
Status upcoming:    rgb(245, 158, 11)  — cam
Status selected:    rgb(46, 143, 232)  — xanh
```

---

## 10. Vấn đề hiện tại của giao diện

### Vấn đề UX
1. **Sidebar quá text-heavy** — dữ liệu hiển thị dạng list, không có hierarchy rõ ràng, không "scan" được nhanh bằng mắt
2. **Không có visual hierarchy** — tất cả sections trông giống nhau, không biết nhìn vào đâu trước
3. **KPI cards trong sidebar quá nhỏ** — số liệu quan trọng nhất (65 tòa, bao nhiêu có lớp) bị ẩn sâu
4. **Layer bar chiếm không gian** nhưng ít dùng — toggle layer nên gọn hơn
5. **3D viewport và sidebar không "nói chuyện" với nhau** — chưa có cảm giác connected
6. **Không có sense of urgency** — alert hiển thị nhưng không nổi bật trong toàn cảnh

### Vấn đề kỹ thuật
- USS không support `gap`, `grid`, `animation` — phải dùng flexbox + margin
- ScrollView trong Unity cần `overflow: hidden` trên container để scroll hoạt động
- Sidebar `sidebar-body` được rebuild hoàn toàn mỗi 2s khi data thay đổi — nên dùng USS classes thay vì inline styles khi có thể để tránh layout recalc

---

## 11. Yêu cầu thiết kế mới

### Nguyên tắc cốt lõi
> **"Nhìn một phát ra bức tranh toàn cảnh"** — người quản trị mở app lên phải thấy ngay: campus đang ở trạng thái nào, khu nào cần chú ý, có bao nhiêu cảnh báo.

### Layout target
```
┌──────────────────────────────────────────────────────────────────┐
│  TOPBAR — compact, thông tin quan trọng nổi bật hơn              │
├──────────────────────────────────────────────────────────────────┤
│         │                                    │                   │
│  LEFT   │        3D VIEWPORT                 │   RIGHT PANEL     │
│  PANEL  │        (3D campus model)           │   (Command Center)│
│ (ẩn/   │                                    │                   │
│  mini)  │   [overlay: legend, stats,         │                   │
│         │    search, zoom controls]          │                   │
│         │                                    │                   │
└──────────────────────────────────────────────────────────────────┘
```

### Right Panel (sidebar) — "Command Center"
Luôn hiển thị campus overview, không bao giờ mất đi khi click building:

**State 1 — Campus Overview:**
```
┌─────────────────────────────┐
│  [CAMPUS] Bách Khoa Đà Nẵng │  ← fixed header
│  65 tòa · HH:mm ICT        │
├─────────────────────────────┤
│  STATUS BANNER              │  ← xX% campus hoạt động
├─────────────────────────────┤
│  KPI: [Có lớp] [Trống]     │  ← số lớn, màu rõ
│       [Sắp có] [Tổng]      │
├─────────────────────────────┤
│  THEO KHU (progress bars)   │
│  A ████░░ 8/12              │
│  B ██░░░░ 3/10              │
│  ...                        │
├─────────────────────────────┤
│  CẢNH BÁO (nếu có)         │
│  🔴 B2 — Điện bất thường   │
│  ⚠  H1 — 3 AC lỗi          │
├─────────────────────────────┤
│  TIMELINE HÔM NAY           │
│  07 ─────█████──██───ℕ─── 21│
└─────────────────────────────┘
```

**State 2 — Building Selected (click vào tòa 3D):**
```
┌─────────────────────────────┐
│  [KHU B] Tòa B2             │  ← fixed header đổi
│  5 tầng · Giảng đường       │
├─────────────────────────────┤
│  STATUS: ĐANG CÓ 6 LỚP      │  ← banner đỏ/xanh/cam
├─────────────────────────────┤
│  BUILDING CARD              │
│  • 6 lớp đang học           │
│  • ⚡ Điện bất thường (tag) │
├─────────────────────────────┤
│  THEO KHU (vẫn hiện)        │
├─────────────────────────────┤
│  CẢNH BÁO (vẫn hiện)        │
├─────────────────────────────┤
│  [← Bỏ chọn tòa nhà]        │
└─────────────────────────────┘
```

### Viewport Overlay
Các element overlay trên 3D canvas:
- **Search bar** — top-left, tìm tòa nhà/phòng
- **Zoom controls** (+/-/Reset) — kế search bar
- **Legend** — bottom-left: màu dots trạng thái
- **Mini stats** — bottom-left above legend: 4 KPI cards nhỏ

### Topbar
- Logo (trái)
- Nav tabs (giữa): Campus 3D | Dashboard | Lịch học
- KPI chips: "32 có lớp" (xanh) | "3 ⚠ alerts" (đỏ)
- Bell icon + badge
- Live dot + LIVE label
- Datetime

### Layer Bar
Nằm dưới topbar — toggle 6 layers dữ liệu: 📚 Lịch học | 👥 Mật độ | ⚡ Hạ tầng | 🖥 Thiết bị | 📅 Sự kiện | 🔧 Bảo trì

---

## 12. Yêu cầu output từ Claude

Khi thiết kế, hãy cung cấp:

### A. `DUT_Main.uxml` (hoàn chỉnh)
- Cấu trúc UXML với tất cả `name` attributes để UIManager.cs có thể query
- Các `name` quan trọng phải giữ nguyên (xem danh sách bên dưới)
- Chỉ dùng elements: `VisualElement`, `Label`, `Button`, `ScrollView`

### B. `DUT_Main.uss` (hoàn chỉnh)
- Toàn bộ stylesheet, bao gồm Dashboard và Schedule styles
- Tuân thủ USS syntax (không dùng `gap`, `grid`, `animation`, `box-shadow`)
- Dùng `flex-direction`, `align-items`, `justify-content` cho layout
- Dark theme, color palette giữ nguyên hoặc cải thiện (navy/blue đậm)

### C. Ghi chú thay đổi UIManager.cs
- Nếu thêm/đổi `name` của elements, cần liệt kê để dev cập nhật C# queries
- Không cần viết toàn bộ UIManager.cs — chỉ cần ghi chú phần nào cần sửa

---

## 13. Danh sách `name` elements phải giữ nguyên

UIManager.cs và AlertManager.cs query các elements sau bằng `_root.Q<T>("name")`:

```
# Topbar
"kpi-occupied"       Label — chip số tòa có lớp
"kpi-alerts"         Label — chip số cảnh báo
"alert-bell"         Button — chuông
"alert-badge"        Label — số đỏ trên chuông
"datetime-label"     Label — hiện giờ HH:mm:ss
"live-dot"           VisualElement — chấm xanh nhấp nháy

# Nav
"nav-3d"             Button — tab Campus 3D
"nav-dashboard"      Button — tab Dashboard
"nav-schedule"       Button — tab Lịch học

# Layer toggles
"layer-schedule"     Button
"layer-occupancy"    Button
"layer-infra"        Button
"layer-equipment"    Button
"layer-events"       Button
"layer-maint"        Button

# Sidebar
"building-header"    VisualElement — header cố định
"building-badge"     Label — "CAMPUS" hoặc "KHU X"
"building-name"      Label — tên tòa/campus
"building-meta"      Label — metadata
"status-banner"      VisualElement — banner màu
"sidebar-body"       VisualElement — nơi UIManager rebuild content

# Viewport overlay
"viewport-overlay"   VisualElement
"search-bar"         Label (placeholder, chưa interactive)
"ministats"          VisualElement — 4 stat cards mini
"legend"             VisualElement — bảng màu

# Alerts
"alert-dropdown"     VisualElement — dropdown panel
"alert-dropdown-list" VisualElement — nơi AlertManager add rows
"alert-dropdown-count" Label — "X cảnh báo"
"alert-toast"        VisualElement — toast notification
"alert-toast-msg"    Label — nội dung toast
```

---

## 14. Ví dụ USS hợp lệ (để tham khảo)

```css
/* Sidebar section */
.section {
    padding-top: 14px;
    padding-bottom: 14px;
    padding-left: 16px;
    padding-right: 16px;
    border-bottom-width: 1px;
    border-bottom-color: rgb(26, 48, 80);
    flex-shrink: 0;
}

.section__title {
    font-size: 9px;
    color: rgb(61, 88, 112);
    -unity-font-style: bold;
    letter-spacing: 1px;
    margin-bottom: 10px;
}

/* Card với border */
.building-card {
    margin-left: 12px;
    margin-right: 12px;
    margin-top: 8px;
    margin-bottom: 4px;
    padding-top: 10px;
    padding-bottom: 10px;
    padding-left: 12px;
    padding-right: 12px;
    background-color: rgb(16, 30, 46);
    border-width: 1px;
    border-color: rgb(26, 48, 80);
    border-radius: 8px;
}

/* Progress bar */
.progress-track {
    height: 5px;
    background-color: rgb(22, 38, 56);
    border-radius: 3px;
    flex-grow: 1;
}
.progress-fill {
    height: 5px;
    border-radius: 3px;
    background-color: rgb(46, 143, 232);
    /* width được set bằng C#: fill.style.width = new StyleLength(new Length(ratio*100, LengthUnit.Percent)); */
}
```

---

## 15. Pitfalls kỹ thuật cần tránh

1. **`padding: 12px 16px`** — shorthand 2-giá-trị KHÔNG hoạt động trong USS. Phải viết từng thuộc tính:
   ```css
   padding-top: 12px; padding-bottom: 12px;
   padding-left: 16px; padding-right: 16px;
   ```

2. **`gap: 8px`** — không tồn tại. Dùng `margin-right/bottom` trên children.

3. **`width: 100%` trong ScrollView child** — có thể gây layout issue. Dùng `flex-grow: 1` thay thế khi có thể.

4. **`position: absolute`** chỉ dùng cho overlays (dropdown, toast, legend). Sidebar content phải dùng flex layout bình thường.

5. **`border-color` shorthand** OK trong USS: `border-color: rgb(26, 48, 80);`

6. **Font không load được** — chỉ dùng system fonts, không reference font file trừ khi đã có trong project.

7. **`display: none`** không có trong USS — dùng `display: DisplayStyle.None` trong C# hoặc conditional rendering.

---

## 16. Cấu trúc file hiện tại

```
Assets/
  UI/
    DUT_Main.uxml        ← file cần redesign
    DUT_Main.uss         ← file cần redesign
    DUT_Dashboard.uxml   ← Dashboard screen
    DUT_Dashboard.uss    ← Dashboard styles
    DUT_Schedule.uxml    ← Schedule screen
    DUT_Schedule.uss     ← Schedule styles
  Scripts/
    UI/
      UIManager.cs       ← rebuild sidebar content
      AlertManager.cs    ← bell badge, toast, dropdown
      ScreenManager.cs   ← navigation 3 màn hình
      DUTTime.cs         ← DateTime UTC+7
    Core/
      LayerManager.cs    ← toggle 6 data layers
      CameraController.cs
      SceneBootstrapper.cs
    Data/
      DUTModels.cs       ← tất cả data structs (xem phần 3)
      BuildingDataStore.cs
      MockDataProvider.cs
      RealDataProvider.cs
```

---

*Document version 1.0 — 2026-05-25*  
*Cập nhật khi có sprint mới hoặc thay đổi kiến trúc.*
