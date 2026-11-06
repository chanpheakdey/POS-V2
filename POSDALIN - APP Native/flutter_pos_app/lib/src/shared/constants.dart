/// Global constants and configuration values for the Flutter POS App
///
/// This file contains app-wide constants including:
/// - Category color palette (21 colors for category cards)
/// - Image dimensions and optimization settings
/// - Timeout and network configuration
/// - File paths for local storage
library;

import 'package:flutter/material.dart';

/// Category and group card background colors (21 colors, cycles)
///
/// These colors are applied to category and group cards in the order
/// they appear, cycling back to the first color after the 21st item.
const List<String> kCategoryColors = [
  '#04478a',
  '#8a040a',
  '#8a045b',
  '#6f048a',
  '#31048a',
  '#04148a',
  '#045f8a',
  '#048a71',
  '#048a27',
  '#3e8a04',
  '#888a04',
  '#8a4804',
  '#8a1704',
  '#b32020',
  '#b32071',
  '#9620b3',
  '#3d20b3',
  '#2068b3',
  '#20b3a5',
  '#20b339',
  '#4bb320',
];

/// Returns the color for a given index (cycles through kCategoryColors)
Color colorForIndex(int index) {
  final colorHex = kCategoryColors[index % kCategoryColors.length];
  return Color(
    int.parse(colorHex.replaceFirst('#', '0xFF')),
  );
}

/// Image dimension constants
class ImageDimensions {
  const ImageDimensions._();

  /// Category image dimensions: 300x200 pixels
  static const int categoryWidth = 300;
  static const int categoryHeight = 200;

  /// Group image dimensions: 300x200 pixels
  static const int groupWidth = 300;
  static const int groupHeight = 200;

  /// Item thumbnail dimensions: 200x110 pixels
  static const int itemThumbWidth = 200;
  static const int itemThumbHeight = 110;

  /// Maximum concurrent image downloads
  static const int maxConcurrentDownloads = 5;
}

/// Network and API configuration
class NetworkConfig {
  const NetworkConfig._();

  /// API request timeout in seconds
  static const int requestTimeoutSeconds = 30;

  /// ASMX service base URL (update this to your backend URL)
  static const String baseUrl = 'https://your-backend-url.com';

  /// ASMX service endpoint
  static const String asmxEndpoint = '/appservice.asmx';

  /// Image endpoint for dynamic resizing
  static const String imageEndpoint = '/hdImage_all.ashx';
}

/// Local storage paths (relative to app documents directory)
class StoragePaths {
  const StoragePaths._();

  // Catalog data files
  static const String catalogDir = 'catalog';
  static const String categoriesFile = 'catalog/categories.json';
  static const String groupsDir = 'catalog/groups';
  static const String itemsDir = 'catalog/items';
  static const String itemDetailsDir = 'catalog/item_details';
  static const String priceTiersDir = 'catalog/price_tiers';

  // Image storage
  static const String imagesDir = 'images';
  static const String categoryImagesDir = 'images/categories';
  static const String groupImagesDir = 'images/groups';
  static const String itemImagesDir = 'images/items';

  // Order and cart storage
  static const String cartFile = 'cart.json';
  static const String orderQueueFile = 'orders/queue.json';

  // Sync metadata
  static const String syncMetaFile = 'sync_meta.json';
}

/// Asset bundle paths (for pre-bundled data)
class AssetPaths {
  const AssetPaths._();

  // Catalog data assets
  static const String catalogDir = 'assets/catalog';
  static const String categoriesAsset = 'assets/catalog/categories.json';
  static const String groupsDir = 'assets/catalog/groups';
  static const String itemsDir = 'assets/catalog/items';
  static const String itemDetailsDir = 'assets/catalog/item_details';
  static const String priceTiersDir = 'assets/catalog/price_tiers';

  // Image assets
  static const String imagesDir = 'assets/images';
  static const String categoryImagesDir = 'assets/images/categories';
  static const String groupImagesDir = 'assets/images/groups';
  static const String itemImagesDir = 'assets/images/items';
  static const String placeholderImage = 'assets/images/placeholder.png';

  // Fonts
  static const String fontsDir = 'assets/fonts';
}

/// User level constants (for authentication filtering)
class UserLevel {
  const UserLevel._();

  /// Only customers are allowed to use the mobile app
  static const String customer = 'Customer';
}

/// Error messages (exact messages from backend)
class ErrorMessages {
  const ErrorMessages._();

  static const String usernameIncorrect = 'User name is incorrect!';
  static const String passwordIncorrect = 'Password is incorrect!';
  static const String accountLocked =
      'Your user account has been locked! Please contact IT';
  static const String noInternet =
      'No internet connection. Please try again when online.';
  static const String unexpectedError = 'An unexpected error occurred';
  static const String noItemsFound = 'No items found';
  static const String cartEmpty = 'Your cart is empty';
}

/// Validation rules
class ValidationRules {
  const ValidationRules._();

  /// Minimum search query length
  static const int minSearchLength = 2;

  /// Password attempt limit before account lock
  static const int maxPasswordAttempts = 5;
}

/// Performance targets (for testing and monitoring)
class PerformanceTargets {
  const PerformanceTargets._();

  /// Maximum splash screen duration in seconds
  static const int maxSplashDuration = 2;

  /// Target cold-start time in seconds
  static const int targetColdStart = 3;

  /// Target local catalog load time in milliseconds
  static const int targetCatalogLoad = 500;

  /// Target frame rate for smooth scrolling
  static const int targetFrameRate = 60;

  /// Target screen transition duration in milliseconds
  static const int targetTransitionTime = 300;
}
