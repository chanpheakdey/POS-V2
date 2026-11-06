import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'api_client.dart';

/// Provider for the base URL configuration
/// This should be configured based on environment (dev/staging/production)
final baseUrlProvider = Provider<String>((ref) {
  // TODO: Replace with actual backend URL
  // This could be loaded from environment config or build config
  return 'https://your-backend-url.com';
});

/// Provider for the ApiClient instance
/// Watches for token changes and creates new client when token updates
final apiClientProvider = Provider<ApiClient>((ref) {
  final baseUrl = ref.watch(baseUrlProvider);
  final token = ref.watch(authTokenProvider);
  
  return ApiClient(
    baseUrl: baseUrl,
    token: token,
  );
});

/// Provider for the current authentication token
/// This should be implemented by the auth service layer
final authTokenProvider = Provider<String?>((ref) {
  // This will be overridden by the auth service
  return null;
});
