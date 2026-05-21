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
      // STEP A: Parse QR string
      final parts = data.split('|');
      if (parts.length != 3) {
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

      final sessionGuid = parts[0];
      final courseId = parts[1];
      final classroomId = parts[2];

      // STEP B: Validate session from Supabase
      setState(() => _status = 'Checking session...');
      final sessionResp = await Supabase.instance.client
          .from('qr_sessions')
          .select('id, expires_at')
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

      // STEP C: Get classroom GPS from Supabase
      setState(() => _status = 'Checking your location...');
      final classroomResp = await Supabase.instance.client
          .from('classrooms')
          .select('id, name, latitude, longitude')
          .eq('id', classroomId)
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

      // STEP D: Check geofence
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

      // STEP E: Mark attendance
      setState(() => _status = 'Marking attendance...');
      final userId = Supabase.instance.client.auth.currentUser!.id;

      final studentResp = await Supabase.instance.client
          .from('students')
          .select('id')
          .eq('user_id', userId)
          .maybeSingle();

      if (studentResp == null) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Student record not found.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      await Supabase.instance.client.from('attendance_logs').insert({
        'student_id': studentResp['id'],
        'course_id': courseId,
        'classroom_id': classroomId,
        'method': 'qr',
        'status': 'present',
        'timestamp': DateTime.now().toUtc().toIso8601String(),
      });

      // STEP F: Success
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

  Future<bool> _checkGeofence() async {
    const double targetLat = 0.0;
    const double targetLng = 0.0;
    return _checkGeofenceWithCoords(targetLat, targetLng);
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
      return dist <= 200; // 200 meters
    }
    return false;
  }
}
