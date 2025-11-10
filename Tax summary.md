# Tax Filing App — Key Requirements & Calculations (Markdown Blueprint)

*Source: Finance Act 2025 (Sierra Leone) summary by The Betts Firm.* 

---

## 1) Core tax domains & scope

* **Income Tax**

  * Taxpayer category thresholds (Large / Medium / Small / Micro) drive filing office, forms, and penalties.
  * **Minimum Alternate Tax (MAT):** 2% of revenue when company reports losses for two consecutive years; now applies to **mining & petroleum** companies.
* **Payroll Tax**

  * Employer filing required (including **foreign employees**).
  * **Annual payroll return due:** **31 Jan** each year *or* **1 month after** a foreign employee starts work.
* **Goods & Services Tax (GST)**

  * **Standard rate:** **15%**.
  * **Filing deadline:** **21 days** after end of tax period.
  * **Payment deadline:** unchanged (end of the following month).
  * **Relieved entities:** diplomatic bodies and companies with *ratified Parliamentary agreements* are relieved at **point of purchase** (no pay‐and‐refund flow).
  * **Input/Output schedules:** itemized **transaction-level** schedules now mandatory.
* **Excise Duty**

  * **Return + payment due:** **within 21 days** of domestic delivery or import.
  * Updated **specific rates** (see §4.3).
* **Customs & Exemptions (highlights)**

  * **Import duty on rice:** 5% (2025) → 10% (2026).
  * **INGOs/NNGOs:** duty/GST relief on vehicle imports (quotas per 5-year window).
  * Minister **cannot** grant CIT/WHT exemptions in new/renewed agreements (consistency rule).
* **Sector fees & royalties (extractives, petroleum)**

  * Revised **PRA** (Petroleum Regulatory Agency) fees; regulatory fee per litre increased.
  * Clearer **royalty valuation** rules; new royalty rates for **marble/granite** and **dimension stone**.

---

## 2) Taxpayer segmentation (drives UX, logic, penalties)

| Segment    |   Annual turnover (SLE) | Notes                          |
| ---------- | ----------------------: | ------------------------------ |
| **Large**  |         **> 6,000,000** | Large taxpayer office handling |
| **Medium** | **500,000 – 6,000,000** |                                |
| **Small**  |    **10,000 – 500,000** |                                |
| **Micro**  |            **≤ 10,000** |                                |

*Use segment in: registration flow, default form presets, penalty tables, and routing.* 

---

## 3) Filing calendars (enforce in UI & reminders)

* **Annual Income Tax Return:** statutory due date per NRA calendar; penalties vary by size (see §5).
* **Monthly Income Tax (PAYE/WHT remittances):** monthly, statutory due date; penalties vary by size.
* **Payroll Tax Return (employers):** **31 Jan** (annual) or **+1 month** from foreign employee start date.
* **GST Return:** **+21 days** from period end; **payment** still end of following month.
* **Excise Duty Return & Payment:** **+21 days** from domestic delivery/import date. 

---

## 4) Computation rules (engine-ready)

### 4.1 Income Tax & MAT

* **Inputs:** `accounting_profit_before_tax`, `tax_adjustments`, `loss_carryforward`, `revenue`, `loss_years_in_a_row`.
* **Taxable_Profit:** `accounting_profit_before_tax + tax_adjustments - allowable_deductions`.
* **Base Income Tax:** `taxable_profit * CIT_rate(year)` (rate table in config).
* **MAT Applicability:** if `loss_years_in_a_row >= 2` → `MAT = 0.02 * revenue` (incl. mining/petroleum).
* **Income Tax Payable:** `max(Base Income Tax, MAT) - credits - prepayments`. 

### 4.2 GST

* **Inputs:** itemized `outputs[]` (taxable sales), `inputs[]` (taxable purchases), relief flags.
* **Output GST:** sum over outputs: `price_excl_tax * 0.15`.
* **Input GST (creditable):** sum over inputs: `price_excl_tax * 0.15` (subject to rules).
* **Net GST (per period):** `OutputGST - InputGST`.
* **Special:** entities with Parliamentary/diplomatic relief: no GST at purchase; app must **block** GST charged at POS and **omit** refund workflow. **Schedules are itemized**. 

