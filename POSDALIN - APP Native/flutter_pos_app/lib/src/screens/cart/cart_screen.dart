import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import '../../cart/cart_service.dart';
import '../../catalog/catalog_service.dart';
import '../../order/order_service.dart';
import '../../order/order_result.dart';
import '../../auth/auth_service.dart';
import '../../auth/auth_state.dart';
import '../../shared/models/cart_item.dart';
import '../../routing/routes.dart';

/// Cart screen widget that displays all items in the shopping cart.
/// 
/// Features:
/// - List all cart items with name, code, quantity, unit price, line total
/// - Quantity adjustment controls (+/- buttons)
/// - Remove button per item
/// - Grand total display
/// - Empty cart state with "Your cart is empty" message
/// - Checkout button
/// 
/// **Validates: Requirements 19.1, 19.2, 19.3, 19.4, 19.5, 19.6, 19.7, 19.8, 19.10**
class CartScreen extends ConsumerWidget {
  const CartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cartAsync = ref.watch(cartProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Shopping Cart'),
        centerTitle: true,
      ),
      body: cartAsync.when(
        data: (cartService) {
          final items = cartService.items;

          // Empty cart state
          if (items.isEmpty) {
            return const _EmptyCartView();
          }

          // Cart with items
          return Column(
            children: [
              // Cart items list
              Expanded(
                child: ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: items.length,
                  separatorBuilder: (context, index) => const Divider(height: 32),
                  itemBuilder: (context, index) {
                    return _CartItemCard(cartItem: items[index]);
                  },
                ),
              ),
              
              // Grand total and checkout section
              _CartFooter(total: cartService.total),
            ],
          );
        },
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stack) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48, color: Colors.red),
              const SizedBox(height: 16),
              Text('Error loading cart: $error'),
            ],
          ),
        ),
      ),
    );
  }
}

/// Empty cart view displaying "Your cart is empty" message.
/// 
/// **Validates: Requirements 19.10**
class _EmptyCartView extends StatelessWidget {
  const _EmptyCartView();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.shopping_cart_outlined,
            size: 96,
            color: Colors.grey.shade400,
          ),
          const SizedBox(height: 24),
          Text(
            'Your cart is empty',
            style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  color: Colors.grey.shade600,
                ),
          ),
          const SizedBox(height: 16),
          ElevatedButton.icon(
            onPressed: () => context.go(Routes.home),
            icon: const Icon(Icons.shopping_bag),
            label: const Text('Start Shopping'),
          ),
        ],
      ),
    );
  }
}

/// Cart item card displaying item details and quantity controls.
/// 
/// **Validates: Requirements 19.2, 19.3, 19.4, 19.5, 19.6**
class _CartItemCard extends ConsumerWidget {
  final CartItem cartItem;

  const _CartItemCard({required this.cartItem});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Item name and code
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        cartItem.itemName,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        'Code: ${cartItem.itemCode}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Colors.grey.shade600,
                            ),
                      ),
                    ],
                  ),
                ),
                // Remove button
                IconButton(
                  onPressed: () => _handleRemove(context, ref),
                  icon: const Icon(Icons.delete_outline),
                  color: Colors.red,
                  tooltip: 'Remove item',
                ),
              ],
            ),
            
            const SizedBox(height: 12),
            const Divider(),
            const SizedBox(height: 12),

            // Quantity controls and price
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                // Quantity controls
                _QuantityControls(cartItem: cartItem),
                
                // Price information
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      'Unit Price',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Colors.grey.shade600,
                          ),
                    ),
                    Text(
                      '\$${cartItem.unitPriceUsd.toStringAsFixed(2)}',
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                    Text(
                      '៛${cartItem.unitPriceKhr.toStringAsFixed(0)}',
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            color: Colors.grey.shade600,
                          ),
                    ),
                  ],
                ),
              ],
            ),

            const SizedBox(height: 12),

            // Line total
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.blue.shade50,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    'Line Total',
                    style: Theme.of(context).textTheme.titleSmall?.copyWith(
                          fontWeight: FontWeight.bold,
                        ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        '\$${cartItem.lineTotalUsd.toStringAsFixed(2)}',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: Colors.blue.shade700,
                            ),
                      ),
                      Text(
                        '៛${cartItem.lineTotalKhr.toStringAsFixed(0)}',
                        style: Theme.of(context).textTheme.bodySmall?.copyWith(
                              color: Colors.grey.shade600,
                            ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _handleRemove(BuildContext context, WidgetRef ref) async {
    // Show confirmation dialog
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Remove Item'),
        content: Text('Remove ${cartItem.itemName} from cart?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            style: TextButton.styleFrom(foregroundColor: Colors.red),
            child: const Text('Remove'),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      await ref.read(cartProvider.notifier).removeItem(cartItem.itemId);
      
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('${cartItem.itemName} removed from cart'),
            duration: const Duration(seconds: 2),
          ),
        );
      }
    }
  }
}

