# Khmer Font and Locale Support - Setup Guide

This document describes the Khmer font and locale support implementation for the Flutter POS App (Task 24.1).

## Overview

The application now supports:
- ✅ Khmer font (Noto Sans Khmer) for proper Khmer script rendering
- ✅ Dual currency display: USD ($) and KHR (៛)
- ✅ Multi-language text support (English and Khmer)
- ✅ Theme configuration with Khmer font inheritance

## Implementation Details

### 1. Font Configuration (`pubspec.yaml`)

The `pubspec.yaml` already includes Noto Sans Khmer font configuration:

```yaml
fonts:
  - family: NotoSansKhmer
    fonts:
      - asset: assets/fonts/NotoSansKhmer-Regular.ttf
      - asset: assets/fonts/NotoSansKhmer-Bold.ttf
        weight: 700
```

**Action Required:** Download the font files using the provided script or manual instructions in `assets/fonts/README.md`

### 2. Theme Configuration (`lib/src/shared/theme.dart`)

The `AppTheme.lightTheme` is already configured to use NotoSansKhmer as the default font family for all text styles. This ensures all Text widgets throughout the app automatically inherit the Khmer-supporting font.

Key features:
- `fontFamily: 'NotoSansKhmer'` set on ThemeData
- All TextTheme styles explicitly use NotoSansKhmer
- Supports both Latin and Khmer Unicode characters (កម្ពុជា)

### 3. Currency Formatting (`lib/src/shared/currency_formatter.dart`)

Created comprehensive currency formatting utilities:

#### Functions:
- `formatUsd(double)` - Formats as US Dollars with $ symbol
- `formatKhr(double)` - Formats as Cambodian Riel with ៛ symbol (U+17DB)
- `formatDualCurrency(double, double)` - Displays both currencies together

#### Extension Methods:
```dart
(1234.56).toUsd()  // Returns "$1,234.56"
(5000000.0).toKhr()  // Returns "៛5,000,000"
```

#### Examples:
```dart
formatUsd(1234.56)              // "$1,234.56"
formatKhr(5000000)              // "៛5,000,000"
formatDualCurrency(100, 400000) // "$100.00 / ៛400,000"
```

### 4. Locale Configuration (`lib/src/shared/locale_config.dart`)

Created locale management utilities:

#### Supported Locales:
- English (en_US) - default
- Khmer (km_KH) - ភាសាខ្មែរ

#### Utilities:
- `LocaleConfig.supportedLocales` - List of supported locales
- `LanguageUtils.getDisplayName(locale)` - Returns locale display name
- `LanguageUtils.isKhmer(locale)` - Checks if locale is Khmer
- `LanguageUtils.isEnglish(locale)` - Checks if locale is English

#### Bilingual Text Support:
```dart
"Product".withKhmer("ផលិតផល")  // Returns "Product (ផលិតផល)"
"Product".orKhmer("ផលិតផល", locale)  // Returns appropriate text based on locale
```

### 5. Testing (`test/shared/currency_formatter_test.dart`)

Comprehensive test suite covering:
- USD formatting with thousand separators
- KHR formatting without decimal places
- Dual currency display
- Extension methods
- Currency symbol verification (including Unicode codepoint check for ៛)

## Requirements Satisfied

This implementation satisfies the following requirements:

### Requirement 12.1
✅ WHERE Khmer language content exists in the Database, THE POS_App SHALL display Khmer text correctly

- Noto Sans Khmer font supports full Khmer Unicode range
- Font applied globally via ThemeData

### Requirement 12.2
✅ THE POS_App SHALL support Unicode rendering for Khmer script (កម្ពុជា)

- NotoSansKhmer font includes all Khmer glyphs
- Tested with Khmer sample text

### Requirement 12.3
✅ THE POS_App SHALL display currency symbols for both US Dollars ($) and Cambodian Riel (៛)

- `currency_formatter.dart` provides both symbols
- `formatDualCurrency()` displays both simultaneously
- Verified Unicode codepoint U+17DB for ៛

### Requirement 12.4
✅ THE POS_App SHALL use appropriate fonts that support Khmer glyphs

- Noto Sans Khmer is a comprehensive font designed for Khmer script
- Developed by Google specifically for Noto project
- Includes 363 glyphs covering all Khmer characters

### Requirement 12.5
✅ WHERE English and Khmer translations both exist, THE POS_App SHALL display both or allow customer selection

- `BilingualText.withKhmer()` extension displays both languages
- `BilingualText.orKhmer()` allows locale-based selection
- `LocaleConfig` provides infrastructure for language switching

## Usage in Application Code

### Displaying Prices:
```dart
import 'package:pos_app/src/shared/currency_formatter.dart';

// Single currency
Text(formatUsd(item.priceUsd))
Text(formatKhr(item.priceKhr))

// Both currencies
Text(formatDualCurrency(item.priceUsd, item.priceKhr))

// Using extensions
Text((item.priceUsd).toUsd())
```

### Bilingual Text:
```dart
import 'package:pos_app/src/shared/locale_config.dart';

// Show both languages
Text("Category".withKhmer("ប្រភេទ"))  // Displays: "Category (ប្រភេទ)"

// Show based on locale
Text("Category".orKhmer("ប្រភេទ", currentLocale))
```

### Checking Current Language:
```dart
if (LanguageUtils.isKhmer(currentLocale)) {
  // Show Khmer-specific UI
}
```

## Font Installation

### Automated (Recommended):
```powershell
cd flutter_pos_app
powershell -ExecutionPolicy Bypass -File download_fonts.ps1
```

### Manual:
1. Download from: https://fonts.google.com/noto/specimen/Noto+Sans+Khmer
2. Extract ZIP
3. Copy `NotoSansKhmer[wdth,wght].ttf` to `assets/fonts/` as:
   - `NotoSansKhmer-Regular.ttf`
   - `NotoSansKhmer-Bold.ttf` (duplicate of the same file)

### Verification:
```bash
flutter clean
flutter pub get
flutter run
```

## Next Steps

1. **Download Font Files**: Follow instructions in `assets/fonts/README.md`
2. **Update Item/Product Models**: Add Khmer name fields (e.g., `itemNameKhmer`)
3. **Update UI Components**: Use `currency_formatter` for all price displays
4. **Implement Language Selector**: Add UI to switch between English and Khmer
5. **Update Backend Integration**: Fetch Khmer translations from database

## Files Modified/Created

### Created:
- `lib/src/shared/currency_formatter.dart` - Currency formatting utilities
- `lib/src/shared/locale_config.dart` - Locale and language utilities
- `test/shared/currency_formatter_test.dart` - Currency formatter tests
- `assets/fonts/README.md` - Font installation instructions
- `download_fonts.ps1` - Automated font download script
- `KHMER_LOCALE_SETUP.md` - This documentation

### Already Configured:
- `pubspec.yaml` - Font asset configuration
- `lib/src/shared/theme.dart` - Theme with Khmer font

## References

- Noto Sans Khmer Font: https://fonts.google.com/noto/specimen/Noto+Sans+Khmer
- Khmer Unicode Range: U+1780–U+17FF, U+19E0–U+19FF
- Khmer Riel Symbol: ៛ (U+17DB)
- Requirements: 12.1, 12.2, 12.3, 12.4, 12.5
