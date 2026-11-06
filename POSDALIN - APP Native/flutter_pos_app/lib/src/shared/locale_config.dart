import 'package:flutter/material.dart';

/// Locale configuration for multi-language support (English and Khmer).
///
/// The app supports both English (en) and Khmer (km) languages as specified
/// in Requirements 12.1, 12.2, 12.5.
class LocaleConfig {
  LocaleConfig._();

  /// English locale (default)
  static const Locale english = Locale('en', 'US');

  /// Khmer locale (កម្ពុជា)
  static const Locale khmer = Locale('km', 'KH');

  /// List of supported locales
  static const List<Locale> supportedLocales = [
    english,
    khmer,
  ];

  /// Default locale for the application
  static const Locale defaultLocale = english;
}

/// Language display utilities
class LanguageUtils {
  LanguageUtils._();

  /// Returns the display name for a locale
  static String getDisplayName(Locale locale) {
    switch (locale.languageCode) {
      case 'en':
        return 'English';
      case 'km':
        return 'ភាសាខ្មែរ'; // "Khmer" in Khmer script
      default:
        return locale.languageCode;
    }
  }

  /// Checks if Khmer content should be displayed for the given locale
  static bool isKhmer(Locale locale) {
    return locale.languageCode == 'km';
  }

  /// Checks if English content should be displayed for the given locale
  static bool isEnglish(Locale locale) {
    return locale.languageCode == 'en';
  }
}

/// Extension on String to provide bilingual text support
extension BilingualText on String {
  /// Combines English and Khmer text for display when both are available.
  ///
  /// Example: `"Product".withKhmer("ផលិតផល")` returns `"Product (ផលិតផល)"`
  ///
  /// This satisfies Requirement 12.5: "WHERE English and Khmer translations
  /// both exist, THE POS_App SHALL display both or allow customer selection"
  String withKhmer(String khmerText) {
    if (khmerText.isEmpty) return this;
    return '$this ($khmerText)';
  }

  /// Returns either this string or the Khmer alternative based on current locale
  String orKhmer(String khmerText, Locale locale) {
    return LanguageUtils.isKhmer(locale) && khmerText.isNotEmpty
        ? khmerText
        : this;
  }
}
