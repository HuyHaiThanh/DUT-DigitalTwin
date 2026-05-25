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

        // ── Tiết DUT → giờ bắt đầu ───────────────────────────────────
        static readonly int[] TietHour = { 0, 7, 7, 8, 9, 10, 11, 12, 13, 14, 15, 15, 16, 17, 18 };
        static readonly int[] TietMin  = { 0, 0, 50,40,30,20,10,30, 20, 10,  0, 50, 40, 30, 20 };

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
        // DASHBOARD
        // ═══════════════════════════════════════════════════════════════
        IEnumerator Co_PopulateDashboard()
        {
            yield return null; // espera 1 frame para el UIDocument se active
            var root = docDashboard?.rootVisualElement;
            if (root == null || store == null) yield break;

            var buildings = store.AllBuildings;
            int total    = buildings.Count;
            int occupied = buildings.Count(b => store.GetStatus(b.building_id) == BuildingStatus.Occupied);
            int empty    = buildings.Count(b => store.GetStatus(b.building_id) == BuildingStatus.Empty);
            int upcoming = buildings.Count(b => store.GetStatus(b.building_id) == BuildingStatus.Upcoming);

            // ── Stat cards ────────────────────────────────────────────
            SetStatCard(root, 0, total.ToString(),    "Campus DUT");
            SetStatCard(root, 1, occupied.ToString(), $"{(total>0?(occupied*100/total):0)}% tổng số");
            SetStatCard(root, 2, empty.ToString(),    $"{(total>0?(empty*100/total):0)}% tổng số");
            SetStatCard(root, 3, upcoming.ToString(), "trong 30 phút");

            // ── Bar chart: occupied per "tiết" nhóm theo giờ ─────────
            // Tính số buildings có lớp theo tiết (7-17h)
            var hourBars = new int[11]; // index 0=7h, 1=8h, ..., 10=17h
            foreach (var b in buildings)
            {
                var live = store.GetLiveData(b.building_id);
                if (live?.schedule?.today_classes == null) continue;
                foreach (var cls in live.schedule.today_classes)
                {
                    if (!TryParseTime(cls.time_start, out int sh, out _)) continue;
                    if (!TryParseTime(cls.time_end,   out int eh, out _)) continue;
                    for (int h = Math.Max(7, sh); h < Math.Min(18, eh); h++)
                        if (h - 7 < hourBars.Length) hourBars[h - 7]++;
                }
            }
            int barMax = Mathf.Max(1, hourBars.Max());
            var barCols = root.Query<VisualElement>("", "bar-col").ToList();
            for (int i = 0; i < barCols.Count && i < hourBars.Length; i++)
            {
                var fill = barCols[i].Q<VisualElement>("", "bar-fill");
                if (fill == null) continue;
                float ratio = (float)hourBars[i] / barMax;
                fill.style.height = new StyleLength(Mathf.Max(4, ratio * 100));
                // Màu theo mức độ
                fill.RemoveFromClassList("bar-fill--occupied");
                fill.RemoveFromClassList("bar-fill--upcoming");
                if (ratio > 0.7f) fill.AddToClassList("bar-fill--occupied");
                else if (ratio > 0.3f) fill.AddToClassList("bar-fill--upcoming");
            }

            // ── Donut: phân bổ chức năng ──────────────────────────────
            int cntGD = buildings.Count(b => b.chuc_nang == BuildingFunction.GiangDuong);
            int cntTN = buildings.Count(b => b.chuc_nang == BuildingFunction.ThiNghiem);
            int cntHC = buildings.Count(b => b.chuc_nang == BuildingFunction.HanhChinh);
            int cntTC = total - cntGD - cntTN - cntHC;
            var donutPct = root.Q<Label>("", "donut-pct");
            if (donutPct != null) donutPct.text = total > 0 ? $"{cntGD * 100 / total}%" : "—";
            var legendVals = root.Query<Label>("", "donut-legend-val").ToList();
            if (legendVals.Count >= 4)
            {
                legendVals[0].text = cntGD.ToString();
                legendVals[1].text = cntTN.ToString();
                legendVals[2].text = cntHC.ToString();
                legendVals[3].text = cntTC.ToString();
            }

            // ── Top list: buildings có nhiều lớp nhất hôm nay ─────────
            var ranked = buildings
                .Select(b => (b, cnt: store.GetLiveData(b.building_id)?.schedule?.today_classes?.Count ?? 0))
                .OrderByDescending(x => x.cnt)
                .Take(5)
                .ToList();

            var rows = root.Query<VisualElement>("", "top-list-row").ToList();
            int maxCnt = ranked.Count > 0 ? Math.Max(1, ranked[0].cnt) : 1;
            for (int i = 0; i < rows.Count && i < ranked.Count; i++)
            {
                var (b, cnt) = ranked[i];
                rows[i].Q<Label>("", "top-list__name")?.SetText(b.ten_ngan);
                var fill = rows[i].Q<VisualElement>("", "top-list__bar-fill");
                if (fill != null) fill.style.width = new StyleLength(new Length(cnt * 100f / maxCnt, LengthUnit.Percent));
                var pct = rows[i].Q<Label>("", "top-list__pct");
                if (pct != null) pct.text = cnt > 0 ? $"{cnt * 100 / maxCnt}%" : "—";
            }

            // ── Heatmap: khu × giờ ─────────────────────────────────────
            var hmRows = root.Query<VisualElement>("", "heatmap-row").ToList();
            var khuList = new[] { "A", "C", "F", "I", "K", "G", "D" };
            for (int ri = 0; ri < hmRows.Count && ri < khuList.Length; ri++)
            {
                string khu = khuList[ri];
                var khuBldgs = store.GetBuildingsInKhu(khu);
                var cells = hmRows[ri].Query<VisualElement>("", "heatmap-cell").ToList();
                int[] hours = { 7, 9, 11, 13, 15, 17, 19, 21 };
                for (int ci = 0; ci < cells.Count && ci < hours.Length; ci++)
                {
                    int h = hours[ci];
                    int occ = khuBldgs.Count(b =>
                    {
                        var live = store.GetLiveData(b.building_id);
                        if (live?.schedule?.today_classes == null) return false;
                        return live.schedule.today_classes.Any(cls =>
                        {
                            TryParseTime(cls.time_start, out int sh, out _);
                            TryParseTime(cls.time_end,   out int eh, out _);
                            return h >= sh && h < eh;
                        });
                    });
                    int khuCount = Math.Max(1, khuBldgs.Count);
                    int level = occ == 0 ? 0 : occ * 4 / khuCount + 1;
                    level = Mathf.Clamp(level, 0, 4);
                    for (int lv = 0; lv <= 4; lv++) cells[ci].RemoveFromClassList($"hm-{lv}");
                    cells[ci].AddToClassList($"hm-{level}");
                }
            }

            // ── Datetime ──────────────────────────────────────────────
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
        // SCHEDULE
        // ═══════════════════════════════════════════════════════════════
        int _schedDayOffset = 0; // 0 = hôm nay, -1 = hôm qua, +1 = ngày mai

        IEnumerator Co_PopulateSchedule()
        {
            yield return null;
            var root = docSchedule?.rootVisualElement;
            if (root == null || store == null) yield break;

            // Bind day buttons
            var dayBtns = root.Query<Button>("", "sched-day-btn").ToList();
            for (int i = 0; i < dayBtns.Count; i++)
            {
                int offset = i - GetTodayIndex(); // offset từ đầu tuần đến hôm nay
                int finalOffset = i;
                dayBtns[i].RegisterCallback<ClickEvent>(_ =>
                {
                    _schedDayOffset = finalOffset;
                    StartCoroutine(Co_PopulateScheduleGrid(root));
                    // Toggle active
                    foreach (var b in dayBtns) b.RemoveFromClassList("sched-day-btn--active");
                    dayBtns[finalOffset].AddToClassList("sched-day-btn--active");
                });
            }

            // Label tuần
            var weekBtn = root.Q<Button>("", "sched-day-btn");
            UpdateWeekDayLabels(root);
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

            // DUT thu: T2=2, T3=3... Chuyển từ schedule offset (0=T2)
            int dutThu = _schedDayOffset + 2; // 0→2(T2), 1→3(T3)...

            // Chọn 7 buildings có nhiều lớp nhất trong ngày đó
            var buildings = store.AllBuildings
                .Select(b => (b, classes: GetClassesForDay(b.building_id, dutThu)))
                .Where(x => x.classes.Count > 0)
                .OrderByDescending(x => x.classes.Count)
                .Take(7)
                .ToList();

            // Fallback: nếu không có classes hôm nay (real data chưa đủ),
            // lấy buildings có any today_classes (mock data)
            if (buildings.Count == 0) {
                buildings = store.AllBuildings
                    .Select(b => {
                        var live = store.GetLiveData(b.building_id);
                        var cls = live?.schedule?.today_classes ?? new List<ClassInfo>();
                        return (b, classes: cls);
                    })
                    .Where(x => x.classes.Count > 0)
                    .OrderByDescending(x => x.classes.Count)
                    .Take(7)
                    .ToList();
            }

            // Nếu vẫn không đủ 7, bổ sung buildings giảng đường
            if (buildings.Count < 7) {
                var extras = store.AllBuildings
                    .Where(b => (b.chuc_nang == BuildingFunction.GiangDuong ||
                                 b.chuc_nang == BuildingFunction.ThiNghiem) &&
                                !buildings.Any(x => x.b.building_id == b.building_id))
                    .Take(7 - buildings.Count)
                    .Select(b => (b, classes: new List<ClassInfo>()));
                buildings.AddRange(extras);
            }

            // ── Header columns ────────────────────────────────────────
            var headers = root.Query<VisualElement>("", "sched-col-header").ToList();
            for (int i = 0; i < headers.Count && i < buildings.Count; i++)
            {
                var (b, _) = buildings[i];
                headers[i].Q<Label>("", "sched-col-name")?.SetText(b.ten_ngan);
                headers[i].Q<Label>("", "sched-col-khu") ?.SetText($"Khu {b.ten_khu}");
            }

            // ── Time rows ─────────────────────────────────────────────
            // Tiết slots: 1-4 (7:00-9:30), 5-7 (10:20-12:10), 7-10 (12:30-15:00), 10-13 (15:00-17:30)
            var timeSlots = new[] { 1, 5, 7, 10, 13 }; // tiết đầu mỗi row
            var timeLabels = new[] { "07:00", "10:20", "12:30", "15:00", "17:30" };

            var timeRows = root.Query<VisualElement>("", "sched-time-row").ToList();
            for (int ri = 0; ri < timeRows.Count && ri < timeSlots.Length; ri++)
            {
                int tiet = timeSlots[ri];
                int nextTiet = ri + 1 < timeSlots.Length ? timeSlots[ri + 1] : 15;

                // Label giờ
                var lbl = timeRows[ri].Q<Label>("", "sched-time-label");
                if (lbl != null) lbl.text = timeLabels[ri];

                // Cells
                var cells = timeRows[ri].Query<VisualElement>("", "sched-cell").ToList();
                for (int ci = 0; ci < cells.Count && ci < buildings.Count; ci++)
                {
                    var (_, classes) = buildings[ci];

                    // Tìm lớp trùng với slot này
                    var cls = classes.FirstOrDefault(c =>
                    {
                        int cs, ce;
                        bool hasEnc = c.room_id?.Contains("tiet=") ?? false;
                        if (hasEnc) ParseTiet(ExtractTietFromRoomId(c.room_id), out cs, out ce);
                        else        (cs, ce) = TietRangeFromClass(c);
                        return cs <= nextTiet - 1 && ce >= tiet;
                    });

                    cells[ci].ClearClassList();
                    cells[ci].AddToClassList("sched-cell");

                    if (cls != null)
                    {
                        bool isNow = IsOngoing(cls);
                        bool isSoon = !isNow && IsUpcoming(cls);
                        cells[ci].AddToClassList(isNow ? "sched-cell--occupied" :
                                                 isSoon ? "sched-cell--upcoming" : "sched-cell--occupied");

                        cells[ci].Q<Label>("", "sched-cell__course")?.SetText(
                            cls.class_name.Length > 16 ? cls.class_name.Substring(0, 14) + "…" : cls.class_name);
                        cells[ci].Q<Label>("", "sched-cell__info")?.SetText(
                            $"{cls.time_start}  ·  {cls.student_count} SV");
                    }
                    else
                    {
                        cells[ci].AddToClassList("sched-cell--empty");
                        cells[ci].Q<Label>("", "sched-cell__course")?.SetText("");
                        cells[ci].Q<Label>("", "sched-cell__info") ?.SetText("");
                    }
                }
            }
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

        // Tính tiet range từ time_start/time_end của class (dùng khi không có encoding)
        static (int start, int end) TietRangeFromClass(ClassInfo c)
        {
            if (!TryParseTime(c.time_start, out int sh, out _)) return (1, 2);
            if (!TryParseTime(c.time_end,   out int eh, out _)) return (1, 2);
            // Tiết 1=7:00, 2=7:50... mỗi tiết 50 phút
            int startTiet = Mathf.Max(1, (sh - 7) * 60 / 50 + 1);
            int endTiet   = Mathf.Max(startTiet, (eh * 60 - 7 * 60) / 50);
            return (startTiet, endTiet);
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
