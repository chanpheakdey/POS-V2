import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_pos_app/src/api/api.dart';
import 'package:flutter_pos_app/src/catalog/catalog.dart';
import 'package:flutter_pos_app/src/shared/local_file_store.dart';
import 'package:flutter_pos_app/src/shared/models/models.dart';

/// Mock implementation of LocalFileStore for testing
class MockLocalFileStore implements LocalFileStore {
  final Map<String, List<dynamic>> _storage = {};
  final Set<String> _copiedAssets = {};

  @override
  Future<List<T>> readList<T>(
    String path,
    T Function(Map<String, dynamic>) fromJson,
  ) async {
    if (!_storage.containsKey(path)) {
      return [];
    }

    final data = _storage[path]!;
    return data.map((item) => fromJson(item as Map<String, dynamic>)).toList();
  }

  @override
  Future<void> writeList<T>(
    String path,
    List<T> items,
    Map<String, dynamic> Function(T) toJson,
  ) async {
    _storage[path] = items.map(toJson).toList();
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
    if (!_copiedAssets.contains(localPath)) {
      _copiedAssets.add(localPath);
    }
  }

  /// Helper method to seed storage with data
  void seedData<T>(
    String path,
    List<T> items,
    Map<String, dynamic> Function(T) toJson,
  ) {
    _storage[path] = items.map(toJson).toList();
  }

  /// Helper method to check if asset was copied
  bool wasAssetCopied(String localPath) {
    return _copiedAssets.contains(localPath);
  }
}

/// Mock implementation of ApiClient for testing
class MockApiClient implements ApiClient {
  final Map<String, dynamic> _responses = {};
  bool _shouldThrowError = false;

  void setResponse<T>(String key, T response) {
    _responses[key] = response;
  }

  void throwError() {
    _shouldThrowError = true;
  }

  void reset() {
    _responses.clear();
    _shouldThrowError = false;
  }

  @override
  Future<List<CategoryDto>> getCategories() async {
    if (_shouldThrowError) {
      throw Exception('Network error');
    }
    return (_responses['categories'] as List<CategoryDto>?) ?? [];
  }

  @override
  Future<List<GroupDto>> getGroups(int categoryId) async {
    if (_shouldThrowError) {
      throw Exception('Network error');
    }
    return (_responses['groups_$categoryId'] as List<GroupDto>?) ?? [];
  }

  @override
  Future<List<ItemDto>> getSaleItems(int categoryId, int groupId) async {
    if (_shouldThrowError) {
      throw Exception('Network error');
    }
    return (_responses['items_${categoryId}_$groupId'] as List<ItemDto>?) ?? [];
  }

  @override
  Future<ItemDetailDto> getItemDetail(int itemId) async {
    if (_shouldThrowError) {
      throw Exception('Network error');
    }
    return _responses['item_detail_$itemId'] as ItemDetailDto? ??
        ItemDetailDto(
          itemId: itemId,
          itemName: 'Unknown Item',
          itemCode: 'UNKNOWN',
          imageUrl: '',
        );
  }

  @override
  Future<List<PriceTierDto>> getItemUnits(int itemId) async {
    if (_shouldThrowError) {
      throw Exception('Network error');
    }
    return (_responses['price_tiers_$itemId'] as List<PriceTierDto>?) ?? [];
  }

  // Unused methods for this test
  @override
  Future<String> checkToken(String token) async => '';

  @override
  Future<String> expireToken(String token) async => '';

  @override
  Future<String> getImageUrl(String imagePath, int width, int height) async =>
      '';

  @override
  Future<List<OrderDto>> getOrders(int customerId) async => [];

  @override
  Future<String> login(String username, String password) async => '';

  @override
  Future<OrderResultDto> submitOrder(OrderPayloadDto payload) async =>
      OrderResultDto(orderId: 0, invoiceNo: '', success: false, message: '');
}

