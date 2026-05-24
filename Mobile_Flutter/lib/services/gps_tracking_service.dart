import 'dart:async';
import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

class GpsTrackingService {
  static final GpsTrackingService _instance = GpsTrackingService._internal();
  factory GpsTrackingService() => _instance;
  GpsTrackingService._internal();

  Timer? _timer;
  DateTime? outOfRangeStartTime;
  bool isTracking = false;

  final ValueNotifier<bool> trackingActive = ValueNotifier(false);

  String? _studentId;
  List<String> _courseIds = [];

  static const _days = [
    'Monday',
    'Tuesday',
    'Wednesday',
    'Thursday',
    'Friday',
    'Saturday',
    'Sunday',
  ];

  void startTracking(String studentId, List<String> courseIds) {
    if (isTracking) return;
    _studentId = studentId;
    _courseIds = courseIds;
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
      final dayName = _days[now.weekday - 1];

      if (_courseIds.isEmpty) {
        outOfRangeStartTime = null;
        return;
      }

      final timetableResp = await Supabase.instance.client
          .from('timetable')
          .select('course_id, start_time, end_time')
          .inFilter('course_id', _courseIds)
          .eq('day_of_week', dayName);

      if (timetableResp.isEmpty) {
        outOfRangeStartTime = null;
        return;
      }

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

      if (activeClass == null) {
        outOfRangeStartTime = null;
        return;
      }

      final courseId = activeClass['course_id'];

      final classroomResp = await Supabase.instance.client
          .from('CLASSROOMS')
          .select('classroom_id, latitude, longitude')
          .limit(1)
          .maybeSingle();

      if (classroomResp == null) return;

      final classroomId = classroomResp['classroom_id'];
      final targetLat = (classroomResp['latitude'] as num).toDouble();
      final targetLng = (classroomResp['longitude'] as num).toDouble();

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission != LocationPermission.always &&
          permission != LocationPermission.whileInUse) {
        return;
      }

      final pos = await Geolocator.getCurrentPosition();

      final dist = Geolocator.distanceBetween(
          pos.latitude, pos.longitude, targetLat, targetLng);

      if (dist > 20) {
        outOfRangeStartTime ??= DateTime.now();

        final minutesOut =
            DateTime.now().difference(outOfRangeStartTime!).inMinutes;

        if (minutesOut >= 10) {
          await Supabase.instance.client.from('attendance_logs').insert({
            'student_id': _studentId,
            'course_id': courseId,
            'classroom_id': classroomId,
            'method_id': 4,
            'status_id': 4,
            'flagged_reason': 'Out of 20m range for 10+ minutes',
            'timestamp': DateTime.now().toUtc().toIso8601String(),
          });

          outOfRangeStartTime = null;
        }
      } else {
        outOfRangeStartTime = null;
      }
    } catch (e) {
      debugPrint('GPS Tracking error: $e');
    }
  }
}
