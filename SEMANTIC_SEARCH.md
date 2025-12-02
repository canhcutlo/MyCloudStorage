# Semantic Search Feature

## Overview
The application now includes **Semantic Search** with Vietnamese language support, enabling intelligent file and folder discovery even when search terms don't exactly match file names.

## Key Features

### 1. Vietnamese Accent Normalization
Automatically removes Vietnamese accents for matching:
- **Search:** `"Báo cáo tài chính"`
- **Matches:** `"Bao_cao_tai_chinh.pdf"`, `"bao-cao-tai-chinh.docx"`

### 2. Common Abbreviations
Understands Vietnamese business abbreviations:
- **T10, T.10, Thang 10** → All match "tháng 10" (October)
- **BC** → "báo cáo" (report)
- **TC** → "tài chính" (financial)
- **HD** → "hợp đồng" (contract)
- **KH** → "kế hoạch" (plan)
- **Q1, Q2, Q3, Q4** → Quarters

### 3. Separator Normalization
Handles various file naming conventions:
- Underscores: `Bao_cao_tai_chinh.pdf`
- Hyphens: `Bao-cao-tai-chinh.pdf`
- Dots: `Bao.cao.tai.chinh.pdf`
- Mixed: `Bao_cao-tai.chinh.pdf`

### 4. Number Matching
Prioritizes matches with same numbers:
- Search: `"báo cáo tháng 10"`
- Best match: `"Bao_cao_T10.pdf"` (number 10 matches)
- Also finds: `"Bao_cao_Q4.pdf"` (contextually related)

### 5. Token-Based Matching
Uses intelligent word tokenization:
- Breaks filenames into meaningful tokens
- Compares tokens semantically
- Ranks results by relevance

## Search Examples

| Search Query | Matches |
|--------------|---------|
| `Báo cáo tài chính tháng 10` | `Bao_cao_tai_chinh_T10.pdf`<br/>`BaoCaoTaiChinh_Thang10.docx`<br/>`BC_TC_T10_2024.xlsx` |
| `hợp đồng thuê văn phòng` | `Hop_dong_thue_van_phong.pdf`<br/>`HD_ThueVP_2024.docx`<br/>`hopdong-thue-VP.pdf` |
| `kế hoạch kinh doanh Q1` | `Ke_hoach_kinh_doanh_Q1.pdf`<br/>`KH_KD_Quy1_2024.xlsx`<br/>`KeHoachKinhDoanh_Q1.pptx` |
| `đơn vị hành chính` | `Don_vi_hanh_chinh.pdf`<br/>`DV_HC.xlsx`<br/>`donvi-hanhchinh.docx` |

## How It Works

### Similarity Score Calculation
1. **Text Normalization:** Remove accents, convert to lowercase
2. **Tokenization:** Split text into words using separators
3. **Abbreviation Expansion:** Expand known abbreviations
4. **Jaccard Similarity:** Calculate token overlap percentage
5. **Substring Bonus:** +30% if one string contains another
6. **Number Bonus:** +20% if numbers match

### Threshold
- **Minimum Score:** 0.4 (40% similarity)
- Items scored < 0.4 are filtered out
- Results sorted by score (highest first)

## Usage

### Basic Search
```
Navigate to: Storage → Search
Enter: "Báo cáo tài chính"
Result: All financial reports, regardless of accent usage
```

### Filtered Search
```
1. Enter search term
2. Select filter: "Files Only" or "Folders Only"
3. Click Search
```

### Advanced Examples

**Find all October reports:**
```
Search: "tháng 10" or "T10" or "thang 10"
```

**Find contracts:**
```
Search: "hợp đồng" or "HD" or "hop dong"
```

**Find Q1 planning documents:**
```
Search: "kế hoạch Q1" or "KH Q1" or "ke hoach quy 1"
```

## Technical Implementation

### Services
- **SemanticSearchService:** Core semantic matching engine
- **StorageService:** Integrated with search functionality

### Algorithm Features
- Jaccard similarity coefficient
- Token-based comparison
- Vietnamese character mapping (60+ character mappings)
- 25+ common business abbreviations
- Flexible number matching

### Performance
- Searches all user files in memory
- Returns top 100 most relevant results
- Optimized for datasets up to 10,000 files

## Supported Abbreviations

| Abbreviation | Full Form | English |
|--------------|-----------|---------|
| T, Thang | Tháng | Month |
| Q, Quy | Quý | Quarter |
| BC | Báo cáo | Report |
| TC | Tài chính | Financial |
| HD | Hợp đồng | Contract |
| KH | Kế hoạch | Plan |
| VP | Văn phòng | Office |
| CT | Công ty | Company |
| GD | Giám đốc | Director |
| NS | Nhân sự | Human Resources |
| KD | Kinh doanh | Business |
| DV | Đơn vị | Unit/Department |
| CN | Chi nhánh | Branch |

## Future Enhancements
- Machine learning-based relevance tuning
- User feedback on search results
- Search history and suggestions
- Multi-language support (English, French, etc.)
- Fuzzy matching for typos
- Context-aware search
