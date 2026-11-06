import 'dart:io';
import 'package:path_provider/path_provider.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../shared/local_file_store.dart';
import '../shared/models/item.dart';

part 'search_service.g.dart';

/// Service for searching items in the local catalog.
/// 
/// Performs client-side search against locally stored catalog data
/// for instant results without network dependency.
abstract class SearchService {
  /// Search for items matching the query string.
  /// 
  /// - Requires minimum 2 characters
  /// - Case-insensitive partial match on ItemName and ItemCode
  /// - Reads from LocalFileStore only (no network call)
  /// 
  /// **Validates: Requirements 9.1, 9.2, 9.3, 9.7, 9.8**
  Future<List<Item>> search(String query);
}

/// Implementation of SearchService using LocalFileStore.
class SearchServiceImpl implements SearchService {
  final LocalFileStore _localFileStore;

  SearchServiceImpl(this._localFileStore);

  @override
  Future<List<Item>> search(String query) async {
    // Enforce 2-character minimum (Requirement 9.7)
    if (query.length < 2) {
      return [];
    }

    // Normalize query to lowercase for case-insensitive matching
    final normalizedQuery = query.toLowerCase();

    // Get all item files from the catalog/items directory
    final allItems = await _getAllLocalItems();

    // Filter items where ItemName or ItemCode contains the query (case-insensitive)
    // Requirement 9.2, 9.3: case-insensitive partial match
    final matchingItems = allItems.where((item) {
      final nameMatches = item.itemName.toLowerCase().contains(normalizedQuery);
      final codeMatches = item.itemCode.toLowerCase().contains(normalizedQuery);
      return nameMatches || codeMatches;
    }).toList();

    return matchingItems;
  }

  /// Read all items from all local item files.
  /// 
  /// Items are stored in files like: catalog/items/<category_id>_<group_id>.json
  Future<List<Item>> _getAllLocalItems() async {
    try {
      final appDir = await getApplicationDocumentsDirectory();
      final itemsDir = Directory('${appDir.path}/catalog/items');

      // If directory doesn't exist, return empty list
      if (!await itemsDir.exists()) {
        return [];
      }

      final allItems = <Item>[];

      // List all JSON files in the items directory
      await for (final entity in itemsDir.list()) {
        if (entity is File && entity.path.endsWith('.json')) {
          // Extract relative path for LocalFileStore
          final relativePath = entity.path.substring(appDir.path.length + 1);
          
          // Read items from this file
          final items = await _localFileStore.readList<Item>(
            relativePath,
            Item.fromJson,
          );
          
          allItems.addAll(items);
        }
      }

      return allItems;
    } catch (e) {
      // On any error, return empty list
      return [];
    }
  }
}

/// Riverpod provider for SearchService.
/// 
/// **Validates: Requirements 9.1, 9.2, 9.3, 9.7, 9.8**
@riverpod
SearchService searchService(SearchServiceRef ref) {
  final localFileStore = ref.watch(localFileStoreProvider);
  return SearchServiceImpl(localFileStore);
}
