# Flutter POS App - Project Structure

## Overview

This document describes the complete project structure, module organization, and architectural decisions for the Flutter POS mobile application.

## Directory Structure

```
flutter_pos_app/
│
├── android/                      # Android-specific configuration
├── ios/                          # iOS-specific configuration
├── linux/                        # Linux desktop configuration
├── macos/                        # macOS desktop configuration
├── windows/                      # Windows desktop configuration
├── web/                          # Web platform configuration
│
├── assets/                       # Pre-bundled static assets
│   ├── catalog/                  # Pre-bundled catalog JSON files
│   │   ├── categories.json       # Category list (empty initially)
│   │   ├── groups/               # Group data by category ID
│   │   ├── items/                # Item data by category_group ID
│   │   ├── item_details/         # Detailed item info by item ID
│   │   └── price_tiers/          # Price tiers by item ID
│   │
│   ├── images/                   # Pre-bundled optimized images
│   │   ├── categories/           # 300x200 category images
│   │   ├── groups/               # 300x200 group images
│   │   ├── items/                # 200x110 item thumbnails
│   │   └── placeholder.png       # Fallback image
│   │
│   └── fonts/                    # Custom fonts
│       ├── NotoSansKhmer-Regular.ttf
│       └── NotoSansKhmer-Bold.ttf
│
├── lib/                          # Application source code
│   ├── main.dart                 # App entry point
│   │
│   └── src/                      # Feature modules (organized by domain)
│       │
│       ├── api/                  # Remote data source layer
│       │   ├── api_client.dart           # ASMX HTTP client wrapper
│       │   ├── api_exception.dart        # Typed API exceptions
│       │   └── dto/                      # Data transfer objects
│       │       ├── category_dto.dart
│       │       ├── group_dto.dart
│       │       ├── item_dto.dart
│       │       ├── price_tier_dto.dart
│       │       ├── order_dto.dart
│       │       └── order_payload_dto.dart
│       │
│       ├── shared/               # Shared utilities, models, widgets
│       │   ├── constants.dart            # App-wide constants
│       │   ├── models/                   # Domain models
│       │   │   ├── category.dart
│       │   │   ├── group.dart
│       │   │   ├── item.dart
│       │   │   ├── item_detail.dart
│       │   │   ├── price_tier.dart
│       │   │   ├── cart_item.dart
│       │   │   ├── order.dart
│       │   │   ├── order_detail.dart
│       │   │   ├── sync_meta.dart
│       │   │   └── service_result.dart   # Sealed class for typed errors
│       │   ├── widgets/                  # Reusable UI components
│       │   ├── utils/                    # Helper functions
│       │   └── local_file_store.dart     # File I/O abstraction
│       │
│       ├── auth/                 # Authentication module
│       │   ├── auth_service.dart         # Authentication business logic
│       │   ├── auth_provider.dart        # Riverpod state provider
│       │   └── auth_state.dart           # Authentication state model
│       │
│       ├── catalog/              # Product catalog module
│       │   ├── catalog_service.dart      # Offline-first catalog logic
│       │   ├── catalog_provider.dart     # Riverpod providers
│       │   ├── image_handler.dart        # Image loading priority logic
│       │   └── screens/                  # Catalog UI screens
│       │       ├── home_screen.dart      # Category grid
│       │       ├── group_screen.dart     # Group list
│       │       ├── item_list_screen.dart # Item grid
│       │       └── item_detail_screen.dart
│       │
│       ├── cart/                 # Shopping cart module
│       │   ├── cart_service.dart         # Cart state management
│       │   ├── cart_provider.dart        # Riverpod notifier
│       │   └── screens/
│       │       └── cart_screen.dart      # Cart view with totals
│       │
│       ├── order/                # Order management module
│       │   ├── order_service.dart        # Order submission & history
│       │   ├── order_provider.dart       # Riverpod providers
│       │   └── screens/
│       │       ├── checkout_screen.dart  # Checkout flow
│       │       ├── order_history_screen.dart
│       │       └── order_detail_screen.dart
│       │
│       ├── search/               # Search module
│       │   ├── search_service.dart       # Local catalog search
│       │   ├── search_provider.dart      # Riverpod providers
│       │   └── screens/
│       │       └── search_screen.dart    # Search UI
│       │
│       └── sync/                 # Background synchronization module
│           ├── sync_service.dart         # Incremental sync logic
│           ├── sync_provider.dart        # Riverpod providers
│           └── connectivity_service.dart # Network state monitoring
│
├── test/                         # Tests (mirrors lib/ structure)
│   ├── src/
│   │   ├── api/
│   │   │   └── api_client_test.dart
│   │   ├── shared/
│   │   │   └── local_file_store_test.dart
│   │   ├── auth/
│   │   │   └── auth_service_test.dart
│   │   ├── catalog/
│   │   │   ├── catalog_service_test.dart
│   │   │   └── image_handler_test.dart
│   │   ├── cart/
│   │   │   ├── cart_service_test.dart
│   │   │   └── cart_service_property_test.dart  # Property tests
│   │   ├── order/
│   │   │   └── order_service_test.dart
│   │   ├── search/
│   │   │   └── search_service_test.dart
│   │   └── sync/
│   │       └── sync_service_test.dart
│   │
│   └── widget_test.dart          # Widget and integration tests
│
├── pubspec.yaml                  # Dependency and asset configuration
├── analysis_options.yaml         # Lint rules and analyzer config
├── SETUP.md                      # Setup and development guide
├── PROJECT_STRUCTURE.md          # This file
└── README.md                     # Project overview

```

