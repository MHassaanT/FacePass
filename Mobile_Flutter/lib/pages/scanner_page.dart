import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:geolocator/geolocator.dart';
import 'package:provider/provider.dart';
import '../services/supabase_service.dart';

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
          icon: const Icon(Icons.arrow_back_ios_new_rounded, color: Colors.white),
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
        // Semi-transparent background with a hole
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
        // Corner markers
        Align(
          alignment: Alignment.center,
          child: Container(
            width: 260,
            height: 260,
            decoration: BoxDecoration(
              border: Border.all(color: Theme.of(context).primaryColor, width: 2),
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
      // 1. Check Geofencing (20m radius)
      bool inRange = await _checkGeofence();
      if (!inRange) {
        setState(() {
          _isProcessing = false;
          _status = '❌ Out of Range\nYou must be in the classroom.';
          _statusColor = Colors.redAccent;
        });
        return;
      }

      // 2. Process QR Data (Mocked parse)
      // data format: session_id|course_id|classroom_id
      final parts = data.split('|');
      if (parts.length < 3) {
        throw 'Invalid QR Code';
      }

      // 3. Mark Attendance in Supabase
      final success = await context.read<SupabaseService>().markAttendance(
            studentId: '00000000-0000-0000-0000-000000000001',
            courseId: parts[1],
            classroomId: parts[2],
            method: 'qr',
          );

      if (success) {
        setState(() {
          _status = '✅ Attendance Marked!';
          _statusColor = const Color(0xFF00E676);
        });
        await Future.delayed(const Duration(seconds: 2));
        if (mounted) Navigator.pop(context);
      } else {
        throw 'Database update failed';
      }
    } catch (e) {
      setState(() {
        _isProcessing = false;
        _status = '❌ Error: $e';
        _statusColor = Colors.redAccent;
      });
    }
  }

  Future<bool> _checkGeofence() async {
    // Mocked target classroom coordinates
    const double targetLat = 0.0;
    const double targetLng = 0.0;

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.always || permission == LocationPermission.whileInUse) {
      Position pos = await Geolocator.getCurrentPosition();
      double dist = Geolocator.distanceBetween(pos.latitude, pos.longitude, targetLat, targetLng);
      return dist <= 20; // 20 meters
    }
    return false;
  }
}
