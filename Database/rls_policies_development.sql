-- =============================================================================
-- FacePass — Development RLS policies (anon key / desktop & mobile clients)
-- =============================================================================
-- Run in Supabase SQL Editor AFTER schema migration and seed_lookup_tables.sql
--
-- WARNING: These policies allow the `anon` role broad read/write access.
-- They exist because Management Portal uses custom BCrypt login against "USER"
-- (not Supabase Auth JWTs). Replace with role-based policies before production.
-- =============================================================================

-- Optional: ensure API roles can use the public schema
GRANT USAGE ON SCHEMA public TO anon, authenticated;
GRANT ALL ON ALL TABLES IN SCHEMA public TO anon, authenticated;
GRANT ALL ON ALL SEQUENCES IN SCHEMA public TO anon, authenticated;

-- Helper macro pattern: one policy per operation per table
-- Drop existing dev policies if re-running (safe)

-- ---------- "ROLE" (lookup) ----------
ALTER TABLE "ROLE" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_role ON "ROLE";
DROP POLICY IF EXISTS dev_anon_insert_role ON "ROLE";
DROP POLICY IF EXISTS dev_anon_update_role ON "ROLE";
DROP POLICY IF EXISTS dev_anon_delete_role ON "ROLE";
CREATE POLICY dev_anon_select_role ON "ROLE" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_role ON "ROLE" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_role ON "ROLE" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_role ON "ROLE" FOR DELETE TO anon USING (true);

-- ---------- "USER" ----------
ALTER TABLE "USER" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_user ON "USER";
DROP POLICY IF EXISTS dev_anon_insert_user ON "USER";
DROP POLICY IF EXISTS dev_anon_update_user ON "USER";
DROP POLICY IF EXISTS dev_anon_delete_user ON "USER";
CREATE POLICY dev_anon_select_user ON "USER" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_user ON "USER" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_user ON "USER" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_user ON "USER" FOR DELETE TO anon USING (true);

-- ---------- "DEPARTMENT" ----------
ALTER TABLE "DEPARTMENT" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_department ON "DEPARTMENT";
DROP POLICY IF EXISTS dev_anon_insert_department ON "DEPARTMENT";
DROP POLICY IF EXISTS dev_anon_update_department ON "DEPARTMENT";
DROP POLICY IF EXISTS dev_anon_delete_department ON "DEPARTMENT";
CREATE POLICY dev_anon_select_department ON "DEPARTMENT" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_department ON "DEPARTMENT" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_department ON "DEPARTMENT" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_department ON "DEPARTMENT" FOR DELETE TO anon USING (true);

-- ---------- "TEACHERS" ----------
ALTER TABLE "TEACHERS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_teachers ON "TEACHERS";
DROP POLICY IF EXISTS dev_anon_insert_teachers ON "TEACHERS";
DROP POLICY IF EXISTS dev_anon_update_teachers ON "TEACHERS";
DROP POLICY IF EXISTS dev_anon_delete_teachers ON "TEACHERS";
CREATE POLICY dev_anon_select_teachers ON "TEACHERS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_teachers ON "TEACHERS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_teachers ON "TEACHERS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_teachers ON "TEACHERS" FOR DELETE TO anon USING (true);

-- ---------- "STUDENTS" ----------
ALTER TABLE "STUDENTS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_students ON "STUDENTS";
DROP POLICY IF EXISTS dev_anon_insert_students ON "STUDENTS";
DROP POLICY IF EXISTS dev_anon_update_students ON "STUDENTS";
DROP POLICY IF EXISTS dev_anon_delete_students ON "STUDENTS";
CREATE POLICY dev_anon_select_students ON "STUDENTS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_students ON "STUDENTS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_students ON "STUDENTS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_students ON "STUDENTS" FOR DELETE TO anon USING (true);

-- ---------- "COURSES" ----------
ALTER TABLE "COURSES" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_courses ON "COURSES";
DROP POLICY IF EXISTS dev_anon_insert_courses ON "COURSES";
DROP POLICY IF EXISTS dev_anon_update_courses ON "COURSES";
DROP POLICY IF EXISTS dev_anon_delete_courses ON "COURSES";
CREATE POLICY dev_anon_select_courses ON "COURSES" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_courses ON "COURSES" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_courses ON "COURSES" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_courses ON "COURSES" FOR DELETE TO anon USING (true);

-- ---------- "COURSE_ENROLLMENTS" ----------
ALTER TABLE "COURSE_ENROLLMENTS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_enrollments ON "COURSE_ENROLLMENTS";
DROP POLICY IF EXISTS dev_anon_insert_enrollments ON "COURSE_ENROLLMENTS";
DROP POLICY IF EXISTS dev_anon_update_enrollments ON "COURSE_ENROLLMENTS";
DROP POLICY IF EXISTS dev_anon_delete_enrollments ON "COURSE_ENROLLMENTS";
CREATE POLICY dev_anon_select_enrollments ON "COURSE_ENROLLMENTS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_enrollments ON "COURSE_ENROLLMENTS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_enrollments ON "COURSE_ENROLLMENTS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_enrollments ON "COURSE_ENROLLMENTS" FOR DELETE TO anon USING (true);

