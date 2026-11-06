import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../order/order_service.dart';
import '../../shared/models/order_detail.dart';

/// Order detail screen displaying the full item breakdown of an order.
/// 
/// Features:
/// - Displays ItemName, ItemCode, quantity, unit price, line total for each item
/// - Shows grand total at the bottom
/// - Supports pull-to-refresh
/// 
/// **Validates: Requirements 8.4**
class OrderDetailScreen extends ConsumerStatefulWidget {
  final int orderId;
  
  const OrderDetailScreen({
    super.key,
    required this.orderId,
  });

  @override
  ConsumerState<OrderDetailScreen> createState() => _OrderDetailScreenState();
}

class _OrderDetailScreenState extends ConsumerState<OrderDetailScreen> {
  List<OrderDetail> _orderDetails = [];
  bool _isLoading = false;
  bool _hasError = false;
  String _errorMessage = '';

  @override
  void initState() {
    super.initState();
    _loadOrderDetails();
  }

  Future<void> _loadOrderDetails() async {
    if (_isLoading) return;

    setState(() {
      _isLoading = true;
      _hasError = false;
      _errorMessage = '';
    });

    try {
      final orderService = ref.read(orderServiceProvider.notifier);
      final orderDetails = await orderService.getOrderDetails(widget.orderId);

      setState(() {
        _orderDetails = orderDetails;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _hasError = true;
        _errorMessage = e.toString();
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Order #${widget.orderId}'),
        elevation: 2,
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _orderDetails.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    if (_hasError) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.error_outline,
              size: 64,
              color: Colors.red,
            ),
            const SizedBox(height: 16),
            Text(
              'Failed to load order details',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 32),
              child: Text(
                _errorMessage,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium,
              ),
            ),
            const SizedBox(height: 24),
            ElevatedButton.icon(
              onPressed: _loadOrderDetails,
              icon: const Icon(Icons.refresh),
              label: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    if (_orderDetails.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.receipt_long,
              size: 64,
              color: Colors.grey,
            ),
            const SizedBox(height: 16),
            Text(
              'No items in this order',
              style: Theme.of(context).textTheme.titleLarge,
            ),
          ],
        ),
      );
    }

    // Calculate grand total
    final grandTotal = _orderDetails.fold<double>(
      0.0,
      (sum, detail) => sum + detail.lineTotal,
    );
    
    // Calculate KHR amount (assuming 4000 KHR per USD as a typical rate)
    const double exchangeRate = 4000.0;
    final grandTotalKhr = grandTotal * exchangeRate;

    return RefreshIndicator(
      onRefresh: _loadOrderDetails,
      child: Column(
        children: [
          // Order items list
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.all(8),
              itemCount: _orderDetails.length,
              itemBuilder: (context, index) {
                final detail = _orderDetails[index];
                return _OrderDetailCard(detail: detail);
              },
            ),
          ),
          
          // Grand total section
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              boxShadow: [
                BoxShadow(
                  color: Colors.black.withOpacity(0.1),
                  blurRadius: 4,
                  offset: const Offset(0, -2),
                ),
              ],
            ),
            padding: const EdgeInsets.all(16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Grand Total:',
                  style: Theme.of(context).textTheme.titleLarge?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '\$${grandTotal.toStringAsFixed(2)}',
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: Colors.green[700],
                          ),
                    ),
                    Text(
                      '៛${grandTotalKhr.toStringAsFixed(0)}',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: Colors.grey[600],
                          ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// Card widget displaying a single order detail line item.
/// 
/// Shows:
/// - Item Name and Code
/// - Quantity
/// - Unit Price (USD and KHR)
/// - Line Total (USD and KHR)
class _OrderDetailCard extends StatelessWidget {
  final OrderDetail detail;

  const _OrderDetailCard({
    required this.detail,
  });

  @override
  Widget build(BuildContext context) {
    // Calculate KHR amounts
    const double exchangeRate = 4000.0;
    final unitPriceKhr = detail.unitPrice * exchangeRate;
    final lineTotalKhr = detail.lineTotal * exchangeRate;

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Item name
            Text(
              detail.itemName,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 4),
            
            // Item code
            Text(
              'Code: ${detail.itemCode}',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Colors.grey[600],
                  ),
            ),
            const SizedBox(height: 12),
            
            // Quantity and unit price row
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                // Quantity
                Row(
                  children: [
                    const Icon(
                      Icons.shopping_basket,
                      size: 16,
                      color: Colors.blue,
                    ),
                    const SizedBox(width: 4),
                    Text(
                      'Qty: ${detail.quantity}',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  ],
                ),
                
                // Unit price
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '\$${detail.unitPrice.toStringAsFixed(2)} / unit',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            fontWeight: FontWeight.w500,
                          ),
                    ),
                    Text(
                      '៛${unitPriceKhr.toStringAsFixed(0)} / unit',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Colors.grey[600],
                          ),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: 12),
            
            // Divider
            const Divider(),
            const SizedBox(height: 8),
            
            // Line total
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Line Total:',
                  style: Theme.of(context).textTheme.titleSmall?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      '\$${detail.lineTotal.toStringAsFixed(2)}',
                      style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold,
                            color: Colors.green[700],
                          ),
                    ),
                    Text(
                      '៛${lineTotalKhr.toStringAsFixed(0)}',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Colors.grey[600],
                          ),
                    ),
                  ],
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
