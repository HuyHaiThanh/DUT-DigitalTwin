using System.Collections.Generic;

namespace DUT.Data
{
    /// <summary>
    /// Danh sách phòng tĩnh của trường, dùng để hiển thị toàn bộ grid lịch.
    /// Phòng được nhóm theo tòa/khu, hiển thị theo thứ tự này trên Schedule screen.
    /// </summary>
    public static class RoomRegistry
    {
        public struct RoomGroup
        {
            public string Id;       // dùng nội bộ
            public string Label;    // tiêu đề nhóm trên UI
            public string[] Rooms;  // danh sách roomId khớp với phong= trong schedule_data
        }

        // Tạo dãy roomId dạng "B101"→"B109": prefix + floor + room 2 chữ số
        static string[] Seq(string prefix, int floor, int from, int to)
        {
            var list = new List<string>();
            for (int i = from; i <= to; i++)
                list.Add($"{prefix}{floor}{i:D2}");
            return list.ToArray();
        }

        // Tạo dãy roomId dạng "E1.101"→"E1.107": prefix + "." + floor + room 2 chữ số
        static string[] SeqDot(string prefix, int floor, int from, int to)
        {
            var list = new List<string>();
            for (int i = from; i <= to; i++)
                list.Add($"{prefix}.{floor}{i:D2}");
            return list.ToArray();
        }

        static string[] Merge(params string[][] arrays)
        {
            var all = new List<string>();
            foreach (var a in arrays) all.AddRange(a);
            return all.ToArray();
        }

        public static readonly RoomGroup[] Groups =
        {
            new() {
                Id = "B", Label = "Khu B",
                Rooms = Merge(
                    Seq("B", 1, 1, 9),
                    Seq("B", 2, 1, 9),
                    Seq("B", 3, 1, 6))
            },
            new() {
                Id = "D", Label = "Khu D",
                Rooms = Merge(
                    Seq("D", 1, 1, 15),
                    Seq("D", 2, 1, 15))
            },
            new() {
                Id = "E1", Label = "Khu E — Tòa E1",
                Rooms = Merge(
                    SeqDot("E1", 1, 1, 7),
                    SeqDot("E1", 2, 1, 7))
            },
            new() {
                Id = "E2", Label = "Khu E — Tòa E2",
                Rooms = Merge(
                    SeqDot("E2", 1, 1, 6),
                    SeqDot("E2", 2, 1, 6),
                    SeqDot("E2", 3, 1, 6),
                    SeqDot("E2", 4, 1, 6))
            },
            new() {
                Id = "F", Label = "Khu F",
                Rooms = Merge(
                    Seq("F", 1, 1, 10),
                    Seq("F", 2, 1, 10),
                    Seq("F", 3, 1, 10),
                    Seq("F", 4, 1,  9))
            },
            new() {
                Id = "H", Label = "Khu H",
                Rooms = Merge(
                    Seq("H", 1, 1, 8),
                    Seq("H", 2, 1, 8),
                    Seq("H", 3, 1, 8),
                    Seq("H", 4, 1, 2))
            },
            new() {
                Id = "PFIEV", Label = "PFIEV (Khu K)",
                Rooms = new[] { "P1","P2","P3","P4","P5","P6","P7" }
            },
            new() {
                Id = "K-TN", Label = "TN Cơ khí (Khu K)",
                Rooms = Seq("K", 1, 1, 8)
            },
        };

        static int _totalRooms = -1;
        public static int TotalRooms
        {
            get
            {
                if (_totalRooms < 0)
                {
                    _totalRooms = 0;
                    foreach (var g in Groups) _totalRooms += g.Rooms.Length;
                }
                return _totalRooms;
            }
        }
    }
}
