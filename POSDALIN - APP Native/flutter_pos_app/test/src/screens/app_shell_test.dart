import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:pos_app/src/screens/app_shell.dart';
import 'package:pos_app/src/cart/cart_service.dart';
import 'package:pos_app/src/sync/sync_service.dart';
import 'package:pos_app/src/shared/local_file_store.dart';
import 'package:pos_app/src/shared/models/cart_item.dart';

void main() {
  group('AppShell', () {
    testWidgets('displays bottom navigation bar with 4 items', (tester) async {
      // Mock providers
      final container = ProviderContainer(
        overrides: [
          cartProvider.overrideWith((ref) async {
            return MockCartService();
          }),
          connectivityStreamProvider.overrideWith((ref) {
            return Stream.value([ConnectivityResult.wifi]);
          }),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: AppShell(
              child: Scaffold(
                appBar: AppBar(title: const Text('Test')),
                body: const Center(child: Text('Test Content')),
              ),
            ),
          ),
        ),
      );

      // Wait for async operations
      await tester.pumpAndSettle();

      // Verify bottom navigation bar exists
      expect(find.byType(BottomNavigationBar), findsOneWidget);

      // Verify 4 navigation items (Home, Cart, Orders, Logout)
      expect(find.text('Home'), findsOneWidget);
      expect(find.text('Cart'), findsOneWidget);
      expect(find.text('Orders'), findsOneWidget);
      expect(find.text('Logout'), findsOneWidget);

      // Verify child content is displayed
      expect(find.text('Test Content'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays cart badge when cart has items', (tester) async {
      final container = ProviderContainer(
        overrides: [
          cartProvider.overrideWith((ref) async {
            return MockCartServiceWithItems();
          }),
          connectivityStreamProvider.overrideWith((ref) {
            return Stream.value([ConnectivityResult.wifi]);
          }),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: AppShell(
              child: Scaffold(
                body: const Center(child: Text('Test')),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Verify badge is displayed
      expect(find.byType(Badge), findsOneWidget);
      expect(find.text('3'), findsOneWidget); // 3 items in mock cart

      container.dispose();
    });

    testWidgets('displays online status banner when connected', (tester) async {
      final container = ProviderContainer(
        overrides: [
          cartProvider.overrideWith((ref) async {
            return MockCartService();
          }),
          connectivityStreamProvider.overrideWith((ref) {
            return Stream.value([ConnectivityResult.wifi]);
          }),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: AppShell(
              child: Scaffold(
                body: const Center(child: Text('Test')),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Verify online banner is displayed
      expect(find.text('Online'), findsOneWidget);
      expect(find.byIcon(Icons.wifi), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays offline status banner when disconnected', (tester) async {
      final container = ProviderContainer(
        overrides: [
          cartProvider.overrideWith((ref) async {
            return MockCartService();
          }),
          connectivityStreamProvider.overrideWith((ref) {
            return Stream.value([ConnectivityResult.none]);
          }),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: AppShell(
              child: Scaffold(
                body: const Center(child: Text('Test')),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Verify offline banner is displayed
      expect(find.textContaining('Offline'), findsOneWidget);
      expect(find.byIcon(Icons.wifi_off), findsOneWidget);

      container.dispose();
    });

    testWidgets('no badge displayed when cart is empty', (tester) async {
      final container = ProviderContainer(
        overrides: [
          cartProvider.overrideWith((ref) async {
            return MockCartService(); // Empty cart
          }),
          connectivityStreamProvider.overrideWith((ref) {
            return Stream.value([ConnectivityResult.wifi]);
          }),
        ],
      );

      await tester.pumpWidget(
        UncontrolledProviderScope(
          container: container,
          child: MaterialApp(
            home: AppShell(
              child: Scaffold(
                body: const Center(child: Text('Test')),
              ),
            ),
          ),
        ),
      );

      await tester.pumpAndSettle();

      // Verify no badge is displayed for empty cart
      expect(find.byType(Badge), findsNothing);

      container.dispose();
    });
  });
}

// Mock CartService with empty cart
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

// Mock CartService with 3 items
class MockCartServiceWithItems implements CartService {
  @override
  List<CartItem> get items => [
        CartItem(
          itemId: 1,
          itemName: 'Item 1',
          itemCode: 'CODE1',
          quantity: 1,
          unitPriceUsd: 10.0,
          unitPriceKhr: 40000.0,
          appliedSaleId: 1,
        ),
        CartItem(
          itemId: 2,
          itemName: 'Item 2',
          itemCode: 'CODE2',
          quantity: 2,
          unitPriceUsd: 15.0,
          unitPriceKhr: 60000.0,
          appliedSaleId: 2,
        ),
        CartItem(
          itemId: 3,
          itemName: 'Item 3',
          itemCode: 'CODE3',
          quantity: 1,
          unitPriceUsd: 20.0,
          unitPriceKhr: 80000.0,
          appliedSaleId: 3,
        ),
      ];

  @override
  double get total => 65.0;

  @override
  Future<void> addItem(item, priceTiers) async {}

  @override
  Future<void> setQuantity(int itemId, int quantity, priceTiers) async {}

  @override
  Future<void> removeItem(int itemId) async {}

  @override
  Future<void> clear() async {}

  @override
  int quantityFor(int itemId) {
    final item = items.firstWhere(
      (i) => i.itemId == itemId,
      orElse: () => CartItem(
        itemId: -1,
        itemName: '',
        itemCode: '',
        quantity: 0,
        unitPriceUsd: 0.0,
        unitPriceKhr: 0.0,
        appliedSaleId: 0,
      ),
    );
    return item.quantity;
  }
}
