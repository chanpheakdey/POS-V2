import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';

import 'package:flutter_pos_app/src/catalog/item_list_screen.dart';
import 'package:flutter_pos_app/src/catalog/catalog_service.dart';
import 'package:flutter_pos_app/src/cart/cart_service.dart';
import 'package:flutter_pos_app/src/shared/models/models.dart';
import 'package:flutter_pos_app/src/shared/image_handler.dart';

import 'item_list_screen_test.mocks.dart';

@GenerateMocks([CatalogService, CartService, ImageHandler])
void main() {
  group('ItemListScreen Widget Tests', () {
    late MockCatalogService mockCatalogService;
    late MockCartService mockCartService;
    late MockImageHandler mockImageHandler;

    setUp(() {
      mockCatalogService = MockCatalogService();
      mockCartService = MockCartService();
      mockImageHandler = MockImageHandler();
    });

    Widget createTestWidget({
      required int categoryId,
      required int groupId,
      String groupName = 'Test Group',
    }) {
      return ProviderScope(
        overrides: [
          catalogServiceProvider.overrideWithValue(mockCatalogService),
          cartProvider.overrideWith((ref) => Stream.value(mockCartService)),
          imageHandlerProvider.overrideWithValue(mockImageHandler),
        ],
        child: MaterialApp(
          home: ItemListScreen(
            categoryId: categoryId,
            groupId: groupId,
            groupName: groupName,
          ),
        ),
      );
    }

    testWidgets('displays loading indicator while fetching items',
        (WidgetTester tester) async {
      // Arrange
      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => Future.delayed(
                const Duration(seconds: 1),
                () => [],
              ));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pump();

      // Assert
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('displays empty state when no items are found',
        (WidgetTester tester) async {
      // Arrange
      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => []);

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('No items found'), findsOneWidget);
      expect(find.byIcon(Icons.inventory_2_outlined), findsOneWidget);
    });

    testWidgets('displays error state when loading fails',
        (WidgetTester tester) async {
      // Arrange
      when(mockCatalogService.getItems(any, any))
          .thenThrow(Exception('Network error'));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('Error loading items'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);
    });

    testWidgets('displays grid of items when data is loaded',
        (WidgetTester tester) async {
      // Arrange
      final testItems = [
        Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 1',
          itemCode: 'ITEM001',
          imageUrl: 'https://example.com/item1.jpg',
          price: 10.0,
        ),
        Item(
          itemId: 2,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 2',
          itemCode: 'ITEM002',
          imageUrl: 'https://example.com/item2.jpg',
          price: 20.0,
        ),
      ];

      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => testItems);
      when(mockCartService.quantityFor(any)).thenReturn(0);
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byType(GridView), findsOneWidget);
      expect(find.byType(ItemCard), findsNWidgets(2));
      expect(find.text('Item 1'), findsOneWidget);
      expect(find.text('Item 2'), findsOneWidget);
    });

    testWidgets('displays quantity badge when item is in cart',
        (WidgetTester tester) async {
      // Arrange
      final testItems = [
        Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 1',
          itemCode: 'ITEM001',
          imageUrl: 'https://example.com/item1.jpg',
          price: 10.0,
        ),
      ];

      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => testItems);
      when(mockCartService.quantityFor(1)).thenReturn(5);
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byType(QuantityBadge), findsOneWidget);
      expect(find.text('5'), findsOneWidget);
    });

    testWidgets('does not display quantity badge when item not in cart',
        (WidgetTester tester) async {
      // Arrange
      final testItems = [
        Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 1',
          itemCode: 'ITEM001',
          imageUrl: 'https://example.com/item1.jpg',
          price: 10.0,
        ),
      ];

      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => testItems);
      when(mockCartService.quantityFor(1)).thenReturn(0);
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Assert
      expect(find.byType(QuantityBadge), findsNothing);
    });

    testWidgets('shows visual feedback when item is tapped',
        (WidgetTester tester) async {
      // Arrange
      final testItems = [
        Item(
          itemId: 1,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item 1',
          itemCode: 'ITEM001',
          imageUrl: 'https://example.com/item1.jpg',
          price: 10.0,
        ),
      ];

      final testPriceTiers = [
        PriceTier(
          saleId: 1,
          itemId: 1,
          fromQty: 1,
          toQty: 0,
          priceUsd: 10.0,
          priceKhr: 41000.0,
        ),
      ];

      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => testItems);
      when(mockCatalogService.getPriceTiers(1))
          .thenAnswer((_) async => testPriceTiers);
      when(mockCartService.quantityFor(1)).thenReturn(0);
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Find the item card and tap it
      final itemCard = find.byType(ItemCard).first;
      await tester.tap(itemCard);
      await tester.pump(); // Trigger animation

      // Assert - animation should start
      // Note: Testing animation details might require more sophisticated checks
      expect(find.byType(AnimatedScale), findsWidgets);
    });

    testWidgets('displays correct grid column count for different screen sizes',
        (WidgetTester tester) async {
      // Arrange
      final testItems = List.generate(
        10,
        (index) => Item(
          itemId: index,
          categoryId: 1,
          groupId: 1,
          itemName: 'Item $index',
          itemCode: 'ITEM00$index',
          imageUrl: 'https://example.com/item$index.jpg',
          price: 10.0 * index,
        ),
      );

      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => testItems);
      when(mockCartService.quantityFor(any)).thenReturn(0);
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      // Act - small screen (< 600)
      await tester.pumpWidget(
        MediaQuery(
          data: const MediaQueryData(size: Size(400, 800)),
          child: createTestWidget(categoryId: 1, groupId: 1),
        ),
      );
      await tester.pumpAndSettle();

      // Assert - should have 2 columns for small screens
      final gridView = tester.widget<GridView>(find.byType(GridView));
      final delegate =
          gridView.gridDelegate as SliverGridDelegateWithFixedCrossAxisCount;
      expect(delegate.crossAxisCount, 2);
    });

    testWidgets('handles retry button tap after error',
        (WidgetTester tester) async {
      // Arrange
      var callCount = 0;
      when(mockCatalogService.getItems(any, any)).thenAnswer((_) async {
        callCount++;
        if (callCount == 1) {
          throw Exception('Network error');
        }
        return [];
      });

      // Act
      await tester.pumpWidget(createTestWidget(categoryId: 1, groupId: 1));
      await tester.pumpAndSettle();

      // Verify error state
      expect(find.text('Error loading items'), findsOneWidget);

      // Tap retry button
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      // Assert - should show empty state after successful retry
      expect(find.text('No items found'), findsOneWidget);
      expect(callCount, 2);
    });

    testWidgets('displays group name in app bar',
        (WidgetTester tester) async {
      // Arrange
      when(mockCatalogService.getItems(any, any))
          .thenAnswer((_) async => []);

      // Act
      await tester.pumpWidget(createTestWidget(
        categoryId: 1,
        groupId: 1,
        groupName: 'Electronics',
      ));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('Electronics'), findsOneWidget);
    });
  });

  group('ItemCard Widget Tests', () {
    testWidgets('displays item name correctly', (WidgetTester tester) async {
      // Arrange
      final item = Item(
        itemId: 1,
        categoryId: 1,
        groupId: 1,
        itemName: 'Test Item',
        itemCode: 'TEST001',
        imageUrl: 'https://example.com/test.jpg',
        price: 10.0,
      );

      final mockImageHandler = MockImageHandler();
      when(mockImageHandler.buildImage(any, any, any,
              placeholder: anyNamed('placeholder')))
          .thenReturn(Container(color: Colors.grey));

      final mockCartService = MockCartService();
      when(mockCartService.quantityFor(1)).thenReturn(0);

      // Act
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            imageHandlerProvider.overrideWithValue(mockImageHandler),
            cartProvider.overrideWith((ref) => Stream.value(mockCartService)),
          ],
          child: MaterialApp(
            home: Scaffold(
              body: ItemCard(
                item: item,
                imageHandler: mockImageHandler,
                isAnimating: false,
                onTap: () {},
                onLongPress: () {},
              ),
            ),
          ),
        ),
      );

      // Assert
      expect(find.text('Test Item'), findsOneWidget);
    });
  });

  group('QuantityBadge Widget Tests', () {
    testWidgets('displays quantity correctly', (WidgetTester tester) async {
      // Act
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: QuantityBadge(quantity: 42),
          ),
        ),
      );

      // Assert
      expect(find.text('42'), findsOneWidget);
    });

    testWidgets('has correct styling', (WidgetTester tester) async {
      // Act
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: QuantityBadge(quantity: 5),
          ),
        ),
      );

      // Assert
      final container = tester.widget<Container>(
        find.ancestor(
          of: find.text('5'),
          matching: find.byType(Container),
        ),
      );

      final decoration = container.decoration as BoxDecoration;
      expect(decoration.color, Colors.red);
      expect(decoration.borderRadius, BorderRadius.circular(12));
    });
  });
}
