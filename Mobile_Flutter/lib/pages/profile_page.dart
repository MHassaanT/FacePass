import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:provider/provider.dart';
import 'package:flutter_animate/flutter_animate.dart';
import '../services/supabase_service.dart';
import '../widgets/glass_card.dart';
import 'login_page.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key});

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<SupabaseService>().fetchStudentProfile();
    });
  }

  @override
  Widget build(BuildContext context) {
    final service = context.watch<SupabaseService>();
    final profile = service.studentProfile;

    return Scaffold(
      appBar: AppBar(
        title: Text('My Profile',
            style: GoogleFonts.outfit(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.transparent,
        elevation: 0,
        centerTitle: true,
      ),
      body: profile == null
          ? const Center(child: CircularProgressIndicator())
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Column(
                children: [
                  const CircleAvatar(
                    radius: 60,
                    backgroundColor: Color(0xFF00E676),
                    child: Icon(Icons.person_rounded,
                        size: 60, color: Colors.black87),
                  )
                      .animate()
                      .scale(duration: 400.ms, curve: Curves.easeOut)
                      .fadeIn(),
                  const SizedBox(height: 20),
                  Text(
                    profile['full_name'] ?? 'N/A',
                    style: GoogleFonts.outfit(
                        fontSize: 28, fontWeight: FontWeight.bold),
                  ).animate().fadeIn(delay: 200.ms).slideY(begin: 0.2),
                  const SizedBox(height: 4),
                  Text(
                    'Student ID: ${profile['student_id_number'] ?? profile['id']?.toString().substring(0, 8) ?? 'N/A'}',
                    style: const TextStyle(color: Colors.white54, fontSize: 14),
                  ).animate().fadeIn(delay: 300.ms),
                  const SizedBox(height: 40),
                  GlassCard(
                    padding: const EdgeInsets.all(20),
                    child: Column(
                      children: [
                        _ProfileTile(
                          icon: Icons.email_outlined,
                          label: 'Email Address',
                          value: profile['email'] ?? 'N/A',
                        ),
                        const Divider(color: Colors.white10, height: 24),
                        _ProfileTile(
                          icon: Icons.school_outlined,
                          label: 'Class / Section',
                          value: profile['class_name'] ?? 'N/A',
                        ),
                        const Divider(color: Colors.white10, height: 24),
                        _ProfileTile(
                          icon: Icons.library_books_outlined,
                          label: 'Enrolled Subjects',
                          value: profile['subjects'] ?? 'No subjects enrolled',
                        ),
                      ],
                    ),
                  ).animate().fadeIn(delay: 400.ms).slideY(begin: 0.1),
                  const SizedBox(height: 40),
                  ElevatedButton(
                    onPressed: () async {
                      await service.signOut();
                      if (mounted) {
                        Navigator.of(context).pushAndRemoveUntil(
                          MaterialPageRoute(builder: (_) => const LoginPage()),
                          (route) => false,
                        );
                      }
                    },
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.redAccent.withOpacity(0.1),
                      foregroundColor: Colors.redAccent,
                      minimumSize: const Size(double.infinity, 56),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16),
                        side: const BorderSide(
                            color: Colors.redAccent, width: 0.5),
                      ),
                      elevation: 0,
                    ),
                    child: Text(
                      'Logout',
                      style: GoogleFonts.outfit(
                          fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                  ).animate().fadeIn(delay: 600.ms),
                ],
              ),
            ),
    );
  }
}

class _ProfileTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _ProfileTile(
      {required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.all(10),
          decoration: BoxDecoration(
            color: const Color(0xFF00E676).withOpacity(0.1),
            shape: BoxShape.circle,
          ),
          child: Icon(icon, color: const Color(0xFF00E676), size: 20),
        ),
        const SizedBox(width: 16),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: const TextStyle(color: Colors.white54, fontSize: 12),
              ),
              const SizedBox(height: 4),
              Text(
                value,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w500,
                  fontSize: 15,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
