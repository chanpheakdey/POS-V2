/// Data Transfer Object for submitting an order to ASMX service
class OrderPayloadDto {
  final int customerId;
  final List<OrderItemPayloadDto> items;
  final double total;

  OrderPayloadDto({
    required this.customerId,
    required this.items,
    required this.total,
  });

  Map<String, dynamic> toJson() {
    return {
      'CustomerID': customerId,
      'Items': items.map((item) => item.toJson()).toList(),
      'Total': total,
    };
  }
}

/// Item in order payload
class OrderItemPayloadDto {
  final int itemId;
  final String itemName;
  final String itemCode;
  final int quantity;
  final double unitPrice;
  final int saleId;

  OrderItemPayloadDto({
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.quantity,
    required this.unitPrice,
    required this.saleId,
  });

  Map<String, dynamic> toJson() {
    return {
      'ItemID': itemId,
      'ItemName': itemName,
      'ItemCode': itemCode,
      'Quantity': quantity,
      'UnitPrice': unitPrice,
      'SaleID': saleId,
    };
  }
}
