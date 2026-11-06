import 'dart:convert';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../api/api_client.dart';
import '../api/api_client_provider.dart';
import '../api/api_exception.dart';
import '../api/dtos/order_dto.dart';
import '../api/dtos/order_payload_dto.dart';
import '../shared/local_file_store.dart';
import '../shared/models/cart_item.dart';
import '../shared/models/order.dart';
import '../shared/models/order_detail.dart';
import 'order_result.dart';

part 'order_service.g.dart';

/// Service for managing order placement and order history retrieval.
/// 
/// Handles both online and offline order submission:
/// - Online: submits directly to backend via ApiClient
/// - Offline: queues orders in local file for later submission
/// 
/// **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 8.1, 8.2, 8.5, 10.8, 10.11**
@riverpod
class OrderService extends _$OrderService {
  static const String _queuePath = 'orders/queue.json';

  @override
  FutureOr<void> build() async {
    // Initialize service
  }

  /// Place an order with the given items.
  /// 
  /// - If online: submits order to backend and returns OrderSuccess
  /// - If offline: queues order locally and returns OrderQueued
  /// - On validation error: returns OrderFailure
  /// - On network error: returns OrderNetworkError
  /// 
  /// **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 10.8, 10.11**
  Future<OrderResult> placeOrder(int customerId, List<CartItem> items) async {
    // Validate cart is not empty
    if (items.isEmpty) {
      return const OrderFailure('Cart is empty');
    }

    // Calculate total
    final total = items.fold<double>(
      0.0,
      (sum, item) => sum + (item.quantity * item.unitPriceUsd),
    );

    // Build order payload
    final payload = OrderPayloadDto(
      customerId: customerId,
      items: items.map((item) {
        return OrderItemPayloadDto(
          itemId: item.itemId,
          itemName: item.itemName,
          itemCode: item.itemCode,
          quantity: item.quantity,
          unitPrice: item.unitPriceUsd,
          saleId: item.appliedSaleId,
        );
      }).toList(),
      total: total,
    );

    // Try to submit online first
    try {
      final apiClient = ref.read(apiClientProvider);
      final result = await apiClient.submitOrder(payload);
      
      return OrderSuccess(
        orderId: result.orderId,
        invoiceNo: result.invoiceNo,
        createdDate: result.createdDate,
      );
    } on ApiTimeoutException catch (_) {
      // Queue for offline submission
      await queueOfflineOrder(payload);
      return const OrderNetworkError();
    } on ApiNetworkException catch (_) {
      // Queue for offline submission
      await queueOfflineOrder(payload);
      return const OrderNetworkError();
    } on ApiException catch (e) {
      // API-level error (validation, etc.)
      return OrderFailure(e.message);
    } catch (e) {
      // Unexpected error
      return OrderFailure('An unexpected error occurred: $e');
    }
  }

  /// Retrieve order history for a customer.
  /// 
  /// Filters out deleted orders and sorts by createdDate descending.
  /// 
  /// **Validates: Requirements 8.1, 8.2, 8.5**
  Future<List<Order>> getOrderHistory(int customerId) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      final orderDtos = await apiClient.getOrders(customerId);
      
      // Convert DTOs to domain models
      final orders = orderDtos.map((dto) {
        return Order(
          orderId: dto.orderId,
          customerId: dto.customerId,
          invoiceNo: dto.invoiceNo,
          createdDate: dto.createdDate,
          total: dto.total,
          deleted: dto.deleted,
        );
      }).toList();
      
      // Filter out deleted orders
      final activeOrders = orders.where((order) => !order.deleted).toList();
      
      // Sort by createdDate descending (newest first)
      activeOrders.sort((a, b) => b.createdDate.compareTo(a.createdDate));
      
