import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import '../services/supabase_service.dart';
import 'scanner_page.dart';

class DashboardPage extends StatefulWidget {
  const DashboardPage({super.key});

  @override
  State<DashboardPage> createState() => _DashboardPageState();
}

class _DashboardPageState extends State<DashboardPage> {
  @override
  void initState() {
    super.initState();
    // Load data on start
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SupabaseService>().loadDashboardData('00000000-0000-0000-0000-000000000001');
    });
  }

  @override
  Widget build(BuildContext context) {
    final service = context.watch<SupabaseService>();
    final stats = service.studentStats;

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          _buildAppBar(),
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(20.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildStatsCard(stats),
                  const SizedBox(height: 30),
                  Text(
                    'Recent Activity',
                    style: GoogleFonts.outfit(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  const SizedBox(height: 15),
                  _buildHistoryList(service.history),
                ],
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => const ScannerPage()),
        ),
        icon: const Icon(Icons.qr_code_scanner_rounded),
        label: const Text('Scan QR'),
        backgroundColor: Theme.of(context).primaryColor,
        foregroundColor: Colors.black,
      ).animate().scale(delay: 500.ms),
    );
  }

  Widget _buildAppBar() {
    return SliverAppBar(
      expandedHeight: 120.0,
      floating: false,
      pinned: true,
      backgroundColor: const Color(0xFF0D0D0D),
      flexibleSpace: FlexibleSpaceBar(
        title: Text(
          'FacePass',
          style: GoogleFonts.outfit(fontWeight: FontWeight.bold),
        ),
        centerTitle: false,
        titlePadding: const EdgeInsets.only(left: 20, bottom: 16),
      ),
      actions: [
        IconButton(
          onPressed: () {},
          icon: const Icon(Icons.person_outline_rounded),
        ),
        const SizedBox(width: 10),
      ],
    );
  }

  Widget _buildStatsCard(Map<String, dynamic>? stats) {
    double percentage = (stats?['attendance_percentage'] ?? 0.0).toDouble();

    return Container(
      padding: const EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [
            const Color(0xFF1565C0).withOpacity(0.8),
            const Color(0xFF0D47A1).withOpacity(0.9),
          ],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1565C0).withOpacity(0.3),
            blurRadius: 20,
            offset: const Offset(0, 10),
          ),
        ],
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Overall Attendance',
                    style: GoogleFonts.outfit(
                      color: Colors.white70,
                      fontSize: 16,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    '${percentage.toStringAsFixed(0)}%',
                    style: GoogleFonts.outfit(
                      color: Colors.white,
                      fontSize: 42,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
              Stack(
                alignment: Alignment.center,
                children: [
                  SizedBox(
                    width: 80,
                    height: 80,
                    child: CircularProgressIndicator(
                      value: percentage / 100,
                      strokeWidth: 8,
                      backgroundColor: Colors.white12,
                      valueColor: const AlwaysStoppedAnimation(Colors.white),
                    ),
                  ),
                  const Icon(Icons.check_circle_outline, color: Colors.white, size: 30),
                ],
              ),
            ],
          ),
          const SizedBox(height: 24),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceAround,
            children: [
              _buildSmallStat('Sessions', stats?['total_sessions']?.toString() ?? '0'),
              Container(width: 1, height: 30, color: Colors.white24),
              _buildSmallStat('Attended', stats?['present_count']?.toString() ?? '0'),
              Container(width: 1, height: 30, color: Colors.white24),
              _buildSmallStat('Rank', '#12'),
            ],
          ),
        ],
      ),
    ).animate().fadeIn(duration: 600.ms).slideY(begin: 0.2);
  }

  Widget _buildSmallStat(String label, String value) {
    return Column(
      children: [
        Text(
          value,
          style: GoogleFonts.outfit(
            color: Colors.white,
            fontSize: 18,
            fontWeight: FontWeight.bold,
          ),
        ),
        Text(
          label,
          style: GoogleFonts.outfit(
            color: Colors.white60,
            fontSize: 12,
          ),
        ),
      ],
    );
  }

  Widget _buildHistoryList(List<dynamic> history) {
    if (history.isEmpty) {
      return const Center(
        child: Padding(
          padding: EdgeInsets.only(top: 40),
          child: Text('No recent activity', style: TextStyle(color: Colors.white38)),
        ),
      );
    }

    return ListView.separated(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      itemCount: history.length,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (context, index) {
        final item = history[index];
        final status = item['status']?.toString().toLowerCase() ?? 'unknown';
        
        return Container(
          padding: const EdgeInsets.all(16),
          decoration: BoxDecoration(
            color: const Color(0xFF1A1A1A),
            borderRadius: BorderRadius.circular(16),
            border: Border.all(color: Colors.white.withOpacity(0.05)),
          ),
          child: Row(
            children: [
              Container(
                padding: const EdgeInsets.all(10),
                decoration: BoxDecoration(
                  color: _getStatusColor(status).withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(_getStatusIcon(status), color: _getStatusColor(status), size: 20),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      item['courses']?['name'] ?? 'Unknown Course',
                      style: GoogleFonts.outfit(fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                    Text(
                      item['timestamp']?.toString().substring(0, 16) ?? '',
                      style: const TextStyle(color: Colors.white38, fontSize: 12),
                    ),
                  ],
                ),
              ),
              Text(
                status.toUpperCase(),
                style: GoogleFonts.outfit(
                  color: _getStatusColor(status),
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1,
                ),
              ),
            ],
          ),
        ).animate().fadeIn(delay: (index * 100).ms).slideX(begin: 0.1);
      },
    );
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'present': return const Color(0xFF00E676);
      case 'suspicious': return const Color(0xFFFF5252);
      case 'manual_override': return const Color(0xFF448AFF);
      default: return Colors.grey;
    }
  }

  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'present': return Icons.check_rounded;
      case 'suspicious': return Icons.warning_amber_rounded;
      case 'manual_override': return Icons.edit_note_rounded;
      default: return Icons.help_outline_rounded;
    }
  }
}
