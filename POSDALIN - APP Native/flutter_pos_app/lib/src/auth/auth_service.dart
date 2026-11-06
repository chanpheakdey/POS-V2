import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../api/api_client.dart';
import '../api/api_exception.dart';
import '../shared/local_file_store.dart';
import 'auth_state.dart';
import 'auth_result.dart';

part 'auth_service.g.dart';

/// Service managing authentication lifecycle including login, token validation, and logout.
/// 
/// Implements token-based authentication with persistent storage using platform secure storage.
/// Tokens persist indefinitely until explicit logout (no expiration).
/// 
/// **Validates: Requirements 1.2, 1.3, 1.5, 1.7, 1.8, 2.1, 2.2, 2.6, 2.7, 15.2, 15.3, 17.3**
@riverpod
class AuthService extends _$AuthService {
  static const _tokenKey = 'auth_token';
  static const _userIdKey = 'user_id';
  static const _usernameKey = 'username';
  static const _cartFilePath = 'cart.json';

  late final FlutterSecureStorage _secureStorage;
  late final ApiClient _apiClient;
  late final LocalFileStore _localFileStore;

  @override
  Future<AuthState> build() async {
    _secureStorage = const FlutterSecureStorage();
    // Initialize API client with base URL
    // The token will be set after validation
    _apiClient = ApiClient(
      baseUrl: 'https://your-backend-url.com', // TODO: Load from config
    );
    _localFileStore = ref.watch(localFileStoreProvider);

    // On app start, validate stored token
    return await _validateStoredToken();
  }

  /// Current authentication token, or null if not authenticated.
  String? get currentToken {
    final authState = state.value;
    if (authState is Authenticated) {
      return authState.token;
    }
    return null;
  }

  /// Current user ID, or null if not authenticated.
  int? get currentUserId {
    final authState = state.value;
    if (authState is Authenticated) {
      return authState.userId;
    }
    return null;
  }

  /// Login with username and password.
  /// 
  /// Only allows customer-level user authentication (UserLevel='Customer').
  /// Stores token persistently with no expiration.
  /// 
  /// **Validates: Requirements 1.2, 1.3, 1.7, 1.8**
  Future<AuthResult> login(String username, String password) async {
    if (username.trim().isEmpty || password.trim().isEmpty) {
      return const AuthFailure('Username and password are required');
    }

    try {
      // Call backend login endpoint - this will set cookies and return token
      final token = await _apiClient.login(username, password);
      
      if (token.isEmpty) {
        return const AuthFailure('Failed to obtain authentication token');
      }
      
      // Set token in API client for subsequent requests
      _apiClient.setToken(token);
      
      // Validate the token and get user information
      final validationResult = await _apiClient.checkToken(token);
      
      if (validationResult != 'Valid') {
        return const AuthFailure('Authentication failed');
      }
      
      // Get userId by checking the token in database
      // For now, we'll use a placeholder - in production, this should come from 
      // a separate API call that returns user details
      final userId = await _getUserIdFromToken(token);
      
      // Store credentials securely
      await _secureStorage.write(key: _tokenKey, value: token);
      await _secureStorage.write(key: _userIdKey, value: userId.toString());
      await _secureStorage.write(key: _usernameKey, value: username);
      
      // Update state to authenticated
      state = AsyncData(Authenticated(
        token: token,
        userId: userId,
        username: username,
      ));
      
      return AuthSuccess(
        token: token,
        userId: userId,
        username: username,
      );
    } on ApiException catch (e) {
      // Map API exceptions to appropriate auth results based on exact backend error messages
      final message = e.message;
      
      // Check for exact error messages from the backend
      if (message.contains('User name is incorrect!')) {
        return const AuthFailure('User name is incorrect!');
      } else if (message.contains('Password is incorrect!')) {
        return const AuthFailure('Password is incorrect!');
      } else if (message.contains('Your user account has been locked!')) {
        return const AuthAccountLocked();
      } else if (message.toLowerCase().contains('customer')) {
        return const AuthInvalidUserLevel();
      } else if (e is ApiTimeoutException) {
        return const AuthNetworkError('Request timed out. Please try again.');
      } else if (e is ApiNetworkException) {
        return AuthNetworkError('Network error: ${e.message}');
      }
      
      return AuthFailure(e.message);
    } catch (e) {
      return AuthNetworkError('An unexpected error occurred: $e');
    }
  }
  
