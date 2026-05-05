import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import '../widgets/glass_card.dart';

class ProfilePage extends StatelessWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('My Profile', style: GoogleFonts.outfit(fontWeight: FontWeight.bold)),
        backgroundColor: Colors.transparent,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            const CircleAvatar(
              radius: 50,
              backgroundColor: Color(0xFF1A1A1A),
              child: Icon(Icons.person_rounded, size: 50, color: Color(0xFF00E676)),
            ),
            const SizedBox(height: 20),
            Text(
              'Muhammad Hassaan',
              style: GoogleFonts.outfit(fontSize: 24, fontWeight: FontWeight.bold),
            ),
            const Text('ID: FP-2026-0042', style: TextStyle(color: Colors.white54)),
            const SizedBox(height: 30),
            const GlassCard(
              child: Column(
                children: [
                  _ProfileTile(icon: Icons.email_outlined, label: 'Email', value: 'hassaan@university.edu'),
                  Divider(color: Colors.white10),
                  _ProfileTile(icon: Icons.phone_outlined, label: 'Phone', value: '+92 300 1234567'),
                  Divider(color: Colors.white10),
                  _ProfileTile(icon: Icons.school_outlined, label: 'Department', value: 'Software Engineering'),
                ],
              ),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: () {},
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.redAccent.withOpacity(0.1),
                foregroundColor: Colors.redAccent,
                minimumSize: const Size(double.infinity, 50),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              ),
              child: const Text('Logout'),
            ),
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

  const _ProfileTile({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 8),
      child: Row(
        children: [
          Icon(icon, color: const Color(0xFF00E676), size: 20),
          const SizedBox(width: 16),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: const TextStyle(color: Colors.white54, fontSize: 12)),
              Text(value, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w500)),
            ],
          ),
        ],
      ),
    );
  }
}
