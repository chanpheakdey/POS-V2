# App Shell Implementation

## Overview

The `AppShell` widget provides persistent navigation and status indicators across all authenticated screens in the Flutter POS app.

## Features

### 1. Bottom Navigation Bar

The app shell includes a bottom navigation bar with four items:

- **Home** (Home icon): Navigate to the main catalog browsing screen
- **Cart** (Shopping cart icon): Navigate to the shopping cart view
  - Displays a red badge with the number of items in the cart
  - Badge is hidden when cart is empty
- **Orders** (Receipt icon): Navigate to order history
- **Logout** (Logout icon): Sign out and return to login screen
  - Shows confirmation dialog before logging out
  - Clears authentication token and cart data

### 2. Online/Offline Status Indicator

A colored banner at the top of the screen indicates connectivity status:

- **Online**: Green banner with WiFi icon and "Online" text
  - Subtle design that doesn't obstruct content
- **Offline**: Red banner with WiFi-off icon and warning message
  - Prominent design to alert users that some features may be unavailable
  - Message: "Offline - Some features may be unavailable"

### 3. Integration with Services

The app shell integrates with key services:

- **CartService**: Watches cart state to display real-time item count badge
- **ConnectivityService**: Monitors network status via `connectivity_plus` package
- **AuthService**: Handles logout functionality and token management

## Architecture

The app shell uses Riverpod for state management and go_router's ShellRoute for persistent UI:

```dart
ShellRoute(
  builder: (context, state, child) => AppShell(child: child),
  routes: [
    // All authenticated routes wrapped by AppShell
    GoRoute(path: '/home', ...),
    GoRoute(path: '/cart', ...),
    // ... other routes
  ],
)
```

## Requirements Validated

- **Requirement 2.7**: Token-based session management with logout functionality
- **Requirement 10.10**: Visual indicator showing online/offline status
- **Requirement 15.1**: Logout button accessible from main navigation
- **Requirement 19.9**: Cart icon with total number of items in main navigation

## Usage

The app shell automatically wraps all authenticated routes. Individual screens don't need to implement navigation or status indicators:

```dart
// Example authenticated screen
class HomeScreen extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Home')),
      body: Center(child: Text('Catalog content here')),
    );
    // AppShell provides navigation bar and status automatically
  }
}
```

## Testing

Widget tests verify:
- Bottom navigation bar displays 4 items
- Cart badge shows correct item count
- Badge is hidden when cart is empty
- Online status banner displays when connected
- Offline status banner displays when disconnected
- Navigation between screens works correctly
- Logout confirmation dialog appears

See `test/src/screens/app_shell_test.dart` for complete test suite.

## Design Notes

### Cart Badge
- Uses Flutter's built-in `Badge` widget for consistency
- Red background with white text for visibility
- Updates automatically when cart changes via Riverpod state

### Connectivity Banner
- Minimal height when online to preserve screen space
- More prominent when offline to ensure users are aware
- Uses platform connectivity detection via `connectivity_plus`

### Navigation Behavior
- Uses `context.go()` for navigation (replaces current route)
- Current route is highlighted in navigation bar
- Routes defined in `Routes` class for type safety

### Logout Flow
1. User taps Logout in navigation bar
2. Confirmation dialog appears
3. If confirmed:
   - AuthService expires token on backend
   - Token removed from secure storage
   - Cart cleared from local storage
   - Navigate to login screen
