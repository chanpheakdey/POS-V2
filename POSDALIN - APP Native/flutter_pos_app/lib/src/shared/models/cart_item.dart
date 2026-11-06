/// Domain model for an item in the shopping cart
class CartItem {
  final int itemId;
  final String itemName;
  final String itemCode;
  int quantity;
  double unitPriceUsd;
  double unitPriceKhr;
  int appliedSaleId;

  CartItem({
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.quantity,
    required this.unitPriceUsd,
    required this.unitPriceKhr,
    required this.appliedSaleId,
  });

  /// Create a copy of this CartItem with the given fields replaced
  CartItem copyWith({
    int? itemId,
    String? itemName,
    String? itemCode,
    int? quantity,
    double? unitPriceUsd,
    double? unitPriceKhr,
    int? appliedSaleId,
  }) {
    return CartItem(
      itemId: itemId ?? this.itemId,
      itemName: itemName ?? this.itemName,
      itemCode: itemCode ?? this.itemCode,
      quantity: quantity ?? this.quantity,
      unitPriceUsd: unitPriceUsd ?? this.unitPriceUsd,
      unitPriceKhr: unitPriceKhr ?? this.unitPriceKhr,
      appliedSaleId: appliedSaleId ?? this.appliedSaleId,
    );
  }

  /// Create a CartItem from JSON
  factory CartItem.fromJson(Map<String, dynamic> json) {
    return CartItem(
      itemId: json['itemId'] as int,
      itemName: json['itemName'] as String,
      itemCode: json['itemCode'] as String,
      quantity: json['quantity'] as int,
      unitPriceUsd: (json['unitPriceUsd'] as num).toDouble(),
      unitPriceKhr: (json['unitPriceKhr'] as num).toDouble(),
      appliedSaleId: json['appliedSaleId'] as int,
    );
  }

  /// Convert this CartItem to JSON
  Map<String, dynamic> toJson() {
    return {
      'itemId': itemId,
      'itemName': itemName,
      'itemCode': itemCode,
      'quantity': quantity,
      'unitPriceUsd': unitPriceUsd,
      'unitPriceKhr': unitPriceKhr,
      'appliedSaleId': appliedSaleId,
    };
  }

  /// Calculate the line total in USD
  double get lineTotalUsd => quantity * unitPriceUsd;

  /// Calculate the line total in KHR
  double get lineTotalKhr => quantity * unitPriceKhr;

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is CartItem &&
        other.itemId == itemId &&
        other.itemName == itemName &&
        other.itemCode == itemCode &&
        other.quantity == quantity &&
        other.unitPriceUsd == unitPriceUsd &&
        other.unitPriceKhr == unitPriceKhr &&
        other.appliedSaleId == appliedSaleId;
  }

  @override
  int get hashCode {
    return Object.hash(
      itemId,
      itemName,
      itemCode,
      quantity,
      unitPriceUsd,
      unitPriceKhr,
      appliedSaleId,
    );
  }

  @override
  String toString() {
    return 'CartItem(itemId: $itemId, itemName: $itemName, itemCode: $itemCode, quantity: $quantity, unitPriceUsd: $unitPriceUsd, unitPriceKhr: $unitPriceKhr, appliedSaleId: $appliedSaleId)';
  }
}
