/// Domain model for a customer order
class Order {
  final int orderId;
  final int customerId;
  final String invoiceNo;
  final DateTime createdDate;
  final double total;
  final bool deleted;

  const Order({
    required this.orderId,
    required this.customerId,
    required this.invoiceNo,
    required this.createdDate,
    required this.total,
    required this.deleted,
  });

  /// Create a copy of this Order with the given fields replaced
  Order copyWith({
    int? orderId,
    int? customerId,
    String? invoiceNo,
    DateTime? createdDate,
    double? total,
    bool? deleted,
  }) {
    return Order(
      orderId: orderId ?? this.orderId,
      customerId: customerId ?? this.customerId,
      invoiceNo: invoiceNo ?? this.invoiceNo,
      createdDate: createdDate ?? this.createdDate,
      total: total ?? this.total,
      deleted: deleted ?? this.deleted,
    );
  }

  /// Create an Order from JSON
  factory Order.fromJson(Map<String, dynamic> json) {
    return Order(
      orderId: json['orderId'] as int,
      customerId: json['customerId'] as int,
      invoiceNo: json['invoiceNo'] as String,
      createdDate: DateTime.parse(json['createdDate'] as String),
      total: (json['total'] as num).toDouble(),
      deleted: json['deleted'] as bool,
    );
  }

  /// Convert this Order to JSON
  Map<String, dynamic> toJson() {
    return {
      'orderId': orderId,
      'customerId': customerId,
      'invoiceNo': invoiceNo,
      'createdDate': createdDate.toIso8601String(),
      'total': total,
      'deleted': deleted,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Order &&
        other.orderId == orderId &&
        other.customerId == customerId &&
        other.invoiceNo == invoiceNo &&
        other.createdDate == createdDate &&
        other.total == total &&
        other.deleted == deleted;
  }

  @override
  int get hashCode {
    return Object.hash(
      orderId,
      customerId,
      invoiceNo,
      createdDate,
      total,
      deleted,
    );
  }

  @override
  String toString() {
    return 'Order(orderId: $orderId, customerId: $customerId, invoiceNo: $invoiceNo, createdDate: $createdDate, total: $total, deleted: $deleted)';
  }
}
