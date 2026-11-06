import 'package:flutter/material.dart';

/// Responsive layout utilities for adaptive UI across different screen sizes.
/// 
/// Provides centralized logic for:
/// - Grid column count based on screen width (phone portrait/landscape, tablet)
/// - Touch target sizing (minimum 48×48 logical pixels)
/// - Text scaling for different screen densities
/// - Breakpoints for responsive layouts
/// 
/// **Validates: Requirements 14.1, 14.2, 14.3, 14.4, 14.5, 14.6**
class ResponsiveLayout {
  /// Minimum touch target size in logical pixels (WCAG 2.1 Level AA)
  static const double minTouchTargetSize = 48.0;

  /// Screen size breakpoints
  static const double smallPhoneWidth = 360.0;  // 4-5 inches
  static const double standardPhoneWidth = 600.0;  // 5-6 inches
  static const double tabletWidth = 900.0;  // 7+ inches

  /// Portrait orientation breakpoint
  static const double portraitAspectRatio = 1.0;

  /// Calculate number of grid columns based on screen width and orientation
  /// 
  /// Returns:
  /// - 1 column: small phones in portrait (< 360dp)
  /// - 2 columns: standard phones in portrait (360-600dp)
  /// - 3 columns: phones in landscape or small tablets (600-900dp)
  /// - 4 columns: tablets and larger devices (> 900dp)
  /// 
  /// **Validates: Requirements 14.1, 14.3**
  static int getGridColumnCount(BuildContext context) {
    final size = MediaQuery.of(context).size;
    final screenWidth = size.width;
    final isPortrait = size.height > size.width;

    // Tablet (7+ inches, typically >900dp)
    if (screenWidth >= tabletWidth) {
      return 4;
    }
    // Large phone landscape or small tablet (600-900dp)
    else if (screenWidth >= standardPhoneWidth) {
      return isPortrait ? 2 : 3;
    }
    // Standard phone (360-600dp)
    else if (screenWidth >= smallPhoneWidth) {
      return 2;
    }
    // Small phone (< 360dp)
    else {
      return 1;
    }
  }

  /// Calculate item grid column count (optimized for item cards)
  /// 
  /// Returns:
  /// - 2 columns: phones in portrait (< 600dp)
  /// - 3 columns: phones in landscape or small tablets (600-900dp)
  /// - 4 columns: tablets and larger devices (> 900dp)
  /// 
  /// **Validates: Requirements 14.1, 14.3**
  static int getItemGridColumnCount(BuildContext context) {
    final size = MediaQuery.of(context).size;
    final screenWidth = size.width;

    // Tablet (> 900dp)
    if (screenWidth >= tabletWidth) {
      return 4;
    }
    // Phone landscape or small tablet (600-900dp)
    else if (screenWidth >= standardPhoneWidth) {
      return 3;
    }
    // Phone portrait (< 600dp)
    else {
      return 2;
    }
  }

  /// Get responsive text scale factor based on screen width
  /// 
  /// Returns a scale factor for text sizing:
  /// - 0.9: small phones (< 360dp)
  /// - 1.0: standard phones (360-600dp)
  /// - 1.1: tablets (> 600dp)
  /// 
  /// **Validates: Requirements 14.4**
  static double getTextScaleFactor(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;

    if (screenWidth >= standardPhoneWidth) {
      return 1.1; // Slightly larger text on tablets
    } else if (screenWidth >= smallPhoneWidth) {
      return 1.0; // Standard size on normal phones
    } else {
      return 0.9; // Slightly smaller on small phones
    }
  }

  /// Get responsive padding based on screen size
  /// 
  /// Returns EdgeInsets with:
  /// - 8.0: small phones (< 360dp)
  /// - 12.0: standard phones (360-600dp)
  /// - 16.0: tablets (> 600dp)
  /// 
  /// **Validates: Requirements 14.5, 14.6**
  static EdgeInsets getResponsivePadding(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;

    if (screenWidth >= standardPhoneWidth) {
      return const EdgeInsets.all(16.0);
    } else if (screenWidth >= smallPhoneWidth) {
      return const EdgeInsets.all(12.0);
    } else {
      return const EdgeInsets.all(8.0);
    }
  }

