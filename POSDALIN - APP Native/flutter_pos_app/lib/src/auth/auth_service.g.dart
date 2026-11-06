// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'auth_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$authServiceHash() => r'9e8b5f7c8d6a4e2b1c9f8e7d6c5b4a3e2d1c0b9a';

/// Service managing authentication lifecycle including login, token validation, and logout.
///
/// Implements token-based authentication with persistent storage using platform secure storage.
/// Tokens persist indefinitely until explicit logout (no expiration).
///
/// **Validates: Requirements 1.2, 1.3, 1.5, 1.7, 1.8, 2.1, 2.2, 2.6, 2.7, 15.2, 15.3, 17.3**
///
/// Copied from [AuthService].
@ProviderFor(AuthService)
final authServiceProvider =
    AutoDisposeAsyncNotifierProvider<AuthService, AuthState>.internal(
  AuthService.new,
  name: r'authServiceProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$authServiceHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef _$AuthService = AutoDisposeAsyncNotifier<AuthState>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