-- ---------- "BUILDINGS" ----------
ALTER TABLE "BUILDINGS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_buildings ON "BUILDINGS";
DROP POLICY IF EXISTS dev_anon_insert_buildings ON "BUILDINGS";
DROP POLICY IF EXISTS dev_anon_update_buildings ON "BUILDINGS";
DROP POLICY IF EXISTS dev_anon_delete_buildings ON "BUILDINGS";
CREATE POLICY dev_anon_select_buildings ON "BUILDINGS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_buildings ON "BUILDINGS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_buildings ON "BUILDINGS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_buildings ON "BUILDINGS" FOR DELETE TO anon USING (true);

-- ---------- "CLASSROOMS" ----------
ALTER TABLE "CLASSROOMS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_classrooms ON "CLASSROOMS";
DROP POLICY IF EXISTS dev_anon_insert_classrooms ON "CLASSROOMS";
DROP POLICY IF EXISTS dev_anon_update_classrooms ON "CLASSROOMS";
DROP POLICY IF EXISTS dev_anon_delete_classrooms ON "CLASSROOMS";
CREATE POLICY dev_anon_select_classrooms ON "CLASSROOMS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_classrooms ON "CLASSROOMS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_classrooms ON "CLASSROOMS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_classrooms ON "CLASSROOMS" FOR DELETE TO anon USING (true);

-- ---------- "FACE_ENCODINGS" ----------
ALTER TABLE "FACE_ENCODINGS" ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_face ON "FACE_ENCODINGS";
DROP POLICY IF EXISTS dev_anon_insert_face ON "FACE_ENCODINGS";
DROP POLICY IF EXISTS dev_anon_update_face ON "FACE_ENCODINGS";
DROP POLICY IF EXISTS dev_anon_delete_face ON "FACE_ENCODINGS";
CREATE POLICY dev_anon_select_face ON "FACE_ENCODINGS" FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_face ON "FACE_ENCODINGS" FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_face ON "FACE_ENCODINGS" FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_face ON "FACE_ENCODINGS" FOR DELETE TO anon USING (true);

-- ---------- attendance_methods (lookup — read for all, write for admin seeding) ----------
ALTER TABLE attendance_methods ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_methods ON attendance_methods;
DROP POLICY IF EXISTS dev_anon_insert_methods ON attendance_methods;
CREATE POLICY dev_anon_select_methods ON attendance_methods FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_methods ON attendance_methods FOR INSERT TO anon WITH CHECK (true);

-- ---------- attendance_statuses (lookup) ----------
ALTER TABLE attendance_statuses ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_statuses ON attendance_statuses;
DROP POLICY IF EXISTS dev_anon_insert_statuses ON attendance_statuses;
CREATE POLICY dev_anon_select_statuses ON attendance_statuses FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_statuses ON attendance_statuses FOR INSERT TO anon WITH CHECK (true);

-- ---------- attendance_logs ----------
ALTER TABLE attendance_logs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_logs ON attendance_logs;
DROP POLICY IF EXISTS dev_anon_insert_logs ON attendance_logs;
DROP POLICY IF EXISTS dev_anon_update_logs ON attendance_logs;
DROP POLICY IF EXISTS dev_anon_delete_logs ON attendance_logs;
CREATE POLICY dev_anon_select_logs ON attendance_logs FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_logs ON attendance_logs FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_logs ON attendance_logs FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_logs ON attendance_logs FOR DELETE TO anon USING (true);

-- ---------- qr_sessions ----------
ALTER TABLE qr_sessions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_qr ON qr_sessions;
DROP POLICY IF EXISTS dev_anon_insert_qr ON qr_sessions;
DROP POLICY IF EXISTS dev_anon_update_qr ON qr_sessions;
DROP POLICY IF EXISTS dev_anon_delete_qr ON qr_sessions;
CREATE POLICY dev_anon_select_qr ON qr_sessions FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_qr ON qr_sessions FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_qr ON qr_sessions FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_qr ON qr_sessions FOR DELETE TO anon USING (true);

-- ---------- timetable ----------
ALTER TABLE timetable ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_timetable ON timetable;
DROP POLICY IF EXISTS dev_anon_insert_timetable ON timetable;
DROP POLICY IF EXISTS dev_anon_update_timetable ON timetable;
DROP POLICY IF EXISTS dev_anon_delete_timetable ON timetable;
CREATE POLICY dev_anon_select_timetable ON timetable FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_timetable ON timetable FOR INSERT TO anon WITH CHECK (true);
CREATE POLICY dev_anon_update_timetable ON timetable FOR UPDATE TO anon USING (true) WITH CHECK (true);
CREATE POLICY dev_anon_delete_timetable ON timetable FOR DELETE TO anon USING (true);

-- ---------- audit_logs ----------
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS dev_anon_select_audit ON audit_logs;
DROP POLICY IF EXISTS dev_anon_insert_audit ON audit_logs;
CREATE POLICY dev_anon_select_audit ON audit_logs FOR SELECT TO anon USING (true);
CREATE POLICY dev_anon_insert_audit ON audit_logs FOR INSERT TO anon WITH CHECK (true);
