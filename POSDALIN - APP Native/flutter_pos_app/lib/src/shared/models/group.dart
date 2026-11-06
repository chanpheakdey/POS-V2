/// Domain model for a product group within a category
class Group {
  final int groupId;
  final int categoryId;
  final String groupName;
  final String imageUrl;

  const Group({
    required this.groupId,
    required this.categoryId,
    required this.groupName,
    required this.imageUrl,
  });

  /// Create a copy of this Group with the given fields replaced
  Group copyWith({
    int? groupId,
    int? categoryId,
    String? groupName,
    String? imageUrl,
  }) {
    return Group(
      groupId: groupId ?? this.groupId,
      categoryId: categoryId ?? this.categoryId,
      groupName: groupName ?? this.groupName,
      imageUrl: imageUrl ?? this.imageUrl,
    );
  }

  /// Create a Group from JSON
  factory Group.fromJson(Map<String, dynamic> json) {
    return Group(
      groupId: json['groupId'] as int,
      categoryId: json['categoryId'] as int,
      groupName: json['groupName'] as String,
      imageUrl: json['imageUrl'] as String,
    );
  }

  /// Convert this Group to JSON
  Map<String, dynamic> toJson() {
    return {
      'groupId': groupId,
      'categoryId': categoryId,
      'groupName': groupName,
      'imageUrl': imageUrl,
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is Group &&
        other.groupId == groupId &&
        other.categoryId == categoryId &&
        other.groupName == groupName &&
        other.imageUrl == imageUrl;
  }

  @override
  int get hashCode {
    return Object.hash(
      groupId,
      categoryId,
      groupName,
      imageUrl,
    );
  }

  @override
  String toString() {
    return 'Group(groupId: $groupId, categoryId: $categoryId, groupName: $groupName, imageUrl: $imageUrl)';
  }
}
