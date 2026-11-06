/// Data Transfer Object for order submission result from ASMX service
class OrderResultDto {
  final int orderId;
  final String invoiceNo;
  final DateTime createdDate;

  OrderResultDto({
    required this.orderId,
    required this.invoiceNo,
    required this.createdDate,
  });

  factory OrderResultDto.fromJson(Map<String, dynamic> json) {
    return OrderResultDto(
      orderId: json['OrderID'] as int,
      invoiceNo: json['InvoiceNo'] as String,
      createdDate: DateTime.parse(json['CreatedDate'] as String),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'OrderID': orderId,
      'InvoiceNo': invoiceNo,
      'CreatedDate': createdDate.toIso8601String(),
    };
  }
}
