import 'package:flutter/material.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

class SupabaseService with ChangeNotifier {
  final _supabase = Supabase.instance.client;

  bool _isLoading = false;
  bool get isLoading => _isLoading;

  Map<String, dynamic>? _studentStats;
  Map<String, dynamic>? get studentStats => _studentStats;

  List<dynamic> _history = [];
  List<dynamic> get history => _history;

  Future<void> loadDashboardData(String studentId) async {
    _isLoading = true;
    notifyListeners();

    try {
      // Fetch stats (mocked logic based on MAUI version)
      // In real scenario, this would be a RPC or multiple queries
      final response = await _supabase
          .from('attendance_logs')
          .select('status, classrooms(name)')
          .eq('student_id', studentId);
      
      // Calculate stats locally for demo
      int total = response.length;
      int present = response.where((e) => e['status'] == 'present').length;
      
      _studentStats = {
        'attendance_percentage': total > 0 ? (present / total * 100) : 0.0,
        'total_sessions': total,
        'present_count': present,
      };

      // Fetch history
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
}
