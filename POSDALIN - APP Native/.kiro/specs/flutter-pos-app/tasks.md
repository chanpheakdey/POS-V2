# Implementation Plan: Flutter POS App

## Overview

Build a Flutter mobile POS application for Android and iOS that replicates the customer-facing flow of the existing ASP.NET/ASMX backend. The implementation follows a layered architecture (Presentation → Riverpod State → Domain Services → Data Sources) with an offline-first local file store, token-based persistent authentication, and append-only catalog synchronization.

Tasks are ordered so that foundational infrastructure and data models come first, followed by service implementations (each wired to their Riverpod providers), then screens, and finally integration and sync. Property-based tests use `rapid_check` and target the 7 correctness properties defined in the design.

---

## Tasks

- [x] 1. Set up Flutter project structure, dependencies, and core configuration
  - Initialize Flutter project (or validate existing structure) with `flutter create`
  - Add dependencies to `pubspec.yaml`: `riverpod`, `go_router`, `flutter_secure_storage`, `connectivity_plus`, `cached_network_image`, `rapid_check` (dev), `mocktail` (dev)
  - Create top-level directory scaffold: `lib/src/{api,auth,catalog,cart,order,search,sync,shared}`, `assets/catalog/`, `assets/images/`
  - Configure `flutter:assets` section in `pubspec.yaml` to include bundled JSON and image assets
  - Add lint rules (`analysis_options.yaml`) matching the existing project style
  - _Requirements: 10.1, 10.2, 18.1, 18.2_

- [x] 2. Define all domain models and DTO models
  - [x] 2.1 Implement domain model classes
    - Write `Category`, `Group`, `Item`, `ItemDetail`, `PriceTier`, `CartItem`, `Order`, `OrderDetail`, `SyncMeta` as immutable Dart classes with `copyWith`, `==`, and `hashCode`
    - Write `fromJson` / `toJson` for each model
    - Define `kCategoryColors` constant list (21 hex colors) and `colorForIndex` helper
    - _Requirements: 3.6, 3.7, 4.7, 5.5, 5.6, 5.7, 6.5_

  - [x] 2.2 Implement DTO models and ASMX response parsers
    - Write `CategoryDto`, `GroupDto`, `ItemDto`, `ItemDetailDto`, `PriceTierDto`, `OrderDto`, `OrderDetailDto`, `OrderPayloadDto`, `OrderResultDto`
    - Implement `fromJson` for JSON-returning endpoints and HTML-string parsers for endpoints that return HTML
    - _Requirements: 1.2, 3.3, 4.3, 4.4_

  - [x] 2.3 Implement `ServiceResult<T>` sealed class
    - Write `Ok`, `NetworkError`, `AuthError`, `ValidationError`, `UnexpectedError` variants
    - _Requirements: 13.1, 13.2, 13.4, 13.6_

- [x] 3. Implement `LocalFileStore`
  - [x] 3.1 Implement `LocalFileStore` class
    - Write `readList<T>`, `writeList<T>`, `appendItems<T>`, and `copyAssetIfAbsent` methods
    - `appendItems` must read existing records, diff by ID key, and append only new items — never overwriting existing entries
    - Wire as a Riverpod `Provider`
    - _Requirements: 10.3, 10.4, 18.4, 18.7_

  - [ ]* 3.2 Write property test for `appendItems` (Property 4)
    - **Property 4: Append-only sync never loses existing catalog data**
    - **Validates: Requirements 3.4, 3.5, 10.3, 10.4, 18.4, 18.7**
    - Generate arbitrary existing lists and incoming lists; assert every pre-existing record survives and every new-ID record is added

- [x] 4. Implement `ApiClient`
  - [x] 4.1 Implement `ApiClient` with all ASMX endpoints
    - Implement `checkToken`, `login`, `getCategories`, `getGroups`, `getSaleItems`, `getItemDetail`, `getItemUnits`, `submitOrder`, `getOrders`, `expireToken`, `getImageUrl`
    - Set 30-second timeout on every request; throw typed `ApiException` on timeout or network failure
    - Attach token to all authenticated requests
    - Wire as a Riverpod `Provider`
    - _Requirements: 1.2, 2.5, 13.5_

  - [ ]* 4.2 Write unit tests for `ApiClient`
    - Mock HTTP client; test successful responses, timeout → `ApiException`, error status codes
    - _Requirements: 13.1, 13.5_

