// ignore_for_file: unused_local_variable

/// Example usage of Khmer font and locale support features.
///
/// This file demonstrates how to use the currency formatting and
/// bilingual text utilities in actual UI components.
///
/// DO NOT import this file in production code - it's for reference only.
library;

import 'package:flutter/material.dart';
import '../currency_formatter.dart';
import '../locale_config.dart';

/// Example: Price display with dual currency
class PriceDisplayExample extends StatelessWidget {
  const PriceDisplayExample({
    super.key,
    required this.priceUsd,
    required this.priceKhr,
  });

  final double priceUsd;
  final double priceKhr;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Example 1: Show both currencies on one line
        Text(
          formatDualCurrency(priceUsd, priceKhr),
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 8),

        // Example 2: Show currencies on separate lines
        Text(
          formatUsd(priceUsd),
          style: Theme.of(context).textTheme.bodyLarge,
        ),
        Text(
          formatKhr(priceKhr),
          style: Theme.of(context).textTheme.bodyMedium,
        ),
        const SizedBox(height: 8),

        // Example 3: Using extension methods
        Text(priceUsd.toUsd()),
        Text(priceKhr.toKhr()),
      ],
    );
  }
}

/// Example: Bilingual product name display
class BilingualProductNameExample extends StatelessWidget {
  const BilingualProductNameExample({
    super.key,
    required this.nameEnglish,
    required this.nameKhmer,
    required this.currentLocale,
  });

  final String nameEnglish;
  final String nameKhmer;
  final Locale currentLocale;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Example 1: Show both languages if Khmer is available
        Text(
          nameKhmer.isNotEmpty
              ? nameEnglish.withKhmer(nameKhmer)
              : nameEnglish,
          style: Theme.of(context).textTheme.headlineSmall,
        ),
        const SizedBox(height: 8),

        // Example 2: Show based on current locale
        Text(
          nameEnglish.orKhmer(nameKhmer, currentLocale),
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: 8),

        // Example 3: Conditional display based on locale
        if (LanguageUtils.isKhmer(currentLocale) && nameKhmer.isNotEmpty)
          Text(
            nameKhmer,
            style: Theme.of(context).textTheme.bodyLarge,
          )
        else
          Text(
            nameEnglish,
            style: Theme.of(context).textTheme.bodyLarge,
          ),
      ],
    );
  }
}

/// Example: Item card with price and bilingual name
class ItemCardExample extends StatelessWidget {
  const ItemCardExample({
    super.key,
    required this.itemNameEnglish,
    required this.itemNameKhmer,
    required this.itemCode,
    required this.priceUsd,
    required this.priceKhr,
    required this.currentLocale,
  });

  final String itemNameEnglish;
  final String itemNameKhmer;
  final String itemCode;
  final double priceUsd;
  final double priceKhr;
  final Locale currentLocale;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Bilingual item name
            Text(
              itemNameKhmer.isNotEmpty
                  ? itemNameEnglish.withKhmer(itemNameKhmer)
                  : itemNameEnglish,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: 4),

            // Item code
            Text(
              'Code: $itemCode',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Colors.grey[600],
                  ),
            ),
            const SizedBox(height: 12),

            // Dual currency price
            Container(
              padding: const EdgeInsets.symmetric(
                horizontal: 12,
                vertical: 6,
              ),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.primaryContainer,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                formatDualCurrency(priceUsd, priceKhr),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      color: Theme.of(context).colorScheme.onPrimaryContainer,
                      fontWeight: FontWeight.bold,
                    ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Example: Category header with bilingual title
class CategoryHeaderExample extends StatelessWidget {
  const CategoryHeaderExample({
    super.key,
    required this.categoryNameEnglish,
    required this.categoryNameKhmer,
  });

  final String categoryNameEnglish;
  final String categoryNameKhmer;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      color: Theme.of(context).colorScheme.surfaceContainerHighest,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Show both languages
          Text(
            categoryNameKhmer.isNotEmpty
                ? categoryNameEnglish.withKhmer(categoryNameKhmer)
                : categoryNameEnglish,
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
        ],
      ),
    );
  }
}

/// Example: Language selector widget
class LanguageSelectorExample extends StatelessWidget {
  const LanguageSelectorExample({
    super.key,
    required this.currentLocale,
    required this.onLocaleChanged,
  });

  final Locale currentLocale;
  final ValueChanged<Locale> onLocaleChanged;

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<Locale>(
      segments: LocaleConfig.supportedLocales
          .map(
            (locale) => ButtonSegment<Locale>(
              value: locale,
              label: Text(LanguageUtils.getDisplayName(locale)),
            ),
          )
          .toList(),
      selected: {currentLocale},
      onSelectionChanged: (Set<Locale> selected) {
        if (selected.isNotEmpty) {
          onLocaleChanged(selected.first);
        }
      },
    );
  }
}

/// Example: Cart total display with dual currency
class CartTotalExample extends StatelessWidget {
  const CartTotalExample({
    super.key,
    required this.totalUsd,
    required this.totalKhr,
  });

  final double totalUsd;
  final double totalKhr;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.secondaryContainer,
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        children: [
          Text(
            'Total',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSecondaryContainer,
                ),
          ),
          const SizedBox(height: 8),
          Text(
            formatDualCurrency(totalUsd, totalKhr),
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onSecondaryContainer,
                  fontWeight: FontWeight.bold,
                ),
          ),
        ],
      ),
    );
  }
}
