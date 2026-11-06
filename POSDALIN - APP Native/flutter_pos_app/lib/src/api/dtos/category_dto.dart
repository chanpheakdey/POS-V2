/// Data Transfer Object for Category from ASMX service
class CategoryDto {
  final int categoryId;
  final String categoryName;
  final String imageUrl;

  CategoryDto({
    required this.categoryId,
    required this.categoryName,
    required this.imageUrl,
  });

  factory CategoryDto.fromJson(Map<String, dynamic> json) {
    return CategoryDto(
      categoryId: json['CategoryID'] as int,
      categoryName: json['CategoryName'] as String,
      imageUrl: json['ImageUrl'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'CategoryID': categoryId,
      'CategoryName': categoryName,
      'ImageUrl': imageUrl,
    };
  }
}
