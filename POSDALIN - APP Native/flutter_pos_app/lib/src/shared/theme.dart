import 'package:flutter/material.dart';

/// Application-wide theme configuration with Khmer font support.
///
/// This theme ensures all text widgets inherit the Noto Sans Khmer font family,
/// which supports both Latin and Khmer Unicode characters (កម្ពុជា).
class AppTheme {
  AppTheme._();

  /// Primary theme data with Khmer font configuration.
  ///
  /// Applies NotoSansKhmer as the default font family for all text styles,
  /// ensuring proper rendering of Khmer script and currency symbols (៛).
  static ThemeData get lightTheme {
    return ThemeData(
      colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
      useMaterial3: true,
      fontFamily: 'NotoSansKhmer',
      
      // Explicit text theme configuration with Khmer font
      textTheme: const TextTheme(
        displayLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        displayMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        displaySmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodyLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodyMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodySmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
      ),
      
      // Primary text theme for dark-on-light text
      primaryTextTheme: const TextTheme(
        displayLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        displayMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        displaySmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        headlineSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        titleSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodyLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodyMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        bodySmall: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelLarge: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelMedium: TextStyle(fontFamily: 'NotoSansKhmer'),
        labelSmall: TextStyle(fontFamily: 'NotoSansKhmer'),
      ),
    );
  }
}