- [x] 5. Implement `AuthService` and authentication state
  - [x] 5.1 Implement `AuthService`
    - Implement `login`, `validateStoredToken`, `logout`, `currentToken`, `currentUserId`
    - `login`: call `ApiClient.login`, store token via `flutter_secure_storage`, UserLevel='Customer' only
    - `validateStoredToken`: read token from secure storage → call `ApiClient.checkToken`
    - `logout`: call `ApiClient.expireToken`, delete token, clear cart from `LocalFileStore`
    - Wire as a Riverpod `AsyncNotifierProvider`
    - _Requirements: 1.2, 1.3, 1.5, 1.7, 1.8, 2.1, 2.2, 2.6, 2.7, 15.2, 15.3, 17.3_

  - [ ]* 5.2 Write unit tests for `AuthService`
    - Test login success, wrong password (`"Password is incorrect!"`), unknown username (`"User name is incorrect!"`), locked account (`"Your user account has been locked! Please contact IT"`), token validation, logout flow
    - _Requirements: 1.4, 1.5, 1.6, 2.7_

- [x] 6. Implement `CartService` and cart state
  - [x] 6.1 Implement `CartService`
    - Implement `addItem`, `setQuantity`, `removeItem`, `clear`, `quantityFor`, `resolvePrice`, `total`
    - `resolvePrice`: pure function — find tier where `fromQty ≤ qty ≤ toQty` (toQty=0 = unlimited), pick highest matching `fromQty`
    - Persist cart contents to `LocalFileStore` (`cart.json`) on every mutation; reload on app start
    - Wire as a Riverpod `NotifierProvider`
    - _Requirements: 6.1, 6.2, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 19.4, 19.5_

  - [ ]* 6.2 Write property test for `resolvePrice` (Property 1)
    - **Property 1: Price tier selection is deterministic and correct**
    - **Validates: Requirements 6.6, 6.10**
    - Generate arbitrary positive quantities and arbitrary non-empty tier lists; assert correct tier returned

  - [ ]* 6.3 Write property test for cart total (Property 2)
    - **Property 2: Cart total equals sum of line totals**
    - **Validates: Requirements 6.8, 19.7**
    - Generate arbitrary cart states; assert `total == Σ(quantity × unitPriceUsd)`

  - [ ]* 6.4 Write property test for `addItem` (Property 6)
    - **Property 6: Adding an item increments its quantity by exactly 1**
    - **Validates: Requirements 6.1, 6.2**
    - For any cart state and any item, assert quantity increases by 1 and other items are unchanged

  - [ ]* 6.5 Write property test for `removeItem` (Property 7)
    - **Property 7: Removing an item produces a cart without that item**
    - **Validates: Requirements 6.7, 19.6**
    - For any cart containing itemId, assert `quantityFor(itemId) == 0` and total reflects removal

  - [ ]* 6.6 Write property test for cart serialization round-trip (Property 5)
    - **Property 5: Cart serialization round-trip is lossless**
    - **Validates: Requirements 6.9, 19.2**
    - Generate arbitrary cart states; serialize to JSON and deserialize; assert structural equality

- [x] 7. Checkpoint — Ensure all service-layer tests pass
  - Run `flutter test --tags property` and `flutter test` for all unit tests written so far
  - Ask the user if questions arise before continuing

- [x] 8. Implement `CatalogService`
  - [x] 8.1 Implement `CatalogService` with offline-first read strategy
    - Implement `getCategories`, `getGroups`, `getItems`, `getItemDetail`, `getPriceTiers`
    - Read strategy: check `LocalFileStore` first; if non-empty return immediately, then fetch from `ApiClient` in background and append new records
    - On first launch, copy pre-bundled asset files via `LocalFileStore.copyAssetIfAbsent`
    - Wire as a Riverpod `Provider`
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 5.2, 5.3, 5.4, 10.1, 10.5_

  - [ ]* 8.2 Write unit tests for `CatalogService`
    - Mock `LocalFileStore` and `ApiClient`; test local-first path, API fallback, append logic, offline behavior
    - _Requirements: 10.5, 18.3_

- [x] 9. Implement `SearchService`
  - [x] 9.1 Implement `SearchService`
    - Implement `search(String query)` — enforce 2-char minimum, case-insensitive partial match on `ItemName` and `ItemCode`, read from `LocalFileStore` only
    - Wire as a Riverpod `Provider`
    - _Requirements: 9.1, 9.2, 9.3, 9.7, 9.8_

  - [ ]* 9.2 Write property test for `SearchService` (Property 3)
    - **Property 3: Search returns only matching items**
    - **Validates: Requirements 9.2, 9.3, 9.7**
    - Generate arbitrary query strings (≥2 chars) and item catalogs; assert every returned item matches and no matching item is omitted

