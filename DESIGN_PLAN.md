# Design Plan — Glassmorphism + Adapted Patterns cho 5 Form

> Sinh ra từ `/ui-ux-pro-max` queries cho từng vai trò.
> Skill thiên về web (React/Tailwind), tôi đã adapt sang WinForms + Montserrat.

---

## Nguyên tắc chung

Khác với LoginForm (splash, glassmorphism nặng), các form work là **công cụ làm việc nhiều giờ** — không thể dùng full Liquid Glass vì:
- Performance: backdrop blur trên DataGridView gây lag
- A11y: text-on-glass khó đọc kéo dài
- Eye strain: gradient backdrop làm mỏi mắt

**Chiến lược 3 lớp:**

```
┌─────────────────────────────────────────────────────────────┐
│ Surface (Solid clean white)  ← chỗ user thao tác chính     │
│   - DataGridView, form fields, content area                │
├─────────────────────────────────────────────────────────────┤
│ Accents (Glass elements)     ← các điểm nhấn               │
│   - Sidebar navigation, KPI cards, role chip               │
├─────────────────────────────────────────────────────────────┤
│ Backdrop (Subtle gradient tint, ít hơn LoginForm)          │
│   - Chỉ ở viền + góc, không phủ toàn screen                │
└─────────────────────────────────────────────────────────────┘
```

## Design tokens chung (bổ sung vào `UiTheme.cs`)

```csharp
// Healthcare palette adapted từ skill (chung cho mọi form work)
public static readonly Color HealthCyan      = Color.FromArgb(8,   145, 178);   // #0891B2
public static readonly Color HealthCyanLight = Color.FromArgb(34,  211, 238);   // #22D3EE
public static readonly Color HealthGreen     = Color.FromArgb(5,   150, 105);   // #059669
public static readonly Color HealthEmerald   = Color.FromArgb(22,  163, 74);    // #16A34A
public static readonly Color HealthBgTint    = Color.FromArgb(236, 254, 255);   // #ECFEFF (cyan-50)
public static readonly Color HealthBgMint    = Color.FromArgb(236, 253, 245);   // #ECFDF5 (emerald-50)

// Status semantic
public static readonly Color StatusSuccess   = HealthGreen;
public static readonly Color StatusWarning   = Color.FromArgb(217, 119, 6);     // #D97706
public static readonly Color StatusInfo      = HealthCyan;
public static readonly Color StatusDanger    = Color.FromArgb(220, 38, 38);     // #DC2626

// Spacing scale (skill khuyến nghị 4/8dp rhythm)
public const int Spacing1 = 4;
public const int Spacing2 = 8;
public const int Spacing3 = 12;
public const int Spacing4 = 16;
public const int Spacing5 = 24;
public const int Spacing6 = 32;
public const int Spacing7 = 48;
public const int Spacing8 = 64;

// Border radius
public const int RadiusSm = 6;
public const int RadiusMd = 12;
public const int RadiusLg = 18;
public const int RadiusPill = 999;
```

## Control library cần tạo

Trước khi build 5 form, cần tạo thêm các control tái sử dụng:

| Control | Mục đích | File |
|---------|----------|------|
| `Sidebar` | Sidebar navigation thay TabControl ngang | `Controls/Sidebar.cs` |
| `SidebarItem` | Item trong sidebar (icon + text + active state) | `Controls/Sidebar.cs` |
| `Card` | Container bo tròn + shadow nhẹ thay Panel | `Controls/Card.cs` |
| `KpiCard` | Card hiển thị 1 con số + label + icon | `Controls/KpiCard.cs` |
| `RoleChip` | Badge tròn hiển thị vai trò có màu | `Controls/RoleChip.cs` |
| `RoundedButton` | Button bo tròn dùng UiTheme | `Controls/RoundedButton.cs` |
| `StatusPill` | Pill nhỏ "Đã kết luận" / "Đang điều trị" | `Controls/StatusPill.cs` |
| `Avatar` | Tròn chứa chữ cái đầu tên (gradient bg) | `Controls/Avatar.cs` |
| `StatusBar` | Footer mỗi form: thông tin connection + role + clock | `Controls/StatusBar.cs` |
| `EmptyState` | Khi grid trống: icon + text + CTA | `Controls/EmptyState.cs` |

