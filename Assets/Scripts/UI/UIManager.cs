using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DUT.Data;
using DUT.Core;

namespace DUT.UI
{
    public enum PanelState { NoSelection, BuildingSelected }

    [RequireComponent(typeof(UIDocument))]
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }
        public BuildingDataStore store;

        UIDocument      _doc;
        VisualElement   _root;
        VisualElement   _leftPanelBody;
        PanelState      _state      = PanelState.NoSelection;
        string          _selectedId;
        int             _activeTab  = 0;
        CameraController _cam;

        static readonly Color C_OCCUPIED = new Color(0.94f, 0.27f, 0.27f);
        static readonly Color C_EMPTY    = new Color(0.13f, 0.77f, 0.37f);
        static readonly Color C_UPCOMING = new Color(0.96f, 0.62f, 0.04f);
        static readonly Color C_BLUE     = new Color(0.18f, 0.56f, 0.91f);
        static readonly Color C_MUTED    = new Color(0.48f, 0.60f, 0.72f);
        static readonly Color C_DIM      = new Color(0.24f, 0.35f, 0.44f);

        void Awake() { Instance = this; _doc = GetComponent<UIDocument>(); }

        void OnEnable()
        {
            Instance = this;
            if (store == null) store = Resources.Load<BuildingDataStore>("BuildingDataStore");
            SubscribeEvents();
            StartCoroutine(Co_RebindUI());
        }

        void OnDisable() { if (Instance == this) Instance = null; UnsubscribeEvents(); }
        void OnDestroy() => UnsubscribeEvents();

        void Start()
        {
            if (store == null) store = Resources.Load<BuildingDataStore>("BuildingDataStore");
            _cam = FindFirstObjectByType<CameraController>();
        }

        IEnumerator Co_RebindUI()
        {
            yield return null;
            _root = _doc?.rootVisualElement;
            if (_root == null) yield break;
            _leftPanelBody = _root.Q<VisualElement>("left-panel-body");
            BindNav();
            if (_state == PanelState.BuildingSelected && _selectedId != null)
                BuildPanel_Building(_selectedId);
            else
                BuildPanel_Overview();
        }

        void SubscribeEvents()
        {
            if (store == null) return;
            UnsubscribeEvents();
            store.OnBuildingSelected        += OnBuildingSelected;
            store.OnBuildingLiveDataUpdated += OnDataUpdated;
            store.OnSelectionCleared        += OnSelectionCleared;
        }

        void UnsubscribeEvents()
        {
            if (store == null) return;
            store.OnBuildingSelected        -= OnBuildingSelected;
            store.OnBuildingLiveDataUpdated -= OnDataUpdated;
            store.OnSelectionCleared        -= OnSelectionCleared;
        }

        void OnBuildingSelected(string id)
        {
            if (id == null) { BuildPanel_Overview(); return; }
            _selectedId = id; _state = PanelState.BuildingSelected; _activeTab = 0;
            var info = store.GetInfo(id);
            if (info != null && _cam != null)
            {
                float dist = Mathf.Clamp(Mathf.Max(info.bounds_size.x, info.bounds_size.z) * 1.8f, 60f, 200f);
                _cam.FlyTo(info.world_position, dist);
            }
            BuildPanel_Building(id);
        }

        float _lastUpdate = -999f;
void OnDataUpdated(string id)
{
    if (UnityEngine.Time.time - _lastUpdate < 2f) return;
    _lastUpdate = UnityEngine.Time.time;
    if (_state == PanelState.BuildingSelected && _selectedId != null)
        BuildPanel_Building(_selectedId);
    else
        BuildPanel_Overview();
}

        void OnSelectionCleared()
        {
            _selectedId = null; _state = PanelState.NoSelection;
            BuildPanel_Overview(); _cam?.ReturnToOverview();
        }

        void BindNav()
        {
            _root?.Q<Button>("nav-3d")       ?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance?.ShowScreen(ScreenManager.Screen.Campus3D));
            _root?.Q<Button>("nav-dashboard")?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance?.ShowScreen(ScreenManager.Screen.Dashboard));
            _root?.Q<Button>("nav-schedule") ?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance?.ShowScreen(ScreenManager.Screen.Schedule));
            var layerMap = new Dictionary<string, LayerId> {
                { "layer-schedule",  LayerId.Schedule },{ "layer-occupancy", LayerId.Occupancy },
                { "layer-infra",     LayerId.Infrastructure },{ "layer-equipment", LayerId.Equipment },
                { "layer-events",    LayerId.Events },{ "layer-maint", LayerId.Maintenance },
            };
            foreach (var kv in layerMap) {
                string btn = kv.Key; LayerId lid = kv.Value;
                _root?.Q<Button>(btn)?.RegisterCallback<ClickEvent>(_ => { LayerManager.Instance?.SetPrimaryLayer(lid); RefreshLayerButtons(btn); });
            }
        }

        void RefreshLayerButtons(string active) {
            foreach (var n in new[]{ "layer-schedule","layer-occupancy","layer-infra","layer-equipment","layer-events","layer-maint" }) {
                var b = _root?.Q<Button>(n); if (b == null) continue;
                if (n == active) b.AddToClassList("layer-btn--active"); else b.RemoveFromClassList("layer-btn--active");
            }
        }

        // ── Overview ─────────────────────────────────────────────────────
void BuildPanel_Overview()
{
    if (_root == null) return;
    int occ = store?.CountByStatus(BuildingStatus.Occupied) ?? 0;
    int emp = store?.CountByStatus(BuildingStatus.Empty)    ?? 0;
    int upc = store?.CountByStatus(BuildingStatus.Upcoming) ?? 0;
    int tot = store?.AllBuildings?.Count ?? 0;
    int alertCount = AlertManager.Instance?.Alerts?.Count ?? 0;

    BuildLeftPanel(occ, emp, upc, tot);
    UpdateTopbarKpi(occ, alertCount);
    UpdateMinistats(occ, emp, upc, tot);

    // Header: campus identity
    Show("building-header", true);
    _root.Q<Label>("building-badge")?.SetText("CAMPUS");
    _root.Q<Label>("building-name") ?.SetText("Bách Khoa Đà Nẵng");
    _root.Q<Label>("building-meta") ?.SetText($"{tot} tòa nhà  ·  {DUTTime.Now:HH:mm} ICT");

    // Status banner: campus live
    int pct = tot > 0 ? occ * 100 / tot : 0;
    var banner = _root.Q<VisualElement>("status-banner");
    if (banner != null) {
        Show("status-banner", true);
        SetClass(banner, "status-banner--occupied", "status-banner--upcoming", "status-banner--empty",
            occ > 0 ? "status-banner--occupied" : "status-banner--empty");
        banner.Q<Label>(className:"status-label")?.SetText($"{occ} ĐANG CÓ LỚP");
        string alertDesc = alertCount == 0 ? "Hệ thống bình thường"
            : alertCount == 1 ? "1 cảnh báo cần xử lý"
            : $"{alertCount} cảnh báo cần xử lý";
        bool hasCritical = AlertManager.Instance?.Alerts?.Any(a => a.severity == AlertSeverity.Critical) == true;
        banner.Q<Label>(className:"status-desc")?.SetText($"{pct}% campus hoạt động  ·  {alertDesc}{(hasCritical ? "  ⚠" : "")}");
    }

    // Body
    var body = _root.Q<VisualElement>("sidebar-body");
    if (body == null) return;
    body.Clear();

    body.Add(BuildSection_KpiCards(occ, emp, upc, tot));
    body.Add(BuildSection_KhuBreakdown());
    body.Add(BuildSection_AlertSummary());
    body.Add(BuildTimeline_Campus());
}

