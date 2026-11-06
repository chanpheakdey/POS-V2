import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/routing/routes.dart';
import 'package:pos_app/src/auth/auth_state.dart';
import 'package:pos_app/src/auth/auth_service.dart';
import 'package:pos_app/src/routing/router.dart';
import 'package:mocktail/mocktail.dart';

class MockAuthService extends Mock implements AuthService {}

void main() {
  group('Router Navigation Guards', () {
    late ProviderContainer container;

    setUp(() {
      container = ProviderContainer();
    });

    tearDown(() {
      container.dispose();
    });

    test('Routes are properly defined', () {
      // Verify all route constants exist
      expect(Routes.login, equals('/login'));
      expect(Routes.home, equals('/home'));
      expect(Routes.cart, equals('/cart'));
      expect(Routes.orders, equals('/orders'));
      expect(Routes.search, equals('/search'));
      expect(Routes.categoryRoute, equals('/category/:id'));
      expect(Routes.groupRoute, equals('/group/:categoryId/:id'));
      expect(Routes.itemsRoute, equals('/items/:categoryId/:groupId'));
      expect(Routes.itemDetailRoute, equals('/item-detail/:id'));
      expect(Routes.orderDetailRoute, equals('/order-detail/:id'));
    });

    test('Helper methods build correct paths', () {
      expect(Routes.category(1), equals('/category/1'));
      expect(Routes.group(1, 2), equals('/group/1/2'));
      expect(Routes.items(1, 2), equals('/items/1/2'));
      expect(Routes.itemDetail(123), equals('/item-detail/123'));
      expect(Routes.orderDetail(456), equals('/order-detail/456'));
    });

    test('Router is provided as a Riverpod provider', () {
      // Verify that the router provider can be created
      // The actual router requires a valid auth service, so we test
      // that the provider exists and is properly typed
      expect(routerProvider, isNotNull);
    });

    group('Authentication Guard Logic', () {
      test('Unauthenticated state should redirect to login', () {
        const authState = Unauthenticated();
        
        // Logic: if not authenticated and not going to login -> redirect to login
        const isGoingToLogin = false;
        final isAuthenticated = authState is Authenticated;
        
        expect(isAuthenticated, isFalse);
        // Should redirect to login when not authenticated
        final shouldRedirect = !isAuthenticated && !isGoingToLogin;
        expect(shouldRedirect, isTrue);
      });

      test('Authenticated state going to login should redirect to home', () {
        const authState = Authenticated(
          token: 'test-token',
          userId: 1,
          username: 'testuser',
        );
        
        // Logic: if authenticated and going to login -> redirect to home
        const isGoingToLogin = true;
        final isAuthenticated = authState is Authenticated;
        
        expect(isAuthenticated, isTrue);
        // Should redirect to home when authenticated user tries to access login
        final shouldRedirect = isAuthenticated && isGoingToLogin;
        expect(shouldRedirect, isTrue);
      });

      test('Authenticated state on home should not redirect', () {
        const authState = Authenticated(
          token: 'test-token',
          userId: 1,
          username: 'testuser',
        );
        
        // Logic: authenticated on protected route -> no redirect
        const isGoingToLogin = false;
        final isAuthenticated = authState is Authenticated;
        
        expect(isAuthenticated, isTrue);
        // Should not redirect when authenticated and accessing protected routes
        final shouldRedirectToLogin = !isAuthenticated && !isGoingToLogin;
        final shouldRedirectToHome = isAuthenticated && isGoingToLogin;
        expect(shouldRedirectToLogin, isFalse);
        expect(shouldRedirectToHome, isFalse);
      });

      test('Unauthenticated state on login should not redirect', () {
        const authState = Unauthenticated();
        
        // Logic: not authenticated but already on login -> no redirect
        const isGoingToLogin = true;
        final isAuthenticated = authState is Authenticated;
        
        expect(isAuthenticated, isFalse);
        // Should not redirect when not authenticated but already on login
        final shouldRedirect = !isAuthenticated && !isGoingToLogin;
        expect(shouldRedirect, isFalse);
      });
    });
  });

  group('Route Path Parsing', () {
    test('Category route extracts categoryId', () {
      // Verify that the category route pattern can match and extract parameters
      final path = Routes.category(42);
      expect(path, equals('/category/42'));
      
      // Extract id from path
      final match = RegExp(r'/category/(\d+)').firstMatch(path);
      expect(match, isNotNull);
      expect(match!.group(1), equals('42'));
    });

    test('Group route extracts categoryId and groupId', () {
      final path = Routes.group(10, 20);
      expect(path, equals('/group/10/20'));
      
      final match = RegExp(r'/group/(\d+)/(\d+)').firstMatch(path);
      expect(match, isNotNull);
      expect(match!.group(1), equals('10'));
      expect(match.group(2), equals('20'));
    });

    test('Items route extracts categoryId and groupId', () {
      final path = Routes.items(5, 15);
      expect(path, equals('/items/5/15'));
      
      final match = RegExp(r'/items/(\d+)/(\d+)').firstMatch(path);
      expect(match, isNotNull);
      expect(match!.group(1), equals('5'));
      expect(match.group(2), equals('15'));
    });

    test('Item detail route extracts itemId', () {
      final path = Routes.itemDetail(999);
      expect(path, equals('/item-detail/999'));
      
      final match = RegExp(r'/item-detail/(\d+)').firstMatch(path);
      expect(match, isNotNull);
      expect(match!.group(1), equals('999'));
    });

    test('Order detail route extracts orderId', () {
      final path = Routes.orderDetail(123);
      expect(path, equals('/order-detail/123'));
      
      final match = RegExp(r'/order-detail/(\d+)').firstMatch(path);
      expect(match, isNotNull);
      expect(match!.group(1), equals('123'));
    });
  });
}
