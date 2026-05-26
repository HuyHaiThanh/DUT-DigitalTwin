"""
Enriched scraper: kết hợp PageLopHPKH (ma_lhp, slsv) + PageLichCT (actual schedule)
Output: Assets/StreamingAssets/schedule_data.json

Chạy: python -X utf8 scraper_dut.py [--week N]
  --week N  : chọn tuần N (nếu bỏ qua → dùng tuần hiện tại trên trang)
"""
import requests
from bs4 import BeautifulSoup, NavigableString, Tag
import json, re, sys
from datetime import datetime
from collections import defaultdict, Counter

BASE_LHPKH = "https://cb.dut.udn.vn/PageLopHPKH.aspx"
BASE_LICHCT = "https://cb.dut.udn.vn/PageLichCT.aspx"
OUT_FILE    = "Assets/StreamingAssets/schedule_data.json"
HEADERS     = {"User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"}

DAY_MAP  = {"T2": 2, "T3": 3, "T4": 4, "T5": 5, "T6": 6, "T7": 7, "CN": 8}
SLOT_PAT = re.compile(
    r'^(\d+[-–]\d+),\s*(.+?)\s*-\s*(Coi thi|Bù|Học|Thi lại|Kiểm tra)?\s*$'
)

session = requests.Session()


# ═══════════════════════════════════════════════════════════════
# HELPERS
# ═══════════════════════════════════════════════════════════════

def tiet_bounds(tiet_str: str) -> tuple:
    """'1-2' → (1, 2),  '3' → (3, 3)"""
    s = tiet_str.replace('–', '-').split('-')
    try:
        start = int(s[0].strip())
        end   = int(s[1].strip()) if len(s) > 1 else start
        return start, end
    except (ValueError, IndexError):
        return 1, 1


def tiets_overlap(a_str: str, b_str: str) -> bool:
    as_, ae = tiet_bounds(a_str)
    bs_, be = tiet_bounds(b_str)
    return as_ <= be and bs_ <= ae


def parse_tkb_text(raw: str) -> list:
    """
    Parse TKB cell text từ PageLopHPKH.
    Formats gặp:
      "T5,1-2,E2.406"
      "T5 1-2 E2.406"
      "T5,1-2,E2.406\nT3,3-4,C1.101"   (nhiều dòng)
      "T5,1-5,E2.406;T3,3-5,F208"       (phân cách bằng ;)
    Trả về: list of {thu, tiet, phong}
    """
    entries = []
    if not raw:
        return entries
    for part in re.split(r'[\n;]+', raw):
        part = part.strip()
        if not part:
            continue
        m = re.match(
            r'(T[2-7]|CN)[,\s]+(\d{1,2}(?:[-–]\d{1,2})?)[,\s]+([A-Z][A-Z0-9.\-]*)',
            part, re.IGNORECASE
        )
        if not m:
            continue
        thu_str = m.group(1).upper()
        tiet    = m.group(2).replace('–', '-').strip()
        phong   = m.group(3).strip().upper()
        thu     = DAY_MAP.get(thu_str, 0)
        if thu > 0 and phong:
            entries.append({'thu': thu, 'tiet': tiet, 'phong': phong})
    return entries


# ═══════════════════════════════════════════════════════════════
# PHASE A — PageLopHPKH
# ═══════════════════════════════════════════════════════════════

# Keyword sets để nhận dạng cột từ header (khớp với tên tiếng Việt)
_COL_KEYWORDS = {
    'ma_lhp':     ['mã lớp', 'ma lhp', 'mã lhp', 'lớp hp', 'lophp'],
    'ten_hp':     ['tên lớp', 'tên hp', 'ten hp'],
    'giang_vien': ['giảng viên', 'giangvien', 'giang vien'],
    'tkb':        ['thời khóa', 'tkb', 'lịch học'],
    'slsv':       ['slsv', 'sl sv', 'số sv', 'sĩ số', 'số sinh viên'],
    'lich_trinh': ['lịch trình', 'tuần học', 'tuan hoc'],
}

def _detect_cols(header_row) -> dict:
    """
    Nhận dạng cột có xử lý colspan: trả về data-column-index thực tế.
    Bảng PageLopHPKH có 2 header rows; hàm này chỉ dùng row đầu tiên.
    """
    col_map = {}
    offset = 0
    for cell in header_row.find_all(['th', 'td']):
        cs   = int(cell.get('colspan', 1))
        txt  = cell.get_text(separator=' ', strip=True).lower()
        for field, keywords in _COL_KEYWORDS.items():
            if field not in col_map and any(k in txt for k in keywords):
                col_map[field] = offset   # first data-col spanned by this header
        offset += cs
    return col_map


