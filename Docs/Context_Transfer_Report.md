# FacePass: AI Context Transfer & Project Summary Report

## 1. Project Goal
**FacePass** is an enterprise biometric and spatial attendance ecosystem built using a C# .NET stack and Supabase. It eliminates proxy attendance through facial recognition, liveness detection, and GPS geofencing.

## 2. Technical Stack
- **Core SDK**: .NET 8.0
- **UI Frameworks**: WPF (Desktop) and .NET MAUI (Mobile)
- **Computer Vision**: Emgu CV 4.10 (OpenCV Wrapper)
- **Backend**: Supabase (PostgreSQL + PostGIS + Edge Functions + Storage)
- **Security**: BCrypt.Net-Next (Auth), iText7 (Reporting), VARBINARY Biometric Storage
- **Mobile Utilities**: ZXing.Net.Maui (QR Scanning), Microsoft.Maui.Devices.Sensors (GPS)

## 3. Module Breakdown & File Structure

### 3.1 Module 1: Desktop Kiosk (WPF)
- **Location**: `/FacePass/Kiosk/`
- **Core Services**:
  - `CameraService.cs`: Captures webcam feed.
  - `FaceDetectionService.cs`: Haar Cascade detection.
  - `FaceEncodingService.cs`: Extracts 128-d face encodings (LBPH approach).
  - `LivenessChallengeService.cs`: Random challenges (Smile, Blink, Head Turn).
  - `QrSessionService.cs`: Generates dynamic QR sessions every 30s.
- **ViewModel**: `MainWindowViewModel.cs` - Orchestrates the biometric pipeline.
- **View**: `MainWindow.xaml` - Premium dark-mode UI with biometric overlays.

### 3.2 Module 2: Mobile App (.NET MAUI)
- **Location**: `/FacePass/Mobile/`
- **Core Services**:
  - `GeofencingService.cs`: GPS tracking using Haversine formula (20m radius).
  - `SupabaseMobileService.cs`: Student-scoped REST client.
- **Views**:
  - `DashboardPage.xaml`: Attendance stats & history.
  - `ScannerPage.xaml`: Geofence-locked QR scanner.
  - `DisputePage.xaml`: Reason submission for flagged logs.
- **Backend**: `Backend/Functions/notify-attendance/` (Deno Edge Function for FCM).

### 3.3 Module 3: Management Portal (WPF)
- **Location**: `/FacePass/Management/`
- **Core Services**:
  - `AuthService.cs`: RBAC login using BCrypt verification.
  - `ReportService.cs`: iText7 PDF generation logic.
- **Views**:
  - `LoginWindow.xaml`: Role-based entry point.
  - `TeacherDashboard.xaml`: Live feed & Student Biometric Registration.
  - `AdminDashboard.xaml`: User CRUD & Audit Logs.

### 3.4 Shared Logic & Security
- **Location**: `/FacePass/Shared/`
- **`BiometricUtility.cs`**: Handles `float[]` to `byte[]` serialization for DB storage.
- **`impossible_travel_trigger.sql`**: PostgreSQL trigger for cross-building proxy detection.

## 4. Key Design Decisions (FOR THE AI)
1.  **Biometric Matching**: Uses Euclidean distance comparison against serialized LBPH histograms (BYTEA) in Supabase.
2.  **Liveness Verification**: Active challenges prevent photo/video spoofing. Challenge logic relies on EAR (Eye Aspect Ratio) and Nose tip X-offset.
3.  **QR Security**: QR codes are short-lived (30s) and signed by the server-side session GUID.
4.  **Database Integration**: Replaced heavy client-side SDKs with a lightweight custom `SupabaseService` using `HttpClient` for better performance in WPF/MAUI environments.

## 5. Current Implementation State
- [x] All 3 application projects created and configured.
- [x] All core services (Camera, Face, QR, Geofence, PDF, Auth) implemented.
- [x] Database schema, triggers, and Edge functions written.
- [x] Dark-mode design system consistent across all modules.

## 6. Known Prerequisites for Windows Boot
- **NuGet Restore**: Required for all 3 projects.
- **Haar Cascades**: Ensure `haarcascade_*.xml` files are in the `bin` output folder.
- **Supabase Credentials**: Must be updated in `appsettings.json` and `MauiProgram.cs`.

---
**Instruction for AI Instance**: Use this report to understand the architectural flow. The project is "Production Ready" in code but requires local credential configuration and asset placement (XMLs) to run. If debugging, prioritize checking Emgu CV library linking and Supabase RLS policies.
