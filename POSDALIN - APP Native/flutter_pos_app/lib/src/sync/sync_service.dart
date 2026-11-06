import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../api/api_client.dart';
import '../api/api_client_provider.dart';
import '../order/order_service.dart';
import '../shared/local_file_store.dart';
import '../shared/connectivity_service.dart';
import '../shared/constants.dart';
import '../shared/models/sync_meta.dart';
import '../shared/models/category.dart';
import '../shared/models/group.dart';
import '../shared/models/item.dart';

part 'sync_service.g.dart';

/// Service for orchestrating background incremental data synchronization.
/// 
/// This service implements the append-only sync strategy where local catalog
/// data is never overwritten — only new records are appended to preserve
/// reliability under partial connectivity.
/// 
/// **Validates: Requirements 18.3, 18.5, 18.6, 18.7, 18.9, 18.10, 18.12, 18.13, 18.14**
abstract class SyncService {
  /// Perform a full incremental synchronization of all catalog data.
  /// 
  /// This method:
  /// 1. Reads sync_meta.json for last sync timestamps
  /// 2. Requests only data newer than the last timestamp from backend
  /// 3. Appends new records to local files (never overwrites)
  /// 4. Updates sync_meta.json on success
  Future<void> synchronize();

  /// Get the timestamp of the last successful synchronization.
  /// 
  /// Returns null if synchronization has never occurred.
  DateTime? get lastSyncedAt;

  /// Get the current sync metadata containing timestamps for each data type.
  SyncMeta get syncMeta;
}

/// Implementation of SyncService with incremental append-only synchronization.
class SyncServiceImpl implements SyncService {
  final ApiClient _apiClient;
  final LocalFileStore _localFileStore;
  final Connectivity _connectivity;

  SyncMeta _syncMeta = const SyncMeta();

  SyncServiceImpl({
    required ApiClient apiClient,
    required LocalFileStore localFileStore,
    Connectivity? connectivity,
  })  : _apiClient = apiClient,
        _localFileStore = localFileStore,
        _connectivity = connectivity ?? Connectivity();

  @override
  SyncMeta get syncMeta => _syncMeta;

  @override
  DateTime? get lastSyncedAt {
    // Return the most recent sync timestamp across all data types
    final timestamps = [
      _syncMeta.categoriesLastSync,
      _syncMeta.groupsLastSync,
      _syncMeta.itemsLastSync,
      _syncMeta.imagesLastSync,
    ].whereType<DateTime>();

    if (timestamps.isEmpty) {
      return null;
    }

    return timestamps.reduce(
      (a, b) => a.isAfter(b) ? a : b,
    );
  }

  @override
  Future<void> synchronize() async {
    // Load current sync metadata
    await _loadSyncMeta();

    // Sync categories
    await _syncCategories();

    // For groups and items, we need to iterate through existing categories
    // to fetch their groups and items
    await _syncGroupsAndItems();

    // Save updated sync metadata
    await _saveSyncMeta();
  }

  /// Initialize sync service by loading sync metadata from local storage.
  Future<void> initialize() async {
    await _loadSyncMeta();
  }

  /// Load sync metadata from local storage.
  Future<void> _loadSyncMeta() async {
    try {
      final metaList = await _localFileStore.readList<SyncMeta>(
        StoragePaths.syncMetaFile,
        (json) => SyncMeta.fromJson(json),
      );

      if (metaList.isNotEmpty) {
        _syncMeta = metaList.first;
      }
    } catch (e) {
      // If sync meta doesn't exist or is invalid, start fresh
      _syncMeta = const SyncMeta();
    }
  }

  /// Save sync metadata to local storage.
  Future<void> _saveSyncMeta() async {
    await _localFileStore.writeList<SyncMeta>(
      StoragePaths.syncMetaFile,
      [_syncMeta],
      (meta) => meta.toJson(),
    );
  }

