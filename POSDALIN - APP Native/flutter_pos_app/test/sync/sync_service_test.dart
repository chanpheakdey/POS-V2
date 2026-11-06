import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:pos_app/src/sync/sync_service.dart';
import 'package:pos_app/src/api/api_client.dart';
import 'package:pos_app/src/api/dtos/category_dto.dart';
import 'package:pos_app/src/api/dtos/group_dto.dart';
import 'package:pos_app/src/api/dtos/item_dto.dart';
import 'package:pos_app/src/shared/local_file_store.dart';
import 'package:pos_app/src/shared/models/sync_meta.dart';
import 'package:pos_app/src/shared/models/category.dart';
import 'package:pos_app/src/shared/models/group.dart';
import 'package:pos_app/src/shared/models/item.dart';
import 'package:pos_app/src/shared/constants.dart';

// Mocks
class MockApiClient extends Mock implements ApiClient {}

class MockLocalFileStore extends Mock implements LocalFileStore {}

class MockConnectivity extends Mock implements Connectivity {}

void main() {
  late MockApiClient mockApiClient;
  late MockLocalFileStore mockLocalFileStore;
  late MockConnectivity mockConnectivity;
  late SyncServiceImpl syncService;

  setUp(() {
    mockApiClient = MockApiClient();
    mockLocalFileStore = MockLocalFileStore();
    mockConnectivity = MockConnectivity();

    syncService = SyncServiceImpl(
      apiClient: mockApiClient,
      localFileStore: mockLocalFileStore,
      connectivity: mockConnectivity,
    );

    // Register fallback values for mocktail
    registerFallbackValue(<Category>[]);
    registerFallbackValue(<Group>[]);
    registerFallbackValue(<Item>[]);
    registerFallbackValue(<SyncMeta>[]);
    registerFallbackValue(Category(
      categoryId: 0,
      categoryName: '',
      imageUrl: '',
      colorIndex: 0,
    ));
    registerFallbackValue(Group(
      groupId: 0,
      categoryId: 0,
      groupName: '',
      imageUrl: '',
    ));
    registerFallbackValue(Item(
      itemId: 0,
      categoryId: 0,
      groupId: 0,
      itemName: '',
      itemCode: '',
      imageUrl: '',
      price: 0.0,
    ));
  });

  group('SyncService initialization', () {
    test('lastSyncedAt returns null when no sync has occurred', () {
      expect(syncService.lastSyncedAt, isNull);
    });

    test('initialize loads sync metadata from local storage', () async {
      final testMeta = SyncMeta(
        categoriesLastSync: DateTime(2024, 1, 1),
        groupsLastSync: DateTime(2024, 1, 2),
        itemsLastSync: DateTime(2024, 1, 3),
        imagesLastSync: DateTime(2024, 1, 4),
      );

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => [testMeta]);

      await syncService.initialize();

      expect(syncService.syncMeta, equals(testMeta));
    });

    test('initialize handles missing sync metadata gracefully', () async {
      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      await syncService.initialize();

      expect(syncService.syncMeta, equals(const SyncMeta()));
    });

    test('lastSyncedAt returns most recent timestamp', () async {
      final testMeta = SyncMeta(
        categoriesLastSync: DateTime(2024, 1, 1),
        groupsLastSync: DateTime(2024, 1, 5), // Most recent
        itemsLastSync: DateTime(2024, 1, 3),
        imagesLastSync: DateTime(2024, 1, 2),
      );

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => [testMeta]);

      await syncService.initialize();

      expect(syncService.lastSyncedAt, equals(DateTime(2024, 1, 5)));
    });
  });

  group('Category synchronization', () {
    test('synchronize fetches categories and appends to local storage',
        () async {
      // Validates: Requirements 18.3, 18.5, 18.6, 18.7
      final categoryDtos = [
        CategoryDto(categoryId: 1, categoryName: 'Electronics'),
        CategoryDto(categoryId: 2, categoryName: 'Books'),
      ];

      // Setup mocks
      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories())
          .thenAnswer((_) async => categoryDtos);

      when(() => mockLocalFileStore.appendItems<Category>(
            StoragePaths.categoriesFile,
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      // Execute sync
      await syncService.synchronize();

      // Verify categories were appended
      verify(() => mockLocalFileStore.appendItems<Category>(
            StoragePaths.categoriesFile,
            any(
                that: predicate<List<Category>>((list) =>
                    list.length == 2 &&
                    list[0].categoryId == 1 &&
                    list[1].categoryId == 2)),
            any(),
            any(),
            any(),
          )).called(1);

      // Verify sync meta was saved
      verify(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).called(1);
    });

    test('category sync assigns color indices in rotation', () async {
      final categoryDtos = List.generate(
        25,
        (i) => CategoryDto(categoryId: i + 1, categoryName: 'Category $i'),
      );

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories())
          .thenAnswer((_) async => categoryDtos);

      when(() => mockLocalFileStore.appendItems<Category>(
            StoragePaths.categoriesFile,
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Verify color indices cycle correctly
      final captured = verify(() => mockLocalFileStore.appendItems<Category>(
            StoragePaths.categoriesFile,
            captureAny(),
            any(),
            any(),
            any(),
          )).captured;

      final categories = captured[0] as List<Category>;
      expect(categories[0].colorIndex, 0);
      expect(categories[20].colorIndex, 20);
      expect(categories[21].colorIndex, 0); // Cycles back
      expect(categories[24].colorIndex, 3);
    });

    test('category sync continues on API error without throwing', () async {
      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories())
          .thenThrow(Exception('Network error'));

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      // Should not throw
      await syncService.synchronize();

      // Verify append was never called
      verifyNever(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          ));
    });
  });

  group('Group and item synchronization', () {
    test('synchronize fetches groups for each category', () async {
      // Validates: Requirements 18.9, 18.10
      final categories = [
        Category(
            categoryId: 1,
            categoryName: 'Electronics',
            imageUrl: '',
            colorIndex: 0),
        Category(
            categoryId: 2, categoryName: 'Books', imageUrl: '', colorIndex: 1),
      ];

      final groupDtos = [
        GroupDto(
            groupId: 1,
            categoryId: 1,
            groupName: 'Phones',
            imageUrl: 'phone.jpg'),
        GroupDto(
            groupId: 2,
            categoryId: 1,
            groupName: 'Laptops',
            imageUrl: 'laptop.jpg'),
      ];

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories()).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => categories);

      when(() => mockApiClient.getGroups(any()))
          .thenAnswer((_) async => groupDtos);

      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Group>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.readList<Group>(
            any(),
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Item>(
            any(),
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.appendItems<Item>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Verify groups were fetched for each category
      verify(() => mockApiClient.getGroups(1)).called(1);
      verify(() => mockApiClient.getGroups(2)).called(1);

      // Verify groups were appended
      verify(() => mockLocalFileStore.appendItems<Group>(
            '${StoragePaths.groupsDir}/1.json',
            any(),
            any(),
            any(),
            any(),
          )).called(1);
    });

    test('synchronize fetches items for each category-group combination',
        () async {
      // Validates: Requirements 18.12, 18.13
      final categories = [
        Category(
            categoryId: 1,
            categoryName: 'Electronics',
            imageUrl: '',
            colorIndex: 0),
      ];

      final groups = [
        Group(
            groupId: 1,
            categoryId: 1,
            groupName: 'Phones',
            imageUrl: 'phone.jpg'),
        Group(
            groupId: 2,
            categoryId: 1,
            groupName: 'Laptops',
            imageUrl: 'laptop.jpg'),
      ];

      final itemDtos = [
        ItemDto(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'iPhone 15',
          itemCode: 'IP15',
          imageUrl: 'iphone15.jpg',
          price: 999.0,
        ),
      ];

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories()).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => categories);

      when(() => mockApiClient.getGroups(any())).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Group>(
            '${StoragePaths.groupsDir}/1.json',
            any(),
          )).thenAnswer((_) async => groups);

      when(() => mockApiClient.getSaleItems(any(), any()))
          .thenAnswer((_) async => itemDtos);

      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Group>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Item>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Verify items were fetched for each group
      verify(() => mockApiClient.getSaleItems(1, 1)).called(1);
      verify(() => mockApiClient.getSaleItems(1, 2)).called(1);

      // Verify items were appended
      verify(() => mockLocalFileStore.appendItems<Item>(
            '${StoragePaths.itemsDir}/1_1.json',
            any(),
            any(),
            any(),
            any(),
          )).called(1);
      verify(() => mockLocalFileStore.appendItems<Item>(
            '${StoragePaths.itemsDir}/1_2.json',
            any(),
            any(),
            any(),
            any(),
          )).called(1);
    });

    test('synchronize fetches items directly when category has no groups',
        () async {
      final categories = [
        Category(
            categoryId: 1,
            categoryName: 'Books',
            imageUrl: '',
            colorIndex: 0),
      ];

      final itemDtos = [
        ItemDto(
          itemId: 1,
          categoryId: 1,
          groupId: 0, // No group
          itemName: 'Flutter Cookbook',
          itemCode: 'BOOK001',
          imageUrl: 'book.jpg',
          price: 29.99,
        ),
      ];

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories()).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => categories);

      when(() => mockApiClient.getGroups(any())).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Group>(
            '${StoragePaths.groupsDir}/1.json',
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getSaleItems(1, 0))
          .thenAnswer((_) async => itemDtos);

      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Group>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Item>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Verify items were fetched with groupId = 0
      verify(() => mockApiClient.getSaleItems(1, 0)).called(1);
    });
  });

  group('Append-only synchronization behavior', () {
    test('synchronize never overwrites existing data', () async {
      // Validates: Requirements 18.4, 18.7, 18.14
      final categoryDtos = [
        CategoryDto(categoryId: 1, categoryName: 'New Category'),
      ];

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories())
          .thenAnswer((_) async => categoryDtos);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      // Mock appendItems to capture behavior
      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Verify appendItems was called (not writeList for categories)
      verify(() => mockLocalFileStore.appendItems<Category>(
            StoragePaths.categoriesFile,
            any(),
            any(),
            any(),
            any(),
          )).called(1);
    });
  });

  group('Sync metadata management', () {
    test('synchronize updates sync timestamps on success', () async {
      // Validates: Requirements 18.5, 18.9
      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories()).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      SyncMeta? savedMeta;
      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((invocation) async {
        final metaList =
            invocation.positionalArguments[1] as List<SyncMeta>;
        savedMeta = metaList.first;
      });

      final beforeSync = DateTime.now();
      await syncService.synchronize();
      final afterSync = DateTime.now();

      expect(savedMeta, isNotNull);
      expect(savedMeta!.categoriesLastSync, isNotNull);
      expect(
          savedMeta!.categoriesLastSync!.isAfter(beforeSync.subtract(const Duration(seconds: 1))),
          isTrue);
      expect(savedMeta!.categoriesLastSync!.isBefore(afterSync), isTrue);
    });

    test('synchronize does not update timestamps on failure', () async {
      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories())
          .thenThrow(Exception('Network error'));

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      await syncService.synchronize();

      // Sync meta should still be saved, but timestamps should not be updated for failed operations
      final captured = verify(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            captureAny(),
            any(),
          )).captured;

      final savedMeta = (captured[0] as List<SyncMeta>)[0];
      expect(savedMeta.categoriesLastSync, isNull);
    });
  });

  group('Error handling', () {
    test('sync continues after individual category group fetch fails',
        () async {
      final categories = [
        Category(
            categoryId: 1, categoryName: 'Cat1', imageUrl: '', colorIndex: 0),
        Category(
            categoryId: 2, categoryName: 'Cat2', imageUrl: '', colorIndex: 1),
      ];

      when(() => mockLocalFileStore.readList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockApiClient.getCategories()).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.readList<Category>(
            StoragePaths.categoriesFile,
            any(),
          )).thenAnswer((_) async => categories);

      when(() => mockApiClient.getGroups(1))
          .thenThrow(Exception('Network error'));
      when(() => mockApiClient.getGroups(2)).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.appendItems<Category>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.appendItems<Group>(
            any(),
            any(),
            any(),
            any(),
            any(),
          )).thenAnswer((_) async => {});

      when(() => mockLocalFileStore.readList<Group>(
            any(),
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<SyncMeta>(
            StoragePaths.syncMetaFile,
            any(),
            any(),
          )).thenAnswer((_) async => {});

      // Should not throw - should continue with category 2
      await syncService.synchronize();

      verify(() => mockApiClient.getGroups(1)).called(1);
      verify(() => mockApiClient.getGroups(2)).called(1);
    });
  });
}
