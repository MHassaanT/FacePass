import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../models/subject_attendance.dart';
import '../utils/json_embed.dart';

class SupabaseService with ChangeNotifier {
  final _supabase = Supabase.instance.client;

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  Map<String, dynamic>? _studentStats;
  Map<String, dynamic>? get studentStats => _studentStats;

  List<dynamic> _history = [];
  List<dynamic> get history => _history;

  List<SubjectAttendance> _subjects = [];
  List<SubjectAttendance> get subjects => _subjects;

  Map<String, dynamic>? _studentProfile;
  Map<String, dynamic>? get studentProfile => _studentProfile;

  static const _methodMap = {'face': 1, 'qr': 2, 'manual': 3, 'gps_auto': 4};
  static const _statusMap = {
    'present': 1,
    'suspicious': 2,
    'manual_override': 3,
    'absent': 4,
  };

  Future<String?> _resolveStudentId() async {
    final user = _supabase.auth.currentUser;
    if (user == null) return null;

    final userRow = await _supabase
        .from('USER')
        .select('user_id')
        .eq('email', user.email ?? '')
        .maybeSingle();

    if (userRow != null) {
      return userRow['user_id'].toString();
    }

    final studentRow = await _supabase
        .from('STUDENTS')
        .select('student_id')
        .eq('student_id', user.id)
        .maybeSingle();

    return studentRow?['student_id']?.toString();
  }

  Future<void> loadDashboardData(String studentId) async {
    _isLoading = true;
    notifyListeners();

    try {
      final enrollments = await _supabase
          .from('COURSE_ENROLLMENTS')
          .select('course_id, COURSES(course_name)')
          .eq('student_id', studentId);

      final logs = await _supabase
          .from('attendance_logs')
          .select('course_id, status_id')
          .eq('student_id', studentId);

      _subjects = enrollments.map<SubjectAttendance>((e) {
        final courseId = e['course_id'];
        final courseName =
            JsonEmbed.field(e, 'COURSES', 'course_name').isEmpty
                ? 'Unknown'
                : JsonEmbed.field(e, 'COURSES', 'course_name');

        final courseLogs =
            logs.where((l) => l['course_id'] == courseId).toList();
        final total = courseLogs.length;
        final present =
            courseLogs.where((l) => l['status_id'] == 1).length;
        final percentage = total > 0 ? (present / total * 100) : 0.0;

        return SubjectAttendance(
          courseName: courseName,
          present: present,
          total: total,
          percentage: percentage,
        );
      }).toList();

      double avgPercentage = 0;
      int totalPresent = 0;
      int totalSessions = 0;

      if (_subjects.isNotEmpty) {
        avgPercentage =
            _subjects.map((s) => s.percentage).reduce((a, b) => a + b) /
                _subjects.length;
        totalPresent =
            _subjects.map((s) => s.present).reduce((a, b) => a + b);
        totalSessions =
            _subjects.map((s) => s.total).reduce((a, b) => a + b);
      }

      _studentStats = {
        'attendance_percentage': avgPercentage,
        'total_sessions': totalSessions,
        'present_count': totalPresent,
      };

      _history = await _supabase
          .from('attendance_logs')
          .select('*, COURSES(course_name)')
          .eq('student_id', studentId)
          .order('timestamp', ascending: false)
          .limit(10);
    } catch (e) {
      debugPrint('[Supabase] Error: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> markAttendance({
    required String studentId,
    required String courseId,
    required String classroomId,
    required String method,
    String status = 'present',
    String? flaggedReason,
  }) async {
    try {
      final methodId = _methodMap[method] ?? 1;
      final statusId = _statusMap[status] ?? 1;

      await _supabase.from('attendance_logs').insert({
        'student_id': studentId,
        'course_id': courseId,
        'classroom_id': classroomId,
        'method_id': methodId,
        'status_id': statusId,
        'flagged_reason': flaggedReason,
        'timestamp': DateTime.now().toIso8601String(),
      });
      return true;
    } catch (e) {
      debugPrint('[Attendance] Error: $e');
      return false;
    }
  }

  Future<String?> signIn(String email, String password) async {
    _isLoading = true;
    notifyListeners();

    try {
      final response = await _supabase.auth.signInWithPassword(
        email: email,
        password: password,
      );

      if (response.user != null) {
        return null;
      }
      return 'Login failed';
    } on AuthException catch (e) {
      return e.message;
    } catch (e) {
      return 'An unexpected error occurred';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> fetchStudentProfile() async {
    final studentId = await _resolveStudentId();
    if (studentId == null) return;

    try {
      final profileData = await _supabase
          .from('STUDENTS')
          .select(
              '*, USER(first_name, last_name, email), COURSE_ENROLLMENTS(COURSES(course_name))')
          .eq('student_id', studentId)
          .single();

      final enrollments = profileData['COURSE_ENROLLMENTS'] as List? ?? [];
      final subjectNames = enrollments
          .map((e) => JsonEmbed.field(
              Map<String, dynamic>.from(e as Map), 'COURSES', 'course_name'))
          .where((n) => n.isNotEmpty)
          .toList();

      final user = profileData['USER'] as Map<String, dynamic>?;
      final first = user?['first_name']?.toString() ?? '';
      final last = user?['last_name']?.toString() ?? '';

      _studentProfile = {
        ...profileData,
        'full_name': '$first $last'.trim(),
        'email': user?['email'],
        'student_id_number': profileData['student_id']?.toString(),
        'subjects': subjectNames.join(', '),
      };
      notifyListeners();
    } catch (e) {
      debugPrint('[Profile] Error: $e');
    }
  }

  Future<void> signOut() async {
    await _supabase.auth.signOut();
    _studentProfile = null;
    _studentStats = null;
    _subjects = [];
    _history = [];
    notifyListeners();
  }
}
