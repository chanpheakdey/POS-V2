import 'dart:convert';
import 'dart:io';
import 'package:flutter/services.dart' show rootBundle;
import 'package:path_provider/path_provider.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'local_file_store.g.dart';

/// Abstracts JSON file I/O operations for local catalog data storage.
/// 
/// Provides methods for reading, writing, and appending JSON data with
/// an append-only strategy that preserves existing records while adding new ones.
abstract class LocalFileStore {
  /// Read a JSON file and deserialize into a list of objects.
  /// 
  /// Returns an empty list if the file doesn't exist or is invalid.
  Future<List<T>> readList<T>(
    String path,
    T Function(Map<String, dynamic>) fromJson,
  );

  /// Serialize a list of objects and write to a JSON file.
  /// 
  /// Overwrites any existing file at the given path.
  Future<void> writeList<T>(
    String path,
    List<T> items,
    Map<String, dynamic> Function(T) toJson,
  );

  /// Read existing records, diff by ID key, and append only new items.
  /// 
  /// This is the critical append-only method that never overwrites existing entries.
  /// 
  /// - Reads existing records from the file
  /// - Identifies which incoming items have IDs not present in existing data
  /// - Appends only the new items to the file
  /// 
  /// **Validates: Requirements 10.3, 10.4, 18.4, 18.7**
  Future<void> appendItems<T>(
    String path,
    List<T> newItems,
    String Function(T) idKey,
    T Function(Map<String, dynamic>) fromJson,
    Map<String, dynamic> Function(T) toJson,
  );

  /// Copy a bundled asset file to local storage if it doesn't already exist.
  /// 
  /// Used to initialize local storage with pre-bundled default data on first launch.
  Future<void> copyAssetIfAbsent(String assetPath, String localPath);
}

/// Implementation of LocalFileStore using the device's file system.
class LocalFileStoreImpl implements LocalFileStore {
  LocalFileStoreImpl();

  /// Get the base directory for storing local files.
  Future<Directory> get _baseDirectory async {
    final appDir = await getApplicationDocumentsDirectory();
    return appDir;
  }

  /// Resolve a relative path to an absolute path in the app documents directory.
  Future<String> _resolvePath(String relativePath) async {
    final dir = await _baseDirectory;
    return '${dir.path}/$relativePath';
  }

  @override
  Future<List<T>> readList<T>(
    String path,
    T Function(Map<String, dynamic>) fromJson,
  ) async {
    try {
      final absolutePath = await _resolvePath(path);
      final file = File(absolutePath);

      if (!await file.exists()) {
        return [];
      }

      final contents = await file.readAsString();
      if (contents.trim().isEmpty) {
        return [];
      }

      final jsonData = jsonDecode(contents);
      if (jsonData is! List) {
        return [];
      }

      return jsonData
          .map((item) => fromJson(item as Map<String, dynamic>))
          .toList();
    } catch (e) {
      // On any error (parse error, I/O error), return empty list
      return [];
    }
  }

  @override
  Future<void> writeList<T>(
    String path,
    List<T> items,
    Map<String, dynamic> Function(T) toJson,
  ) async {
    final absolutePath = await _resolvePath(path);
    final file = File(absolutePath);

    // Ensure parent directory exists
    await file.parent.create(recursive: true);

    final jsonData = items.map(toJson).toList();
    final jsonString = jsonEncode(jsonData);

    await file.writeAsString(jsonString);
  }

  @override
  Future<void> appendItems<T>(
    String path,
    List<T> newItems,
    String Function(T) idKey,
    T Function(Map<String, dynamic>) fromJson,
    Map<String, dynamic> Function(T) toJson,
  ) async {
    // Read existing records
    final existingItems = await readList<T>(path, fromJson);

    // Build a set of existing IDs for fast lookup
    final existingIds = existingItems.map(idKey).toSet();

    // Filter new items to only those with IDs not already present
    final itemsToAppend = newItems.where((item) {
      final id = idKey(item);
      return !existingIds.contains(id);
    }).toList();

    // If there are no new items to append, we're done
    if (itemsToAppend.isEmpty) {
      return;
    }

    // Combine existing items with new items and write back
    final combinedItems = [...existingItems, ...itemsToAppend];
    await writeList<T>(path, combinedItems, toJson);
  }

  @override
  Future<void> copyAssetIfAbsent(String assetPath, String localPath) async {
    final absolutePath = await _resolvePath(localPath);
    final file = File(absolutePath);

    // Only copy if the file doesn't already exist
    if (await file.exists()) {
      return;
    }

    // Ensure parent directory exists
    await file.parent.create(recursive: true);

    try {
      // Load asset data
      final assetData = await rootBundle.load(assetPath);
      final bytes = assetData.buffer.asUint8List();

      // Write to local file
      await file.writeAsBytes(bytes);
    } catch (e) {
      // If asset doesn't exist or can't be loaded, silently skip
      // This allows the app to function even if some assets are missing
      return;
    }
  }
}

/// Riverpod provider for LocalFileStore.
/// 
/// **Validates: Requirements 10.3, 10.4, 18.4, 18.7**
@riverpod
LocalFileStore localFileStore(LocalFileStoreRef ref) {
  return LocalFileStoreImpl();
}
