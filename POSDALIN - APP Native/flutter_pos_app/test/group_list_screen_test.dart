import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_pos_app/src/catalog/catalog.dart';
import 'package:flutter_pos_app/src/catalog/catalog_service.dart';
import 'package:flutter_pos_app/src/shared/models/models.dart';
import 'package:flutter_pos_app/src/shared/image_handler.dart';
import 'package:flutter_pos_app/src/cart/cart_service.dart';

/// Test suite for GroupListScreen widget
/// 
/// Tests:
/// - Loading state display
/// - Error state display and retry
/// - Empty state display
/// - Success state with group grid
/// - Color-coded cards with group name and image (300×200)
/// - Tap navigation to item list
/// - Responsive grid (2-4 columns based on screen width)
/// 
/// **Validates: Requirements 4.1, 4.7, 14.3**
void main() {
  group('GroupListScreen Widget Tests', () {
    // Mock data
    final mockGroups = [
      Group(
        groupId: 1,
        categoryId: 100,
        groupName: 'Test Group 1',
        imageUrl: 'https://example.com/group1.jpg',
      ),
      Group(
        groupId: 2,
        categoryId: 100,
        groupName: 'Test Group 2',
        imageUrl: 'https://example.com/group2.jpg',
      ),
      Group(
        groupId: 3,
        categoryId: 100,
        groupName: 'Test Group 3',
        imageUrl: 'https://example.com/group3.jpg',
      ),
    ];

    // Helper to build widget with providers
    Widget buildGroupListScreen({
      required List<Group> groups,
      bool shouldError = false,
      int categoryId = 100,
      String? categoryName,
    }) {
      return ProviderScope(
        overrides: [
          catalogServiceProvider.overrideWith((ref) => _MockCatalogService(
                groups: groups,
                shouldError: shouldError,
              )),
          imageHandlerProvider.overrideWith((ref) => _MockImageHandler()),
          cartProvider.overrideWith((ref) => _MockCartService()),
        ],
        child: MaterialApp(
          home: GroupListScreen(
            categoryId: categoryId,
            categoryName: categoryName,
          ),
        ),
      );
    }

    testWidgets('displays loading indicator initially',
        (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      
      // Should show loading indicator
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('displays group grid after loading', (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Should display all groups
      expect(find.text('Test Group 1'), findsOneWidget);
      expect(find.text('Test Group 2'), findsOneWidget);
      expect(find.text('Test Group 3'), findsOneWidget);
      
      // Should display grid
      expect(find.byType(GridView), findsOneWidget);
    });

    testWidgets('displays error state on failure', (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(
        groups: [],
        shouldError: true,
      ));
      await tester.pumpAndSettle();

      // Should show error icon and message
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
      expect(find.text('Error loading groups'), findsOneWidget);
      expect(find.text('Mock error'), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);
    });

    testWidgets('displays empty state when no groups', (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(groups: []));
      await tester.pumpAndSettle();

      // Should show empty state
      expect(find.byIcon(Icons.folder_open), findsOneWidget);
      expect(find.text('No groups found'), findsOneWidget);
    });

    testWidgets('displays category name in app bar', (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(
        groups: mockGroups,
        categoryName: 'Electronics',
      ));
      await tester.pumpAndSettle();

      // Should show category name
      expect(find.text('Electronics'), findsOneWidget);
    });

    testWidgets('uses correct grid layout for small screens',
        (WidgetTester tester) async {
      // Set small screen size (< 600dp)
      tester.view.physicalSize = const Size(400, 800);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Should have 2 columns on small screens
      final gridView = tester.widget<GridView>(find.byType(GridView));
      final delegate = gridView.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
      expect(delegate.crossAxisCount, 2);
    });

    testWidgets('uses correct grid layout for medium screens',
        (WidgetTester tester) async {
      // Set medium screen size (600-900dp)
      tester.view.physicalSize = const Size(750, 1200);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Should have 3 columns on medium screens
      final gridView = tester.widget<GridView>(find.byType(GridView));
      final delegate = gridView.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
      expect(delegate.crossAxisCount, 3);
    });

    testWidgets('uses correct grid layout for large screens',
        (WidgetTester tester) async {
      // Set large screen size (> 900dp)
      tester.view.physicalSize = const Size(1000, 1400);
      tester.view.devicePixelRatio = 1.0;
      addTearDown(tester.view.resetPhysicalSize);
      addTearDown(tester.view.resetDevicePixelRatio);

      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Should have 4 columns on large screens
      final gridView = tester.widget<GridView>(find.byType(GridView));
      final delegate = gridView.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
      expect(delegate.crossAxisCount, 4);
    });

    testWidgets('navigates to item list on group tap',
        (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Tap on first group
      await tester.tap(find.text('Test Group 1'));
      await tester.pumpAndSettle();

      // Should navigate to ItemListScreen
      // Note: We check that the navigation happened by looking for the new screen
      expect(find.byType(ItemListScreen), findsOneWidget);
    });

    testWidgets('displays group cards with correct aspect ratio',
        (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(groups: mockGroups));
      await tester.pumpAndSettle();

      // Check that grid has correct aspect ratio for 300×200 images
      final gridView = tester.widget<GridView>(find.byType(GridView));
      final delegate = gridView.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
      expect(delegate.childAspectRatio, 300 / 200);
    });

    testWidgets('retry button reloads groups on error',
        (WidgetTester tester) async {
      await tester.pumpWidget(buildGroupListScreen(
        groups: [],
        shouldError: true,
      ));
      await tester.pumpAndSettle();

      // Should show error state
      expect(find.text('Error loading groups'), findsOneWidget);

      // Tap retry button
      await tester.tap(find.text('Retry'));
      await tester.pump();

      // Should show loading indicator again
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });
  });
}

