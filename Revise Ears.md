Revise Ears

---

# 🧾 Client Tax Information System (CTIS) + Tax Filing App

**Requirements (EARS Format)**

**Sources:**

* *Finance Act 2025 (Sierra Leone) – The Betts Firm Summary* (deadlines, penalties, categories, MAT, GST rules).    
* *Concept Note & New Requirements* (CTIS scope, payments, KPIs, reports, chatbot, email, document mgmt).

**Version:** 2.0
**Date:** October 2025

---

## 1) Purpose

Ubiquitous: The system shall centralize client tax data, enable compliant filing (Income, Payroll, GST, Excise), manage documents and payments, surface compliance status/KPIs, and maintain auditability for The Betts Firm and clients.

---

## 2) Roles

Ubiquitous: The system shall support roles: **Client**, **BF Staff (Preparer/Reviewer)**, **BF Admin**, **Auditor (read-only)**.

---

## 3) Taxpayer Categories & Calendars (Legal)

* Ubiquitous: The system shall classify taxpayers by annual turnover: **Large (> SLE 6,000,000)**, **Medium (SLE 500,000–6,000,000)**, **Small (SLE 10,000–500,000)**, **Micro (≤ SLE 10,000)** and route to the appropriate office. 
* Event-Driven: When a GST period ends, the system shall require filing **within 21 days** (payment timing unchanged). 
* Event-Driven: Payroll Tax annual returns shall be due **31 January**; foreign employee filings shall be due **within 1 month of start**. 

---

## 4) Core Filing/Calculation (Legal)

### 4.1 Income Tax & MAT

* Ubiquitous: The system shall compute Base Income Tax = `Taxable Profit × CIT rate (year)`.
* State-Driven: When a company records losses for **≥ 2 consecutive years**, the system shall compute **MAT = 2% of revenue**, including **mining/petroleum**. 
* Event-Driven: On submission, the system shall set liability = `max(Base Tax, MAT)` and post to assessment.

### 4.2 GST

* Ubiquitous: The system shall compute **Output GST – Input GST** at **15%** and require **transaction-level** schedules for inputs/outputs.  
* State-Driven: For **diplomatic/ratified agreement** entities, the system shall **block GST at purchase** (no pay-and-refund). 

### 4.3 Excise

* Event-Driven: When goods are delivered for domestic use or imported, the system shall trigger a **21-day** timer for return + payment. 
* Ubiquitous: The system shall compute Excise as `qty × specific rate` (basis: unit/kg/litre/pack).

### 4.4 Customs Snippets (Config)

* State-Driven: The system shall apply **rice import duty 5% (2025) → 10% (2026)** via effective-date config. 

---

## 5) Penalty Matrix (Legal)

* Ubiquitous: The system shall apply penalties by **tax type** and **segment** using Late (≤30 days) vs **Non-Filing** (>30 days) logic. 
* Ubiquitous: The system shall support these tables (configurable by year):

**Income Tax (Annual & Monthly)**, **Payroll Tax**, **GST**, **Excise**: Late/Non-Filing amounts per **Large/Medium/Small**; **Incorrect Filing** rules (e.g., **Payroll: 25% per misreported employee**; **Income Tax: 25% on underreported difference**). 

---

## 6) CTIS: Client Dashboard & Information Hub (Phase 1)

* Ubiquitous: The system shall provide a **client login dashboard** showing tax years, filing dates, liabilities, and payment status.
* State-Driven: The system shall display a **compliance tracker** with status: **Pending | Filed | Paid | Overdue | Not Applicable**.
* Ubiquitous: The system shall provide a **document repository** (returns, receipts, notices, uploaded docs) with search and filters.
* Ubiquitous: The system shall render an **activity timeline** of actions, submissions, filings, payments, and communications.

---

## 7) Client–Firm Interaction (Phase 2)

* Optional: The system shall allow **client document submission** (drag-drop, capture metadata: tax year/type, period, tags).
* Event-Driven: When a required document is missing for a filing, the system shall **open a request** to the client with due date and checklists.
* Ubiquitous: The system shall support **notifications & alerts** (email/SMS + in-app).
* Optional: The system shall provide **secure messaging** between client and staff (threaded; per tax period/case).

---

## 8) Payment System Integration (Phase 3)

* Ubiquitous: The system shall support **multiple payment methods**: **cash, cheque, bank transfer**; and shall allow plugging additional gateways later.
* Optional: The system shall enable **client-initiated payment requests** from the dashboard (for BF to pay on behalf).
* Event-Driven: When a payment is recorded (manual entry or gateway callback), the system shall:

  * create a **transaction record** (who/when/what, tax type/period, amount, method),
  * link the **receipt** to the filing and ledger,
  * update **compliance status** and **payment KPIs**.
* State-Driven: Where approvals are required, the system shall enforce **client approval** before BF executes a payment.

---

## 9) Compliance Tab & Metrics (Client View)

* Ubiquitous: The system shall display a **Compliance Tab** with: Status summary, Filing checklist (GST, PAYE, Income, Excise), Upcoming deadlines, **Penalty warnings** (estimated), **Document tracker**.
* Ubiquitous: The system shall show **visual metrics** (tiles/graphs):

  * **Compliance Score** (Green/Yellow/Red),
  * **Filing Timeliness %**, **Payment Timeliness %**,
  * **Supporting Documents Status % (Complete/Pending/Rejected)**,
  * **Deadline Adherence trend** (month-by-month).

