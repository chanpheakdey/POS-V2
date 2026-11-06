/// Data Transfer Object for Item from ASMX service
class ItemDto {
  final int itemId;
  final int categoryId;
  final int groupId;
  final String itemName;
  final String itemCode;
  final String imageUrl;
  final double price;

  ItemDto({
    required this.itemId,
    required this.categoryId,
    required this.groupId,
    required this.itemName,
    required this.itemCode,
    required this.imageUrl,
    required this.price,
  });

  factory ItemDto.fromJson(Map<String, dynamic> json) {
    return ItemDto(
      itemId: json['ItemID'] as int,
      categoryId: json['CategoryID'] as int? ?? 0,
      groupId: json['GroupID'] as int? ?? 0,
      itemName: json['ItemName'] as String,
      itemCode: json['ItemCode'] as String? ?? '',
      imageUrl: json['ImageUrl'] as String? ?? '',
      price: (json['Price'] as num?)?.toDouble() ?? 0.0,
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'ItemID': itemId,
      'CategoryID': categoryId,
      'GroupID': groupId,
      'ItemName': itemName,
      'ItemCode': itemCode,
      'ImageUrl': imageUrl,
      'Price': price,
    };
  }
}
