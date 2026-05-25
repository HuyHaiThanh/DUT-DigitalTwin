#if UNITY_EDITOR
// Scripts/Core/ModelSetupHelper.cs
// UNITY EDITOR ONLY — không ảnh hưởng build
// Chạy các menu "DUT/..." để khảo sát và đặt tên lại model 3D
//
// ĐỌC TRƯỚC: file DUT_CAMPUS_MAP_EXTRACT.md để hiểu cấu trúc phòng

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DUT.Core.Editor
{
    public static class ModelSetupHelper
    {
        // ══════════════════════════════════════════════════════════════
        // DATA — Trích xuất từ bản đồ 2D chính thức trường DUT
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Tất cả mã phòng học hợp lệ của DUT.
        /// Gắn RoomObject.cs vào các object có tên khớp với set này.
        /// </summary>
        static readonly HashSet<string> VALID_ROOM_IDS = new HashSet<string>
        {
            // ── Khu A (36 phòng) ──────────────────────────────────────
            "A102","A103","A104","A105","A106","A107","A108","A109","A110",
            "A111","A112","A113","A114","A115","A116","A117","A118","A120",
            "A123","A124","A125","A126","A127","A128","A129","A130",
            "A132","A133","A134","A135","A136","A137","A138","A139",
            "A153","A154",

            // ── Khu B (2 phòng) ───────────────────────────────────────
            "B108","B109",

            // ── Khu C (13 phòng) ──────────────────────────────────────
            "C104","C105","C108","C109","C110","C111","C112","C113",
            "C114","C115","C120","C121","C128",

            // ── Khu D (10 phòng) ──────────────────────────────────────
            "D103","D105","D106","D108","D109","D110","D111","D112","D114","D115",

            // ── Khu E (6 phòng) ───────────────────────────────────────
            "E101","E102","E103","E104","E113","E124",

            // ── Khu F (10 phòng) ──────────────────────────────────────
            "F101","F102","F103","F106","F107","F108","F109","F110","F111","F112",

            // ── Khu G (5 phòng xưởng thực hành) ──────────────────────
            "G102","G103","G104","G105","G106",

            // ── Khu H (4 phòng) ───────────────────────────────────────
            "H101","H102","H103","H104",

            // ── Khu I (4 phòng lab Điện) ──────────────────────────────
            "I101","I104","I105","I106",

            // ── Khu K (8 phòng lab chuyên ngành) ──────────────────────
            "K101","K102","K103","K104","K105","K106","K107","K108",

            // ── Khu P (3 PTN trung tâm) ───────────────────────────────
            "P1","P2","P3",

            // ── Smart Building ─────────────────────────────────────────
            "S0104",
        };

        /// <summary>
        /// Mapping building prefix → tên parent GameObject chuẩn
        /// </summary>
        static readonly Dictionary<string, string> BUILDING_PARENTS = new Dictionary<string, string>
        {
            { "A", "Building_A" },
            { "B", "Building_B" },
            { "C", "Building_C" },
            { "D", "Building_D" },
            { "E", "Building_E" },
            { "F", "Building_F" },
            { "G", "Building_G" },
            { "H", "Building_H" },
            { "I", "Building_I" },
            { "K", "Building_K" },
            { "P", "Building_P" },
            { "S", "Building_S" },
        };

        /// <summary>
        /// Công trình phụ — không có lịch học, không gắn RoomObject
        /// </summary>
        static readonly Dictionary<string, string> MISC_OBJECTS = new Dictionary<string, string>
        {
            { "thu vien",               "Misc_ThuVien"               },
            { "nha thi dau",            "Misc_NhaThiDau"             },
            { "vien co khi",            "Misc_VienCoKhi"             },
            { "hoi truong f",           "Misc_HoiTruongF"            },
            { "san bong chuyen",        "Misc_SanBongChuyen"         },
            { "san bong da",            "Misc_SanBongDa"             },
            { "tt giao duc the chat",   "Misc_TTGiaoDucTheChat"      },
            { "pfiev",                  "Misc_PFIEV"                 },
            { "thuc hanh dien",         "Misc_ThucHanhDien"          },
            { "xuong dong luc",         "Misc_XuongDongLuc"          },
            { "xuong co khi",           "Misc_XuongCoKhi"            },
            { "xuong dien",             "Misc_XuongDien"             },
            { "xuong nhiet",            "Misc_XuongNhiet"            },
            { "gara o to",              "Misc_GaraOto"               },
            { "nha kho",                "Misc_NhaKho"                },
            { "ky tuc xa",              "Misc_KTX"                   },
            { "trung tam nghien cuu",   "Misc_TrungTamNCDienTu"      },
        };

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 1 — Phân tích hierarchy
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/1. Phân tích Hierarchy (xuất báo cáo)")]
        static void AnalyzeHierarchy()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var sb = new StringBuilder();
            sb.AppendLine("=== BÁO CÁO HIERARCHY ===");
            sb.AppendLine($"Tổng MeshRenderer: {renderers.Length}");
            sb.AppendLine($"Thời gian: {System.DateTime.Now}\n");
            sb.AppendLine("FORMAT: [DEPTH] | [PATH] | [Verts] | [Bounds W×H×D m]\n");

            var sorted = renderers.OrderBy(r => GetDepth(r.transform))
                                  .ThenBy(r => r.name);

            foreach (var r in sorted)
            {
                int depth    = GetDepth(r.transform);
                string path  = GetPath(r.transform);
                int verts    = r.GetComponent<MeshFilter>()?.sharedMesh?.vertexCount ?? 0;
                var b        = r.bounds;
                string line  = $"[{depth:D2}] | {path,-80} | v:{verts,6} | {b.size.x:F1}×{b.size.y:F1}×{b.size.z:F1}m";
                sb.AppendLine(line);
            }

            // Thống kê theo depth
            sb.AppendLine("\n=== PHÂN TÍCH THEO DEPTH ===");
            var byDepth = renderers.GroupBy(r => GetDepth(r.transform));
            foreach (var g in byDepth.OrderBy(g => g.Key))
                sb.AppendLine($"Depth {g.Key:D2}: {g.Count(),4} objects");

            // Phân tích tên — tìm pattern
            sb.AppendLine("\n=== PATTERN TÊN THƯỜNG GẶP ===");
            var nameGroups = renderers
                .GroupBy(r => r.name.Length > 3 ? r.name.Substring(0, 3) : r.name)
                .Where(g => g.Count() > 1)
                .OrderByDescending(g => g.Count());
            foreach (var g in nameGroups.Take(30))
                sb.AppendLine($"  '{g.Key}...' : {g.Count()} objects");

            string path2 = "Assets/DUT_HierarchyReport.txt";
            File.WriteAllText(path2, sb.ToString());
            AssetDatabase.Refresh();
            Debug.Log($"[DUT] Báo cáo xuất ra: {path2}\nMở file để đọc cấu trúc model.");
            EditorUtility.DisplayDialog("Hoàn thành",
                $"Báo cáo đã xuất:\n{path2}\n\nTổng {renderers.Length} MeshRenderer.", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 2 — Tìm ứng viên phòng học
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/2. Tìm ứng viên phòng học (bounds nhỏ)")]
        static void FindRoomCandidates()
        {
            var renderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var candidates = renderers.Where(r =>
            {
                var s = r.bounds.size;
                // Phòng học: rộng 5–25m, cao 3–6m, sâu 5–20m
                return s.x is > 4f and < 30f &&
                       s.y is > 2f and < 8f  &&
                       s.z is > 4f and < 30f;
            }).OrderBy(r => r.name).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("=== ỨNG VIÊN PHÒNG HỌC ===");
            sb.AppendLine($"Tìm thấy {candidates.Count} objects có bounds phù hợp phòng học:\n");
            foreach (var r in candidates)
            {
                var s = r.bounds.size;
                sb.AppendLine($"  {GetPath(r.transform),-70} | {s.x:F1}×{s.y:F1}×{s.z:F1}m");
            }

            string path = "Assets/DUT_RoomCandidates.txt";
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();

            // Highlight trong scene
            Selection.objects = candidates.Select(r => r.gameObject).ToArray<Object>();
            Debug.Log($"[DUT] {candidates.Count} ứng viên được highlight. Xuất: {path}");
            EditorUtility.DisplayDialog("Ứng viên phòng học",
                $"Tìm thấy {candidates.Count} objects.\nĐã chọn trong Hierarchy.\nXem chi tiết: {path}", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 3 — Rename object đang chọn thành phòng DUT
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/3. Rename Selection → Room_XX theo bản đồ DUT")]
        static void RenameSelectedAsRoom()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Chưa chọn object nào trong Hierarchy!", "OK");
                return;
            }

            // Cho user nhập mã phòng
            string roomCode = "A101"; // default
            bool confirmed  = EditorUtility.DisplayDialog(
                "Rename thành Room",
                $"Đang rename {selected.Length} object(s).\n\n" +
                "Nhập mã phòng vào Console rồi nhấn OK.\n" +
                "Ví dụ: A101, C112, F108, H103\n\n" +
                $"Mã hợp lệ theo bản đồ DUT:\n" +
                "A(102-154), B(108-109), C(104-128),\n" +
                "D(103-115), E(101-124), F(101-112),\n" +
                "G(102-106), H(101-104), I(101-106),\n" +
                "K(101-108), P(1-3), S(0104)",
                "OK", "Hủy");

            if (!confirmed) return;

            // Với nhiều objects: tự đánh số
            for (int i = 0; i < selected.Length; i++)
            {
                var go       = selected[i];
                string suffix = selected.Length > 1 ? $"_{(i+1):D3}" : "";
                string newName = $"Room_{roomCode}{suffix}";

                Undo.RecordObject(go, "DUT Rename Room");
                go.name = newName;
                go.tag  = "Room";
                SetLayerRecursive(go, LayerMask.NameToLayer("Room"));
                EditorUtility.SetDirty(go);
                Debug.Log($"[DUT] Renamed: {newName}");
            }
            Debug.Log($"[DUT] Renamed {selected.Length} objects.");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 4 — Auto-rename dựa trên pattern tên gốc
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/4. Auto-rename theo pattern (A101, C112...)")]
        static void AutoRenameByPattern()
        {
            var allGos  = GetAllGameObjects();
            int renamed = 0;
            int skipped = 0;

            foreach (var go in allGos)
            {
                string origName = go.name.Trim();

                // Tìm pattern: 1 chữ cái + 3 chữ số (A101, C112, F108...)
                // hoặc 1 chữ cái + 1-2 chữ số (P1, P2, P3)
                string extracted = TryExtractRoomCode(origName);

                if (extracted != null && VALID_ROOM_IDS.Contains(extracted))
                {
                    string newName = $"Room_{extracted}";
                    if (go.name != newName)
                    {
                        Undo.RecordObject(go, "DUT Auto-Rename");
                        go.name = newName;
                        go.tag  = "Room";
                        SetLayerRecursive(go, LayerMask.NameToLayer("Room"));
                        EditorUtility.SetDirty(go);
                        renamed++;
                        Debug.Log($"[DUT] Auto-renamed: '{origName}' → '{newName}'");
                    }
                }
                else skipped++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[DUT] Auto-rename: {renamed} đổi tên, {skipped} bỏ qua.");
            EditorUtility.DisplayDialog("Auto-rename hoàn thành",
                $"Đổi tên thành công: {renamed} objects\nBỏ qua (không khớp): {skipped}", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 5 — Batch add RoomObject component
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/5. Add RoomObject vào tất cả Room_* objects")]
        static void BatchAddRoomObject()
        {
            var allGos  = GetAllGameObjects();
            int added   = 0;
            int already = 0;

            foreach (var go in allGos)
            {
                if (!go.name.StartsWith("Room_")) continue;

                // Đảm bảo có Collider
                if (go.GetComponentInChildren<Collider>() == null)
                {
                    var col = go.GetComponentInChildren<MeshRenderer>();
                    if (col != null)
                    {
                        Undo.AddComponent<MeshCollider>(col.gameObject);
                        Debug.Log($"[DUT] Added MeshCollider: {go.name}");
                    }
                }

                // Add RoomObject nếu chưa có
                // Dùng reflection để tránh lỗi compile nếu chưa có script
                var existing = go.GetComponent("RoomObject");
                if (existing == null)
                {
                    // Tìm type RoomObject trong assemblies
                    var type = System.Type.GetType("DUT.Core.RoomObject, Assembly-CSharp");
                    if (type != null)
                    {
                        Undo.AddComponent(go, type);
                        added++;
                        Debug.Log($"[DUT] Added RoomObject: {go.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[DUT] Chưa tìm thấy RoomObject type. Tạo Scripts/Core/RoomObject.cs trước.");
                        break;
                    }
                }
                else already++;
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Batch Add hoàn thành",
                $"Thêm mới: {added} RoomObject\nĐã có: {already}", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 6 — Tổ chức lại hierarchy theo Building_*
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/6. Tổ chức Hierarchy (Room → Building_* parent)")]
        static void OrganizeHierarchy()
        {
            // Tìm hoặc tạo root "Campus"
            var campusGo = GameObject.Find("Campus");
            if (campusGo == null)
            {
                campusGo = new GameObject("Campus");
                Undo.RegisterCreatedObjectUndo(campusGo, "Create Campus");
            }

            var buildingsGo = campusGo.transform.Find("Buildings")?.gameObject;
            if (buildingsGo == null)
            {
                buildingsGo = new GameObject("Buildings");
                Undo.SetTransformParent(buildingsGo.transform, campusGo.transform, "Parent Buildings");
            }

            // Tạo Building_* parents
            var buildingParents = new Dictionary<string, GameObject>();
            foreach (var kv in BUILDING_PARENTS)
            {
                var existing = buildingsGo.transform.Find(kv.Value)?.gameObject;
                if (existing == null)
                {
                    existing = new GameObject(kv.Value);
                    Undo.RegisterCreatedObjectUndo(existing, $"Create {kv.Value}");
                    Undo.SetTransformParent(existing.transform, buildingsGo.transform, $"Parent {kv.Value}");
                }
                buildingParents[kv.Key] = existing;
            }

            // Di chuyển Room_* vào đúng Building_*
            var roomObjects = GetAllGameObjects()
                .Where(g => g.name.StartsWith("Room_"))
                .ToList();

            int moved = 0;
            foreach (var room in roomObjects)
            {
                // "Room_A101" → prefix "A"
                string code   = room.name.Replace("Room_", "");
                string prefix = code.Length > 0 ? code[0].ToString().ToUpper() : "";

                if (buildingParents.TryGetValue(prefix, out var parent))
                {
                    if (room.transform.parent != parent.transform)
                    {
                        Undo.SetTransformParent(room.transform, parent.transform, $"Move {room.name}");
                        moved++;
                    }
                }
            }

            Debug.Log($"[DUT] Organize: di chuyển {moved} rooms vào Building_* parents.");
            EditorUtility.DisplayDialog("Tổ chức Hierarchy",
                $"Di chuyển {moved} Room objects vào Building_* parents.\n" +
                "Hierarchy đã chuẩn theo:\n" +
                "Campus → Buildings → Building_A/B/C... → Room_*", "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // MENU ITEM 7 — Validate toàn bộ setup
        // ══════════════════════════════════════════════════════════════

        [MenuItem("DUT/7. Validate Setup (báo cáo lỗi)")]
        static void ValidateSetup()
        {
            var allGos     = GetAllGameObjects();
            var roomGos    = allGos.Where(g => g.name.StartsWith("Room_")).ToList();
            var sb         = new StringBuilder();
            int errorCount = 0;

            sb.AppendLine("=== BÁO CÁO VALIDATE DUT ===\n");

            // 1. Kiểm tra room_id hợp lệ
            sb.AppendLine("── Kiểm tra room_id hợp lệ:");
            foreach (var r in roomGos)
            {
                string code = r.name.Replace("Room_", "");
                if (!VALID_ROOM_IDS.Contains(code))
                {
                    sb.AppendLine($"  ⚠ INVALID ID: {r.name} ('{code}' không có trong bản đồ DUT)");
                    errorCount++;
                }
            }
            sb.AppendLine($"  → {roomGos.Count} Room objects, {errorCount} ID không hợp lệ\n");

            // 2. Kiểm tra trùng tên
            sb.AppendLine("── Kiểm tra tên trùng lặp:");
            var dupes = roomGos.GroupBy(g => g.name).Where(g => g.Count() > 1);
            foreach (var d in dupes)
            {
                sb.AppendLine($"  ⚠ TRÙNG: '{d.Key}' xuất hiện {d.Count()} lần");
                errorCount++;
            }
            if (!dupes.Any()) sb.AppendLine("  ✓ Không có tên trùng\n");

            // 3. Kiểm tra Tag
            sb.AppendLine("── Kiểm tra Tag 'Room':");
            var missingTag = roomGos.Where(g => !g.CompareTag("Room")).ToList();
            if (missingTag.Any())
            {
                foreach (var g in missingTag)
                    sb.AppendLine($"  ⚠ Thiếu tag: {g.name}");
                errorCount += missingTag.Count;
            }
            else sb.AppendLine("  ✓ Tất cả có tag 'Room'\n");

            // 4. Kiểm tra Collider
            sb.AppendLine("── Kiểm tra Collider:");
            var missingCol = roomGos.Where(g => g.GetComponentInChildren<Collider>() == null).ToList();
            if (missingCol.Any())
            {
                foreach (var g in missingCol.Take(10))
                    sb.AppendLine($"  ⚠ Thiếu Collider: {g.name}");
                if (missingCol.Count > 10)
                    sb.AppendLine($"  ... và {missingCol.Count - 10} objects khác");
                errorCount += missingCol.Count;
            }
            else sb.AppendLine("  ✓ Tất cả có Collider\n");

            // 5. Kiểm tra Layer
            sb.AppendLine("── Kiểm tra Layer 'Room' (layer 6):");
            bool layerExists = LayerMask.NameToLayer("Room") != -1;
            if (!layerExists)
            {
                sb.AppendLine("  ⚠ Layer 'Room' chưa tạo! Vào Project Settings > Tags and Layers > thêm 'Room' vào slot 6");
                errorCount++;
            }
            else sb.AppendLine("  ✓ Layer 'Room' tồn tại\n");

            // 6. Phòng trong bản đồ nhưng chưa có trong scene
            sb.AppendLine("── Phòng có trong bản đồ nhưng chưa có trong scene:");
            var foundIds    = new HashSet<string>(roomGos.Select(g => g.name.Replace("Room_", "")));
            var missingIds  = VALID_ROOM_IDS.Except(foundIds).OrderBy(s => s).ToList();
            if (missingIds.Any())
            {
                sb.AppendLine($"  Thiếu {missingIds.Count} phòng: {string.Join(", ", missingIds)}");
            }
            else sb.AppendLine("  ✓ Tất cả 102 phòng trong bản đồ đã có trong scene\n");

            // Tổng kết
            sb.AppendLine($"\n══ KẾT QUẢ: {(errorCount == 0 ? "✓ PASS" : $"⚠ {errorCount} LỖI")} ══");
            sb.AppendLine($"Room objects: {roomGos.Count} / 102 phòng trong bản đồ");

            string path = "Assets/DUT_ValidationReport.txt";
            File.WriteAllText(path, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog(
                errorCount == 0 ? "✓ Validate PASS" : $"⚠ {errorCount} lỗi",
                $"Room objects: {roomGos.Count}\nLỗi: {errorCount}\n\nXem chi tiết: {path}",
                "OK");
        }

        // ══════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ══════════════════════════════════════════════════════════════

        static List<GameObject> GetAllGameObjects()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                         .Select(t => t.gameObject)
                         .ToList();
        }

        static int GetDepth(Transform t)
        {
            int d = 0;
            while (t.parent != null) { d++; t = t.parent; }
            return d;
        }

        static string GetPath(Transform t)
        {
            var parts = new List<string>();
            while (t != null) { parts.Insert(0, t.name); t = t.parent; }
            return string.Join("/", parts);
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            if (layer == -1) return;
            go.layer = layer;
            foreach (Transform child in go.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        /// <summary>
        /// Thử trích xuất mã phòng từ tên object.
        /// Ví dụ: "Room.A101", "phong_A101", "A101_mesh" → "A101"
        /// </summary>
        static string TryExtractRoomCode(string name)
        {
            // Pattern: [A-KPS]\d{1,3} (bao gồm P1, P2, P3 và S0104)
            var match = System.Text.RegularExpressions.Regex.Match(
                name.ToUpper(),
                @"\b([A-KPS]\d{1,4})\b");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}
#endif
