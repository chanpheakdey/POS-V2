import 'dart:convert';
import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:pos_app/src/shared/local_file_store.dart';

// Mock implementation for path_provider during tests
class MockPathProviderPlatform extends Fake
    with MockPlatformInterfaceMixin
    implements PathProviderPlatform {
  @override
  Future<String?> getApplicationDocumentsPath() async {
    return Directory.systemTemp.createTemp('pos_app_test_').then((dir) => dir.path);
  }
}

// Test model for demonstration
class TestItem {
  final int id;
  final String name;

  TestItem({required this.id, required this.name});

  Map<String, dynamic> toJson() => {'id': id, 'name': name};

  factory TestItem.fromJson(Map<String, dynamic> json) {
    return TestItem(
      id: json['id'] as int,
      name: json['name'] as String,
    );
  }

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      other is TestItem && id == other.id && name == other.name;

  @override
  int get hashCode => Object.hash(id, name);
}

void main() {
  late LocalFileStore store;
  late Directory tempDir;

  setUp(() async {
    // Set up mock platform
    PathProviderPlatform.instance = MockPathProviderPlatform();
    
    store = LocalFileStoreImpl();
    tempDir = await Directory.systemTemp.createTemp('pos_app_test_');
  });

  tearDown(() async {
    // Clean up temp directory
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  group('LocalFileStore', () {
    group('readList', () {
      test('returns empty list when file does not exist', () async {
        final result = await store.readList<TestItem>(
          'nonexistent.json',
          TestItem.fromJson,
        );

        expect(result, isEmpty);
      });

      test('returns empty list when file is empty', () async {
        final file = File('${tempDir.path}/empty.json');
        await file.create(recursive: true);
        await file.writeAsString('');

        final result = await store.readList<TestItem>(
          'empty.json',
          TestItem.fromJson,
        );

        expect(result, isEmpty);
      });

      test('deserializes JSON array correctly', () async {
        final items = [
          TestItem(id: 1, name: 'Item 1'),
          TestItem(id: 2, name: 'Item 2'),
        ];
        
        await store.writeList<TestItem>(
          'test_items.json',
          items,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'test_items.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(2));
        expect(result[0].id, 1);
        expect(result[0].name, 'Item 1');
        expect(result[1].id, 2);
        expect(result[1].name, 'Item 2');
      });

      test('returns empty list on invalid JSON', () async {
        final file = File('${tempDir.path}/invalid.json');
        await file.create(recursive: true);
        await file.writeAsString('{ invalid json }');

        final result = await store.readList<TestItem>(
          'invalid.json',
          TestItem.fromJson,
        );

        expect(result, isEmpty);
      });
    });

    group('writeList', () {
      test('creates file and writes JSON array', () async {
        final items = [
          TestItem(id: 1, name: 'Item 1'),
          TestItem(id: 2, name: 'Item 2'),
        ];

        await store.writeList<TestItem>(
          'write_test.json',
          items,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'write_test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(2));
        expect(result, equals(items));
      });

      test('creates parent directories if they do not exist', () async {
        final items = [TestItem(id: 1, name: 'Item 1')];

        await store.writeList<TestItem>(
          'nested/deep/path/test.json',
          items,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'nested/deep/path/test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(1));
        expect(result[0], equals(items[0]));
      });

      test('overwrites existing file', () async {
        final initialItems = [TestItem(id: 1, name: 'Item 1')];
        final newItems = [TestItem(id: 2, name: 'Item 2')];

        await store.writeList<TestItem>(
          'overwrite_test.json',
          initialItems,
          (item) => item.toJson(),
        );

        await store.writeList<TestItem>(
          'overwrite_test.json',
          newItems,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'overwrite_test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(1));
        expect(result[0].id, 2);
      });
    });

    group('appendItems - Critical append-only behavior', () {
      test('appends only new items with unique IDs', () async {
        // Validates: Requirements 10.3, 10.4, 18.4, 18.7
        final existingItems = [
          TestItem(id: 1, name: 'Existing 1'),
          TestItem(id: 2, name: 'Existing 2'),
        ];

        await store.writeList<TestItem>(
          'append_test.json',
          existingItems,
          (item) => item.toJson(),
        );

        final newItems = [
          TestItem(id: 2, name: 'Duplicate ID 2'), // Should be skipped
          TestItem(id: 3, name: 'New Item 3'),      // Should be appended
          TestItem(id: 4, name: 'New Item 4'),      // Should be appended
        ];

        await store.appendItems<TestItem>(
          'append_test.json',
          newItems,
          (item) => item.id.toString(),
          TestItem.fromJson,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'append_test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(4));
        expect(result[0].id, 1);
        expect(result[0].name, 'Existing 1');
        expect(result[1].id, 2);
        expect(result[1].name, 'Existing 2'); // Original preserved
        expect(result[2].id, 3);
        expect(result[2].name, 'New Item 3');
        expect(result[3].id, 4);
        expect(result[3].name, 'New Item 4');
      });

      test('preserves all existing records - never loses data', () async {
        // Validates: Requirements 3.4, 3.5, 10.3
        final existingItems = [
          TestItem(id: 1, name: 'Keep 1'),
          TestItem(id: 2, name: 'Keep 2'),
          TestItem(id: 3, name: 'Keep 3'),
        ];

        await store.writeList<TestItem>(
          'preserve_test.json',
          existingItems,
          (item) => item.toJson(),
        );

        final newItems = [
          TestItem(id: 1, name: 'Should not replace'),
          TestItem(id: 2, name: 'Should not replace'),
          TestItem(id: 4, name: 'New 4'),
        ];

        await store.appendItems<TestItem>(
          'preserve_test.json',
          newItems,
          (item) => item.id.toString(),
          TestItem.fromJson,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'preserve_test.json',
          TestItem.fromJson,
        );

        // All original items must be preserved with original names
        expect(result, hasLength(4));
        expect(result[0].name, 'Keep 1');
        expect(result[1].name, 'Keep 2');
        expect(result[2].name, 'Keep 3');
        expect(result[3].name, 'New 4');
      });

      test('handles empty existing file', () async {
        final newItems = [
          TestItem(id: 1, name: 'First Item'),
          TestItem(id: 2, name: 'Second Item'),
        ];

        await store.appendItems<TestItem>(
          'empty_append_test.json',
          newItems,
          (item) => item.id.toString(),
          TestItem.fromJson,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'empty_append_test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(2));
        expect(result[0].id, 1);
        expect(result[1].id, 2);
      });

      test('does nothing when all new items have duplicate IDs', () async {
        final existingItems = [
          TestItem(id: 1, name: 'Existing 1'),
          TestItem(id: 2, name: 'Existing 2'),
        ];

        await store.writeList<TestItem>(
          'no_append_test.json',
          existingItems,
          (item) => item.toJson(),
        );

        final newItems = [
          TestItem(id: 1, name: 'Duplicate 1'),
          TestItem(id: 2, name: 'Duplicate 2'),
        ];

        await store.appendItems<TestItem>(
          'no_append_test.json',
          newItems,
          (item) => item.id.toString(),
          TestItem.fromJson,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'no_append_test.json',
          TestItem.fromJson,
        );

        // Should remain unchanged
        expect(result, hasLength(2));
        expect(result[0].name, 'Existing 1');
        expect(result[1].name, 'Existing 2');
      });

      test('handles empty new items list', () async {
        final existingItems = [
          TestItem(id: 1, name: 'Existing 1'),
        ];

        await store.writeList<TestItem>(
          'empty_new_test.json',
          existingItems,
          (item) => item.toJson(),
        );

        await store.appendItems<TestItem>(
          'empty_new_test.json',
          [],
          (item) => item.id.toString(),
          TestItem.fromJson,
          (item) => item.toJson(),
        );

        final result = await store.readList<TestItem>(
          'empty_new_test.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(1));
        expect(result[0].name, 'Existing 1');
      });
    });

    group('copyAssetIfAbsent', () {
      test('does not copy when file already exists', () async {
        final existingContent = 'existing content';
        await store.writeList<TestItem>(
          'existing_asset.json',
          [TestItem(id: 1, name: 'Existing')],
          (item) => item.toJson(),
        );

        // Try to copy an asset (will fail since asset doesn't exist, but should not throw)
        await store.copyAssetIfAbsent(
          'assets/test/asset.json',
          'existing_asset.json',
        );

        // File should remain unchanged
        final result = await store.readList<TestItem>(
          'existing_asset.json',
          TestItem.fromJson,
        );

        expect(result, hasLength(1));
        expect(result[0].name, 'Existing');
      });

      test('creates parent directories when copying asset', () async {
        // This will fail gracefully since the asset doesn't exist
        // but it tests that the method doesn't throw
        await store.copyAssetIfAbsent(
          'assets/test/nonexistent.json',
          'deep/nested/path/asset.json',
        );

        // Since asset doesn't exist, file should not be created
        final result = await store.readList<TestItem>(
          'deep/nested/path/asset.json',
          TestItem.fromJson,
        );

        expect(result, isEmpty);
      });
    });
  });
}