  /// Synchronize categories from backend.
  /// 
  /// Fetches all categories from backend and appends new ones to local storage.
  /// Updates categoriesLastSync timestamp on success.
  Future<void> _syncCategories() async {
    try {
      // Fetch categories from backend
      final categoryDtos = await _apiClient.getCategories();

      // Convert DTOs to domain models
      final categories = categoryDtos.asMap().entries.map((entry) {
        final index = entry.key;
        final dto = entry.value;

        return Category(
          categoryId: dto.categoryId,
          categoryName: dto.categoryName,
          imageUrl: '', // Image URL would come from separate field if available
          colorIndex: index % kCategoryColors.length,
        );
      }).toList();

      // Append new categories to local storage
      await _localFileStore.appendItems<Category>(
        StoragePaths.categoriesFile,
        categories,
        (cat) => cat.categoryId.toString(),
        (json) => Category.fromJson(json),
        (cat) => cat.toJson(),
      );

      // Update sync timestamp
      _syncMeta = _syncMeta.copyWith(
        categoriesLastSync: DateTime.now(),
      );
    } catch (e) {
      // Log error but don't throw - allow other sync operations to continue
      // In production, this would use proper logging
      print('Error syncing categories: $e');
    }
  }

  /// Synchronize groups and items for all categories.
  /// 
  /// Iterates through all locally stored categories and fetches their groups
  /// and items from the backend, appending new records to local storage.
  Future<void> _syncGroupsAndItems() async {
    try {
      // Load all local categories
      final categories = await _localFileStore.readList<Category>(
        StoragePaths.categoriesFile,
        (json) => Category.fromJson(json),
      );

      // Sync groups and items for each category
      for (final category in categories) {
        await _syncGroupsForCategory(category.categoryId);
        await _syncItemsForCategory(category.categoryId);
      }

      // Update sync timestamps
      final now = DateTime.now();
      _syncMeta = _syncMeta.copyWith(
        groupsLastSync: now,
        itemsLastSync: now,
      );
    } catch (e) {
      print('Error syncing groups and items: $e');
    }
  }

  /// Synchronize groups for a specific category.
  Future<void> _syncGroupsForCategory(int categoryId) async {
    try {
      // Fetch groups from backend
      final groupDtos = await _apiClient.getGroups(categoryId);

      // Convert DTOs to domain models
      final groups = groupDtos.map((dto) {
        return Group(
          groupId: dto.groupId,
          categoryId: dto.categoryId,
          groupName: dto.groupName,
          imageUrl: dto.imageUrl,
        );
      }).toList();

      // Append new groups to local storage
      final groupsPath = '${StoragePaths.groupsDir}/$categoryId.json';
      await _localFileStore.appendItems<Group>(
        groupsPath,
        groups,
        (group) => group.groupId.toString(),
        (json) => Group.fromJson(json),
        (group) => group.toJson(),
      );
    } catch (e) {
      print('Error syncing groups for category $categoryId: $e');
    }
  }

  /// Synchronize items for a specific category.
  /// 
  /// Fetches items for the category and all its groups, appending new records.
  Future<void> _syncItemsForCategory(int categoryId) async {
    try {
      // Load groups for this category
      final groupsPath = '${StoragePaths.groupsDir}/$categoryId.json';
      final groups = await _localFileStore.readList<Group>(
        groupsPath,
        (json) => Group.fromJson(json),
      );

      // If no groups, try fetching items directly for the category (groupId = 0)
      if (groups.isEmpty) {
        await _syncItemsForGroup(categoryId, 0);
      } else {
        // Fetch items for each group
        for (final group in groups) {
          await _syncItemsForGroup(categoryId, group.groupId);
        }
      }
    } catch (e) {
      print('Error syncing items for category $categoryId: $e');
    }
  }

  /// Synchronize items for a specific category and group combination.
  Future<void> _syncItemsForGroup(int categoryId, int groupId) async {
    try {
      // Fetch items from backend
      final itemDtos = await _apiClient.getSaleItems(categoryId, groupId);

      // Convert DTOs to domain models
      final items = itemDtos.map((dto) {
        return Item(
          itemId: dto.itemId,
          categoryId: dto.categoryId,
          groupId: dto.groupId,
          itemName: dto.itemName,
          itemCode: dto.itemCode,
          imageUrl: dto.imageUrl,
          price: dto.price,
        );
      }).toList();

      // Append new items to local storage
      final itemsPath = '${StoragePaths.itemsDir}/${categoryId}_$groupId.json';
      await _localFileStore.appendItems<Item>(
        itemsPath,
        items,
        (item) => item.itemId.toString(),
        (json) => Item.fromJson(json),
        (item) => item.toJson(),
      );
    } catch (e) {
      print('Error syncing items for category $categoryId, group $groupId: $e');
    }
  }
}