      return activeOrders;
    } on ApiException catch (e) {
      throw Exception('Failed to fetch order history: ${e.message}');
    } catch (e) {
      throw Exception('An unexpected error occurred: $e');
    }
  }

  /// Retrieve order details (line items) for a specific order.
  /// 
  /// Returns a list of OrderDetail items with ItemName, ItemCode, quantity, unit price, and line total.
  /// 
  /// **Validates: Requirements 8.4**
  Future<List<OrderDetail>> getOrderDetails(int orderId) async {
    try {
      final apiClient = ref.read(apiClientProvider);
      final orderDetailDtos = await apiClient.getOrderDetails(orderId);
      
      // Convert DTOs to domain models
      final orderDetails = orderDetailDtos.map((dto) {
        return OrderDetail(
          orderId: dto.orderId,
          itemId: dto.itemId,
          itemName: dto.itemName,
          itemCode: dto.itemCode,
          quantity: dto.quantity,
          unitPrice: dto.unitPrice,
          lineTotal: dto.lineTotal,
        );
      }).toList();
      
      return orderDetails;
    } on ApiException catch (e) {
      throw Exception('Failed to fetch order details: ${e.message}');
    } catch (e) {
      throw Exception('An unexpected error occurred: $e');
    }
  }

  /// Queue an order for offline submission.
  /// 
  /// Appends the order payload to the local queue file.
  /// 
  /// **Validates: Requirements 10.11**
  Future<void> queueOfflineOrder(OrderPayloadDto payload) async {
    try {
      final localFileStore = ref.read(localFileStoreProvider);
      
      // Read existing queue
      final queue = await localFileStore.readList<Map<String, dynamic>>(
        _queuePath,
        (json) => json,
      );
      
      // Add timestamp to payload
      final queueItem = {
        ...payload.toJson(),
        'queuedAt': DateTime.now().toIso8601String(),
      };
      
      // Append to queue
      queue.add(queueItem);
      
      // Write back to file
      await localFileStore.writeList<Map<String, dynamic>>(
        _queuePath,
        queue,
        (item) => item,
      );
    } catch (e) {
      // Silently fail - offline queue is a best-effort feature
      // Log error in production app
    }
  }

  /// Flush the offline order queue.
  /// 
  /// Processes queued orders in FIFO order, submitting each to the backend.
  /// Successfully submitted orders are removed from the queue.
  /// 
  /// **Validates: Requirements 10.11**
  Future<void> flushOfflineQueue() async {
    try {
      final localFileStore = ref.read(localFileStoreProvider);
      final apiClient = ref.read(apiClientProvider);
      
      // Read queue
      final queue = await localFileStore.readList<Map<String, dynamic>>(
        _queuePath,
        (json) => json,
      );
      
      if (queue.isEmpty) {
        return;
      }
      
      final failedItems = <Map<String, dynamic>>[];
      
      // Process each queued order in FIFO order
      for (final queueItem in queue) {
        try {
          // Remove queuedAt timestamp before submission
          final payloadJson = Map<String, dynamic>.from(queueItem);
          payloadJson.remove('queuedAt');
          
          // Reconstruct payload
          final payload = OrderPayloadDto(
            customerId: payloadJson['CustomerID'] as int,
            items: (payloadJson['Items'] as List<dynamic>).map((item) {
              final itemMap = item as Map<String, dynamic>;
              return OrderItemPayloadDto(
                itemId: itemMap['ItemID'] as int,
                itemName: itemMap['ItemName'] as String,
                itemCode: itemMap['ItemCode'] as String,
                quantity: itemMap['Quantity'] as int,
                unitPrice: (itemMap['UnitPrice'] as num).toDouble(),
                saleId: itemMap['SaleID'] as int,
              );
            }).toList(),
            total: (payloadJson['Total'] as num).toDouble(),
          );
          
          // Try to submit
          await apiClient.submitOrder(payload);
          
          // If successful, don't add to failed items (removed from queue)
        } catch (e) {
          // If submission fails, keep in queue
          failedItems.add(queueItem);
        }
      }
      
      // Write back only the failed items
      await localFileStore.writeList<Map<String, dynamic>>(
        _queuePath,
        failedItems,
        (item) => item,
      );
    } catch (e) {
      // Silently fail - queue flush is a best-effort feature
      // Log error in production app
    }
  }
}