  /// Get responsive spacing based on screen size
  /// 
  /// Returns spacing value:
  /// - 4.0: small phones (< 360dp)
  /// - 8.0: standard phones (360-600dp)
  /// - 12.0: tablets (> 600dp)
  /// 
  /// **Validates: Requirements 14.5, 14.6**
  static double getResponsiveSpacing(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;

    if (screenWidth >= standardPhoneWidth) {
      return 12.0;
    } else if (screenWidth >= smallPhoneWidth) {
      return 8.0;
    } else {
      return 4.0;
    }
  }

  /// Ensure a widget meets minimum touch target size
  /// 
  /// Wraps the child in a Container with minimum 48×48 logical pixel constraints.
  /// 
  /// **Validates: Requirements 14.2**
  static Widget ensureTouchTarget({
    required Widget child,
    double minSize = minTouchTargetSize,
  }) {
    return ConstrainedBox(
      constraints: BoxConstraints(
        minWidth: minSize,
        minHeight: minSize,
      ),
      child: child,
    );
  }

  /// Create a button with guaranteed touch target size
  /// 
  /// **Validates: Requirements 14.2**
  static Widget touchTargetButton({
    required VoidCallback? onPressed,
    required Widget child,
    EdgeInsetsGeometry? padding,
    double minSize = minTouchTargetSize,
  }) {
    return ConstrainedBox(
      constraints: BoxConstraints(
        minWidth: minSize,
        minHeight: minSize,
      ),
      child: TextButton(
        onPressed: onPressed,
        style: TextButton.styleFrom(
          padding: padding ?? const EdgeInsets.all(8.0),
          minimumSize: Size(minSize, minSize),
          tapTargetSize: MaterialTapTargetSize.padded,
        ),
        child: child,
      ),
    );
  }

  /// Create an icon button with guaranteed touch target size
  /// 
  /// **Validates: Requirements 14.2**
  static Widget touchTargetIconButton({
    required VoidCallback? onPressed,
    required Widget icon,
    String? tooltip,
    Color? color,
    double minSize = minTouchTargetSize,
  }) {
    return IconButton(
      onPressed: onPressed,
      icon: icon,
      tooltip: tooltip,
      color: color,
      iconSize: 24.0,
      padding: const EdgeInsets.all(12.0),
      constraints: BoxConstraints(
        minWidth: minSize,
        minHeight: minSize,
      ),
    );
  }

  /// Check if device is in portrait orientation
  /// 
  /// **Validates: Requirements 14.1**
  static bool isPortrait(BuildContext context) {
    final size = MediaQuery.of(context).size;
    return size.height > size.width;
  }

  /// Check if device is in landscape orientation
  /// 
  /// **Validates: Requirements 14.1**
  static bool isLandscape(BuildContext context) {
    return !isPortrait(context);
  }

  /// Get device category
  /// 
  /// **Validates: Requirements 14.5**
  static DeviceCategory getDeviceCategory(BuildContext context) {
    final screenWidth = MediaQuery.of(context).size.width;

    if (screenWidth >= tabletWidth) {
      return DeviceCategory.tablet;
    } else if (screenWidth >= standardPhoneWidth) {
      return DeviceCategory.standardPhone;
    } else if (screenWidth >= smallPhoneWidth) {
      return DeviceCategory.smallPhone;
    } else {
      return DeviceCategory.tinyPhone;
    }
  }
}

/// Device category enumeration for responsive layouts
enum DeviceCategory {
  tinyPhone,    // < 360dp
  smallPhone,   // 360-600dp
  standardPhone, // 600-900dp (landscape or small tablet)
  tablet,       // > 900dp
}

/// Extension methods for responsive sizing
extension ResponsiveSizeExtension on BuildContext {
  /// Get grid column count for category/group grids
  int get gridColumns => ResponsiveLayout.getGridColumnCount(this);

  /// Get grid column count optimized for item grids
  int get itemGridColumns => ResponsiveLayout.getItemGridColumnCount(this);

  /// Get text scale factor
  double get textScale => ResponsiveLayout.getTextScaleFactor(this);

  /// Get responsive padding
  EdgeInsets get responsivePadding => ResponsiveLayout.getResponsivePadding(this);

  /// Get responsive spacing
  double get responsiveSpacing => ResponsiveLayout.getResponsiveSpacing(this);

  /// Check if portrait
  bool get isPortrait => ResponsiveLayout.isPortrait(this);

  /// Check if landscape
  bool get isLandscape => ResponsiveLayout.isLandscape(this);

  /// Get device category
  DeviceCategory get deviceCategory => ResponsiveLayout.getDeviceCategory(this);
}
