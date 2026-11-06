import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/screens/search/search_screen.dart';
import 'package:pos_app/src/search/search_service.dart';
import 'package:pos_app/src/shared/models/item.dart';
import 'package:pos_app/src/cart/cart_service.dart';
import 'package:pos_app/src/catalog/catalog_service.dart';
import 'package:pos_app/src/shared/image_handler.dart';
import 'package:pos_app/src/shared/models/price_tier.dart';
import 'package:pos_app/src/shared/models/cart_item.dart';

void main() {
  group('SearchScreen', () {
    testWidgets('displays search input field on load', (tester) async {
      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => MockSearchService()),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Verify search input field exists
      expect(find.byType(TextField), findsOneWidget);
      expect(
        find.text('Search by name or code (min 2 characters)'),
        findsOneWidget,
      );

      // Verify initial hint message
      expect(find.text('Search for items by name or code'), findsOneWidget);
      expect(
        find.text('Enter at least 2 characters to start searching'),
        findsOneWidget,
      );

      container.dispose();
    });

    testWidgets('does not search with less than 2 characters', (tester) async {
      final mockSearchService = MockSearchService();
      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => mockSearchService),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Enter 1 character
      await tester.enterText(find.byType(TextField), 'a');
      await tester.pumpAndSettle();

      // Verify search was not called (no results displayed)
      expect(find.byType(GridView), findsNothing);
      expect(mockSearchService.searchCallCount, 0);

      container.dispose();
    });

    testWidgets('performs search with 2 or more characters', (tester) async {
      final mockSearchService = MockSearchService();
      mockSearchService.mockResults = [
        const Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Test Item',
          itemCode: 'TEST001',
          imageUrl: 'test.jpg',
          price: 10.0,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => mockSearchService),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Enter 2 characters
      await tester.enterText(find.byType(TextField), 'te');
      await tester.pumpAndSettle();

      // Verify search was called
      expect(mockSearchService.searchCallCount, 1);
      expect(mockSearchService.lastQuery, 'te');

      // Verify results are displayed in grid
      expect(find.byType(GridView), findsOneWidget);
      expect(find.text('Test Item'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays "No items found" when search returns empty',
        (tester) async {
      final mockSearchService = MockSearchService();
      mockSearchService.mockResults = []; // Empty results

      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => mockSearchService),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Enter search query
      await tester.enterText(find.byType(TextField), 'xyz');
      await tester.pumpAndSettle();

      // Verify "No items found" message
      expect(find.text('No items found'), findsOneWidget);
      expect(find.text('Try a different search term'), findsOneWidget);

      container.dispose();
    });

    testWidgets('clear button clears search and results', (tester) async {
      final mockSearchService = MockSearchService();
      mockSearchService.mockResults = [
        const Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Test Item',
          itemCode: 'TEST001',
          imageUrl: 'test.jpg',
          price: 10.0,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => mockSearchService),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Enter search query
      await tester.enterText(find.byType(TextField), 'test');
      await tester.pumpAndSettle();

      // Verify results are displayed
      expect(find.byType(GridView), findsOneWidget);

      // Tap clear button
      await tester.tap(find.byIcon(Icons.clear));
      await tester.pumpAndSettle();

      // Verify input is cleared and hint is shown
      final textField = tester.widget<TextField>(find.byType(TextField));
      expect(textField.controller?.text, isEmpty);
      expect(find.text('Search for items by name or code'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays results in grid format', (tester) async {
      final mockSearchService = MockSearchService();
      mockSearchService.mockResults = [
        const Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 1',
          itemCode: 'CODE1',
          imageUrl: 'item1.jpg',
          price: 10.0,
        ),
        const Item(
          itemId: 2,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 2',
          itemCode: 'CODE2',
          imageUrl: 'item2.jpg',
          price: 15.0,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          searchServiceProvider.overrideWith((ref) => mockSearchService),
          cartProvider.overrideWith((ref) async => MockCartService()),
          catalogServiceProvider.overrideWith((ref) => MockCatalogService()),
          imageHandlerProvider.overrideWith((ref) => MockImageHandler()),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: const MaterialApp(
            home: SearchScreen(),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Enter search query
      await tester.enterText(find.byType(TextField), 'item');
      await tester.pumpAndSettle();

      // Verify grid is displayed with results
      expect(find.byType(GridView), findsOneWidget);
      expect(find.text('Item 1'), findsOneWidget);
      expect(find.text('Item 2'), findsOneWidget);

      container.dispose();
    });
  });
}

// Mock services

class MockSearchService implements SearchService {
  List<Item> mockResults = [];
  int searchCallCount = 0;
  String? lastQuery;

  @override
  Future<List<Item>> search(String query) async {
    searchCallCount++;
    lastQuery = query;
    return mockResults;
  }
}

class MockCartService implements CartService {
  @override
  List<CartItem> get items => [];

  @override
  double get total => 0.0;

  @override
  Future<void> addItem(item, priceTiers) async {}

  @override
  Future<void> setQuantity(int itemId, int quantity, priceTiers) async {}

  @override
  Future<void> removeItem(int itemId) async {}

  @override
  Future<void> clear() async {}

  @override
  int quantityFor(int itemId) => 0;
}

class MockCatalogService implements CatalogService {
  @override
  Future<List<dynamic>> getCategories() async => [];

  @override
  Future<List<dynamic>> getGroups(int categoryId) async => [];

  @override
  Future<List<Item>> getItems(int categoryId, int groupId) async => [];

  @override
  Future<dynamic> getItemDetail(int itemId) async => null;

  @override
  Future<List<PriceTier>> getPriceTiers(int itemId) async => [
        const PriceTier(
          saleId: 1,
          itemId: itemId,
          fromQty: 1,
          toQty: 0,
          priceUsd: 10.0,
          priceKhr: 40000.0,
        ),
      ];
}

class MockImageHandler implements ImageHandler {
  @override
  Widget buildImage(
    String imagePath,
    int width,
    int height, {
    Widget? placeholder,
  }) {
    return placeholder ??
        Container(
          width: width.toDouble(),
          height: height.toDouble(),
          color: Colors.grey,
        );
  }

  @override
  Future<void> preloadBundledImages() async {}
}
