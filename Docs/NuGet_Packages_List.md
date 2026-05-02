# FacePass: NuGet Dependencies List

This document provides a consolidated list of NuGet packages required for each project in the FacePass ecosystem.

## 1. FacePass.Kiosk (Desktop Kiosk)
Used for biometric detection, liveness, and QR generation.

- `Emgu.CV` (v4.10.0)
- `Emgu.CV.runtime.windows` (v4.10.0)
- `Emgu.CV.UI` (v4.10.0)
- `Emgu.CV.Face` (v4.10.0)
- `QRCoder` (v1.5.0)
- `Newtonsoft.Json` (v13.0.3)
- `Microsoft.Extensions.Configuration` (v8.0.0)
- `Microsoft.Extensions.Configuration.Json` (v8.0.0)
- `System.Drawing.Common` (v8.0.0)

## 2. FacePass.Mobile (Student App)
Used for GPS geofencing and QR scanning.

- `ZXing.Net.Maui` (v0.4.0)
- `ZXing.Net.Maui.Controls` (v0.4.0)
- `Microsoft.Maui.Devices.Sensors` (v8.0.0)
- `Newtonsoft.Json` (v13.0.3)
- `Microsoft.Maui.Controls` (v8.0.0)

## 3. FacePass.Management (Admin/Teacher Portal)
Used for RBAC, registration, and PDF reporting.

- `BCrypt.Net-Next` (v4.0.3)
- `itext7` (v8.0.2)
- `Newtonsoft.Json` (v13.0.3)
- `Emgu.CV` (v4.10.0)
- `Emgu.CV.runtime.windows` (v4.10.0)
- `Microsoft.Extensions.Configuration` (v8.0.0)
- `System.Net.Http.Json` (v8.0.0)

---
**Note**: To install all packages at once, open `FacePass.sln` in Visual Studio 2022, right-click the **Solution**, and select **"Restore NuGet Packages"**.
