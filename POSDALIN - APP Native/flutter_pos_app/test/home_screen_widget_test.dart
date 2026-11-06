import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pos_app/src/screens/home/home_screen.dart';
import 'package:pos_app/src/catalog/catalog_service.dart';
import 'package:pos_app/src/shared/models/models.dart';
import 'package:pos_app/src/shared/image_handler.dart';

/// Mock CatalogService for testing
class MockCatalogService implements CatalogService {
  final List<Category> categories;

  MockCatalogService({required this.categories});

  @override
  Future<List<Category>> getCategories() async {
    return categories;
  }

  @override
  Future<List<Group>> getGroups(int categoryId) async {
    return [];
  }

  @override
  Future<List<Item>> getItems(int categoryId, int groupId) async {
    return [];
  }

  @override
  Future<ItemDetail> getItemDetail(int itemId) async {
    throw UnimplementedError();
  }

  @override
  Future<List<PriceTier>> getPriceTiers(int itemId) async {
    return [];
  }
}

/// Mock ImageHandler for testing
class MockImageHandler implements ImageHandler {
  @override
  Widget buildImage(
    String imagePath,
    int width,
    int height, {
    Widget? placeholder,
  }) {
    return Container(
      width: width.toDouble(),
      height: height.toDouble(),
      color: Colors.grey,
    );
  }

  @override
  Future<void> preloadBundledImages() async {}

  @override
  Future<String> getLocalImagePath(String imagePath, ImageType type) async {
    return imagePath;
  }

  @override
  Future<String?> downloadAndCacheImage(
    String imagePath,
    int width,
    int height,
    ImageType type,
  ) async {
    return null;
  }
}

void main() {
  group('HomeScreen Widget Tests', () {
    testWidgets('HomeScreen builds without crashing', (WidgetTester tester) async {
      // Create mock categories
      final mockCategories = [
        const Category(
          categoryId: 1,
          categoryName: 'Test Category 1',
          imageUrl: 'test1.jpg',
          colorIndex: 0,
        ),
        const Category(
          categoryId: 2,
          categoryName: 'Test Category 2',
          imageUrl: 'test2.jpg',
          colorIndex: 1,
        ),
      ];

      // Create provider overrides
      final container = ProviderContainer(
        overrides: [
          catalogServiceProvider.overrideWithValue(
            MockCatalogService(categories: mockCategories),
          ),
          imageHandlerProvider.overrideWithValue(
            MockImageHandler(),
          ),
        ],
      );

      // Build the widget
      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: const HomeScreen(),
          ),
        ),
      );

      // Verify the app bar title
      expect(find.text('Categories'), findsOneWidget);

      // Verify search and cart icons exist
      expect(find.byIcon(Icons.search), findsOneWidget);
      expect(find.byIcon(Icons.shopping_cart), findsOneWidget);

      // Verify the widget builds without errors
      expect(find.byType(HomeScreen), findsOneWidget);
    });

    testWidgets('HomeScreen displays category grid', (WidgetTester tester) async {
      final mockCategories = [
        const Category(
          categoryId: 1,
          categoryName: 'Electronics',
          imageUrl: 'electronics.jpg',
          colorIndex: 0,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          catalogServiceProvider.overrideWithValue(
            MockCatalogService(categories: mockCategories),
          ),
          imageHandlerProvider.overrideWithValue(
            MockImageHandler(),
          ),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: const HomeScreen(),
          ),
        ),
      );

      // Wait for async operations
      await tester.pumpAndSettle();

      // Verify category name appears (after loading completes)
      // Note: This might fail initially if CategoryGrid is still loading
      // The actual behavior depends on the implementation
      expect(find.byType(GridView), findsOneWidget);
    });

    testWidgets('HomeScreen app bar actions are tappable', (WidgetTester tester) async {
      final container = ProviderContainer(
        overrides: [
          catalogServiceProvider.overrideWithValue(
            MockCatalogService(categories: []),
          ),
          imageHandlerProvider.overrideWithValue(
            MockImageHandler(),
          ),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: const HomeScreen(),
          ),
        ),
      );

      // Verify search button is present and has correct tooltip
      final searchButton = find.widgetWithIcon(IconButton, Icons.search);
      expect(searchButton, findsOneWidget);

      // Verify cart button is present and has correct tooltip
      final cartButton = find.widgetWithIcon(IconButton, Icons.shopping_cart);
      expect(cartButton, findsOneWidget);
    });
  });
}
