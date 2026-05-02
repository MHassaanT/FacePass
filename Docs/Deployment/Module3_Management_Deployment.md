# Module 3 Deployment Guide: Management Portal

## 1. Environment Requirements
- **OS**: Windows 10/11
- **SDK**: .NET 8.0
- **Hardware**: Webcam (for Biometric Student Registration)

## 2. Configuration (`LoginWindow.xaml.cs`)
Ensure your Supabase credentials are set in the `LoginWindow` constructor or a global config:
```csharp
string url = "https://your-project.supabase.co";
string key = "your-anon-key";
```

## 3. iText7 Dependencies
The portal uses `itext7` for PDF generation. No manual DLL placement is required as it is managed via NuGet, but ensure the application has write permissions to the destination folder for reports (e.g., `Documents/Reports`).

## 4. Biometric Registration Setup
Place `haarcascade_frontalface_default.xml` in the root directory. This is required for the "Capture Face" feature in the Teacher Dashboard.

## 5. Build Instructions
Open a terminal in `/FacePass/Management/` and run:
```bash
dotnet restore
dotnet build -c Release
```

## 6. Verification
- **RBAC**: Attempt to login with a 'student' role; the app should deny access.
- **Registration**: Register a test face and verify the `BYTEA` blob appears in the `students` table.
- **Reporting**: Click "Generate Report" and verify a PDF file is created with the correct table layout.