### 4.3 Excise Duty (specific rates)

Use HS mapping + product attributes; compute by unit/weight/volume:

| Item                                             |                      Rate (SLE) | Basis  |
| ------------------------------------------------ | ------------------------------: | ------ |
| Unmanufactured tobacco                           |                     **65 / kg** | weight |
| Cigars/cheroots/cigarillos (tobacco/substitutes) |                   **25 / pack** | packs  |
| Cigarettes (20 sticks)                           |                    **2 / pack** | packs  |
| Other manufactured tobacco; extracts/essences    |                     **65 / kg** | weight |
| Shisha & alternatives                            | **65 / kg** and **175 / litre** | dual   |
| E-cigarette device                               |                    **0.5 / ml** | ml     |
| E-cigarette cartridge                            |                  **0.8 / unit** | unit   |

* **Excise Due:** `sum(item_qty * rate)` by basis.
* **Return + payment due:** **+21 days** (enforce). 

### 4.4 Customs snippets (config flags)

* Rice import duty step: `5% (2025) → 10% (2026)`.
* Iron rods duty 10%; cooking gas 5% (confirmation in rates table).
* NGO vehicle import relief (caps per 5 years; enforce counters). 

### 4.5 Sector fees (PRA)

* **New entrant registration (selected categories):** **SLE 150,000**.
* **Regulatory fee per litre:** **0.40** (was 0.25).
* **Site inspection (Western Area):** **SLE 25,000**; **Bulk depot (mining):** **SLE 25,000**. 

---

## 5) Penalty engine (by tax type & taxpayer size)

Define a **Penalty Matrix** keyed by `{tax_type, filing_frequency, taxpayer_size, lateness_bracket}`.

### 5.1 Definitions

* **Late Filer:** filed **≤ 30 days** after due date.
* **Non-Filer:** filed **> 30 days** after due date.
* **Incorrect Filing (Income/Payroll):** % penalty on difference/misreport. 

### 5.2 Income Tax (Annual)

* **Late:** L **25,000** | M **12,500** | S **1,250**
* **Non-Filing:** L **50,000** | M **25,000** | S **2,500**

### 5.3 Income Tax (Monthly)

* **Late:** L **5,000** | M **2,500** | S **500**
* **Non-Filing:** L **10,000** | M **5,000** | S **1,000**
* **Incorrect Income Tax Filing:** **25%** of **tax difference due to under-reporting**. 

### 5.4 Payroll Tax

* **Late:** L **25,000** | M **12,500** | S **1,250**
* **Non-Filing:** L **50,000** | M **25,000** | S **2,500**
* **Incorrect Filing:** **25%** of **annual payroll tax per misreported employee**. 

### 5.5 GST

* **Late:** L **5,000** | M **2,500** | S **1,500**
* **Non-Filing:** L **10,000** | M **5,000** | S **1,000**. 

### 5.6 Excise Duty

* **Late:** L **5,000** | M **2,500** | S **500**
* **Non-Filing:** L **10,000** | M **5,000** | S **1,000**. 

> **Engine behavior:** On submission, compute `lateness_days`. Choose bracket, look up flat penalty; add % penalties where applicable. Aggregate by period & tax type; surface breakdown in UI. 

---

## 6) Data model (minimal viable schema)

### 6.1 Master data

* `Taxpayer(id, tin, legal_name, segment, industry, relief_flags, addresses[], contacts[])`
* `Registration(status, dates, attachments[], officer_routing)`
* `ConfigTaxRates(year, cit_rate, gst_rate=0.15, mat_rate=0.02, customs_tables, excise_tables[], penalty_tables[])`
* `ReliefEntitlement(type, effective_from, effective_to, caps, documents[])`

### 6.2 Period data

