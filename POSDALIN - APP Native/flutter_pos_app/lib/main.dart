import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_native_splash/flutter_native_splash.dart';
import 'src/routing/router.dart';
import 'src/shared/theme.dart';
import 'src/auth/auth_service.dart';
import 'src/shared/local_file_store.dart';

/// Main entry point for the POS App.
/// 
/// Implements startup performance optimizations:
/// - Native splash screen shows for ≤2 seconds
/// - Parallel execution of AuthService.validateStoredToken and CatalogService asset copy
/// - Target cold-start < 3 seconds
/// 
/// **Validates: Requirements 16.1, 16.2, 16.6, 16.7**
void main() async {
  // Preserve native splash screen while we initialize
  final widgetsBinding = WidgetsFlutterBinding.ensureInitialized();
  FlutterNativeSplash.preserve(widgetsBinding: widgetsBinding);

  // Create a ProviderContainer for initialization
  final container = ProviderContainer();

  // Start timestamp for performance tracking
  final startTime = DateTime.now();

  try {
    // Run parallel startup operations:
    // 1. AuthService token validation
    // 2. LocalFileStore initialization (copies bundled assets if absent)
    await Future.wait([
      _initializeAuth(container),
      _initializeCatalog(container),
    ]);

    // Calculate initialization time
    final elapsedMs = DateTime.now().difference(startTime).inMilliseconds;
    debugPrint('App initialization completed in ${elapsedMs}ms');

    // Ensure splash shows for at least 500ms but no more than 2000ms
    const minSplashDuration = Duration(milliseconds: 500);
    const maxSplashDuration = Duration(milliseconds: 2000);
    
    final elapsed = Duration(milliseconds: elapsedMs);
    if (elapsed < minSplashDuration) {
      // Wait for minimum duration
      await Future.delayed(minSplashDuration - elapsed);
    } else if (elapsed > maxSplashDuration) {
      // If we exceeded max duration, remove splash immediately
      debugPrint('Warning: Initialization took longer than 2 seconds');
    }
  } catch (e) {
    debugPrint('Error during app initialization: $e');
    // Continue anyway - errors are handled by individual services
  } finally {
    // Dispose the temporary container
    container.dispose();
  }

  // Remove splash screen
  FlutterNativeSplash.remove();

  // Start the app
  runApp(
    const ProviderScope(
      child: MyApp(),
    ),
  );
}

/// Initialize authentication by validating stored token.
/// 
/// **Validates: Requirements 2.1, 2.2, 16.6**
Future<void> _initializeAuth(ProviderContainer container) async {
  try {
    // The AuthService build method already validates the stored token
    // We just need to ensure it's initialized
    await container.read(authServiceProvider.future);
    debugPrint('Auth service initialized');
  } catch (e) {
    debugPrint('Error initializing auth: $e');
    // Continue - user will see login screen
  }
}

/// Initialize catalog by ensuring bundled assets are copied to local storage.
/// 
/// This triggers the LocalFileStore's copyAssetIfAbsent for the main catalog file.
/// Target: local catalog load < 500 ms.
/// 
/// **Validates: Requirements 10.1, 16.7**
Future<void> _initializeCatalog(ProviderContainer container) async {
  try {
    final localFileStore = container.read(localFileStoreProvider);
    
    // Copy the main categories file if absent
    // This is the primary catalog data needed for the home screen
    await localFileStore.copyAssetIfAbsent(
      'assets/catalog/categories.json',
      'catalog/categories.json',
    );
    
    debugPrint('Catalog assets initialized');
  } catch (e) {
    debugPrint('Error initializing catalog: $e');
    // Continue - catalog service will handle missing files
  }
}

class MyApp extends ConsumerWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);

    return MaterialApp.router(
      title: 'POS App',
      theme: AppTheme.lightTheme,
      routerConfig: router,
      debugShowCheckedModeBanner: false,
    );
  }
}