---

## 10) KPIs & Dashboards

### 10.1 Internal (The Betts Firm)

* Ubiquitous: The system shall compute:

  * **Client Compliance Rate** (e.g., % filed before deadline),
  * **Tax Filing Timeliness** (avg days before deadline),
  * **Payment Completion Rate** (completed vs initiated),
  * **Document Submission Compliance** (% clients who submitted all required docs on time),
  * **Client Engagement Rate** (logins, chats, uploads).
* Optional: The system shall support KPI target setting and red/amber/green thresholds.

### 10.2 Client

* Ubiquitous: The system shall display per-client KPIs:

  * **My Filing Timeliness** (e.g., average 3 days before deadline),
  * **On-Time Payments %**,
  * **Document Readiness Score %**,
  * **Compliance Score** (composite/traffic-light).

---

## 11) Reports (PDF & Excel)

### 11.1 Client-Facing

* Ubiquitous: The system shall generate on demand:

  * **Tax Filing Report** (monthly/quarterly/annual; filings, dates, statuses, tax types),
  * **Payment History** (who/when/what/tax type and dates),
  * **Compliance Report** (missed deadlines, delayed filings, pending obligations),
  * **Document Submission Report** (submitted/pending/rejected),
  * **Tax Calendar Summary** (upcoming/past obligations + status).

### 11.2 Internal (BF Staff)

* Ubiquitous: The system shall generate:

  * **Client Compliance Overview** (all clients, any time range),
  * **Revenue Collected/Processed** (by tax type/client/date range),
  * **Client Activity Logs** (login frequency, messages, submissions, payments),
  * **Case Management Report** (issues, resolutions, SLAs).

---

## 12) Chatbox / Chatbot

* Ubiquitous: The system shall store **complete chat history** accessible to clients and BF staff attached to the client/case/tax period.
* Admin-Only: The system shall let staff **assign conversations** to team members and add **internal-only notes**.

---

## 13) Tax Types & Currency

* Ubiquitous: The system shall support **PAYE, WHT, PIT, CIT, GST, Excise, Payroll Tax** for records, filings, payments, reports.
* Ubiquitous: The system shall store and display all monetary values in **Sierra Leone Leones (SLE)** and label outputs/receipts with the currency.

---

## 14) Document Management

* Ubiquitous: The system shall support secure upload, storage, versioning, and retrieval for:

  * **Tax Year**, **Tax Type** (metadata),
  * **Tax Returns**, **Tax Receipts**, **Tax Calculations**, **ITAS Submission Forms**, and client-supplied **supporting documents**.
* Event-Driven: On replacement upload, the system shall **version** the document and retain prior versions with timestamps and uploader.

---

## 15) Email/SMS Notifications

* Event-Driven: **10 days before** each filing/payment deadline, the system shall email clients.
* Event-Driven: After the first reminder, the system shall send **daily reminders** up to the deadline to clients who have not filed/paid.
* Ubiquitous: The **default sender** shall be **[clientaccounts@thebettsfirmsl.com](mailto:clientaccounts@thebettsfirmsl.com)** (configurable).
* Event-Driven: On overdue, the system shall send **late** and **non-filing** notices aligned to penalty windows (≤30 vs >30 days). 

---

## 16) Audit, Security & Integrity

* Ubiquitous: The system shall maintain an **immutable audit trail** for: filings, assessments, payments, document actions, and user actions (who/when/what, before/after values).
* Ubiquitous: The system shall enforce **role-based access control** and **least-privilege** access to client data.
* Ubiquitous: The system shall log **payment approvals** and link **receipts** to filings.

---

## 17) Configurability (Year-Versioned)

* Ubiquitous: The system shall store **rate books** per year (CIT, MAT=2%, GST=15%, Excise tables; customs snippets) and **penalty matrices** per year.  
* State-Driven: The system shall use **effective dates** to apply changes mid-year (e.g., rice duty step-up 2026). 

---

## 18) Non-Functional Requirements

* Performance: Filing validation and tax computation shall complete in **≤ 2 seconds** for typical returns.
* Availability: Client dashboards shall target **99.5%** monthly uptime.
* Observability: The system shall expose metrics for KPIs calculation jobs, reminder jobs, and payment posting.
* Export: All reports shall be downloadable in **PDF** and **Excel**.

---

## 19) Open Items & Assumptions

* (Open) Exact **CIT/WHT** rate tables to be confirmed from NRA circulars (kept configurable).
* (Open) Additional **payment gateways** (mobile money/cards) planned; Phase-in via adapters.
* (Assumption) Email/SMS provider supports **daily cadence** and sender ID configuration.

---

### Appendix A — Penalty Tables (Config seeds)

* Include the Finance-Act penalty values for **Income Tax (Annual/Monthly)**, **Payroll Tax**, **GST**, **Excise**, and the definitions of **Late** vs **Non-Filing**.  

---

If you want, I can now:

1. map this to a **database schema & service APIs**, and
2. draft the **KPI calculations** (SQL/ETL formulas) and **report layouts** (fields + filters),
   so engineering can start right away.