- [x] 10. Implement `OrderService`
  - [x] 10.1 Implement `OrderService`
    - Implement `placeOrder`, `getOrderHistory`, `queueOfflineOrder`, `flushOfflineQueue`
    - Online: submit via `ApiClient.submitOrder`; offline: append to `orders/queue.json`
    - `flushOfflineQueue`: drain queue FIFO on reconnection
    - Filter out `deleted == true` orders in `getOrderHistory`; sort by `createdDate` descending
    - Wire as a Riverpod `AsyncNotifierProvider`
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 8.1, 8.2, 8.5, 10.8, 10.11_

  - [ ]* 10.2 Write unit tests for `OrderService`
    - Test successful submission, offline queue, flush on reconnect, order history sort and delete filter
    - _Requirements: 7.4, 7.5, 8.2, 8.5_

- [x] 11. Implement `SyncService`
  - [x] 11.1 Implement `SyncService`
    - Implement `synchronize` and `lastSyncedAt`
    - Read/write `sync_meta.json` via `LocalFileStore`
    - Request only data newer than last sync timestamp from `ApiClient`
    - Append new records for categories, groups, items; compare image metadata to identify new/updated images
    - Wire as a Riverpod `Provider`; trigger on app foreground and network reconnection via `ConnectivityService`
    - _Requirements: 18.3, 18.5, 18.6, 18.7, 18.9, 18.10, 18.12, 18.13, 18.14_

  - [ ]* 11.2 Write unit tests for `SyncService`
    - Mock `LocalFileStore`, `ApiClient`, `ConnectivityService`; test incremental sync, timestamp update, no-overwrite behavior
    - _Requirements: 18.4, 18.7, 18.9_

- [x] 12. Implement `ImageHandler`
  - [x] 12.1 Implement `ImageHandler`
    - Implement `buildImage` (local → remote → placeholder priority) and `preloadBundledImages`
    - Copy bundled image assets via `LocalFileStore.copyAssetIfAbsent` on first launch
    - Resize to correct dimensions: 300×200 for categories/groups, 200×110 for item thumbnails
    - Limit concurrent downloads to 5 using a semaphore; implement lazy loading in lists
    - Use `hdImage_all.ashx` endpoint with `width`/`height` params for remote downloads; save downloaded images to `<documents>/images/<type>/`
    - Wire as a Riverpod `Provider`
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 11.11, 11.12, 16.5_

  - [ ]* 12.2 Write unit tests for `ImageHandler`
    - Test local-first load, placeholder on failure, dimension logic, concurrent download limit
    - _Requirements: 11.3, 11.8, 16.5_

- [x] 13. Checkpoint — Ensure all service tests pass
  - Run `flutter test` for all domain service tests
  - Verify no regressions; ask the user if questions arise before continuing

- [x] 14. Set up `go_router` navigation and app shell
  - [x] 14.1 Define all routes and navigation guards
    - Define routes: `/login`, `/home`, `/category/:id`, `/group/:categoryId/:id`, `/items/:categoryId/:groupId`, `/item-detail/:id`, `/cart`, `/orders`, `/order-detail/:id`, `/search`
    - Implement `redirect` guard: on cold start read `AuthService` state → navigate to `/login` or `/home`
    - On `AuthError` from any service → redirect to `/login`
    - Wire router as a Riverpod `Provider`
    - _Requirements: 1.9, 2.3, 2.4, 13.6_

  - [x] 14.2 Implement app shell with main navigation
    - Create bottom navigation bar or drawer with: Home, Cart (with item-count badge), Orders, Logout
    - Display online/offline status indicator (icon or banner)
    - _Requirements: 2.7, 10.10, 15.1, 19.9_

- [x] 15. Implement Login screen
  - [x] 15.1 Build login screen widget
    - Username and password fields with validation (red border on empty submit)
    - Submit button → call `AuthService.login` → show loading indicator
    - Display exact error messages for wrong password, wrong username, locked account
    - _Requirements: 1.1, 1.4, 1.6, 13.3, 17.2_

  - [ ]* 15.2 Write widget tests for login screen
    - Test field validation (empty fields), error message display, loading state, successful navigation
    - _Requirements: 1.1, 1.4, 13.3_