void UpdateMinistats(int occ, int emp, int upc, int tot)
{
    var ms = _root?.Q<VisualElement>("ministats");
    if (ms == null) return;
    var nums = ms.Query<Label>(null, "stat-card__num").ToList();
    if (nums.Count < 4) return;
    nums[0].text = occ.ToString();
    nums[1].text = emp.ToString();
    nums[2].text = upc.ToString();
    nums[3].text = tot.ToString();
}

        // ── Building ─────────────────────────────────────────────────────
void BuildPanel_Building(string id)
{
    var info = store?.GetInfo(id);
    var live = store?.GetLiveData(id);
    if (info == null || live == null || _root == null) return;

    int occ = store?.CountByStatus(BuildingStatus.Occupied) ?? 0;
    int emp = store?.CountByStatus(BuildingStatus.Empty)    ?? 0;
    int upc = store?.CountByStatus(BuildingStatus.Upcoming) ?? 0;
    int tot = store?.AllBuildings?.Count ?? 0;
    int alertCount = AlertManager.Instance?.Alerts?.Count ?? 0;
    BuildLeftPanel(occ, emp, upc, tot);
    UpdateTopbarKpi(occ, alertCount);
    UpdateMinistats(occ, emp, upc, tot);

    // Header: building identity
    Show("building-header", true);
    _root.Q<Label>("building-badge")?.SetText($"KHU {info.ten_khu}");
    _root.Q<Label>("building-name") ?.SetText(info.ten_ngan);
    _root.Q<Label>("building-meta") ?.SetText($"{info.so_tang} tầng  ·  {info.chuc_nang_str}");

    // Status banner: building status
    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    var ongoing  = GetClassesByTime(live, nowMin, true);
    var upcoming = GetClassesByTime(live, nowMin, false);
    var s = live.schedule.status;
    var banner = _root.Q<VisualElement>("status-banner");
    if (banner != null) {
        Show("status-banner", true);
        SetClass(banner, "status-banner--occupied","status-banner--upcoming","status-banner--empty",
            s==BuildingStatus.Occupied?"status-banner--occupied":s==BuildingStatus.Upcoming?"status-banner--upcoming":"status-banner--empty");
        var dot = banner.Q<VisualElement>(className:"status-dot");
        if (dot != null) SetClass(dot,"status-dot--occupied","status-dot--upcoming","status-dot--empty",
            s==BuildingStatus.Occupied?"status-dot--occupied":s==BuildingStatus.Upcoming?"status-dot--upcoming":"status-dot--empty");
        var lbl = banner.Q<Label>(className:"status-label");
        if (lbl != null) {
            SetClass(lbl,"status-label--occupied","status-label--upcoming","status-label--empty",
                s==BuildingStatus.Occupied?"status-label--occupied":s==BuildingStatus.Upcoming?"status-label--upcoming":"status-label--empty");
            lbl.text = s==BuildingStatus.Occupied?$"ĐANG CÓ {ongoing.Count} LỚP"
                     : s==BuildingStatus.Upcoming?"SẮP CÓ LỚP":"PHÒNG TRỐNG";
        }
        string desc = ongoing.Count>0 ? $"Kết thúc {ongoing.Max(c=>c.time_end)}"
                    : upcoming.Count>0 ? $"Bắt đầu {upcoming.Min(c=>c.time_start)}" : "Không có lịch hôm nay";
        banner.Q<Label>(className:"status-desc")?.SetText(desc);
    }

    var body = _root.Q<VisualElement>("sidebar-body");
    if (body == null) return;
    body.Clear();

    // ── 3-tab bar ────────────────────────────────────────────────────
    var tabBar = new VisualElement(); tabBar.AddToClassList("bld-tab-bar");
    var tabLabels = new[] { "📚 Lịch học", "🚪 Phòng", "⚡ Hạ tầng" };
    for (int i = 0; i < 3; i++) {
        int idx = i; string capturedId = id;
        var btn = new Button(() => { _activeTab = idx; BuildPanel_Building(capturedId); });
        btn.text = tabLabels[i]; btn.AddToClassList("bld-tab-btn");
        if (i == _activeTab) btn.AddToClassList("bld-tab-btn--active");
        tabBar.Add(btn);
    }
    body.Add(tabBar);

    // ── Tab content ───────────────────────────────────────────────────
    VisualElement tabContent = _activeTab switch {
        1 => Tab_RoomList(info, live),
        2 => Tab_Infrastructure(live),
        _ => Tab_Schedule(info, live),
    };
    body.Add(tabContent);

    // ── Back button ───────────────────────────────────────────────────
    var back = new Button(()=>store.ClearSelection()){text="← Bỏ chọn tòa nhà"};
    back.style.marginLeft=back.style.marginRight=12; back.style.marginBottom=12; back.style.marginTop=8;
    back.style.height=30; back.style.fontSize=11; back.style.color=new StyleColor(C_MUTED);
    back.style.backgroundColor=new StyleColor(new Color(0.06f,0.12f,0.18f));
    SetBorder(back,new Color(0.10f,0.19f,0.31f),1,6); body.Add(back);
}

        // ── Tab 0: Lịch học ───────────────────────────────────────────────
        VisualElement Tab_Schedule(BuildingInfo info, BuildingLiveData live)
        {
            var root=new VisualElement(); int nowMin=DUTTime.Now.Hour*60+DUTTime.Now.Minute;
            var all=live.schedule.today_classes??new List<ClassInfo>();
            var ongoing=GetClassesByTime(live,nowMin,true); var upcoming=GetClassesByTime(live,nowMin,false);
            var rest=all.Except(ongoing).Except(upcoming).OrderBy(c=>c.time_start).ToList();

            if (ongoing.Count>0){var sec=Sec($"ĐANG CÓ LỚP ({ongoing.Count} phòng)");foreach(var c in ongoing)sec.Add(ClassCard(c,true));root.Add(sec);}
            if (upcoming.Count>0){var sec=Sec($"SẮP CÓ ({upcoming.Count} lớp)");foreach(var c in upcoming.Take(3))sec.Add(ClassCard(c,false));root.Add(sec);}
            if (rest.Count>0){
                var sec=Sec($"HÔM NAY ({all.Count} lớp tổng)");
                foreach(var c in rest){
                    var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.paddingTop=row.style.paddingBottom=4;
                    row.style.borderBottomWidth=1;row.style.borderBottomColor=new StyleColor(new Color(0.10f,0.19f,0.31f));
                    var tl=Lbl(c.time_start,11,C_BLUE);tl.style.width=44;row.Add(tl);
                    var nm=Lbl(c.class_name,11,C_MUTED);nm.style.flexGrow=1;row.Add(nm);
                    string ph=ExtractPhong(c.room_id);if(!string.IsNullOrEmpty(ph))row.Add(Lbl(ph,10,C_DIM));
                    sec.Add(row);
                }
                root.Add(sec);
            }
            if(all.Count==0)root.Add(Lbl("Không có lịch học hôm nay",12,C_DIM));
            return root;
        }

        VisualElement ClassCard(ClassInfo c, bool isOngoing)
        {
            var card=new VisualElement();
            card.style.paddingTop=card.style.paddingBottom=8; card.style.paddingLeft=card.style.paddingRight=10; card.style.marginBottom=6;
            card.style.backgroundColor=new StyleColor(isOngoing?new Color(0.18f,0.05f,0.05f):new Color(0.04f,0.12f,0.20f));
            SetBorder(card,isOngoing?new Color(0.60f,0.15f,0.15f):new Color(0.10f,0.35f,0.60f),1,6);
            var hdr=new VisualElement();hdr.style.flexDirection=FlexDirection.Row;hdr.style.justifyContent=Justify.SpaceBetween;
            hdr.Add(Lbl(c.class_name,13,isOngoing?C_OCCUPIED:C_UPCOMING,true));hdr.Add(Lbl($"{c.time_start}–{c.time_end}",11,C_DIM));card.Add(hdr);
            card.Add(Lbl($"{c.class_code}  ·  {c.group}",10,C_DIM));card.Add(Lbl(c.lecturer,10,C_MUTED));
            var ft=new VisualElement();ft.style.flexDirection=FlexDirection.Row;ft.style.marginTop=4;ft.style.justifyContent=Justify.SpaceBetween;
            ft.Add(Lbl($"{c.student_count}/{c.student_capacity} SV",10,C_DIM));
            string ph=ExtractPhong(c.room_id);if(!string.IsNullOrEmpty(ph))ft.Add(Lbl($"📍 {ph}",10,C_BLUE));
            card.Add(ft);if(isOngoing)card.Add(ProgressBar(c.progress,C_OCCUPIED));return card;
        }

        VisualElement Tab_Occupancy(BuildingLiveData live){
            var root=new VisualElement();var o=live.occupancy;var sec=Sec("MẬT ĐỘ");
            Color lc=o.level==OccupancyLevel.High||o.level==OccupancyLevel.Overcrowded?C_OCCUPIED:o.level==OccupancyLevel.Medium?C_UPCOMING:C_EMPTY;
            sec.Add(Lbl($"{o.current_count} / {o.max_capacity} người",24,lc,true));
            sec.Add(Lbl($"{o.density_ratio:P0} capacity  ·  {o.trend}",11,C_DIM));sec.Add(ProgressBar(o.density_ratio,lc));root.Add(sec);return root;}

        VisualElement Tab_Infrastructure(BuildingLiveData live){
            var root=new VisualElement();var inf=live.infrastructure;var sec=Sec("HẠ TẦNG");
            sec.Add(InfraRow("⚡ Điện",$"{inf.electric.current_kw:F1} kW (TB {inf.electric.avg_kw:F1})",inf.electric.is_abnormal?"⚠ Cao":"✓",inf.electric.is_abnormal?C_UPCOMING:C_EMPTY));
            sec.Add(InfraRow("💧 Nước",$"{inf.water.current_lph:F0} lít/h",inf.water.is_abnormal?"⚠":"✓",inf.water.is_abnormal?C_UPCOMING:C_EMPTY));
            sec.Add(InfraRow("🌡 Nhiệt độ",$"{inf.climate.temperature_c:F1}°C · {inf.climate.humidity_pct:F0}%",inf.climate.temperature_c>32?"⚠ Nóng":"✓",inf.climate.temperature_c>32?C_UPCOMING:C_EMPTY));
            sec.Add(InfraRow("❄ Điều hòa",$"{inf.climate.ac_active}/{inf.climate.ac_total}",inf.climate.ac_error>0?$"⚠ {inf.climate.ac_error} lỗi":"✓",inf.climate.ac_error>0?C_UPCOMING:C_EMPTY));
            root.Add(sec);return root;}

        VisualElement Tab_Equipment(BuildingLiveData live){
            var root=new VisualElement();var eq=live.equipment;var sec=Sec("THIẾT BỊ");
            var sr=new VisualElement();sr.style.flexDirection=FlexDirection.Row;sr.style.marginBottom=12;
            sr.Add(MiniStat(eq.active_devices.ToString(),"Hoạt động",C_EMPTY));sr.Add(MiniStat(eq.error_devices.ToString(),"Lỗi",eq.error_devices>0?C_OCCUPIED:C_DIM));sr.Add(MiniStat(eq.total_devices.ToString(),"Tổng",C_BLUE));sec.Add(sr);
            foreach(var err in eq.errors){
                var card=new VisualElement();card.style.paddingTop=card.style.paddingBottom=card.style.paddingLeft=card.style.paddingRight=8;card.style.marginBottom=6;
                card.style.backgroundColor=new StyleColor(new Color(0.15f,0.05f,0.05f));SetBorder(card,new Color(0.5f,0.1f,0.1f),1,6);
                card.Add(Lbl($"❌ {err.device_name}",12,C_OCCUPIED,true));card.Add(Lbl(err.location,10,C_DIM));card.Add(Lbl(err.error_type,11,C_MUTED));card.Add(Lbl(err.reported_at,10,C_DIM));sec.Add(card);}
            if(eq.errors.Count==0)sec.Add(Lbl("✅ Tất cả thiết bị hoạt động bình thường",11,C_EMPTY));root.Add(sec);return root;}

        VisualElement Tab_Events(BuildingLiveData live){
            var root=new VisualElement();var sec=Sec("SỰ KIỆN");
            if(live.events.Count==0)sec.Add(Lbl("Không có sự kiện hôm nay",11,C_DIM));
            else foreach(var ev in live.events){
                var card=new VisualElement();card.style.paddingTop=card.style.paddingBottom=card.style.paddingLeft=card.style.paddingRight=10;card.style.marginBottom=8;
                card.style.backgroundColor=new StyleColor(new Color(0.05f,0.10f,0.18f));SetBorder(card,new Color(0.10f,0.19f,0.31f),1,6);
                card.Add(Lbl($"📅 {ev.event_name}",13,C_BLUE,true));card.Add(Lbl($"{ev.time_start}–{ev.time_end}  ·  {ev.location}",11,C_MUTED));card.Add(Lbl($"{ev.attendee_count} người",10,C_DIM));sec.Add(card);}
            root.Add(sec);return root;}

        VisualElement Tab_Maintenance(BuildingLiveData live){
            var root=new VisualElement();var m=live.maintenance;var sec=Sec("BẢO TRÌ");
            if(!m.has_active_work)sec.Add(Lbl("✅ Không có công việc bảo trì đang thực hiện",11,C_EMPTY));
            else foreach(var t in m.active_tickets){
                var card=new VisualElement();card.style.paddingTop=card.style.paddingBottom=card.style.paddingLeft=card.style.paddingRight=10;card.style.marginBottom=8;
                card.style.backgroundColor=new StyleColor(new Color(0.12f,0.07f,0.02f));SetBorder(card,new Color(0.47f,0.21f,0.06f),1,6);
                card.Add(Lbl($"🔧 {t.title}",13,C_UPCOMING,true));
                if(!string.IsNullOrEmpty(t.affected_area))card.Add(Lbl($"Khu vực: {t.affected_area}",11,C_MUTED));
                card.Add(Lbl($"Từ {t.started_at}  ·  Xong {t.expected_done}",10,C_DIM));sec.Add(card);}
            root.Add(sec);return root;}

        VisualElement BuildTimeline(BuildingLiveData live){
            var section=Sec("TIMELINE HÔM NAY");var track=new VisualElement();track.AddToClassList("timeline-track");
            int dayStart=7*60,dayEnd=21*60,dayLen=dayEnd-dayStart;
            var classes=live.schedule.today_classes?.OrderBy(c=>c.time_start).ToList()??new List<ClassInfo>();
            int nowMin=DUTTime.Now.Hour*60+DUTTime.Now.Minute,prev=dayStart;
            foreach(var cls in classes){
                if(!TryParseTime(cls.time_start,out int sh,out int sm))continue;if(!TryParseTime(cls.time_end,out int eh,out int em))continue;
                int s=sh*60+sm,e=eh*60+em;
                if(s>prev){var gap=new VisualElement();gap.AddToClassList("timeline-block");gap.AddToClassList("timeline-block--empty");gap.style.flexGrow=(float)(s-prev)/dayLen;track.Add(gap);}
                bool on=nowMin>=s&&nowMin<e;var blk=new VisualElement();blk.AddToClassList("timeline-block");blk.AddToClassList(on?"timeline-block--occupied":"timeline-block--upcoming");blk.style.flexGrow=(float)(e-s)/dayLen;track.Add(blk);prev=e;}
            if(prev<dayEnd){var t=new VisualElement();t.AddToClassList("timeline-block");t.AddToClassList("timeline-block--empty");t.style.flexGrow=(float)(dayEnd-prev)/dayLen;track.Add(t);}
            var times=new VisualElement();times.AddToClassList("timeline-time");
            foreach(var t2 in new[]{"07:00","12:00","17:00","21:00"}){var tl=new Label(t2);tl.AddToClassList("timeline-time-label");times.Add(tl);}
            section.Add(track);section.Add(times);return section;}

        // ── Helpers ───────────────────────────────────────────────────────
        List<ClassInfo> GetClassesByTime(BuildingLiveData live, int nowMin, bool ongoing) =>
            live.schedule.today_classes?.Where(c=>{
                TryParseTime(c.time_start,out int sh,out int sm);TryParseTime(c.time_end,out int eh,out int em);
                int s=sh*60+sm,e=eh*60+em;
                return ongoing ? nowMin>=s&&nowMin<e : s>nowMin&&s-nowMin<=90;
            }).ToList()??new List<ClassInfo>();

        static string ExtractPhong(string r){
            if(string.IsNullOrEmpty(r))return"";
            var m=System.Text.RegularExpressions.Regex.Match(r,@"phong=(\S+)");
            if(m.Success)return m.Groups[1].Value;
            if(!r.Contains("="))return r;return"";}

        static bool TryParseTime(string t,out int h,out int m){h=m=0;if(string.IsNullOrEmpty(t))return false;var p=t.Split(':');return p.Length==2&&int.TryParse(p[0],out h)&&int.TryParse(p[1],out m);}

        void Show(string name,bool v){var ve=_root?.Q<VisualElement>(name);if(ve!=null)ve.style.display=v?new StyleEnum<DisplayStyle>(DisplayStyle.Flex):new StyleEnum<DisplayStyle>(DisplayStyle.None);}
        static void SetClass(VisualElement ve,string c1,string c2,string c3,string add){ve.RemoveFromClassList(c1);ve.RemoveFromClassList(c2);ve.RemoveFromClassList(c3);if(!string.IsNullOrEmpty(add))ve.AddToClassList(add);}
        VisualElement Sec(string title){var s=new VisualElement();s.AddToClassList("section");if(!string.IsNullOrEmpty(title)){var t=new Label(title);t.AddToClassList("section__title");s.Add(t);}return s;}
        Label Lbl(string text,int size,Color col,bool bold=false){var l=new Label(text);l.style.fontSize=size;l.style.color=new StyleColor(col);if(bold)l.style.unityFontStyleAndWeight=FontStyle.Bold;l.style.whiteSpace=WhiteSpace.Normal;return l;}
        VisualElement ProgressBar(float val,Color col){var t=new VisualElement();t.AddToClassList("progress-track");var f=new VisualElement();f.AddToClassList("progress-fill");f.style.width=new StyleLength(new Length(Mathf.Clamp01(val)*100f,LengthUnit.Percent));f.style.backgroundColor=new StyleColor(col);t.Add(f);return t;}
        VisualElement InfraRow(string label,string val,string status,Color sc){var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.justifyContent=Justify.SpaceBetween;row.style.marginBottom=10;var left=new VisualElement();left.style.flexDirection=FlexDirection.Column;left.Add(Lbl(label,12,C_MUTED));left.Add(Lbl(val,10,C_DIM));row.Add(left);row.Add(Lbl(status,10,sc,true));return row;}
        VisualElement MiniStat(string num,string label,Color col){var c=new VisualElement();c.style.flexGrow=1;c.style.alignItems=Align.Center;c.Add(Lbl(num,20,col,true));c.Add(Lbl(label,10,C_DIM));return c;}
        VisualElement StatCard(string num,string label,Color col){var c=new VisualElement();c.AddToClassList("stat-card");c.style.flexGrow=1;c.style.marginTop=c.style.marginBottom=c.style.marginLeft=c.style.marginRight=4;var n=new Label(num);n.AddToClassList("stat-card__num");n.style.color=new StyleColor(col);c.Add(n);var l=new Label(label);l.AddToClassList("stat-card__label");c.Add(l);return c;}
        VisualElement AlertRow(AlertData alert){var row=new VisualElement();row.style.flexDirection=FlexDirection.Row;row.style.paddingLeft=row.style.paddingRight=16;row.style.paddingTop=row.style.paddingBottom=6;row.style.borderBottomColor=new StyleColor(new Color(0.10f,0.19f,0.31f));row.style.borderBottomWidth=1;string icon=alert.severity==AlertSeverity.Critical?"🔴":alert.severity==AlertSeverity.Warning?"⚠":"ℹ";Color col2=alert.severity==AlertSeverity.Critical?C_OCCUPIED:alert.severity==AlertSeverity.Warning?C_UPCOMING:C_BLUE;var ic=Lbl(icon,12,col2);ic.style.width=20;row.Add(ic);var txt=new VisualElement();txt.style.flexGrow=1;txt.Add(Lbl(alert.title??",11,C_MUTED));txt.Add(Lbl(alert.building_id??",10,C_DIM));row.Add(txt);return row;}
        void SetBorder(VisualElement ve,Color col,float w,float r){ve.style.borderTopColor=ve.style.borderBottomColor=ve.style.borderLeftColor=ve.style.borderRightColor=new StyleColor(col);ve.style.borderTopWidth=ve.style.borderBottomWidth=ve.style.borderLeftWidth=ve.style.borderRightWidth=w;ve.style.borderTopLeftRadius=ve.style.borderTopRightRadius=ve.style.borderBottomLeftRadius=ve.style.borderBottomRightRadius=r;}
    