* `ReturnHeader(id, taxpayer_id, tax_type, period_start, period_end, filed_at, status, assessed_at)`
* `ReturnLines(...)` per tax:

  * **Income:** adjustments, losses, revenue, profit.
  * **GST:** outputs[], inputs[] (transaction lines with date, counterparty, invoice, HS if needed).
  * **Excise:** items (code, basis, qty, rate).
  * **Payroll:** employees (id, resident/foreign flag, wages, PAYE remitted), foreign_start_date.

### 6.3 Assessments & payments

* `Assessment(return_id, base_tax, mat, credits, penalty, interest, total_due)`
* `Payment(receipt_no, date, amount, method, allocation[])`
* `AuditLog(entity, entity_id, action, by, at, changes_json)`

---

## 7) UX & workflows

1. **Onboarding**

   * TIN lookup → auto-classify **segment** → collect industry/relief proofs → set filing calendar.
2. **Return prep (per tax type)**

   * Guided wizard → validations → **pre-compute** tax & penalties → preview → submit.
3. **GST schedules**

   * CSV/Excel import & API; enforce **itemized** lines + **dedup invoices**.
4. **Payroll**

   * Flag **foreign employees**; enforce **31 Jan / +1 month** deadline.
5. **Excise**

   * Product mapping (basis: kg/litre/unit/pack); compute specific duties.
6. **Payments**

   * Single checkout experience; allocate to liabilities; produce **compliance certificate**.
7. **Notices & reminders**

   * Smart reminders before due dates; late/non-filing escalation notices.

---

## 8) Validation rules (examples)

* **Segment validation:** turnover → segment; segment change requires evidence + approval.
* **GST:** require outputs/inputs schedules; block submission if missing or if relieved entity is charged at POS.
* **Payroll:** require foreign employee start date where `employee.is_foreign = true`.
* **Excise:** HS/basis must match allowed basis for rate; compute from qty.
* **Penalties:** due date present; lateness computed on **submission timestamp** (server time).
* **Cross-tax consistency:** revenue in Income vs. GST outputs reconciliation flag.

---

## 9) Configuration & versioning

* Year-keyed **rate books** (CIT, MAT, GST, customs, excise, fees).
* Year-keyed **penalty matrices** (amounts & rules).
* Feature flags: **GST threshold value** (kept configurable), relief categories, PRA fees.
* Effective-date engine to support mid-year changes (e.g., **rice duty step-up in 2026**). 

---

## 10) APIs (suggested)

* `POST /returns/{taxType}/{period}/draft`
* `POST /returns/{taxType}/{period}/validate`
* `POST /returns/{taxType}/{period}/submit`
* `GET /assessments/{returnId}`
* `POST /payments`
* `POST /imports/gst-outputs` | `gst-inputs` | `excise-items` | `payroll-lines`
* `GET /config/rates?year=YYYY`
* `GET /penalties/table?year=YYYY`
* `GET /notices/{taxpayerId}`

---

## 11) Reporting

* **Compliance dashboard:** filings on time vs. late; penalties by type/segment.
* **GST audit pack:** outputs, inputs, reconciliations, anomaly flags.
* **Payroll pack:** headcount, foreign starts, misreport penalties.
* **Excise pack:** quantities, duties by HS, basis integrity checks.

---

## 12) Testing checklist (high level)

* Segment change impact on penalties.
* MAT override scenarios (loss years toggling).
* GST relief behavior at POS (must not charge).
* Excise basis conversions (ml vs. litre; kg rounding).
* Late vs. Non-filer thresholds (≤30 vs. >30 days).
* Year roll-over (rice duty 2025→2026).

---

### Notes & open configs

* **GST registration threshold:** labeled “increased” in source; **store as configurable value** until confirmed.
* Keep all amounts in **SLE**; allow presentation rounding but preserve **exact** arithmetic in ledger. 

---

If you want, I can turn this into a ready-to-use **requirements doc (EARS)** or a **database schema** next.
