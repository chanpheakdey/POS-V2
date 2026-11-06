import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../auth/auth_service.dart';
import '../../order/order_service.dart';
import '../../shared/models/order.dart';
import '../../routing/routes.dart';

/// Order history screen displaying customer orders.
/// 
/// Features:
/// - Retrieves orders via OrderService.getOrderHistory
/// - Displays OrderID, InvoiceNo, CreatedDate, Total
/// - Sorted descending by CreatedDate (newest first)
/// - Supports pagination/infinite scroll for large lists
/// - Tap to navigate to order detail
/// - Filters out deleted orders
/// 
/// **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.6**
class OrdersScreen extends ConsumerStatefulWidget {
  const OrdersScreen({super.key});

  @override
  ConsumerState<OrdersScreen> createState() => _OrdersScreenState();
}

class _OrdersScreenState extends ConsumerState<OrdersScreen> {
  final ScrollController _scrollController = ScrollController();
  List<Order> _orders = [];
  bool _isLoading = false;
  bool _hasError = false;
  String _errorMessage = '';
  int _currentPage = 0;
  static const int _pageSize = 20;
  bool _hasMoreData = true;

  @override
  void initState() {
    super.initState();
    _scrollController.addListener(_onScroll);
    _loadOrders();
  }

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  void _onScroll() {
    // Trigger pagination when user scrolls near the bottom
    if (_scrollController.position.pixels >=
        _scrollController.position.maxScrollExtent * 0.8) {
      if (!_isLoading && _hasMoreData) {
        _loadMoreOrders();
      }
    }
  }

  Future<void> _loadOrders() async {
    if (_isLoading) return;

    setState(() {
      _isLoading = true;
      _hasError = false;
      _errorMessage = '';
    });

    try {
      final authService = await ref.read(authServiceProvider.future);
      final userId = authService.currentUserId;

      if (userId == null) {
        setState(() {
          _hasError = true;
          _errorMessage = 'User not authenticated';
          _isLoading = false;
        });
        return;
      }

      final orderService = ref.read(orderServiceProvider.notifier);
      final orders = await orderService.getOrderHistory(userId);

      setState(() {
        _orders = orders.take(_pageSize).toList();
        _currentPage = 1;
        _hasMoreData = orders.length > _pageSize;
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

  Future<void> _loadMoreOrders() async {
    if (_isLoading || !_hasMoreData) return;

    setState(() {
      _isLoading = true;
    });

    try {
      final authService = await ref.read(authServiceProvider.future);
      final userId = authService.currentUserId;

      if (userId == null) {
        setState(() {
          _isLoading = false;
        });
        return;
      }

      final orderService = ref.read(orderServiceProvider.notifier);
      final allOrders = await orderService.getOrderHistory(userId);

      final startIndex = _currentPage * _pageSize;
      final endIndex = startIndex + _pageSize;

      if (startIndex < allOrders.length) {
        final moreOrders = allOrders.skip(startIndex).take(_pageSize).toList();
        setState(() {
          _orders.addAll(moreOrders);
          _currentPage++;
          _hasMoreData = endIndex < allOrders.length;
          _isLoading = false;
        });
      } else {
        setState(() {
          _hasMoreData = false;
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _isLoading = false;
      });
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error loading more orders: $e')),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Order History'),
        elevation: 2,
      ),
      body: _buildBody(),
    );
  }

  Widget _buildBody() {
    if (_isLoading && _orders.isEmpty) {
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
              'Failed to load orders',
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
              onPressed: _loadOrders,
              icon: const Icon(Icons.refresh),
              label: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    if (_orders.isEmpty) {
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
              'No orders yet',
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 8),
            Text(
              'Your order history will appear here',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Colors.grey,
                  ),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadOrders,
      child: ListView.builder(
        controller: _scrollController,
        padding: const EdgeInsets.all(8),
        itemCount: _orders.length + (_hasMoreData ? 1 : 0),
        itemBuilder: (context, index) {
          if (index == _orders.length) {
            // Loading indicator at the bottom
            return const Padding(
              padding: EdgeInsets.all(16.0),
              child: Center(
                child: CircularProgressIndicator(),
              ),
            );
          }

          final order = _orders[index];
          return _OrderCard(
            order: order,
            onTap: () => _navigateToOrderDetail(order.orderId),
          );
        },
      ),
    );
  }

  void _navigateToOrderDetail(int orderId) {
    context.push(Routes.orderDetail(orderId));
  }
}

/// Card widget displaying order summary information.
/// 
/// Shows:
/// - Order ID and Invoice Number
/// - Created Date (formatted)
/// - Total amount in USD and KHR
/// 
/// Tappable to navigate to order detail screen.
class _OrderCard extends StatelessWidget {
  final Order order;
  final VoidCallback onTap;

  const _OrderCard({
    required this.order,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final dateFormat = DateFormat('MMM dd, yyyy HH:mm');
    final formattedDate = dateFormat.format(order.createdDate);
    
    // Calculate KHR amount (assuming 4000 KHR per USD as a typical rate)
    // In production, this should come from the backend or be configurable
    const double exchangeRate = 4000.0;
    final totalKhr = order.total * exchangeRate;

    return Card(
      margin: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
      elevation: 2,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Order ID and Invoice Number
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Row(
                    children: [
                      const Icon(
                        Icons.receipt,
                        size: 20,
                        color: Colors.blue,
                      ),
                      const SizedBox(width: 8),
                      Text(
                        'Order #${order.orderId}',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                    ],
                  ),
                  const Icon(
                    Icons.chevron_right,
                    color: Colors.grey,
                  ),
                ],
              ),
              const SizedBox(height: 8),
              
              // Invoice Number
              Row(
                children: [
                  const Icon(
                    Icons.tag,
                    size: 16,
                    color: Colors.grey,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    'Invoice: ${order.invoiceNo}',
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey[700],
                        ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              
              // Created Date
              Row(
                children: [
                  const Icon(
                    Icons.calendar_today,
                    size: 16,
                    color: Colors.grey,
                  ),
                  const SizedBox(width: 8),
                  Text(
                    formattedDate,
                    style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey[700],
                        ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              
              // Divider
              const Divider(),
              const SizedBox(height: 8),
              
              // Total amount
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Total:',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w500,
                        ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        '\$${order.total.toStringAsFixed(2)}',
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Colors.green[700],
                            ),
                      ),
                      Text(
                        '៛${totalKhr.toStringAsFixed(0)}',
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
      ),
    );
  }
}
