/// Data Transfer Object for Order from ASMX service
class OrderDto {
  final int orderId;
  final int customerId;
  final String invoiceNo;
  final DateTime createdDate;
  final double total;
  final bool deleted;

  OrderDto({
    required this.orderId,
    required this.customerId,
    required this.invoiceNo,
    required this.createdDate,
    required this.total,
    this.deleted = false,
  });

  factory OrderDto.fromJson(Map<String, dynamic> json) {
    return OrderDto(
      orderId: json['OrderID'] as int,
      customerId: json['CustomerID'] as int? ?? 0,
      invoiceNo: json['InvoiceNo'] as String? ?? '',
      createdDate: DateTime.parse(json['CreatedDate'] as String),
      total: (json['Total'] as num).toDouble(),
      deleted: json['Deleted'] as bool? ?? false,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'OrderID': orderId,
      'CustomerID': customerId,
      'InvoiceNo': invoiceNo,
      'CreatedDate': createdDate.toIso8601String(),
      'Total': total,
      'Deleted': deleted,
    };
  }
}

/// Data Transfer Object for Order Detail (line items)
class OrderDetailDto {
  final int orderId;
  final int itemId;
  final String itemName;
  final String itemCode;
  final int quantity;
  final double unitPrice;
  final double lineTotal;

  OrderDetailDto({
    required this.orderId,
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  factory OrderDetailDto.fromJson(Map<String, dynamic> json) {
    return OrderDetailDto(
      orderId: json['OrderID'] as int,
      itemId: json['ItemID'] as int,
      itemName: json['ItemName'] as String,
      itemCode: json['ItemCode'] as String? ?? '',
      quantity: json['Quantity'] as int,
      unitPrice: (json['UnitPrice'] as num).toDouble(),
      lineTotal: (json['LineTotal'] as num).toDouble(),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'OrderID': orderId,
      'ItemID': itemId,
      'ItemName': itemName,
      'ItemCode': itemCode,
      'Quantity': quantity,
      'UnitPrice': unitPrice,
      'LineTotal': lineTotal,
    };
  }
}
