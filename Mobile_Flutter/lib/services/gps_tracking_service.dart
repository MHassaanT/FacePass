import 'dart:async';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

class GpsTrackingService {
  // Singleton
  static final GpsTrackingService _instance = GpsTrackingService._internal();
  factory GpsTrackingService() => _instance;
  GpsTrackingService._internal();

  Timer? _timer;
  DateTime? outOfRangeStartTime;
  bool isTracking = false;

  // ValueNotifier so Dashboard can show the green dot
  final ValueNotifier<bool> trackingActive = ValueNotifier(false);

  String? _studentId;
  String? _classId;

  void startTracking(String studentId, String classId) {
    if (isTracking) return;
    _studentId = studentId;
    _classId = classId;
    isTracking = true;
    trackingActive.value = true;

    _timer = Timer.periodic(const Duration(seconds: 60), (timer) {
      _onTick();
    });
  }

  void stopTracking() {
    _timer?.cancel();
    _timer = null;
    isTracking = false;
    trackingActive.value = false;
    outOfRangeStartTime = null;
  }

  void dispose() {
    stopTracking();
    trackingActive.dispose();
  }

  Future<void> _onTick() async {
    try {
      final now = DateTime.now();
      final dayOfWeek = now.weekday; // 1=Mon, 7=Sun

      // Step a: Check if there's an active class right now
      final timetableResp = await Supabase.instance.client
          .from('timetable')
          .select('course_id, classroom_id, start_time, end_time')
          .eq('class_id', _classId!)
          .eq('day_of_week', dayOfWeek);

      if (timetableResp == null || (timetableResp as List).isEmpty) {
        outOfRangeStartTime = null;
        return;
      }

      // Filter active class client-side
      final timeNow = TimeOfDay.fromDateTime(now);
      Map<String, dynamic>? activeClass;

      for (final slot in timetableResp) {
        final startParts = (slot['start_time'] as String).split(':');
        final endParts = (slot['end_time'] as String).split(':');
        final start = TimeOfDay(
            hour: int.parse(startParts[0]), minute: int.parse(startParts[1]));
        final end = TimeOfDay(
            hour: int.parse(endParts[0]), minute: int.parse(endParts[1]));

        final nowMinutes = timeNow.hour * 60 + timeNow.minute;
        final startMinutes = start.hour * 60 + start.minute;
        final endMinutes = end.hour * 60 + end.minute;

        if (nowMinutes >= startMinutes && nowMinutes <= endMinutes) {
          activeClass = slot;
          break;
        }
      }

      // Step c: No active class
      if (activeClass == null) {
        outOfRangeStartTime = null;
        return;
      }

      // Step d: Get classroom GPS
      final classroomId = activeClass['classroom_id'];
      final courseId = activeClass['course_id'];

      final classroomResp = await Supabase.instance.client
          .from('classrooms')
          .select('latitude, longitude')
          .eq('id', classroomId)
          .maybeSingle();

      if (classroomResp == null) return;

      final targetLat = (classroomResp['latitude'] as num).toDouble();
      final targetLng = (classroomResp['longitude'] as num).toDouble();

      // Step e: Get current GPS
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission != LocationPermission.always &&
          permission != LocationPermission.whileInUse) return;

      final pos = await Geolocator.getCurrentPosition();

      // Step f: Calculate distance
      final dist = Geolocator.distanceBetween(
          pos.latitude, pos.longitude, targetLat, targetLng);

      // Step g: Check 200m range
      if (dist > 200) {
        outOfRangeStartTime ??= DateTime.now();

        final minutesOut =
            DateTime.now().difference(outOfRangeStartTime!).inMinutes;

        if (minutesOut >= 10) {
          // Get student record id
          final studentResp = await Supabase.instance.client
              .from('students')
              .select('id')
              .eq('user_id', _studentId!)
              .maybeSingle();

          if (studentResp == null) return;

          await Supabase.instance.client.from('attendance_logs').insert({
            'student_id': studentResp['id'],
            'course_id': courseId,
            'classroom_id': classroomId,
            'method': 'gps_auto',
            'status': 'absent',
            'flagged_reason': 'Out of 200m range for 10+ minutes',
            'timestamp': DateTime.now().toUtc().toIso8601String(),
          });

          // Reset so it doesn't log again for same session
          outOfRangeStartTime = null;
        }
      } else {
        // Step h: In range, reset timer
        outOfRangeStartTime = null;
      }
    } catch (e) {
      debugPrint('GPS Tracking error: $e');
    }
  }
}
