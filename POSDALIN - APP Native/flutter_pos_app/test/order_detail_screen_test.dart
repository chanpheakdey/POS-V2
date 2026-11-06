import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/api/api_client.dart';
import 'package:pos_app/src/api/api_client_provider.dart';
import 'package:pos_app/src/api/dtos/order_dto.dart';
import 'package:pos_app/src/order/order_service.dart';
import 'package:pos_app/src/screens/orders/order_detail_screen.dart';
import 'package:pos_app/src/shared/local_file_store.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockLocalFileStore extends Mock implements LocalFileStore {}

void main() {
  late MockApiClient mockApiClient;
  late MockLocalFileStore mockLocalFileStore;

  setUp(() {
    mockApiClient = MockApiClient();
    mockLocalFileStore = MockLocalFileStore();
  });

  group('OrderService.getOrderDetails', () {
    test('returns order details with item breakdown', () async {
      // Arrange
      final orderDetailDtos = [
        OrderDetailDto(
          orderId: 123,
          itemId: 1,
          itemName: 'Test Item 1',
          itemCode: 'TEST001',
          quantity: 2,
          unitPrice: 10.0,
          lineTotal: 20.0,
        ),
        OrderDetailDto(
          orderId: 123,
          itemId: 2,
          itemName: 'Test Item 2',
          itemCode: 'TEST002',
          quantity: 3,
          unitPrice: 5.0,
          lineTotal: 15.0,
        ),
      ];

      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => orderDetailDtos);

      final container = ProviderContainer(
        overrides: [
          apiClientProvider.overrideWithValue(mockApiClient),
          localFileStoreProvider.overrideWithValue(mockLocalFileStore),
        ],
      );

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orderDetails = await service.getOrderDetails(123);

      // Assert
      expect(orderDetails.length, 2);
      
      // Verify first item
      expect(orderDetails[0].orderId, 123);
      expect(orderDetails[0].itemId, 1);
      expect(orderDetails[0].itemName, 'Test Item 1');
      expect(orderDetails[0].itemCode, 'TEST001');
      expect(orderDetails[0].quantity, 2);
      expect(orderDetails[0].unitPrice, 10.0);
      expect(orderDetails[0].lineTotal, 20.0);
      
      // Verify second item
      expect(orderDetails[1].orderId, 123);
      expect(orderDetails[1].itemId, 2);
      expect(orderDetails[1].itemName, 'Test Item 2');
      expect(orderDetails[1].itemCode, 'TEST002');
      expect(orderDetails[1].quantity, 3);
      expect(orderDetails[1].unitPrice, 5.0);
      expect(orderDetails[1].lineTotal, 15.0);

      verify(() => mockApiClient.getOrderDetails(123)).called(1);
      
      container.dispose();
    });

    test('returns empty list when order has no items', () async {
      // Arrange
      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => []);

      final container = ProviderContainer(
        overrides: [
          apiClientProvider.overrideWithValue(mockApiClient),
          localFileStoreProvider.overrideWithValue(mockLocalFileStore),
        ],
      );

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orderDetails = await service.getOrderDetails(123);

      // Assert
      expect(orderDetails, isEmpty);
      verify(() => mockApiClient.getOrderDetails(123)).called(1);
      
      container.dispose();
    });

    test('calculates line totals correctly', () async {
      // Arrange
      final orderDetailDtos = [
        OrderDetailDto(
          orderId: 123,
          itemId: 1,
          itemName: 'Item with tiered price',
          itemCode: 'TIER001',
          quantity: 10,
          unitPrice: 8.50,
          lineTotal: 85.0, // 10 * 8.50
        ),
      ];

      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => orderDetailDtos);

      final container = ProviderContainer(
        overrides: [
          apiClientProvider.overrideWithValue(mockApiClient),
          localFileStoreProvider.overrideWithValue(mockLocalFileStore),
        ],
      );

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orderDetails = await service.getOrderDetails(123);

      // Assert
      expect(orderDetails.length, 1);
      expect(orderDetails[0].quantity, 10);
      expect(orderDetails[0].unitPrice, 8.50);
      expect(orderDetails[0].lineTotal, 85.0);
      
      container.dispose();
    });
  });

  group('OrderDetailScreen Widget', () {
    testWidgets('displays loading indicator initially', (tester) async {
      // Arrange
      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async {
            await Future.delayed(const Duration(seconds: 1));
            return [];
          });

      // Act
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            apiClientProvider.overrideWithValue(mockApiClient),
            localFileStoreProvider.overrideWithValue(mockLocalFileStore),
          ],
          child: const MaterialApp(
            home: OrderDetailScreen(orderId: 123),
          ),
        ),
      );

      // Assert
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
    });

    testWidgets('displays order details after loading', (tester) async {
      // Arrange
      final orderDetailDtos = [
        OrderDetailDto(
          orderId: 123,
          itemId: 1,
          itemName: 'Test Item',
          itemCode: 'TEST001',
          quantity: 2,
          unitPrice: 10.0,
          lineTotal: 20.0,
        ),
      ];

      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => orderDetailDtos);

      // Act
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            apiClientProvider.overrideWithValue(mockApiClient),
            localFileStoreProvider.overrideWithValue(mockLocalFileStore),
          ],
          child: const MaterialApp(
            home: OrderDetailScreen(orderId: 123),
          ),
        ),
      );

      // Wait for loading to complete
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('Test Item'), findsOneWidget);
      expect(find.text('Code: TEST001'), findsOneWidget);
      expect(find.text('Qty: 2'), findsOneWidget);
      expect(find.textContaining('\$10.00'), findsOneWidget);
      expect(find.textContaining('\$20.00'), findsOneWidget);
      expect(find.text('Grand Total:'), findsOneWidget);
    });

    testWidgets('displays empty state when no items', (tester) async {
      // Arrange
      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => []);

      // Act
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            apiClientProvider.overrideWithValue(mockApiClient),
            localFileStoreProvider.overrideWithValue(mockLocalFileStore),
          ],
          child: const MaterialApp(
            home: OrderDetailScreen(orderId: 123),
          ),
        ),
      );

      // Wait for loading to complete
      await tester.pumpAndSettle();

      // Assert
      expect(find.text('No items in this order'), findsOneWidget);
    });

    testWidgets('calculates grand total correctly', (tester) async {
      // Arrange
      final orderDetailDtos = [
        OrderDetailDto(
          orderId: 123,
          itemId: 1,
          itemName: 'Item 1',
          itemCode: 'TEST001',
          quantity: 2,
          unitPrice: 10.0,
          lineTotal: 20.0,
        ),
        OrderDetailDto(
          orderId: 123,
          itemId: 2,
          itemName: 'Item 2',
          itemCode: 'TEST002',
          quantity: 3,
          unitPrice: 5.0,
          lineTotal: 15.0,
        ),
      ];

      when(() => mockApiClient.getOrderDetails(123))
          .thenAnswer((_) async => orderDetailDtos);

      // Act
      await tester.pumpWidget(
        ProviderScope(
          overrides: [
            apiClientProvider.overrideWithValue(mockApiClient),
            localFileStoreProvider.overrideWithValue(mockLocalFileStore),
          ],
          child: const MaterialApp(
            home: OrderDetailScreen(orderId: 123),
          ),
        ),
      );

      // Wait for loading to complete
      await tester.pumpAndSettle();

      // Assert - grand total should be 20.0 + 15.0 = 35.0
      expect(find.text('\$35.00'), findsOneWidget);
    });
  });
}
