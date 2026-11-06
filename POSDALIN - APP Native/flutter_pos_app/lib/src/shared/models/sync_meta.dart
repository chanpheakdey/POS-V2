/// Domain model for tracking synchronization metadata
class SyncMeta {
  final DateTime? categoriesLastSync;
  final DateTime? groupsLastSync;
  final DateTime? itemsLastSync;
  final DateTime? imagesLastSync;

  const SyncMeta({
    this.categoriesLastSync,
    this.groupsLastSync,
    this.itemsLastSync,
    this.imagesLastSync,
  });

  /// Create a copy of this SyncMeta with the given fields replaced
  SyncMeta copyWith({
    DateTime? categoriesLastSync,
    DateTime? groupsLastSync,
    DateTime? itemsLastSync,
    DateTime? imagesLastSync,
  }) {
    return SyncMeta(
      categoriesLastSync: categoriesLastSync ?? this.categoriesLastSync,
      groupsLastSync: groupsLastSync ?? this.groupsLastSync,
      itemsLastSync: itemsLastSync ?? this.itemsLastSync,
      imagesLastSync: imagesLastSync ?? this.imagesLastSync,
    );
  }

  /// Create a SyncMeta from JSON
  factory SyncMeta.fromJson(Map<String, dynamic> json) {
    return SyncMeta(
      categoriesLastSync: json['categoriesLastSync'] != null
          ? DateTime.parse(json['categoriesLastSync'] as String)
          : null,
      groupsLastSync: json['groupsLastSync'] != null
          ? DateTime.parse(json['groupsLastSync'] as String)
          : null,
      itemsLastSync: json['itemsLastSync'] != null
          ? DateTime.parse(json['itemsLastSync'] as String)
          : null,
      imagesLastSync: json['imagesLastSync'] != null
          ? DateTime.parse(json['imagesLastSync'] as String)
          : null,
    );
  }

  /// Convert this SyncMeta to JSON
  Map<String, dynamic> toJson() {
    return {
      'categoriesLastSync': categoriesLastSync?.toIso8601String(),
      'groupsLastSync': groupsLastSync?.toIso8601String(),
      'itemsLastSync': itemsLastSync?.toIso8601String(),
      'imagesLastSync': imagesLastSync?.toIso8601String(),
    };
  }

  @override
  bool operator ==(Object other) {
    if (identical(this, other)) return true;
    return other is SyncMeta &&
        other.categoriesLastSync == categoriesLastSync &&
        other.groupsLastSync == groupsLastSync &&
        other.itemsLastSync == itemsLastSync &&
        other.imagesLastSync == imagesLastSync;
  }

  @override
  int get hashCode {
    return Object.hash(
      categoriesLastSync,
      groupsLastSync,
      itemsLastSync,
      imagesLastSync,
    );
  }

  @override
  String toString() {
    return 'SyncMeta(categoriesLastSync: $categoriesLastSync, groupsLastSync: $groupsLastSync, itemsLastSync: $itemsLastSync, imagesLastSync: $imagesLastSync)';
  }
}