VisualElement BuildTimeline_Campus()
{
    var section = Sec("TIMELINE HÔM NAY");
    var track = new VisualElement(); track.AddToClassList("timeline-track");
    int dayStart=7*60, dayEnd=21*60; int nowMin=DUTTime.Now.Hour*60+DUTTime.Now.Minute;
    int slots=(dayEnd-dayStart)/30;
    for (int i=0; i<slots; i++)
    {
        int ss=dayStart+i*30, se=ss+30;
        bool isCurrent=nowMin>=ss&&nowMin<se, hasClass=false;
        if (store?.AllBuildings!=null)
            foreach (var b in store.AllBuildings) {
                var lv=store.GetLiveData(b.building_id);
                if (lv?.schedule.today_classes==null) continue;
                foreach (var cls in lv.schedule.today_classes) {
                    if (!TryParseTime(cls.time_start,out int sh,out int sm)) continue;
                    if (!TryParseTime(cls.time_end,out int eh,out int em)) continue;
                    if (sh*60+sm < se && eh*60+em > ss) { hasClass=true; break; }
                }
                if (hasClass) break;
            }
        var blk=new VisualElement(); blk.AddToClassList("timeline-block");
        blk.AddToClassList(hasClass?(isCurrent?"timeline-block--occupied":"timeline-block--upcoming"):"timeline-block--empty");
        blk.style.flexGrow=1; track.Add(blk);
    }
    var times=new VisualElement(); times.AddToClassList("timeline-time");
    foreach (var t in new[]{"07:00","12:00","17:00","21:00"}) { var tl=new Label(t); tl.AddToClassList("timeline-time-label"); times.Add(tl); }
    section.Add(track); section.Add(times); return section;
}


