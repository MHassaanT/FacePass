# Module 2 Deployment Guide: Mobile App (.NET MAUI)

## 1. Environment Requirements
- **IDE**: Visual Studio 2022 (with MAUI workload)
- **Platforms**: Android 5.0+ (API 21) or iOS 14.2+
- **Hardware**: Physical device recommended for GPS and Camera testing

## 2. Configuration (`MauiProgram.cs`)
Replace placeholders in `MauiProgram.cs` with your Supabase credentials:
```csharp
builder.Services.AddSingleton(new SupabaseMobileService(
    "https://your-project.supabase.co", 
    "your-anon-key"
));
```

## 3. Platform Specifics (Permissions)
The app is pre-configured to request:
- `LocationWhenInUse`
- `Camera`
- `RemoteNotifications`

### 3.1 Android (AndroidManifest.xml)
Ensure the following are present:
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.CAMERA" />
```

### 3.2 iOS (Info.plist)
Ensure the following are present:
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>FacePass needs your location to verify you are in class.</string>
<key>NSCameraUsageDescription</key>
<string>FacePass needs camera access to scan QR codes.</string>
```

## 4. Push Notifications (FCM)
1.  Register your app in the **Firebase Console**.
2.  Download `google-services.json` (Android) and `GoogleService-Info.plist` (iOS).
3.  Add them to the project:
    - Android: `Platforms/Android/`
    - iOS: `Platforms/iOS/`

## 5. Build & Deploy
Select your target device in Visual Studio and press **F5**.

## 6. Verification
- **Geofence**: Check if the header turns green when you are at the target coordinates.
- **Scanner**: Verify the scanner detects the QR code from the Kiosk.
- **Dashboard**: Ensure your historical logs are fetched from Supabase.
