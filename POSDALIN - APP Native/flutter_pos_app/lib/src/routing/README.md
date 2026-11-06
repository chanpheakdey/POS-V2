# Routing Implementation

## Overview

The routing system uses **go_router** for declarative navigation with authentication guards integrated via **Riverpod**.

## Key Features

### 1. Route Definitions

All routes are defined in `routes.dart`:

- `/login` - Login screen
- `/home` - Home screen (authenticated)
- `/category/:id` - Category screen with groups or items
- `/group/:categoryId/:id` - Group screen with items
- `/items/:categoryId/:groupId` - Items listing screen
- `/item-detail/:id` - Item detail screen
- `/cart` - Shopping cart screen
- `/orders` - Order history screen
- `/order-detail/:id` - Order detail screen
- `/search` - Search screen

### 2. Authentication Guards

The router implements redirect logic based on `AuthService` state:

#### On Cold Start
- Reads `AuthService` state from persistent storage
- If valid token exists → navigates to `/home`
- If no valid token → navigates to `/login`

#### During Runtime
- When `AuthError` occurs → redirects to `/login`
- When authenticated user tries to access `/login` → redirects to `/home`
- When unauthenticated user tries to access protected routes → redirects to `/login`

#### Implementation Details

The router uses `refreshListenable` with a custom `_GoRouterRefreshStream` to react to auth state changes:

```dart
refreshListenable: _GoRouterRefreshStream(
  ref.read(authServiceProvider.notifier).stream,
),
```

This ensures that whenever the authentication state changes (login, logout, token expiry), the router's redirect guards are re-evaluated.

### 3. Redirect Logic

```dart
redirect: (BuildContext context, GoRouterState state) {
  return authServiceAsync.when(
    data: (authState) {
      final isAuthenticated = authState is Authenticated;
      final isGoingToLogin = state.matchedLocation == Routes.login;
      
      // Redirect to login if not authenticated
      if (!isAuthenticated && !isGoingToLogin) {
        return Routes.login;
      }
      
      // Redirect to home if authenticated and trying to access login
      if (isAuthenticated && isGoingToLogin) {
        return Routes.home;
      }
      
      return null; // No redirect needed
    },
    loading: () => null, // Don't redirect while loading
    error: (error, stack) {
      // On auth error, redirect to login
      final isGoingToLogin = state.matchedLocation == Routes.login;
      return isGoingToLogin ? null : Routes.login;
    },
  );
}
```

### 4. AsyncValue Handling

The router properly handles all `AsyncValue` states from `AuthService`:

- **loading**: No redirect while authentication is being validated (prevents flash to login screen)
- **data**: Execute normal redirect logic based on auth state
- **error**: Redirect to login on any authentication error

### 5. Riverpod Integration

The router is provided as a Riverpod provider:

```dart
@riverpod
GoRouter router(RouterRef ref) {
  // Implementation
}
```

This allows the router to:
- Watch auth service state reactively
- Access the router via `ref.watch(routerProvider)` in widgets
- Automatically dispose resources when the provider is disposed

## Requirements Validated

- **Requirement 1.9**: Navigate to home screen after successful authentication
- **Requirement 2.3**: Navigate directly to home if valid token exists on cold start
- **Requirement 2.4**: Display login screen if token is invalid or missing
- **Requirement 13.6**: Redirect to login screen when token validation fails

## Usage in App

To use the router in your app:

```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/routing/router.dart';

class MyApp extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    
    return MaterialApp.router(
      routerConfig: router,
      title: 'POS App',
    );
  }
}
```

## Navigation Examples

### Using Named Routes

```dart
// Navigate to a category
context.go(Routes.category(categoryId));

// Navigate to a group
context.go(Routes.group(categoryId, groupId));

// Navigate to items
context.go(Routes.items(categoryId, groupId));

// Navigate to item detail
context.go(Routes.itemDetail(itemId));

// Navigate to cart
context.go(Routes.cart);
```

### Using go_router Methods

```dart
// Push a new route
context.push(Routes.search);

// Replace current route
context.replace(Routes.home);

// Pop back
context.pop();
```

## Testing

Router tests are located in `test/src/routing/router_test.dart` and verify:

1. Route path constants are correctly defined
2. Helper methods build correct parameterized paths
3. Authentication guard logic works correctly for all state combinations
4. Route path parsing extracts parameters correctly

## Future Enhancements

Potential improvements for future tasks:

1. **Deep linking**: Support for opening the app from URLs
2. **Route transitions**: Custom animations between screens
3. **Route guards**: Additional guards for specific features (e.g., admin routes)
4. **Analytics**: Track navigation events for user behavior analysis
5. **Error boundaries**: Better error handling for route-level errors
