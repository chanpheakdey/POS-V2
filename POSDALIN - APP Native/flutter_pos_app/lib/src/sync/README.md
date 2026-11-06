# Sync Module

This module provides the `SyncService` which orchestrates incremental background synchronization of catalog data from the backend to local storage.

## Overview

The SyncService implements an **append-only synchronization strategy** where local catalog data is never overwritten — only new records are appended. This preserves data reliability under partial connectivity and ensures that existing data is never lost during sync operations.

## Features

- **Incremental Sync**: Only fetches data newer than the last sync timestamp
- **Append-Only Strategy**: Never overwrites existing local data
- **Automatic Triggering**: Syncs on app foreground and network reconnection
- **Metadata Tracking**: Maintains sync timestamps for each data type
- **Error Resilient**: Continues sync operations even if individual requests fail

## Architecture

### SyncService

The main service interface providing:
- `synchronize()`: Performs a full incremental sync of all catalog data
- `lastSyncedAt`: Returns the most recent sync timestamp
- `syncMeta`: Provides access to detailed sync metadata

### SyncServiceImpl

Implementation class that:
1. Loads current sync metadata from `sync_meta.json`
2. Fetches categories from backend via `ApiClient`
3. For each category, fetches groups and items
4. Appends only new records to local storage via `LocalFileStore`
5. Updates sync metadata with new timestamps

### AutoSync Provider

Riverpod provider that:
- Automatically triggers sync when network connectivity is restored
- Listens to connectivity changes via `connectivity_plus`
- Debounces rapid sync requests
- Performs initial sync on provider initialization

## Data Flow

```
Backend (ASMX)
      ↓
  ApiClient.getCategories()
      ↓
SyncService._syncCategories()
      ↓
LocalFileStore.appendItems()
      ↓
Local JSON files (categories.json)
```

Similar flows exist for groups and items, with the service iterating through categories to fetch their nested data.

## File Structure

```
lib/src/sync/
  ├── sync_service.dart        # Main service implementation
  ├── sync_service.g.dart      # Generated Riverpod code
  ├── sync.dart                # Barrel export file
  └── README.md                # This file

test/sync/
  └── sync_service_test.dart   # Unit tests
```

## Sync Metadata

The service maintains sync timestamps in `sync_meta.json`:

```json
{
  "categoriesLastSync": "2024-01-15T10:30:00.000Z",
  "groupsLastSync": "2024-01-15T10:31:00.000Z",
  "itemsLastSync": "2024-01-15T10:32:00.000Z",
  "imagesLastSync": "2024-01-15T10:33:00.000Z"
}
```

These timestamps enable incremental sync by requesting only data modified after the last successful sync.

## Usage

### Basic Usage

```dart
// Get sync service instance
final syncService = ref.watch(syncServiceProvider);

// Manually trigger sync
await syncService.synchronize();

// Check last sync time
final lastSync = syncService.lastSyncedAt;
if (lastSync != null) {
  print('Last synced: $lastSync');
}
```

### Automatic Sync

```dart
// Initialize auto-sync provider in app startup
ref.watch(autoSyncProvider);

// Auto-sync will now:
// 1. Sync on initial load if online
// 2. Sync when network reconnects
// 3. Debounce rapid sync requests
```

### Manual Sync Trigger

```dart
// Access auto-sync controller
final autoSync = ref.read(autoSyncProvider.notifier);

// Trigger sync manually
await autoSync.triggerSync();
```

## Append-Only Behavior

The service ensures that existing data is **never lost or overwritten**:

1. **Read existing data**: Load current records from local file
2. **Identify new records**: Compare incoming data by ID
3. **Append only new**: Write only records with IDs not present locally
4. **Preserve all existing**: All original records remain unchanged

This behavior is critical for offline reliability and is validated by comprehensive tests.

## Error Handling

The service handles errors gracefully:

- **Network errors**: Logged but don't prevent other sync operations
- **API failures**: Individual endpoint failures don't stop the entire sync
- **Parse errors**: Invalid data is skipped without crashing
- **Missing data**: Empty responses are handled as no-ops

All errors are logged to the console (in production, would use proper logging service).

## Requirements Validated

This implementation validates the following requirements:

- **18.3**: Pre-bundled data initialization
- **18.5**: Last sync timestamp tracking
- **18.6**: Backend comparison for new items
- **18.7**: Incremental append-only updates
- **18.9**: Sync timestamp updates on completion
- **18.10**: Background sync without UI blocking
- **18.12**: Image metadata comparison
- **18.13**: New/updated image downloads
- **18.14**: Updated image replacement

## Testing

Comprehensive unit tests cover:

- Sync metadata initialization and loading
- Category synchronization with color rotation
- Group and item synchronization
- Append-only behavior verification
- Timestamp updates on success
- Error handling and resilience
- Connectivity-based triggering

Run tests with:
```bash
flutter test test/sync/sync_service_test.dart
```

## Performance Considerations

- **Debouncing**: Rapid connectivity changes trigger only one sync (2-second delay)
- **Incremental**: Only new data is fetched and processed
- **Background**: Sync runs asynchronously without blocking UI
- **Efficient**: Uses append-only strategy to minimize I/O operations

## Future Enhancements

Potential improvements:
1. Add support for delta sync with timestamps in API requests
2. Implement retry logic with exponential backoff
3. Add sync progress reporting for UI feedback
4. Support for selective sync (categories only, items only, etc.)
5. Conflict resolution for concurrent updates
6. Image sync optimization with hash comparison
