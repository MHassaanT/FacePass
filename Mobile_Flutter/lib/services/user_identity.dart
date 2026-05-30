import 'package:supabase_flutter/supabase_flutter.dart';

class AppUserIdentity {
  static SupabaseClient get _client => Supabase.instance.client;
  static String? _currentUserId;
  static String? _currentStudentId;
  static String? _currentEmail;
  static String? _currentFullName;
  static String? _currentRoleName;

  static void setSession({
    required String userId,
    String? studentId,
    String? email,
    String? fullName,
    String? roleName,
  }) {
    _currentUserId = userId;
    _currentStudentId = studentId;
    _currentEmail = email;
    _currentFullName = fullName;
    _currentRoleName = roleName;
  }

  static void clearSession() {
    _currentUserId = null;
    _currentStudentId = null;
    _currentEmail = null;
    _currentFullName = null;
    _currentRoleName = null;
  }

  static bool get hasSession => _currentUserId != null;
  static String? get currentEmail => _currentEmail;
  static String? get currentFullName => _currentFullName;
  static String? get currentRoleName => _currentRoleName;

  static Future<String?> resolveAppUserIdByEmail(String email) async {
    if (email.trim().isEmpty) return null;

    final userRow = await _client
        .from('USER')
        .select('user_id')
        .eq('email', email.trim())
        .maybeSingle();

    return userRow?['user_id']?.toString();
  }

  static Future<String?> resolveCurrentAppUserId() async {
    if (_currentUserId != null) return _currentUserId;
    if (_currentEmail == null) return null;
    return resolveAppUserIdByEmail(_currentEmail!);
  }

  static Future<String?> resolveCurrentStudentId() async {
    if (_currentStudentId != null) return _currentStudentId;

    final userId = await resolveCurrentAppUserId();
    if (userId == null) return null;

    final studentRow = await _client
        .from('STUDENTS')
        .select('student_id')
        .eq('student_id', userId)
        .maybeSingle();

    final resolvedStudentId = studentRow?['student_id']?.toString();
    if (resolvedStudentId != null) {
      _currentStudentId = resolvedStudentId;
    }
    return resolvedStudentId;
  }
}
