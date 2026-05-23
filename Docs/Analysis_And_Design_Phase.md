# Analysis and Design Phase

## Chapter One

### Business Requirements & Rules

1. **System Roles & Access**: The system must support three distinct user roles (Administrator, Teacher, and Student), enforcing Role-Based Access Control (RBAC) across all platforms.
2. **Biometric Enrollment**: Teachers must be able to securely register and bind student facial encodings to their respective student profiles.
3. **Dual Attendance Mechanisms**: Attendance can be marked in two ways: via facial recognition at a physical classroom kiosk, or by scanning a dynamic QR code using the student's mobile application.
4. **Anti-Spoofing (Liveness Detection)**: Biometric recognition must incorporate randomized liveness challenges (e.g., Smile, Blink, Head Turn) to prevent attendance proxying via photos or videos.
5. **QR Code Security**: To prevent token sharing, classroom QR codes must dynamically regenerate every 30 seconds, authenticated by a server-side session GUID.
6. **Spatial Geofencing**: Mobile QR code attendance must be strictly restricted to a 20-meter GPS radius around the designated classroom location using the Haversine formula.
7. **Impossible Travel Detection**: The database must enforce a trigger that flags an attendance log as "suspicious" if a student attempts to mark attendance in two different locations within a 60-second window.
8. **Dispute Resolution**: Students must have the ability to view their attendance history and submit justification/disputes for any attendance logs marked as suspicious.
9. **Audit & Reporting**: Administrators must have the capability to audit system activities, perform CRUD operations on users, and export comprehensive PDF attendance reports for review.

### List of Final Entities

**Regular Entities**
1. **User**: Stores core authentication credentials (BCrypt hashes) and personal information.
2. **Student**: Specialization of a User representing a student enrolled in the institution.
3. **Teacher**: Specialization of a User representing a faculty member.
4. **Department**: Represents an academic organizational unit within the institution.
5. **Course**: Represents an academic subject offered by a specific Department.
6. **Building**: Represents a physical structure on the campus.
7. **Classroom**: Represents a specific room located within a Building, equipped with a Kiosk.
8. **FaceEncoding**: Stores the serialized 128-dimensional biometric histogram (LBPH) for a given student.
9. **QRSession**: Tracks the short-lived (30s) dynamic QR tokens generated for a given Classroom.
10. **AttendanceLog**: The primary transactional entity recording an attendance event.

**Associative Entities**
11. **CourseEnrollment**: Resolves the many-to-many relationship mapping Students to the Courses they are taking.

**Lookup Entities**
12. **Role**: Defines system access levels (e.g., Admin, Teacher, Student).
13. **AttendanceMethod**: Specifies how the attendance was captured (e.g., Biometric, QR_Scan).
14. **AttendanceStatus**: Specifies the current state of the log (e.g., Verified, Suspicious, Disputed).

### List of Final Relationships

1. **User - Role (N:1)**: Many Users are assigned One Role.
2. **User Supertype (Is-A)**: A User can be a Student or a Teacher.
3. **Department - Course (1:N)**: One Department offers Many Courses.
4. **Building - Classroom (1:N)**: One Building contains Many Classrooms.
5. **Student - FaceEncoding (1:1)**: One Student has exactly One FaceEncoding.
6. **Student - Course (M:N)**: Many Students enroll in Many Courses (Resolved via **CourseEnrollment**).
7. **Teacher - Course (1:N)**: One Teacher can be assigned to teach Many Courses.
8. **Classroom - QRSession (1:N)**: One Classroom generates Many QRSessions over time.
9. **Student - AttendanceLog (1:N)**: One Student generates Many AttendanceLogs.
10. **Classroom - AttendanceLog (1:N)**: One Classroom acts as the location for Many AttendanceLogs.
11. **AttendanceMethod - AttendanceLog (1:N)**: One AttendanceMethod is utilized for Many AttendanceLogs.
12. **AttendanceStatus - AttendanceLog (1:N)**: One AttendanceStatus applies to Many AttendanceLogs.

---

## Chapter Two

### Tables After 3NF

*(Note: The exact schema may be updated with the user's specific SQL dump, but the following represents the current system in Third Normal Form based on the documented entities).*

1. `users (user_id PK, first_name, last_name, email, password_hash, role_id FK, created_at)`
2. `roles (role_id PK, role_name)`
3. `students (student_id PK/FK, enrollment_date, status)` *(student_id references users.user_id)*
4. `teachers (teacher_id PK/FK, hire_date, department_id FK)` *(teacher_id references users.user_id)*
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
