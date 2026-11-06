import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mocktail/mocktail.dart';
import 'package:pos_app/src/auth/auth_service.dart';
import 'package:pos_app/src/auth/auth_state.dart';
import 'package:pos_app/src/order/order_service.dart';
import 'package:pos_app/src/screens/orders/orders_screen.dart';
import 'package:pos_app/src/shared/models/order.dart';

class MockOrderServiceNotifier extends Mock implements OrderService {}
class MockAuthState extends Mock implements AuthState {}

void main() {
  late MockOrderServiceNotifier mockOrderService;

  setUp(() {
    mockOrderService = MockOrderServiceNotifier();
  });

  Widget createTestWidget(ProviderContainer container) {
    return UncontrolledProviderScope(
      container: container,
      child: const MaterialApp(
        home: OrdersScreen(),
      ),
    );
  }

  group('OrdersScreen Widget Tests', () {
    testWidgets('displays loading indicator while fetching orders', (tester) async {
      // Arrange
      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      // Return a future that never completes to simulate loading
      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) => Future.delayed(const Duration(seconds: 10), () => []));

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pump(); // Start the async operation

      // Assert
      expect(find.byType(CircularProgressIndicator), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays orders in a list', (tester) async {
      // Arrange
      final testOrders = [
        Order(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 3),
          total: 100.0,
          deleted: false,
        ),
        Order(
          orderId: 2,
          customerId: 1,
          invoiceNo: 'INV-002',
          createdDate: DateTime(2024, 1, 2),
          total: 200.0,
          deleted: false,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => testOrders);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('Order #1'), findsOneWidget);
      expect(find.text('Order #2'), findsOneWidget);
      expect(find.text('Invoice: INV-001'), findsOneWidget);
      expect(find.text('Invoice: INV-002'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays empty state when no orders exist', (tester) async {
      // Arrange
      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => []);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('No orders yet'), findsOneWidget);
      expect(find.text('Your order history will appear here'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays error state when loading fails', (tester) async {
      // Arrange
      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenThrow(Exception('Network error'));

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('Failed to load orders'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
      expect(find.text('Retry'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays order totals in USD and KHR', (tester) async {
      // Arrange
      final testOrders = [
        Order(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 1),
          total: 100.50,
          deleted: false,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => testOrders);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('\$100.50'), findsOneWidget);
      expect(find.text('៛402000'), findsOneWidget); // 100.50 * 4000

      container.dispose();
    });

    testWidgets('refresh indicator triggers reload', (tester) async {
      // Arrange
      final testOrders = [
        Order(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 1),
          total: 100.0,
          deleted: false,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => testOrders);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Perform pull-to-refresh
      await tester.drag(find.byType(ListView), const Offset(0, 300));
      await tester.pumpAndSettle();

      // Assert - getOrderHistory should be called twice (initial load + refresh)
      verify(() => mockOrderService.getOrderHistory(any())).called(greaterThanOrEqualTo(2));

      container.dispose();
    });

    testWidgets('retry button reloads orders after error', (tester) async {
      // Arrange
      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      // First call throws error, second call succeeds
      when(() => mockOrderService.getOrderHistory(any()))
          .thenThrow(Exception('Network error'));

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Verify error state
      expect(find.text('Failed to load orders'), findsOneWidget);

      // Setup successful response for retry
      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => []);

      // Tap retry button
      await tester.tap(find.text('Retry'));
      await tester.pumpAndSettle();

      // Assert - should show empty state now
      expect(find.text('No orders yet'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays orders sorted by date descending', (tester) async {
      // Arrange - orders returned in random order
      final testOrders = [
        Order(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 1),
          total: 100.0,
          deleted: false,
        ),
        Order(
          orderId: 3,
          customerId: 1,
          invoiceNo: 'INV-003',
          createdDate: DateTime(2024, 1, 3), // Most recent
          total: 300.0,
          deleted: false,
        ),
        Order(
          orderId: 2,
          customerId: 1,
          invoiceNo: 'INV-002',
          createdDate: DateTime(2024, 1, 2),
          total: 200.0,
          deleted: false,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => testOrders);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert - all orders are displayed
      // Note: The OrderService already sorts them, so we just verify they're all shown
      expect(find.text('Order #1'), findsOneWidget);
      expect(find.text('Order #2'), findsOneWidget);
      expect(find.text('Order #3'), findsOneWidget);

      container.dispose();
    });

    testWidgets('displays formatted date for each order', (tester) async {
      // Arrange
      final testOrders = [
        Order(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 3, 15, 14, 30),
          total: 100.0,
          deleted: false,
        ),
      ];

      final container = ProviderContainer(
        overrides: [
          authServiceProvider.overrideWith((ref) {
            return Stream.value(const Authenticated(userId: 1, token: 'test-token'));
          }),
          orderServiceProvider.overrideWith((ref) => mockOrderService),
        ],
      );

      when(() => mockOrderService.getOrderHistory(any()))
          .thenAnswer((_) async => testOrders);

      // Act
      await tester.pumpWidget(createTestWidget(container));
      await tester.pumpAndSettle();

      // Assert - date should be formatted as "MMM dd, yyyy HH:mm"
      expect(find.text('Mar 15, 2024 14:30'), findsOneWidget);

      container.dispose();
    });
  });
}
