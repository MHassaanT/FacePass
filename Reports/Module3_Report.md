# FacePass: Module 3 Technical & Functionality Report
## Management Portal Ecosystem (WPF)

### 1. Overview
The Management Portal is the administrative backbone of the FacePass system. It provides high-level oversight for Administrators and day-to-day operational tools for Teachers, including biometric registration and real-time attendance monitoring.

### 2. Technical Architecture
Built as a **WPF .NET 8.0** application, the portal focuses on data density and administrative efficiency. It uses a modular View/ViewModel structure to handle multi-role dashboards within a single navigation host.

#### 2.1 Core Technologies
- **Framework**: .NET 8.0 (WPF)
- **Security**: BCrypt.Net-Next (Password Hashing)
- **Reporting**: iText7 (PDF Generation)
- **Computer Vision**: Emgu CV 4.10 (Biometric Registration)

### 3. Feature Breakdown

#### 3.1 RBAC Authentication (Security First)
- **Logic**: Authentication is decoupled from the database scope. Passwords are never stored in plain text; instead, the system uses **BCrypt** with a high cost-factor to hash passwords before insertion.
- **Routing**: Upon successful login, the `AuthService` retrieves the user's role and the `MainWindow` dynamically swaps the content view to either the `TeacherDashboard` or `AdminDashboard`.

#### 3.2 Teacher Dashboard: Real-time & Registration
- **Live Feed**: A high-priority DataGrid that monitors the `attendance_logs` table.
- **Biometric Registration**: Integrated with the Kiosk's `FaceEncodingService`, allowing teachers to capture a student's face ROI and store the resulting 128-d encoding as a **VARBINARY (BYTEA)** blob in the `students` table.
- **Manual Overrides**: Provides a fail-safe for physical attendance verification with audit-logged reasons.

#### 3.3 Admin Dashboard: Governance & Auditing
- **User CRUD**: A complete management interface for Students, Teachers, and Admins.
- **Audit Logs**: A dedicated viewer for the `audit_logs` table, providing transparency into administrative actions and system overrides.

#### 3.4 PDF Reporting (iText7)
- **Generation**: The `ReportService` builds dynamic PDF documents including headers, student metadata, and a color-coded attendance table.
- **Compliance**: Reports are designed to meet academic standards for record-keeping.

### 4. Implementation Details

| Component | Responsibility | Technical Implementation |
| :--- | :--- | :--- |
| `AuthService` | Identity Verification | BCrypt hashing + Role-based redirection |
| `ReportService` | Document Generation | iText7 Kernel & Layout API |
| `TeacherDashboard` | Operations | Emgu CV ROI Extraction + Real-time DataGrid |
| `UserDialog` | Administrative CRUD | MVVM Modal with input validation |

### 5. UI/UX Design
The portal uses a "Command Center" aesthetic:
- **Navigation Shell**: A consistent header with role indicators and user greetings.
- **Glassmorphism Elements**: Modern UI components with subtle shadows and high contrast.
- **Accessibility**: High-legibility fonts (Segoe UI) and color-blind-friendly status indicators.

---
**Status**: Production Ready
**Last Updated**: 2026-04-29
**Author**: Lead AI Architect (Antigravity)
