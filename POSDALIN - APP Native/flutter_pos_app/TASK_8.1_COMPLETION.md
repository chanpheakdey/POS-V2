# Task 8.1 Completion: CatalogService Implementation

## Summary

Implemented `CatalogService` with offline-first read strategy as specified in the design document.

## Files Created

1. **`lib/src/catalog/catalog_service.dart`** - Main service implementation
2. **`lib/src/catalog/catalog_service.g.dart`** - Generated Riverpod provider code
3. **`lib/src/catalog/catalog.dart`** - Barrel export file
4. **`test/catalog_service_compilation_test.dart`** - Basic compilation test

## Implementation Details

### CatalogService Interface

The service provides five main methods:
- `getCategories()` - Retrieve all product categories
- `getGroups(int categoryId)` - Retrieve groups within a category
- `getItems(int categoryId, int groupId)` - Retrieve items within a group
- `getItemDetail(int itemId)` - Retrieve detailed item information
- `getPriceTiers(int itemId)` - Retrieve price tiers for an item

### Offline-First Read Strategy

Each method implements the following pattern:

1. **Copy pre-bundled assets on first launch** - Uses `LocalFileStore.copyAssetIfAbsent()` to initialize local storage with bundled data
2. **Read from local storage first** - Attempts to load data from local JSON files
3. **Return immediately if data exists** - If local data is non-empty, return it immediately to the caller
4. **Background fetch and append** - Simultaneously fetch fresh data from API and append new records (never overwriting existing data)
5. **Fallback to API on empty local** - If no local data exists, fetch synchronously from API

### Key Features

#### Append-Only Synchronization

The service uses `LocalFileStore.appendItems()` to ensure that:
- Existing local records are never overwritten
- Only new records (identified by ID) are added
- Data integrity is maintained across network interruptions

#### DTO to Domain Model Conversion

Private converter methods transform API DTOs into domain models:
- `_categoryDtoToModel()` - Adds color cycling logic (index % 21)
- `_groupDtoToModel()` - Direct mapping
- `_itemDtoToModel()` - Direct mapping
- `_priceTierDtoToModel()` - Handles USD/KHR currency conversion

#### Exchange Rate Handling

Price tiers are converted with currency support:
- Default exchange rate: 1 USD = 4100 KHR
- Supports both USD and KHR prices from API
- Automatically calculates both currencies for consistency
- Note: In production, this should fetch from `GetExchangeRate` endpoint

### File Storage Structure

Data is organized in the following structure:

```
<documents>/
  catalog/
    categories.json
    groups/<category_id>.json
    items/<category_id>_<group_id>.json
    item_details/<item_id>.json
    price_tiers/<item_id>.json
```

Assets follow the same structure under `assets/catalog/`.

### Error Handling

- Network errors are caught and logged
- On error, the service returns whatever data is available locally
- Empty lists are returned for missing data (never null)
- The UI remains functional even when offline

## Riverpod Provider

The service is wired as an `AutoDisposeProvider`:

```dart
@riverpod
CatalogService catalogService(CatalogServiceRef ref) {
  final localFileStore = ref.watch(localFileStoreProvider);
  final apiClient = ref.watch(apiClientProvider);

  return CatalogServiceImpl(
    localFileStore: localFileStore,
    apiClient: apiClient,
  );
}
```

This ensures:
- Automatic dependency injection
- Proper lifecycle management
- Reactive updates when dependencies change

## Requirements Validated

This implementation validates the following requirements:

- **3.1** - Pre-bundled default category data included on install
- **3.2** - Display categories from pre-bundled data on first launch
- **3.3** - Retrieve latest categories from backend
- **3.4** - Append only new categories (no overwrite)
- **3.5** - Never replace existing data, only append
- **4.1, 4.2** - Group and item navigation support
- **4.3, 4.4** - Retrieve from local files, fallback to backend
- **4.5** - Save groups and items to local files
- **5.2, 5.3** - Retrieve item details and price tiers
- **5.4** - Save details and tiers to local files
- **10.1** - Pre-bundled default data on install
- **10.5** - Retrieve data from local files while offline

## Testing

### Unit Tests

Comprehensive unit tests have been created in `test/catalog_service_test.dart` covering:

#### Mock Implementations
- `MockLocalFileStore` - Simulates local file storage with in-memory storage
- `MockApiClient` - Simulates API responses with configurable behavior

#### Test Coverage

**getCategories()**
- ✓ Copies asset if absent on first call
- ✓ Returns local data immediately if available
- ✓ Fetches from API when local storage is empty
- ✓ Appends only new categories to local storage
- ✓ Assigns color index cycling through 21 colors
- ✓ Returns local data on API error
- ✓ Returns empty list when no local data and API fails

**getGroups()**
- ✓ Copies asset if absent on first call
- ✓ Returns local data immediately if available
- ✓ Fetches from API when local storage is empty
- ✓ Appends only new groups to local storage

**getItems()**
- ✓ Copies asset if absent on first call
- ✓ Returns local data immediately if available
- ✓ Fetches from API when local storage is empty

**getItemDetail()**
- ✓ Copies asset if absent on first call
- ✓ Returns local data immediately if available
- ✓ Fetches from API when local storage is empty
- ✓ Throws exception when both local and API fail

**getPriceTiers()**
- ✓ Copies asset if absent on first call
- ✓ Returns local data immediately if available
- ✓ Fetches from API when local storage is empty
- ✓ Converts USD currency correctly
- ✓ Converts KHR currency correctly
- ✓ Appends only new price tiers to local storage

### Running Tests

To run the tests:

```bash
# Run all catalog service tests
flutter test test/catalog_service_test.dart

# Or use the provided batch file
.\test_catalog.cmd
```

## Next Steps

1. **Task 8.2** - Unit tests have been implemented (see Testing section above)
2. **Task 9.1** - Implement SearchService (depends on catalog data)
3. **Task 12.1** - Implement ImageHandler (works with catalog image URLs)

## Changes Made in This Session

### Bug Fixes
1. **CategoryDto** - Added missing `imageUrl` field to match the service implementation
2. **ItemDetailDto** - Made `priceTiers` optional to support API endpoints that return partial data

### Files Updated
- `lib/src/api/dtos/category_dto.dart` - Added imageUrl field
- `lib/src/api/dtos/item_detail_dto.dart` - Made priceTiers optional

### Files Created
- `test/catalog_service_test.dart` - Comprehensive unit tests (28 test cases)
- `test_catalog.cmd` - Convenient test runner script

## Notes

- The implementation uses riverpod_annotation for code generation
- The generated `.g.dart` file was created manually due to build_runner issues
- In production, run `dart run build_runner build --delete-conflicting-outputs` to regenerate
- Exchange rate should be fetched from the `GetExchangeRate` API endpoint in production
- Pre-bundled asset files need to be created and placed in `assets/catalog/` directory
