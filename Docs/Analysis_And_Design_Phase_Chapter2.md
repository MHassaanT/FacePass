# Analysis and Design Phase

## Chapter Two

### Tables After 3NF

1. `users (user_id PK, first_name, last_name, email, password_hash, role_id FK, created_at)`
2. `roles (role_id PK, role_name)`
3. `students (student_id PK/FK, enrollment_date, status)`
4. `teachers (teacher_id PK/FK, hire_date, department_id FK)`
5. `departments (department_id PK, department_name)`
6. `courses (course_id PK, course_name, course_code, department_id FK, teacher_id FK)`
7. `course_enrollments (enrollment_id PK, student_id FK, course_id FK)`
8. `buildings (building_id PK, building_name, location_coordinates)`
9. `classrooms (classroom_id PK, room_number, building_id FK, capacity)`
10. `face_encodings (encoding_id PK, student_id FK, vector_data_bytea, registered_at)`
11. `qr_sessions (session_id PK, classroom_id FK, session_token, expires_at)`
12. `attendance_methods (method_id PK, method_name)`
13. `attendance_statuses (status_id PK, status_name)`
14. `attendance_logs (log_id PK, student_id FK, classroom_id FK, method_id FK, status_id FK, timestamp, flagged_reason)`

### Forms & Form Layouts

1. **Login & Role Selection Form (Management Portal)**
   - **Purpose**: Authenticate users into the Management Portal based on RBAC.
   - **Layout**: Clean, centered login modal with a premium dark-mode aesthetic.
   - **Elements**: Email (TextBox), Password (PasswordBox), "Remember Me" (CheckBox), Login (Button).

2. **Student Biometric Registration Form (Teacher Dashboard)**
   - **Purpose**: Allow teachers to capture and encode student facial data.
   - **Layout**: Split-screen design. The left pane shows the live webcam feed with facial bounding boxes. The right pane contains data entry controls.
   - **Elements**: Select Student (Searchable ComboBox), Capture Face (Button), Status Indicator (Label/Progress Bar).

3. **Kiosk Attendance Form (Desktop Kiosk)**
   - **Purpose**: The primary classroom interface for students to mark attendance.
   - **Layout**: Immersive full-screen dark mode UI. The center contains the live camera feed. The bottom right corner displays the dynamic QR code overlay.
   - **Elements**: Live Camera Feed (Image Control), Dynamic QR Code (Image Control), Liveness Instructions (Label: "Smile / Blink").

4. **Mobile Dashboard & Scanner Form (MAUI App)**
   - **Purpose**: Allow students to scan the kiosk QR code within the geofence.
   - **Layout**: Standard mobile view with bottom tab navigation. The main scanner area shows a circular camera viewport.
   - **Elements**: Camera Viewport, GPS Location Status (Icon/Text: "Within Range" / "Out of Bounds"), Scan (Button).

5. **Attendance Dispute Submission Form (MAUI App)**
   - **Purpose**: Enable students to dispute logs flagged as suspicious (e.g., Impossible Travel).
   - **Layout**: A dedicated modal page triggered from the attendance history list.
   - **Elements**: Selected Log Details (Read-only Text), Dispute Reason (TextArea), Submit Dispute (Button).

### Reports & Report Layouts

1. **Daily Classroom Attendance Report**
   - **Purpose**: A summary of attendance for a specific course on a specific day.
   - **Layout**: Tabular PDF layout generated via iText7.
   - **Data Fields**: Date, Course Name, Teacher, Total Present, Total Absent. Main table includes Student ID, Name, Time Marked, and Method (Face/QR).

2. **Student Attendance History Report**
   - **Purpose**: A comprehensive view of a single student's attendance over a semester.
   - **Layout**: Summary header followed by a chronological data grid.
   - **Data Fields**: Student Info, Overall Attendance Percentage. Main table includes Date, Course, Status (Present/Absent/Suspicious), and Remarks.

3. **Suspicious Logs / Impossible Travel Audit Report**
   - **Purpose**: For administrators to review flagged activities indicating proxy attempts.
   - **Layout**: Alert-focused table highlighting anomalies in red.
   - **Data Fields**: Date/Time, Student Name, Classroom A, Classroom B, Time Difference, Flagged Reason (e.g., "Different building within 60s").

4. **Course Enrollment Roster Report**
   - **Purpose**: A manifest of all students enrolled in a specific course.
   - **Layout**: Grouped list format by Department and Course.
   - **Data Fields**: Course Code, Course Name, Total Enrolled. List of Student IDs and Names.

5. **System Audit & Role Assignment Report**
   - **Purpose**: To track administrative actions and user permission changes.
   - **Layout**: Administrative chronological log format.
   - **Data Fields**: Timestamp, Admin Name, Action Performed (e.g., "Created User", "Registered Biometrics"), Target User ID.
