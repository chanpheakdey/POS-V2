/// Data Transfer Object for Price Tier (Sale Price) from ASMX service
class PriceTierDto {
  final int saleId;
  final int itemId;
  final int fromQty;
  final int toQty;
  final double price;
  final String currency;

  PriceTierDto({
    required this.saleId,
    required this.itemId,
    required this.fromQty,
    required this.toQty,
    required this.price,
    required this.currency,
  });

  factory PriceTierDto.fromJson(Map<String, dynamic> json) {
    return PriceTierDto(
      saleId: json['ID'] as int,
      itemId: json['ItemID'] as int? ?? 0,
      fromQty: json['FromQty'] as int,
      toQty: json['ToQty'] as int,
      price: (json['Price'] as num).toDouble(),
      currency: json['Currency'] as String? ?? '\$',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'ID': saleId,
      'ItemID': itemId,
      'FromQty': fromQty,
      'ToQty': toQty,
      'Price': price,
      'Currency': currency,
    };
  }
}