- [x] 16. Implement Home screen — Category grid
  - [x] 16.1 Build category grid widget
    - Scrollable grid layout; each card shows category name and image with color-coded background using `colorForIndex`
    - Load via `CatalogService.getCategories`; handle loading, error, and empty states
    - Tap → navigate to groups or items for that category
    - Implement pagination for >50 categories
    - _Requirements: 3.2, 3.6, 3.7, 3.8, 3.9, 14.3, 16.4_

  - [ ]* 16.2 Write widget tests for category grid
    - Test color cycling, tap navigation, loading/error states, empty state
    - _Requirements: 3.6, 3.7, 3.8_

- [x] 17. Implement Group and Item list screens
  - [x] 17.1 Build group list screen
    - Scrollable grid; color-coded cards with group name and image (300×200)
    - Load via `CatalogService.getGroups`; tap → navigate to item list for that group
    - _Requirements: 4.1, 4.7, 14.3_

  - [x] 17.2 Build item list screen
    - Scrollable grid; item card shows image (200×110), name, quantity badge (visible when qty > 0)
    - Load via `CatalogService.getItems`; tap → `CartService.addItem` (+1); long-press → navigate to item detail
    - Quantity badge updates immediately after each tap (no navigation required)
    - Show visual feedback animation on add
    - _Requirements: 4.6, 4.7, 6.1, 6.2, 6.3, 6.4, 6.11, 6.12_

  - [ ]* 17.3 Write widget tests for item list
    - Test quantity badge visibility, badge update on tap, visual feedback, long-press navigation
    - _Requirements: 6.3, 6.4, 6.11, 6.12_

- [x] 18. Implement Item Detail screen
  - [x] 18.1 Build item detail screen
    - Display `ItemName`, `ItemCode`, full-resolution image (lazy-loaded), price tiers (FromQty–ToQty, USD + KHR)
    - Show current cart quantity; provide quick-add buttons (+1, +5, +10, +50) — each calls `CartService.addItem` or `CartService.setQuantity`
    - Price tier table updates displayed price as quantity changes
    - _Requirements: 5.1, 5.5, 5.6, 5.7, 5.9, 5.10, 11.8, 12.3_

  - [ ]* 18.2 Write widget tests for item detail screen
    - Test price tier display, quantity update, USD/KHR formatting, cart quantity reflection
    - _Requirements: 5.6, 5.7, 5.10_

- [x] 19. Implement Cart screen
  - [x] 19.1 Build cart screen widget
    - List all cart items: `ItemName`, `ItemCode`, quantity (+/- controls), unit price, line total
    - Remove button per item (deletes item, sets badge to 0); grand total display; checkout button
    - Empty cart state: display "Your cart is empty"
    - _Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6, 19.7, 19.8, 19.10_

  - [ ]* 19.2 Write widget tests for cart screen
    - Test line totals, grand total, remove button, empty state, quantity controls
    - _Requirements: 19.3, 19.7, 19.10_

- [x] 20. Implement Checkout flow
  - [x] 20.1 Build checkout confirmation and submission
    - Validate cart is non-empty before allowing checkout
    - Check connectivity: if offline show "No internet connection. Please try again when online."
    - On submit: call `OrderService.placeOrder`; on success clear cart and navigate to `/orders`; on failure retain cart and show error
    - _Requirements: 7.1, 7.2, 7.4, 7.5, 7.7, 10.8_

  - [ ]* 20.2 Write widget tests for checkout flow
    - Test empty cart block, offline message, success navigation, failure retention
    - _Requirements: 7.1, 7.4, 7.5_

- [x] 21. Implement Order History and Order Detail screens
  - [x] 21.1 Build order history screen
    - Retrieve via `OrderService.getOrderHistory`; display `OrderID`, `InvoiceNo`, `CreatedDate`, `Total`; sorted descending by date
    - Support pagination/infinite scroll for large lists
    - Tap → navigate to order detail
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.6_

  - [x] 21.2 Build order detail screen
    - Display full item breakdown: `ItemName`, `ItemCode`, quantity, unit price, line total
    - _Requirements: 8.4_

  - [ ]* 21.3 Write widget tests for order history and detail
    - Test sort order, pagination, delete filter, detail expansion
    - _Requirements: 8.2, 8.5_

- [x] 22. Implement Search screen
  - [x] 22.1 Build search screen widget
    - Search input accessible from home screen; enforce 2-char minimum before querying
    - Display results in same grid format as category browsing; show "No items found" on empty results
    - Clear → return to category view
    - Tap on result → navigate to item detail or trigger add-to-cart
    - _Requirements: 9.1, 9.2, 9.4, 9.5, 9.6, 9.7_

  - [ ]* 22.2 Write widget tests for search screen
    - Test 2-char minimum enforcement, no-results message, clear behavior
    - _Requirements: 9.5, 9.6, 9.7_