VisualElement MakeAlertTag(string text, Color col)
{
    var tag = new Label(text);
    tag.style.fontSize = 10; tag.style.color = new StyleColor(col);
    tag.style.paddingLeft = tag.style.paddingRight = 6; tag.style.paddingTop = tag.style.paddingBottom = 2;
    tag.style.marginRight = 4; tag.style.marginBottom = 2;
    tag.style.backgroundColor = new StyleColor(new Color(col.r*0.15f, col.g*0.15f, col.b*0.15f, 0.8f));
    tag.style.borderTopLeftRadius = tag.style.borderTopRightRadius = tag.style.borderBottomLeftRadius = tag.style.borderBottomRightRadius = 4;
    tag.style.borderTopWidth = tag.style.borderBottomWidth = tag.style.borderLeftWidth = tag.style.borderRightWidth = 1;
    tag.style.borderTopColor = tag.style.borderBottomColor = tag.style.borderLeftColor = tag.style.borderRightColor = new StyleColor(new Color(col.r*0.4f, col.g*0.4f, col.b*0.4f));
    return tag;
}


VisualElement BuildSection_BuildingCard(BuildingInfo info, BuildingLiveData live)
{
    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    var ongoing  = GetClassesByTime(live, nowMin, true);
    var upcoming = GetClassesByTime(live, nowMin, false);
    var s = live.schedule.status;
    Color statusCol = s==BuildingStatus.Occupied?C_OCCUPIED:s==BuildingStatus.Upcoming?C_UPCOMING:C_EMPTY;
    string statusTxt = s==BuildingStatus.Occupied?$"{ongoing.Count} lớp đang học":s==BuildingStatus.Upcoming?$"{upcoming.Count} lớp sắp bắt đầu":"Không có lớp";
    var card = new VisualElement();
    card.style.marginLeft = card.style.marginRight = 12; card.style.marginTop = 8; card.style.marginBottom = 4;
    card.style.paddingTop = card.style.paddingBottom = 10; card.style.paddingLeft = card.style.paddingRight = 12;
    card.style.backgroundColor = new StyleColor(new Color(0.05f, 0.12f, 0.20f));
    SetBorder(card, new Color(0.12f, 0.27f, 0.48f), 1, 8);
    card.Add(Lbl(statusTxt, 13, statusCol, true));
    foreach (var cls in ongoing.Take(3))
    {
        var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.marginTop = 5;
        var nm = Lbl(cls.class_name, 10, C_MUTED); nm.style.flexGrow = 1; nm.style.whiteSpace = WhiteSpace.Normal; row.Add(nm);
        string ph = ExtractPhong(cls.room_id);
        if (!string.IsNullOrEmpty(ph)) { var pl = Lbl(ph, 10, C_BLUE); pl.style.marginLeft = 4; row.Add(pl); }
        card.Add(row);
    }
    if (ongoing.Count > 3) card.Add(Lbl($"  + {ongoing.Count-3} phòng nữa...", 10, C_DIM));
    bool hasInfra = live.infrastructure.electric.is_abnormal || live.infrastructure.climate.ac_error > 0;
    bool hasEquip = live.equipment.error_devices > 0;
    if (hasInfra || hasEquip)
    {
        var ar = new VisualElement(); ar.style.flexDirection = FlexDirection.Row; ar.style.marginTop = 8; ar.style.flexWrap = Wrap.Wrap;
        if (live.infrastructure.electric.is_abnormal) ar.Add(MakeAlertTag("⚡ Điện", C_UPCOMING));
        if (live.infrastructure.climate.ac_error > 0) ar.Add(MakeAlertTag($"❄ {live.infrastructure.climate.ac_error} AC lỗi", C_UPCOMING));
        if (live.equipment.has_critical_error) ar.Add(MakeAlertTag($"❌ {live.equipment.error_devices} thiết bị", C_OCCUPIED));
        else if (hasEquip) ar.Add(MakeAlertTag($"⚠ {live.equipment.error_devices} thiết bị", C_UPCOMING));
        card.Add(ar);
    }
    return card;
}


