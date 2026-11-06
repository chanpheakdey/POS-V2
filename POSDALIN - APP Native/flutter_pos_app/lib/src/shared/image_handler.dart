import 'dart:convert';
import 'dart:io';
import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:path_provider/path_provider.dart';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import 'package:http/http.dart' as http;
import '../api/api_client.dart';
import '../api/api_client_provider.dart';
import 'local_file_store.dart';

part 'image_handler.g.dart';

/// Image type for organizing storage by type
enum ImageType {
  category,
  group,
  item;

  String get folderName {
    switch (this) {
      case ImageType.category:
        return 'categories';
      case ImageType.group:
        return 'groups';
      case ImageType.item:
        return 'items';
    }
  }
}

/// Manages image loading with local-first priority and bandwidth optimization.
/// 
/// Images are loaded with the following priority:
/// 1. Local storage (if image exists)
/// 2. Remote download from backend (with resizing)
/// 3. Placeholder image (on failure)
/// 
/// Pre-bundled image assets are copied to local storage on first launch.
/// Remote images are resized to appropriate dimensions:
/// - Categories/Groups: 300×200 pixels
/// - Item thumbnails: 200×110 pixels
/// 
/// Concurrent downloads are limited to 5 using a semaphore.
/// 
/// **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 11.11, 11.12, 16.5**
abstract class ImageHandler {
  /// Build an image widget with local → remote → placeholder priority.
  /// 
  /// [imagePath] is the relative path or URL of the image
  /// [width] and [height] are the target dimensions for resizing
  /// [placeholder] is an optional custom placeholder widget
  /// 
  /// Returns a Widget that displays the image with the specified dimensions.
  Widget buildImage(
    String imagePath,
    int width,
    int height, {
    Widget? placeholder,
  });

  /// Preload bundled images by copying them to local storage if absent.
  /// 
  /// Called on first launch to initialize local image cache with pre-bundled assets.
  Future<void> preloadBundledImages();

  /// Get the local file path for an image.
  /// 
  /// Used internally to determine where images are stored locally.
  Future<String> getLocalImagePath(String imagePath, ImageType type);

  /// Download and cache an image from the remote server.
  /// 
  /// Downloads the image from hdImage_all.ashx with width/height parameters,
  /// saves it to local storage, and returns the local file path.
  Future<String?> downloadAndCacheImage(
    String imagePath,
    int width,
    int height,
    ImageType type,
  );
}

/// Implementation of ImageHandler with concurrent download limiting.
class ImageHandlerImpl implements ImageHandler {
  final ApiClient _apiClient;
  final LocalFileStore _localFileStore;
  final int _maxConcurrentDownloads = 5;
  
  /// Semaphore for limiting concurrent downloads
  final List<Future<void>> _activeDownloads = [];

  ImageHandlerImpl({
    required ApiClient apiClient,
    required LocalFileStore localFileStore,
  })  : _apiClient = apiClient,
        _localFileStore = localFileStore;

  @override
  Widget buildImage(
    String imagePath,
    int width,
    int height, {
    Widget? placeholder,
  }) {
    // Handle empty or invalid image paths
    if (imagePath.isEmpty) {
      return _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder);
    }

    // Determine image type based on dimensions
    final type = _determineImageType(width, height);

