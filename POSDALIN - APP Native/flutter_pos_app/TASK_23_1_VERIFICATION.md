# Task 23.1 Verification: Wire Logout to AuthService and Navigation

## Implementation Summary

This task has been successfully completed. The logout button in the main navigation is now properly wired to call `AuthService.logout()` and handle all required cleanup.

## Changes Made

### 1. Updated `app_shell.dart`
- **File**: `lib/src/screens/app_shell.dart`
- **Changes**:
  - Added `connectivity_plus` import to support ConnectivityResult enum
  - Logout button already exists in bottom navigation bar (4th item, index 3)
  - `_handleLogout()` method already properly implemented with:
    - Confirmation dialog
    - Calls to `AuthService.logout()`
    - Navigation to login screen

### 2. AuthService Implementation (Already Complete)
- **File**: `lib/src/auth/auth_service.dart`
- **Logout Flow**:
  1. Calls `ApiClient.expireToken(token)` to expire token on backend
  2. Calls `_clearCart()` to empty cart.json file
  3. Calls `_clearStoredCredentials()` to remove token from secure storage
  4. Calls `_apiClient.setToken(null)` to clear API client
  5. Updates state to `Unauthenticated`

### 3. Router Integration (Already Complete)
- **File**: `lib/src/routing/router.dart`
- Router automatically redirects to `/login` when auth state becomes `Unauthenticated`
- AppShell also explicitly calls `context.go(Routes.login)` after logout

## Requirements Validated

✅ **Requirement 15.1**: Logout button accessible from main navigation
- Bottom navigation bar includes logout button with icon and label at index 3

✅ **Requirement 15.2**: Expire token on backend
- `AuthService.logout()` calls `ApiClient.expireToken(token)`
- Backend marks token as expired in tblToken table

✅ **Requirement 15.3**: Remove token from local storage
- `AuthService.logout()` calls `_clearStoredCredentials()`
- Deletes auth_token, user_id, and username from secure storage

✅ **Requirement 15.4**: Clear shopping cart contents
- `AuthService.logout()` calls `_clearCart()`
- Writes empty array to cart.json file

✅ **Requirement 15.5**: Navigate to login screen
- `AppShell._handleLogout()` calls `context.go(Routes.login)`
- Router redirect also handles auto-navigation when auth state changes

✅ **Requirement 15.6**: Do not retain customer-specific data
- All customer data cleared (token, userId, username, cart)
- Public product catalog data remains cached (categories, groups, items)

## Logout Flow Sequence

1. User taps logout button in bottom navigation (index 3)
2. Confirmation dialog appears: "Are you sure you want to logout?"
3. If user confirms:
   a. `AuthService.logout()` is called
   b. Token expired on backend via API call
   c. Cart contents cleared from local file
   d. Token and credentials deleted from secure storage
   e. API client token cleared
   f. Auth state updated to `Unauthenticated`
   g. Navigation to `/login` triggered
4. If user cancels: dialog dismissed, no action taken

## Offline Behavior

The logout implementation handles offline scenarios gracefully:
- If device is offline, `ApiClient.expireToken()` will fail
- The error is caught and logout continues
- Local storage is still cleared (token, cart)
- State transitions to `Unauthenticated`
- Navigation to login still happens
- Token remains active on backend until next access or different device login

## Test Coverage

Comprehensive test suite exists in `test/screens/app_shell_logout_test.dart`:
- Logout button visibility test
- Confirmation dialog display test
- Cancel logout test
- Logout flow requirements documentation
- Navigation integration tests
- Cart badge integration tests
- Online status banner tests

## Confirmation

Task 23.1 is **COMPLETE**. The logout button is properly wired to `AuthService.logout()` with all required cleanup:
- ✅ Token expiration on backend
- ✅ Token removal from secure storage
- ✅ Cart clearing
- ✅ Navigation to login
- ✅ No customer-specific data retained (except public catalog cache)

The implementation matches all requirements (15.1, 15.2, 15.3, 15.4, 15.5, 15.6) and follows the design specifications exactly.
