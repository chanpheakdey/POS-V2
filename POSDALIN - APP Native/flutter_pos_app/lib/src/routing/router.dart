import 'dart:async';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../auth/auth_service.dart';
import '../auth/auth_state.dart';
import '../screens/app_shell.dart';
import '../screens/login/login.dart';
import '../screens/home/home.dart';
import '../screens/search/search_screen.dart';
import '../screens/orders/orders_screen.dart';
import '../screens/orders/order_detail_screen.dart';
import '../screens/item_detail/item_detail_screen.dart';
import '../screens/cart/cart_screen.dart';
import '../catalog/catalog.dart';
import 'routes.dart';

part 'router.g.dart';

/// Navigation router with authentication guards.
/// 
/// Implements declarative routing with go_router and integrates with AuthService
/// for redirect logic:
/// - On cold start: reads AuthService state → navigate to /login or /home
/// - On AuthError from any service: redirect to /login
/// 
/// Routes defined:
/// - /login: Login screen
/// - /home: Home screen (authenticated)
/// - /category/:id: Category screen with groups or items
/// - /group/:categoryId/:id: Group screen with items
/// - /items/:categoryId/:groupId: Items listing screen
/// - /item-detail/:id: Item detail screen
/// - /cart: Shopping cart screen
/// - /orders: Order history screen
/// - /order-detail/:id: Order detail screen
/// - /search: Search screen
/// 
/// **Validates: Requirements 1.9, 2.3, 2.4, 13.6**
@riverpod
GoRouter router(RouterRef ref) {
  final authServiceAsync = ref.watch(authServiceProvider);
  
  return GoRouter(
    initialLocation: Routes.login,
    debugLogDiagnostics: true,
    
    // Refresh listenable to react to auth state changes
    refreshListenable: _GoRouterRefreshStream(
      ref.read(authServiceProvider.notifier).stream,
    ),
    
    redirect: (BuildContext context, GoRouterState state) {
      // Handle AsyncValue states from auth service
      return authServiceAsync.when(
        data: (authState) {
          final isAuthenticated = authState is Authenticated;
          final isGoingToLogin = state.matchedLocation == Routes.login;
          
          // If not authenticated and not going to login, redirect to login
          if (!isAuthenticated && !isGoingToLogin) {
            return Routes.login;
          }
          
          // If authenticated and going to login, redirect to home
          if (isAuthenticated && isGoingToLogin) {
            return Routes.home;
          }
          
          // No redirect needed
          return null;
        },
        loading: () {
          // While loading auth state, don't redirect
          // This prevents flashing to login screen during validation
          return null;
        },
        error: (error, stack) {
          // On auth error, redirect to login unless already there
          final isGoingToLogin = state.matchedLocation == Routes.login;
          return isGoingToLogin ? null : Routes.login;
        },
      );
    },
    
    routes: [
      GoRoute(
        path: Routes.login,
        name: 'login',
        builder: (context, state) => const LoginScreen(),
      ),
      
      // Shell route wraps all authenticated routes with AppShell
      ShellRoute(
        builder: (context, state, child) => AppShell(child: child),
        routes: [
          GoRoute(
            path: Routes.home,
            name: 'home',
            builder: (context, state) => const HomeScreen(),
          ),
          GoRoute(
            path: Routes.categoryRoute,
            name: 'category',
            builder: (context, state) {
              final categoryId = int.parse(state.pathParameters['id']!);
              return CategoryScreen(categoryId: categoryId);
            },
          ),
          GoRoute(
            path: Routes.groupRoute,
            name: 'group',
            builder: (context, state) {
              final categoryId = int.parse(state.pathParameters['categoryId']!);
              final groupId = int.parse(state.pathParameters['id']!);
              return GroupScreen(
                categoryId: categoryId,
                groupId: groupId,
              );
            },
          ),
          GoRoute(
            path: Routes.itemsRoute,
            name: 'items',
            builder: (context, state) {
              final categoryId = int.parse(state.pathParameters['categoryId']!);
              final groupId = int.parse(state.pathParameters['groupId']!);
              return ItemsScreen(
                categoryId: categoryId,
                groupId: groupId,
              );
            },
          ),
          GoRoute(
            path: Routes.itemDetailRoute,
            name: 'item-detail',
            builder: (context, state) {
              final itemId = int.parse(state.pathParameters['id']!);
              return ItemDetailScreen(itemId: itemId);
            },
          ),
          GoRoute(
            path: Routes.cart,
            name: 'cart',
            builder: (context, state) => const CartScreen(),
          ),
          GoRoute(
            path: Routes.orders,
            name: 'orders',
            builder: (context, state) => const OrdersScreen(),
          ),
          GoRoute(
            path: Routes.orderDetailRoute,
            name: 'order-detail',
            builder: (context, state) {
              final orderId = int.parse(state.pathParameters['id']!);
              return OrderDetailScreen(orderId: orderId);
            },
          ),
          GoRoute(
            path: Routes.search,
            name: 'search',
            builder: (context, state) => const SearchScreen(),
          ),
        ],
      ),
    ],
    errorBuilder: (context, state) => ErrorScreen(error: state.error),
  );
}


// Placeholder screens - these will be implemented in subsequent tasks

class CategoryScreen extends StatelessWidget {
  final int categoryId;
  
  const CategoryScreen({super.key, required this.categoryId});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Category $categoryId')),
      body: Center(child: Text('Category Screen - To be implemented')),
    );
  }
}

class GroupScreen extends StatelessWidget {
  final int categoryId;
  final int groupId;
  
  const GroupScreen({
    super.key,
    required this.categoryId,
    required this.groupId,
  });

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Group $groupId')),
      body: Center(child: Text('Group Screen - To be implemented')),
    );
  }
}

class ItemsScreen extends ConsumerWidget {
  final int categoryId;
  final int groupId;
  
  const ItemsScreen({
    super.key,
    required this.categoryId,
    required this.groupId,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Get the group name from the catalog service
    // For now, we'll use a placeholder name "Items"
    // In a real implementation, we might want to pass the group name through route state
    return ItemListScreen(
      categoryId: categoryId,
      groupId: groupId,
      groupName: 'Items',
    );
  }
}





class SearchScreen extends StatelessWidget {
  const SearchScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Search')),
      body: const Center(child: Text('Search Screen - To be implemented')),
    );
  }
}

class ErrorScreen extends StatelessWidget {
  final Exception? error;
  
  const ErrorScreen({super.key, this.error});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Error')),
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(Icons.error_outline, size: 48, color: Colors.red),
            const SizedBox(height: 16),
            const Text(
              'Page not found',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            if (error != null) ...[
              const SizedBox(height: 8),
              Text(
                error.toString(),
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.grey),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Wrapper to convert Stream into ChangeNotifier for go_router refreshListenable.
/// 
/// This enables the router to react to auth state changes from the AuthService
/// stream, triggering route guards when authentication state changes.
class _GoRouterRefreshStream extends ChangeNotifier {
  _GoRouterRefreshStream(Stream<dynamic> stream) {
    _subscription = stream.listen((_) {
      notifyListeners();
    });
  }

  late final StreamSubscription<dynamic> _subscription;

  @override
  void dispose() {
    _subscription.cancel();
    super.dispose();
  }
}
