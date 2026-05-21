import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';
import '../models/subject_attendance.dart';

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

  Future<void> loadDashboardData(String studentId) async {
    _isLoading = true;
    notifyListeners();

    try {
      // Fetch enrollments
      final enrollments = await _supabase
          .from('enrollments')
          .select('course_id, courses(name)')
          .eq('student_id', studentId);

      // Fetch all attendance logs for this student
      final logs = await _supabase
          .from('attendance_logs')
          .select('course_id, status')
          .eq('student_id', studentId);
      
      _subjects = enrollments.map<SubjectAttendance>((e) {
        final courseId = e['course_id'];
        final courseName = e['courses']['name'] ?? 'Unknown';
        
        final courseLogs = logs.where((l) => l['course_id'] == courseId).toList();
        final total = courseLogs.length;
        final present = courseLogs.where((l) => l['status'] == 'present').length;
        final percentage = total > 0 ? (present / total * 100) : 0.0;

        return SubjectAttendance(
          courseName: courseName,
          present: present,
          total: total,
          percentage: percentage,
        );
      }).toList();

      // Calculate overall stats based on the average of all subjects
      double avgPercentage = 0;
      int totalPresent = 0;
      int totalSessions = 0;

      if (_subjects.isNotEmpty) {
        avgPercentage = _subjects.map((s) => s.percentage).reduce((a, b) => a + b) / _subjects.length;
        totalPresent = _subjects.map((s) => s.present).reduce((a, b) => a + b);
        totalSessions = _subjects.map((s) => s.total).reduce((a, b) => a + b);
      }
      
      _studentStats = {
        'attendance_percentage': avgPercentage,
        'total_sessions': totalSessions,
        'present_count': totalPresent,
      };

      // Fetch history (keep as is)
      _history = await _supabase
          .from('attendance_logs')
          .select('*, courses(name)')
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
      await _supabase.from('attendance_logs').insert({
        'student_id': studentId,
        'course_id': courseId,
        'classroom_id': classroomId,
        'method': method,
        'status': status,
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
        return null; // Success
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
    final user = _supabase.auth.currentUser;
    if (user == null) return;

    try {
      final profileData = await _supabase
          .from('students')
          .select('*')
          .eq('id', user.id)
          .single();

      final enrollments = await _supabase
          .from('enrollments')
          .select('courses(name)')
          .eq('student_id', user.id);

      final List<String> subjectNames = (enrollments as List)
          .map((e) => e['courses']['name'] as String)
          .toList();

      _studentProfile = {
        ...profileData,
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
