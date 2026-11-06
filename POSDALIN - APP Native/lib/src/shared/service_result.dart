/// Represents the result of a service operation.
///
/// This sealed class provides a type-safe way to handle different outcomes
/// of service operations, including success and various error types.
///
/// Validates Requirements 13.1, 13.2, 13.4, 13.6
sealed class ServiceResult<T> {
  const ServiceResult();

  /// Creates a successful result with the given value.
  const factory ServiceResult.ok(T value) = Ok;

  /// Creates a network error result with an error message.
  const factory ServiceResult.networkError(String message) = NetworkError;

  /// Creates an authentication error result.
  const factory ServiceResult.authError() = AuthError;

  /// Creates a validation error result with an error message.
  const factory ServiceResult.validationError(String message) = ValidationError;

  /// Creates an unexpected error result with an error message.
  const factory ServiceResult.unexpectedError(String message) = UnexpectedError;
}

/// Represents a successful service operation result.
final class Ok<T> extends ServiceResult<T> {
  /// The value returned by the successful operation.
  final T value;

  const Ok(this.value);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is Ok<T> &&
          runtimeType == other.runtimeType &&
          value == other.value;

  @override
  int get hashCode => value.hashCode;

  @override
  String toString() => 'Ok(value: $value)';
}

/// Represents a network error result.
///
/// Network errors are shown as snackbars; cached/local data continues
/// to serve the UI.
final class NetworkError<T> extends ServiceResult<T> {
  /// The error message describing what went wrong.
  final String message;

  const NetworkError(this.message);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is NetworkError<T> &&
          runtimeType == other.runtimeType &&
          message == other.message;

  @override
  int get hashCode => message.hashCode;

  @override
  String toString() => 'NetworkError(message: $message)';
}

/// Represents an authentication error result.
///
/// Authentication errors cause the router to redirect to `/login`.
final class AuthError<T> extends ServiceResult<T> {
  const AuthError();

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is AuthError<T> && runtimeType == other.runtimeType;

  @override
  int get hashCode => runtimeType.hashCode;

  @override
  String toString() => 'AuthError()';
}

/// Represents a validation error result.
///
/// Validation errors are highlighted with inline messages (e.g., red border
/// on login fields).
final class ValidationError<T> extends ServiceResult<T> {
  /// The validation error message.
  final String message;

  const ValidationError(this.message);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is ValidationError<T> &&
          runtimeType == other.runtimeType &&
          message == other.message;

  @override
  int get hashCode => message.hashCode;

  @override
  String toString() => 'ValidationError(message: $message)';
}

/// Represents an unexpected error result.
///
/// Unexpected errors are logged to the device log file and a generic
/// "An unexpected error occurred" toast is shown.
final class UnexpectedError<T> extends ServiceResult<T> {
  /// The error message describing what went wrong.
  final String message;

  const UnexpectedError(this.message);

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is UnexpectedError<T> &&
          runtimeType == other.runtimeType &&
          message == other.message;

  @override
  int get hashCode => message.hashCode;

  @override
  String toString() => 'UnexpectedError(message: $message)';
}
