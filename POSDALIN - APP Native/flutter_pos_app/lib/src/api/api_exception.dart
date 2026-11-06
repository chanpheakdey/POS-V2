/// Exception thrown by API client when requests fail
class ApiException implements Exception {
  final String message;
  final int? statusCode;
  final String? endpoint;

  ApiException({
    required this.message,
    this.statusCode,
    this.endpoint,
  });

  @override
  String toString() {
    final buffer = StringBuffer('ApiException: $message');
    if (statusCode != null) {
      buffer.write(' (Status: $statusCode)');
    }
    if (endpoint != null) {
      buffer.write(' [Endpoint: $endpoint]');
    }
    return buffer.toString();
  }
}

/// Thrown when request timeout occurs
class ApiTimeoutException extends ApiException {
  ApiTimeoutException({required String endpoint})
      : super(
          message: 'Request timeout after 30 seconds',
          endpoint: endpoint,
        );
}

/// Thrown when network connection fails
class ApiNetworkException extends ApiException {
  ApiNetworkException({required String endpoint, String? details})
      : super(
          message: 'Network connection failed${details != null ? ': $details' : ''}',
          endpoint: endpoint,
        );
}
