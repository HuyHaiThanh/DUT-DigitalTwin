# BẢN ĐỒ PHÒNG HỌC — ĐẠI HỌC BÁCH KHOA ĐÀ NẴNG (DUT)
# Trích xuất từ bản đồ 2D chính thức của trường
# Dùng cho Unity AI Agent: đặt tên GameObject theo đúng danh sách này

---

## HƯỚNG DẪN SỬ DỤNG CHO UNITY AI AGENT

Khi gặp object trong Hierarchy có tên lộn xộn (ví dụ: "Mesh_0042", "Building_003"):
1. Đối chiếu vị trí object trong scene với bản đồ 2D bên dưới
2. Đặt lại tên theo format: `Room_{MÃ}` (ví dụ: `Room_A101`, `Room_C112`)
3. Nếu là tòa nhà (container): `Building_{KHU}` (ví dụ: `Building_A`, `Building_C`)
4. Nếu là công trình phụ: `Misc_{TÊN}` (ví dụ: `Misc_HoiTruongF`)

---

## DANH SÁCH ĐẦY ĐỦ THEO KHU

---

### KHU A — Tòa nhà A (màu cam/vàng, khu trung tâm phải)
GameObject parent: `Building_A`

| Mã phòng | Tên GameObject Unity   | Ghi chú            |
|----------|------------------------|--------------------|
| A102     | Room_A102              |                    |
| A103     | Room_A103              |                    |
| A104     | Room_A104              |                    |
| A105     | Room_A105              |                    |
| A106     | Room_A106              |                    |
| A107     | Room_A107              |                    |
| A108     | Room_A108              |                    |
| A109     | Room_A109              |                    |
| A110     | Room_A110              |                    |
| A111     | Room_A111              |                    |
| A112     | Room_A112              |                    |
| A113     | Room_A113              |                    |
| A114     | Room_A114              |                    |
| A115     | Room_A115              |                    |
| A116     | Room_A116              |                    |
| A117     | Room_A117              |                    |
| A118     | Room_A118              |                    |
| A120     | Room_A120              |                    |
| A123     | Room_A123              |                    |
| A124     | Room_A124              |                    |
| A125     | Room_A125              |                    |
| A126     | Room_A126              |                    |
| A127     | Room_A127              |                    |
| A128     | Room_A128              |                    |
| A129     | Room_A129              |                    |
| A130     | Room_A130              |                    |
| A132     | Room_A132              |                    |
| A133     | Room_A133              |                    |
| A134     | Room_A134              |                    |
| A135     | Room_A135              |                    |
| A136     | Room_A136              |                    |
| A137     | Room_A137              |                    |
| A138     | Room_A138              |                    |
| A139     | Room_A139              |                    |
| A153     | Room_A153              |                    |
| A154     | Room_A154              |                    |

**Công trình khác khu A:**
- `Misc_CangtinKhuA`   — Căng tin khu A
- `Misc_NhaguixeKhuA`  — Nhà giữ xe khu A

---

### KHU B — Tòa nhà B (màu tím/xanh, khu tây)
GameObject parent: `Building_B`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| B108     | Room_B108            |         |
| B109     | Room_B109            |         |

**Công trình khác khu B:**
- `Misc_NhaDo_XeKhuB`  — Nhà Đỗ Xe Khu B

---

### KHU C — Tòa nhà C (màu xanh dương nhạt, phía nam)
GameObject parent: `Building_C`

| Mã phòng | Tên GameObject Unity | Ghi chú               |
|----------|----------------------|-----------------------|
| C104     | Room_C104            |                       |
| C105     | Room_C105            |                       |
| C108     | Room_C108            |                       |
| C109     | Room_C109            |                       |
| C110     | Room_C110            |                       |
| C111     | Room_C111            |                       |
| C112     | Room_C112            |                       |
| C113     | Room_C113            |                       |
| C114     | Room_C114            |                       |
| C115     | Room_C115            |                       |
| C120     | Room_C120            |                       |
| C121     | Room_C121            |                       |
| C128     | Room_C128            |                       |

