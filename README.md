# FitTrack - Gym Membership & Wallet Management System

FitTrack is a modern, high-performance web application built with **ASP.NET Core 10.0 MVC**. It is designed to streamline gym operations, from secure member check-ins using tamper-proof QR codes to a robust digital wallet system for seamless financial transactions.

## 🌟 Key Features

### 🛡️ Secure QR Check-in System
*   **Tamper-Proof QR Codes**: Utilizes the ASP.NET Core Data Protection API to generate encrypted, time-sensitive, and verifiable check-in payloads.
*   **Instant Invalidation**: Members can regenerate their QR tokens instantly, which immediately invalidates any previously generated or scanned codes.
*   **Admin Scanner**: A dedicated scanning interface for gym staff to verify member identity and membership status in real-time.

### 💳 Integrated Digital Wallet System
*   **Secure Ledger**: Every user is assigned a digital wallet with a detailed transaction history.
*   **Flexible Funding**: Support for manual deposit requests with image-based proof of payment.
*   **Peer-to-Peer Transfers**: Securely transfer funds to other users within the platform using their unique identifiers.
*   **Membership Automation**: Pay for and renew membership tiers directly using wallet balances.

### 👥 User & Admin Portals
*   **Role-Based Access Control (RBAC)**: Distinct portals for Administrators and Members powered by ASP.NET Core Identity.
*   **Tiered Memberships**: Support for multiple membership levels (e.g., Classic, Premium) with dynamic pricing and duration logic.
*   **Profile Customization**: Image upload support for profile pictures and comprehensive account management.

### 📊 Administrative Tools
*   **Centralized Dashboard**: Real-time overview of active memberships, system-wide financial health, and check-in logs.
*   **Request Management**: A workflow-driven system for approving or rejecting wallet deposit requests.
*   **Full Audit Trail**: Searchable logs for all transactions and gym entries.

## 🛠️ Technology Stack

*   **Framework**: [ASP.NET Core 10.0 MVC](https://dotnet.microsoft.com/en-us/apps/aspnet)
*   **Database**: [PostgreSQL](https://www.postgresql.org/) (Optimized for Supabase)
*   **ORM**: [Entity Framework Core 10.0](https://learn.microsoft.com/en-us/ef/core/)
*   **Security**: ASP.NET Core Identity & Data Protection API
*   **Frontend**: Razor Views, Vanilla CSS (with CSS Isolation), Modern JavaScript
*   **Deployment**: Ready for Azure Web Apps (via GitHub Actions)

## 🔐 Technical Deep Dive: QR Security

The QR check-in system is designed with a "Zero-Trust" approach:
1.  **Payload Protection**: Member ID and a unique `QrCodeToken` are serialized to JSON and encrypted using **AES-256-CBC + HMAC** via the ASP.NET Data Protection API.
2.  **Unpredictable Tokens**: Tokens are generated using `RandomNumberGenerator.GetBytes(32)`, providing 256 bits of entropy.
3.  **Encapsulation**: The QR code never contains raw user data; it only contains an opaque, encrypted string that can only be decrypted by the FitTrack server.

## 🚀 Getting Started

### Prerequisites
*   [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
*   [PostgreSQL](https://www.postgresql.org/download/) (Local instance or Supabase)
*   IDE: Visual Studio 2022 (v17.11+), JetBrains Rider, or VS Code

### Local Setup

1.  **Clone the Repository**:
    ```bash
    git clone https://github.com/yourusername/FitTrack.git
    cd FitTrack
    ```

2.  **Configure Database**:
    We recommend using [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store your connection string:
    ```bash
    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=your_host;Database=FitTrackDB;Username=your_user;Password=your_password"
    ```

3.  **Initialize Database**:
    Apply the Entity Framework migrations to set up your PostgreSQL schema:
    ```bash
    dotnet ef database update
    ```

4.  **Launch the App**:
    ```bash
    dotnet run
    ```
    The application will automatically seed the database with initial roles and a default administrator account on the first run.

## 📁 Project Structure

*   `/Controllers`: HTTP request handlers and business logic orchestration.
*   `/Models`: Core domain entities (Users, Wallets, Transactions, Check-in Logs).
*   `/Services`: Reusable logic like `MemberQrCodeService`.
*   `/ViewModels`: Specialized data structures for UI-Controller interaction.
*   `/Views`: Razor templates for the user interface.
*   `/Data`: `ApplicationDbContext` and database migrations.
*   `/wwwroot`: Static assets (CSS, JS, images, uploads).

---
*Built for gym owners and fitness enthusiasts.*

## 👥 Development Team

*   **Mary Jasmin Pancho** — Project Manager
*   **Paul Alarba** — Lead Programmer
*   **John Alfred Lupian** — Programmer
*   **Althea Kathleen** — Lead UI/UX Designer
*   **Joemarie Jean Tibur** — UI/UX Designer