def _cell_text(cells, key, col_map) -> str:
    idx = col_map.get(key, -1)
    if idx < 0 or idx >= len(cells):
        return ''
    return cells[idx].get_text(separator='\n', strip=True)


def fetch_lhpkh() -> list:
    """
    Trả về list of:
      {ma_lhp, ten_hp, giang_vien, slsv, lich_trinh, tkb_slots: [{thu,tiet,phong}]}
    """
    print(f"[A1] GET {BASE_LHPKH} ...")
    r = session.get(BASE_LHPKH, headers=HEADERS, timeout=60)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    print(f"     Response: {r.status_code}, {len(r.content):,} bytes")

    # Tìm bảng chính — thử theo id trước, fallback → bảng lớn nhất
    table = None
    for tid in ['LHPKH_Grid', 'gvLopHP', 'GridView1', 'DataGrid1', 'gv_LopHPKH']:
        table = soup.find('table', id=tid)
        if table:
            print(f"     Found table: #{tid}")
            break
    if not table:
        tables = soup.find_all('table')
        if tables:
            table = max(tables, key=lambda t: len(t.find_all('tr')))
            print(f"     Using largest table ({len(table.find_all('tr'))} rows)")
    if not table:
        print("ERROR: Không tìm thấy bảng dữ liệu PageLopHPKH")
        return []

    rows = table.find_all('tr')
    if len(rows) < 2:
        print("ERROR: Bảng PageLopHPKH trống")
        return []

    # Nhận dạng cột từ header (xử lý colspan)
    col_map = _detect_cols(rows[0])
    print(f"     Column map: {col_map}")

    # Fallback — layout thực tế (0-indexed data cols):
    # TT(0), MaLHP(1), TenLHP(2), SoTC(3),
    # GV-PhụTrachChinh(4), GV-CongTac(5),
    # TKB-ThuTietPhong(6), TKB-Tuan(7),
    # SLSV(8), LichTrinh(9), ...
    if 'ma_lhp' not in col_map:
        col_map = {'ma_lhp': 1, 'ten_hp': 2, 'giang_vien': 4,
                   'tkb': 6, 'slsv': 8, 'lich_trinh': 9}
        print(f"     Using fallback column map: {col_map}")

    # Bảng có 2 header rows — bỏ qua rows[0] & rows[1], dữ liệu bắt đầu từ rows[2]
    data_start = 1
    if len(rows) > 1:
        r1_cells = rows[1].find_all('td')
        if r1_cells and not r1_cells[0].get_text(strip=True).isdigit():
            data_start = 2

    records = []
    for row in rows[data_start:]:
        cells = row.find_all('td')
        if not cells:
            continue
        ma_lhp     = _cell_text(cells, 'ma_lhp',     col_map)
        ten_hp     = _cell_text(cells, 'ten_hp',     col_map)
        giang_vien = _cell_text(cells, 'giang_vien', col_map)
        tkb_raw    = _cell_text(cells, 'tkb',        col_map)
        lich_trinh = _cell_text(cells, 'lich_trinh', col_map)
        slsv_raw   = _cell_text(cells, 'slsv',       col_map)

        if not ma_lhp and not ten_hp:
            continue  # dòng pager / rỗng

        slsv = 0
        try:
            m = re.search(r'\d+', slsv_raw)
            if m:
                slsv = int(m.group())
        except (ValueError, AttributeError):
            pass

        records.append({
            'ma_lhp':     ma_lhp,
            'ten_hp':     ten_hp,
            'giang_vien': giang_vien,
            'slsv':       slsv,
            'lich_trinh': lich_trinh,
            'tkb_slots':  parse_tkb_text(tkb_raw),
        })

    print(f"[A2] Parsed {len(records)} LHP records")

    # Diagnostic: log mẫu
    if records:
        sample = records[0]
        print(f"     Sample: ma_lhp={sample['ma_lhp']!r}, ten_hp={sample['ten_hp']!r}, "
              f"slsv={sample['slsv']}, tkb_slots={sample['tkb_slots'][:2]}")

    return records


