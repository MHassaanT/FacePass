# FacePass: Module 2 Technical & Functionality Report
## Mobile App Ecosystem (.NET MAUI)

### 1. Overview
The FacePass Mobile App is a student-centric tool designed to provide a secure, location-aware method of marking attendance using dynamic QR codes. It leverages native mobile sensors for geofencing and push notifications to provide immediate feedback.

### 2. Technical Architecture
The application is built using **.NET MAUI**, enabling a single C# codebase to target both Android and iOS. It utilizes Dependency Injection (DI) to manage services and follows a clean View-Service separation.

#### 2.1 Core Technologies
- **Framework**: .NET 8.0 MAUI
- **Sensors**: Microsoft.Maui.Devices.Sensors (GPS)
- **Scanning**: ZXing.Net.Maui (QR/Barcode)
- **Networking**: HttpClient + Newtonsoft.Json
- **Backend Logic**: Supabase Edge Functions (Deno/TypeScript)

### 3. Feature Breakdown

#### 3.1 GPS Geofencing (Anti-Proxy Measure)
To prevent students from sharing QR code photos with peers outside the classroom, the app implements a strict geofence:
- **Algorithm**: Haversine Formula (Sphere distance calculation).
- **Radius**: 20 Meters from the classroom centroid.
- **Enforcement**: The QR scanner is programmatically disabled (`IsDetecting = false`) and the UI is locked with a warning if the student is outside the range.

#### 3.2 Secure QR Scanning
- **Validation**: Scanned payloads are decrypted/parsed to check the `expires_at` UTC timestamp.
- **Redundancy**: The scan only succeeds if the student is physically present (verified by GPS) and the QR is active (verified by time).

#### 3.3 Student Dashboard & Disputes
- **Statistics**: Real-time calculation of attendance percentages and session counts.
- **Dispute Workflow**: Allows students to submit a `flagged_reason` for records marked as "Suspicious" during the Kiosk biometric phase.

#### 3.4 Push Notifications
- **Trigger**: DB Webhook on `attendance_logs`.
- **Infrastructure**: Supabase Edge Function → Firebase Cloud Messaging (FCM).
- **Payload**: Confirmation message sent to the student's registered device token.

### 4. Implementation Details

| Component | Responsibility | Technical Implementation |
| :--- | :--- | :--- |
| `GeofencingService` | Location calculation | `Geolocation.GetLocationAsync` + Haversine |
| `ScannerPage` | QR Camera Interface | `zxing:CameraBarcodeReaderView` |
| `SupabaseMobileService` | Mobile API Proxy | RESTful PATCH/POST with student scope |
| `notify-attendance` | Backend Push Trigger | Deno Runtime + FCM REST API |

### 5. UI/UX Design
The mobile experience prioritizes speed and clarity:
- **Interactive Geofence Indicator**: A color-coded header (Green/Red) provides instant feedback on location status.
- **Tabbed Navigation**: Easy access to Dashboard, Scanner, and Profile.
- **Aesthetic**: Consistent dark-mode theme to match the Desktop Kiosk.

### 6. Privacy & Security
- **Scoped API Access**: Mobile calls are restricted to the student's own records.
- **Hardware Permissions**: Uses standard Android/iOS permission prompts for Location and Camera.

---
**Status**: Production Ready
**Last Updated**: 2026-04-29
**Author**: Lead AI Architect (Antigravity)