Tổng: 10 control mới. Ước lượng 4-5h.

---

## FORM 1 — AdminDashboard (Phân hệ 1)

### Skill khuyến nghị
- **Pattern:** Real-Time / Operations
- **Style:** Data-Dense Dashboard (`#1E40AF` blue + `#D97706` amber accents)
- **Performance:** ⚡ Excellent | **A11y:** ✓ WCAG AA

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│                                                                 │
│  ┌───────────┬─────────────────────────────────────────────┐   │
│  │  ⚙ Admin  │  KPIs row: [Users 24] [Roles 8] [Grants 156]│   │
│  │           ├─────────────────────────────────────────────┤   │
│  │ 👥 Users  │                                              │   │
│  │ 🏷  Roles │   ┌─ Tab Active: Users ─────────────────┐  │   │
│  │ 🔑 Grant  │   │ ┌─Search box─┐ ┌─ Filter ▾─┐ [+ New]│  │   │
│  │ ✋ Revoke │   │ └────────────┘ └────────────┘        │  │   │
│  │ 📊 View   │   │                                       │  │   │
│  │ 📜 Audit  │   │ ┌──────────────────────────────────┐ │  │   │
│  │ ────────  │   │ │ USER     ROLE   STATUS    ACTIONS │ │  │   │
│  │ ⏻ Logout  │   │ │ ──────   ────   ──────    ─────── │ │  │   │
│  │           │   │ │ BS_NV003 BS     ●Active   ⋮      │ │  │   │
│  │ ⌨ DBA     │   │ │ KTV_001  KTV    ●Active   ⋮      │ │  │   │
│  │  ●●●○○   │   │ │ ...                                │ │  │   │
│  │  online   │   │ └──────────────────────────────────┘ │  │   │
│  └───────────┴───└───────────────────────────────────────┘──┘   │
│                                                                 │
│  Connected: localhost/XEPDB1  |  SYSTEM  |  18:42:15  |  ⏻    │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Sidebar trái 220px**: tab navigation thay TabControl ngang (theo skill "data-dense dashboard")
- **Header gradient mini** chỉ ở top (`Primary → PrimaryDark`)
- **KPI row** ở top: 3 card glass nhỏ hiển thị tổng số User/Role/Grant
- **Content area** trắng sạch, padding 24px
- **Table**: striped rows, hover highlight, action column (3-dot menu)
- **Tab "Audit log"** mới: xem `DBA_AUDIT_TRAIL` + `APP_LOGIN_LOG` (chưa có ở bản hiện tại)
- **Status bar** dưới đáy

### Color override cho Admin
```csharp
// Admin dùng blue + amber accents
HeaderColor = UiTheme.Primary;        // #1E5AA0 (đã có)
KpiAccent   = UiTheme.StatusWarning;  // amber #D97706
TableHeader = UiTheme.BgLight;
```

### Risks
- Sidebar refactor breaking 5 tab hiện có — cần test kỹ
- Audit tab là tính năng mới, có thể defer sang Sprint 7

---

## FORM 2 — DPVForm (Điều phối viên)

