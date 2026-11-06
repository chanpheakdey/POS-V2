/// Domain model for an item within an order
class OrderDetail {
  final int orderId;
  final int itemId;
  final String itemName;
  final String itemCode;
  final int quantity;
  final double unitPrice;
  final double lineTotal;

  const OrderDetail({
    required this.orderId,
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  /// Create a copy of this OrderDetail with the given fields replaced
  OrderDetail copyWith({
    int? orderId,
    int? itemId,
    String? itemName,
    String? itemCode,
    int? quantity,
    double? unitPrice,
    double? lineTotal,
  }) {
    return OrderDetail(
      orderId: orderId ?? this.orderId,
      itemId: itemId ?? this.itemId,
      itemName: itemName ?? this.itemName,
      itemCode: itemCode ?? this.itemCode,
      quantity: quantity ?? this.quantity,
      unitPrice: unitPrice ?? this.unitPrice,
      lineTotal: lineTotal ?? this.lineTotal,
    );
  }

  /// Create an OrderDetail from JSON
  factory OrderDetail.fromJson(Map<String, dynamic> json) {
    return OrderDetail(
      orderId: json['orderId'] as int,
      itemId: json['itemId'] as int,
      itemName: json['itemName'] as String,
      itemCode: json['itemCode'] as String,
      quantity: json['quantity'] as int,
      unitPrice: (json['unitPrice'] as num).toDouble(),
      lineTotal: (json['lineTotal'] as num).toDouble(),
    );
  }

  /// Convert this OrderDetail to JSON
  Map<String, dynamic> toJson() {
    return {
      'orderId': orderId,
      'itemId': itemId,
      'itemName': itemName,
      'itemCode': itemCode,
      'quantity': quantity,
      'unitPrice': unitPrice,
      'lineTotal': lineTotal,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is OrderDetail &&
        other.orderId == orderId &&
        other.itemId == itemId &&
        other.itemName == itemName &&
        other.itemCode == itemCode &&
        other.quantity == quantity &&
        other.unitPrice == unitPrice &&
        other.lineTotal == lineTotal;
  }

  @override
  int get hashCode {
    return Object.hash(
      orderId,
      itemId,
      itemName,
      itemCode,
      quantity,
      unitPrice,
      lineTotal,
    );
  }

  @override
  String toString() {
    return 'OrderDetail(orderId: $orderId, itemId: $itemId, itemName: $itemName, itemCode: $itemCode, quantity: $quantity, unitPrice: $unitPrice, lineTotal: $lineTotal)';
  }
}
