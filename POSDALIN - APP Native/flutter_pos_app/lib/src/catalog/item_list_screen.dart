import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../shared/constants.dart';
import '../shared/models/models.dart';
import '../shared/image_handler.dart';
import '../shared/responsive_layout.dart';
import '../cart/cart_service.dart';
import '../routing/routes.dart';
import 'catalog_service.dart';

/// Screen displaying a scrollable grid of items within a category/group.
/// 
/// Features:
/// - Scrollable grid with item cards (image 200×110, name, quantity badge)
/// - Load via CatalogService.getItems
/// - Tap → CartService.addItem (+1), quantity badge updates immediately
/// - Long-press → navigate to item detail screen
/// - Show visual feedback animation on add
/// 
/// **Validates: Requirements 4.6, 4.7, 6.1, 6.2, 6.3, 6.4, 6.11, 6.12**
class ItemListScreen extends ConsumerStatefulWidget {
  final int categoryId;
  final int groupId;
  final String groupName;

  const ItemListScreen({
    super.key,
    required this.categoryId,
    required this.groupId,
    required this.groupName,
  });

  @override
  ConsumerState<ItemListScreen> createState() => _ItemListScreenState();
}

class _ItemListScreenState extends ConsumerState<ItemListScreen> {
  // Track which item is currently being animated
  int? _animatingItemId;

  @override
  Widget build(BuildContext context) {
    final catalogService = ref.watch(catalogServiceProvider);

    return Scaffold(
      appBar: AppBar(
        title: Text(widget.groupName),
        backgroundColor: colorForIndex(widget.categoryId),
      ),
      body: FutureBuilder<List<Item>>(
        future: catalogService.getItems(widget.categoryId, widget.groupId),
        builder: (context, snapshot) {
          // Loading state
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const Center(
              child: CircularProgressIndicator(),
            );
          }

          // Error state
          if (snapshot.hasError) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(
                    Icons.error_outline,
                    size: 64,
                    color: Colors.red,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Error loading items',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    snapshot.error.toString(),
                    style: Theme.of(context).textTheme.bodyMedium,
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 16),
                  ElevatedButton.icon(
                    onPressed: () {
                      // Force rebuild to retry
                      setState(() {});
                    },
                    icon: const Icon(Icons.refresh),
                    label: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          // Empty state
          final items = snapshot.data ?? [];
          if (items.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(
                    Icons.inventory_2_outlined,
                    size: 64,
                    color: Colors.grey,
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'No items found',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ],
              ),
            );
          }

          // Success state - display grid
          return ItemGrid(
            items: items,
            animatingItemId: _animatingItemId,
            onItemTap: _handleItemTap,
            onItemLongPress: _handleItemLongPress,
          );
        },
      ),
    );
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

  /// Handle item long-press - navigate to item detail
  void _handleItemLongPress(Item item) {
    // Navigate to item detail screen using go_router
    context.go(Routes.itemDetail(item.itemId));
  }
}

/// Grid widget for displaying items
class ItemGrid extends ConsumerWidget {
  final List<Item> items;
  final int? animatingItemId;
  final Future<void> Function(Item) onItemTap;
  final void Function(Item) onItemLongPress;

  const ItemGrid({
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
        return ItemCard(
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

/// Individual card widget for an item with quantity badge
class ItemCard extends ConsumerWidget {
  final Item item;
  final ImageHandler imageHandler;
  final bool isAnimating;
  final VoidCallback onTap;
  final VoidCallback onLongPress;

  const ItemCard({
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
                            child: QuantityBadge(quantity: quantity),
                          ),
                      ],
                    ),
                  ),
                  // Item name section
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
                            maxLines: 2,
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
class QuantityBadge extends StatelessWidget {
  final int quantity;

  const QuantityBadge({
    super.key,
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
