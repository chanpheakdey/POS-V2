/// Domain model for a product item
class Item {
  final int itemId;
  final int categoryId;
  final int groupId;
  final String itemName;
  final String itemCode;
  final String imageUrl;
  final double price; // lowest tier / display price

  const Item({
    required this.itemId,
    required this.categoryId,
    required this.groupId,
    required this.itemName,
    required this.itemCode,
    required this.imageUrl,
    required this.price,
  });

  /// Create a copy of this Item with the given fields replaced
  Item copyWith({
    int? itemId,
    int? categoryId,
    int? groupId,
    String? itemName,
    String? itemCode,
    String? imageUrl,
    double? price,
  }) {
    return Item(
      itemId: itemId ?? this.itemId,
      categoryId: categoryId ?? this.categoryId,
      groupId: groupId ?? this.groupId,
      itemName: itemName ?? this.itemName,
      itemCode: itemCode ?? this.itemCode,
      imageUrl: imageUrl ?? this.imageUrl,
      price: price ?? this.price,
    );
  }

  /// Create an Item from JSON
  factory Item.fromJson(Map<String, dynamic> json) {
    return Item(
      itemId: json['itemId'] as int,
      categoryId: json['categoryId'] as int,
      groupId: json['groupId'] as int,
      itemName: json['itemName'] as String,
      itemCode: json['itemCode'] as String,
      imageUrl: json['imageUrl'] as String,
      price: (json['price'] as num).toDouble(),
    );
  }

  /// Convert this Item to JSON
  Map<String, dynamic> toJson() {
    return {
      'itemId': itemId,
      'categoryId': categoryId,
      'groupId': groupId,
      'itemName': itemName,
      'itemCode': itemCode,
      'imageUrl': imageUrl,
      'price': price,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Item &&
        other.itemId == itemId &&
        other.categoryId == categoryId &&
        other.groupId == groupId &&
        other.itemName == itemName &&
        other.itemCode == itemCode &&
        other.imageUrl == imageUrl &&
        other.price == price;
  }

  @override
  int get hashCode {
    return Object.hash(
      itemId,
      categoryId,
      groupId,
      itemName,
      itemCode,
      imageUrl,
      price,
    );
  }

  @override
  String toString() {
    return 'Item(itemId: $itemId, categoryId: $categoryId, groupId: $groupId, itemName: $itemName, itemCode: $itemCode, imageUrl: $imageUrl, price: $price)';
  }
}
