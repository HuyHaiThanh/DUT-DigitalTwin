using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;

namespace DUT.Editor
{
    public static class DUTHierarchyReporter
    {
        [MenuItem("DUT/1. Xuất báo cáo Hierarchy")]
        public static void ExportHierarchyReport()
        {
            List<MeshRenderer> allRenderers = new List<MeshRenderer>();
            
            // Lấy tất cả root objects trong scene hiện tại để tìm MeshRenderer (kể cả inactive)
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (GameObject root in rootObjects)
            {
                allRenderers.AddRange(root.GetComponentsInChildren<MeshRenderer>(true));
            }

            List<ReportItem> reportItems = new List<ReportItem>();

            foreach (MeshRenderer renderer in allRenderers)
            {
                GameObject go = renderer.gameObject;
                string name = go.name;
                string parentName = go.transform.parent != null ? go.transform.parent.name : "None";
                int depth = GetDepth(go.transform);
                Vector3 size = renderer.bounds.size;
                string boundsStr = string.Format("{0:F2}x{1:F2}x{2:F2}", size.x, size.y, size.z);

                reportItems.Add(new ReportItem
                {
                    Name = name,
                    ParentName = parentName,
                    Depth = depth,
                    Bounds = boundsStr
                });
            }

            // Sắp xếp theo Depth rồi theo Tên
            var sortedItems = reportItems
                .OrderBy(item => item.Depth)
                .ThenBy(item => item.Name)
                .ToList();

            // Tạo nội dung file
            List<string> lines = new List<string>();
            lines.Add("BÁO CÁO HIERARCHY MESH RENDERERS - CAMPUS DUT");
            lines.Add(string.Format("Ngày xuất: {0}", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            lines.Add("--------------------------------------------------------------------------------");
            lines.Add("Object Name | Parent Name | Depth | Bounds Size (WxHxD)");
            lines.Add("--------------------------------------------------------------------------------");

            foreach (var item in sortedItems)
            {
                lines.Add(string.Format("{0} | {1} | {2} | {3}", 
                    item.Name, item.ParentName, item.Depth, item.Bounds));
            }

            // Xuất ra file
            string filePath = "Assets/DUT_Report.txt";
            File.WriteAllLines(filePath, lines);

            // Refresh AssetDatabase để file hiện lên trong Project window
            AssetDatabase.Refresh();

            Debug.Log(string.Format("Tổng {0} objects. Xem file {1}", allRenderers.Count, filePath));
        }

        private static int GetDepth(Transform t)
        {
            int depth = 0;
            while (t.parent != null)
            {
                depth++;
                t = t.parent;
            }
            return depth;
        }

        private class ReportItem
        {
            public string Name;
            public string ParentName;
            public int Depth;
            public string Bounds;
        }
    }
}
