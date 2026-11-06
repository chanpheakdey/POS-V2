/// Verification script for Task 20.1: Checkout Implementation
/// 
/// This script verifies that all components needed for checkout are properly implemented.
/// 
/// Usage: dart run verify_checkout.dart

import 'dart:io';

void main() {
  print('=== Task 20.1: Checkout Implementation Verification ===\n');

  final verifications = <String, bool>{};

  // 1. Check cart_screen.dart exists and contains checkout implementation
  print('1. Checking cart_screen.dart...');
  final cartScreenFile = File('lib/src/screens/cart/cart_screen.dart');
  if (!cartScreenFile.existsSync()) {
    verifications['cart_screen.dart exists'] = false;
    print('   ❌ cart_screen.dart not found');
  } else {
    final content = cartScreenFile.readAsStringSync();
    
    // Check for required implementations
    final checks = {
      'Cart validation': content.contains('if (cartService.items.isEmpty)'),
      'Connectivity check': content.contains('Connectivity().checkConnectivity()'),
      'Offline message': content.contains('No internet connection. Please try again when online.'),
      'OrderService.placeOrder call': content.contains('orderService.placeOrder'),
      'Cart clear on success': content.contains('cartProvider.notifier).clear()'),
      'Navigate to orders': content.contains('context.go(Routes.orders)'),
      'Error handling': content.contains('OrderNetworkError') && content.contains('OrderFailure'),
      'Loading indicator': content.contains('Processing order'),
    };

    checks.forEach((check, passed) {
      verifications[check] = passed;
      print('   ${passed ? "✅" : "❌"} $check');
    });
  }

  // 2. Check order_service.dart exists and has placeOrder method
  print('\n2. Checking order_service.dart...');
  final orderServiceFile = File('lib/src/order/order_service.dart');
  if (!orderServiceFile.existsSync()) {
    verifications['order_service.dart exists'] = false;
    print('   ❌ order_service.dart not found');
  } else {
    final content = orderServiceFile.readAsStringSync();
    
    final checks = {
      'placeOrder method': content.contains('Future<OrderResult> placeOrder'),
      'Cart validation': content.contains('if (items.isEmpty)'),
      'ApiClient integration': content.contains('apiClient.submitOrder'),
      'Offline queue': content.contains('queueOfflineOrder'),
    };

    checks.forEach((check, passed) {
      verifications[check] = passed;
      print('   ${passed ? "✅" : "❌"} $check');
    });
  }

  // 3. Check order_result.dart exists with proper types
  print('\n3. Checking order_result.dart...');
  final orderResultFile = File('lib/src/order/order_result.dart');
  if (!orderResultFile.existsSync()) {
    verifications['order_result.dart exists'] = false;
    print('   ❌ order_result.dart not found');
  } else {
    final content = orderResultFile.readAsStringSync();
    
    final checks = {
      'OrderResult sealed class': content.contains('sealed class OrderResult'),
      'OrderSuccess type': content.contains('class OrderSuccess extends OrderResult'),
      'OrderFailure type': content.contains('class OrderFailure extends OrderResult'),
      'OrderNetworkError type': content.contains('class OrderNetworkError extends OrderResult'),
    };

    checks.forEach((check, passed) {
      verifications[check] = passed;
      print('   ${passed ? "✅" : "❌"} $check');
    });
  }

  // 4. Check routes.dart has orders route
  print('\n4. Checking routes.dart...');
  final routesFile = File('lib/src/routing/routes.dart');
  if (!routesFile.existsSync()) {
    verifications['routes.dart exists'] = false;
    print('   ❌ routes.dart not found');
  } else {
    final content = routesFile.readAsStringSync();
    
    final checks = {
      'Orders route defined': content.contains("orders = '/orders'"),
    };

    checks.forEach((check, passed) {
      verifications[check] = passed;
      print('   ${passed ? "✅" : "❌"} $check');
    });
  }

  // Summary
  print('\n=== Verification Summary ===');
  final passed = verifications.values.where((v) => v).length;
  final total = verifications.length;
  
  print('Total checks: $total');
  print('Passed: $passed');
  print('Failed: ${total - passed}');
  
  if (passed == total) {
    print('\n✅ All verifications passed! Task 20.1 is complete.');
    exit(0);
  } else {
    print('\n❌ Some verifications failed. Please review the implementation.');
    exit(1);
  }
}
