using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DUT.Editor
{
    public static class FindLargeObjects
    {
        [MenuItem("DUT/1D. Tìm objects lớn")]
        public static void ExportRemainingList()
        {
            GameObject modelRoot = GameObject.Find("DUT_MODELS");
            if (modelRoot == null)
            {
                Debug.LogError("Không tìm thấy GameObject 'DUT_MODELS' trong scene.");
                return;
            }

            List<string> lines = new List<string>();
            foreach (Transform child in modelRoot.transform)
            {
                string name = child.name;
                // Bỏ qua các object đã được đặt tên Building_ hoặc Misc_
                if (name.StartsWith("Misc_") || name.StartsWith("Building_")) continue;

                // Tính toán bounds tổng cho toàn bộ cây con
                Bounds combinedBounds = new Bounds();
                bool hasBounds = false;
                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                
                foreach (var r in renderers)
                {
                    if (!hasBounds)
                    {
                        combinedBounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(r.bounds);
                    }
                }

                if (hasBounds)
                {
                    float width = combinedBounds.size.x;
                    float depth = combinedBounds.size.z;

                    // Chỉ lấy object có W > 20m và D > 20m
                    if (width > 20f && depth > 20f)
                    {
                        lines.Add(string.Format("{0} | POS(X={1:F0}, Z={2:F0}) | SIZE({3:F0}×{4:F0})",
                            name, child.position.x, child.position.z, width, depth));
                    }
                }
            }

            string filePath = "Assets/DUT_Remaining.txt";
            File.WriteAllLines(filePath, lines);
            AssetDatabase.Refresh();

            Debug.Log("Đã xuất danh sách objects lớn vào: " + filePath);
        }
    }
}