### Skill khuyến nghị
- **Pattern:** Operational workflow
- **Style:** Accessible & Ethical (WCAG AAA, healthcare cyan `#0891B2`)
- **Anti-patterns:** Bright neon, motion-heavy animations

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│  📋 Điều phối viên — DPV_NV001              [🔔 5] [👤] [⏻]    │
├─────────────────────────────────────────────────────────────────┤
│  ┌───────────────┐                                              │
│  │ 🏥 Bệnh nhân │ ← active tab (cyan underline 3px)             │
│  │ 📂 HSBA       │                                              │
│  │ 👤 Tôi        │                                              │
│  └───────────────┘                                              │
│                                                                 │
│  KPIs: [BN hôm nay 12] [HSBA mở 8] [Chờ giao 3]                │
│                                                                 │
│  ┌─ Search [🔍 tìm BN...] ──────────────────┐ [+ Thêm BN]      │
│  └──────────────────────────────────────────┘                  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ┌─Card BN┬─Card BN┬─Card BN┬─Card BN─────────────┐    │   │
│  │ │ 👤 Mai │ 👤 Ngu │ 👤 Tran│ ...                  │    │   │
│  │ │ BN001  │ BN002  │ BN003  │                      │    │   │
│  │ │ Nữ 55t │ Nam 39 │ Nữ 30  │                      │    │   │
│  │ │ ●HCM   │ ●HCM   │ ●Da Na │                      │    │   │
│  │ └────────┴────────┴────────┴──────────────────────┘    │   │
│  │                                                          │   │
│  │ [< 1 / 5 >]    50 dòng/trang ▾                          │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Connected: BS_NV003  |  Vai trò: ĐPV  |  18:42  |  ⏻         │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Card grid** thay DataGridView cho BN list (skill: "card-based for healthcare productivity")
- **Avatar** trong mỗi BN card (gradient + chữ đầu tên)
- **Pagination** cho 100k BN
- **KPI row** hiển thị daily stats
- **Header chip role** thay vì plain text — `RoleChip` với màu cyan
- **Tab vẫn 3** như hiện tại nhưng underline thay vì plain tab
- **Modal form** thay split panel khi thêm/sửa BN — full-screen drawer từ phải

### Khác biệt với hiện tại
- Bỏ split container left/right → BN list dạng card grid, edit qua drawer modal
- Bỏ "Thêm BN" form trong panel right → drawer slide-in từ phải, blur backdrop
- HSBA tab: timeline view thay flat table

---

## FORM 3 — BSForm (Bác sĩ)

### Skill khuyến nghị
- **Pattern:** Clinical workspace
- **Style:** Accessible & Ethical (medical teal `#0891B2` + emerald `#16A34A`)
- **Mood:** medical, clean, trustworthy

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│  👨‍⚕ Dr. Lê Cường — BS_NV003 (Tim mạch)        [👤] [⏻]      │
├─────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────┬──────────────────────────────────┐ │
│  │  HỒ SƠ BỆNH ÁN        │ Patient: BN001 - Mai Thi Hoa     │ │
│  │  ────────────────────  │ Female, 55t, HCM                 │ │
│  │  ┌──────────────────┐ │ ┌─Tab: 📝 Chẩn đoán─🔬 DV─💊 Đơn┐│ │
│  │  │ ●  HS001 04/04   │ │ │                                ││ │
│  │  │    Tim mạch      │ │ │ Chẩn đoán  ┌────────────────┐ ││ │
│  │  │    [Đang điều trị]│ │ │ Đái tháo │  multi-line     │ ││ │
│  │  │                  │ │ │ đường... │  textarea với   │ ││ │
│  │  │ ●  HS004 15/04   │ │ │           │  word counter   │ ││ │
│  │  │    Tim mạch      │ │ │           │  500/2000       │ ││ │
│  │  │    [Đã kết luận] │ │ │           └────────────────┘ ││ │
│  │  │                  │ │ │                                ││ │
│  │  │ + ...            │ │ │ Điều trị   ┌────────────────┐ ││ │
│  │  └──────────────────┘ │ │           │ Insulin...     │ ││ │
│  │                       │ │           └────────────────┘ ││ │
│  │                       │ │                                ││ │
│  │                       │ │ Kết luận                       ││ │
│  │                       │ │ [...] │                        ││ │
│  │                       │ │                                ││ │
│  │                       │ │ [💾 Lưu (Ctrl+S)] [↺ Hủy]   ││ │
│  │                       │ └────────────────────────────────┘│ │
│  └────────────────────────┴──────────────────────────────────┘ │
│  Connected: ...  |  ⏻ Idle: 7m  |  🔒 RBAC | VPD active       │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Master-detail layout**: HSBA list trái 30% + workspace phải 70%
- **HSBA card** trong list có `StatusPill` (Đang điều trị / Đã kết luận)
- **Patient header strip** trên cùng workspace hiển thị BN info
- **Tab inside workspace**: Chẩn đoán / Dịch vụ / Đơn thuốc (giống hiện tại nhưng visual cleaner)
- **TextArea với character counter** (500/2000) cho CHANDOAN/DIEUTRI/KETLUAN
- **Save indicator** trong status bar: hiển thị "🔒 RBAC | VPD active" để user biết policy đang chạy
- **Color**: emerald accent cho nút Save thay primary blue (theo skill khuyến nghị)

