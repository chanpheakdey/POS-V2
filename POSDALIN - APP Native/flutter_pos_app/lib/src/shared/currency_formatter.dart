/// Currency formatting utilities for USD and KHR (Khmer Riel).
///
/// Provides consistent formatting for displaying prices in both
/// US Dollars ($) and Cambodian Riel (៛) throughout the application.
library;

/// Formats a price value as US Dollars with $ symbol.
///
/// Example: `formatUsd(1234.56)` returns `"$1,234.56"`
String formatUsd(double amount) {
  return '\$${_formatNumber(amount, decimalPlaces: 2)}';
}

/// Formats a price value as Cambodian Riel with ៛ symbol.
///
/// Example: `formatKhr(5000000)` returns `"៛5,000,000"`
/// 
/// Note: Riel values are typically whole numbers (no decimal places).
String formatKhr(double amount) {
  return '៛${_formatNumber(amount, decimalPlaces: 0)}';
}

/// Formats a price displaying both USD and KHR currencies.
///
/// Example: `formatDualCurrency(100, 400000)` returns `"$100.00 / ៛400,000"`
///
/// This format is used throughout the app to show prices in both currencies
/// simultaneously, meeting the multi-currency requirement (Req 12.3).
String formatDualCurrency(double usd, double khr) {
  return '${formatUsd(usd)} / ${formatKhr(khr)}';
}

/// Internal helper to format numbers with thousand separators and decimal places.
String _formatNumber(double amount, {required int decimalPlaces}) {
  // Format with specified decimal places
  final String formatted = amount.toStringAsFixed(decimalPlaces);
  final parts = formatted.split('.');
  final integerPart = parts[0];
  final decimalPart = parts.length > 1 ? parts[1] : '';
  
  // Add thousand separators
  final buffer = StringBuffer();
  var count = 0;
  for (var i = integerPart.length - 1; i >= 0; i--) {
    if (count > 0 && count % 3 == 0) {
      buffer.write(',');
    }
    buffer.write(integerPart[i]);
    count++;
  }
  
  // Reverse the string (it was built backwards)
  final integerWithSeparators = buffer.toString().split('').reversed.join();
  
  // Combine integer and decimal parts
  if (decimalPlaces > 0 && decimalPart.isNotEmpty) {
    return '$integerWithSeparators.$decimalPart';
  } else {
    return integerWithSeparators;
  }
}

/// Extension on double to provide convenient currency formatting methods.
extension CurrencyFormatting on double {
  /// Formats this value as USD currency.
  /// 
  /// Example: `(1234.56).toUsd()` returns `"$1,234.56"`
  String toUsd() => formatUsd(this);
  
  /// Formats this value as KHR currency.
  /// 
  /// Example: `(5000000.0).toKhr()` returns `"៛5,000,000"`
  String toKhr() => formatKhr(this);
}
