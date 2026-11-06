import 'package:flutter_test/flutter_test.dart';
import 'package:pos_app/src/shared/currency_formatter.dart';

void main() {
  group('Currency Formatter', () {
    group('formatUsd', () {
      test('formats simple dollar amount', () {
        expect(formatUsd(100.00), equals('\$100.00'));
      });

      test('formats with thousand separators', () {
        expect(formatUsd(1234.56), equals('\$1,234.56'));
      });

      test('formats large amounts with multiple separators', () {
        expect(formatUsd(1234567.89), equals('\$1,234,567.89'));
      });

      test('formats zero', () {
        expect(formatUsd(0.00), equals('\$0.00'));
      });

      test('formats fractional cents correctly', () {
        expect(formatUsd(99.99), equals('\$99.99'));
      });
    });

    group('formatKhr', () {
      test('formats simple riel amount without decimals', () {
        expect(formatKhr(100000), equals('៛100,000'));
      });

      test('formats with thousand separators', () {
        expect(formatKhr(5000000), equals('៛5,000,000'));
      });

      test('formats large amounts', () {
        expect(formatKhr(12345678), equals('៛12,345,678'));
      });

      test('formats zero', () {
        expect(formatKhr(0), equals('៛0'));
      });

      test('truncates decimal places for riel', () {
        expect(formatKhr(100000.99), equals('៛100,001'));
      });
    });

    group('formatDualCurrency', () {
      test('formats both currencies together', () {
        expect(
          formatDualCurrency(100.00, 400000),
          equals('\$100.00 / ៛400,000'),
        );
      });

      test('formats with proper separators for both', () {
        expect(
          formatDualCurrency(1234.56, 5000000),
          equals('\$1,234.56 / ៛5,000,000'),
        );
      });

      test('formats zero amounts', () {
        expect(
          formatDualCurrency(0, 0),
          equals('\$0.00 / ៛0'),
        );
      });
    });

    group('Extension methods', () {
      test('toUsd extension works', () {
        expect((1234.56).toUsd(), equals('\$1,234.56'));
      });

      test('toKhr extension works', () {
        expect((5000000.0).toKhr(), equals('៛5,000,000'));
      });
    });

    group('Currency symbols', () {
      test('USD symbol is standard dollar sign', () {
        final result = formatUsd(1.0);
        expect(result.startsWith('\$'), isTrue);
      });

      test('KHR symbol is Khmer riel symbol (៛)', () {
        final result = formatKhr(1.0);
        expect(result.startsWith('៛'), isTrue);
        // Verify it's the actual Unicode Khmer currency symbol
        expect(result.codeUnitAt(0), equals(0x17DB));
      });
    });
  });
}