### Trade-off
- Master-detail tốt hơn split horizontal hiện tại — BS dễ nhảy giữa HSBA
- Cần `StatusPill` control mới

---

## FORM 4 — KTVForm (Kỹ thuật viên)

### Skill khuyến nghị
- **Pattern:** List-detail, results entry
- **Style:** Soft UI Evolution (green `#059669` + amber accent)
- **Performance:** ⚡ Excellent | **A11y:** ✓ WCAG AA+

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│  🔬 KTV Phương — KTV_NV006                       [👤] [⏻]     │
├─────────────────────────────────────────────────────────────────┤
│  KPIs: [Hôm nay 5] [Chờ kết quả 3] [Đã xong 12]                │
│                                                                 │
│  ┌─ Filter ─────────────────────────────────────────────────┐  │
│  │ [Tất cả] [Chưa có KQ ●3] [Đã có KQ ●12]                  │  │
│  └───────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ ⊞ Xét nghiệm máu             HS001  04/04   [▸ Nhập KQ] │  │
│  │ ⊞ Siêu âm tim                HS001  05/04   ●Đã có      │  │
│  │ ⊞ Điện não đồ                HS002  05/04   [▸ Nhập KQ] │  │
│  │ ...                                                       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─ Khi click: Drawer trượt từ phải ───────────────────────┐  │
│  │  Nhập kết quả                                       [✕] │  │
│  │  ┌────────────────────────────────────────────────────┐ │  │
│  │  │ HSBA: HS001  -  Xét nghiệm máu  -  04/04/2025    │ │  │
│  │  │                                                    │ │  │
│  │  │ Kết quả                                            │ │  │
│  │  │ ┌────────────────────────────────────────────────┐│ │  │
│  │  │ │ multi-line textarea                            ││ │  │
│  │  │ │ HC: ...                                        ││ │  │
│  │  │ │ Glucose: ...                                   ││ │  │
│  │  │ │ HbA1c: ...                                     ││ │  │
│  │  │ └────────────────────────────────────────────────┘│ │  │
│  │  │                                                    │ │  │
│  │  │ ⚠ Mọi cập nhật được ghi vào LOG_KTV_KETQUA      │ │  │
│  │  │                                                    │ │  │
│  │  │ [💾 Lưu (Ctrl+S)] [Hủy (Esc)]                    │ │  │
│  │  └────────────────────────────────────────────────────┘ │  │
│  └──────────────────────────────────────────────────────────┘  │
│  Audit: trigger active                                          │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Filter chips** trên đầu: Tất cả / Chưa có KQ (badge số) / Đã có KQ
- **Status indicator** trong mỗi row: ●Đã có (green) hoặc [▸ Nhập KQ] (button)
- **Drawer slide từ phải** khi click nhập KQ thay vì panel cố định bên dưới
- **Audit reminder** trong drawer + status bar
- **Green primary** thay blue (skill khuyến nghị cho KTV — Soft UI Evolution)

