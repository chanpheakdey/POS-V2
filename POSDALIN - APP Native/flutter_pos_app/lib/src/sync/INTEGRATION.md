# SyncService Integration Guide

This document explains how to integrate the SyncService into the Flutter POS app.

## Integration Points

### 1. App Initialization

Initialize the auto-sync provider when the app starts:

```dart
// lib/main.dart
void main() {
  runApp(
    ProviderScope(
      child: MyApp(),
    ),
  );
}

class MyApp extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Initialize auto-sync on app start
    ref.watch(autoSyncProvider);
    
    return MaterialApp(
      // ... app configuration
    );
  }
}
```

### 2. Foreground/Background Handling

Trigger sync when app returns to foreground:

```dart
// lib/src/app/app_lifecycle_observer.dart
class AppLifecycleObserver extends WidgetsBindingObserver {
  final WidgetRef ref;
  
  AppLifecycleObserver(this.ref);
  
  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      // App came to foreground - trigger sync
      ref.read(autoSyncProvider.notifier).triggerSync();
    }
  }
}

// Register in main app widget
class _MyAppState extends State<MyApp> with WidgetsBindingObserver {
  late AppLifecycleObserver _observer;
  
  @override
  void initState() {
    super.initState();
    _observer = AppLifecycleObserver(ref);
    WidgetsBinding.instance.addObserver(_observer);
  }
  
  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(_observer);
    super.dispose();
  }
}
```

### 3. Manual Sync Trigger

Add a manual sync button in settings or app bar:

```dart
// lib/src/catalog/widgets/sync_button.dart
class SyncButton extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final autoSync = ref.watch(autoSyncProvider);
    
    return IconButton(
      icon: autoSync.isLoading
          ? CircularProgressIndicator()
          : Icon(Icons.sync),
      onPressed: autoSync.isLoading
          ? null
          : () {
              ref.read(autoSyncProvider.notifier).triggerSync();
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('Syncing data...')),
              );
            },
      tooltip: 'Sync catalog data',
    );
  }
}
```

### 4. Sync Status Display

Show last sync time to users:

```dart
// lib/src/settings/widgets/sync_status.dart
class SyncStatus extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final syncService = ref.watch(syncServiceProvider);
    final lastSync = syncService.lastSyncedAt;
    
    return ListTile(
      leading: Icon(Icons.cloud_sync),
      title: Text('Last Sync'),
      subtitle: Text(
        lastSync != null
            ? _formatDateTime(lastSync)
            : 'Never synced',
      ),
      trailing: TextButton(
        onPressed: () {
          ref.read(autoSyncProvider.notifier).triggerSync();
        },
        child: Text('SYNC NOW'),
      ),
    );
  }
  
  String _formatDateTime(DateTime dt) {
    final now = DateTime.now();
    final diff = now.difference(dt);
    
    if (diff.inMinutes < 1) return 'Just now';
    if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
    if (diff.inHours < 24) return '${diff.inHours}h ago';
    return '${diff.inDays}d ago';
  }
}
```

### 5. Connectivity Indicator

Display online/offline status:

```dart
// lib/src/shared/widgets/connectivity_indicator.dart
class ConnectivityIndicator extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final connectivityAsync = ref.watch(connectivityStreamProvider);
    
    return connectivityAsync.when(
      data: (results) {
        final isOnline = results.any(
          (r) => r == ConnectivityResult.mobile ||
                 r == ConnectivityResult.wifi ||
                 r == ConnectivityResult.ethernet,
        );
        
        return Container(
          padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: isOnline ? Colors.green : Colors.grey,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                isOnline ? Icons.cloud_done : Icons.cloud_off,
                size: 16,
                color: Colors.white,
              ),
              SizedBox(width: 4),
              Text(
                isOnline ? 'Online' : 'Offline',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        );
      },
      loading: () => SizedBox.shrink(),
      error: (_, __) => SizedBox.shrink(),
    );
  }
}
```

### 6. Catalog Service Integration

Update CatalogService to use locally synced data:

```dart
// lib/src/catalog/catalog_service.dart
class CatalogServiceImpl implements CatalogService {
  final ApiClient _apiClient;
  final LocalFileStore _localFileStore;
  final SyncService _syncService;
  
  @override
  Future<List<Category>> getCategories() async {
    // Read from local storage (synced by SyncService)
    final categories = await _localFileStore.readList<Category>(
      StoragePaths.categoriesFile,
      (json) => Category.fromJson(json),
    );
    
    // If empty, trigger sync to populate
    if (categories.isEmpty) {
      await _syncService.synchronize();
      return await _localFileStore.readList<Category>(
        StoragePaths.categoriesFile,
        (json) => Category.fromJson(json),
      );
    }
    
    return categories;
  }
  
  // Similar pattern for groups and items
}
```

## Sync Flow Diagram

```
App Start
    ↓
Initialize AutoSync Provider
    ↓
Check Connectivity
    ↓
[If Online] → Trigger Initial Sync
    ↓
Listen for Connectivity Changes
    ↓
[On Reconnect] → Debounce (2s) → Trigger Sync
    ↓
Background Sync Process:
    1. Load sync metadata
    2. Fetch categories from API
    3. Append new categories to local store
    4. For each category:
       - Fetch groups
       - Append new groups
       - For each group:
         * Fetch items
         * Append new items
    5. Update sync metadata timestamps
    6. Save metadata to local store
```

## Testing Integration

Test the integration with mock providers:

```dart
// test/integration/sync_integration_test.dart
void main() {
  testWidgets('AutoSync initializes and triggers sync', (tester) async {
    final container = ProviderContainer(
      overrides: [
        apiClientProvider.overrideWithValue(mockApiClient),
        localFileStoreProvider.overrideWithValue(mockLocalFileStore),
      ],
    );
    
    await tester.pumpWidget(
      UncontrolledProviderScope(
        container: container,
        child: MyApp(),
      ),
    );
    
    // Verify auto-sync was triggered
    await tester.pumpAndSettle();
    
    // Verify sync service methods were called
    verify(() => mockApiClient.getCategories()).called(1);
  });
}
```

## Best Practices

1. **Don't block UI**: Always run sync in background
2. **Provide feedback**: Show sync status to users
3. **Handle errors gracefully**: Network failures shouldn't crash the app
4. **Debounce triggers**: Avoid rapid-fire sync requests
5. **Test thoroughly**: Verify offline and online scenarios
6. **Monitor performance**: Track sync duration and data size

## Common Issues

### Issue: Sync runs too frequently
**Solution**: Increase debounce delay in AutoSync provider

### Issue: Sync fails silently
**Solution**: Add user-visible error notifications

### Issue: Large data sets cause slow sync
**Solution**: Implement pagination or batch processing

### Issue: Connectivity detection is unreliable
**Solution**: Add actual API ping test before sync

## Related Modules

- **ApiClient**: Fetches data from backend
- **LocalFileStore**: Manages local JSON files
- **CatalogService**: Provides catalog data to UI
- **ConnectivityService**: Monitors network status

## Next Steps

After integrating SyncService:

1. Test offline mode thoroughly
2. Verify append-only behavior preserves data
3. Monitor sync performance with real data
4. Add analytics to track sync success/failure rates
5. Consider adding sync conflict resolution if needed
