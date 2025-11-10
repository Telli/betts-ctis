# Demo User Credentials

## Test Users (For Development/Testing)

### Admin User
- **Email:** `admin@bettsfirm.sl`
- **Password:** `Admin123!`
- **Role:** Admin
- **Use:** Full system administration and testing

### Associate User
- **Email:** `associate@bettsfirm.sl`
- **Password:** `Associate123!`
- **Role:** Associate
- **Use:** Testing associate-level features (tax filing, client management)

### Client User
- **Email:** `client@testcompany.sl`
- **Password:** `Client123!`
- **Role:** Client
- **Business:** Test Company Ltd
- **Use:** Testing client portal features

---

## System Admin (Production-like)

- **Email:** `admin@thebettsfirmsl.com`
- **Password:** `AdminPass123!`
- **Role:** SystemAdmin
- **Use:** System-level configuration and management

---

## Sample Clients (Portal Users)

### Sierra Mining Corporation
- **Email:** `john.kamara@sierramining.sl`
- **Password:** `Demo123!`
- **Role:** Client
- **TIN:** TIN-001-2024
- **Type:** Large Corporation

### Freetown Logistics Ltd
- **Email:** `fatima.sesay@freetownlogistics.sl`
- **Password:** `Demo123!`
- **Role:** Client
- **TIN:** TIN-002-2024
- **Type:** Medium Corporation

---

## Sample Associates (Staff Users)

### Associate 1
- **Email:** `associate1@thebettsfirmsl.com`
- **Password:** `Associate123!`
- **Role:** Associate
- **Name:** Sarah Bangura

### Associate 2
- **Email:** `associate2@thebettsfirmsl.com`
- **Password:** `Associate123!`
- **Role:** Associate
- **Name:** Mohamed Conteh

---

## Additional Demo Clients

The system also includes 3 additional demo client companies without portal access:
- Atlantic Petroleum Services (TIN-003-2024)
- Diamond Mining Co Ltd (TIN-004-2024)
- Kono Agricultural Enterprises (TIN-005-2024)

These clients have associated tax filings and payment records for testing reporting features.

---

## Notes

- All demo users are created automatically on first application startup
- Demo data is only seeded if no clients exist in the database
- To reset demo data, delete the `BettsTax.db` file and restart the application
- All passwords meet the default ASP.NET Identity requirements:
  - At least 6 characters
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one digit
  - At least one special character
