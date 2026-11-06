import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';

part 'connectivity_service.g.dart';

/// Service for monitoring network connectivity status.
/// 
/// Wraps connectivity_plus and provides a clean interface for monitoring
/// online/offline state throughout the application.
/// 
/// **Validates: Requirements 10.9, 10.10**
abstract class ConnectivityService {
  /// Stream of connectivity status changes.
  Stream<bool> get onConnectivityChanged;

  /// Check current connectivity status.
  /// 
  /// Returns true if device has internet connectivity (mobile, wifi, ethernet).
  Future<bool> get isOnline;

  /// Check if device is currently offline.
  Future<bool> get isOffline;
}

/// Implementation of ConnectivityService using connectivity_plus package.
class ConnectivityServiceImpl implements ConnectivityService {
  final Connectivity _connectivity;
  StreamController<bool>? _statusController;

  ConnectivityServiceImpl({Connectivity? connectivity})
      : _connectivity = connectivity ?? Connectivity() {
    _initializeStatusStream();
  }

  void _initializeStatusStream() {
    _statusController = StreamController<bool>.broadcast();

    // Listen to connectivity changes and convert to boolean stream
    _connectivity.onConnectivityChanged.listen((results) {
      final isOnline = _hasInternetConnectivity(results);
      _statusController?.add(isOnline);
    });
  }

  @override
  Stream<bool> get onConnectivityChanged {
    return _statusController?.stream ?? const Stream.empty();
  }

  @override
  Future<bool> get isOnline async {
    final results = await _connectivity.checkConnectivity();
    return _hasInternetConnectivity(results);
  }

  @override
  Future<bool> get isOffline async {
    return !(await isOnline);
  }

  /// Helper method to determine if connectivity results indicate internet access.
  bool _hasInternetConnectivity(List<ConnectivityResult> results) {
    return results.any(
      (result) =>
          result == ConnectivityResult.mobile ||
          result == ConnectivityResult.wifi ||
          result == ConnectivityResult.ethernet,
    );
  }

  void dispose() {
    _statusController?.close();
  }
}

/// Riverpod provider for ConnectivityService.
/// 
/// **Validates: Requirements 10.9, 10.10**
@riverpod
ConnectivityService connectivityService(ConnectivityServiceRef ref) {
  final service = ConnectivityServiceImpl();
  
  // Dispose of the service when the provider is disposed
  ref.onDispose(() {
    service.dispose();
  });

  return service;
}

/// Stream provider for connectivity status as boolean.
/// 
/// Emits true when online, false when offline.
/// Initial value is emitted immediately based on current connectivity.
/// 
/// **Validates: Requirements 10.9, 10.10**
@riverpod
Stream<bool> connectivityStatus(ConnectivityStatusRef ref) async* {
  final service = ref.watch(connectivityServiceProvider);
  
  // Emit initial connectivity status immediately
  final initialStatus = await service.isOnline;
  yield initialStatus;
  
  // Then emit subsequent connectivity changes
  yield* service.onConnectivityChanged;
}

/// Provider for current connectivity status.
/// 
/// Returns true if device is currently online.
/// 
/// **Validates: Requirements 10.10**
@riverpod
Future<bool> isOnline(IsOnlineRef ref) async {
  final service = ref.watch(connectivityServiceProvider);
  return service.isOnline;
}