def build_lhp_lookup(records: list) -> dict:
    """
    Lookup: (thu, phong) → list of {ma_lhp, ten_hp, giang_vien, slsv, tiet_s, tiet_e}
    Phong được normalize (uppercase, strip spaces).
    """
    lookup = defaultdict(list)
    skipped = 0
    for rec in records:
        for slot in rec['tkb_slots']:
            phong = slot['phong'].upper().replace(' ', '')
            if not phong:
                skipped += 1
                continue
            ts, te = tiet_bounds(slot['tiet'])
            lookup[(slot['thu'], phong)].append({
                'ma_lhp':     rec['ma_lhp'],
                'ten_hp':     rec['ten_hp'],
                'giang_vien': rec['giang_vien'],
                'slsv':       rec['slsv'],
                'tiet_s':     ts,
                'tiet_e':     te,
            })
    print(f"[A3] Built LHP lookup: {len(lookup)} (thu, phong) keys  ({skipped} slots skipped)")
    return dict(lookup)


# ═══════════════════════════════════════════════════════════════
# PHASE B — PageLichCT
# ═══════════════════════════════════════════════════════════════

def parse_cell(cell):
    """Parse 1 ô LCTT_Grid → list of slot dicts"""
    slots = []
    nodes = [n for n in cell.children
             if (isinstance(n, NavigableString) and n.strip())
             or (isinstance(n, Tag) and n.name == 'b')]
    current = None
    for node in nodes:
        if isinstance(node, NavigableString):
            text = node.strip()
            m = SLOT_PAT.match(text)
            if m:
                if current:
                    slots.append(current)
                loai = m.group(3) or 'Học'
                current = {
                    'tiet':       m.group(1).replace('–', '-'),
                    'phong':      m.group(2).strip().upper(),
                    'loai':       loai,
                    'ten_mon':    '',
                    'giang_vien': '',
                }
            elif current:
                current['ten_mon'] = text
        elif isinstance(node, Tag) and node.name == 'b':
            gv = node.get_text(strip=True)
            if gv and current:
                current['giang_vien'] = gv
    if current:
        slots.append(current)

    # Dedup: cùng (tiết, phòng, loại, môn) nhưng nhiều giám thị → gộp tên GV
    seen, result = {}, []
    for s in slots:
        key = (s['tiet'], s['phong'], s['loai'], s['ten_mon'])
        if key in seen:
            ex = seen[key]
            if s['giang_vien'] and s['giang_vien'] not in ex['giang_vien']:
                ex['giang_vien'] += ', ' + s['giang_vien']
        else:
            seen[key] = s
            result.append(s)
    return result


def fetch_lichct(target_week: str = None) -> tuple:
    """
    Trả về (entries, tuan_raw).
    target_week: nếu truyền vào (giá trị option value) sẽ POST để chọn tuần đó.
    """
    print(f"\n[B1] GET {BASE_LICHCT} ...")
    r = session.get(BASE_LICHCT, headers=HEADERS, timeout=60)
    r.raise_for_status()
    soup = BeautifulSoup(r.text, "html.parser")
    print(f"     Response: {r.status_code}, {len(r.content):,} bytes")

    # Tuần đang hiển thị
    tuan_sel = soup.find("select", {"name": "_ctl0:MainContent:LCTT_cboTuan"})
    tuan_raw = ""
    selected_value = None
    if tuan_sel:
        for opt in tuan_sel.find_all("option"):
            if opt.get("selected"):
                tuan_raw = opt.text.strip()
                selected_value = opt.get("value", "")
        if not tuan_raw:
            for opt in tuan_sel.find_all("option"):
                if opt.get("value"):
                    tuan_raw = opt.text.strip()
                    selected_value = opt.get("value", "")
                    break
    print(f"     Tuần: {tuan_raw}")

    # Nếu cần chọn tuần cụ thể → POST
    if target_week and selected_value != target_week:
        vsf   = soup.find("input", {"name": "__VIEWSTATE"})
        evvf  = soup.find("input", {"name": "__EVENTVALIDATION"})
        vsgen = soup.find("input", {"name": "__VIEWSTATEGENERATOR"})
        data  = {
            "__EVENTTARGET":         "_ctl0:MainContent:LCTT_cboTuan",
            "__EVENTARGUMENT":       "",
            "__VIEWSTATE":           vsf["value"]   if vsf   else "",
            "__EVENTVALIDATION":     evvf["value"]  if evvf  else "",
            "__VIEWSTATEGENERATOR":  vsgen["value"] if vsgen else "",
            "_ctl0:MainContent:LCTT_cboTuan": target_week,
        }
        print(f"[B1b] POST to select week {target_week!r} ...")
        r = session.post(BASE_LICHCT, data=data, headers=HEADERS, timeout=60)
        r.raise_for_status()
        soup = BeautifulSoup(r.text, "html.parser")
        for opt in (tuan_sel or soup.find("select", {"name": "_ctl0:MainContent:LCTT_cboTuan"}) or []):
            if hasattr(opt, 'get') and opt.get("selected"):
                tuan_raw = opt.text.strip()
        print(f"     Tuần sau khi chọn: {tuan_raw}")

    table = soup.find("table", id="LCTT_Grid")
    if not table:
        print("ERROR: Không tìm thấy LCTT_Grid")
        return [], tuan_raw

    rows = table.find_all("tr", recursive=False)
    if len(rows) < 2:
        return [], tuan_raw

    hdr_cells = rows[0].find_all(["th", "td"])
    columns = []
    for c in hdr_cells:
        txt = c.get_text(strip=True)
        m = re.match(r"(T\d|CN)\s*\((\d{2}/\d{2}/\d{4})\)", txt)
        if m:
            columns.append((DAY_MAP.get(m.group(1), 0), m.group(2), m.group(1)))
        else:
            columns.append((0, "", txt))
    print(f"     Ngày: {[f'{n}({t})' for t,n,_ in columns if t]}")

    entries = []
    for data_row in rows[1:]:
        cells = data_row.find_all("td", recursive=False)
        for ci, cell in enumerate(cells):
            if ci >= len(columns):
                break
            thu_num, ngay_str, _ = columns[ci]
            if thu_num == 0:
                continue
            for slot in parse_cell(cell):
                slot['thu']  = thu_num
                slot['ngay'] = ngay_str
                entries.append(slot)

    print(f"[B2] Parsed {len(entries)} slot entries")
    loai_cnt = Counter(e['loai'] for e in entries)
    print(f"     Loại: {dict(loai_cnt.most_common())}")
    return entries, tuan_raw