## Module Responsibilities

### 1. API Module (`lib/src/api/`)
**Purpose**: Remote data source layer for ASMX web service communication

**Key Components**:
- `ApiClient`: HTTP wrapper with 30-second timeout
- DTOs: Parse ASMX responses (JSON and HTML formats)
- `ApiException`: Typed exceptions for network errors

**Dependencies**: `http`, `html` (for HTML parsing)

### 2. Shared Module (`lib/src/shared/`)
**Purpose**: Cross-cutting concerns, domain models, and utilities

**Key Components**:
- `LocalFileStore`: File I/O abstraction with append-only logic
- Domain models: Immutable classes with `fromJson`/`toJson`
- `ServiceResult<T>`: Sealed class for typed error handling
- `constants.dart`: Color array, image dimensions, error messages

**Dependencies**: `path_provider`

### 3. Auth Module (`lib/src/auth/`)
**Purpose**: Token-based authentication with persistent storage

**Key Components**:
- `AuthService`: Login, token validation, logout
- Token stored via `flutter_secure_storage` (no expiration)
- Only `UserLevel='Customer'` allowed

**Dependencies**: `flutter_secure_storage`, `ApiClient`, `LocalFileStore`

### 4. Catalog Module (`lib/src/catalog/`)
**Purpose**: Offline-first product catalog with pre-bundled data

**Key Components**:
- `CatalogService`: Read local-first, fetch in background, append new
- `ImageHandler`: Local → remote → placeholder priority
- Screens: Category grid, group list, item grid, item detail

**Dependencies**: `LocalFileStore`, `ApiClient`, `cached_network_image`

**Read Strategy**:
1. Check `LocalFileStore` first
2. If non-empty, return immediately
3. Fetch from API in background
4. Append only new records (never overwrite)

### 5. Cart Module (`lib/src/cart/`)
**Purpose**: Shopping cart state with persistent storage

**Key Components**:
- `CartService`: Add, remove, quantity management
- `resolvePrice`: Pure function for tier selection (property-tested)
- Persisted to `cart.json` on every mutation

**Dependencies**: `LocalFileStore`

### 6. Order Module (`lib/src/order/`)
**Purpose**: Order placement and history management

**Key Components**:
- `OrderService`: Submit orders, offline queue, history retrieval
- Offline orders stored in `orders/queue.json`
- `flushOfflineQueue`: Drain queue on reconnection (FIFO)

**Dependencies**: `ApiClient`, `LocalFileStore`, `ConnectivityService`

### 7. Search Module (`lib/src/search/`)
**Purpose**: Client-side catalog search

**Key Components**:
- `SearchService`: Case-insensitive partial match on `ItemName`/`ItemCode`
- Minimum 2 characters before querying
- Reads from `LocalFileStore` only (no network)

**Dependencies**: `LocalFileStore`

