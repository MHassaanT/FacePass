# FacePass Mobile

Flutter client for the FacePass attendance system.

## Run

```bash
flutter pub get
flutter run
```

The app uses the bundled Supabase defaults in `lib/config/supabase_config.dart`.
You can override them at build time:

```bash
flutter run --dart-define=SUPABASE_URL=https://your-project.supabase.co \
  --dart-define=SUPABASE_ANON_KEY=your-anon-key
```

## Notes

- Student and teacher identity resolution now uses the app's `USER` table instead of guessing from auth IDs.
- Passwords are no longer trimmed before login.
- The QR scanner and dashboard now resolve the current student consistently through the shared identity helper.
- Mobile login now verifies the stored bcrypt hash directly from the `USER` table, so no custom login RPC is required.
