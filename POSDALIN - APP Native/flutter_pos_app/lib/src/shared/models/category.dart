import 'dart:ui';

/// Domain model for a product category
class Category {
  final int categoryId;
  final String categoryName;
  final String imageUrl;
  final int colorIndex; // 0-20, maps to color array

  const Category({
    required this.categoryId,
    required this.categoryName,
    required this.imageUrl,
    required this.colorIndex,
  });

  /// Create a copy of this Category with the given fields replaced
  Category copyWith({
    int? categoryId,
    String? categoryName,
    String? imageUrl,
    int? colorIndex,
  }) {
    return Category(
      categoryId: categoryId ?? this.categoryId,
      categoryName: categoryName ?? this.categoryName,
      imageUrl: imageUrl ?? this.imageUrl,
      colorIndex: colorIndex ?? this.colorIndex,
    );
  }

  /// Create a Category from JSON
  factory Category.fromJson(Map<String, dynamic> json) {
    return Category(
      categoryId: json['categoryId'] as int,
      categoryName: json['categoryName'] as String,
      imageUrl: json['imageUrl'] as String,
      colorIndex: json['colorIndex'] as int,
    );
  }

  /// Convert this Category to JSON
  Map<String, dynamic> toJson() {
    return {
      'categoryId': categoryId,
      'categoryName': categoryName,
      'imageUrl': imageUrl,
      'colorIndex': colorIndex,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Category &&
        other.categoryId == categoryId &&
        other.categoryName == categoryName &&
        other.imageUrl == imageUrl &&
        other.colorIndex == colorIndex;
  }

  @override
  int get hashCode {
    return Object.hash(
      categoryId,
      categoryName,
      imageUrl,
      colorIndex,
    );
  }

  @override
  String toString() {
    return 'Category(categoryId: $categoryId, categoryName: $categoryName, imageUrl: $imageUrl, colorIndex: $colorIndex)';
  }
}
