import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../search/search_service.dart';
import '../../shared/models/item.dart';
import '../../shared/image_handler.dart';
import '../../shared/constants.dart';
import '../../shared/responsive_layout.dart';
import '../../cart/cart_service.dart';
import '../../catalog/catalog_service.dart';

/// Search screen that allows customers to search for items by name or code.
/// 
/// Features:
/// - Search input field with 2-character minimum enforcement
/// - Real-time search results displayed in grid format
/// - Results match category browsing grid style
/// - "No items found" message when search returns empty
/// - Clear button to return to category view
/// - Tap on item → add to cart with visual feedback
/// - Long-press on item → navigate to item detail (future)
/// 
/// **Validates: Requirements 9.1, 9.2, 9.4, 9.5, 9.6, 9.7**
class SearchScreen extends ConsumerStatefulWidget {
  const SearchScreen({super.key});

  @override
  ConsumerState<SearchScreen> createState() => _SearchScreenState();
}

class _SearchScreenState extends ConsumerState<SearchScreen> {
  final TextEditingController _searchController = TextEditingController();
  List<Item> _searchResults = [];
  bool _isSearching = false;
  String _currentQuery = '';
  int? _animatingItemId;

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Search Products'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          tooltip: 'Back to Categories',
          onPressed: () {
            context.pop();
          },
        ),
      ),
      body: Column(
        children: [
          // Search input section
          _buildSearchInput(),
          
          // Search results section
          Expanded(
            child: _buildSearchResults(),
          ),
        ],
      ),
    );
  }

  /// Build the search input field with clear button
  Widget _buildSearchInput() {
    return Container(
      padding: const EdgeInsets.all(16.0),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.1),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      child: TextField(
        controller: _searchController,
        autofocus: true,
        decoration: InputDecoration(
          hintText: 'Search by name or code (min 2 characters)',
          prefixIcon: const Icon(Icons.search),
          suffixIcon: _searchController.text.isNotEmpty
              ? IconButton(
                  icon: const Icon(Icons.clear),
                  tooltip: 'Clear search',
                  onPressed: () {
                    setState(() {
                      _searchController.clear();
                      _searchResults = [];
                      _currentQuery = '';
                    });
                  },
                )
              : null,
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: BorderSide(color: Colors.grey.shade300),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: BorderSide(color: Colors.grey.shade300),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: Colors.blue, width: 2),
          ),
          filled: true,
          fillColor: Colors.grey.shade50,
        ),
        onChanged: _handleSearchQueryChanged,
      ),
    );
  }

  /// Build the search results area
  Widget _buildSearchResults() {
    // Show hint when no search has been performed
    if (_currentQuery.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.search,
              size: 64,
              color: Colors.grey.shade400,
            ),
            const SizedBox(height: 16),
            Text(
              'Search for items by name or code',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Colors.grey.shade600,
                  ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              'Enter at least 2 characters to start searching',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Colors.grey.shade500,
                  ),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      );
    }

    // Show loading indicator while searching
    if (_isSearching) {
      return const Center(
        child: CircularProgressIndicator(),
      );
    }

    // Show "No items found" when search returns empty (Requirement 9.5)
    if (_searchResults.isEmpty) {
      return Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              Icons.search_off,
              size: 64,
              color: Colors.grey.shade400,
            ),
            const SizedBox(height: 16),
            Text(
              'No items found',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                    color: Colors.grey.shade700,
                  ),
            ),
            const SizedBox(height: 8),
            Text(
              'Try a different search term',
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: Colors.grey.shade500,
                  ),
            ),
          ],
        ),
      );
    }

    // Display search results in grid format (same as category browsing - Requirement 9.4)
    return SearchResultsGrid(
      items: _searchResults,
      animatingItemId: _animatingItemId,
      onItemTap: _handleItemTap,
      onItemLongPress: _handleItemLongPress,
    );
  }

  /// Handle search query changes with 2-character minimum enforcement
  Future<void> _handleSearchQueryChanged(String query) async {
    setState(() {
      _currentQuery = query;
    });

    // Enforce 2-character minimum (Requirement 9.7)
    if (query.length < 2) {
      setState(() {
        _searchResults = [];
        _isSearching = false;
      });
      return;
    }

    // Perform search
    setState(() {
      _isSearching = true;
    });

    try {
      final searchService = ref.read(searchServiceProvider);
      final results = await searchService.search(query);

      setState(() {
        _searchResults = results;
        _isSearching = false;
      });
    } catch (e) {
      // On error, show empty results
      setState(() {
        _searchResults = [];
        _isSearching = false;
      });

      // Show error message
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Search failed: ${e.toString()}'),
            backgroundColor: Colors.red,
            duration: const Duration(seconds: 2),
          ),
        );
      }
    }
  }

  /// Handle item tap - add one unit to cart with animation
  Future<void> _handleItemTap(Item item) async {
    final catalogService = ref.read(catalogServiceProvider);
    final cartNotifier = ref.read(cartProvider.notifier);

    try {
      // Get price tiers for the item
      final priceTiers = await catalogService.getPriceTiers(item.itemId);

      // Trigger animation
      setState(() {
        _animatingItemId = item.itemId;
      });

      // Add item to cart
      await cartNotifier.addItem(item, priceTiers);

      // Clear animation after a short delay
      Future.delayed(const Duration(milliseconds: 300), () {
        if (mounted) {
          setState(() {
            _animatingItemId = null;
          });
        }
      });
    } catch (e) {
      // Show error message
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to add item: ${e.toString()}'),
            backgroundColor: Colors.red,
            duration: const Duration(seconds: 2),
          ),
        );
      }
    }
  }

  /// Handle item long-press - navigate to item detail (placeholder for future implementation)
  void _handleItemLongPress(Item item) {
    // TODO: Navigate to item detail screen when it's implemented
    // For now, show a placeholder message
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text('View details for ${item.itemName}'),
        duration: const Duration(seconds: 1),
      ),
    );

    // This will be replaced with actual navigation:
    // context.push(Routes.itemDetail(item.itemId));
  }
}