### Khác hiện tại
- Bỏ panel split top/bottom
- Drawer modal cho focused entry → đỡ phân tâm

---

## FORM 5 — BNForm (Bệnh nhân)

### Skill khuyến nghị (override một phần)
- **Pattern:** App Store Style Landing (skill đề xuất, nhưng không phù hợp — đây là profile)
- **Style:** Neumorphism (skill đề xuất, nhưng có a11y risk)

**→ Tôi override:** Card-based "Personal Health" style như Apple Health/Samsung Health
- Reasoning: BN không cần "wow factor", cần **dễ đọc + dễ chỉnh sửa**
- Giữ healthcare cyan palette
- Bỏ neumorphism (low contrast risk), dùng card flat với border subtle

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│  🧑‍⚕ Hồ sơ của tôi — Mai Thi Hoa                  [👤] [⏻]   │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────── Avatar + Name + ID ───────┐                          │
│  │     ╭────╮                        │                          │
│  │     │ MH │   Mai Thi Hoa          │  ← Gradient avatar      │
│  │     ╰────╯   BN001                │     85px tròn            │
│  │                                    │                          │
│  │     Nữ • 55 tuổi • HCM             │                          │
│  └────────────────────────────────────┘                          │
│                                                                 │
│  ┌─Card: 📌 Thông tin định danh────────────────────────────┐   │
│  │  CCCD:        300112345678                              │   │
│  │  Ngày sinh:   20/04/1970                                │   │
│  │  Phái:        Nữ                                        │   │
│  │  ℹ Liên hệ DBA nếu cần điều chỉnh                       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─Card: 🏠 Địa chỉ liên lạc       [✏ Chỉnh sửa]────────┐    │
│  │  Số nhà / Đường: 12 Lê Lợi                              │   │
│  │  Quận: Q.1                                              │   │
│  │  Tỉnh/TP: HCM                                           │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─Card: 🩺 Thông tin y tế        [✏ Chỉnh sửa]──────────┐    │
│  │  Tiền sử bệnh:   Tiểu đường type 2                      │   │
│  │  Tiền sử GĐ:     (chưa có)                              │   │
│  │  Dị ứng thuốc:   Penicillin                             │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─Tabs───────────────────────────────────────────────────┐   │
│  │ [Thông tin] [📋 Lịch sử khám] [💊 Đơn thuốc] [📢 TB]   │   │
│  └─────────────────────────────────────────────────────────┘   │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Profile header** với gradient avatar 85px
- **Cards** thay vì table layout — dễ đọc cho người không quen IT
- **Edit inline**: nút "✏ Chỉnh sửa" trên mỗi card → biến card thành form
- **Lock icons** cạnh các field định danh (CCCD/Phái/Ngày sinh) để rõ chính sách
- **Lớn hơn** font, padding hơn (BN có thể là người lớn tuổi)
- **Empty state** trong "Đơn thuốc" tab khi chưa có đơn

### Khác hiện tại
- Bỏ split layout, dùng vertical cards
- Inline edit thay vì bulk save
- Thêm tab "Đơn thuốc của tôi" (read-only) — feature mới

---

## FORM 6 — OLSViewerForm

### Skill khuyến nghị
- **Pattern:** Newsletter / Content First
- **Style:** Exaggerated Minimalism (`#2563EB` blue, paper-like)

### Layout mock