/// Quantity adjustment controls (+/- buttons with current quantity display).
/// 
/// **Validates: Requirements 19.4, 19.5**
class _QuantityControls extends ConsumerWidget {
  final CartItem cartItem;

  const _QuantityControls({required this.cartItem});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(color: Colors.grey.shade300),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          // Decrement button
          IconButton(
            onPressed: cartItem.quantity > 1
                ? () => _handleDecrement(ref)
                : null,
            icon: const Icon(Icons.remove),
            iconSize: 20,
            padding: const EdgeInsets.all(8),
            constraints: const BoxConstraints(minWidth: 40, minHeight: 40),
          ),
          
          // Quantity display
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            decoration: BoxDecoration(
              color: Colors.grey.shade100,
              border: Border.symmetric(
                vertical: BorderSide(color: Colors.grey.shade300),
              ),
            ),
            child: Text(
              cartItem.quantity.toString(),
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
          ),
          
          // Increment button
          IconButton(
            onPressed: () => _handleIncrement(ref),
            icon: const Icon(Icons.add),
            iconSize: 20,
            padding: const EdgeInsets.all(8),
            constraints: const BoxConstraints(minWidth: 40, minHeight: 40),
          ),
        ],
      ),
    );
  }

  Future<void> _handleDecrement(WidgetRef ref) async {
    // Get price tiers from catalog service
    final catalogService = ref.read(catalogServiceProvider);
    final priceTiers = await catalogService.getPriceTiers(cartItem.itemId);
    
    // Update quantity
    final newQuantity = cartItem.quantity - 1;
    await ref.read(cartProvider.notifier).setQuantity(
          cartItem.itemId,
          newQuantity,
          priceTiers,
        );
  }

  Future<void> _handleIncrement(WidgetRef ref) async {
    // Get price tiers from catalog service
    final catalogService = ref.read(catalogServiceProvider);
    final priceTiers = await catalogService.getPriceTiers(cartItem.itemId);
    
    // Update quantity
    final newQuantity = cartItem.quantity + 1;
    await ref.read(cartProvider.notifier).setQuantity(
          cartItem.itemId,
          newQuantity,
          priceTiers,
        );
  }
}

/// Cart footer displaying grand total and checkout button.
/// 
/// Implements complete checkout flow:
/// - Validates cart is non-empty (Requirement 7.1)
/// - Checks connectivity before allowing checkout (Requirement 10.8)
/// - Submits order via OrderService.placeOrder (Requirement 7.2)
/// - On success: clears cart and navigates to /orders (Requirement 7.4, 7.7)
/// - On failure: retains cart and shows error message (Requirement 7.5)
/// 
/// **Validates: Requirements 7.1, 7.2, 7.4, 7.5, 7.7, 10.8, 19.7, 19.8**
class _CartFooter extends ConsumerWidget {
  final double total;