VisualElement BuildSection_AlertSummary()
{
    var alerts = AlertManager.Instance?.Alerts?.ToList() ?? new System.Collections.Generic.List<AlertData>();
    int count = alerts.Count;
    var sec = Sec(count > 0 ? $"CẢNH BÁO ({count})" : "CẢNH BÁO");
    if (count == 0) { sec.Add(Lbl("✅ Hệ thống hoạt động bình thường", 11, C_EMPTY)); return sec; }
    foreach (var a in alerts.Take(5))
    {
        Color sevCol = a.severity == AlertSeverity.Critical ? C_OCCUPIED : a.severity == AlertSeverity.Warning ? C_UPCOMING : C_BLUE;
        string icon  = a.severity == AlertSeverity.Critical ? "🔴" : a.severity == AlertSeverity.Warning ? "⚠" : "ℹ";
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row; row.style.paddingTop = row.style.paddingBottom = 5;
        row.style.borderBottomWidth = 1; row.style.borderBottomColor = new StyleColor(new Color(0.10f, 0.19f, 0.31f));
        row.style.alignItems = Align.Center;
        var ic = Lbl(icon, 11, sevCol); ic.style.width = 18; ic.style.flexShrink = 0; row.Add(ic);
        var txt = Lbl(a.title ?? "–", 11, C_MUTED); txt.style.flexGrow = 1; txt.style.whiteSpace = WhiteSpace.Normal; row.Add(txt);
        row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new StyleColor(new Color(0.08f, 0.15f, 0.24f)));
        row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = new StyleColor(Color.clear));
        string bid = a.building_id;
        row.RegisterCallback<ClickEvent>(_ => { if (bid != null && store != null) store.SelectBuilding(bid); });
        sec.Add(row);
    }
    if (count > 5) { var more = Lbl($"+ {count-5} cảnh báo khác...", 10, C_DIM); more.style.paddingTop = 6; sec.Add(more); }
    return sec;
}


