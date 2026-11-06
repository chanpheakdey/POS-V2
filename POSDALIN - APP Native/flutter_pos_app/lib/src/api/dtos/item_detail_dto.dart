import 'price_tier_dto.dart';

/// Data Transfer Object for Item Detail from ASMX service
class ItemDetailDto {
  final int itemId;
  final String itemName;
  final String itemCode;
  final String imageUrl;
  final List<PriceTierDto>? priceTiers;

  ItemDetailDto({
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.imageUrl,
    this.priceTiers,
  });

  factory ItemDetailDto.fromJson(Map<String, dynamic> json) {
    return ItemDetailDto(
      itemId: json['ItemID'] as int,
      itemName: json['ItemName'] as String,
      itemCode: json['ItemCode'] as String? ?? '',
      imageUrl: json['ImageUrl'] as String? ?? '',
      priceTiers: (json['PriceTiers'] as List<dynamic>?)
              ?.map((tier) => PriceTierDto.fromJson(tier as Map<String, dynamic>))
              .toList() ??
          [],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'ItemID': itemId,
      'ItemName': itemName,
      'ItemCode': itemCode,
      'ImageUrl': imageUrl,
      'PriceTiers': priceTiers.map((tier) => tier.toJson()).toList(),
    };
  }
}