# ═══════════════════════════════════════════════════════════════
# PHASE C — Enrich
# ═══════════════════════════════════════════════════════════════

def enrich_entries(entries: list, lhp_lookup: dict) -> list:
    matched = unmatched = 0
    for slot in entries:
        slot['ma_lhp'] = ''
        slot['slsv']   = 0

        # Coi thi / Thi lại / Kiểm tra không có trong TKB lớp HP → bỏ qua
        if slot.get('loai') in ('Coi thi', 'Thi lại', 'Kiểm tra'):
            continue

        phong = slot['phong'].upper().replace(' ', '')
        key   = (slot['thu'], phong)
        candidates = lhp_lookup.get(key, [])
        if not candidates:
            unmatched += 1
            continue

        ts, te = tiet_bounds(slot['tiet'])
        for cand in candidates:
            if cand['tiet_s'] <= te and ts <= cand['tiet_e']:
                slot['ma_lhp'] = cand['ma_lhp']
                slot['slsv']   = cand['slsv']
                # Bổ sung ten_mon / giang_vien nếu rỗng
                if not slot.get('ten_mon'):
                    slot['ten_mon'] = cand['ten_hp']
                if not slot.get('giang_vien'):
                    slot['giang_vien'] = cand['giang_vien']
                matched += 1
                break
        else:
            unmatched += 1

    print(f"[C1] Enrichment: {matched} matched, {unmatched} unmatched (Coi thi không tính)")
    return entries


# ═══════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════

# Parse --week argument
target_week = None
if '--week' in sys.argv:
    try:
        target_week = sys.argv[sys.argv.index('--week') + 1]
    except IndexError:
        pass

# Phase A: PageLopHPKH
lhp_records = fetch_lhpkh()
lhp_lookup  = build_lhp_lookup(lhp_records) if lhp_records else {}

# Phase B: PageLichCT
entries, tuan_raw = fetch_lichct(target_week)

# Phase C: Enrich
if lhp_lookup:
    entries = enrich_entries(entries, lhp_lookup)
else:
    print("[C1] Bỏ qua enrich (không có dữ liệu LHP)")
    for slot in entries:
        slot.setdefault('ma_lhp', '')
        slot.setdefault('slsv', 0)

# Phase D: Output
if not entries:
    print("\n[D1] WARNING: 0 entries parsed — giữ nguyên file cũ, không overwrite.")
    sys.exit(0)

output = {
    "scraped_at": datetime.now().strftime("%Y-%m-%dT%H:%M:%S"),
    "source":     BASE_LICHCT,
    "source_lhp": BASE_LHPKH,
    "format":     "slot_per_day_enriched",
    "tuan_raw":   tuan_raw,
    "count":      len(entries),
    "data":       entries,
}

with open(OUT_FILE, "w", encoding="utf-8") as f:
    json.dump(output, f, ensure_ascii=False, indent=2)

print(f"\n[D1] Saved → {OUT_FILE}  ({len(entries)} entries)")
print("\n=== SAMPLE (4 entries) ===")
for e in entries[:4]:
    print(json.dumps(e, ensure_ascii=False))