VisualElement BuildSection_KhuBreakdown()
{
    var sec = Sec("THEO KHU");
    if (store?.AllBuildings == null) return sec;
    var khus = store.AllBuildings.Select(b => b.ten_khu).Distinct().OrderBy(k => k).ToList();
    foreach (var khu in khus)
    {
        var buildings = store.GetBuildingsInKhu(khu);
        int total = buildings.Count; if (total == 0) continue;
        int occK = buildings.Count(b => store.GetStatus(b.building_id) == BuildingStatus.Occupied);
        int upcK = buildings.Count(b => store.GetStatus(b.building_id) == BuildingStatus.Upcoming);
        float ratio = (float)occK / total;
        Color barCol = occK > 0 ? C_OCCUPIED : upcK > 0 ? C_UPCOMING : C_DIM;
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row; row.style.alignItems = Align.Center; row.style.marginBottom = 6;
        string capturedKhu = khu;
        var khuBtn = new Button(() => store.SelectKhu(capturedKhu));
        khuBtn.text = $"Khu {khu}"; khuBtn.style.width = 56; khuBtn.style.height = 22; khuBtn.style.fontSize = 11;
        khuBtn.style.color = new StyleColor(C_MUTED); khuBtn.style.backgroundColor = new StyleColor(Color.clear);
        khuBtn.style.borderTopWidth = khuBtn.style.borderBottomWidth = khuBtn.style.borderLeftWidth = khuBtn.style.borderRightWidth = 0;
        khuBtn.style.paddingLeft = khuBtn.style.paddingRight = 0; khuBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
        row.Add(khuBtn);
        var track = new VisualElement();
        track.style.flexGrow = 1; track.style.height = 5; track.style.marginLeft = 4;
        track.style.borderTopLeftRadius = track.style.borderTopRightRadius = track.style.borderBottomLeftRadius = track.style.borderBottomRightRadius = 3;
        track.style.backgroundColor = new StyleColor(new Color(0.10f, 0.19f, 0.31f));
        var fill = new VisualElement(); fill.style.height = 5;
        fill.style.width = new StyleLength(new Length(Mathf.Clamp01(ratio)*100f, LengthUnit.Percent));
        fill.style.backgroundColor = new StyleColor(barCol);
        fill.style.borderTopLeftRadius = fill.style.borderTopRightRadius = fill.style.borderBottomLeftRadius = fill.style.borderBottomRightRadius = 3;
        track.Add(fill); row.Add(track);
        string cntTxt = occK > 0 ? $"{occK}/{total}" : upcK > 0 ? $"{upcK}/{total}" : $"0/{total}";
        var cnt = Lbl(cntTxt, 10, barCol); cnt.style.width = 30; cnt.style.marginLeft = 6; cnt.style.unityTextAlign = TextAnchor.MiddleRight;
        row.Add(cnt);
        row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new StyleColor(new Color(0.08f, 0.15f, 0.24f)));
        row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = new StyleColor(Color.clear));
        sec.Add(row);
    }
    return sec;
}


VisualElement BuildSection_KpiCards(int occ, int emp, int upc, int tot)
{
    var sec = new VisualElement(); sec.AddToClassList("section");
    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap; row.style.paddingBottom = 4;
    row.Add(StatCard(occ.ToString(), "CÓ LỚP", C_OCCUPIED));
    row.Add(StatCard(emp.ToString(), "TRỐNG",   C_EMPTY));
    row.Add(StatCard(upc.ToString(), "SẮP CÓ",  C_UPCOMING));
    row.Add(StatCard(tot.ToString(), "TỔNG TÒA",C_BLUE));
    sec.Add(row); return sec;
}


// ── Left Panel ──────────────────────────────────────────────────────────
void BuildLeftPanel(int occ, int emp, int upc, int tot)
{
    if (_leftPanelBody == null) return;
    _leftPanelBody.Clear();

    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    int totalRooms = 0, roomsOcc = 0, studentsNow = 0;
    float sumTemp = 0f; int tempCount = 0;
    int acAct = 0, acTot = 0, equipErr = 0, evCount = 0, maintCount = 0;

    if (store?.AllBuildings != null)
        foreach (var b in store.AllBuildings) {
            totalRooms += b.so_phong;
            var lv = store.GetLiveData(b.building_id); if (lv == null) continue;
            var on = GetClassesByTime(lv, nowMin, true);
            roomsOcc    += on.Count;
            studentsNow += on.Sum(c => c.student_count);
            float t = lv.infrastructure.climate.temperature_c;
            if (t > 0) { sumTemp += t; tempCount++; }
            acAct   += lv.infrastructure.climate.ac_active;
            acTot   += lv.infrastructure.climate.ac_total;
            equipErr += lv.equipment.error_devices;
            if (lv.events?.Count > 0) evCount++;
            if (lv.maintenance.has_active_work) maintCount++;
        }

    float avgTemp   = tempCount > 0 ? sumTemp / tempCount : 0f;
    int roomsEmpty  = Mathf.Max(0, totalRooms - roomsOcc);
    int roomPct     = totalRooms > 0 ? Mathf.Clamp(roomsOcc * 100 / totalRooms, 0, 100) : 0;

    _leftPanelBody.Add(BuildLP_OccupancyRing(roomsOcc, roomsEmpty, roomPct, totalRooms));
    _leftPanelBody.Add(BuildLP_StudentFlow(studentsNow));
    _leftPanelBody.Add(BuildLP_HourlyBars());
    _leftPanelBody.Add(BuildLP_PowerCard());
    _leftPanelBody.Add(BuildLP_QuickStats(avgTemp, acAct, acTot, equipErr, evCount, maintCount));
}

VisualElement LpCard(string title, out VisualElement body)
{
    var card = new VisualElement(); card.AddToClassList("lp-card");
    var t = new Label(title); t.AddToClassList("lp-card__title"); card.Add(t);
    body = new VisualElement(); body.AddToClassList("lp-card__body"); card.Add(body);
    return card;
}

