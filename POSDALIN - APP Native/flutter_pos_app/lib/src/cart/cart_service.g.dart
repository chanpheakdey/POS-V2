// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'cart_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$cartHash() => r'c1a2r3t4s5e6r7v8i9c0e1a2b3c4d5e6f7g8h9i0';

/// Riverpod notifier for CartService.
///
/// Loads cart from storage on initialization and provides reactive state updates.
///
/// **Validates: Requirements 6.9, 19.2**
///
/// Copied from [Cart].
@ProviderFor(Cart)
final cartProvider = AutoDisposeAsyncNotifierProvider<Cart, CartService>.internal(
  Cart.new,
  name: r'cartProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$cartHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef _$Cart = AutoDisposeAsyncNotifier<CartService>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
