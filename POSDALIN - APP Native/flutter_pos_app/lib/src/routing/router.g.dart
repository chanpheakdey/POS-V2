// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'router.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$routerHash() => r'r1o2u3t4e5r6p7r8o9v0i1d2e3r4h5a6s7h8e9';

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
///
/// Copied from [router].
@ProviderFor(router)
final routerProvider = AutoDisposeProvider<GoRouter>.internal(
  router,
  name: r'routerProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$routerHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef RouterRef = AutoDisposeProviderRef<GoRouter>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
