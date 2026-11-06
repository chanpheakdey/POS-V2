import 'package:flutter_test/flutter_test.dart';
import 'package:mocktail/mocktail.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/api/api_client.dart';
import 'package:pos_app/src/api/api_client_provider.dart';
import 'package:pos_app/src/api/api_exception.dart';
import 'package:pos_app/src/api/dtos/order_dto.dart';
import 'package:pos_app/src/api/dtos/order_payload_dto.dart';
import 'package:pos_app/src/api/dtos/order_result_dto.dart';
import 'package:pos_app/src/order/order_result.dart';
import 'package:pos_app/src/order/order_service.dart';
import 'package:pos_app/src/shared/local_file_store.dart';
import 'package:pos_app/src/shared/models/cart_item.dart';
import 'package:pos_app/src/shared/models/order.dart';

class MockApiClient extends Mock implements ApiClient {}
class MockLocalFileStore extends Mock implements LocalFileStore {}

void main() {
  late MockApiClient mockApiClient;
  late MockLocalFileStore mockLocalFileStore;
  late ProviderContainer container;

  setUp(() {
    mockApiClient = MockApiClient();
    mockLocalFileStore = MockLocalFileStore();

    container = ProviderContainer(
      overrides: [
        apiClientProvider.overrideWithValue(mockApiClient),
        localFileStoreProvider.overrideWithValue(mockLocalFileStore),
      ],
    );

    // Register fallback values for mocktail
    registerFallbackValue(
      OrderPayloadDto(
        customerId: 1,
        items: [],
        total: 0.0,
      ),
    );
  });

  tearDown(() {
    container.dispose();
  });

  group('OrderService.placeOrder', () {
    test('returns OrderSuccess when order is submitted successfully online', () async {
      // Arrange
      final cartItems = [
        CartItem(
          itemId: 1,
          itemName: 'Test Item',
          itemCode: 'TEST001',
          quantity: 2,
          unitPriceUsd: 10.0,
          unitPriceKhr: 40000.0,
          appliedSaleId: 1,
        ),
      ];

      final expectedResult = OrderResultDto(
        orderId: 123,
        invoiceNo: 'INV-001',
        createdDate: DateTime(2024, 1, 1),
      );

      when(() => mockApiClient.submitOrder(any()))
          .thenAnswer((_) async => expectedResult);

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final result = await service.placeOrder(1, cartItems);

      // Assert
      expect(result, isA<OrderSuccess>());
      final success = result as OrderSuccess;
      expect(success.orderId, 123);
      expect(success.invoiceNo, 'INV-001');
      expect(success.createdDate, DateTime(2024, 1, 1));

      verify(() => mockApiClient.submitOrder(any())).called(1);
    });

    test('returns OrderFailure when cart is empty', () async {
      // Arrange
      final service = container.read(orderServiceProvider.notifier);

      // Act
      final result = await service.placeOrder(1, []);

      // Assert
      expect(result, isA<OrderFailure>());
      expect((result as OrderFailure).message, 'Cart is empty');

      verifyNever(() => mockApiClient.submitOrder(any()));
    });

    test('returns OrderNetworkError and queues order when network fails', () async {
      // Arrange
      final cartItems = [
        CartItem(
          itemId: 1,
          itemName: 'Test Item',
          itemCode: 'TEST001',
          quantity: 1,
          unitPriceUsd: 10.0,
          unitPriceKhr: 40000.0,
          appliedSaleId: 1,
        ),
      ];

      when(() => mockApiClient.submitOrder(any()))
          .thenThrow(ApiNetworkException(endpoint: 'test'));

      when(() => mockLocalFileStore.readList<Map<String, dynamic>>(
            any(),
            any(),
          )).thenAnswer((_) async => []);

      when(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            any(),
            any(),
          )).thenAnswer((_) async {});

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final result = await service.placeOrder(1, cartItems);

      // Assert
      expect(result, isA<OrderNetworkError>());

      verify(() => mockApiClient.submitOrder(any())).called(1);
      verify(() => mockLocalFileStore.readList<Map<String, dynamic>>(
            any(),
            any(),
          )).called(1);
      verify(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            any(),
            any(),
          )).called(1);
    });

    test('calculates total correctly from cart items', () async {
      // Arrange
      final cartItems = [
        CartItem(
          itemId: 1,
          itemName: 'Item 1',
          itemCode: 'TEST001',
          quantity: 2,
          unitPriceUsd: 10.0,
          unitPriceKhr: 40000.0,
          appliedSaleId: 1,
        ),
        CartItem(
          itemId: 2,
          itemName: 'Item 2',
          itemCode: 'TEST002',
          quantity: 3,
          unitPriceUsd: 5.0,
          unitPriceKhr: 20000.0,
          appliedSaleId: 2,
        ),
      ];

      OrderPayloadDto? capturedPayload;
      when(() => mockApiClient.submitOrder(any())).thenAnswer((invocation) async {
        capturedPayload = invocation.positionalArguments[0] as OrderPayloadDto;
        return OrderResultDto(
          orderId: 123,
          invoiceNo: 'INV-001',
          createdDate: DateTime.now(),
        );
      });

      final service = container.read(orderServiceProvider.notifier);

      // Act
      await service.placeOrder(1, cartItems);

      // Assert
      expect(capturedPayload, isNotNull);
      expect(capturedPayload!.total, 35.0); // (2 * 10) + (3 * 5) = 35
      expect(capturedPayload!.items.length, 2);
    });
  });

  group('OrderService.getOrderHistory', () {
    test('returns orders sorted by createdDate descending', () async {
      // Arrange
      final orderDtos = [
        OrderDto(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 1),
          total: 100.0,
          deleted: false,
        ),
        OrderDto(
          orderId: 2,
          customerId: 1,
          invoiceNo: 'INV-002',
          createdDate: DateTime(2024, 1, 3),
          total: 200.0,
          deleted: false,
        ),
        OrderDto(
          orderId: 3,
          customerId: 1,
          invoiceNo: 'INV-003',
          createdDate: DateTime(2024, 1, 2),
          total: 150.0,
          deleted: false,
        ),
      ];

      when(() => mockApiClient.getOrders(1))
          .thenAnswer((_) async => orderDtos);

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orders = await service.getOrderHistory(1);

      // Assert
      expect(orders.length, 3);
      expect(orders[0].orderId, 2); // Most recent first
      expect(orders[1].orderId, 3);
      expect(orders[2].orderId, 1); // Oldest last
    });

    test('filters out deleted orders', () async {
      // Arrange
      final orderDtos = [
        OrderDto(
          orderId: 1,
          customerId: 1,
          invoiceNo: 'INV-001',
          createdDate: DateTime(2024, 1, 1),
          total: 100.0,
          deleted: false,
        ),
        OrderDto(
          orderId: 2,
          customerId: 1,
          invoiceNo: 'INV-002',
          createdDate: DateTime(2024, 1, 2),
          total: 200.0,
          deleted: true, // Deleted order
        ),
        OrderDto(
          orderId: 3,
          customerId: 1,
          invoiceNo: 'INV-003',
          createdDate: DateTime(2024, 1, 3),
          total: 150.0,
          deleted: false,
        ),
      ];

      when(() => mockApiClient.getOrders(1))
          .thenAnswer((_) async => orderDtos);

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orders = await service.getOrderHistory(1);

      // Assert
      expect(orders.length, 2);
      expect(orders.any((o) => o.orderId == 2), false); // Deleted order not included
      expect(orders[0].orderId, 3);
      expect(orders[1].orderId, 1);
    });

    test('returns empty list when no orders exist', () async {
      // Arrange
      when(() => mockApiClient.getOrders(1))
          .thenAnswer((_) async => []);

      final service = container.read(orderServiceProvider.notifier);

      // Act
      final orders = await service.getOrderHistory(1);

      // Assert
      expect(orders, isEmpty);
    });
  });

  group('OrderService.flushOfflineQueue', () {
    test('processes queued orders in FIFO order', () async {
      // Arrange
      final queuedOrders = [
        {
          'CustomerID': 1,
          'Items': [
            {
              'ItemID': 1,
              'ItemName': 'Item 1',
              'ItemCode': 'TEST001',
              'Quantity': 1,
              'UnitPrice': 10.0,
              'SaleID': 1,
            }
          ],
          'Total': 10.0,
          'queuedAt': DateTime(2024, 1, 1).toIso8601String(),
        },
        {
          'CustomerID': 1,
          'Items': [
            {
              'ItemID': 2,
              'ItemName': 'Item 2',
              'ItemCode': 'TEST002',
              'Quantity': 2,
              'UnitPrice': 5.0,
              'SaleID': 2,
            }
          ],
          'Total': 10.0,
          'queuedAt': DateTime(2024, 1, 2).toIso8601String(),
        },
      ];

      when(() => mockLocalFileStore.readList<Map<String, dynamic>>(
            any(),
            any(),
          )).thenAnswer((_) async => queuedOrders);

      when(() => mockApiClient.submitOrder(any())).thenAnswer((_) async {
        return OrderResultDto(
          orderId: 123,
          invoiceNo: 'INV-001',
          createdDate: DateTime.now(),
        );
      });

      when(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            any(),
            any(),
          )).thenAnswer((_) async {});

      final service = container.read(orderServiceProvider.notifier);

      // Act
      await service.flushOfflineQueue();

      // Assert
      verify(() => mockApiClient.submitOrder(any())).called(2);
      verify(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            [],
            any(),
          )).called(1); // Empty queue written back
    });

    test('keeps failed orders in queue', () async {
      // Arrange
      final queuedOrders = [
        {
          'CustomerID': 1,
          'Items': [
            {
              'ItemID': 1,
              'ItemName': 'Item 1',
              'ItemCode': 'TEST001',
              'Quantity': 1,
              'UnitPrice': 10.0,
              'SaleID': 1,
            }
          ],
          'Total': 10.0,
          'queuedAt': DateTime(2024, 1, 1).toIso8601String(),
        },
      ];

      when(() => mockLocalFileStore.readList<Map<String, dynamic>>(
            any(),
            any(),
          )).thenAnswer((_) async => queuedOrders);

      when(() => mockApiClient.submitOrder(any()))
          .thenThrow(ApiNetworkException(endpoint: 'test'));

      when(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            any(),
            any(),
          )).thenAnswer((_) async {});

      final service = container.read(orderServiceProvider.notifier);

      // Act
      await service.flushOfflineQueue();

      // Assert
      verify(() => mockApiClient.submitOrder(any())).called(1);
      
      // Verify that the failed order is written back to the queue
      final captured = verify(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            captureAny(),
            any(),
          )).captured;
      
      expect(captured.length, 1);
      final writtenQueue = captured[0] as List;
      expect(writtenQueue.length, 1); // Failed order kept in queue
    });

    test('does nothing when queue is empty', () async {
      // Arrange
      when(() => mockLocalFileStore.readList<Map<String, dynamic>>(
            any(),
            any(),
          )).thenAnswer((_) async => []);

      final service = container.read(orderServiceProvider.notifier);

      // Act
      await service.flushOfflineQueue();

      // Assert
      verifyNever(() => mockApiClient.submitOrder(any()));
      verifyNever(() => mockLocalFileStore.writeList<Map<String, dynamic>>(
            any(),
            any(),
            any(),
          ));
    });
  });
}
