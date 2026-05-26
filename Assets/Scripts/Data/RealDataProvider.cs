using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using DUT.Data;

namespace DUT.Data
{
    /// <summary>
    /// Load lịch học thực từ schedule_data.json (StreamingAssets).
    /// Điền ScheduleData.current_class / today_classes cho từng building.
    /// Các layer khác (Occupancy, Infrastructure, Equipment…) giữ mock.
    /// </summary>
    public class RealDataProvider : MonoBehaviour
    {
        public BuildingDataStore store;
        [Tooltip("Tên file JSON trong StreamingAssets")]
        public string jsonFileName = "schedule_data.json";
        [Tooltip("Giây giữa các lần reload")]
        public float reloadInterval = 300f; // 5 phút

        // ─── JSON schema (format: slot_per_day từ PageLichCT.aspx) ──
        [Serializable] class ScheduleRoot { public string format; public List<SlotEntry> data; }
        [Serializable] class SlotEntry
        {
            public int    thu;          // 2=T2..7=T7, 8=CN
            public string ngay;         // "25/05/2026"
            public string tiet;         // "1-2", "3-5"
            public string phong;        // "F308", "E2.101"
            public string loai;         // "Coi thi" | "Bù" | "Học"
            public string ten_mon;      // tên môn học
            public string giang_vien;   // tên giảng viên
            public string ma_lhp;       // mã lớp học phần (từ PageLopHPKH)
            public int    slsv;         // số lượng sinh viên đăng ký
        }

        // ─── Room → building_id lookup ────────────────────────────────
        // Key: room prefix (uppercase), Value: khu name trong Unity
        static readonly Dictionary<string, string> RoomPrefixToKhu = new()
        {
            { "A",    "A"    },
            { "B",    "B"    },
            { "C",    "C"    },
            { "C1",   "C"    },  // C1.xxx → khu C
            { "C2",   "C"    },  // C2.xxx → khu C
            { "C3",   "C"    },  // C3.xxx → khu C
            { "C4",   "C"    },
            { "C5",   "C"    },
            { "D",    "D"    },
            { "E1",   "E"    },
            { "E2",   "E"    },
            { "E",    "E"    },
            { "F",    "F"    },
            { "G",    "G"    },
            { "H",    "H"    },
            { "I",    "I"    },
            { "K",    "K"    },
            { "M",    "G"    },  // Khu M (xưởng) = Khu G trong Unity
            { "S",    "S"    },
            { "S07",  "S"    },  // S07.08 → khu S
            { "S08",  "S"    },
            { "N",    "N"    },
            { "GDTC", "GDTC" },
            { "PTN",  "I"    },  // Phòng thí nghiệm → khu I
            { "XP",   "K"    },  // Xưởng thực hành → khu K
            { "P",    "F"    },  // P2,P3,P6,P7 → khu F (giảng đường lớn)
        };

        // Với E1/E2 cần tìm đúng building theo ten_ngan
        static readonly Dictionary<string, string> RoomPrefixToTenNgan = new()
        {
            { "E1", "Tòa nhà E1" },
            { "E2", "Tòa nhà E2" },
            { "F",  null },  // F1 = Hội trường, F2 = Khu F — dùng khu
        };

        // ─── Tiet → giờ bắt đầu (DUT schedule) ──────────────────────
        // Sáng  (1-5):  7:00, 8:00, 9:00, 10:00, 11:00  (nghỉ 10 phút)
        // Chiều (6-10): 12:30, 13:30, 14:30, 15:30, 16:30 (nghỉ ăn trưa 40 phút)
        // Tối  (11-14): 17:30, 18:30, 19:30, 20:30
        static readonly Dictionary<int, (int h, int m)> TietToTime = new()
        {
            {1,(7,0)},  {2,(8,0)},  {3,(9,0)},  {4,(10,0)},  {5,(11,0)},
            {6,(12,30)},{7,(13,30)},{8,(14,30)}, {9,(15,30)}, {10,(16,30)},
            {11,(17,30)},{12,(18,30)},{13,(19,30)},{14,(20,30)},
        };

        // ─── Runtime state ───────────────────────────────────────────
        // building_id → list of classes today (filtered by current week)
        Dictionary<string, List<ClassInfo>> _buildingClasses = new();
        bool _realDataLoaded = false; // chỉ true khi parse được ít nhất 1 entry
        static readonly System.Random _rng = new(42); // cho mock fallback