### 8. Sync Module (`lib/src/sync/`)
**Purpose**: Background incremental synchronization

**Key Components**:
- `SyncService`: Timestamp-based incremental sync
- `ConnectivityService`: Network state monitoring
- Triggers: App foreground, network reconnection

**Dependencies**: `connectivity_plus`, `ApiClient`, `LocalFileStore`

## State Management Architecture

**Framework**: Riverpod (flutter_riverpod ^2.4.0)

**Provider Types**:
- `Provider`: Stateless services (CatalogService, SearchService)
- `NotifierProvider`: Mutable state (CartService)
- `AsyncNotifierProvider`: Async state (AuthService, OrderService)

**Code Generation**: Uses `riverpod_annotation` + `riverpod_generator`

## Navigation Architecture

**Framework**: go_router (^13.0.0)

**Routes**:
- `/login` - Login screen
- `/home` - Category grid (home)
- `/category/:id` - Group list for category
- `/group/:categoryId/:id` - Item list for group
- `/items/:categoryId/:groupId` - Item list (alternative)
- `/item-detail/:id` - Item detail view
- `/cart` - Shopping cart
- `/orders` - Order history
- `/order-detail/:id` - Order detail
- `/search` - Search screen

**Route Guards**:
- On cold start: Check `AuthService` → redirect to `/login` or `/home`
- On `AuthError`: Redirect to `/login`

## Data Flow

### Read Flow (Offline-First)
```
UI → CatalogService
       ↓
   LocalFileStore.readList()
       ↓
   [Non-empty?] → Return immediately to UI
       ↓
   [Empty or Background] → ApiClient.get()
       ↓
   LocalFileStore.appendItems()  (diff by ID, append only)
       ↓
   Notify UI of updates
```

### Write Flow (Orders)
```
UI → OrderService.placeOrder()
       ↓
   [Online?]
       ↓
   YES → ApiClient.submitOrder()
       ↓
   NO → LocalFileStore.append(queue.json)
       ↓
   [Reconnect] → SyncService → flushOfflineQueue()
```

## Error Handling Strategy

**Typed Errors**: `ServiceResult<T>` sealed class

```dart
sealed class ServiceResult<T> {
  const factory ServiceResult.ok(T value) = Ok;
  const factory ServiceResult.networkError(String message) = NetworkError;
  const factory ServiceResult.authError() = AuthError;
  const factory ServiceResult.validationError(String message) = ValidationError;
  const factory ServiceResult.unexpectedError(String message) = UnexpectedError;
}
```

**Error Handling**:
- `NetworkError`: Show snackbar, continue with cached data
- `AuthError`: Redirect to `/login`
- `ValidationError`: Highlight field, show inline message
- `UnexpectedError`: Log + show generic message

## Testing Strategy

### Unit Tests
- Service layer: Mock `ApiClient` and `LocalFileStore`
- Focus: Business logic, error paths, state transitions

### Property-Based Tests (rapid_check)
- **Property 1**: Price tier selection determinism
- **Property 2**: Cart total equals sum of line totals
- **Property 3**: Search returns only matching items
- **Property 4**: Append-only sync never loses data
- **Property 5**: Cart serialization round-trip
- **Property 6**: Adding item increments quantity by 1
- **Property 7**: Removing item produces cart without item

Minimum **100 iterations** per property test.

### Widget Tests
- Screen rendering, user interactions, navigation
- Mock providers with `ProviderScope`

### Integration Tests
- Full flows: Login → Browse → Add to cart → Checkout
- Offline mode testing

## Configuration Files

### pubspec.yaml
- Dependencies (production + dev)
- Asset paths (catalog JSON, images, fonts)
- Font definitions (Khmer support)

### analysis_options.yaml
- Strict lint rules:
  - No implicit casts
  - Prefer const constructors
  - Prefer final locals
  - Single quotes for strings
  - Trailing commas required

### build.yaml
- Code generation configuration for Riverpod

## Local Storage Schema