- [x] 23. Implement Logout flow
  - [x] 23.1 Wire logout to `AuthService` and navigation
    - Logout button in main navigation calls `AuthService.logout`
    - Expire token on backend, remove from secure storage, clear cart, navigate to `/login`
    - Do not retain customer-specific data after logout (cached public product data may remain)
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5, 15.6_

  - [ ]* 23.2 Write unit/widget tests for logout
    - Test token expiry call, secure storage clear, cart clear, navigation to login
    - _Requirements: 15.2, 15.3, 15.4, 15.5_

- [x] 24. Implement multi-language and Khmer font support
  - [x] 24.1 Configure Khmer font and locale support
    - Add a Khmer-supporting font (e.g., Battambang or Noto Sans Khmer) to `pubspec.yaml` assets
    - Apply font to `ThemeData` so all text widgets inherit it
    - Display both USD (`$`) and KHR (`៛`) currency symbols wherever prices appear
    - Where both English and Khmer translations exist, display both or offer selector
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5_

- [x] 25. Implement connectivity monitoring and background sync wiring
  - [x] 25.1 Wire `ConnectivityService` and background sync triggers
    - Wrap `connectivity_plus` in a `ConnectivityService` Riverpod provider that broadcasts current state
    - Display online/offline banner/icon in app shell
    - On reconnect: call `OrderService.flushOfflineQueue`, then `SyncService.synchronize`
    - Schedule periodic background sync (app foreground) via `SyncService.synchronize`
    - Sync runs in background without blocking UI
    - _Requirements: 10.9, 10.10, 16.8, 18.10_

- [x] 26. Apply responsive layout and performance optimizations
  - [x] 26.1 Implement responsive grid layouts and touch targets
    - Adjust grid column count based on screen width (phone portrait/landscape, tablet)
    - Ensure all interactive elements have ≥48×48 logical pixel touch targets
    - Use `MediaQuery` / `LayoutBuilder` for adaptive layouts
    - Scale text appropriately for different screen densities
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_

  - [x] 26.2 Implement splash screen and startup performance
    - Configure native splash screen showing for ≤2 seconds
    - On app init: run `AuthService.validateStoredToken` and `CatalogService` asset copy in parallel
    - Target cold-start < 3 seconds; local catalog load < 500 ms
    - _Requirements: 16.1, 16.2, 16.6, 16.7_

- [x] 27. Final checkpoint — Ensure all tests pass and wiring is complete
  - Run `flutter test` (all unit, widget, and property tests)
  - Run `flutter analyze` to confirm no static analysis issues
  - Verify all screens navigate correctly and no orphaned code remains
  - Ask the user if questions arise before wrapping up

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP delivery
- Each task references specific requirement clauses for full traceability
- Property tests use `rapid_check` with a minimum of 100 iterations each; they test pure functions and do not hit network or file system
- Checkpoints (tasks 7, 13, 27) validate incremental progress before moving to the next phase
- The `appendItems` method in `LocalFileStore` is the core invariant of the offline-first sync strategy — Property 4 must pass before any sync-dependent tasks proceed
- Khmer font assets must be added before widget tests for text rendering are meaningful
- Image loading uses a local-first cache; `cached_network_image` handles remote fallback for convenience but the local file path takes priority

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["2.1", "2.2", "2.3"] },
    { "id": 1, "tasks": ["3.1"] },
    { "id": 2, "tasks": ["3.2", "4.1"] },
    { "id": 3, "tasks": ["4.2", "5.1", "6.1"] },
    { "id": 4, "tasks": ["5.2", "6.2", "6.3", "6.4", "6.5", "6.6"] },
    { "id": 5, "tasks": ["8.1", "9.1", "10.1", "11.1", "12.1"] },
    { "id": 6, "tasks": ["8.2", "9.2", "10.2", "11.2", "12.2"] },
    { "id": 7, "tasks": ["14.1", "14.2"] },
    { "id": 8, "tasks": ["15.1", "16.1", "17.1", "17.2", "21.1", "21.2", "22.1", "24.1", "25.1", "26.2"] },
    { "id": 9, "tasks": ["15.2", "16.2", "17.3", "18.1", "19.1", "21.3", "22.2", "26.1"] },
    { "id": 10, "tasks": ["18.2", "19.2", "20.1", "23.1"] },
    { "id": 11, "tasks": ["20.2", "23.2"] }
  ]
}
```