```
╭─────────────────────────────────────────────────────────────────╮
│  📢 Thông báo OLS — u4_nvtk_hcm                       [⏻]      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ╭─ Nhãn của bạn ──────────────────────────────────────╮       │
│  │  🔖 NV:HCM:TK                                       │       │
│  │  Nhân viên Khoa Thần kinh tại Hồ Chí Minh           │       │
│  │  Cấp: Nhân viên  |  Cơ sở: HCM  |  Khoa: Thần kinh  │       │
│  ╰──────────────────────────────────────────────────────╯       │
│                                                                 │
│  3 thông báo  |  [🔄 Làm mới (F5)]              [Filter ▾]     │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ ●  TB001  04/05/2025 14:30                              │   │
│  │    Thông báo họp toàn viện ngày 05/05/2025              │   │
│  │    📍 Hội trường lớn                                    │   │
│  │    🏷  NV (Toàn nhân viên)                              │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │ ○  TB003  03/05/2025 09:15                              │   │
│  │    Họp lãnh đạo khoa - Báo cáo quý 2                    │   │
│  │    📍 Phòng họp A                                       │   │
│  │    🏷  LDK                                              │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ℹ Bạn chỉ thấy thông báo phù hợp với nhãn của bạn (OLS)       │
╰─────────────────────────────────────────────────────────────────╯
```

### Đặc trưng
- **Label hero panel** trên cùng giải thích nhãn user theo cách dễ hiểu
- **Inbox-style list**: vòng tròn dot trái (●unread / ○ read), title bold, location/time mờ
- **Tag chip** hiển thị nhãn OLS của từng thông báo
- **Empty state** khi 0 thông báo
- **Minimal**: chỉ 1 màu primary blue, không gradient, không glass

---

## So sánh chiến lược style toàn dự án

| Form | Style chính | Glass usage | Color primary | Lý do |
|------|-------------|-------------|---------------|-------|
| Login | Liquid Glass | **Heavy** | Primary blue | Splash, entry point — "wow" |
| Admin | Data-Dense Dashboard | **Light** (sidebar accent) | Primary + amber | Data productivity |
| DPV | Accessible Healthcare | **Medium** (cards) | Cyan #0891B2 | Operational workflow |
| BS | Accessible Healthcare | **Light** (chips, header) | Cyan + emerald | Clinical focus, ít distraction |
| KTV | Soft UI Evolution | **Light** (filter chips) | Green #059669 | Task-focused entry |
| BN | Card-based Health | **None** | Cyan #0891B2 | A11y first, dễ đọc |
| OLS | Exaggerated Minimalism | **None** | Pure blue | Content-first reading |

→ Glassmorphism được dùng **chiến lược, không lạm dụng**. LoginForm là điểm nhấn duy nhất full glass.

---

## Lịch implement đề xuất

### Sprint A (4-5h) — Foundation
- [ ] Bổ sung tokens vào `UiTheme.cs` (Health palette, spacing scale, radius)
- [ ] Tạo `Controls/Card.cs` (rounded corner, shadow nhẹ, border subtle)
- [ ] Tạo `Controls/RoundedButton.cs` (replace mọi `Button` flat hiện tại)
- [ ] Tạo `Controls/StatusPill.cs`, `RoleChip.cs`, `Avatar.cs`
- [ ] Tạo `Controls/Sidebar.cs` + `SidebarItem.cs`
- [ ] Tạo `Controls/StatusBar.cs`, `EmptyState.cs`, `KpiCard.cs`

### Sprint B (3h) — Refactor AdminDashboard
- [ ] Convert TabControl → Sidebar
- [ ] Thêm KPI row trên top
- [ ] Thêm tab "Audit log" (mới)
- [ ] Status bar dưới đáy

### Sprint C (3h) — Refactor DPVForm
- [ ] Card grid cho BN list + pagination
- [ ] Drawer modal cho thêm/sửa BN
- [ ] Tab underline style + KPI row
- [ ] Timeline view cho HSBA tab

### Sprint D (2.5h) — Refactor BSForm
- [ ] Master-detail layout HSBA list trái + workspace phải
- [ ] StatusPill cho mỗi HSBA
- [ ] Character counter cho textarea
- [ ] Patient header strip

### Sprint E (2h) — Refactor KTVForm
- [ ] Filter chips top
- [ ] Status indicator trong mỗi row
- [ ] Drawer slide-in cho entry kết quả
- [ ] Audit reminder