void main() {
  group('CatalogService', () {
    late MockLocalFileStore mockLocalFileStore;
    late MockApiClient mockApiClient;
    late CatalogService catalogService;

    setUp(() {
      mockLocalFileStore = MockLocalFileStore();
      mockApiClient = MockApiClient();
      catalogService = CatalogServiceImpl(
        localFileStore: mockLocalFileStore,
        apiClient: mockApiClient,
      );
    });

    group('getCategories', () {
      test('copies asset if absent on first call', () async {
        // Arrange
        mockApiClient.setResponse('categories', <CategoryDto>[]);

        // Act
        await catalogService.getCategories();

        // Assert
        expect(
          mockLocalFileStore.wasAssetCopied('catalog/categories.json'),
          isTrue,
        );
      });

      test('returns local data immediately if available', () async {
        // Arrange - seed local storage with categories
        final localCategories = [
          Category(
            categoryId: 1,
            categoryName: 'Local Category 1',
            imageUrl: '',
            colorIndex: 0,
          ),
          Category(
            categoryId: 2,
            categoryName: 'Local Category 2',
            imageUrl: '',
            colorIndex: 1,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/categories.json',
          localCategories,
          (c) => c.toJson(),
        );

        // Set up API to return different data
        mockApiClient.setResponse('categories', [
          CategoryDto(categoryId: 3, categoryName: 'API Category 3', imageUrl: ''),
        ]);

        // Act
        final result = await catalogService.getCategories();

        // Assert - should return local data immediately
        expect(result.length, equals(2));
        expect(result[0].categoryId, equals(1));
        expect(result[1].categoryId, equals(2));
      });

      test('fetches from API when local storage is empty', () async {
        // Arrange - no local data
        mockApiClient.setResponse('categories', [
          CategoryDto(categoryId: 1, categoryName: 'API Category 1', imageUrl: ''),
          CategoryDto(categoryId: 2, categoryName: 'API Category 2', imageUrl: ''),
        ]);

        // Act
        final result = await catalogService.getCategories();

        // Assert - should fetch from API
        expect(result.length, equals(2));
        expect(result[0].categoryName, equals('API Category 1'));
        expect(result[1].categoryName, equals('API Category 2'));
      });

      test('appends only new categories to local storage', () async {
        // Arrange - seed with existing category
        final existingCategories = [
          Category(
            categoryId: 1,
            categoryName: 'Existing Category',
            imageUrl: '',
            colorIndex: 0,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/categories.json',
          existingCategories,
          (c) => c.toJson(),
        );

        // API returns mix of existing and new categories
        mockApiClient.setResponse('categories', [
          CategoryDto(categoryId: 1, categoryName: 'Existing Category', imageUrl: ''),
          CategoryDto(categoryId: 2, categoryName: 'New Category', imageUrl: ''),
        ]);

        // Act
        final result = await catalogService.getCategories();

        // Wait a bit for background fetch to complete
        await Future.delayed(Duration(milliseconds: 100));

        // Read from storage to verify append behavior
        final storedCategories = await mockLocalFileStore.readList<Category>(
          'catalog/categories.json',
          (json) => Category.fromJson(json),
        );

        // Assert - should have 2 categories (1 existing + 1 new)
        expect(storedCategories.length, equals(2));
        expect(storedCategories.any((c) => c.categoryId == 1), isTrue);
        expect(storedCategories.any((c) => c.categoryId == 2), isTrue);
      });

      test('assigns color index cycling through 21 colors', () async {
        // Arrange - API returns multiple categories
        final categories = List.generate(
          25,
          (i) => CategoryDto(
            categoryId: i + 1,
            categoryName: 'Category ${i + 1}',
            imageUrl: '',
          ),
        );
        mockApiClient.setResponse('categories', categories);

        // Act
        final result = await catalogService.getCategories();

        // Assert - verify color indices cycle through 0-20
        expect(result[0].colorIndex, equals(0));
        expect(result[20].colorIndex, equals(20));
        expect(result[21].colorIndex, equals(0)); // Should cycle back
        expect(result[22].colorIndex, equals(1));
      });

      test('returns local data on API error', () async {
        // Arrange - seed local storage
        final localCategories = [
          Category(
            categoryId: 1,
            categoryName: 'Cached Category',
            imageUrl: '',
            colorIndex: 0,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/categories.json',
          localCategories,
          (c) => c.toJson(),
        );

        // Make API throw error
        mockApiClient.throwError();

        // Act
        final result = await catalogService.getCategories();

        // Assert - should return cached data
        expect(result.length, equals(1));
        expect(result[0].categoryName, equals('Cached Category'));
      });

      test('returns empty list when no local data and API fails', () async {
        // Arrange - no local data, API throws error
        mockApiClient.throwError();

        // Act
        final result = await catalogService.getCategories();

        // Assert
        expect(result, isEmpty);
      });
    });

    group('getGroups', () {
      test('copies asset if absent on first call', () async {
        // Arrange
        const categoryId = 5;
        mockApiClient.setResponse('groups_$categoryId', <GroupDto>[]);

        // Act
        await catalogService.getGroups(categoryId);

        // Assert
        expect(
          mockLocalFileStore.wasAssetCopied('catalog/groups/$categoryId.json'),
          isTrue,
        );
      });

      test('returns local data immediately if available', () async {
        // Arrange
        const categoryId = 10;
        final localGroups = [
          Group(
            groupId: 1,
            categoryId: categoryId,
            groupName: 'Local Group',
            imageUrl: '',
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/groups/$categoryId.json',
          localGroups,
          (g) => g.toJson(),
        );

        mockApiClient.setResponse('groups_$categoryId', [
          GroupDto(
            groupId: 2,
            categoryId: categoryId,
            groupName: 'API Group',
            imageUrl: '',
          ),
        ]);

        // Act
        final result = await catalogService.getGroups(categoryId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].groupName, equals('Local Group'));
      });

      test('fetches from API when local storage is empty', () async {
        // Arrange
        const categoryId = 15;
        mockApiClient.setResponse('groups_$categoryId', [
          GroupDto(
            groupId: 1,
            categoryId: categoryId,
            groupName: 'API Group',
            imageUrl: '',
          ),
        ]);

        // Act
        final result = await catalogService.getGroups(categoryId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].groupName, equals('API Group'));
      });

      test('appends only new groups to local storage', () async {
        // Arrange
        const categoryId = 20;
        final existingGroups = [
          Group(
            groupId: 1,
            categoryId: categoryId,
            groupName: 'Existing Group',
            imageUrl: '',
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/groups/$categoryId.json',
          existingGroups,
          (g) => g.toJson(),
        );

        mockApiClient.setResponse('groups_$categoryId', [
          GroupDto(
            groupId: 1,
            categoryId: categoryId,
            groupName: 'Existing Group',
            imageUrl: '',
          ),
          GroupDto(
            groupId: 2,
            categoryId: categoryId,
            groupName: 'New Group',
            imageUrl: '',
          ),
        ]);

        // Act
        await catalogService.getGroups(categoryId);

        // Wait for background fetch
        await Future.delayed(Duration(milliseconds: 100));

        // Verify storage
        final storedGroups = await mockLocalFileStore.readList<Group>(
          'catalog/groups/$categoryId.json',
          (json) => Group.fromJson(json),
        );

        // Assert
        expect(storedGroups.length, equals(2));
        expect(storedGroups.any((g) => g.groupId == 2), isTrue);
      });
    });

    group('getItems', () {
      test('copies asset if absent on first call', () async {
        // Arrange
        const categoryId = 5;
        const groupId = 10;
        mockApiClient.setResponse('items_${categoryId}_$groupId', <ItemDto>[]);

        // Act
        await catalogService.getItems(categoryId, groupId);

        // Assert
        expect(
          mockLocalFileStore
              .wasAssetCopied('catalog/items/${categoryId}_$groupId.json'),
          isTrue,
        );
      });

      test('returns local data immediately if available', () async {
        // Arrange
        const categoryId = 1;
        const groupId = 1;
        final localItems = [
          Item(
            itemId: 1,
            categoryId: categoryId,
            groupId: groupId,
            itemName: 'Local Item',
            itemCode: 'LOCAL001',
            imageUrl: '',
            price: 10.0,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/items/${categoryId}_$groupId.json',
          localItems,
          (i) => i.toJson(),
        );

        // Act
        final result = await catalogService.getItems(categoryId, groupId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].itemName, equals('Local Item'));
      });

      test('fetches from API when local storage is empty', () async {
        // Arrange
        const categoryId = 2;
        const groupId = 2;
        mockApiClient.setResponse('items_${categoryId}_$groupId', [
          ItemDto(
            itemId: 1,
            categoryId: categoryId,
            groupId: groupId,
            itemName: 'API Item',
            itemCode: 'API001',
            imageUrl: '',
            price: 20.0,
          ),
        ]);

        // Act
        final result = await catalogService.getItems(categoryId, groupId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].itemName, equals('API Item'));
      });
    });

    group('getItemDetail', () {
      test('copies asset if absent on first call', () async {
        // Arrange
        const itemId = 100;
        mockApiClient.setResponse(
          'item_detail_$itemId',
          ItemDetailDto(
            itemId: itemId,
            itemName: 'Item',
            itemCode: 'ITEM001',
            imageUrl: '',
          ),
        );
        mockApiClient.setResponse('price_tiers_$itemId', <PriceTierDto>[]);

        // Act
        await catalogService.getItemDetail(itemId);

        // Assert
        expect(
          mockLocalFileStore.wasAssetCopied('catalog/item_details/$itemId.json'),
          isTrue,
        );
      });

      test('returns local data immediately if available', () async {
        // Arrange
        const itemId = 50;
        final localDetail = ItemDetail(
          itemId: itemId,
          itemName: 'Local Item Detail',
          itemCode: 'LOCAL050',
          imageUrl: '',
          priceTiers: [],
        );
        mockLocalFileStore.seedData(
          'catalog/item_details/$itemId.json',
          [localDetail],
          (d) => d.toJson(),
        );

        // Act
        final result = await catalogService.getItemDetail(itemId);

        // Assert
        expect(result.itemName, equals('Local Item Detail'));
      });

      test('fetches from API when local storage is empty', () async {
        // Arrange
        const itemId = 75;
        mockApiClient.setResponse(
          'item_detail_$itemId',
          ItemDetailDto(
            itemId: itemId,
            itemName: 'API Item Detail',
            itemCode: 'API075',
            imageUrl: '',
          ),
        );
        mockApiClient.setResponse('price_tiers_$itemId', <PriceTierDto>[]);

        // Act
        final result = await catalogService.getItemDetail(itemId);

        // Assert
        expect(result.itemName, equals('API Item Detail'));
      });

      test('throws exception when both local and API fail', () async {
        // Arrange
        const itemId = 999;
        mockApiClient.throwError();

        // Act & Assert
        expect(
          () => catalogService.getItemDetail(itemId),
          throwsException,
        );
      });
    });

    group('getPriceTiers', () {
      test('copies asset if absent on first call', () async {
        // Arrange
        const itemId = 200;
        mockApiClient.setResponse('price_tiers_$itemId', <PriceTierDto>[]);

        // Act
        await catalogService.getPriceTiers(itemId);

        // Assert
        expect(
          mockLocalFileStore.wasAssetCopied('catalog/price_tiers/$itemId.json'),
          isTrue,
        );
      });

      test('returns local data immediately if available', () async {
        // Arrange
        const itemId = 150;
        final localTiers = [
          PriceTier(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            priceUsd: 5.0,
            priceKhr: 20500.0,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/price_tiers/$itemId.json',
          localTiers,
          (t) => t.toJson(),
        );

        // Act
        final result = await catalogService.getPriceTiers(itemId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].priceUsd, equals(5.0));
      });

      test('fetches from API when local storage is empty', () async {
        // Arrange
        const itemId = 175;
        mockApiClient.setResponse('price_tiers_$itemId', [
          PriceTierDto(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            price: 7.5,
            currency: '\$',
          ),
        ]);

        // Act
        final result = await catalogService.getPriceTiers(itemId);

        // Assert
        expect(result.length, equals(1));
        expect(result[0].priceUsd, equals(7.5));
      });

      test('converts USD currency correctly', () async {
        // Arrange
        const itemId = 180;
        mockApiClient.setResponse('price_tiers_$itemId', [
          PriceTierDto(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            price: 10.0,
            currency: '\$',
          ),
        ]);

        // Act
        final result = await catalogService.getPriceTiers(itemId);

        // Assert
        expect(result[0].priceUsd, equals(10.0));
        expect(result[0].priceKhr, equals(41000.0)); // 10 * 4100
      });

      test('converts KHR currency correctly', () async {
        // Arrange
        const itemId = 185;
        mockApiClient.setResponse('price_tiers_$itemId', [
          PriceTierDto(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            price: 41000.0,
            currency: '៛',
          ),
        ]);

        // Act
        final result = await catalogService.getPriceTiers(itemId);

        // Assert
        expect(result[0].priceKhr, equals(41000.0));
        expect(result[0].priceUsd, equals(10.0)); // 41000 / 4100
      });

      test('appends only new price tiers to local storage', () async {
        // Arrange
        const itemId = 190;
        final existingTiers = [
          PriceTier(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            priceUsd: 5.0,
            priceKhr: 20500.0,
          ),
        ];
        mockLocalFileStore.seedData(
          'catalog/price_tiers/$itemId.json',
          existingTiers,
          (t) => t.toJson(),
        );

        mockApiClient.setResponse('price_tiers_$itemId', [
          PriceTierDto(
            saleId: 1,
            itemId: itemId,
            fromQty: 1,
            toQty: 10,
            price: 5.0,
            currency: '\$',
          ),
          PriceTierDto(
            saleId: 2,
            itemId: itemId,
            fromQty: 11,
            toQty: 50,
            price: 4.5,
            currency: '\$',
          ),
        ]);

        // Act
        await catalogService.getPriceTiers(itemId);

        // Wait for background fetch
        await Future.delayed(Duration(milliseconds: 100));

        // Verify storage
        final storedTiers = await mockLocalFileStore.readList<PriceTier>(
          'catalog/price_tiers/$itemId.json',
          (json) => PriceTier.fromJson(json),
        );

        // Assert
        expect(storedTiers.length, equals(2));
        expect(storedTiers.any((t) => t.saleId == 2), isTrue);
      });
    });
  });
}
