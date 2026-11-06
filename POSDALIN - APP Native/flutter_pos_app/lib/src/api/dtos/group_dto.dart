/// Data Transfer Object for Group from ASMX service
class GroupDto {
  final int groupId;
  final int categoryId;
  final String groupName;
  final String imageUrl;

  GroupDto({
    required this.groupId,
    required this.categoryId,
    required this.groupName,
    required this.imageUrl,
  });

  factory GroupDto.fromJson(Map<String, dynamic> json) {
    return GroupDto(
      groupId: json['GroupID'] as int,
      categoryId: json['CategoryID'] as int,
      groupName: json['GroupName'] as String,
      imageUrl: json['ImageUrl'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'GroupID': groupId,
      'CategoryID': categoryId,
      'GroupName': groupName,
      'ImageUrl': imageUrl,
    };
  }
}
