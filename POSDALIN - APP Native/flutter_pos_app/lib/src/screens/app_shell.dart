import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import '../cart/cart_service.dart';
import '../sync/sync_service.dart';
import '../auth/auth_service.dart';
import '../shared/connectivity_service.dart';
import '../routing/routes.dart';

/// App shell widget that wraps main screens with persistent navigation.
/// 
/// Provides:
/// - Bottom navigation bar with Home, Cart (with badge), Orders, and Logout
/// - Online/offline status indicator
/// - Persistent UI elements across screen transitions
/// 
/// **Validates: Requirements 2.7, 10.10, 15.1, 19.9**
class AppShell extends ConsumerWidget {
  /// The child widget to display in the main content area
  final Widget child;

  const AppShell({
    super.key,
    required this.child,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Watch cart state to display item count badge
    final cartAsync = ref.watch(cartProvider);
    final cartItemCount = cartAsync.when(
      data: (cartService) => cartService.items.length,
      loading: () => 0,
      error: (_, __) => 0,
    );

    // Watch connectivity status
    final connectivityAsync = ref.watch(connectivityStreamProvider);
    final isOnline = connectivityAsync.when(
      data: (results) => results.any(
        (result) =>
            result == ConnectivityResult.mobile ||
            result == ConnectivityResult.wifi ||
            result == ConnectivityResult.ethernet,
      ),
      loading: () => true, // Assume online while checking
      error: (_, __) => false,
    );

    // Get current route location
    final currentLocation = GoRouterState.of(context).uri.path;

    return Scaffold(
      body: Column(
        children: [
          // Online/offline status indicator
          _OnlineStatusBanner(isOnline: isOnline),
          
          // Main content
          Expanded(child: child),
        ],
      ),
      bottomNavigationBar: _buildBottomNavigationBar(
        context,
        ref,
        currentLocation,
        cartItemCount,
      ),
    );
  }

  Widget _buildBottomNavigationBar(
    BuildContext context,
    WidgetRef ref,
    String currentLocation,
    int cartItemCount,
  ) {
    // Determine current index based on route
    int currentIndex = 0;
    if (currentLocation == Routes.home) {
      currentIndex = 0;
    } else if (currentLocation == Routes.cart) {
      currentIndex = 1;
    } else if (currentLocation == Routes.orders) {
      currentIndex = 2;
    }

    return BottomNavigationBar(
      type: BottomNavigationBarType.fixed,
      currentIndex: currentIndex,
      onTap: (index) => _onNavTap(context, ref, index),
      items: [
        const BottomNavigationBarItem(
          icon: Icon(Icons.home),
          label: 'Home',
        ),
        BottomNavigationBarItem(
          icon: _buildCartIconWithBadge(cartItemCount),
          label: 'Cart',
        ),
        const BottomNavigationBarItem(
          icon: Icon(Icons.receipt_long),
          label: 'Orders',
        ),
        const BottomNavigationBarItem(
          icon: Icon(Icons.logout),
          label: 'Logout',
        ),
      ],
    );
  }

  Widget _buildCartIconWithBadge(int itemCount) {
    if (itemCount == 0) {
      return const Icon(Icons.shopping_cart);
    }

    return Badge(
      label: Text(itemCount.toString()),
      backgroundColor: Colors.red,
      textColor: Colors.white,
      child: const Icon(Icons.shopping_cart),
    );
  }

  void _onNavTap(BuildContext context, WidgetRef ref, int index) {
    switch (index) {
      case 0:
        context.go(Routes.home);
        break;
      case 1:
        context.go(Routes.cart);
        break;
      case 2:
        context.go(Routes.orders);
        break;
      case 3:
        _handleLogout(context, ref);
        break;
    }
  }

  Future<void> _handleLogout(BuildContext context, WidgetRef ref) async {
    // Show confirmation dialog
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Logout'),
        content: const Text('Are you sure you want to logout?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Logout'),
          ),
        ],
      ),
    );

    if (confirmed == true && context.mounted) {
      // Perform logout - this will:
      // 1. Expire token on backend
      // 2. Clear token from secure storage
      // 3. Clear cart contents
      // 4. Update auth state to unauthenticated
      await ref.read(authServiceProvider.notifier).logout();
      
      // Navigation to login will happen automatically via router redirect
      // when auth state changes to unauthenticated, but we can explicitly
      // navigate to ensure it happens immediately
      if (context.mounted) {
        context.go(Routes.login);
      }
    }
  }
}

/// Online/offline status banner widget.
/// 
/// Displays a colored banner at the top of the screen indicating connectivity status.
/// - Green banner with checkmark when online
/// - Red banner with warning icon when offline
/// 
/// **Validates: Requirements 10.10**
class _OnlineStatusBanner extends StatelessWidget {
  final bool isOnline;

  const _OnlineStatusBanner({required this.isOnline});

  @override
  Widget build(BuildContext context) {
    if (isOnline) {
      // Show subtle online indicator
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 8),
        color: Colors.green.shade100,
        child: Row(
          mainAxisSize: MainAxisSize.min,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.wifi,
              size: 16,
              color: Colors.green.shade700,
            ),
            const SizedBox(width: 4),
            Text(
              'Online',
              style: TextStyle(
                fontSize: 12,
                color: Colors.green.shade700,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      );
    } else {
      // Show prominent offline warning
      return Container(
        width: double.infinity,
        padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 12),
        color: Colors.red.shade600,
        child: const Row(
          mainAxisSize: MainAxisSize.min,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.wifi_off,
              size: 18,
              color: Colors.white,
            ),
            SizedBox(width: 8),
            Text(
              'Offline - Some features may be unavailable',
              style: TextStyle(
                fontSize: 13,
                color: Colors.white,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      );
    }
  }
}