        // ─── Unity lifecycle ─────────────────────────────────────────
        void Start()
        {
            if (store == null) store = FindFirstObjectByType<BuildingDataStore>();
            StartCoroutine(Co_LoadAndApply());
        }

        IEnumerator Co_LoadAndApply()
        {
            while (true)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                // Synchronous read — avoid nested coroutine completion issues
                string ePath = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFileName);
                if (!System.IO.File.Exists(ePath))
                    Debug.LogWarning($"[RealData] {ePath} not found — using mock only");
                else
                    ParseJson(System.IO.File.ReadAllText(ePath));
#else
                yield return StartCoroutine(Co_Load());
#endif
                yield return null; // wait one frame so MockDataProvider.Start() has populated the store
                ApplyToStore();
                yield return new WaitForSeconds(reloadInterval);
            }
        }

        // ─── Load JSON (Android / WebGL only) ─────────────────────────
        IEnumerator Co_Load()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, jsonFileName);
            using var req = UnityWebRequest.Get(path);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[RealData] Load failed: {req.error}");
                yield break;
            }
            ParseJson(req.downloadHandler.text);
        }

        void ParseJson(string json)
        {
            ScheduleRoot root;
            try { root = JsonUtility.FromJson<ScheduleRoot>(json); }
            catch (Exception e) { Debug.LogError($"[RealData] Parse error: {e.Message}"); return; }

            if (root?.data == null || root.data.Count == 0)
            {
                Debug.LogWarning("[RealData] schedule_data.json trống — giữ MockDataProvider data nguyên.");
                _realDataLoaded = false;
                return;
            }

            _realDataLoaded = true;
            _buildingClasses.Clear();

            // Dedup: cùng (thu, tiet, phong) = 1 lớp/phòng thi, chỉ giữ 1 entry
            var seen = new HashSet<string>();
            int raw = root.data.Count, dupes = 0;

            foreach (var slot in root.data)
            {
                if (string.IsNullOrEmpty(slot.phong) || string.IsNullOrEmpty(slot.tiet)) continue;

                // Key: cùng phòng+tiết+môn = duplicate giám thị → bỏ; khác môn = 2 lớp cùng phòng → giữ
                string dedupeKey = $"{slot.thu}|{slot.tiet}|{slot.phong}|{slot.ten_mon}";
                if (!seen.Add(dedupeKey)) { dupes++; continue; }

                string buildingId = ResolveBuildingId(slot.phong);
                if (buildingId == null) continue;

                if (!_buildingClasses.ContainsKey(buildingId))
                    _buildingClasses[buildingId] = new List<ClassInfo>();

                _buildingClasses[buildingId].Add(SlotEntryToClassInfo(slot));
            }

            Debug.Log($"[RealData] Parsed {raw} slots ({dupes} dupes removed) → {_buildingClasses.Count} buildings");
        }

        // ─── Apply to BuildingDataStore ───────────────────────────────
        void ApplyToStore()
        {
            if (!_realDataLoaded) return; // không có real data → giữ nguyên mock
            if (store?.AllBuildings == null) return;

            int now_h = DUT.UI.DUTTime.Now.Hour;
            int now_m = DUT.UI.DUTTime.Now.Minute;
            int dutToday = DUT.UI.DUTTime.Now.DayOfWeek == DayOfWeek.Sunday ? 8
                         : (int)DUT.UI.DUTTime.Now.DayOfWeek + 1;

            foreach (var info in store.AllBuildings)
            {
                // Lấy live data hiện có (có thể đã có mock data)
                var live = store.GetLiveData(info.building_id) ?? BuildMockLiveData(info);

                // Toàn bộ classes cả tuần (để Schedule screen filter theo ngày tùy chọn)
                List<ClassInfo> weekClasses = new();
                if (_buildingClasses.TryGetValue(info.building_id, out var all))
                    weekClasses.AddRange(all);

                // Chỉ hôm nay → xác định trạng thái building
                var todayClasses = weekClasses.FindAll(c => ExtractThu(c.room_id) == dutToday);

                // Tìm current + next class
                ClassInfo current = null, next = null;
                foreach (var c in todayClasses)
                {
                    if (IsCurrentlyOngoing(c, now_h, now_m)) { current = c; break; }
                }
                if (current == null)
                {
                    foreach (var c in todayClasses)
                    {
                        if (IsUpcoming(c, now_h, now_m)) { next = c; break; }
                    }
                }

                // Ghi đè schedule — lưu toàn bộ tuần để Schedule screen dùng
                live.schedule.today_classes = weekClasses;
                live.schedule.current_class = current;
                live.schedule.next_class    = next;
                live.schedule.status =
                    current != null ? BuildingStatus.Occupied  :
                    next    != null ? BuildingStatus.Upcoming  :
                    todayClasses.Count > 0 ? BuildingStatus.Empty :
                    BuildingStatus.Empty;

                if (current != null)
                {
                    int cnt = current.student_count > 0 ? current.student_count : current.student_capacity;
                    int cap = Mathf.Max(current.student_capacity, 1);
                    float ratio = (float)cnt / cap;
                    live.schedule.occupancy_rate = ratio;
                    // Sync occupancy layer với dữ liệu lớp học (không dùng suc_chua_toi_da của tòa)
                    live.occupancy.current_count  = cnt;
                    live.occupancy.max_capacity   = cap;
                    live.occupancy.density_ratio  = ratio;
                    live.occupancy.level = ratio < 0.3f ? OccupancyLevel.Low
                                        : ratio < 0.6f ? OccupancyLevel.Medium
                                        : ratio < 0.9f ? OccupancyLevel.High
                                        : OccupancyLevel.Overcrowded;
                }

                live.timestamp = DUT.UI.DUTTime.Now.ToString("o");
                store.UpdateLiveData(live);
            }

            // Refresh màu
            StartCoroutine(Co_RefreshColors());
            Debug.Log($"[RealData] Applied to {store.AllBuildings.Count} buildings");
        }

        // ─── Helpers: Room → BuildingId ───────────────────────────────
