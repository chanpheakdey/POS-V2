/// Result type for order operations
sealed class OrderResult {
  const OrderResult();
}

/// Order placed successfully
class OrderSuccess extends OrderResult {
  final int orderId;
  final String invoiceNo;
  final DateTime createdDate;

  const OrderSuccess({
    required this.orderId,
    required this.invoiceNo,
    required this.createdDate,
  });
}

/// Order queued for offline submission
class OrderQueued extends OrderResult {
  const OrderQueued();
}

/// Order operation failed
class OrderFailure extends OrderResult {
  final String message;

  const OrderFailure(this.message);
}

/// Network error during order operation
class OrderNetworkError extends OrderResult {
  const OrderNetworkError();
}
