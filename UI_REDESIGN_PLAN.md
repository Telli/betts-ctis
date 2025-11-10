# UI Redesign Plan: Figma Mockup to Next.js Frontend

## Executive Summary

**Recommendation:** **Incrementally refactor the existing Next.js frontend** (`sierra-leone-ctis`) rather than migrating to the Figma mockup Vite+React app.

### Reasoning
1. **Feature Parity:** The existing Next.js app has ~50% more backend-connected features (workflow automation, associate permissions, KPI dashboard, reporting, payment integrations, document verification, etc.)
2. **Backend Integration:** The Next.js app already has production-ready API integration with `BettsTax.Web` (C# .NET backend)
3. **ROI:** Refactoring styles/layout is faster than rebuilding all backend wiring
4. **Risk:** The Figma mockup is a static prototype with mock data—migrating would require rewriting all API calls

---

## Design System Comparison

### Figma Mockup Design Tokens (Target)
```css
/* Primary Colors */
--primary: #3d5f7e (muted blue-gray)
--sidebar: #f7fafc (light gray)
--sidebar-accent: #edf2f7 (slightly darker gray)

/* Semantic Colors */
--success: #38a169 (green)
--warning: #d69e2e (gold/amber)
--info: #4299e1 (bright blue)
--destructive: #e53e3e (red)

/* Neutrals */
--background: #ffffff
--card: #ffffff
--border: #e2e8f0 (light gray)
--muted: #f7fafc
--muted-foreground: #718096 (medium gray)

/* Typography */
--radius: 0.5rem (border-radius)
```

### Current Frontend Design Tokens (sierra-leone-ctis)
```css
/* Primary Colors */
--sierra-blue: 221.2 83.2% 53.3% (vibrant blue - HSL)
--sierra-gold: 43 74% 66%
--sierra-green: 142 76% 36%

/* Layout uses custom "sierra-blue" classes throughout */
```

### Key Differences
| Aspect | Figma Mockup | Current Frontend |
|--------|--------------|------------------|
| **Primary Color** | Muted blue-gray (#3d5f7e) | Vibrant blue (HSL sierra-blue) |
| **Sidebar** | Light gray (#f7fafc) | White/light with sierra-blue accents |
| **Active Nav** | Soft accent + left border | Blue background + border |
| **Cards** | Colored top border (4px) | Full card styling |
| **Tables** | Clean, minimal borders | Default shadcn styling |
| **Metric Cards** | Top border indicator | Current mixed styling |

---

## Implementation Plan

### Phase 1: Design Token Migration (2-3 days)

#### 1.1 Update CSS Variables
**File:** `sierra-leone-ctis/app/globals.css`

```css
@layer base {
  :root {
    /* Replace sierra-blue with Figma palette */
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    
    /* Primary: Muted blue-gray from Figma */
    --primary: 210 20% 35%; /* #3d5f7e */
    --primary-foreground: 0 0% 100%;
    
    /* Semantic colors matching Figma */
    --success: 142 76% 36%; /* Keep existing green */
    --warning: 43 74% 66%; /* Keep existing gold */
    --info: 208 80% 57%; /* #4299e1 */
    --destructive: 0 72% 51%; /* #e53e3e */
    
    /* Neutrals */
    --border: 214.3 31.8% 91.4%; /* #e2e8f0 */
    --input: 214.3 31.8% 91.4%;
    --muted: 210 40% 96.1%; /* #f7fafc */
    --muted-foreground: 215.4 16.3% 46.9%; /* #718096 */
    
    /* Sidebar */
    --sidebar: 210 40% 96.1%; /* #f7fafc */
    --sidebar-accent: 214 32% 91%; /* #edf2f7 */
    --sidebar-foreground: 222.2 84% 4.9%;
    --sidebar-primary: 210 20% 35%;
    --sidebar-border: 214.3 31.8% 91.4%;
    
    --radius: 0.5rem;
  }
}
```

**Action Items:**
- [ ] Replace all `sierra-blue` HSL values with new `--primary`
- [ ] Update `--sidebar-*` variables to match Figma light gray palette
- [ ] Remove custom `.sierra-blue` gradient class

#### 1.2 Search & Replace Custom Classes
Run global find/replace:
- `text-sierra-blue-*` → `text-primary`
- `bg-sierra-blue-*` → `bg-primary` / `bg-muted`
- `border-sierra-blue-*` → `border-primary`
- `hover:bg-sierra-blue-*` → `hover:bg-primary/90`

**Files to Update:**
- `components/sidebar.tsx` (navigation styling)
- `components/navbar.tsx` (if exists)
- All page components using custom colors

---

### Phase 2: Layout & Sidebar Refinement (2 days)

#### 2.1 Sidebar Component Refactor
**File:** `sierra-leone-ctis/components/sidebar.tsx`

**Target Changes:**
1. **Background:** Change from white/blue to light gray (`bg-sidebar`)
2. **Active State:** Add 4px left border + soft accent background
3. **Hover State:** Lighter accent on hover
4. **Icon Alignment:** Ensure consistent spacing (Lucide icons)

**Example Update:**
```tsx
// Before
className={`... ${
  item.current 
    ? "bg-sierra-blue-50 text-sierra-blue-700 border border-sierra-blue-200" 
    : "text-gray-700 hover:bg-sierra-blue-25"
}`}

// After (Figma style)
className={`... ${
  item.current 
    ? "bg-sidebar-accent text-sidebar-accent-foreground border-l-4 border-sidebar-primary" 
    : "text-sidebar-foreground hover:bg-sidebar-accent/50"
}`}
```

#### 2.2 Top Bar / Header
**Target:** Clean white header with search, notifications, user menu

**Updates:**
- Ensure header has `bg-card` with `border-b border-border`
- Search input should have light gray background (`bg-input-background`)
- Notification badge: use `bg-destructive` for count

---

### Phase 3: Component Styling (3-4 days)

#### 3.1 Metric Cards
**Reference:** `Client Tax Information System/src/components/MetricCard.tsx`

**Key Features:**
- 4px colored top border (`border-t-4 border-t-success/warning/info/primary`)
- Clean white background
- Trend indicators (up/down/neutral) with colored icons
- Icon in top-right corner

**Action:**
- [ ] Create/update `sierra-leone-ctis/components/metric-card.tsx`
- [ ] Update Dashboard to use new MetricCard format
- [ ] Ensure trend colors match: green (up), red (down), gray (neutral)

#### 3.2 Tables
**Reference:** `Client Tax Information System/src/components/ClientList.tsx`

**Key Features:**
- Clean borders (`border border-border`)
- Rounded container (`rounded-lg`)
- Header row with subtle background
- Consistent row padding

**Action:**
- [ ] Verify `components/ui/table.tsx` matches Figma styling
- [ ] Update any custom table overrides in page components

#### 3.3 Cards & Containers
**Updates:**
- Use `border-t-4 border-t-{color}` for accent cards
- Clean white backgrounds with subtle border
- Consistent padding (p-6 for content areas)

---

### Phase 4: Page-Specific Updates (4-5 days)

#### 4.1 Dashboard Page
**File:** `sierra-leone-ctis/app/dashboard/page.tsx`

**Updates:**
- [ ] Replace metric card styles with new `<MetricCard>` component
- [ ] Update chart colors to match Figma palette
- [ ] Ensure compliance distribution pie chart uses new semantic colors
- [ ] Recent activity timeline with clean dot indicators

#### 4.2 Clients Page
**File:** `sierra-leone-ctis/app/clients/page.tsx`

**Updates:**
- [ ] Update filter bar layout (search + 2 selects)
- [ ] Table with compliance score badges (Excellent/Good/Fair/At Risk)
- [ ] Actions dropdown with proper icon spacing

#### 4.3 Payments Page
**File:** `sierra-leone-ctis/app/payments/page.tsx`

**Updates:**
- [ ] Summary cards with colored top borders
- [ ] Status badges: `bg-success` (Paid), `bg-warning` (Pending), `variant="destructive"` (Overdue)
- [ ] Record Payment dialog with proper form layout

#### 4.4 Other Pages
Apply similar updates to:
- Tax Filings
- Documents
- Compliance
- Reports
- KPI Dashboard
- Admin Settings

---

### Phase 5: Fine-Tuning & Polish (2 days)

#### 5.1 Typography
- Ensure consistent font weights (medium: 500, semibold: 600)
- Page titles: `text-3xl font-bold`
- Section headers: `text-xl font-semibold`
- Labels: `text-sm font-medium`

#### 5.2 Spacing & Padding
- Page container: `p-6`
- Card padding: `p-4` to `p-6`
- Gap between elements: `gap-4` or `gap-6`

#### 5.3 Accessibility
- Ensure all color contrasts meet WCAG AA
- Verify focus indicators are visible
- Test keyboard navigation

---

## Migration Path Assessment

### Option A: Refactor Existing Next.js (Recommended)
**Effort:** 15-20 days
**Risk:** Low
**Benefits:**
- Retain all backend integrations
- Incremental rollout (can deploy per page)
- No feature regression

### Option B: Migrate to Figma Mockup Vite App
**Effort:** 45-60 days
**Risk:** High
**Challenges:**
- Rebuild all API integration (~30 endpoints)
- Reimplement authentication & authorization
- Recreate workflow automation UI
- Port associate permission system
- Migrate payment integration flows
- Rebuild KPI dashboard with real data
- Convert all forms to match backend DTOs

**When to Consider Option B:**
- If backend changes significantly and API contracts are unstable
- If the Figma mockup has critical UX improvements the team validated
- If there's budget/time for a complete rewrite

**Hybrid Option C: Use Figma Mockup as Component Library**
- Extract reusable components from Figma mockup
- Copy `components/ui/*` and custom components (MetricCard, PageHeader)
- Port to Next.js `sierra-leone-ctis/components/ui/`
- Gradually integrate into existing pages

---

## Implementation Checklist

### Prerequisites
- [ ] Backup current `globals.css`
- [ ] Create feature branch: `ui-redesign-figma-palette`
- [ ] Document current color usage with screenshots

### Phase 1: Design Tokens (Week 1)
- [ ] Update CSS variables in `globals.css`
- [ ] Replace custom color classes
- [ ] Test color contrast ratios
- [ ] Verify dark mode (if applicable)

### Phase 2: Layout (Week 1)
- [ ] Update sidebar component
- [ ] Refine top bar/header
- [ ] Test responsive breakpoints

### Phase 3: Components (Week 2)
- [ ] Create MetricCard component
- [ ] Update table styling
- [ ] Refine card components
- [ ] Update badge variants

### Phase 4: Pages (Weeks 2-3)
- [ ] Dashboard
- [ ] Clients
- [ ] Payments
- [ ] Tax Filings
- [ ] Documents
- [ ] Compliance
- [ ] Reports
- [ ] KPI Dashboard
- [ ] Admin Settings

### Phase 5: QA (Week 3)
- [ ] Cross-browser testing
- [ ] Mobile responsiveness
- [ ] Accessibility audit
- [ ] Performance testing
- [ ] Stakeholder review

---

## Color Palette Quick Reference

### Figma Mockup Palette (Target)
```
Primary:        #3d5f7e (muted blue-gray)
Sidebar:        #f7fafc (light gray)
Sidebar Accent: #edf2f7
Success:        #38a169 (green)
Warning:        #d69e2e (amber/gold)
Info:           #4299e1 (bright blue)
Destructive:    #e53e3e (red)
Border:         #e2e8f0 (light gray)
Muted:          #f7fafc
Text:           #1a202c (dark gray)
Muted Text:     #718096 (medium gray)
```

### Current Palette
```
Sierra Blue:    HSL(221.2, 83.2%, 53.3%) - vibrant blue
Sierra Gold:    HSL(43, 74%, 66%)
Sierra Green:   HSL(142, 76%, 36%)
```

---

## Conclusion

**Recommended Approach:** Refactor the existing Next.js frontend using the Figma mockup as a design reference. This preserves all backend integrations while modernizing the UI with a cleaner, more professional look that aligns with the Figma design system.

**Timeline:** 3-4 weeks for complete implementation
**Risk Level:** Low to Medium
**ROI:** High (better UX without feature loss)
