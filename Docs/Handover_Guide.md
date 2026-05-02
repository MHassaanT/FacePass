# FacePass: Project Handover & Deployment Guide

## 1. System Overview
FacePass is an integrated attendance ecosystem consisting of a WPF Desktop Kiosk, a .NET MAUI Mobile App, and a WPF Management Portal, all synchronized via Supabase.

## 2. Prerequisites
- **IDE**: Visual Studio 2022 (with .NET MAUI & WPF workloads)
- **SDK**: .NET 8.0
- **Database**: Supabase Project (PostgreSQL + PostGIS)
- **FCM**: Firebase Project (for Push Notifications)

## 3. Database Setup (Supabase)

### 3.1 Schema Deployment
1.  Run the SQL schema provided in the initial specification in the Supabase SQL Editor.
2.  Ensure the `postgis` extension is enabled: `CREATE EXTENSION IF NOT EXISTS postgis;`.

### 3.2 Security Triggers
1.  Apply the `impossible_travel_trigger.sql` located in `/media/hassaan/New Volume/FacePass/Database/Triggers/`.

### 3.3 Storage Buckets
1.  Create a public bucket named `avatars` in Supabase Storage.
2.  Set RLS (Row Level Security) policies to allow authenticated uploads.

## 4. Backend Deployment (Edge Functions)

### 4.1 Push Notifications
1.  Install Supabase CLI.
2.  Navigate to `/media/hassaan/New Volume/FacePass/Backend/`.
3.  Deploy the function: `supabase functions deploy notify-attendance`.
4.  Set the `FCM_SERVER_KEY` environment variable in Supabase Secrets.

## 5. Application Configuration

### 5.1 Desktop Kiosk & Management Portal
- Update `appsettings.json` in both projects with:
  - `Supabase:Url`
  - `Supabase:AnonKey`
  - `Kiosk:ClassroomId` (Specific to the physical location)

### 5.2 Mobile App
- Update `MauiProgram.cs` with your Supabase URL and Anon Key.
- Add `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) to the `Platforms/` folders.

## 6. Build Instructions

### 6.1 Desktop Apps (Kiosk & Management)
```bash
dotnet restore
dotnet build -c Release
```
Ensure Haar Cascade XML files are present in the output directory.

### 6.2 Mobile App (MAUI)
```bash
dotnet build -t:Run -f net8.0-android
# or
dotnet build -t:Run -f net8.0-ios
```

## 7. Operational Workflow
1.  **Admin**: Creates Teacher/Student accounts in the Management Portal.
2.  **Teacher**: Registers student face encodings using the "Registration" tab in the portal.
3.  **Kiosk**: Starts up in the classroom, displaying video feed and dynamic QR.
4.  **Student**: 
    - Approaches Kiosk → Face detected → Liveness challenge passed → Attendance marked.
    - **OR** Scans Kiosk QR with Mobile App while inside the geofence.
5.  **Audit**: Admin exports PDF reports from the Management Portal for final review.

---
**Project Status**: Functional Prototype / Production Base
**Developer**: Antigravity AI Architecture Team