  /// Get userId from token by querying the backend
  /// This is a helper method to extract user ID after successful login
  Future<int> _getUserIdFromToken(String token) async {
    try {
      // In the real implementation, this should query tblToken -> tblUser
      // For now, we'll return a placeholder
      // TODO: Implement proper getUserId endpoint in backend
      return 0; // Placeholder
    } catch (e) {
      return 0;
    }
  }

  /// Validate stored token on app start.
  /// 
  /// Reads token from secure storage and validates with backend.
  /// Returns authenticated state if valid, unauthenticated otherwise.
  /// 
  /// **Validates: Requirements 2.1, 2.2, 2.3**
  Future<AuthState> _validateStoredToken() async {
    try {
      // Read stored credentials
      final token = await _secureStorage.read(key: _tokenKey);
      final userIdStr = await _secureStorage.read(key: _userIdKey);
      final username = await _secureStorage.read(key: _usernameKey);
      
      if (token == null || userIdStr == null || username == null) {
        return const Unauthenticated();
      }
      
      // Validate token with backend
      final validationResult = await _apiClient.checkToken(token);
      
      if (validationResult == 'Valid') {
        final userId = int.tryParse(userIdStr) ?? 0;
        
        // Set token in API client for subsequent requests
        _apiClient.setToken(token);
        
        return Authenticated(
          token: token,
          userId: userId,
          username: username,
        );
      } else {
        // Token invalid, clear stored credentials
        await _clearStoredCredentials();
        return const Unauthenticated();
      }
    } catch (e) {
      // On any error, return unauthenticated
      return const Unauthenticated();
    }
  }

  /// Validate the currently stored token.
  /// 
  /// Public method for external token validation checks.
  /// 
  /// **Validates: Requirements 2.2**
  Future<bool> validateStoredToken() async {
    final authState = await _validateStoredToken();
    state = AsyncData(authState);
    return authState is Authenticated;
  }

  /// Logout the current user.
  /// 
  /// Expires token on backend, clears secure storage, and clears cart.
  /// 
  /// **Validates: Requirements 15.2, 15.3, 15.4, 15.5, 15.6**
  Future<void> logout() async {
    try {
      final token = currentToken;
      
      // Expire token on backend
      if (token != null) {
        try {
          await _apiClient.expireToken(token);
        } catch (e) {
          // Continue with logout even if backend call fails
        }
      }
      
      // Clear cart contents
      await _clearCart();
      
      // Clear stored credentials
      await _clearStoredCredentials();
      
      // Clear token from API client
      _apiClient.setToken(null);
      
      // Update state to unauthenticated
      state = const AsyncData(Unauthenticated());
    } catch (e) {
      // Even on error, clear local state
      await _clearCart();
      await _clearStoredCredentials();
      _apiClient.setToken(null);
      state = const AsyncData(Unauthenticated());
    }
  }

  /// Clear stored credentials from secure storage.
  Future<void> _clearStoredCredentials() async {
    await _secureStorage.delete(key: _tokenKey);
    await _secureStorage.delete(key: _userIdKey);
    await _secureStorage.delete(key: _usernameKey);
  }

  /// Clear cart contents from local file store.
  /// 
  /// **Validates: Requirements 15.4**
  Future<void> _clearCart() async {
    try {
      await _localFileStore.writeList<Map<String, dynamic>>(
        _cartFilePath,
        [],
        (item) => item,
      );
    } catch (e) {
      // Silently fail if cart clearing fails
    }
  }
}
