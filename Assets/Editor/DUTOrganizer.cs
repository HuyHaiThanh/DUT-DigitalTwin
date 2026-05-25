
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DUTOrganizer : EditorWindow
{
    [MenuItem("DUT/3. Organize - Move to Containers")]
    static void OrganizeHierarchy()
    {
        // Map: tên object (đã rename) → tên container
        var moveMap = new Dictionary<string, string>
        {
            // Buildings
            { "Building_A_main",       "Building_A" },
            { "Khu_A_group",           "Building_A" },
            { "Building_B_main",       "Building_B" },
            { "Khu_B_group",           "Building_B" },
            { "Khu_B_group2",          "Building_B" },
            { "Building_C_area",       "Building_C" },
            { "Khu_C_group",           "Building_C" },
            { "Building_D_main",       "Building_D" },
            { "Building_D_area",       "Building_D" },
            { "Khu_D_group",           "Building_D" },
            { "Building_E_main",       "Building_E" },
            { "Building_E_S_main",     "Building_E" },
            { "Building_E_2",          "Building_E" },
            { "Khu_E_group",           "Building_E" },
            { "Khu_E_group2",          "Building_E" },
            { "Building_F_main",       "Building_F" },
            { "Building_F_B_area",     "Building_F" },
            { "Khu_F_HoiTruongF",      "Building_F" },
            { "Khu_F_group2",          "Building_F" },
            { "Khu_F_group3",          "Building_F" },
            { "Building_G_main",       "Building_G" },
            { "Khu_G_group",           "Building_G" },
            { "Building_H_main",       "Building_H" },
            { "Building_H_2",          "Building_H" },
            { "Khu_H_group",           "Building_H" },
            { "Building_I_main",       "Building_I" },
            { "Building_I_P_main",     "Building_I" },
            { "Building_I_P_2",        "Building_I" },
            { "Khu_I_P_group",         "Building_I" },
            { "Building_K_main",       "Building_K" },
            { "Building_K_east",       "Building_K" },
            { "Khu_K_group",           "Building_K" },
            { "Building_S_main",       "Building_S" },
            { "Khu_S_group",           "Building_S" },
            { "Building_PFIEV_Lab",    "Building_P" },
            { "Building_Lab_Dien",     "Building_I" },

            // Landmarks
            { "Misc_ThuVien_main",         "Landmarks" },
            { "Misc_ThuVien_area1",        "Landmarks" },
            { "Misc_ThuVien_area2",        "Landmarks" },
            { "Misc_ThuVien_2",            "Landmarks" },
            { "Misc_ThuVien",              "Landmarks" },
            { "Misc_VienCoKhi_main",       "Landmarks" },
            { "Misc_VienCoKhi",            "Landmarks" },
            { "Misc_NhaThiDau_main",       "Landmarks" },
            { "Misc_NhaThiDau",            "Landmarks" },
            { "Misc_SanBongChuyen_main",   "Landmarks" },
            { "Misc_SanBongChuyen",        "Landmarks" },
            { "Misc_SanBongDa_main",       "Landmarks" },
            { "Misc_SanBongDa_2",          "Landmarks" },
            { "Misc_SanBongDa_3",          "Landmarks" },
            { "Misc_SanBongDa",            "Landmarks" },
            { "Misc_Lake_main",            "Landmarks" },
            { "Misc_Lake_2",               "Landmarks" },
            { "Misc_Lake",                 "Landmarks" },
            { "Misc_CangtinA",             "Landmarks" },
            { "Misc_NhaKho_1",             "Landmarks" },
            { "Misc_NhaKho_2",             "Landmarks" },
            { "Misc_TayNam_main",          "Landmarks" },
            { "Misc_TayNam_2",             "Landmarks" },
            { "Misc_ParkingArea",          "Landmarks" },
            { "Misc_CentralArea",          "Landmarks" },
            { "Misc_CampusArea",           "Landmarks" },

            // Workshops
            { "Workshop_North_main",  "Workshops" },
            { "Workshop_North_2",     "Workshops" },
            { "Workshop_North_3",     "Workshops" },
            { "Workshop_North_4",     "Workshops" },
            { "Workshop_North_5",     "Workshops" },
            { "Workshop_North_6",     "Workshops" },
            { "Workshop_North_7",     "Workshops" },
            { "Workshop_North_8",     "Workshops" },
            { "Workshop_Nhiet_1",     "Workshops" },
            { "Workshop_Nhiet_2",     "Workshops" },

            // KTX
            { "Misc_KTX_main",   "KyTucXa" },
            { "Misc_KTX_2",      "KyTucXa" },
            { "Misc_KTX_3",      "KyTucXa" },
            { "Misc_KTX_4",      "KyTucXa" },
            { "Misc_KTX_5",      "KyTucXa" },
            { "Misc_KTX_6",      "KyTucXa" },
            { "Misc_KTX_area",   "KyTucXa" },

            // Environment
            { "Environment_Ground",   "Environment" },
            { "Environment_Ground_2", "Environment" },
        };

        // Build lookup: name → GameObject (tìm trong toàn bộ scene)
        var allGos = FindObjectsOfType<GameObject>(true);
        var byName = new Dictionary<string, GameObject>();
        foreach (var g in allGos)
            if (!byName.ContainsKey(g.name)) byName[g.name] = g;

        // Build container lookup bằng path đầy đủ
        var containers = new Dictionary<string, Transform>
        {
            { "Building_A",  GameObject.Find("Campus/Buildings/Building_A")?.transform },
            { "Building_B",  GameObject.Find("Campus/Buildings/Building_B")?.transform },
            { "Building_C",  GameObject.Find("Campus/Buildings/Building_C")?.transform },
            { "Building_D",  GameObject.Find("Campus/Buildings/Building_D")?.transform },
            { "Building_E",  GameObject.Find("Campus/Buildings/Building_E")?.transform },
            { "Building_F",  GameObject.Find("Campus/Buildings/Building_F")?.transform },
            { "Building_G",  GameObject.Find("Campus/Buildings/Building_G")?.transform },
            { "Building_H",  GameObject.Find("Campus/Buildings/Building_H")?.transform },
            { "Building_I",  GameObject.Find("Campus/Buildings/Building_I")?.transform },
            { "Building_K",  GameObject.Find("Campus/Buildings/Building_K")?.transform },
            { "Building_P",  GameObject.Find("Campus/Buildings/Building_P")?.transform },
            { "Building_S",  GameObject.Find("Campus/Buildings/Building_S")?.transform },
            { "Landmarks",   GameObject.Find("Campus/Landmarks")?.transform },
            { "Workshops",   GameObject.Find("Campus/Workshops")?.transform },
            { "KyTucXa",     GameObject.Find("Campus/KyTucXa")?.transform },
            { "Environment", GameObject.Find("Campus/Environment")?.transform },
        };

        int moved = 0, notFound = 0, noContainer = 0;

        foreach (var kv in moveMap)
        {
            if (!byName.TryGetValue(kv.Key, out var go))
            { notFound++; continue; }

            if (!containers.TryGetValue(kv.Value, out var container) || container == null)
            { Debug.LogWarning($"Container not found: {kv.Value} for {kv.Key}"); noContainer++; continue; }

            Undo.SetTransformParent(go.transform, container, $"Move {kv.Key} to {kv.Value}");
            moved++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        Debug.Log($"[DUT Organizer] Moved:{moved} NotFound:{notFound} NoContainer:{noContainer}");
        EditorUtility.DisplayDialog("DUT Organize Done",
            $"Moved: {moved}\nNot found: {notFound}\nNo container: {noContainer}", "OK");
    }

    [MenuItem("DUT/4. Report - Xem kết quả")]
    static void Report()
    {
        var sb = new System.Text.StringBuilder();
        string[] paths = {
            "Campus/Buildings/Building_A", "Campus/Buildings/Building_B",
            "Campus/Buildings/Building_C", "Campus/Buildings/Building_D",
            "Campus/Buildings/Building_E", "Campus/Buildings/Building_F",
            "Campus/Buildings/Building_G", "Campus/Buildings/Building_H",
            "Campus/Buildings/Building_I", "Campus/Buildings/Building_K",
            "Campus/Buildings/Building_P", "Campus/Buildings/Building_S",
            "Campus/Landmarks", "Campus/Workshops", "Campus/KyTucXa", "Campus/Environment"
        };
        foreach (var p in paths)
        {
            var go = GameObject.Find(p);
            string name = p.Split('/')[System.Array.IndexOf(p.Split('/'), p.Split('/')[^1])];
            name = p.Split('/')[p.Split('/').Length - 1];
            sb.AppendLine($"{name}: {(go == null ? "NOT FOUND" : $"{go.transform.childCount} children")}");
        }
        // Đếm còn lại trong DUT_MODELS
        var dut = GameObject.Find("Campus/DUT_MODELS");
        int remaining = dut != null ? dut.transform.childCount : -1;
        sb.AppendLine($"\nDUT_MODELS: {remaining} objects chưa phân loại");
        Debug.Log(sb.ToString());
        EditorUtility.DisplayDialog("DUT Report", sb.ToString(), "OK");
    }
}
#endif
