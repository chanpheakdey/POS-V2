// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'connectivity_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$connectivityServiceHash() => r'a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t';

/// Riverpod provider for ConnectivityService.
///
/// **Validates: Requirements 10.9, 10.10**
///
/// Copied from [connectivityService].
@ProviderFor(connectivityService)
final connectivityServiceProvider =
    AutoDisposeProvider<ConnectivityService>.internal(
  connectivityService,
  name: r'connectivityServiceProvider',
  debugGetCreateSourceHash: const bool.fromEnvironment('dart.vm.product')
      ? null
      : _$connectivityServiceHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef ConnectivityServiceRef = AutoDisposeProviderRef<ConnectivityService>;
String _$connectivityStatusHash() => r'b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u';

/// Stream provider for connectivity status as boolean.
///
/// Emits true when online, false when offline.
///
/// **Validates: Requirements 10.9, 10.10**
///
/// Copied from [connectivityStatus].
@ProviderFor(connectivityStatus)
final connectivityStatusProvider = AutoDisposeStreamProvider<bool>.internal(
  connectivityStatus,
  name: r'connectivityStatusProvider',
  debugGetCreateSourceHash: const bool.fromEnvironment('dart.vm.product')
      ? null
      : _$connectivityStatusHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef ConnectivityStatusRef = AutoDisposeStreamProviderRef<bool>;
String _$isOnlineHash() => r'c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v';

/// Provider for current connectivity status.
///
/// Returns true if device is currently online.
///
/// **Validates: Requirements 10.10**
///
/// Copied from [isOnline].
@ProviderFor(isOnline)
final isOnlineProvider = AutoDisposeFutureProvider<bool>.internal(
  isOnline,
  name: r'isOnlineProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$isOnlineHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef IsOnlineRef = AutoDisposeFutureProviderRef<bool>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
