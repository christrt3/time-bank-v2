# Alachua Community Collective — Setup Guide

## Getting Started in Visual Studio 2022

### 1. Restore NuGet Packages
Right-click the solution → **Restore NuGet Packages**, or let VS auto-restore on build.

### 2. Build the Project
Press **Ctrl+Shift+B** or **Build → Build Solution**. All packages will be downloaded automatically.

### 3. Run the App
Press **F5** (Debug) or **Ctrl+F5** (Run without debug). The SQLite database will be created automatically at:
```
Time-Bank-V1-master/acc_timebank.db
```
Seed data (categories, skills, admin account) is created automatically on first run.

### 4. Default Admin Login
| Field | Value |
|-------|-------|
| Email | admin@acc.org |
| Password | Admin@12345! |

---

## Project Structure

```
├── Data/
│   ├── Entities/           → All EF Core entity models
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs    → Seeds categories, skills, admin user
├── Services/
│   ├── TransactionService.cs
│   ├── OfferService.cs
│   └── AdminService.cs
├── Pages/
│   ├── Index.cshtml        → Login + Registration
│   ├── Dashboard.cshtml    → Main user dashboard
│   ├── Profile.cshtml      → User profile & settings
│   ├── Offers.cshtml       → Browse services
│   ├── Transaction.cshtml  → Time account & history
│   ├── TimeLog.cshtml      → Log completed hours
│   ├── Offers/
│   │   ├── Create.cshtml   → Post a new offer/request
│   │   └── Details.cshtml  → View offer details
│   ├── Confirmations/
│   │   └── Index.cshtml    → Confirm pending transactions
│   ├── Messages/
│   │   ├── Index.cshtml    → Inbox / Sent
│   │   └── Compose.cshtml  → Write a message
│   └── Admin/
│       ├── Index.cshtml    → Admin dashboard
│       ├── Users.cshtml    → Manage users + grant hours
│       ├── Categories.cshtml → Manage service categories
│       └── Reports.cshtml  → Platform reports + CSV export
├── wwwroot/
│   ├── css/site.css        → Styles + accessibility
│   ├── js/site.js          → Text-to-speech, text resize, contrast
│   └── uploads/avatars/    → Profile pictures stored here
└── Program.cs              → App configuration, DI registration
```

---

## Database Migrations (Optional)

The app uses `Database.EnsureCreated()` to auto-create the schema on first run.

For proper EF Core migrations (recommended for production):

**Package Manager Console:**
```
Add-Migration InitialCreate
Update-Database
```

---

## Key Features Implemented

| Feature | Location |
|---------|----------|
| Registration + Login (ASP.NET Core Identity) | Index.cshtml |
| Profile with photo upload, skills, availability | Profile.cshtml |
| Public profile visibility controls | Profile.cshtml |
| Service offers + requests with categories | Offers.cshtml, Offers/Create.cshtml |
| Keyword/location/type matching | Offers.cshtml + OfferService |
| Mutual confirmation handshake | Confirmations/Index.cshtml |
| Balance tracking with overdraft protection (-3h max) | TransactionService.cs |
| Donate hours to other users | TransactionService.cs |
| Anonymous messaging + category broadcast | Messages/ |
| Positive-only peer feedback | Confirmations/Index.cshtml |
| Admin: reports, grant hours, flag users | Admin/ |
| Admin: manage categories (add/edit/deactivate) | Admin/Categories.cshtml |
| Monthly accessibility grants (configurable) | AdminService + TransactionService |
| Text resize (A+/A-) | site.js + Accessibility toolbar |
| Text-to-speech on hover | site.js |
| High contrast mode | site.js |
| English / Spanish toggle | Layout.cshtml |
| CSV export (transactions + users) | Transaction.cshtml, Admin/Reports.cshtml |
| Role-based auth (Admin, Member, Organization) | Program.cs |

---

## Password Requirements
- Minimum 8 characters
- At least 1 uppercase letter
- At least 1 digit
- At least 1 non-alphanumeric character

Example: `Password@1`