### Sprint F (2.5h) — Refactor BNForm
- [ ] Profile header + gradient avatar
- [ ] Card-based layout cho 3 section
- [ ] Inline edit per card
- [ ] Tab "Đơn thuốc" mới (read-only)

### Sprint G (1h) — Refactor OLSViewerForm
- [ ] Label hero panel
- [ ] Inbox list style
- [ ] Tag chips
- [ ] Empty state

**Tổng: ~18h.** Có thể chia 4-5 ngày, mỗi ngày 1 form.

---

## Decision Matrix — Triển khai theo thứ tự nào?

| Form | Người dùng nhiều nhất | Impact điểm vấn đáp | Effort | Priority |
|------|----------------------|--------------------|---------| ---------|
| Login | Demo entry — ai cũng thấy đầu tiên | ⭐⭐⭐⭐⭐ | ✅ DONE | DONE |
| AdminDashboard | DBA dùng nhiều giờ | ⭐⭐⭐⭐ | High (3h) | **P0** |
| BSForm | BS dùng nhiều, có VPD demo | ⭐⭐⭐⭐⭐ | Med (2.5h) | **P0** |
| DPVForm | DPV dùng nhiều, có UI phức tạp | ⭐⭐⭐⭐ | High (3h) | **P1** |
| KTVForm | Đơn giản, ít content | ⭐⭐⭐ | Low (2h) | **P1** |
| BNForm | BN dùng ít, nhưng demo TC#5 | ⭐⭐⭐⭐ | Med (2.5h) | **P2** |
| OLSViewerForm | Chỉ demo OLS | ⭐⭐⭐ | Low (1h) | **P2** |

**Đề xuất:**
- **Hôm nay:** Sprint A (foundation controls) + Sprint B (Admin) + Sprint D (BS)
- **Hôm sau:** Sprint C (DPV) + Sprint E (KTV)
- **Cuối tuần:** Sprint F (BN) + Sprint G (OLS) + polishing

---

## Anti-patterns cần tránh (rút từ skill)

- ❌ Bright neon colors (skill warning cho healthcare)
- ❌ AI purple/pink gradients
- ❌ Motion-heavy animations (gây mệt trong môi trường y tế)
- ❌ Emoji as structural icons → đổi sang vector icon
- ❌ Heavy glassmorphism trên DataGridView (performance)
- ❌ Neumorphism cho BN (a11y risk, low contrast)
- ❌ Hover-only interactions (mobile/touch không có)
- ❌ Animation < 150ms (giật) hoặc > 500ms (chậm)

---

## Câu hỏi quyết định

Trước khi triển khai, bạn xác nhận:

1. **Bạn muốn implement theo thứ tự nào?**
   - (a) Foundation → tất cả form theo P0/P1/P2 (~18h, 4-5 ngày)
   - (b) Chỉ refactor 2-3 form quan trọng (Admin + BS + DPV) (~10h)
   - (c) Chỉ thêm các control library (Sidebar/Card/...) rồi tích hợp dần khi cần
   - (d) Khác — bạn chỉ định

2. **Có muốn giữ tabs cũ cho form không (BS, BN)** hay convert hết sang sidebar?
   - Đa số form work hiện đang dùng TabControl ngang — chuyển sang sidebar sẽ phá compatibility nhưng đẹp hơn

3. **Vector icons từ đâu?**
   - SVG embed vào WinForms khá phức tạp
   - Option: dùng Segoe MDL2 Assets (font icon có sẵn Windows)
   - Option: SVG render bằng Svg.NET (NuGet)
   - Option: PNG resource (kém scale)

4. **Animation/transition trong WinForms?**
   - WinForms không có animation native → cần `System.Windows.Forms.Timer` + manual interpolation
   - Option: dùng thư viện như `MaterialSkin.NET` (NuGet) — đẹp nhưng có thể conflict UiTheme
   - Option: animation tối thiểu chỉ ở fade/slide drawer

Tôi sẽ chờ bạn quyết định trước khi tiếp tục code.