VisualElement BuildLP_OccupancyRing(int roomsOcc, int roomsEmpty, int roomPct, int totalRooms)
{
    var card = LpCard("PHÒNG ĐANG CÓ LỚP", out var body);
    var wrap = new VisualElement(); wrap.AddToClassList("lp-ring-wrap");

    var ring = new VisualElement(); ring.AddToClassList("lp-ring");
    Color rc = roomPct > 60 ? C_OCCUPIED : roomPct > 30 ? C_UPCOMING : C_BLUE;
    ring.style.borderTopColor = ring.style.borderBottomColor =
    ring.style.borderLeftColor = ring.style.borderRightColor = new StyleColor(rc);
    var pctLbl = new Label($"{roomPct}%"); pctLbl.AddToClassList("lp-ring__pct"); ring.Add(pctLbl);
    wrap.Add(ring);

    var right = new VisualElement(); right.style.flexGrow = 1;
    var bigNum = Lbl(roomsOcc.ToString(), 28, C_BLUE, true);
    right.Add(bigNum);
    right.Add(Lbl($"/ {totalRooms} phòng  ·  {roomsEmpty} trống", 9, C_DIM));
    wrap.Add(right); body.Add(wrap);
    return card;
}

VisualElement BuildLP_StudentFlow(int studentsNow)
{
    Color accentCol = studentsNow > 500 ? C_OCCUPIED : studentsNow > 200 ? C_UPCOMING : C_BLUE;
    var card = LpCard("LƯU LƯỢNG SINH VIÊN", out var body);

    // Big number header
    var hdr = new VisualElement(); hdr.style.flexDirection = FlexDirection.Row;
    hdr.style.alignItems = Align.FlexEnd; hdr.style.marginBottom = 5;
    var bigNum = Lbl(studentsNow.ToString(), 26, accentCol, true);
    hdr.Add(bigNum);
    var sub = Lbl("  SV hiện tại", 9, C_DIM); sub.style.marginBottom = 2; hdr.Add(sub);
    body.Add(hdr);

    // Hourly student count bars (07h-21h)
    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    int dayStart = 7 * 60, slots = 14; int maxSt = 1;
    var stCounts = new int[slots];
    if (store?.AllBuildings != null)
        for (int i = 0; i < slots; i++) {
            int ss = dayStart + i * 60, se = ss + 60;
            foreach (var b in store.AllBuildings) {
                var lv = store.GetLiveData(b.building_id);
                if (lv?.schedule.today_classes == null) continue;
                foreach (var cls in lv.schedule.today_classes) {
                    if (!TryParseTime(cls.time_start, out int sh, out int sm)) continue;
                    if (!TryParseTime(cls.time_end,   out int eh, out int em)) continue;
                    if (sh*60+sm < se && eh*60+em > ss) stCounts[i] += cls.student_count;
                }
            }
            if (stCounts[i] > maxSt) maxSt = stCounts[i];
        }

    var chart = new VisualElement(); chart.AddToClassList("lp-chart");
    for (int i = 0; i < slots; i++) {
        int ss = dayStart + i * 60;
        bool cur  = nowMin >= ss && nowMin < ss + 60;
        bool past = nowMin >= ss + 60;
        int barH = Mathf.Max(3, Mathf.RoundToInt((float)stCounts[i] / maxSt * 48));
        var bar = new VisualElement(); bar.AddToClassList("lp-bar");
        Color bc = cur ? C_BLUE : past ? new Color(0.18f, 0.56f, 0.91f, 0.55f) : new Color(0.10f, 0.27f, 0.51f, 0.55f);
        bar.style.backgroundColor = new StyleColor(bc);
        bar.style.height = barH; chart.Add(bar);
    }
    body.Add(chart);
    var lblRow = new VisualElement(); lblRow.AddToClassList("lp-chart-labels");
    foreach (var t in new[] { "7h", "10h", "13h", "16h", "19h", "21h" })
    { var l = new Label(t); l.AddToClassList("lp-chart-label"); lblRow.Add(l); }
    body.Add(lblRow);
    return card;
}

VisualElement BuildLP_HourlyBars()
{
    var card = LpCard("PHÒNG CÓ LỚP THEO GIỜ", out var body);
    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    int dayStart = 7 * 60, slots = 14; // 07h-21h, 1h each
    int maxCount = 1;
    var counts = new int[slots];

    if (store?.AllBuildings != null)
        for (int i = 0; i < slots; i++) {
            int ss = dayStart + i * 60, se = ss + 60;
            foreach (var b in store.AllBuildings) {
                var lv = store.GetLiveData(b.building_id);
                if (lv?.schedule.today_classes == null) continue;
                foreach (var cls in lv.schedule.today_classes) {
                    if (!TryParseTime(cls.time_start, out int sh, out int sm)) continue;
                    if (!TryParseTime(cls.time_end,   out int eh, out int em)) continue;
                    if (sh*60+sm < se && eh*60+em > ss) { counts[i]++; break; }
                }
            }
            if (counts[i] > maxCount) maxCount = counts[i];
        }

    var chart = new VisualElement(); chart.AddToClassList("lp-chart");
    for (int i = 0; i < slots; i++) {
        int ss = dayStart + i * 60;
        bool cur = nowMin >= ss && nowMin < ss + 60;
        float ratio = (float)counts[i] / maxCount;
        int barH = Mathf.Max(3, Mathf.RoundToInt(ratio * 48));
        var bar = new VisualElement(); bar.AddToClassList("lp-bar");
        Color bc = cur                ? C_BLUE
                 : ratio > 0.6f       ? new Color(0.94f, 0.27f, 0.27f, 0.85f)
                 : counts[i] > 0      ? new Color(0.96f, 0.62f, 0.04f, 0.65f)
                 :                      new Color(0.12f, 0.20f, 0.27f, 0.45f);
        bar.style.backgroundColor = new StyleColor(bc);
        bar.style.height = barH; chart.Add(bar);
    }
    body.Add(chart);

    var lblRow = new VisualElement(); lblRow.AddToClassList("lp-chart-labels");
    foreach (var t in new[] { "7h", "10h", "13h", "16h", "19h", "21h" })
    { var l = new Label(t); l.AddToClassList("lp-chart-label"); lblRow.Add(l); }
    body.Add(lblRow);

    // Color legend
    var lgnd = new VisualElement(); lgnd.style.flexDirection = FlexDirection.Row;
    lgnd.style.marginTop = 5; lgnd.style.flexWrap = Wrap.Wrap;
    void LItem(Color c, string lbl) {
        var item = new VisualElement(); item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center; item.style.marginRight = 8;
        var dot = new VisualElement(); dot.style.width = 7; dot.style.height = 7;
        dot.style.borderTopLeftRadius = dot.style.borderTopRightRadius = 1;
        dot.style.borderBottomLeftRadius = dot.style.borderBottomRightRadius = 1;
        dot.style.backgroundColor = new StyleColor(c); dot.style.marginRight = 3;
        item.Add(dot); item.Add(Lbl(lbl, 8, C_DIM)); lgnd.Add(item);
    }
    LItem(new Color(0.94f, 0.27f, 0.27f), "Cao điểm");
    LItem(new Color(0.96f, 0.62f, 0.04f), "Bình thường");
    LItem(C_BLUE, "Hiện tại");
    body.Add(lgnd);
    return card;
}