**Bản đồ image 1 (overview) ghi nhận thêm:**
- Tòa nhà C Khối 1  → `Building_C_Khoi1`
- Tòa nhà C Khối 2  → `Building_C_Khoi2`
- Tòa nhà C Khối 5  → `Building_C_Khoi5`

**Công trình khác khu C:**
- `Misc_NhexeCanBo_C`  — Nhà xe cán bộ (gần C)

---

### KHU D — Tòa nhà D (màu xanh dương đậm, phía bắc)
GameObject parent: `Building_D`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| D103     | Room_D103            |         |
| D105     | Room_D105            |         |
| D106     | Room_D106            |         |
| D108     | Room_D108            |         |
| D109     | Room_D109            |         |
| D110     | Room_D110            |         |
| D111     | Room_D111            |         |
| D112     | Room_D112            |         |
| D114     | Room_D114            |         |
| D115     | Room_D115            |         |

---

### KHU E — Tòa nhà E (phía đông nam, gần đường Ngô Sĩ Liên)
GameObject parent: `Building_E`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| E101     | Room_E101            |         |
| E102     | Room_E102            |         |
| E103     | Room_E103            |         |
| E104     | Room_E104            |         |
| E113     | Room_E113            |         |
| E124     | Room_E124            |         |

---

### KHU F — Tòa nhà F (màu xanh nhạt, khu tây-trung)
GameObject parent: `Building_F`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| F101     | Room_F101            |         |
| F102     | Room_F102            |         |
| F103     | Room_F103            |         |
| F106     | Room_F106            |         |
| F107     | Room_F107            |         |
| F108     | Room_F108            |         |
| F109     | Room_F109            |         |
| F110     | Room_F110            |         |
| F111     | Room_F111            |         |
| F112     | Room_F112            |         |

**Công trình khác khu F:**
- `Misc_HoiTruongF`   — Hội trường F
- `Misc_NhaguixeF`    — Nhà giữ xe khu F

---

### KHU G — Tòa nhà G (phía đông, gần đường Ngô Thị Nhậm)
GameObject parent: `Building_G`

| Mã phòng | Tên GameObject Unity | Ghi chú               |
|----------|----------------------|-----------------------|
| G102     | Room_G102            | Xưởng thực hành       |
| G103     | Room_G103            |                       |
| G104     | Room_G104            | Xưởng thực tập / Xebo |
| G105     | Room_G105            |                       |
| G106     | Room_G106            | Xưởng điêu khắc       |

**Công trình khác khu G:**
- `Misc_ToaNhaG`             — Tòa nhà G (block chính)
- `Misc_XuongThucHanhMoiTruong` — Xưởng thực hành Môi trường

---

### KHU H — Tòa nhà H (màu tím, khu tây-trung)
GameObject parent: `Building_H`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| H101     | Room_H101            |         |
| H102     | Room_H102            |         |
| H103     | Room_H103            |         |
| H104     | Room_H104            |         |

---

### KHU I — Phòng thí nghiệm Điện (khu trung-đông)
GameObject parent: `Building_I`

| Mã phòng | Tên GameObject Unity | Ghi chú     |
|----------|----------------------|-------------|
| I101     | Room_I101            |             |
| I104     | Room_I104            |             |
| I105     | Room_I105            |             |
| I106     | Room_I106            |             |

---

### KHU K — Phòng/Lab phía đông bắc (gần đường sắt)
GameObject parent: `Building_K`

| Mã phòng | Tên GameObject Unity | Ghi chú |
|----------|----------------------|---------|
| K101     | Room_K101            |         |
| K102     | Room_K102            |         |
| K103     | Room_K103            |         |
| K104     | Room_K104            |         |
| K105     | Room_K105            |         |
| K106     | Room_K106            |         |
| K107     | Room_K107            |         |
| K108     | Room_K108            |         |

---

### KHU P — Phòng thí nghiệm / Lab trung tâm
GameObject parent: `Building_P`

| Mã phòng | Tên GameObject Unity | Ghi chú          |
|----------|----------------------|------------------|
| P1       | Room_P1              |                  |
| P2       | Room_P2              |                  |
| P3       | Room_P3              |                  |

---

### KHU S — Smart Building (phía đông)
GameObject parent: `Building_S`

