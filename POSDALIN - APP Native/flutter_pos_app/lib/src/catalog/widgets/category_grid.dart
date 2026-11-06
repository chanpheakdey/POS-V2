import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../shared/constants.dart';
import '../../shared/models/models.dart';
import '../../shared/image_handler.dart';
import '../../shared/responsive_layout.dart';
import '../catalog_service.dart';

/// Category grid widget that displays categories in a scrollable grid layout.
/// 
/// Features:
/// - Scrollable grid layout with color-coded category cards
/// - Each card shows category name and image
/// - Load via CatalogService.getCategories
/// - Handle loading, error, and empty states
/// - Tap to navigate to groups or items for that category
/// - Implement pagination for >50 categories
/// 
/// **Validates: Requirements 3.2, 3.6, 3.7, 3.8, 3.9, 14.3, 16.4**
class CategoryGrid extends ConsumerStatefulWidget {
  /// Callback when a category is tapped
  final void Function(Category category) onCategoryTap;

  const CategoryGrid({
    super.key,
    required this.onCategoryTap,
  });

  @override
  ConsumerState<CategoryGrid> createState() => _CategoryGridState();
}

class _CategoryGridState extends ConsumerState<CategoryGrid> {
  List<Category> _categories = [];
  bool _isLoading = false;
  String? _errorMessage;
  int _currentPage = 0;
  static const int _pageSize = 50;

  @override
  void initState() {
    super.initState();
    _loadCategories();
  }

  Future<void> _loadCategories() async {
    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final catalogService = ref.read(catalogServiceProvider);
      final categories = await catalogService.getCategories();

      if (mounted) {
        setState(() {
          _categories = categories;
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _errorMessage = e.toString();
          _isLoading = false;
        });
      }
    }
  }

  List<Category> get _displayedCategories {
    final endIndex = (_currentPage + 1) * _pageSize;
    return _categories.take(endIndex).toList();
  }

  bool get _hasMorePages {
    return _categories.length > (_currentPage + 1) * _pageSize;
  }

  void _loadNextPage() {
    if (_hasMorePages) {
      setState(() {
        _currentPage++;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    // Loading state
    if (_isLoading && _categories.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    // Error state
    if (_errorMessage != null && _categories.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.error_outline,
              size: 48,
              color: Colors.red,
            ),
            const SizedBox(height: 16),
            Text(
              _errorMessage!,
              textAlign: TextAlign.center,
              style: const TextStyle(color: Colors.red),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: _loadCategories,
              child: const Text('Retry'),
            ),
          ],
        ),
      );
    }

    // Empty state
    if (_categories.isEmpty) {
      return const Center(
        child: Text('No categories found'),
      );
    }

    // Category grid with pagination
    return NotificationListener<ScrollNotification>(
      onNotification: (ScrollNotification scrollInfo) {
        // Load more when reaching bottom
        if (!_isLoading &&
            _hasMorePages &&
            scrollInfo.metrics.pixels >= scrollInfo.metrics.maxScrollExtent * 0.9) {
          _loadNextPage();
        }
        return false;
      },
      child: GridView.builder(
        padding: context.responsivePadding,
        gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: context.gridColumns,
          crossAxisSpacing: context.responsiveSpacing,
          mainAxisSpacing: context.responsiveSpacing,
          childAspectRatio: 1.5, // Width:Height ratio for cards
        ),
        itemCount: _displayedCategories.length + (_hasMorePages ? 1 : 0),
        itemBuilder: (context, index) {
          // Show loading indicator for next page
          if (index >= _displayedCategories.length) {
            return const Center(
              child: Padding(
                padding: EdgeInsets.all(16.0),
                child: CircularProgressIndicator(),
              ),
            );
          }

          final category = _displayedCategories[index];
          return CategoryCard(
            category: category,
            onTap: () => widget.onCategoryTap(category),
          );
        },
      ),
    );
  }
}

/// Individual category card widget with color-coded background
class CategoryCard extends ConsumerWidget {
  final Category category;
  final VoidCallback onTap;

  const CategoryCard({
    super.key,
    required this.category,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final imageHandler = ref.watch(imageHandlerProvider);
    final textScale = context.textScale;
    
    return Card(
      elevation: 2,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: ResponsiveLayout.ensureTouchTarget(
          child: Container(
            decoration: BoxDecoration(
              color: colorForIndex(category.colorIndex),
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                // Category image
                if (category.imageUrl.isNotEmpty)
                  Expanded(
                    flex: 2,
                    child: Padding(
                      padding: const EdgeInsets.all(8.0),
                      child: imageHandler.buildImage(
                        category.imageUrl,
                        ImageDimensions.categoryWidth,
                        ImageDimensions.categoryHeight,
                        placeholder: const Center(
                          child: CircularProgressIndicator(
                            valueColor: AlwaysStoppedAnimation<Color>(Colors.white),
                          ),
                        ),
                      ),
                    ),
                  ),
                
                // Category name
                Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Text(
                    category.categoryName,
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 16 * textScale,
                      fontWeight: FontWeight.bold,
                    ),
                    textAlign: TextAlign.center,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
