/// Result of an authentication operation.
/// 
/// **Validates: Requirements 1.4, 1.5, 1.6, 13.2, 13.3**
sealed class AuthResult {
  const AuthResult();
}

/// Authentication was successful.
class AuthSuccess extends AuthResult {
  final String token;
  final int userId;
  final String username;

  const AuthSuccess({
    required this.token,
    required this.userId,
    required this.username,
  });
}

/// Authentication failed due to incorrect credentials.
class AuthFailure extends AuthResult {
  final String message;

  const AuthFailure(this.message);
}

/// Authentication failed due to account being locked.
class AuthAccountLocked extends AuthResult {
  const AuthAccountLocked();
  
  String get message => 'Your user account has been locked! Please contact IT';
}

/// Authentication failed due to network error.
class AuthNetworkError extends AuthResult {
  final String message;

  const AuthNetworkError(this.message);
}

/// Authentication failed due to user not being a customer.
class AuthInvalidUserLevel extends AuthResult {
  const AuthInvalidUserLevel();
  
  String get message => 'Only customer accounts can access this application';
}