  const _CartFooter({required this.total});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Calculate KHR total (approximation: 1 USD = 4100 KHR)
    const double exchangeRate = 4100.0;
    final totalKhr = total * exchangeRate;

    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 8,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              // Grand total display
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green.shade50,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.green.shade200),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Grand Total',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          '\$${total.toStringAsFixed(2)}',
                          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                                fontWeight: FontWeight.bold,
                                color: Colors.green.shade700,
                              ),
                        ),
                        Text(
                          '៛${totalKhr.toStringAsFixed(0)}',
                          style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                                color: Colors.grey.shade700,
                              ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              
              const SizedBox(height: 16),

              // Checkout button
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () => _handleCheckout(context, ref),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.green.shade600,
                    foregroundColor: Colors.white,
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.payment),
                      const SizedBox(width: 8),
                      Text(
                        'Proceed to Checkout',
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              color: Colors.white,
                              fontWeight: FontWeight.bold,
                            ),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _handleCheckout(BuildContext context, WidgetRef ref) async {
    // Task 20.1: Validate cart is non-empty before allowing checkout
    final cartService = await ref.read(cartProvider.future);
    if (cartService.items.isEmpty) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Cart is empty'),
            backgroundColor: Colors.red,
            duration: Duration(seconds: 2),
          ),
        );
      }
      return;
    }

    // Task 20.1: Check connectivity - if offline show error message
    final connectivityResult = await Connectivity().checkConnectivity();
    if (connectivityResult == ConnectivityResult.none) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('No internet connection. Please try again when online.'),
            backgroundColor: Colors.red,
            duration: Duration(seconds: 3),
          ),
        );
      }
      return;
    }

    // Get current user ID
    final authService = ref.read(authServiceProvider).value;
    final userId = authService is Authenticated ? authService.userId : null;
    
    if (userId == null) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Authentication error. Please login again.'),
            backgroundColor: Colors.red,
            duration: Duration(seconds: 3),
          ),
        );
        context.go(Routes.login);
      }
      return;
    }

    // Show loading indicator
    if (context.mounted) {
      showDialog(
        context: context,
        barrierDismissible: false,
        builder: (context) => const Center(
          child: Card(
            child: Padding(
              padding: EdgeInsets.all(24.0),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircularProgressIndicator(),
                  SizedBox(height: 16),
                  Text('Processing order...'),
                ],
              ),
            ),
          ),
        ),
      );
    }

    try {
      // Task 20.1: On submit - call OrderService.placeOrder
      final orderService = ref.read(orderServiceProvider.notifier);
      final result = await orderService.placeOrder(userId, cartService.items);

      // Dismiss loading dialog
      if (context.mounted) {
        Navigator.of(context).pop();
      }

      // Task 20.1: Handle result
      if (result is OrderSuccess) {
        // On success: clear cart and navigate to /orders
        await ref.read(cartProvider.notifier).clear();
        
        if (context.mounted) {
          // Show success message
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Order placed successfully! Invoice: ${result.invoiceNo}'),
              backgroundColor: Colors.green,
              duration: const Duration(seconds: 3),
            ),
          );
          
          // Navigate to order history
          context.go(Routes.orders);
        }
      } else if (result is OrderNetworkError) {
        // Task 20.1: On failure - retain cart and show error
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(
              content: Text('No internet connection. Please try again when online.'),
              backgroundColor: Colors.red,
              duration: Duration(seconds: 3),
            ),
          );
        }
      } else if (result is OrderFailure) {
        // Task 20.1: On failure - retain cart and show error
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Text('Order failed: ${result.message}'),
              backgroundColor: Colors.red,
              duration: const Duration(seconds: 3),
            ),
          );
        }
      }
    } catch (e) {
      // Dismiss loading dialog
      if (context.mounted) {
        Navigator.of(context).pop();
      }
      
      // Task 20.1: On failure - retain cart and show error
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('An unexpected error occurred: $e'),
            backgroundColor: Colors.red,
            duration: const Duration(seconds: 3),
          ),
        );
      }
    }
  }
}
