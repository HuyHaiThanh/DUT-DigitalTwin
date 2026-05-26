using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DUT.Data;

namespace DUT.UI
{
    /// <summary>
    /// Quản lý toàn bộ navigation giữa 3 màn hình:
    /// Campus3D (DUT_Main.uxml), Dashboard (DUT_Dashboard.uxml), Schedule (DUT_Schedule.uxml)
    /// và populate data thực cho Dashboard + Schedule.
    /// </summary>
    public class ScreenManager : MonoBehaviour
    {
        public static ScreenManager Instance { get; private set; }

        [Header("UIDocuments")]
        public UIDocument docMain;
        public UIDocument docDashboard;
        public UIDocument docSchedule;

        [Header("Data")]
        public BuildingDataStore store;

        public enum Screen { Campus3D, Dashboard, Schedule }
        Screen _current = Screen.Campus3D;

        // ── Tiết DUT — giờ bắt đầu chính xác (10 phút nghỉ, nghỉ trưa 40 phút) ──
        // Sáng:  1=7:00  2=8:00  3=9:00  4=10:00  5=11:00
        // Chiều: 6=12:30 7=13:30 8=14:30 9=15:30  10=16:30
        // Tối:  11=17:30 12=18:30 13=19:30 14=20:30
        static readonly int[] TietHour = { 0,  7,  8,  9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        static readonly int[] TietMin  = { 0,  0,  0,  0,  0,  0, 30, 30, 30, 30, 30, 30, 30, 30, 30 };

        void Awake() => Instance = this;

        void Start()
        {
            if (store == null) store = Resources.Load<BuildingDataStore>("BuildingDataStore");
            if (docMain)      docMain.gameObject.SetActive(true);
            if (docDashboard) docDashboard.gameObject.SetActive(false);
            if (docSchedule)  docSchedule.gameObject.SetActive(false);
            StartCoroutine(Co_BindAfterFrame(docMain?.rootVisualElement));
            StartCoroutine(Co_TickDatetime());
        }

        System.Collections.IEnumerator Co_BindAfterFrame(UnityEngine.UIElements.VisualElement root)
        {
            yield return null;
            BindNav(root);
            UpdateDatetime(root);
        }

        System.Collections.IEnumerator Co_ActivateScreen(
            UnityEngine.UIElements.UIDocument doc, bool isDash, bool isSched)
        {
            yield return null;
            var root = doc?.rootVisualElement;
            BindNav(root);
            UpdateDatetime(root);
            if (isDash)  yield return StartCoroutine(Co_PopulateDashboard());
            if (isSched) yield return StartCoroutine(Co_PopulateSchedule());
        }

        IEnumerator Co_TickDatetime()
        {
            while (true)
            {
                yield return new WaitForSeconds(30f);
                var activeDoc = _current == Screen.Campus3D   ? docMain
                              : _current == Screen.Dashboard  ? docDashboard
                              : docSchedule;
                var root = activeDoc?.rootVisualElement;
                if (root != null) UpdateDatetime(root);
            }
        }

        // ── Nav binding (mỗi UXML có topbar nav riêng) ───────────────
        void BindNavAll() { } // deprecated — now bound lazily per ShowScreen()

        void BindNav(VisualElement root)
        {
            if (root == null) return;
            root.Q<Button>("nav-3d")        ?.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Campus3D));
            root.Q<Button>("nav-dashboard") ?.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Dashboard));
            root.Q<Button>("nav-schedule")  ?.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Schedule));

            // Fallback: buttons phân biệt qua text (DUT_Dashboard/Schedule dùng class không name)
            foreach (var btn in root.Query<Button>().ToList())
            {
                if (btn.text == "Campus 3D")  btn.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Campus3D));
                if (btn.text == "Dashboard")  btn.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Dashboard));
                if (btn.text == "Lịch học")   btn.RegisterCallback<ClickEvent>(_ => ShowScreen(Screen.Schedule));
            }
        }

        public void ShowScreen(Screen s)
        {
            _current = s;
            bool is3D   = s == Screen.Campus3D;
            bool isDash  = s == Screen.Dashboard;
            bool isSched = s == Screen.Schedule;

            if (docMain)      docMain.gameObject.SetActive(is3D);
            if (docDashboard) docDashboard.gameObject.SetActive(isDash);
            if (docSchedule)  docSchedule.gameObject.SetActive(isSched);

            // Bind nav sau 1 frame (UIDocument cần 1 frame để activate)
            var activeDoc = is3D ? docMain : isDash ? docDashboard : docSchedule;
            StartCoroutine(Co_ActivateScreen(activeDoc, isDash, isSched));
        }

        // ═══════════════════════════════════════════════════════════════
        // DASHBOARD v2
        // ═══════════════════════════════════════════════════════════════
        IEnumerator Co_PopulateDashboard()
        {
            yield return null;
            var root = docDashboard?.rootVisualElement;
            if (root == null || store == null) yield break;

            var buildings = store.AllBuildings;
            int total = buildings.Count;
            var now = DUT.UI.DUTTime.Now;
            int nowMin = now.Hour * 60 + now.Minute;
            int nowH   = now.Hour;

            // ── Aggregate stats ──────────────────────────────────────
            int roomsTotal = 0, roomsOcc = 0, studentsNow = 0, towsOcc = 0, towsUpc = 0;
            foreach (var b in buildings)
            {
                roomsTotal += b.so_phong;
                var status = store.GetStatus(b.building_id);
                if (status == BuildingStatus.Occupied) towsOcc++;
                else if (status == BuildingStatus.Upcoming) towsUpc++;
                var live = store.GetLiveData(b.building_id);
                if (live?.schedule?.today_classes == null) continue;
                foreach (var cls in live.schedule.today_classes)
                {
                    TryParseTime(cls.time_start, out int sh, out int sm);
                    TryParseTime(cls.time_end,   out int eh, out int em);
                    int sMin = sh*60+sm, eMin = eh*60+em;
                    if (nowMin >= sMin && nowMin < eMin) { roomsOcc++; studentsNow += cls.student_count; }
                }
            }
            int roomsEmpty = Mathf.Max(0, roomsTotal - roomsOcc);

            // ── 6 KPI chips ─────────────────────────────────────────
            root.Q<Label>("kpi-val-0")?.SetText(roomsOcc.ToString());
            root.Q<Label>("kpi-sub-0")?.SetText($"/ {roomsTotal} tổng");
            root.Q<Label>("kpi-val-1")?.SetText(roomsEmpty.ToString());
            root.Q<Label>("kpi-sub-1")?.SetText("Sẵn sàng sử dụng");
            root.Q<Label>("kpi-val-2")?.SetText(studentsNow.ToString());
            root.Q<Label>("kpi-sub-2")?.SetText("Hiện tại");
            root.Q<Label>("kpi-val-3")?.SetText($"{towsOcc}/{total}");
            root.Q<Label>("kpi-sub-3")?.SetText($"{towsUpc} sắp có");
            int totalSlots = buildings.Sum(b => store.GetLiveData(b.building_id)?.schedule?.today_classes?.Count ?? 0);
            root.Q<Label>("kpi-val-4")?.SetText(totalSlots.ToString());
            root.Q<Label>("kpi-sub-4")?.SetText("Slots lịch hôm nay");

            // Alert count from live data
            int alertCount = 0;
            var alertItems = new List<(string title, AlertSeverity sev)>();
            foreach (var b in buildings)
            {
                var live = store.GetLiveData(b.building_id);
                if (live == null) continue;
                foreach (var ticket in live.maintenance.active_tickets)
                { alertItems.Add(($"🔧 {b.ten_ngan}: {ticket.title}", ticket.severity)); alertCount++; }
                if (live.equipment.has_critical_error)
                { alertItems.Add(($"🖥 {b.ten_ngan}: Thiết bị lỗi", AlertSeverity.Critical)); alertCount++; }
                if (live.infrastructure.has_alert)
                { alertItems.Add(($"⚡ {b.ten_ngan}: Cảnh báo hạ tầng", AlertSeverity.Warning)); alertCount++; }
            }
            root.Q<Label>("kpi-val-5")?.SetText(alertCount > 0 ? alertCount.ToString() : "0");
            root.Q<Label>("kpi-sub-5")?.SetText(alertCount > 0 ? "Cần xử lý" : "OK");
            var kv5 = root.Q<Label>("kpi-val-5");
            if (kv5 != null)
            {
                kv5.RemoveFromClassList("dash-kpi--ok"); kv5.RemoveFromClassList("dash-kpi--alert");
                kv5.AddToClassList(alertCount > 0 ? "dash-kpi--alert" : "dash-kpi--ok");
            }

            // ── Zone breakdown ──────────────────────────────────────
            var zoneList = root.Q<VisualElement>("dash-zone-list");
            if (zoneList != null)
            {
                zoneList.Clear();
                var zones = buildings.Select(b => b.ten_khu).Where(z => !string.IsNullOrEmpty(z))
                            .Distinct().OrderBy(z => z).ToList();
                foreach (var zone in zones)
                {
                    var zb = store.GetBuildingsInKhu(zone);
                    if (zb.Count == 0) continue;
                    int zt = zb.Sum(b => b.so_phong), zo = 0;
                    foreach (var b in zb)
                    {
                        var live = store.GetLiveData(b.building_id);
                        if (live?.schedule?.today_classes == null) continue;
                        foreach (var cls in live.schedule.today_classes)
                        {
                            TryParseTime(cls.time_start, out int sh, out int sm);
                            TryParseTime(cls.time_end,   out int eh, out int em);
                            if (nowMin >= sh*60+sm && nowMin < eh*60+em) zo++;
                        }
                    }
                    int zpct = zt > 0 ? Mathf.Clamp(zo*100/zt, 0, 100) : 0;

                    var row = new VisualElement(); row.AddToClassList("dash-zone-row");
                    var hdr = new VisualElement(); hdr.AddToClassList("dash-zone-header");
                    var nm  = new Label($"Khu {zone}"); nm.AddToClassList("dash-zone-name");
                    var cnt = new Label($"{zo}/{zt} phòng"); cnt.AddToClassList("dash-zone-count");
                    if (zo > 0) cnt.AddToClassList("dash-zone-count--active");
                    hdr.Add(nm); hdr.Add(cnt);
                    var track = new VisualElement(); track.AddToClassList("dash-bar-track");
                    var fill  = new VisualElement(); fill.AddToClassList("dash-bar-fill");
                    fill.AddToClassList(zpct > 60 ? "dash-bar-fill--peak" : zpct > 30 ? "dash-bar-fill--mid" : "dash-bar-fill--low");
                    fill.style.width = new StyleLength(new Length(zpct, LengthUnit.Percent));
                    track.Add(fill);
                    var pctL = new Label($"{zpct}% sử dụng"); pctL.AddToClassList("dash-zone-pct");
                    row.Add(hdr); row.Add(track); row.Add(pctL);
                    zoneList.Add(row);
                }
            }

            // ── Class type (total / current) ─────────────────────────
            var ctList = root.Q<VisualElement>("dash-classtype-list");
            if (ctList != null)
            {
                ctList.Clear();
                foreach (var (lbl, val, isCrit) in new[] {
                    ("Tổng slots lịch", totalSlots, false),
                    ("Phòng có lớp ngay", roomsOcc, true),
                })
                {
                    var ct  = new VisualElement(); ct.AddToClassList("dash-classtype-row");
                    var ch  = new VisualElement(); ch.AddToClassList("dash-classtype-header");
                    var cn  = new Label(lbl); cn.AddToClassList("dash-classtype-name");
                    var cc  = new Label($"{val}"); cc.AddToClassList("dash-classtype-count");
                    cc.AddToClassList(isCrit ? "dash-classtype-count--exam" : "dash-classtype-count--bu");
                    ch.Add(cn); ch.Add(cc);
                    int pct2 = totalSlots > 0 && val > 0 ? val*100/totalSlots : 0;
                    var trk = new VisualElement(); trk.AddToClassList("dash-bar-track");
                    var fl  = new VisualElement(); fl.AddToClassList("dash-bar-fill");
                    fl.AddToClassList(isCrit ? "dash-bar-fill--peak" : "dash-bar-fill--low");
                    fl.style.width = new StyleLength(new Length(pct2, LengthUnit.Percent));
                    trk.Add(fl);
                    ct.Add(ch); ct.Add(trk);
                    ctList.Add(ct);
                }
            }

            // ── Hourly chart (14 bars 7h-20h) ────────────────────────
            var hourlyChart = root.Q<VisualElement>("dash-hourly-chart");
            if (hourlyChart != null)
            {
                hourlyChart.Clear();
                int[] hourSV = new int[14];
                foreach (var b in buildings)
                {
                    var live = store.GetLiveData(b.building_id);
                    if (live?.schedule?.today_classes == null) continue;
                    foreach (var cls in live.schedule.today_classes)
                    {
                        TryParseTime(cls.time_start, out int sh, out int sm);
                        TryParseTime(cls.time_end,   out int eh, out int em);
                        int sMin = sh*60+sm, eMin = eh*60+em;
                        for (int hi = 0; hi < 14; hi++)
                        {
                            int hS = (7+hi)*60, hE = (8+hi)*60;
                            if (sMin < hE && eMin > hS) hourSV[hi] += cls.student_count;
                        }
                    }
                }
                int maxSV = Mathf.Max(1, hourSV.Max());
                for (int i = 0; i < 14; i++)
                {
                    int h = 7 + i;
                    bool isNow = h == nowH;
                    float ratio = (float)hourSV[i] / maxSV;
                    var col = new VisualElement(); col.AddToClassList("dash-hourly-col");
                    var bar = new VisualElement(); bar.AddToClassList("dash-hourly-bar");
                    bar.style.height = new StyleLength(Mathf.Max(2f, ratio * 110f));
                    if (isNow)             bar.AddToClassList("dash-hourly-bar--now");
                    else if (ratio > 0.6f) bar.AddToClassList("dash-hourly-bar--peak");
                    else if (hourSV[i]>0)  bar.AddToClassList("dash-hourly-bar--normal");
                    var lbl = new Label($"{h:00}"); lbl.AddToClassList("dash-hourly-label");
                    if (isNow) lbl.AddToClassList("dash-hourly-label--now");
                    col.Add(bar); col.Add(lbl);
                    hourlyChart.Add(col);
                }
            }

            // ── Alert table ─────────────────────────────────────────
            var alertSection = root.Q<VisualElement>("dash-alert-section");
            if (alertSection != null)
            {
                alertSection.Clear();
                if (alertItems.Count > 0)
                {
                    var atitle = new Label($"CẢNH BÁO HỆ THỐNG ({alertItems.Count})");
                    atitle.AddToClassList("dash-alert-title");
                    alertSection.Add(atitle);
                    var box = new VisualElement(); box.AddToClassList("dash-alert-box");
                    foreach (var (msg, sev) in alertItems.Take(5))
                    {
                        bool isCrit = sev == AlertSeverity.Critical;
                        var arow = new VisualElement(); arow.AddToClassList("dash-alert-row");
                        var dot  = new VisualElement(); dot.AddToClassList("dash-alert-dot");
                        dot.AddToClassList(isCrit ? "dash-alert-dot--critical" : "dash-alert-dot--warning");
                        var msgL = new Label(msg); msgL.AddToClassList("dash-alert-msg");
                        var sevL = new Label(isCrit ? "Critical" : "Warning"); sevL.AddToClassList("dash-alert-sev");
                        sevL.AddToClassList(isCrit ? "dash-alert-sev--critical" : "dash-alert-sev--warning");
                        arow.Add(dot); arow.Add(msgL); arow.Add(sevL);
                        box.Add(arow);
                    }
                    alertSection.Add(box);
                }
            }

            // ── Heatmap (zones × 14h) ────────────────────────────────
            var heatmap = root.Q<VisualElement>("dash-heatmap");
            if (heatmap != null)
            {
                heatmap.Clear();
                var zones2 = buildings.Select(b => b.ten_khu).Where(z => !string.IsNullOrEmpty(z))
                             .Distinct().OrderBy(z => z).ToList();
                foreach (var zone in zones2)
                {
                    var zb = store.GetBuildingsInKhu(zone);
                    if (zb.Count == 0) continue;
                    int zt = Mathf.Max(1, zb.Sum(b => b.so_phong));
                    var row = new VisualElement(); row.AddToClassList("dash-hm-row");
                    var zl  = new Label(zone); zl.AddToClassList("dash-hm-zone");
                    row.Add(zl);
                    for (int ci = 0; ci < 14; ci++)
                    {
                        int h = 7 + ci; bool isNow = h == nowH;
                        int occ = 0;
                        foreach (var b in zb)
                        {
                            var live = store.GetLiveData(b.building_id);
                            if (live?.schedule?.today_classes == null) continue;
                            foreach (var cls in live.schedule.today_classes)
                            {
                                TryParseTime(cls.time_start, out int sh, out _);
                                TryParseTime(cls.time_end,   out int eh, out _);
                                if (sh < h+1 && eh > h) occ++;
                            }
                        }
                        int intensity = occ == 0 ? 0 : Mathf.Clamp(occ*4/zt+1, 1, 4);
                        var cell = new VisualElement(); cell.AddToClassList("dash-hm-cell");
                        cell.AddToClassList($"hm-{intensity}");
                        if (isNow) cell.AddToClassList("dash-hm-cell--now");
                        row.Add(cell);
                    }
                    heatmap.Add(row);
                }
            }

            // ── Heatmap X-labels ────────────────────────────────────
            var xlabels = root.Q<VisualElement>("dash-hm-xlabels");
            if (xlabels != null)
            {
                xlabels.Clear();
                foreach (int h in new[] { 7, 9, 11, 13, 15, 17, 19 })
                { var xl = new Label($"{h}"); xl.AddToClassList("dash-heatmap-xlabel"); xlabels.Add(xl); }
            }

            UpdateDatetime(root);
        }

        void SetStatCard(VisualElement root, int index, string num, string sub)
        {
            var cards = root.Query<VisualElement>("", "dash-stat-card").ToList();
            if (index >= cards.Count) return;
            cards[index].Q<Label>("", "dash-stat__num")?.SetText(num);
            cards[index].Q<Label>("", "dash-stat__sub")?.SetText(sub);
        }

        // ═══════════════════════════════════════════════════════════════
        // SCHEDULE v2 — Room × Tiết grid
        // ═══════════════════════════════════════════════════════════════
        int _schedDayOffset = 0;

        IEnumerator Co_PopulateSchedule()
        {
            yield return null;
            var root = docSchedule?.rootVisualElement;
            if (root == null || store == null) yield break;

            // ── Thu buttons ─────────────────────────────────────────
            var thuRow = root.Q<VisualElement>("sched-thu-row");
            if (thuRow != null)
            {
                thuRow.Clear();
                var labels = new[] { "T2", "T3", "T4", "T5", "T6" };
                var today = DUT.UI.DUTTime.Now;
                int dow = (int)today.DayOfWeek;
                var monday = today.AddDays(dow == 0 ? -6 : -(dow - 1));
                int todayIdx = GetTodayIndex();
                _schedDayOffset = todayIdx;

                for (int i = 0; i < 5; i++)
                {
                    int idx = i;
                    var d = monday.AddDays(i);
                    var btn = new Button(); btn.AddToClassList("sched-thu-btn");
                    btn.text = $"{labels[i]}  {d:dd/MM}";
                    if (i == todayIdx) btn.AddToClassList("sched-thu-btn--active");
                    Button captured = btn;
                    btn.RegisterCallback<ClickEvent>(_ =>
                    {
                        _schedDayOffset = idx;
                        thuRow.Query<Button>().ForEach(b => b.RemoveFromClassList("sched-thu-btn--active"));
                        captured.AddToClassList("sched-thu-btn--active");
                        StartCoroutine(Co_PopulateScheduleGrid(root));
                    });
                    thuRow.Add(btn);
                }
            }

            UpdateDatetime(root);
            yield return StartCoroutine(Co_PopulateScheduleGrid(root));
        }

        void UpdateWeekDayLabels(VisualElement root)
        {
            var today = DUT.UI.DUTTime.Now;
            // Lùi về thứ 2
            int dow = (int)today.DayOfWeek;
            var monday = today.AddDays(dow == 0 ? -6 : -(dow - 1));
            var labels = new[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6" };
            var dayBtns = root.Query<Button>("", "sched-day-btn").ToList();
            // Skip week button (index 0 in sched-filter__section TUẦN)
            for (int i = 0; i < Math.Min(dayBtns.Count, 5); i++)
            {
                var d = monday.AddDays(i);
                dayBtns[i].text = $"{labels[i]}  –  {d:dd/MM}";
                if (i == GetTodayIndex())
                {
                    dayBtns[i].AddToClassList("sched-day-btn--active");
                    _schedDayOffset = i;
                }
            }
        }

        static int GetTodayIndex()
        {
            int dow = (int)DUT.UI.DUTTime.Now.DayOfWeek; // Sun=0, Mon=1..Sat=6
            if (dow == 0 || dow == 6) return 0; // weekend → show Monday
            return dow - 1; // Mon=0..Fri=4
        }

        IEnumerator Co_PopulateScheduleGrid(VisualElement root)
        {
            yield return null;
            var body = root.Q<VisualElement>("sched-room-body");
            if (body == null) yield break;
            body.Clear();

            int dutThu = _schedDayOffset + 2;
            var now = DUT.UI.DUTTime.Now;
            int nowMin = now.Hour * 60 + now.Minute;

            // Giờ đúng của từng tiết (phút từ 00:00) — có nghỉ giữa tiết và nghỉ trưa
            var tietSM = new[] {0, 420,480,540,600,660,750,810,870,930, 990,1050,1110,1170,1230};
            var tietEM = new[] {0, 470,530,590,650,710,800,860,920,980,1040,1100,1160,1220,1280};
            int nowTiet = 0;
            for (int t = 1; t <= 14; t++)
                if (nowMin >= tietSM[t] && nowMin < tietEM[t]) { nowTiet = t; break; }

            // Highlight header
            var tietHdrs = root.Query<VisualElement>("", "sched-tiet-col-header").ToList();
            for (int i = 0; i < tietHdrs.Count && i < 14; i++)
            {
                bool isN = (i + 1) == nowTiet;
                tietHdrs[i].RemoveFromClassList("sched-tiet-col-header--now");
                if (isN) tietHdrs[i].AddToClassList("sched-tiet-col-header--now");
                var nl = tietHdrs[i].Q<Label>("", "sched-tiet-num");
                if (nl != null) { nl.RemoveFromClassList("sched-tiet-num--now"); if (isN) nl.AddToClassList("sched-tiet-num--now"); }
            }

            // Collect room → classes
            var roomMap = new Dictionary<string, List<(ClassInfo cls, int ts, int te)>>();
            foreach (var b in store.AllBuildings)
            {
                var classes = GetClassesForDay(b.building_id, dutThu);
                foreach (var cls in classes)
                {
                    // Extract room key
                    string room = null;
                    if (cls.room_id?.Contains("phong=") == true)
                    {
                        var m = System.Text.RegularExpressions.Regex.Match(cls.room_id, @"phong=([^&]+)");
                        if (m.Success) room = m.Groups[1].Value;
                    }
                    if (string.IsNullOrEmpty(room)) room = b.ten_ngan;

                    ParseTiet(ExtractTietFromRoomId(cls.room_id), out int ts, out int te);
                    if (ts <= 0) { var r2 = TietRangeFromClass(cls); ts = r2.start; te = r2.end; }
                    ts = Mathf.Max(1, ts); te = Mathf.Min(14, Mathf.Max(ts, te));

                    if (!roomMap.ContainsKey(room)) roomMap[room] = new List<(ClassInfo, int, int)>();
                    roomMap[room].Add((cls, ts, te));
                }
            }

            // ── Stats chip ───────────────────────────────────────────
            var statsArea = root.Q<VisualElement>("sched-stats-area");
            if (statsArea != null)
            {
                statsArea.Clear();
                var chip = new VisualElement(); chip.AddToClassList("sched-stat-chip");
                var numL = new Label(roomMap.Count.ToString()); numL.AddToClassList("sched-stat-chip__num");
                numL.style.color = new StyleColor(new Color(46f/255, 143f/255, 232f/255));
                var lblL = new Label("phòng có lịch"); lblL.AddToClassList("sched-stat-chip__label");
                chip.Add(numL); chip.Add(lblL);
                statsArea.Add(chip);

                if (nowTiet > 0 && _schedDayOffset == GetTodayIndex())
                {
                    var nowChip = new VisualElement(); nowChip.AddToClassList("sched-now-chip");
                    var dot = new VisualElement(); dot.AddToClassList("sched-now-dot");
                    var nl = new Label($"Tiết {nowTiet} · {TietStartStr(nowTiet)}");
                    nl.AddToClassList("sched-now-label");
                    nowChip.Add(dot); nowChip.Add(nl);
                    statsArea.Add(nowChip);
                }
            }

            // ── Grid: build động từ data thực (chỉ phòng có lịch) ────────
            // Group phòng theo prefix tòa nhà, sắp xếp theo tên
            var grouped = new SortedDictionary<string, List<string>>();
            foreach (var room in roomMap.Keys)
            {
                string pfx = GetRoomPrefix(room);
                if (!grouped.ContainsKey(pfx)) grouped[pfx] = new List<string>();
                grouped[pfx].Add(room);
            }

            foreach (var (prefix, rooms) in grouped)
            {
                // Group header
                var grpHdr = new VisualElement(); grpHdr.AddToClassList("sched-group-header");
                var grpLbl = new Label(GetGroupLabel(prefix)); grpLbl.AddToClassList("sched-group-label");
                var grpCnt = new Label(rooms.Count.ToString()); grpCnt.AddToClassList("sched-group-count");
                grpHdr.Add(grpLbl); grpHdr.Add(grpCnt);
                body.Add(grpHdr);

                foreach (var roomId in rooms.OrderBy(r => r))
                    body.Add(BuildRoomRow(roomId, roomMap[roomId], nowTiet));
            }

            if (roomMap.Count == 0)
            {
                var empty = new Label("Không có lịch học ngày này");
                empty.AddToClassList("sched-empty-label");
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                empty.style.paddingTop = 40;
                empty.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                body.Add(empty);
            }
        }

        static VisualElement BuildRoomRow(
            string roomId,
            List<(ClassInfo cls, int ts, int te)> rSlots,
            int nowTiet)
        {
            var tietOcc = new HashSet<int>();
            foreach (var (_, rts, rte) in rSlots)
                for (int t = rts; t <= rte; t++) tietOcc.Add(t);

            var row = new VisualElement(); row.AddToClassList("sched-room-row");
            var lc  = new VisualElement(); lc.AddToClassList("sched-room-label-cell");
            var rid = new Label(roomId); rid.AddToClassList("sched-room-id");
            lc.Add(rid); row.Add(lc);

            for (int t = 1; t <= 14; t++)
            {
                var cell = new VisualElement(); cell.AddToClassList("sched-tiet-data-col");
                if (t == nowTiet)        cell.AddToClassList("sched-tiet-data-col--now");
                else if (t % 2 == 0)     cell.AddToClassList("sched-tiet-data-col--even");
                if (tietOcc.Contains(t)) cell.AddToClassList("sched-tiet-data-col--occ");
                row.Add(cell);
            }

            var seenTs     = new HashSet<int>();
            var blockInfos = new List<(VisualElement ve, int ts, int span)>();
            foreach (var (cls, ts, te) in rSlots.OrderBy(s => s.ts))
            {
                if (!seenTs.Add(ts)) continue;
                int span = Mathf.Max(1, te - ts + 1);
                var block = new VisualElement();
                block.AddToClassList("sched-class-block");
                block.AddToClassList(cls.class_code == "Coi thi"
                    ? "sched-class-block--exam" : "sched-class-block--regular");
                block.style.position = Position.Absolute;
                block.style.top    = new StyleLength(2f);
                block.style.bottom = new StyleLength(2f);
                block.style.left   = new StyleLength(0f);
                block.style.width  = new StyleLength(40f);
                string cname = cls.class_name ?? "";
                if (cname.Length > 20) cname = cname.Substring(0, 19) + "…";
                var nameL = new Label(cname); nameL.AddToClassList("sched-class-name");
                block.Add(nameL);
                row.Add(block);
                blockInfos.Add((block, ts, span));
            }

            if (blockInfos.Count > 0)
            {
                const float gutterPx = 90f;
                const int   numCols  = 14;
                var captured = blockInfos;
                row.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    float rowW = row.resolvedStyle.width;
                    if (rowW <= gutterPx + 10f) return;
                    float cellW = (rowW - gutterPx) / numCols;
                    foreach (var (ve, ts, span) in captured)
                    {
                        ve.style.left  = gutterPx + (ts - 1) * cellW + 1f;
                        ve.style.width = span * cellW - 2f;
                    }
                });
            }

            return row;
        }

        // Lấy prefix tòa nhà từ roomId: "E2.404"→"E2", "C1.201"→"C1", "F308"→"F", "H201"→"H"
        static string GetRoomPrefix(string roomId)
        {
            var m = System.Text.RegularExpressions.Regex.Match(roomId, @"^([A-Z]+\d+)\.");
            if (m.Success) return m.Groups[1].Value;
            m = System.Text.RegularExpressions.Regex.Match(roomId, @"^([A-Z]+)\d");
            if (m.Success) return m.Groups[1].Value;
            return roomId;
        }

        static string GetGroupLabel(string prefix) => prefix switch {
            "A"    => "Khu A",
            "B"    => "Khu B",
            "C1"   => "Khu C — C1",
            "C2"   => "Khu C — C2",
            "C3"   => "Khu C — C3",
            "C4"   => "Khu C — C4",
            "C5"   => "Khu C — C5",
            "D"    => "Khu D",
            "E1"   => "Khu E — E1",
            "E2"   => "Khu E — E2",
            "F"    => "Khu F",
            "G"    => "Khu G (Xưởng)",
            "H"    => "Khu H",
            "I"    => "Khu I",
            "K"    => "Khu K",
            "M"    => "Khu M",
            "P"    => "PFIEV",
            "S"    => "Khu S",
            "S07"  => "Khu S — S07",
            "S08"  => "Khu S — S08",
            "GDTC" => "GDTC",
            _      => $"Khu {prefix}",
        };

        static string TietStartStr(int tiet)
        {
            var starts = new[] {"","07:00","08:00","09:00","10:00","11:00","12:30","13:30","14:30","15:30","16:30","17:30","18:30","19:30","20:30"};
            return tiet >= 1 && tiet <= 14 ? starts[tiet] : "";
        }

        List<ClassInfo> GetClassesForDay(string buildingId, int dutThu)
        {
            var live = store.GetLiveData(buildingId);
            if (live?.schedule?.today_classes == null) return new List<ClassInfo>();
            var all = live.schedule.today_classes;
            // Nếu room_id có encode thu=X (RealDataProvider), filter theo thu
            bool hasEncoding = all.Count > 0 && (all[0].room_id?.Contains("thu=") ?? false);
            if (hasEncoding)
                return all.Where(c => ExtractThu(c.room_id) == dutThu).ToList();
            // MockDataProvider: today_classes đã là hôm nay, trả về tất cả
            return all;
        }

        // ── Helpers ──────────────────────────────────────────────────
        static void UpdateDatetime(VisualElement root)
        {
            if (root == null) return;
            var now = DUT.UI.DUTTime.Now;
            string[] days = { "CN", "T2", "T3", "T4", "T5", "T6", "T7" };
            string text = $"{days[(int)now.DayOfWeek]}  ·  {now:dd/MM/yyyy}  ·  {now:HH:mm}";
            root.Query<Label>(null, "datetime-label").ForEach(l => l.text = text);
        }

        static bool TryParseTime(string t, out int h, out int m)
        {
            h = m = 0;
            if (string.IsNullOrEmpty(t)) return false;
            var p = t.Split(':');
            return p.Length == 2 && int.TryParse(p[0], out h) && int.TryParse(p[1], out m);
        }

        static bool IsOngoing(ClassInfo c)
        {
            int now = DUT.UI.DUTTime.Now.Hour * 60 + DUT.UI.DUTTime.Now.Minute;
            TryParseTime(c.time_start, out int sh, out int sm);
            TryParseTime(c.time_end,   out int eh, out int em);
            return now >= sh * 60 + sm && now < eh * 60 + em;
        }

        static bool IsUpcoming(ClassInfo c)
        {
            int now = DUT.UI.DUTTime.Now.Hour * 60 + DUT.UI.DUTTime.Now.Minute;
            TryParseTime(c.time_start, out int sh, out int sm);
            return sh * 60 + sm > now && sh * 60 + sm - now <= 90;
        }

        static int ExtractThu(string roomId)
        {
            var m = System.Text.RegularExpressions.Regex.Match(roomId ?? "", @"thu=(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }

        static string ExtractTietFromRoomId(string roomId)
        {
            var m = System.Text.RegularExpressions.Regex.Match(roomId ?? "", @"tiet=([0-9\-]+)");
            return m.Success ? m.Groups[1].Value : "1-2";
        }

        // Tính tiet range từ time_start/time_end (dùng khi room_id không có tiet= encoding)
        static (int start, int end) TietRangeFromClass(ClassInfo c)
        {
            if (!TryParseTime(c.time_start, out int sh, out int sm)) return (1, 2);
            if (!TryParseTime(c.time_end,   out int eh, out int em)) return (1, 2);
            int startMin = sh * 60 + sm;
            int endMin   = eh * 60 + em;
            // Mốc giờ đúng của từng tiết (phút từ 00:00)
            var tS = new[] {0, 420,480,540,600,660,750,810,870,930,990,1050,1110,1170,1230};
            int ts = 1, te = 1;
            for (int t = 1; t <= 14; t++) if (startMin >= tS[t] && startMin < tS[t] + 50) { ts = t; break; }
            for (int t = 14; t >= 1; t--) if (endMin   >  tS[t]) { te = t; break; }
            return (Mathf.Max(1, ts), Mathf.Max(ts, Mathf.Min(14, te)));
        }

        static void ParseTiet(string tiet, out int start, out int end)
        {
            var parts = tiet.Split('-');
            start = int.TryParse(parts[0], out int s) ? s : 1;
            end   = parts.Length > 1 && int.TryParse(parts[1], out int e) ? e : start;
        }
    }

    static class VEExtensions
    {
        public static void SetText(this Label l, string text) { if (l != null) l.text = text; }
    }
}