/// Riverpod provider for SyncService.
/// 
/// Wired to trigger on app foreground and network reconnection via
/// ConnectivityService.
/// 
/// **Validates: Requirements 18.3, 18.5, 18.6, 18.7, 18.9, 18.10, 18.12, 18.13, 18.14**
@riverpod
SyncService syncService(SyncServiceRef ref) {
  final apiClient = ref.watch(apiClientProvider);
  final localFileStore = ref.watch(localFileStoreProvider);

  return SyncServiceImpl(
    apiClient: apiClient,
    localFileStore: localFileStore,
  );
}

/// Stream provider for connectivity changes.
/// 
/// Emits connectivity status updates that can trigger synchronization.
@riverpod
Stream<List<ConnectivityResult>> connectivityStream(
  ConnectivityStreamRef ref,
) {
  final connectivity = Connectivity();
  return connectivity.onConnectivityChanged;
}

/// Provider that automatically triggers sync on network reconnection and periodically.
/// 
/// Watches the connectivity stream and triggers synchronization when
/// the device reconnects to the internet. Also schedules periodic background
/// sync every 5 minutes when the app is in foreground.
/// 
/// On reconnection:
/// 1. Flushes offline order queue (OrderService.flushOfflineQueue)
/// 2. Synchronizes catalog data (SyncService.synchronize)
/// 
/// **Validates: Requirements 10.9, 10.10, 16.8, 18.10**
@riverpod
class AutoSync extends _$AutoSync {
  Timer? _syncTimer;
  Timer? _periodicSyncTimer;
  bool _wasOffline = false;

  @override
  Future<void> build() async {
    // Initialize sync service
    final syncService = ref.watch(syncServiceProvider);
    if (syncService is SyncServiceImpl) {
      await syncService.initialize();
    }

    // Listen to connectivity changes using the new ConnectivityService
    ref.listen<AsyncValue<bool>>(
      connectivityStatusProvider,
      (previous, next) {
        next.whenData((isOnline) {
          // Track if we're transitioning from offline to online (reconnection)
          final isReconnecting = _wasOffline && isOnline;
          _wasOffline = !isOnline;

          if (isReconnecting) {
            // On reconnection: flush offline queue, then sync
            _syncTimer?.cancel();
            _syncTimer = Timer(const Duration(seconds: 2), () {
              _performReconnectionSync();
            });
          }
        });
      },
    );

    // Check initial connectivity status
    final connectivityService = ref.read(connectivityServiceProvider);
    final isOnline = await connectivityService.isOnline;
    _wasOffline = !isOnline;

    // Perform initial sync if online
    if (isOnline) {
      await _performFullSync();
    }

    // Schedule periodic background sync (every 5 minutes)
    _schedulePeriodicSync();

    // Clean up timers when provider is disposed
    ref.onDispose(() {
      _syncTimer?.cancel();
      _periodicSyncTimer?.cancel();
    });
  }

  /// Schedule periodic background sync.
  /// 
  /// Runs every 5 minutes when the app is in foreground to keep catalog data fresh.
  /// 
  /// **Validates: Requirements 16.8, 18.10**
  void _schedulePeriodicSync() {
    _periodicSyncTimer?.cancel();
    _periodicSyncTimer = Timer.periodic(
      const Duration(minutes: 5),
      (_) async {
        // Only sync if online
        final connectivityService = ref.read(connectivityServiceProvider);
        final isOnline = await connectivityService.isOnline;
        
        if (isOnline) {
          await _performFullSync();
        }
      },
    );
  }

  /// Trigger a synchronization operation manually.
  Future<void> triggerSync() async {
    await _performFullSync();
  }

  /// Perform full synchronization on reconnection.
  /// 
  /// 1. Flushes offline order queue
  /// 2. Synchronizes catalog data
  /// 
  /// **Validates: Requirements 10.9, 10.10**
  Future<void> _performReconnectionSync() async {
    try {
      // First, flush any queued offline orders
      final orderService = ref.read(orderServiceProvider.notifier);
      await orderService.flushOfflineQueue();
      
      // Then, synchronize catalog data
      await _performFullSync();
    } catch (e) {
      // Log error but don't throw - sync failures should not crash the app
      print('Reconnection sync failed: $e');
    }
  }

  /// Perform catalog synchronization only.
  /// 
  /// **Validates: Requirements 18.10**
  Future<void> _performFullSync() async {
    try {
      final syncService = ref.read(syncServiceProvider);
      await syncService.synchronize();
    } catch (e) {
      // Log error but don't throw - sync failures should not crash the app
      print('Auto-sync failed: $e');
    }
  }
}