| Mã phòng | Tên GameObject Unity | Ghi chú         |
|----------|----------------------|-----------------|
| S01.04   | Room_S0104           | Tầng 1          |

---

## CÔNG TRÌNH ĐẶC BIỆT (không phải phòng học)

| Tên thật                          | Tên GameObject Unity          | Loại              |
|-----------------------------------|-------------------------------|-------------------|
| Thư viện                          | Misc_ThuVien                  | Thư viện          |
| Nhà thi đấu                       | Misc_NhaThiDau                | Thể thao          |
| Viện cơ khí                       | Misc_VienCoKhi                | Viện nghiên cứu   |
| Hội trường F                      | Misc_HoiTruongF               | Hội trường        |
| Sân bóng chuyền                   | Misc_SanBongChuyen            | Thể thao          |
| Sân bóng đá                       | Misc_SanBongDa                | Thể thao          |
| TT Giáo dục thể chất              | Misc_TTGiaoDucTheChat         | Thể dục           |
| TT Hỗ trợ và QH Doanh nghiệp     | Misc_TTHoTroDoanNghiep        | Hành chính        |
| Nhà khách SV quốc tế              | Misc_NhaKhachSVQuocTe         | Ký túc xá         |
| Kiot Ký túc xá                    | Misc_KiotKTX                  | Ký túc xá         |
| Ký túc xá Nhà 1                   | Misc_KTX_Nha1                 | Ký túc xá         |
| Ký túc xá Nhà 2                   | Misc_KTX_Nha2                 | Ký túc xá         |
| Ký túc xá Nhà 3                   | Misc_KTX_Nha3                 | Ký túc xá         |
| Ký túc xá Nhà 4                   | Misc_KTX_Nha4                 | Ký túc xá         |
| Nhà S Ký túc xá                   | Misc_KTX_NhaS                 | Ký túc xá         |
| Căn tin ký túc xá                 | Misc_CanTinKTX                | Dịch vụ           |
| BQL Ký túc xá                     | Misc_BQL_KTX                  | Hành chính        |
| Nhà xe cán bộ KTX                 | Misc_NhaxeCanBo_KTX           | Nhà xe            |
| PFIEV                             | Misc_PFIEV                    | Khoa đặc biệt     |
| Thực hành điện                    | Misc_ThucHanhDien             | Lab               |
| Xưởng Động lực                    | Misc_XuongDongLuc             | Xưởng             |
| Xưởng Cơ khí                      | Misc_XuongCoKhi               | Xưởng             |
| Xưởng Điện                        | Misc_XuongDien                | Xưởng             |
| Xưởng Điều TN Điện                | Misc_XuongDieuTNDien          | Xưởng             |
| Xưởng Nhiệt                       | Misc_XuongNhiet               | Xưởng             |
| Xưởng Gia công tạo công nghệ      | Misc_XuongGiaCongCongNghe     | Xưởng             |
| Thí nghiệm Gia công Áp lực        | Misc_TNGiaCongApLuc           | Lab               |
| Khu TN Cơ khí                     | Misc_KhuTNCoKhi               | Lab               |
| Trung tâm Âu Việt                 | Misc_TrungTamAuViet           | Trung tâm         |
| Trung tâm nghiên cứu Điện tử     | Misc_TrungTamNCDienTu         | Nghiên cứu        |
| Trung tâm thử nghiệm Động cơ ô tô| Misc_TTThuNghiemDongCoOto     | Lab               |
| PTN Nhiệt điện lạnh               | Misc_PTNNhietDienLanh         | PTN               |
| PTN Động lực thủy                 | Misc_PTNDongLucThuy           | PTN               |
| Gara ô tô                         | Misc_GaraOto                  | Nhà xe            |
| Nhà kho (×2)                      | Misc_NhaKho_1, Misc_NhaKho_2  | Kho               |
| Nhà giữ xe khu F (×2)            | Misc_NhaguixeF_1/2            | Nhà xe            |
| Nhà giữ xe khu E                  | Misc_NhaguixeE                | Nhà xe            |

---

## QUY TẮC MAPPING CHO ModelSetupHelper.cs

