-- Run once in Supabase SQL Editor if lookup tables are empty.
-- Then run rls_policies_development.sql
-- Safe to re-run: uses ON CONFLICT where possible.

INSERT INTO "DEPARTMENT" (department_id, department_name) VALUES
  (1, 'Computer Science')
ON CONFLICT (department_id) DO UPDATE SET department_name = EXCLUDED.department_name;

SELECT setval(pg_get_serial_sequence('"DEPARTMENT"', 'department_id'),
  (SELECT COALESCE(MAX(department_id), 1) FROM "DEPARTMENT"));

INSERT INTO "ROLE" (role_id, role_name) VALUES
  (1, 'admin'),
  (2, 'teacher'),
  (3, 'student')
ON CONFLICT (role_id) DO UPDATE SET role_name = EXCLUDED.role_name;

-- Reset identity sequence after explicit IDs (PostgreSQL)
SELECT setval(pg_get_serial_sequence('"ROLE"', 'role_id'), (SELECT COALESCE(MAX(role_id), 1) FROM "ROLE"));

INSERT INTO attendance_methods (method_id, method_name) VALUES
  (1, 'face'),
  (2, 'qr'),
  (3, 'manual'),
  (4, 'gps_auto')
ON CONFLICT (method_id) DO UPDATE SET method_name = EXCLUDED.method_name;

INSERT INTO attendance_statuses (status_id, status_name) VALUES
  (1, 'present'),
  (2, 'suspicious'),
  (3, 'manual_override'),
  (4, 'absent')
ON CONFLICT (status_id) DO UPDATE SET status_name = EXCLUDED.status_name;
