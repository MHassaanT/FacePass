# Shared Backend Deployment Guide: Supabase

## 1. SQL Schema & Database
1.  **PostGIS**: Enable the extension in the Supabase Dashboard -> Extensions or run:
    ```sql
    CREATE EXTENSION IF NOT EXISTS postgis;
    ```
2.  **Schema**: Run the full schema SQL (provided in initial specs) to create `users`, `students`, `attendance_logs`, `courses`, `classrooms`, and `qr_sessions`.
3.  **Triggers**: Run the script in `/media/hassaan/New Volume/FacePass/Database/Triggers/impossible_travel_trigger.sql` to enable automated security monitoring.

## 2. Storage Buckets
1.  Go to **Storage** in the Supabase Dashboard.
2.  Create a new bucket named **`avatars`**.
3.  Set the bucket to **Public** (or configure RLS for authenticated access).

## 3. Edge Functions (Deno)
1.  Install the **Supabase CLI** on your machine.
2.  Login: `supabase login`.
3.  Initialize: `supabase init` in the `/FacePass/Backend/` folder.
4.  Deploy the notification function:
    ```bash
    supabase functions deploy notify-attendance
    ```
5.  Set your FCM Secret:
    ```bash
    supabase secrets set FCM_SERVER_KEY=your_firebase_key
    ```

## 4. Database Webhooks
1.  Go to **Database -> Webhooks** in the Supabase Dashboard.
2.  Create a new webhook for the `attendance_logs` table.
3.  **Events**: `INSERT`.
4.  **Target**: `HTTP Request` to your `notify-attendance` edge function URL.

## 5. RLS (Row Level Security) - Recommended
For production, ensure `attendance_logs` has policies where:
- Students can only **SELECT** where `student_id = auth.uid()`.
- Teachers can **SELECT** and **INSERT** for their assigned courses.
- Admins have full access.
