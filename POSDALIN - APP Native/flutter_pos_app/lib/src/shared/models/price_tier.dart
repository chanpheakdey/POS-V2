/// Domain model for a price tier based on quantity ranges
class PriceTier {
  final int saleId;
  final int itemId;
  final int fromQty;
  final int toQty; // 0 = unlimited
  final double priceUsd;
  final double priceKhr; // priceUsd × exchange rate

  const PriceTier({
    required this.saleId,
    required this.itemId,
    required this.fromQty,
    required this.toQty,
    required this.priceUsd,
    required this.priceKhr,
  });

  /// Create a copy of this PriceTier with the given fields replaced
  PriceTier copyWith({
    int? saleId,
    int? itemId,
    int? fromQty,
    int? toQty,
    double? priceUsd,
    double? priceKhr,
  }) {
    return PriceTier(
      saleId: saleId ?? this.saleId,
      itemId: itemId ?? this.itemId,
      fromQty: fromQty ?? this.fromQty,
      toQty: toQty ?? this.toQty,
      priceUsd: priceUsd ?? this.priceUsd,
      priceKhr: priceKhr ?? this.priceKhr,
    );
  }

  /// Create a PriceTier from JSON
  factory PriceTier.fromJson(Map<String, dynamic> json) {
    return PriceTier(
      saleId: json['saleId'] as int,
      itemId: json['itemId'] as int,
      fromQty: json['fromQty'] as int,
      toQty: json['toQty'] as int,
      priceUsd: (json['priceUsd'] as num).toDouble(),
      priceKhr: (json['priceKhr'] as num).toDouble(),
    );
  }

  /// Convert this PriceTier to JSON
  Map<String, dynamic> toJson() {
    return {
      'saleId': saleId,
      'itemId': itemId,
      'fromQty': fromQty,
      'toQty': toQty,
      'priceUsd': priceUsd,
      'priceKhr': priceKhr,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is PriceTier &&
        other.saleId == saleId &&
        other.itemId == itemId &&
        other.fromQty == fromQty &&
        other.toQty == toQty &&
        other.priceUsd == priceUsd &&
        other.priceKhr == priceKhr;
  }

  @override
  int get hashCode {
    return Object.hash(
      saleId,
      itemId,
      fromQty,
      toQty,
      priceUsd,
      priceKhr,
    );
  }

  @override
  String toString() {
    return 'PriceTier(saleId: $saleId, itemId: $itemId, fromQty: $fromQty, toQty: $toQty, priceUsd: $priceUsd, priceKhr: $priceKhr)';
  }
}