### Documents Directory Structure
```
<app_documents>/
├── catalog/
│   ├── categories.json           # Array of Category objects
│   ├── groups/
│   │   └── <category_id>.json    # Array of Group objects
│   ├── items/
│   │   └── <cat_id>_<grp_id>.json  # Array of Item objects
│   ├── item_details/
│   │   └── <item_id>.json        # ItemDetail object
│   └── price_tiers/
│       └── <item_id>.json        # Array of PriceTier objects
│
├── images/
│   ├── categories/
│   │   └── <category_id>.png
│   ├── groups/
│   │   └── <group_id>.png
│   └── items/
│       └── <item_id>.png
│
├── orders/
│   └── queue.json                # Array of queued order payloads
│
├── cart.json                     # Current cart state
└── sync_meta.json                # Last sync timestamps
```

## Asset Bundle Structure
```
assets/
├── catalog/
│   ├── categories.json           # Pre-bundled categories (empty initially)
│   ├── groups/                   # Pre-bundled group data
│   ├── items/                    # Pre-bundled item data
│   ├── item_details/             # Pre-bundled item details
│   └── price_tiers/              # Pre-bundled price tiers
│
├── images/
│   ├── categories/               # Pre-optimized 300x200 images
│   ├── groups/                   # Pre-optimized 300x200 images
│   ├── items/                    # Pre-optimized 200x110 images
│   └── placeholder.png
│
└── fonts/
    ├── NotoSansKhmer-Regular.ttf
    └── NotoSansKhmer-Bold.ttf
```

## Key Design Decisions

### 1. Offline-First Architecture
**Decision**: Read from local storage first, fetch from API in background

**Rationale**: 
- Instant app usability after installation
- No loading spinners for catalog browsing
- Resilient to poor network conditions

### 2. Append-Only Sync
**Decision**: Never overwrite existing catalog records, only append new

**Rationale**:
- Prevents data loss from partial syncs
- Preserves reliability under intermittent connectivity
- Simplifies conflict resolution

### 3. Persistent Token (No Expiration)
**Decision**: Store token indefinitely until explicit logout

**Rationale**:
- Matches existing backend behavior
- Better mobile UX (no forced re-login)
- Token validation on app start ensures security

### 4. Pre-bundled Data and Images
**Decision**: Include default catalog and images in app bundle

**Rationale**:
- Immediate browsing without download wait
- Consistent first-run experience
- Reduces initial network load

### 5. Riverpod for State Management
**Decision**: Use Riverpod over other state management solutions

**Rationale**:
- Compile-time safety
- Testability (easy mocking)
- Scoped state (no global state pollution)
- Code generation reduces boilerplate

### 6. go_router for Navigation
**Decision**: Declarative routing with type-safe routes

**Rationale**:
- Route guards for authentication
- Deep linking support
- Type-safe parameters
- Better testability

## Performance Targets

| Metric | Target | Requirement |
|--------|--------|-------------|
| Splash screen duration | ≤ 2 seconds | 16.1 |
| Cold-start time | < 3 seconds | 16.6 |
| Local catalog load | < 500 ms | 16.7 |
| Screen transition | < 300 ms | 16.2 |
| Scrolling frame rate | 60 fps | 16.3 |
| Concurrent downloads | Max 5 | 16.5 |

## Requirements Mapping

| Module | Requirements Validated |
|--------|------------------------|
| Auth | 1.1-1.9, 2.1-2.7, 15.1-15.6, 17.2-17.3, 17.7-17.8 |
| Catalog | 3.1-3.10, 4.1-4.8, 5.1-5.10, 10.1-10.7, 11.1-11.12, 18.1-18.14 |
| Cart | 6.1-6.12, 19.1-19.10 |
| Order | 7.1-7.7, 8.1-8.6 |
| Search | 9.1-9.8 |
| Sync | 18.3, 18.5-18.6, 18.9-18.14 |
| Shared | 10.3-10.4, 12.1-12.5, 13.1-13.7, 14.1-14.6, 16.1-16.8 |

## Next Steps

1. Implement domain models (`lib/src/shared/models/`)
2. Implement `LocalFileStore` with append-only logic
3. Implement `ApiClient` with ASMX endpoint wrappers
4. Implement service layer (Auth, Catalog, Cart, Order, Search, Sync)
5. Write property-based tests for core business logic
6. Implement UI screens with Riverpod providers
7. Wire navigation with go_router
8. Add pre-bundled data and images to assets
9. Integration testing
10. Performance optimization

---

**Last Updated**: Initial setup - Task 1 completed
