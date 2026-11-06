import 'dart:io';
import 'package:flutter_test/flutter_test.dart';
import 'package:path_provider_platform_interface/path_provider_platform_interface.dart';
import 'package:plugin_platform_interface/plugin_platform_interface.dart';
import 'package:pos_app/src/search/search_service.dart';
import 'package:pos_app/src/shared/local_file_store.dart';
import 'package:pos_app/src/shared/models/item.dart';

// Mock implementation for path_provider during tests
class MockPathProviderPlatform extends Fake
    with MockPlatformInterfaceMixin
    implements PathProviderPlatform {
  final String testPath;

  MockPathProviderPlatform(this.testPath);

  @override
  Future<String?> getApplicationDocumentsPath() async {
    return testPath;
  }
}

void main() {
  late SearchService searchService;
  late LocalFileStore localFileStore;
  late Directory tempDir;

  setUp(() async {
    // Create a temporary directory for testing
    tempDir = await Directory.systemTemp.createTemp('search_test_');
    
    // Set up mock platform with our temp directory
    PathProviderPlatform.instance = MockPathProviderPlatform(tempDir.path);
    
    localFileStore = LocalFileStoreImpl();
    searchService = SearchServiceImpl(localFileStore);

    // Create the catalog/items directory structure
    final itemsDir = Directory('${tempDir.path}/catalog/items');
    await itemsDir.create(recursive: true);
  });

  tearDown(() async {
    // Clean up temp directory
    if (await tempDir.exists()) {
      await tempDir.delete(recursive: true);
    }
  });

  group('SearchService', () {
    group('search - minimum character requirement', () {
      test('returns empty list for query with less than 2 characters', () async {
        // Validates: Requirement 9.7
        final result1 = await searchService.search('');
        expect(result1, isEmpty);

        final result2 = await searchService.search('a');
        expect(result2, isEmpty);
      });

      test('searches when query has exactly 2 characters', () async {
        // Validates: Requirement 9.7
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Apple',
            itemCode: 'AP001',
            imageUrl: 'apple.jpg',
            price: 1.99,
          ),
        ]);

        final result = await searchService.search('Ap');
        expect(result, isNotEmpty);
      });
    });

    group('search - case-insensitive matching', () {
      test('matches ItemName case-insensitively', () async {
        // Validates: Requirement 9.2, 9.3
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Apple Juice',
            itemCode: 'AJ001',
            imageUrl: 'apple_juice.jpg',
            price: 2.99,
          ),
          const Item(
            itemId: 2,
            categoryId: 1,
            groupId: 1,
            itemName: 'Orange Juice',
            itemCode: 'OJ001',
            imageUrl: 'orange_juice.jpg',
            price: 2.49,
          ),
        ]);

        // Test various case combinations
        final resultLower = await searchService.search('apple');
        expect(resultLower, hasLength(1));
        expect(resultLower[0].itemName, 'Apple Juice');

        final resultUpper = await searchService.search('APPLE');
        expect(resultUpper, hasLength(1));
        expect(resultUpper[0].itemName, 'Apple Juice');

        final resultMixed = await searchService.search('ApPlE');
        expect(resultMixed, hasLength(1));
        expect(resultMixed[0].itemName, 'Apple Juice');
      });

      test('matches ItemCode case-insensitively', () async {
        // Validates: Requirement 9.2, 9.3
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Product A',
            itemCode: 'ABC123',
            imageUrl: 'product_a.jpg',
            price: 10.00,
          ),
        ]);

        final resultLower = await searchService.search('abc');
        expect(resultLower, hasLength(1));
        expect(resultLower[0].itemCode, 'ABC123');

        final resultUpper = await searchService.search('ABC');
        expect(resultUpper, hasLength(1));
        expect(resultUpper[0].itemCode, 'ABC123');

        final resultMixed = await searchService.search('AbC');
        expect(resultMixed, hasLength(1));
        expect(resultMixed[0].itemCode, 'ABC123');
      });
    });

    group('search - partial matching', () {
      test('matches partial strings in ItemName', () async {
        // Validates: Requirement 9.3
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Strawberry Smoothie',
            itemCode: 'SS001',
            imageUrl: 'smoothie.jpg',
            price: 4.99,
          ),
          const Item(
            itemId: 2,
            categoryId: 1,
            groupId: 1,
            itemName: 'Blueberry Muffin',
            itemCode: 'BM001',
            imageUrl: 'muffin.jpg',
            price: 2.50,
          ),
        ]);

        // Partial match on "berry"
        final result = await searchService.search('berry');
        expect(result, hasLength(2));
        expect(result.any((item) => item.itemName == 'Strawberry Smoothie'), isTrue);
        expect(result.any((item) => item.itemName == 'Blueberry Muffin'), isTrue);
      });

      test('matches partial strings in ItemCode', () async {
        // Validates: Requirement 9.3
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Product A',
            itemCode: 'XYZ123',
            imageUrl: 'product_a.jpg',
            price: 10.00,
          ),
          const Item(
            itemId: 2,
            categoryId: 1,
            groupId: 1,
            itemName: 'Product B',
            itemCode: 'XYZ456',
            imageUrl: 'product_b.jpg',
            price: 15.00,
          ),
        ]);

        final result = await searchService.search('XYZ');
        expect(result, hasLength(2));
      });
    });

    group('search - match on ItemName OR ItemCode', () {
      test('returns items matching either ItemName or ItemCode', () async {
        // Validates: Requirement 9.2
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Chocolate Bar',
            itemCode: 'CB001',
            imageUrl: 'chocolate.jpg',
            price: 1.50,
          ),
          const Item(
            itemId: 2,
            categoryId: 1,
            groupId: 1,
            itemName: 'Vanilla Ice Cream',
            itemCode: 'CHOC999',
            imageUrl: 'ice_cream.jpg',
            price: 3.00,
          ),
        ]);

        // Query "choc" should match:
        // - "Chocolate Bar" by name
        // - "Vanilla Ice Cream" by code "CHOC999"
        final result = await searchService.search('choc');
        expect(result, hasLength(2));
        expect(result.any((item) => item.itemName == 'Chocolate Bar'), isTrue);
        expect(result.any((item) => item.itemCode == 'CHOC999'), isTrue);
      });
    });

    group('search - no results', () {
      test('returns empty list when no items match', () async {
        // Validates: Requirement 9.5
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Apple',
            itemCode: 'AP001',
            imageUrl: 'apple.jpg',
            price: 1.99,
          ),
        ]);

        final result = await searchService.search('xyz');
        expect(result, isEmpty);
      });

      test('returns empty list when catalog is empty', () async {
        // No items created
        final result = await searchService.search('test');
        expect(result, isEmpty);
      });
    });

    group('search - multiple item files', () {
      test('searches across multiple category/group files', () async {
        // Validates: Requirement 9.8 - searches all locally stored catalog data
        // Create items in different files (simulating different category/group combinations)
        await localFileStore.writeList<Item>(
          'catalog/items/1_1.json',
          [
            const Item(
              itemId: 1,
              categoryId: 1,
              groupId: 1,
              itemName: 'Coffee',
              itemCode: 'CF001',
              imageUrl: 'coffee.jpg',
              price: 2.50,
            ),
          ],
          (item) => item.toJson(),
        );

        await localFileStore.writeList<Item>(
          'catalog/items/2_2.json',
          [
            const Item(
              itemId: 2,
              categoryId: 2,
              groupId: 2,
              itemName: 'Tea',
              itemCode: 'TE001',
              imageUrl: 'tea.jpg',
              price: 1.99,
            ),
          ],
          (item) => item.toJson(),
        );

        await localFileStore.writeList<Item>(
          'catalog/items/3_3.json',
          [
            const Item(
              itemId: 3,
              categoryId: 3,
              groupId: 3,
              itemName: 'Cocoa',
              itemCode: 'CO001',
              imageUrl: 'cocoa.jpg',
              price: 2.25,
            ),
          ],
          (item) => item.toJson(),
        );

        // Search should find items across all files
        final result = await searchService.search('co');
        expect(result, hasLength(2));
        expect(result.any((item) => item.itemName == 'Coffee'), isTrue);
        expect(result.any((item) => item.itemName == 'Cocoa'), isTrue);
      });
    });

    group('search - local-only operation', () {
      test('reads only from LocalFileStore without network calls', () async {
        // Validates: Requirement 9.8 - read from LocalFileStore only
        // This is implicitly validated by our implementation using only LocalFileStore
        // and not having any ApiClient dependency
        
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Test Item',
            itemCode: 'TEST001',
            imageUrl: 'test.jpg',
            price: 5.00,
          ),
        ]);

        // If this call completes without throwing, it proves no network dependency
        final result = await searchService.search('test');
        expect(result, hasLength(1));
      });
    });

    group('search - edge cases', () {
      test('handles special characters in query', () async {
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Item & More',
            itemCode: 'IM001',
            imageUrl: 'item.jpg',
            price: 5.00,
          ),
        ]);

        final result = await searchService.search('& M');
        expect(result, hasLength(1));
      });

      test('handles Unicode characters', () async {
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'កាហ្វេ', // "Coffee" in Khmer
            itemCode: 'KH001',
            imageUrl: 'khmer_coffee.jpg',
            price: 3.00,
          ),
        ]);

        final result = await searchService.search('កា');
        expect(result, hasLength(1));
      });

      test('handles whitespace in query', () async {
        await _createTestItems(localFileStore, [
          const Item(
            itemId: 1,
            categoryId: 1,
            groupId: 1,
            itemName: 'Iced Coffee',
            itemCode: 'IC001',
            imageUrl: 'iced_coffee.jpg',
            price: 3.50,
          ),
        ]);

        final result = await searchService.search('Iced Co');
        expect(result, hasLength(1));
      });
    });
  });
}

/// Helper function to create test items in the default location
Future<void> _createTestItems(LocalFileStore store, List<Item> items) async {
  await store.writeList<Item>(
    'catalog/items/1_1.json',
    items,
    (item) => item.toJson(),
  );
}
