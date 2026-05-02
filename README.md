# FitTrack - Gym Membership & Wallet Management System

FitTrack is a comprehensive web application built with ASP.NET Core MVC. It is designed to manage gym memberships, user profiles, and incorporates a robust digital wallet system for seamless financial transactions. 

## 🌟 Key Features

### User Management & Authentication
* **Secure Authentication**: Registration and login powered by ASP.NET Core Identity.
* **Role-Based Access Control (RBAC)**: Distinct, isolated portals and permissions for Administrators and Members.
* **Profile Management**: Users can update their personal details and account settings directly from their dashboard.

### Administrator Operations
* **Centralized Dashboard**: A comprehensive overview of system statistics, active users, and financial summaries.
* **User Administration**: Capabilities to view, edit, manage, and remove member accounts from the system.
* **Transaction Oversight**: Full visibility into all system-wide financial movements, deposits, and payments.
* **Request Approvals**: Ability to review and approve or reject manual payment requests for memberships.

### Member Experience
* **Personalized Dashboard**: At-a-glance view of active membership status, wallet balance, and recent activities.
* **Tiered Memberships**: Select and upgrade between different membership tiers (e.g., Classic, Premium/Black Card) with dynamic pricing validation.
* **Activity Tracking**: Detailed, personalized ledger of all deposits, transfers, and membership payments.

### Integrated Digital Wallet System
* **Wallet Balances**: Every user is assigned a digital wallet upon registration.
* **Fund Deposits**: Seamlessly deposit funds into the digital wallet for future use.
* **Peer-to-Peer Transfers**: Transfer funds securely to other users within the platform.
* **Membership Payments**: Directly utilize the wallet balance to seamlessly pay for active gym memberships.

## 🛠️ Technology Stack
* **Framework**: ASP.NET Core MVC (C#)
* **Database**: Microsoft SQL Server
* **ORM**: Entity Framework Core
* **Security**: ASP.NET Core Identity
* **Frontend**: HTML5, Vanilla CSS (with modern, responsive UI design), JavaScript, Razor Views

## 🚀 How to Run the Project locally

### Prerequisites
* [.NET SDK](https://dotnet.microsoft.com/download) (Version 8.0 or newer)
* [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express, LocalDB, or Developer edition)
* IDE: Visual Studio 2022, JetBrains Rider, or Visual Studio Code

### Installation Steps

1. **Navigate to the project directory**:
   Ensure you are in the root folder containing the `FitTrack.sln` or `FitTrack.csproj` file.

2. **Configure the Database Connection**: 
   Open `appsettings.json` and verify the `DefaultConnection` string accurately points to your local SQL Server instance.
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=FitTrackDB;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

3. **Apply Database Migrations**:
   Open your terminal in the project root and execute the Entity Framework migrations to create your database schema:
   ```bash
   dotnet ef database update
   ```
   *Note: If using Visual Studio, you can run `Update-Database` in the Package Manager Console.*

4. **Run the Application**:
   Execute the following command in your terminal:
   ```bash
   dotnet run
   ```
   Alternatively, press `F5` in Visual Studio to start debugging.

5. **Initial Setup & Seeding**:
   Upon the application's first launch, the `SeedData` logic in `Program.cs` will automatically create the necessary Roles (Admin, Member) and provision a default Administrator account if one does not exist. 

## 📁 Project Structure Overview
* `/Controllers`: Contains the C# classes handling HTTP requests and orchestrating business logic.
* `/Models`: Defines the core domain entities (e.g., `ApplicationUser`, `Wallet`, `Transaction`) representing the database schema.
* `/ViewModels`: Specialized models tailored specifically for data transfer between Controllers and Views.
* `/Views`: Contains all the Razor (`.cshtml`) pages responsible for the application's UI.
* `/Data`: Houses the `ApplicationDbContext` and Entity Framework migration history.
* `/wwwroot`: The public-facing directory for static web assets like custom CSS, JavaScript, and images.
