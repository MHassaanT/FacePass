import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:supabase_flutter/supabase_flutter.dart';

import 'config/supabase_config.dart';
import 'pages/login_page.dart';
import 'services/supabase_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();

  await Supabase.initialize(
    url: SupabaseConfig.url,
    anonKey: SupabaseConfig.anonKey,
  );
  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => SupabaseService()),
      ],
      child: const FacePassApp(),
    ),
  );
}

class FacePassApp extends StatelessWidget {
  const FacePassApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'FacePass',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        brightness: Brightness.dark,
        scaffoldBackgroundColor: const Color(0xFF0D0D0D),
        primaryColor: const Color(0xFF00E676),
        colorScheme: const ColorScheme.dark(
          primary: Color(0xFF00E676),
          secondary: Color(0xFF1565C0),
          surface: Color(0xFF1A1A1A),
        ),
        textTheme: ThemeData.dark().textTheme,
        useMaterial3: true,
      ),
      home: const LoginPage(),
    );
  }
}
