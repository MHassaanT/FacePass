import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/supabase_service.dart';
import 'dashboard_page.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;

  @override
  void dispose() {
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    final email = _emailController.text.trim();
    final password = _passwordController.text;

    if (email.isEmpty || password.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter email and password')),
      );
      return;
    }

    final service = context.read<SupabaseService>();
    final error = await service.signIn(email, password);

    if (mounted) {
      if (error == null) {
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => const DashboardPage()),
        );
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(error), backgroundColor: Colors.redAccent),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final isLoading = context.watch<SupabaseService>().isLoading;

    return Scaffold(
      backgroundColor: const Color(0xFF0D0D0D),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const SizedBox(height: 80),
              // Logo/Title
              Text(
                'FacePass',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 48,
                  fontWeight: FontWeight.bold,
                  color: Color(0xFF00E676),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Secure Biometric Attendance',
                textAlign: TextAlign.center,
                style: const TextStyle(
                  fontSize: 16,
                  color: Colors.white54,
                ),
              ),
              const SizedBox(height: 60),
              // Email Field
              _buildTextField(
                controller: _emailController,
                label: 'Email Address',
                hint: 'name@example.com',
                icon: Icons.email_outlined,
              ),
              const SizedBox(height: 20),
              // Password Field
              _buildTextField(
                controller: _passwordController,
                label: 'Password',
                hint: '••••••••',
                icon: Icons.lock_outline_rounded,
                isPassword: true,
                obscureText: _obscurePassword,
                onTogglePassword: () {
                  setState(() => _obscurePassword = !_obscurePassword);
                },
              ),
              const SizedBox(height: 12),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton(
                  onPressed: () {},
                  child: Text(
                    'Forgot Password?',
                    style: const TextStyle(color: Color(0xFF00E676)),
                  ),
                ),
              ),
              const SizedBox(height: 40),
              // Login Button
              ElevatedButton(
                onPressed: isLoading ? null : _handleLogin,
                style: ElevatedButton.styleFrom(
                  backgroundColor: const Color(0xFF00E676),
                  foregroundColor: Colors.black,
                  padding: const EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                  elevation: 0,
                ),
                child: isLoading
                    ? const SizedBox(
                        height: 20,
                        width: 20,
                        child: CircularProgressIndicator(
                          strokeWidth: 2,
                          color: Colors.black,
                        ),
                      )
                    : Text(
                        'LOGIN',
                        style: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
              ),
              const SizedBox(height: 24),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Text(
                    "Don't have an account? ",
                    style: TextStyle(color: Colors.white54),
                  ),
                  GestureDetector(
                    onTap: () {},
                    child: Text(
                      'Sign Up',
                      style: const TextStyle(
                        color: Color(0xFF00E676),
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    required String hint,
    required IconData icon,
    bool isPassword = false,
    bool obscureText = false,
    VoidCallback? onTogglePassword,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(
            color: Colors.white70,
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),
        const SizedBox(height: 8),
        TextField(
          controller: controller,
          obscureText: obscureText,
          style: const TextStyle(color: Colors.white),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: const TextStyle(color: Colors.white24),
            prefixIcon: Icon(icon, color: Colors.white54, size: 22),
            suffixIcon: isPassword
                ? IconButton(
                    icon: Icon(
                      obscureText ? Icons.visibility_off : Icons.visibility,
                      color: Colors.white54,
                      size: 22,
                    ),
                    onPressed: onTogglePassword,
                  )
                : null,
            filled: true,
            fillColor: const Color(0xFF1A1A1A),
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(16),
              borderSide: BorderSide.none,
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(16),
              borderSide: const BorderSide(color: Color(0xFF00E676), width: 1.5),
            ),
            contentPadding: const EdgeInsets.symmetric(vertical: 18),
          ),
        ),
      ],
    );
  }
}