    return FutureBuilder<String>(
      future: _resolveImagePath(imagePath, width, height, type),
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder);
        }

        if (snapshot.hasError || !snapshot.hasData) {
          return _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder);
        }

        final resolvedPath = snapshot.data!;

        // Check if it's a local file or remote URL
        if (resolvedPath.startsWith('http')) {
          // Use CachedNetworkImage for remote images
          return CachedNetworkImage(
            imageUrl: resolvedPath,
            width: width.toDouble(),
            height: height.toDouble(),
            fit: BoxFit.cover,
            placeholder: (context, url) =>
                _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder),
            errorWidget: (context, url, error) =>
                _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder),
          );
        } else {
          // Use Image.file for local images
          final file = File(resolvedPath);
          if (file.existsSync()) {
            return Image.file(
              file,
              width: width.toDouble(),
              height: height.toDouble(),
              fit: BoxFit.cover,
              errorBuilder: (context, error, stackTrace) =>
                  _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder),
            );
          } else {
            return _buildPlaceholder(width.toDouble(), height.toDouble(), placeholder);
          }
        }
      },
    );
  }

  @override
  Future<void> preloadBundledImages() async {
    // Copy bundled images for each type
    await _copyBundledImagesForType(ImageType.category);
    await _copyBundledImagesForType(ImageType.group);
    await _copyBundledImagesForType(ImageType.item);
  }

  @override
  Future<String> getLocalImagePath(String imagePath, ImageType type) async {
    final dir = await getApplicationDocumentsDirectory();
    final fileName = _extractFileName(imagePath);
    return '${dir.path}/images/${type.folderName}/$fileName';
  }

  @override
  Future<String?> downloadAndCacheImage(
    String imagePath,
    int width,
    int height,
    ImageType type,
  ) async {
    try {
      // Wait for available download slot
      await _waitForDownloadSlot();

      // Create download future
      final downloadFuture = _performDownload(imagePath, width, height, type);
      
      // Add to active downloads
      _activeDownloads.add(downloadFuture.then((_) {}).catchError((_) {}));

      // Perform the download
      final result = await downloadFuture;

      // Remove from active downloads when complete
      _activeDownloads.removeWhere((f) => f == downloadFuture);

      return result;
    } catch (e) {
      // On error, return null to fallback to placeholder
      return null;
    }
  }

  // ==================== Private Helper Methods ====================

  /// Resolve the image path with priority: local → remote → placeholder
  Future<String> _resolveImagePath(
    String imagePath,
    int width,
    int height,
    ImageType type,
  ) async {
    // Check local storage first
    final localPath = await getLocalImagePath(imagePath, type);
    final localFile = File(localPath);

    if (await localFile.exists()) {
      return localPath;
    }

    // Try to download from remote
    final downloadedPath = await downloadAndCacheImage(
      imagePath,
      width,
      height,
      type,
    );

    if (downloadedPath != null) {
      return downloadedPath;
    }

    // Fallback to remote URL (CachedNetworkImage will handle caching)
    return _apiClient.getImageUrl(imagePath, width, height);
  }

  /// Perform the actual download operation
  Future<String?> _performDownload(
    String imagePath,
    int width,
    int height,
    ImageType type,
  ) async {
    try {
      // Get the remote image URL with resizing parameters
      final imageUrl = _apiClient.getImageUrl(imagePath, width, height);

      // Download the image
      final response = await http.get(Uri.parse(imageUrl))
          .timeout(const Duration(seconds: 30));

      if (response.statusCode != 200) {
        return null;
      }

      // Save to local storage
      final localPath = await getLocalImagePath(imagePath, type);
      final file = File(localPath);

      // Ensure parent directory exists
      await file.parent.create(recursive: true);

      // Write image bytes
      await file.writeAsBytes(response.bodyBytes);

      return localPath;
    } catch (e) {
      return null;
    }
  }

  /// Wait for an available download slot (max 5 concurrent downloads)
  Future<void> _waitForDownloadSlot() async {
    while (_activeDownloads.length >= _maxConcurrentDownloads) {
      // Wait for any download to complete
      if (_activeDownloads.isNotEmpty) {
        await Future.any(_activeDownloads);
        // Clean up completed futures
        _activeDownloads.removeWhere((future) {
          // Check if the future is complete by checking if it's resolved
          bool isComplete = false;
          future.then((_) => isComplete = true).catchError((_) => isComplete = true);
          return isComplete;
        });
      } else {
        break;
      }
    }
  }

  /// Copy bundled images for a specific type to local storage
  Future<void> _copyBundledImagesForType(ImageType type) async {
    try {
      final assetPath = 'assets/images/${type.folderName}/';
      
      // Load asset manifest to discover all images
      final manifestMap = await rootBundle.loadStructuredData<Map<String, dynamic>>(
        'AssetManifest.json',
        (String jsonStr) async {
          return Map<String, dynamic>.from(
            json.decode(jsonStr) as Map,
          );
        },
      );

      // For each asset image, copy to local storage if absent
      for (final assetKey in manifestMap.keys) {
        if (assetKey.startsWith(assetPath) && !assetKey.endsWith('/')) {
          final fileName = assetKey.substring(assetPath.length);
          
          // Use LocalFileStore.copyAssetIfAbsent to copy the asset
          await _localFileStore.copyAssetIfAbsent(
            assetKey,
            'images/${type.folderName}/$fileName',
          );
        }
      }
    } catch (e) {
      // Silently fail if asset loading fails
      // The app can still function with remote images
    }
  }

  /// Determine image type based on dimensions
  ImageType _determineImageType(int width, int height) {
    // Categories and groups use 300×200
    if (width == 300 && height == 200) {
      // Can't distinguish between category and group by dimensions alone
      // Default to category
      return ImageType.category;
    }
    // Item thumbnails use 200×110
    else if (width == 200 && height == 110) {
      return ImageType.item;
    }
    // Default to item for other dimensions
    return ImageType.item;
  }

  /// Extract file name from image path
  String _extractFileName(String imagePath) {
    // Handle paths with ^ separator (backend format)
    final cleanPath = imagePath.replaceAll('^', '/');
    
    // Extract the last component as file name
    final parts = cleanPath.split('/');
    return parts.isNotEmpty ? parts.last : imagePath;
  }

  /// Build a placeholder widget
  Widget _buildPlaceholder(double width, double height, Widget? customPlaceholder) {
    if (customPlaceholder != null) {
      return SizedBox(
        width: width,
        height: height,
        child: customPlaceholder,
      );
    }

    // Default placeholder: gray box with icon
    return Container(
      width: width,
      height: height,
      color: Colors.grey[300],
      child: const Icon(
        Icons.image,
        color: Colors.grey,
      ),
    );
  }
}

/// Riverpod provider for ImageHandler.
/// 
/// **Validates: Requirements 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 11.11, 11.12, 16.5**
@Riverpod(keepAlive: true)
ImageHandler imageHandler(ImageHandlerRef ref) {
  // Dependencies are injected from other providers
  final apiClient = ref.watch(apiClientProvider);
  final localFileStore = ref.watch(localFileStoreProvider);

  return ImageHandlerImpl(
    apiClient: apiClient,
    localFileStore: localFileStore,
  );
}
