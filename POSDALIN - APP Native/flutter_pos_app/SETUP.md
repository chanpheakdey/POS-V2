# Flutter POS App - Setup Guide

## Project Structure

This Flutter mobile POS application follows a layered architecture with the following structure:

```
flutter_pos_app/
├── lib/
│   ├── main.dart                 # App entry point
│   └── src/
│       ├── api/                  # HTTP client and ASMX service wrappers
│       ├── auth/                 # Authentication service and state
│       ├── catalog/              # Product catalog service
│       ├── cart/                 # Shopping cart service and state
│       ├── order/                # Order placement and history service
│       ├── search/               # Search service for local catalog
│       ├── sync/                 # Background data synchronization
│       └── shared/               # Shared utilities, models, and widgets
│
├── assets/
│   ├── catalog/                  # Pre-bundled JSON catalog data
│   │   ├── categories.json
│   │   ├── groups/
│   │   ├── items/
│   │   ├── item_details/
│   │   └── price_tiers/
│   ├── images/                   # Pre-bundled product images
│   │   ├── categories/           # 300x200 category images
│   │   ├── groups/               # 300x200 group images
│   │   ├── items/                # 200x110 item thumbnail images
│   │   └── placeholder.png       # Fallback placeholder image
│   └── fonts/                    # Khmer font files
│       ├── NotoSansKhmer-Regular.ttf
│       └── NotoSansKhmer-Bold.ttf
│
└── test/                         # Unit, widget, and property-based tests
```

## Dependencies

### Production Dependencies

- **flutter_riverpod** (^2.4.0): State management
- **riverpod_annotation** (^2.3.0): Code generation for Riverpod providers
- **go_router** (^13.0.0): Declarative navigation with route guards
- **flutter_secure_storage** (^9.0.0): Secure token storage
- **connectivity_plus** (^5.0.0): Network connectivity monitoring
- **cached_network_image** (^3.3.0): Image loading and caching
- **path_provider** (^2.1.0): File system access
- **http** (^1.1.0): HTTP client for ASMX web services
- **html** (^0.15.4): HTML parsing for ASMX responses

### Development Dependencies

- **rapid_check** (^0.5.0): Property-based testing framework
- **mocktail** (^1.0.0): Mocking library for unit tests
- **build_runner** (^2.4.6): Code generation runner
- **riverpod_generator** (^2.3.0): Riverpod code generation
- **flutter_lints** (^3.0.0): Recommended lint rules

## Setup Instructions

### 1. Install Flutter

Ensure you have Flutter SDK installed. This project requires:
- Flutter SDK: >=3.0.0 <4.0.0
- Dart SDK: >=3.0.0 <4.0.0

Check your Flutter installation:
```bash
flutter --version
flutter doctor
```

### 2. Get Dependencies

From the `flutter_pos_app` directory, run:
```bash
flutter pub get
```

### 3. Generate Code

Generate Riverpod providers and other code:
```bash
flutter pub run build_runner build --delete-conflicting-outputs
```

### 4. Add Pre-bundled Assets

#### Catalog Data
Place pre-bundled JSON files in `assets/catalog/`:
- `categories.json` - Array of category objects
- `groups/<category_id>.json` - Groups for each category
- `items/<category_id>_<group_id>.json` - Items for each group
- `item_details/<item_id>.json` - Detailed item information
- `price_tiers/<item_id>.json` - Price tiers for each item

#### Images
Place pre-optimized images in `assets/images/`:
- `categories/` - 300x200px category images
- `groups/` - 300x200px group images
- `items/` - 200x110px item thumbnail images
- `placeholder.png` - Fallback image for failed loads

#### Fonts
Download and add Khmer fonts to `assets/fonts/`:
- NotoSansKhmer-Regular.ttf
- NotoSansKhmer-Bold.ttf

You can download Noto Sans Khmer from [Google Fonts](https://fonts.google.com/noto/specimen/Noto+Sans+Khmer).

### 5. Configure Backend Endpoint

Update the ASMX service endpoint in `lib/src/api/api_client.dart` to point to your backend:
```dart
static const String baseUrl = 'https://your-backend-url.com';
```

### 6. Run the App

For development:
```bash
flutter run
```

For specific platforms:
```bash
flutter run -d android
flutter run -d ios
```

## Testing

### Run All Tests
```bash
flutter test
```

### Run Property-Based Tests Only
```bash
flutter test --tags property
```

### Run Specific Test File
```bash
flutter test test/src/cart/cart_service_test.dart
```

### Run with Coverage
```bash
flutter test --coverage
```

## Development Guidelines

### Lint Rules
This project uses strict lint rules defined in `analysis_options.yaml`:
- Implicit casts and dynamic types are disabled
- Final locals and fields are preferred
- Const constructors are enforced where possible
- Single quotes for strings
- Trailing commas required for better formatting

Run static analysis:
```bash
flutter analyze
```

### Code Generation
When creating new Riverpod providers with `@riverpod` annotation, regenerate code:
```bash
flutter pub run build_runner watch
```

### Architecture Guidelines

1. **State Management**: All state is managed through Riverpod providers
2. **Navigation**: Use `go_router` for all navigation (no Navigator.push)
3. **Offline-First**: Always check local storage before making network calls
4. **Append-Only Sync**: Never overwrite existing catalog data, only append
5. **Error Handling**: Use `ServiceResult<T>` sealed class for typed errors
6. **Testing**: Write both example-based and property-based tests

## Troubleshooting

### Dependencies Won't Resolve
```bash
flutter clean
flutter pub get
```

### Code Generation Issues
```bash
flutter pub run build_runner clean
flutter pub run build_runner build --delete-conflicting-outputs
```

### Platform-Specific Issues

**Android**: Ensure `minSdkVersion` is at least 21 in `android/app/build.gradle`

**iOS**: Run `pod install` in the `ios/` directory:
```bash
cd ios && pod install && cd ..
```

## Additional Resources

- [Flutter Documentation](https://docs.flutter.dev/)
- [Riverpod Documentation](https://riverpod.dev/)
- [go_router Documentation](https://pub.dev/packages/go_router)
- [rapid_check Documentation](https://pub.dev/packages/rapid_check)

## Requirements Validated

This setup satisfies:
- **Requirement 10.1**: Pre-bundled catalog data included in assets
- **Requirement 10.2**: Pre-bundled images included in assets
- **Requirement 18.1**: Offline-first architecture with local file storage
- **Requirement 18.2**: Append-only synchronization strategy
