import 'package:flutter/material.dart';

class ReportPage extends StatefulWidget {
  const ReportPage({super.key});

  @override
  State<ReportPage> createState() => _ReportPageState();
}

class _ReportPageState extends State<ReportPage> {
  // These will store the dates the student picks
  DateTime? fromDate;
  DateTime? toDate;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D0D0D), // Dark theme background
      appBar: AppBar(
        title: const Text('Export Report', style: TextStyle(color: Colors.black)),
        backgroundColor: const Color(0xFF00E676), // Your bright green color
        iconTheme: const IconThemeData(color: Colors.black),
        elevation: 0,
      ),
      body: Padding(
        padding: const EdgeInsets.all(20.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Select Date Range',
              style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 25),
            
            // "From" Date Selection Button
            _buildDateTile(
              label: 'From Date',
              date: fromDate,
              icon: Icons.calendar_today,
              onTap: () {
                // We will add the calendar picker logic later!
              },
            ),
            
            const SizedBox(height: 15),
            
            // "To" Date Selection Button
            _buildDateTile(
              label: 'To Date',
              date: toDate,
              icon: Icons.event,
              onTap: () {
                // We will add the calendar picker logic later!
              },
            ),
            
            const Spacer(),
            
            // The Big Green "Generate" Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () {
                  // This is where the PDF magic will happen!
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF00E676),
                  padding: const EdgeInsets.symmetric(vertical: 18),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                child: const Text(
                  'GENERATE PDF REPORT',
                  style: TextStyle(color: Colors.black, fontSize: 16, fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // This is a helper to make the "Date Buttons" look nice and clean
  Widget _buildDateTile({required String label, required DateTime? date, required IconData icon, required VoidCallback onTap}) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(15),
        decoration: BoxDecoration(
          color: Colors.grey[900],
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Colors.white10),
        ),
        child: Row(
          children: [
            Icon(icon, color: const Color(0xFF00E676), size: 20),
            const SizedBox(width: 15),
            Text(
              date == null ? label : "${date.day}/${date.month}/${date.year}",
              style: const TextStyle(color: Colors.white, fontSize: 16),
            ),
            const Spacer(),
            const Icon(Icons.arrow_drop_down, color: Colors.white54),
          ],
        ),
      ),
    );
  }
}