// ==================== Mock Implementations ====================

class _MockCatalogService implements CatalogService {
  final List<Group> groups;
  final bool shouldError;

  _MockCatalogService({
    required this.groups,
    this.shouldError = false,
  });

  @override
  Future<List<Category>> getCategories() async {
    if (shouldError) throw Exception('Mock error');
    return [];
  }

  @override
  Future<List<Group>> getGroups(int categoryId) async {
    if (shouldError) throw Exception('Mock error');
    await Future.delayed(const Duration(milliseconds: 100));
    return groups;
  }

  @override
  Future<List<Item>> getItems(int categoryId, int groupId) async {
    if (shouldError) throw Exception('Mock error');
    return [];
  }

  @override
  Future<ItemDetail> getItemDetail(int itemId) async {
    if (shouldError) throw Exception('Mock error');
    throw UnimplementedError();
  }

  @override
  Future<List<PriceTier>> getPriceTiers(int itemId) async {
    if (shouldError) throw Exception('Mock error');
    return [];
  }
}

class _MockImageHandler implements ImageHandler {
  @override
  Widget buildImage(
    String imagePath,
    int width,
    int height, {
    Widget? placeholder,
  }) {
    // Return a simple container for testing
    return Container(
      width: width.toDouble(),
      height: height.toDouble(),
      color: Colors.grey,
      child: const Icon(Icons.image),
    );
  }

  @override
  Future<void> preloadBundledImages() async {
    // No-op for testing
  }
}

class _MockCartService extends CartService {
  _MockCartService() : super();

  @override
  List<CartItem> get items => [];

  @override
  double get total => 0.0;

  @override
  Future<void> addItem(Item item, List<PriceTier> priceTiers) async {
    // No-op for testing
  }

  @override
  Future<void> setQuantity(int itemId, int quantity) async {
    // No-op for testing
  }

  @override
  Future<void> removeItem(int itemId) async {
    // No-op for testing
  }

  @override
  Future<void> clear() async {
    // No-op for testing
  }

  @override
  int quantityFor(int itemId) {
    return 0;
  }

  @override
  PriceTier resolvePrice(int itemId, int quantity, List<PriceTier> tiers) {
    return tiers.first;
  }
}