/// Grid widget for displaying search results in the same format as category browsing
class SearchResultsGrid extends ConsumerWidget {
  final List<Item> items;
  final int? animatingItemId;
  final Future<void> Function(Item) onItemTap;
  final void Function(Item) onItemLongPress;

  const SearchResultsGrid({
    super.key,
    required this.items,
    required this.animatingItemId,
    required this.onItemTap,
    required this.onItemLongPress,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final imageHandler = ref.watch(imageHandlerProvider);

    return GridView.builder(
      padding: context.responsivePadding,
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: context.itemGridColumns,
        crossAxisSpacing: context.responsiveSpacing,
        mainAxisSpacing: context.responsiveSpacing,
        childAspectRatio: 200 / 140, // 200×110 image + space for name
      ),
      itemCount: items.length,
      itemBuilder: (context, index) {
        final item = items[index];
        return SearchResultCard(
          item: item,
          imageHandler: imageHandler,
          isAnimating: animatingItemId == item.itemId,
          onTap: () => onItemTap(item),
          onLongPress: () => onItemLongPress(item),
        );
      },
    );
  }
}

/// Individual card widget for a search result item with quantity badge
class SearchResultCard extends ConsumerWidget {
  final Item item;
  final ImageHandler imageHandler;
  final bool isAnimating;
  final VoidCallback onTap;
  final VoidCallback onLongPress;

  const SearchResultCard({
    super.key,
    required this.item,
    required this.imageHandler,
    required this.isAnimating,
    required this.onTap,
    required this.onLongPress,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Watch cart state to get current quantity
    final cartAsync = ref.watch(cartProvider);
    final quantity = cartAsync.when(
      data: (cartService) => cartService.quantityFor(item.itemId),
      loading: () => 0,
      error: (_, __) => 0,
    );
    final textScale = context.textScale;

    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: 4,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
      ),
      child: InkWell(
        onTap: onTap,
        onLongPress: onLongPress,
        child: ResponsiveLayout.ensureTouchTarget(
          child: AnimatedScale(
            scale: isAnimating ? 0.95 : 1.0,
            duration: const Duration(milliseconds: 150),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              decoration: BoxDecoration(
                color: isAnimating ? Colors.green.withOpacity(0.1) : null,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Image section with quantity badge
                  Expanded(
                    flex: 3,
                    child: Stack(
                      children: [
                        // Item image
                        imageHandler.buildImage(
                          item.imageUrl,
                          ImageDimensions.itemThumbWidth,
                          ImageDimensions.itemThumbHeight,
                          placeholder: Container(
                            color: Colors.grey[200],
                            child: const Icon(
                              Icons.image,
                              size: 32,
                              color: Colors.grey,
                            ),
                          ),
                        ),
                        // Quantity badge (visible when qty > 0)
                        if (quantity > 0)
                          Positioned(
                            top: 4,
                            right: 4,
                            child: _QuantityBadge(quantity: quantity),
                          ),
                      ],
                    ),
                  ),
                  // Item name and code section
                  Expanded(
                    flex: 1,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 4,
                        vertical: 4,
                      ),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Text(
                            item.itemName,
                            style: TextStyle(
                              fontSize: 12 * textScale,
                              fontWeight: FontWeight.w500,
                            ),
                            textAlign: TextAlign.center,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                          Text(
                            item.itemCode,
                            style: TextStyle(
                              fontSize: 10 * textScale,
                              color: Colors.grey.shade600,
                            ),
                            textAlign: TextAlign.center,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// Quantity badge widget to display item quantity in cart
class _QuantityBadge extends StatelessWidget {
  final int quantity;

  const _QuantityBadge({
    required this.quantity,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: Colors.red,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.3),
            blurRadius: 4,
            offset: const Offset(0, 2),
          ),
        ],
      ),
      constraints: const BoxConstraints(
        minWidth: 24,
        minHeight: 24,
      ),
      child: Center(
        child: Text(
          quantity.toString(),
          style: const TextStyle(
            color: Colors.white,
            fontSize: 12,
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
    );
  }
}