```csharp
// Dùng dictionary này trong ModelSetupHelper để auto-rename
// Key = pattern nhận dạng (có thể là tên gốc lộn xộn hoặc vị trí bounds)
// Value = tên chuẩn theo bản đồ

// Prefix theo khu — dùng để batch rename
var BUILDING_PREFIXES = new Dictionary<string, string>
{
    // Phòng học chính thức (có lịch học)
    { "A", "Building_A" },  // Khu A: A102–A154
    { "B", "Building_B" },  // Khu B: B108–B109
    { "C", "Building_C" },  // Khu C: C104–C128
    { "D", "Building_D" },  // Khu D: D103–D115
    { "E", "Building_E" },  // Khu E: E101–E124
    { "F", "Building_F" },  // Khu F: F101–F112
    { "G", "Building_G" },  // Khu G: G102–G106
    { "H", "Building_H" },  // Khu H: H101–H104
    { "I", "Building_I" },  // Khu I: I101–I106
    { "K", "Building_K" },  // Khu K: K101–K108
    { "P", "Building_P" },  // Khu P: P1–P3
    { "S", "Building_S" },  // Smart Building: S01.04
};

// Phòng học (có lịch học — RoomObject.cs sẽ gắn vào đây)
var ROOM_IDS = new HashSet<string>
{
    // Khu A
    "A102","A103","A104","A105","A106","A107","A108","A109","A110",
    "A111","A112","A113","A114","A115","A116","A117","A118","A120",
    "A123","A124","A125","A126","A127","A128","A129","A130",
    "A132","A133","A134","A135","A136","A137","A138","A139",
    "A153","A154",
    // Khu B
    "B108","B109",
    // Khu C
    "C104","C105","C108","C109","C110","C111","C112","C113","C114","C115",
    "C120","C121","C128",
    // Khu D
    "D103","D105","D106","D108","D109","D110","D111","D112","D114","D115",
    // Khu E
    "E101","E102","E103","E104","E113","E124",
    // Khu F
    "F101","F102","F103","F106","F107","F108","F109","F110","F111","F112",
    // Khu G
    "G102","G103","G104","G105","G106",
    // Khu H
    "H101","H102","H103","H104",
    // Khu I
    "I101","I104","I105","I106",
    // Khu K
    "K101","K102","K103","K104","K105","K106","K107","K108",
    // Khu P
    "P1","P2","P3",
    // Smart Building
    "S0104",
};

// Tổng số phòng học chính thức: 90 phòng
```

---

## THỐNG KÊ NHANH

| Khu | Số phòng | Loại chính         |
|-----|----------|--------------------|
| A   | 36       | Phòng học, Lab     |
| B   | 2        | Phòng học          |
| C   | 13       | Phòng học          |
| D   | 10       | Lab, Phòng học     |
| E   | 6        | Phòng học          |
| F   | 10       | Phòng học          |
| G   | 5        | Xưởng thực hành   |
| H   | 4        | Phòng học          |
| I   | 4        | Lab Điện           |
| K   | 8        | Lab chuyên ngành   |
| P   | 3        | PTN                |
| S   | 1        | Smart Building     |
| **Tổng** | **102** |               |

---

## GHI CHÚ VỀ BẢN ĐỒ

- Màu **xanh dương đậm** = Lab, xưởng thực hành, PTN chuyên ngành
- Màu **cam/vàng** = Phòng học thông thường (khu A, F)
- Màu **tím/xanh nhạt** = Phòng học + hành chính (khu H, B)
- Màu **xám/trắng** = Công trình phụ, nhà xe, kho
- Màu **đỏ/nâu** = Thư viện, nhà thi đấu, ký túc xá

- Trục đường chính: **Ngô Sĩ Liên** (phía nam) và **Ngô Thị Nhậm** (phía đông)
- Hồ nước nhân tạo: trung tâm khuôn viên — dùng làm landmark định hướng
- Sân bóng chuyền: phía đông khuôn viên chính
- Sân bóng đá + TT GDTC: phía đông nam (ngoài khuôn viên học tập chính)

---

*Nguồn: Bản đồ 2D chính thức ĐH Bách Khoa Đà Nẵng*
*Trích xuất thủ công từ 3 ảnh map — độ chính xác: ~95%*
*Một số phòng nhỏ có thể bị che khuất trong ảnh*