VisualElement BuildLP_PowerCard()
{
    var card = LpCard("TIÊU THỤ ĐIỆN", out var body);
    float totalKw = 0f, totalAvg = 0f; int abnormal = 0;
    if (store?.AllBuildings != null)
        foreach (var b in store.AllBuildings) {
            var lv = store.GetLiveData(b.building_id); if (lv == null) continue;
            totalKw  += lv.infrastructure.electric.current_kw;
            totalAvg += lv.infrastructure.electric.avg_kw;
            if (lv.infrastructure.electric.is_abnormal) abnormal++;
        }
    Color pwCol = abnormal > 0 ? C_UPCOMING : C_EMPTY;
    body.Add(Lbl($"{totalKw:F0} kW", 22, pwCol, true));
    string subTxt = abnormal > 0 ? $"⚠ {abnormal} bất thường  ·  TB {totalAvg:F0} kW"
                                 : $"✓ Bình thường  ·  TB {totalAvg:F0} kW";
    body.Add(Lbl(subTxt, 10, C_DIM));
    if (totalAvg > 0)
        body.Add(ProgressBar(Mathf.Clamp01(totalKw / (totalAvg * 1.5f)), pwCol));
    return card;
}

VisualElement BuildLP_QuickStats(float avgTemp, int acAct, int acTot, int equipErr, int evCount, int maintCount)
{
    var card = LpCard("THỐNG KÊ TỨC THỜI", out var body);
    void SRow(string icon, string label, string val, bool bad = false) {
        var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center; row.style.justifyContent = Justify.SpaceBetween;
        row.style.paddingTop = row.style.paddingBottom = 5;
        row.style.borderBottomWidth = 1;
        row.style.borderBottomColor = new StyleColor(new Color(0.10f, 0.19f, 0.31f, 0.6f));
        var left = new VisualElement(); left.style.flexDirection = FlexDirection.Row;
        left.style.alignItems = Align.Center;
        var ic = new Label(icon); ic.style.fontSize = 12; ic.style.marginRight = 6; left.Add(ic);
        left.Add(Lbl(label, 10, C_MUTED));
        row.Add(left);
        row.Add(Lbl(val, 12, bad ? C_OCCUPIED : C_BLUE, true));
        body.Add(row);
    }
    SRow("🌡", "Nhiệt độ TB",     avgTemp > 0 ? $"{avgTemp:F1}°C" : "—", avgTemp > 32f);
    SRow("❄", "Điều hòa",         acTot > 0 ? $"{acAct} / {acTot}" : "—", acTot > 0 && acAct < acTot * 0.6f);
    SRow("🖥", "Thiết bị lỗi",    equipErr.ToString(), equipErr > 0);
    SRow("📅", "Sự kiện hôm nay", evCount.ToString());
    SRow("🔧", "Đang bảo trì",    maintCount.ToString(), maintCount > 0);
    return card;
}

// ── Room List Tab ────────────────────────────────────────────────────────
VisualElement Tab_RoomList(BuildingInfo info, BuildingLiveData live)
{
    var root = new VisualElement();
    int nowMin = DUTTime.Now.Hour * 60 + DUTTime.Now.Minute;
    var all = live.schedule.today_classes ?? new List<ClassInfo>();
    var roomMap = new Dictionary<string, (string className, BuildingStatus status)>();

    foreach (var cls in all) {
        string rid = ExtractPhong(cls.room_id); if (string.IsNullOrEmpty(rid)) continue;
        if (!TryParseTime(cls.time_start, out int sh, out int sm)) continue;
        if (!TryParseTime(cls.time_end,   out int eh, out int em)) continue;
        int s = sh*60+sm, e = eh*60+em;
        BuildingStatus st = nowMin>=s&&nowMin<e ? BuildingStatus.Occupied
                          : s>nowMin&&s-nowMin<=90 ? BuildingStatus.Upcoming
                          : BuildingStatus.Empty;
        if (!roomMap.ContainsKey(rid) || (int)st > (int)roomMap[rid].status)
            roomMap[rid] = (cls.class_name, st);
    }

    if (roomMap.Count == 0) {
        var empty = new VisualElement(); empty.AddToClassList("section");
        empty.Add(Lbl("Không có dữ liệu phòng hôm nay", 11, C_DIM)); root.Add(empty);
        return root;
    }

    var byFloor = roomMap.GroupBy(kv => ExtractFloor(kv.Key)).OrderBy(g => g.Key).ToList();
    foreach (var group in byFloor) {
        var hdr = new VisualElement(); hdr.AddToClassList("room-floor-header");
        var fl = new Label(group.Key > 0 ? $"TẦNG {group.Key}" : "PHÒNG"); fl.AddToClassList("room-floor-label"); hdr.Add(fl);
        var cnt = new Label($"{group.Count()} phòng"); cnt.style.fontSize = 9; cnt.style.color = new StyleColor(C_DIM); hdr.Add(cnt);
        root.Add(hdr);

        foreach (var kv in group.OrderBy(x => x.Key)) {
            var (className, st) = kv.Value;
            Color dc = st==BuildingStatus.Occupied?C_OCCUPIED:st==BuildingStatus.Upcoming?C_UPCOMING:C_EMPTY;
            var row = new VisualElement(); row.AddToClassList("room-card");
            var idLbl = new Label(kv.Key); idLbl.AddToClassList("room-card__id"); row.Add(idLbl);
            var inf = new VisualElement(); inf.AddToClassList("room-card__info");
            if (st != BuildingStatus.Empty) {
                var nm = new Label(className); nm.AddToClassList("room-card__name"); inf.Add(nm);
                var sub = new Label(st==BuildingStatus.Occupied?"Đang có lớp":"Sắp có lớp");
                sub.AddToClassList("room-card__sub"); sub.style.color = new StyleColor(dc); inf.Add(sub);
            } else {
                var sub = new Label("Phòng trống"); sub.AddToClassList("room-card__sub"); inf.Add(sub);
            }
            row.Add(inf);
            var dot = new VisualElement(); dot.AddToClassList("room-card__dot");
            dot.style.backgroundColor = new StyleColor(dc); row.Add(dot);
            root.Add(row);
        }
    }
    return root;
}

static int ExtractFloor(string roomId)
{
    if (string.IsNullOrEmpty(roomId)) return 0;
    var m = System.Text.RegularExpressions.Regex.Match(roomId, @"[A-Za-z]+(\d)");
    return m.Success ? int.Parse(m.Groups[1].Value) : 0;
}

void UpdateTopbarKpi(int occ, int alertCount)
{
    var kpiOcc = _root?.Q<Label>("kpi-occupied");
    if (kpiOcc != null) kpiOcc.text = $"{occ} có lớp";
    var kpiAlert = _root?.Q<Label>("kpi-alerts");
    if (kpiAlert == null) return;
    bool hasCritical = AlertManager.Instance?.Alerts?.Any(a => a.severity == AlertSeverity.Critical) == true;
    kpiAlert.RemoveFromClassList("kpi-chip--ok");
    kpiAlert.RemoveFromClassList("kpi-chip--alert");
    kpiAlert.RemoveFromClassList("kpi-chip--critical");
    if (alertCount == 0) { kpiAlert.text = "✅ OK"; kpiAlert.AddToClassList("kpi-chip--ok"); }
    else if (hasCritical) { kpiAlert.text = $"{alertCount} ⚠ CRITICAL"; kpiAlert.AddToClassList("kpi-chip--critical"); }
    else { kpiAlert.text = $"{alertCount} ⚠"; kpiAlert.AddToClassList("kpi-chip--alert"); }
}
}
}