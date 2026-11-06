// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'order_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$orderServiceHash() => r'o1r2d3e4r5s6e7r8v9i0c1e2h3a4s5h6v7a8l9u0';

/// Service for managing order placement and order history retrieval.
///
/// Handles both online and offline order submission:
/// - Online: submits directly to backend via ApiClient
/// - Offline: queues orders in local file for later submission
///
/// **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 8.1, 8.2, 8.5, 10.8, 10.11**
///
/// Copied from [OrderService].
@ProviderFor(OrderService)
final orderServiceProvider =
    AutoDisposeAsyncNotifierProvider<OrderService, void>.internal(
  OrderService.new,
  name: r'orderServiceProvider',
  debugGetCreateSourceHash: const bool.fromEnvironment('dart.vm.product')
      ? null
      : _$orderServiceHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef _$OrderService = AutoDisposeAsyncNotifier<void>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
