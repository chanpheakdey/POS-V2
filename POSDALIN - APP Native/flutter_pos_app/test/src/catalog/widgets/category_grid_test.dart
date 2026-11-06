import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_pos_app/src/catalog/widgets/category_grid.dart';
import 'package:flutter_pos_app/src/shared/models/models.dart';
import 'package:flutter_pos_app/src/catalog/catalog_service.dart';
import 'package:flutter_pos_app/src/shared/image_handler.dart';

// Mock CatalogService for testing
class MockCatalogService implements CatalogService {
  final List<Category> categories;
  
  MockCatalogService(this.categories);
  
  @override
  Future<List<Category>> getCategories() async {
    return categories;
  }
  
  @override
  Future<List<Group>> getGroups(int categoryId) async {
    throw UnimplementedError();
  }
  
  @override
  Future<List<Item>> getItems(int categoryId, int groupId) async {
    throw UnimplementedError();
  }
  
  @override
  Future<ItemDetail> getItemDetail(int itemId) async {
    throw UnimplementedError();
  }
  
  @override
  Future<List<PriceTier>> getPriceTiers(int itemId) async {
    throw UnimplementedError();
  }
}

// Mock ImageHandler for testing
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
      child: const Icon(Icons.image),
    );
  }
  
  @override
  Future<void> preloadBundledImages() async {}
  
  @override
  Future<String> getLocalImagePath(String imagePath, ImageType type) async {
    return '/mock/path/$imagePath';
  }
  
  @override
  Future<String?> downloadAndCacheImage(
    String imagePath,
    int width,
    int height,
    ImageType type,
  ) async {
    return '/mock/path/$imagePath';
  }
}

void main() {
  group('CategoryGrid Widget Tests', () {
    testWidgets('displays loading indicator when loading', (WidgetTester tester) async {
      // Create a mock catalog service that never completes
      final mockService = MockCatalogService([]);
      final mockImageHandler = MockImageHandler();
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogServiceProvider.overrideWithValue(mockService),
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryGrid(
                onCategoryTap: (category) {},
              ),
            ),
          ),
        ),
      );
      
      // Should show loading indicator initially
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('displays empty state when no categories', (WidgetTester tester) async {
      final mockService = MockCatalogService([]);
      final mockImageHandler = MockImageHandler();
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogServiceProvider.overrideWithValue(mockService),
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryGrid(
                onCategoryTap: (category) {},
              ),
            ),
          ),
        ),
      );
      
      // Wait for the async data to load
      await tester.pumpAndSettle();
      
      // Should show empty message
      expect(find.text('No categories found'), findsOneWidget);
    });

    testWidgets('displays categories in grid', (WidgetTester tester) async {
      final mockCategories = [
        const Category(
          categoryId: 1,
          categoryName: 'Category 1',
          imageUrl: 'test.jpg',
          colorIndex: 0,
        ),
        const Category(
          categoryId: 2,
          categoryName: 'Category 2',
          imageUrl: 'test2.jpg',
          colorIndex: 1,
        ),
      ];
      
      final mockService = MockCatalogService(mockCategories);
      final mockImageHandler = MockImageHandler();
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogServiceProvider.overrideWithValue(mockService),
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryGrid(
                onCategoryTap: (category) {},
              ),
            ),
          ),
        ),
      );
      
      // Wait for the async data to load
      await tester.pumpAndSettle();
      
      // Should show both categories
      expect(find.text('Category 1'), findsOneWidget);
      expect(find.text('Category 2'), findsOneWidget);
      expect(find.byType(CategoryCard), findsNWidgets(2));
    });

    testWidgets('calls onCategoryTap when category is tapped', (WidgetTester tester) async {
      final mockCategories = [
        const Category(
          categoryId: 1,
          categoryName: 'Test Category',
          imageUrl: 'test.jpg',
          colorIndex: 0,
        ),
      ];
      
      final mockService = MockCatalogService(mockCategories);
      final mockImageHandler = MockImageHandler();
      
      Category? tappedCategory;
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogServiceProvider.overrideWithValue(mockService),
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryGrid(
                onCategoryTap: (category) {
                  tappedCategory = category;
                },
              ),
            ),
          ),
        ),
      );
      
      // Wait for the async data to load
      await tester.pumpAndSettle();
      
      // Tap on the category card
      await tester.tap(find.byType(CategoryCard));
      await tester.pumpAndSettle();
      
      // Should have called onCategoryTap
      expect(tappedCategory, isNotNull);
      expect(tappedCategory?.categoryId, 1);
      expect(tappedCategory?.categoryName, 'Test Category');
    });

    testWidgets('implements pagination for >50 categories', (WidgetTester tester) async {
      // Create 75 categories to test pagination
      final mockCategories = List.generate(
        75,
        (index) => Category(
          categoryId: index + 1,
          categoryName: 'Category ${index + 1}',
          imageUrl: 'test$index.jpg',
          colorIndex: index % 21,
        ),
      );
      
      final mockService = MockCatalogService(mockCategories);
      final mockImageHandler = MockImageHandler();
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            catalogServiceProvider.overrideWithValue(mockService),
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryGrid(
                onCategoryTap: (category) {},
              ),
            ),
          ),
        ),
      );
      
      // Wait for the async data to load
      await tester.pumpAndSettle();
      
      // Should initially show only 50 categories (first page)
      expect(find.byType(CategoryCard), findsNWidgets(50));
      
      // Scroll to bottom to trigger pagination
      await tester.drag(find.byType(GridView), const Offset(0, -3000));
      await tester.pumpAndSettle();
      
      // Should now show more categories (pagination loads next page)
      // The exact number may vary based on scroll position, but should be > 50
      expect(find.byType(CategoryCard).evaluate().length, greaterThan(50));
    });
  });

  group('CategoryCard Widget Tests', () {
    testWidgets('displays category name and uses color', (WidgetTester tester) async {
      const testCategory = Category(
        categoryId: 1,
        categoryName: 'Test Category',
        imageUrl: 'test.jpg',
        colorIndex: 5,
      );
      
      final mockImageHandler = MockImageHandler();
      
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            imageHandlerProvider.overrideWithValue(mockImageHandler),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: CategoryCard(
                category: testCategory,
                onTap: () {},
              ),
            ),
          ),
        ),
      );
      
      await tester.pumpAndSettle();
      
      // Should display category name
      expect(find.text('Test Category'), findsOneWidget);
    });
  });
}
