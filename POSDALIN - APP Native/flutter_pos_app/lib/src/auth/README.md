# Authentication Module

Token-based authentication service for customer-level users with persistent session management.

## Overview

The authentication module provides secure token-based authentication using platform secure storage. Tokens persist indefinitely until explicit logout, eliminating the need for repeated logins.

## Features

- **Customer-only authentication**: Only users with `UserLevel='Customer'` can authenticate
- **Persistent tokens**: Tokens stored securely with no expiration
- **Automatic token validation**: On app start, stored tokens are validated with backend
- **Secure logout**: Properly expires tokens on backend and clears all local state
- **Cart clearing**: Automatically clears shopping cart on logout

## Usage

### Basic Authentication Flow

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/auth/auth.dart';

// In a widget
class LoginScreen extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authServiceProvider);
    
    return authState.when(
      data: (state) {
        if (state is Authenticated) {
          // User is authenticated, navigate to home
          return HomeScreen();
        } else {
          // Show login form
          return LoginForm();
        }
      },
      loading: () => LoadingIndicator(),
      error: (error, stack) => ErrorScreen(error),
    );
  }
}
```

### Login

```dart
Future<void> handleLogin(WidgetRef ref, String username, String password) async {
  final authService = ref.read(authServiceProvider.notifier);
  final result = await authService.login(username, password);
  
  switch (result) {
    case AuthSuccess():
      // Navigate to home screen
      router.go('/home');
      
    case AuthFailure(message: final msg):
      // Show error message
      showSnackBar(msg);
      
    case AuthAccountLocked():
      // Show account locked message
      showSnackBar('Your user account has been locked! Please contact IT');
      
    case AuthInvalidUserLevel():
      // Show invalid user level message
      showSnackBar('Only customer accounts can access this application');
      
    case AuthNetworkError(message: final msg):
      // Show network error
      showSnackBar('Network error: $msg');
  }
}
```

### Logout

```dart
Future<void> handleLogout(WidgetRef ref) async {
  final authService = ref.read(authServiceProvider.notifier);
  await authService.logout();
  
  // Navigate to login screen
  router.go('/login');
}
```

### Accessing Current User

```dart
// Get current token
final authService = ref.read(authServiceProvider.notifier);
final token = authService.currentToken;

// Get current user ID
final userId = authService.currentUserId;
```

### Validate Token

```dart
// Manually validate stored token
final authService = ref.read(authServiceProvider.notifier);
final isValid = await authService.validateStoredToken();

if (isValid) {
  // Token is valid, proceed
} else {
  // Token invalid, show login screen
}
```

## Architecture

### Components

- **AuthService**: Main service class managing authentication lifecycle
- **AuthState**: Represents current authentication state
  - `Unauthenticated`: No valid session
  - `Authenticated`: Valid session with token and user info
  - `AuthLoading`: Authentication in progress
- **AuthResult**: Result of authentication operations
  - `AuthSuccess`: Login succeeded
  - `AuthFailure`: Login failed with error message
  - `AuthAccountLocked`: Account locked after failed attempts
  - `AuthInvalidUserLevel`: User is not a customer
  - `AuthNetworkError`: Network-related error

### Storage

- **Token storage**: `flutter_secure_storage` for platform-specific secure storage
- **Stored keys**:
  - `auth_token`: Authentication token
  - `user_id`: User ID
  - `username`: Username

### Backend Integration

The authentication service communicates with the following backend endpoints:

- `APPLogin.aspx`: Login endpoint
- `appservice.asmx/CheckToken`: Token validation
- `appservice.asmx/removetoken`: Token expiration

## Security

- Tokens stored in platform secure storage (Keychain on iOS, KeyStore on Android)
- Passwords never logged or stored locally
- All API communication over HTTPS
- Automatic token validation on app start
- Secure token expiration on logout

## Requirements Validated

This module validates the following requirements:

- **1.2**: Credential validation against database for `UserLevel='Customer'`
- **1.3**: Token generation and persistent storage
- **1.5**: Account locking after 5 failed attempts
- **1.7**: Customer-level only authentication
- **1.8**: Persistent token storage without expiration
- **2.1**: Token validation on app launch
- **2.2**: Token validation with backend
- **2.6**: Token persistence until explicit logout
- **2.7**: Token removal and expiration on logout
- **15.2**: Token expiration in database
- **15.3**: Token removal from local storage
- **17.3**: Secure token storage using platform APIs

## Testing

See `test/auth/auth_service_test.dart` for comprehensive unit tests covering:

- Login success and failure scenarios
- Token validation
- Logout flow
- Error handling
- State management

## Error Messages

The service returns exact error messages from the backend:

- `"User name is incorrect!"` - Username not found
- `"Password is incorrect!"` - Wrong password
- `"Your user account has been locked! Please contact IT"` - Account locked
- `"Only customer accounts can access this application"` - Non-customer user

## Dependencies

- `flutter_secure_storage`: Secure platform storage
- `flutter_riverpod`: State management
- `../api/api_client.dart`: Backend API communication
- `../shared/local_file_store.dart`: Local file operations
