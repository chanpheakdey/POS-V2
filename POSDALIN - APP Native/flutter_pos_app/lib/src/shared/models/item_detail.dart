import 'price_tier.dart';

/// Domain model for detailed item information including price tiers
class ItemDetail {
  final int itemId;
  final String itemName;
  final String itemCode;
  final String imageUrl;
  final List<PriceTier> priceTiers;

  const ItemDetail({
    required this.itemId,
    required this.itemName,
    required this.itemCode,
    required this.imageUrl,
    required this.priceTiers,
  });

  /// Create a copy of this ItemDetail with the given fields replaced
  ItemDetail copyWith({
    int? itemId,
    String? itemName,
    String? itemCode,
    String? imageUrl,
    List<PriceTier>? priceTiers,
  }) {
    return ItemDetail(
      itemId: itemId ?? this.itemId,
      itemName: itemName ?? this.itemName,
      itemCode: itemCode ?? this.itemCode,
      imageUrl: imageUrl ?? this.imageUrl,
      priceTiers: priceTiers ?? this.priceTiers,
    );
  }

  /// Create an ItemDetail from JSON
  factory ItemDetail.fromJson(Map<String, dynamic> json) {
    return ItemDetail(
      itemId: json['itemId'] as int,
      itemName: json['itemName'] as String,
      itemCode: json['itemCode'] as String,
      imageUrl: json['imageUrl'] as String,
      priceTiers: (json['priceTiers'] as List<dynamic>)
          .map((tier) => PriceTier.fromJson(tier as Map<String, dynamic>))
          .toList(),
    );
  }

  /// Convert this ItemDetail to JSON
  Map<String, dynamic> toJson() {
    return {
      'itemId': itemId,
      'itemName': itemName,
      'itemCode': itemCode,
      'imageUrl': imageUrl,
      'priceTiers': priceTiers.map((tier) => tier.toJson()).toList(),
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    if (other is! ItemDetail) return false;
    if (other.itemId != itemId) return false;
    if (other.itemName != itemName) return false;
    if (other.itemCode != itemCode) return false;
    if (other.imageUrl != imageUrl) return false;
    if (other.priceTiers.length != priceTiers.length) return false;
    for (int i = 0; i < priceTiers.length; i++) {
      if (other.priceTiers[i] != priceTiers[i]) return false;
    }
    return true;
  }

  @override
  int get hashCode {
    return Object.hash(
      itemId,
      itemName,
      itemCode,
      imageUrl,
      Object.hashAll(priceTiers),
    );
  }

  @override
  String toString() {
    return 'ItemDetail(itemId: $itemId, itemName: $itemName, itemCode: $itemCode, imageUrl: $imageUrl, priceTiers: $priceTiers)';
  }
}
