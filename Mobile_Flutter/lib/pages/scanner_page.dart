import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:geolocator/geolocator.dart';

import 'package:supabase_flutter/supabase_flutter.dart';

class ScannerPage extends StatefulWidget {
  const ScannerPage({super.key});

  @override
  State<ScannerPage> createState() => _ScannerPageState();
}

class _ScannerPageState extends State<ScannerPage> {
  bool _isProcessing = false;
  String _status = 'Align QR Code within the frame';
  Color _statusColor = Colors.white70;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: true,
      appBar: AppBar(
        backgroundColor: Colors.transparent,
        elevation: 0,
        leading: IconButton(
          icon:
              const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.white),
          onPressed: () => Navigator.pop(context),
        ),
      ),
      body: Stack(
        children: [
          MobileScanner(
            onDetect: (capture) {
              final List<Barcode> barcodes = capture.barcodes;
              if (barcodes.isNotEmpty && !_isProcessing) {
                _onQrDetected(barcodes.first.rawValue ?? '');
              }
            },
          ),
          _buildOverlay(),
          _buildStatusPanel(),
        ],
      ),
    );
  }

  Widget _buildOverlay() {
    return Stack(
      children: [
        ColorFiltered(
          colorFilter: ColorFilter.mode(
            Colors.black.withOpacity(0.7),
            BlendMode.srcOut,
          ),
          child: Stack(
            children: [
              Container(
                decoration: const BoxDecoration(
                  color: Colors.black,
                  backgroundBlendMode: BlendMode.dstOut,
                ),
              ),
              Align(
                alignment: Alignment.center,
                child: Container(
                  width: 250,
                  height: 250,
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(30),
                  ),
                ),
              ),
            ],
          ),
        ),
        Align(
          alignment: Alignment.center,
          child: Container(
            width: 260,
            height: 260,
            decoration: BoxDecoration(
              border:
                  Border.all(color: Theme.of(context).primaryColor, width: 2),
              borderRadius: BorderRadius.circular(35),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildStatusPanel() {
    return Align(
      alignment: Alignment.bottomCenter,
      child: Container(
        margin: const EdgeInsets.all(30),
        padding: const EdgeInsets.symmetric(vertical: 24, horizontal: 30),
        decoration: BoxDecoration(
          color: const Color(0xFF1A1A1A),
          borderRadius: BorderRadius.circular(20),
          boxShadow: [
            BoxShadow(color: Colors.black.withOpacity(0.5), blurRadius: 30),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (_isProcessing)
              const CircularProgressIndicator()
            else
              const Icon(Icons.location_on_rounded, color: Color(0xFF00E676)),
            const SizedBox(height: 16),
            Text(
              _status,
              textAlign: TextAlign.center,
              style: GoogleFonts.outfit(
                color: _statusColor,
                fontSize: 16,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _onQrDetected(String data) async {
    setState(() {
      _isProcessing = true;
      _status = 'Verifying location and session...';
      _statusColor = Colors.white;
    });

    try {
      final Map<String, dynamic> payload;
      try {
        payload = jsonDecode(data) as Map<String, dynamic>;
      } catch (_) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Invalid QR Code format.';
          _statusColor = Colors.redAccent;
        });
        Future.delayed(const Duration(seconds: 3), () {
          if (mounted) setState(() => _isProcessing = false);
        });
        return;
      }

      final sessionGuid = payload['session_guid'] as String?;
      final classroomIdStr = payload['classroom_id']?.toString();

      if (sessionGuid == null || classroomIdStr == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Invalid QR Code format.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      final classroomId = int.parse(classroomIdStr);

      setState(() => _status = 'Checking session...');
      final sessionResp = await Supabase.instance.client
          .from('qr_sessions')
          .select('session_id, expires_at, classroom_id')
          .eq('session_guid', sessionGuid)
          .eq('classroom_id', classroomId)
          .maybeSingle();

      if (sessionResp == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ QR code not found.\nAsk your teacher for a new code.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      final expiresAt = DateTime.parse(sessionResp['expires_at']).toUtc();
      if (DateTime.now().toUtc().isAfter(expiresAt)) {
        setState(() {
          _isProcessing = false;
          _status = '❌ QR code has expired.\nAsk your teacher for a new code.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      setState(() => _status = 'Checking your location...');
      final classroomResp = await Supabase.instance.client
          .from('CLASSROOMS')
          .select('classroom_id, latitude, longitude')
          .eq('classroom_id', classroomId)
          .maybeSingle();

      if (classroomResp == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Classroom not found.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      final targetLat = (classroomResp['latitude'] as num).toDouble();
      final targetLng = (classroomResp['longitude'] as num).toDouble();

      final bool inRange = await _checkGeofenceWithCoords(targetLat, targetLng);
      if (!inRange) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Out of Range\nYou must be in the classroom.';
          _statusColor = Colors.redAccent;
        });
        Future.delayed(const Duration(seconds: 3), () {
          if (mounted) setState(() => _isProcessing = false);
        });
        return;
      }

      setState(() => _status = 'Marking attendance...');
      final authUser = Supabase.instance.client.auth.currentUser!;

      final userRow = await Supabase.instance.client
          .from('USER')
          .select('user_id')
          .eq('email', authUser.email ?? '')
          .maybeSingle();

      String? studentId = userRow?['user_id']?.toString();

      if (studentId == null) {
        final studentResp = await Supabase.instance.client
            .from('STUDENTS')
            .select('student_id')
            .eq('student_id', authUser.id)
            .maybeSingle();
        studentId = studentResp?['student_id']?.toString();
      }

      if (studentId == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Student record not found.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      final enrollments = await Supabase.instance.client
          .from('COURSE_ENROLLMENTS')
          .select('course_id')
          .eq('student_id', studentId)
          .limit(1);

      final courseId = enrollments.isNotEmpty
          ? enrollments.first['course_id']
          : null;

      if (courseId == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ No course enrollment found.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      await Supabase.instance.client.from('attendance_logs').insert({
        'student_id': studentId,
        'course_id': courseId,
        'classroom_id': classroomId,
        'method_id': 2,
        'status_id': 1,
        'timestamp': DateTime.now().toUtc().toIso8601String(),
      });

      setState(() {
        _status = '✅ Attendance Marked!';
        _statusColor = const Color(0xFF00E676);
        _isProcessing = false;
      });

      await Future.delayed(const Duration(seconds: 2));
      if (mounted) Navigator.pop(context);
    } catch (e) {
      debugPrint('Error: $e');
      setState(() {
        _status = 'Error: $e';
        _statusColor = Colors.red;
        _isProcessing = false;
      });
      Future.delayed(const Duration(seconds: 3), () {
        if (mounted) setState(() => _isProcessing = false);
      });
    }
  }

  Future<bool> _checkGeofenceWithCoords(
      double targetLat, double targetLng) async {
    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.always ||
        permission == LocationPermission.whileInUse) {
      Position pos = await Geolocator.getCurrentPosition();
      double dist = Geolocator.distanceBetween(
          pos.latitude, pos.longitude, targetLat, targetLng);
      return dist <= 20;
    }
    return false;
  }
}
