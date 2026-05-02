# Module 1 Deployment Guide: Desktop Kiosk

## 1. Environment Requirements
- **OS**: Windows 10/11 (required for WPF and Emgu CV Windows runtime)
- **SDK**: .NET 8.0
- **Hardware**: USB Webcam or integrated camera

## 2. Configuration (`appsettings.json`)
Ensure your `appsettings.json` is correctly configured in the project root:
```json
{
  "Supabase": {
    "Url": "https://your-project-url.supabase.co",
    "AnonKey": "your-anon-key"
  },
  "Kiosk": {
    "ClassroomId": "GUID-OF-THE-CLASSROOM",
    "CourseId": "GUID-OF-THE-ACTIVE-COURSE"
  }
}
```

## 3. External Dependencies
The following Haar Cascade XML files must be present in the root directory (set to "Copy if newer"):
- `haarcascade_frontalface_default.xml` (Face detection)
- `haarcascade_eye.xml` (Blink detection)
- `haarcascade_mcs_mouth.xml` (Smile detection)

## 4. Build Instructions
Open a terminal in `/FacePass/Kiosk/` and run:
```bash
dotnet restore
dotnet build -c Release
```

## 5. Running the Application
1.  Navigate to `bin/Release/net8.0-windows/`.
2.  Launch `FacePass.Kiosk.exe`.
3.  The application will start in a windowed-fullscreen mode.

## 6. Verification
- **Webcam**: Ensure the feed appears immediately.
- **Bounding Box**: Stand in front of the camera; a green box should appear around your face.
- **QR Refresh**: Check that the QR code in the bottom-right corner changes every 30 seconds.
