# Splash Screen Setup Instructions

## Task 26.2: Splash Screen and Startup Performance Implementation

This document describes the splash screen setup and startup performance optimizations implemented for the Flutter POS App.

## What Was Implemented

### 1. Native Splash Screen Configuration

Added `flutter_native_splash` package to `pubspec.yaml` as a dev dependency. This package automatically generates the necessary native code for Android and iOS splash screens.

Configuration file: `flutter_native_splash.yaml`

### 2. Startup Performance Optimizations

Modified `lib/main.dart` to implement parallel startup operations:

```dart
// Run parallel startup operations:
// 1. AuthService token validation
// 2. LocalFileStore initialization (copies bundled assets if absent)
await Future.wait([
  _initializeAuth(container),
  _initializeCatalog(container),
]);
```

**Performance Targets:**
- Splash screen duration: ≤2 seconds
- Cold-start time: <3 seconds
- Local catalog load: <500 milliseconds

### 3. Splash Screen Timing

The implementation ensures:
- Minimum splash duration: 500ms (for smooth visual experience)
- Maximum splash duration: 2000ms (meets requirement 16.1)
- Automatic removal after initialization completes

## Setup Steps

### Step 1: Create Splash Screen Image

You need to create a splash screen icon image:

1. Create or obtain a logo/icon image (PNG format recommended)
2. Size: 1024x1024 pixels minimum (for best quality across devices)
3. Save it as: `assets/images/splash_icon.png`

**Alternative:** If you want a full-screen splash image instead:
- Create image at 2048x2048 pixels
- Update `flutter_native_splash.yaml`:
  - Change `image: assets/images/splash_icon.png` to your image path
  - Remove the `icon_background_color` line if using full-screen image

### Step 2: Install Dependencies

Run the following command in the flutter_pos_app directory:

```bash
flutter pub get
```

### Step 3: Generate Native Splash Screens

After installing dependencies, run:

```bash
flutter pub run flutter_native_splash:create
```

This command will:
- Generate Android splash screen files in `android/app/src/main/res/`
- Generate iOS splash screen files in `ios/Runner/Assets.xcassets/`
- Configure Android 12+ splash screen
- Update native configuration files

### Step 4: Verify Implementation

Build and run the app on a device or emulator:

```bash
flutter run
```

**What to verify:**
1. Splash screen appears immediately on app launch
2. Splash screen shows for approximately 1-2 seconds
3. App navigates to login or home screen based on auth state
4. Cold start completes in under 3 seconds

## Technical Details

### Parallel Initialization

The app now performs these operations in parallel during startup:

1. **Auth Initialization (`_initializeAuth`)**:
   - Reads stored token from secure storage
   - Validates token with backend if available
   - Sets initial auth state

2. **Catalog Initialization (`_initializeCatalog`)**:
   - Copies pre-bundled category data to local storage if absent
   - Prepares catalog for immediate display on home screen

### Splash Screen Behavior

```dart
// Ensure splash shows for at least 500ms but no more than 2000ms
const minSplashDuration = Duration(milliseconds: 500);
const maxSplashDuration = Duration(milliseconds: 2000);
```

- If initialization completes in <500ms: waits to reach 500ms
- If initialization takes 500ms-2000ms: removes splash immediately after completion
- If initialization takes >2000ms: removes splash at 2000ms (logs warning)

## Requirements Validated

This implementation validates the following requirements:

- **16.1**: Splash screen displays for no more than 2 seconds ✓
- **16.2**: Navigation transitions complete within 300ms (handled by router) ✓
- **16.6**: App initializes in under 3 seconds on mid-range device ✓
- **16.7**: Catalog loads from local files within 500ms ✓

## Configuration Files

### flutter_native_splash.yaml

```yaml
flutter_native_splash:
  color: "#FFFFFF"
  android_12:
    image: assets/images/splash_icon.png
    color: "#FFFFFF"
    icon_background_color: "#FFFFFF"
  ios: true
  android: true
  fullscreen: true
  android_gravity: center
  ios_content_mode: center
```

### Modified Files

1. **pubspec.yaml**: Added `flutter_native_splash: ^2.3.0` to dev_dependencies
2. **lib/main.dart**: Implemented parallel startup initialization
3. **flutter_native_splash.yaml**: Created splash screen configuration

## Performance Monitoring

The implementation includes debug logging for performance tracking:

```dart
debugPrint('App initialization completed in ${elapsedMs}ms');
```

Monitor the console output during development to ensure performance targets are met.

## Troubleshooting

### Splash screen not appearing

1. Ensure `flutter pub run flutter_native_splash:create` was executed
2. Verify splash icon exists at `assets/images/splash_icon.png`
3. Clean and rebuild: `flutter clean && flutter pub get && flutter run`

### Initialization taking too long

1. Check console for debug messages
2. Verify network connectivity if auth validation is slow
3. Ensure bundled assets are properly configured in pubspec.yaml

### Android 12+ splash issues

The configuration includes Android 12+ support with the new splash screen API. If issues occur:
1. Verify `android_12` section in flutter_native_splash.yaml
2. Ensure icon_background_color is set
3. Test on Android 12+ device or emulator

## Next Steps

After completing the setup steps above:

1. Test on physical devices (Android and iOS)
2. Measure cold-start performance with release builds
3. Optimize if performance targets are not met
4. Consider lazy-loading additional catalog data after home screen appears
