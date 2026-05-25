using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DUT.Editor
{
    public static class DUTDepth2Exporter
    {
        [MenuItem("DUT/1B. Xuất Depth2 List")]
        public static void ExportDepth2List()
        {
            GameObject modelRoot = GameObject.Find("DUT_MODELS");
            if (modelRoot == null)
            {
                Debug.LogError("Không tìm thấy GameObject 'DUT_MODELS' trong scene.");
                return;
            }

            List<Depth2Item> items = new List<Depth2Item>();

            foreach (Transform child in modelRoot.transform)
            {
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

                items.Add(new Depth2Item
                {
                    Name = child.name,
                    Position = child.position,
                    Size = hasBounds ? combinedBounds.size : Vector3.zero,
                    ChildCount = child.childCount,
                    Area = combinedBounds.size.x * combinedBounds.size.z
                });
            }

            // Sắp xếp theo diện tích (Width x Depth) từ lớn đến nhỏ
            var sortedItems = items.OrderByDescending(i => i.Area).ToList();

            List<string> lines = new List<string>();
            for (int i = 0; i < sortedItems.Count; i++)
            {
                var item = sortedItems[i];
                lines.Add(string.Format("[{0:D3}] | {1} | POS(X={2:F0}, Z={3:F0}) | SIZE({4:F0}×{5:F0}m) | CHILDREN={6}",
                    i + 1,
                    item.Name,
                    item.Position.x,
                    item.Position.z,
                    item.Size.x,
                    item.Size.z,
                    item.ChildCount));
            }

            string filePath = "Assets/DUT_Depth2.txt";
            File.WriteAllLines(filePath, lines);
            AssetDatabase.Refresh();

            Debug.Log(string.Format("Đã xuất {0} objects con của DUT_MODELS vào {1}", sortedItems.Count, filePath));
        }

        private class Depth2Item
        {
            public string Name;
            public Vector3 Position;
            public Vector3 Size;
            public int ChildCount;
            public float Area;
        }
    }
}
