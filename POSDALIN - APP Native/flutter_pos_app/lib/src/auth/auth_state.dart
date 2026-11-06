/// Authentication state representing the current user session.
/// 
/// **Validates: Requirements 1.2, 1.3, 2.1, 2.2, 2.3**
sealed class AuthState {
  const AuthState();
}

/// User is not authenticated (no token or invalid token).
class Unauthenticated extends AuthState {
  const Unauthenticated();
}

/// User is authenticated with a valid token.
class Authenticated extends AuthState {
  final String token;
  final int userId;
  final String username;

  const Authenticated({
    required this.token,
    required this.userId,
    required this.username,
  });

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Authenticated &&
        other.token == token &&
        other.userId == userId &&
        other.username == username;
  }

  @override
  int get hashCode => Object.hash(token, userId, username);
}

/// Authentication is being validated.
class AuthLoading extends AuthState {
  const AuthLoading();
}
