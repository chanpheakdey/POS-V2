// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'sync_service.dart';

// **************************************************************************
// RiverpodGenerator
// **************************************************************************

String _$syncServiceHash() => r'bb0d1e2f3a4b5c6d7e8f9g0h1i2j3k4l5m6n7o8p';

/// Riverpod provider for SyncService.
///
/// Wired to trigger on app foreground and network reconnection via
/// ConnectivityService.
///
/// **Validates: Requirements 18.3, 18.5, 18.6, 18.7, 18.9, 18.10, 18.12, 18.13, 18.14**
///
/// Copied from [syncService].
@ProviderFor(syncService)
final syncServiceProvider = AutoDisposeProvider<SyncService>.internal(
  syncService,
  name: r'syncServiceProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$syncServiceHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef SyncServiceRef = AutoDisposeProviderRef<SyncService>;
String _$connectivityStreamHash() => r'c9d0e1f2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p';

/// Stream provider for connectivity changes.
///
/// Emits connectivity status updates that can trigger synchronization.
///
/// Copied from [connectivityStream].
@ProviderFor(connectivityStream)
final connectivityStreamProvider =
    AutoDisposeStreamProvider<List<ConnectivityResult>>.internal(
  connectivityStream,
  name: r'connectivityStreamProvider',
  debugGetCreateSourceHash: const bool.fromEnvironment('dart.vm.product')
      ? null
      : _$connectivityStreamHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef ConnectivityStreamRef
    = AutoDisposeStreamProviderRef<List<ConnectivityResult>>;
String _$autoSyncHash() => r'd0e1f2a3b4c5d6e7f8g9h0i1j2k3l4m5n6o7p8q';

/// Provider that automatically triggers sync on network reconnection.
///
/// Watches the connectivity stream and triggers synchronization when
/// the device reconnects to the internet.
///
/// Copied from [AutoSync].
@ProviderFor(AutoSync)
final autoSyncProvider =
    AutoDisposeAsyncNotifierProvider<AutoSync, void>.internal(
  AutoSync.new,
  name: r'autoSyncProvider',
  debugGetCreateSourceHash:
      const bool.fromEnvironment('dart.vm.product') ? null : _$autoSyncHash,
  dependencies: null,
  allTransitiveDependencies: null,
);

typedef _$AutoSync = AutoDisposeAsyncNotifier<void>;
// ignore_for_file: type=lint
// ignore_for_file: subtype_of_sealed_class, invalid_use_of_internal_member, invalid_use_of_visible_for_testing_member