string ResolveBuildingId(string phong)
        {
            if (string.IsNullOrEmpty(phong)) return null;

            string prefix = ExtractRoomPrefix(phong);
            if (!RoomPrefixToKhu.TryGetValue(prefix, out string khu)) return null;

            var candidates = store.AllBuildings.FindAll(b => b.ten_khu == khu);
            if (candidates.Count == 0) return null;
            if (candidates.Count == 1) return candidates[0].building_id;

            // Khu E: E1.xxx → Tòa nhà E1, E2.xxx → Tòa nhà E2
            if (prefix == "E1" || prefix == "E2")
            {
                var match = candidates.Find(b => b.ten_ngan.Contains(prefix));
                if (match != null) return match.building_id;
            }

            // Khu F: F.xxx → "Khu F" (giảng đường), không phải Hội trường F
            if (khu == "F")
            {
                var match = candidates.Find(b => b.ten_ngan == "Khu F");
                if (match != null) return match.building_id;
            }

            // Khu C: C1.xxx → building C1, C2.xxx → C2... (sort theo building_id)
            if (khu == "C" && prefix.Length > 1
                && int.TryParse(prefix.Substring(1), out int cIdx) && cIdx >= 1)
            {
                var sorted = candidates.OrderBy(b => b.building_id).ToList();
                int i = cIdx - 1;
                if (i < sorted.Count) return sorted[i].building_id;
            }

            // GDTC: lịch sân → Trung tâm giáo dục thể chất
            if (khu == "GDTC")
            {
                var match = candidates.Find(b => b.ten_ngan.Contains("Trung tâm giáo dục thể chất"));
                if (match != null) return match.building_id;
            }

            return candidates[0].building_id;
        }

        static string ExtractRoomPrefix(string phong)
        {
            // E1.101 → E1, E2.406 → E2
            var m = System.Text.RegularExpressions.Regex.Match(phong, @"^([A-Z]+\d*)\.");
            if (m.Success) return m.Groups[1].Value;

            // F208 → F, H201 → H, M202 → M
            m = System.Text.RegularExpressions.Regex.Match(phong, @"^([A-Z]+)\d");
            if (m.Success) return m.Groups[1].Value;

            // GDTC, XP
            return phong;
        }

        // Week filtering không cần thiết — PageLichCT đã trả về đúng tuần hiện tại

        // ─── Helpers: SlotEntry → ClassInfo ──────────────────────────
        ClassInfo SlotEntryToClassInfo(SlotEntry slot)
        {
            ParseTiet(slot.tiet, out int startTiet, out int endTiet);
            GetTietTime(startTiet, out int sh, out int sm);
            // End = start of last tiet + 50 min (không dùng TietToTime[n+1] vì có khoảng nghỉ)
            GetTietTime(endTiet, out int eh, out int em);
            em += 50;
            if (em >= 60) { eh += em / 60; em %= 60; }

            string label = !string.IsNullOrEmpty(slot.ten_mon) ? slot.ten_mon
                         : slot.loai == "Bù" ? $"Bù — {slot.phong}"
                         : slot.phong;

            return new ClassInfo
            {
                class_name       = label,
                class_code       = slot.loai ?? "Học",
                group            = !string.IsNullOrEmpty(slot.ma_lhp) ? slot.ma_lhp : (slot.loai ?? "Học"),
                lecturer         = slot.giang_vien ?? "",
                time_start       = $"{sh:D2}:{sm:D2}",
                time_end         = $"{eh:D2}:{em:D2}",
                student_count    = slot.slsv,
                student_capacity = 45,
                room_id          = $"thu={slot.thu};tiet={slot.tiet};phong={slot.phong}",
                progress         = 0f,
            };
        }

        static void ParseTiet(string tiet, out int start, out int end)
        {
            var parts = tiet.Split('-');
            start = int.TryParse(parts[0], out int s) ? s : 1;
            end   = parts.Length > 1 && int.TryParse(parts[1], out int e) ? e : start;
        }

        static void GetTietTime(int tiet, out int h, out int m)
        {
            if (TietToTime.TryGetValue(tiet, out var t)) { h = t.h; m = t.m; }
            else { h = 7 + tiet; m = 0; }
        }

        static int ExtractThu(string roomId)
        {
            var m = System.Text.RegularExpressions.Regex.Match(roomId ?? "", @"thu=(\d+)");
            return m.Success ? int.Parse(m.Groups[1].Value) : -1;
        }

        static bool IsCurrentlyOngoing(ClassInfo c, int h, int m)
        {
            if (!TryParseTime(c.time_start, out int sh, out int sm)) return false;
            if (!TryParseTime(c.time_end,   out int eh, out int em)) return false;
            int now = h * 60 + m, s = sh * 60 + sm, e = eh * 60 + em;
            return now >= s && now < e;
        }

        static bool IsUpcoming(ClassInfo c, int h, int m)
        {
            if (!TryParseTime(c.time_start, out int sh, out int sm)) return false;
            int now = h * 60 + m, s = sh * 60 + sm;
            return s > now && s - now <= 60; // trong vòng 1 tiếng tới
        }

        static bool TryParseTime(string t, out int h, out int m)
        {
            h = m = 0;
            if (string.IsNullOrEmpty(t)) return false;
            var p = t.Split(':');
            return p.Length == 2 && int.TryParse(p[0], out h) && int.TryParse(p[1], out m);
        }

        // ─── Mock fallback (copy nhỏ từ MockDataProvider) ────────────
        BuildingLiveData BuildMockLiveData(BuildingInfo info)
        {
            var live = new BuildingLiveData
            {
                building_id = info.building_id,
                timestamp   = DUT.UI.DUTTime.Now.ToString("o"),
                schedule    = new ScheduleData { status = BuildingStatus.Unknown },
            };
            // Occupancy mock đơn giản
            int cap = Mathf.Max(info.suc_chua_toi_da, 50);
            live.occupancy = new OccupancyData
            {
                max_capacity = cap, current_count = 0,
                density_ratio = 0f, level = OccupancyLevel.Empty,
                trend = "—",
            };
            live.infrastructure = new InfrastructureData
            {
                electric = new ElectricData { current_kw = 5f, avg_kw = 5f },
                water    = new WaterData    { current_lph = 50f },
                climate  = new ClimateData  { temperature_c = 28f, humidity_pct = 65f, ac_total = 4 },
            };
            live.equipment    = new EquipmentData { total_devices = 10, active_devices = 10 };
            live.events       = new List<EventData>();
            live.maintenance  = new MaintenanceData { overall_status = MaintenanceStatus.Normal };
            return live;
        }

        IEnumerator Co_RefreshColors()
        {
            yield return null;
            yield return null;
            var cols = FindObjectsByType<DUT.Core.BuildingColorizer>(FindObjectsSortMode.None);
            foreach (var c in cols) c.Refresh();
        }
    }
}
