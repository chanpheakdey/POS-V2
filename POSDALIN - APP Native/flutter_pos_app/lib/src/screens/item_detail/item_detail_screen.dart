import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../shared/models/models.dart';
import '../../shared/image_handler.dart';
import '../../catalog/catalog_service.dart';
import '../../cart/cart_service.dart';
import '../../shared/constants.dart';

/// Screen displaying detailed information about an item with cart management.
/// 
/// Features:
/// - Display ItemName, ItemCode, full-resolution image (lazy-loaded)
/// - Show price tiers table (FromQty–ToQty, USD + KHR)
/// - Display current cart quantity
/// - Provide quick-add buttons (+1, +5, +10, +50)
/// - Price tier table updates displayed price as quantity changes
/// 
/// **Validates: Requirements 5.1, 5.5, 5.6, 5.7, 5.9, 5.10, 11.8, 12.3**
class ItemDetailScreen extends ConsumerStatefulWidget {
  final int itemId;

  const ItemDetailScreen({
    super.key,
    required this.itemId,
  });

  @override
  ConsumerState<ItemDetailScreen> createState() => _ItemDetailScreenState();
}

class _ItemDetailScreenState extends ConsumerState<ItemDetailScreen> {
  // Track local quantity state for price tier calculation
  int? _displayQuantity;

  @override
  Widget build(BuildContext context) {
    final catalogService = ref.watch(catalogServiceProvider);
    final cartAsync = ref.watch(cartProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Item Details'),
        actions: [
          // Cart icon in app bar for quick navigation
          IconButton(
            icon: const Icon(Icons.shopping_cart),
            onPressed: () {
              context.go('/cart');
            },
          ),
        ],
      ),
      body: FutureBuilder<ItemDetail>(
        future: catalogService.getItemDetail(widget.itemId),
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
                    'Error loading item details',
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
                      setState(() {});
                    },
                    icon: const Icon(Icons.refresh),
                    label: const Text('Retry'),
                  ),
                ],
              ),
            );
          }

          final itemDetail = snapshot.data!;
          
          // Get current cart quantity or use display quantity if set
          final cartService = cartAsync.valueOrNull;
          final cartQuantity = cartService?.quantityFor(widget.itemId) ?? 0;
          final displayQuantity = _displayQuantity ?? cartQuantity;

          // Determine which price tier is applicable for current quantity
          PriceTier? applicableTier;
          if (displayQuantity > 0 && itemDetail.priceTiers.isNotEmpty) {
            applicableTier = CartService.resolvePrice(
              displayQuantity,
              itemDetail.priceTiers,
            );
          }

          return SingleChildScrollView(
            padding: const EdgeInsets.all(16.0),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Item image (full resolution, lazy-loaded)
                _buildItemImage(context, itemDetail),
                
                const SizedBox(height: 24),
                
                // Item name
                Text(
                  itemDetail.itemName,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
                ),
                
                const SizedBox(height: 8),
                
                // Item code
                Text(
                  'Item Code: ${itemDetail.itemCode}',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Colors.grey[700],
                  ),
                ),
                
                const SizedBox(height: 24),
                
                // Current cart quantity
                _buildCartQuantitySection(context, cartQuantity, displayQuantity),
                
                const SizedBox(height: 24),
                
                // Quick add buttons
                _buildQuickAddButtons(context, itemDetail, cartService),
                
                const SizedBox(height: 24),
                
                // Price tiers table
                _buildPriceTiersSection(
                  context,
                  itemDetail.priceTiers,
                  applicableTier,
                  displayQuantity,
                ),
              ],
            ),
          );
        },
      ),
    );
  }

  /// Build the item image with lazy loading
  Widget _buildItemImage(BuildContext context, ItemDetail itemDetail) {
    final imageHandler = ref.watch(imageHandlerProvider);
    
    return Container(
      width: double.infinity,
      height: 300,
      decoration: BoxDecoration(
        color: Colors.grey[200],
        borderRadius: BorderRadius.circular(12),
      ),
      child: ClipRRect(
        borderRadius: BorderRadius.circular(12),
        child: imageHandler.buildImage(
          itemDetail.imageUrl,
          600, // Full resolution width
          400, // Full resolution height
          placeholder: const Center(
            child: Icon(
              Icons.image,
              size: 64,
              color: Colors.grey,
            ),
          ),
        ),
      ),
    );
  }

  /// Build the cart quantity section
  Widget _buildCartQuantitySection(
    BuildContext context,
    int cartQuantity,
    int displayQuantity,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Current Cart Quantity',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 4),
                Text(
                  '$cartQuantity items',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Theme.of(context).primaryColor,
                  ),
                ),
              ],
            ),
            if (displayQuantity != cartQuantity)
              Container(
                padding: const EdgeInsets.symmetric(
                  horizontal: 12,
                  vertical: 6,
                ),
                decoration: BoxDecoration(
                  color: Colors.orange[100],
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  'Preview: $displayQuantity',
                  style: TextStyle(
                    color: Colors.orange[900],
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  /// Build quick-add buttons section
  Widget _buildQuickAddButtons(
    BuildContext context,
    ItemDetail itemDetail,
    CartService? cartService,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Quick Add to Cart',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 12,
              runSpacing: 12,
              children: [
                _buildQuickAddButton(
                  context,
                  '+1',
                  1,
                  itemDetail,
                  cartService,
                  Colors.blue,
                ),
                _buildQuickAddButton(
                  context,
                  '+5',
                  5,
                  itemDetail,
                  cartService,
                  Colors.green,
                ),
                _buildQuickAddButton(
                  context,
                  '+10',
                  10,
                  itemDetail,
                  cartService,
                  Colors.orange,
                ),
                _buildQuickAddButton(
                  context,
                  '+50',
                  50,
                  itemDetail,
                  cartService,
                  Colors.purple,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  /// Build a single quick-add button
  Widget _buildQuickAddButton(
    BuildContext context,
    String label,
    int quantity,
    ItemDetail itemDetail,
    CartService? cartService,
    Color color,
  ) {
    return ElevatedButton(
      onPressed: cartService == null
          ? null
          : () async {
              final currentQuantity = cartService.quantityFor(widget.itemId);
              final newQuantity = currentQuantity + quantity;
              
              // Show preview of new quantity before adding
              setState(() {
                _displayQuantity = newQuantity;
              });
              
              // Add to cart
              if (quantity == 1) {
                // For +1, use addItem for single increment
                final item = Item(
                  itemId: itemDetail.itemId,
                  categoryId: 0, // Not needed for cart operation
                  groupId: 0, // Not needed for cart operation
                  itemName: itemDetail.itemName,
                  itemCode: itemDetail.itemCode,
                  imageUrl: itemDetail.imageUrl,
                  price: itemDetail.priceTiers.first.priceUsd,
                );
                await ref.read(cartProvider.notifier).addItem(
                  item,
                  itemDetail.priceTiers,
                );
              } else {
                // For bulk quantities, use setQuantity
                await ref.read(cartProvider.notifier).setQuantity(
                  widget.itemId,
                  newQuantity,
                  itemDetail.priceTiers,
                );
              }
              
              // Clear preview after a short delay
              Future.delayed(const Duration(milliseconds: 500), () {
                if (mounted) {
                  setState(() {
                    _displayQuantity = null;
                  });
                }
              });
              
              // Show success feedback
              if (mounted) {
                ScaffoldMessenger.of(context).showSnackBar(
                  SnackBar(
                    content: Text('Added $quantity item(s) to cart'),
                    duration: const Duration(seconds: 1),
                    backgroundColor: Colors.green,
                  ),
                );
              }
            },
      style: ElevatedButton.styleFrom(
        backgroundColor: color,
        foregroundColor: Colors.white,
        padding: const EdgeInsets.symmetric(
          horizontal: 20,
          vertical: 12,
        ),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(8),
        ),
      ),
      child: Text(
        label,
        style: const TextStyle(
          fontSize: 16,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }

  /// Build price tiers section with table
  Widget _buildPriceTiersSection(
    BuildContext context,
    List<PriceTier> priceTiers,
    PriceTier? applicableTier,
    int displayQuantity,
  ) {
    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Price Tiers',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            
            // Price tiers table
            _buildPriceTiersTable(
              context,
              priceTiers,
              applicableTier,
            ),
            
            // Show applicable price if quantity > 0
            if (displayQuantity > 0 && applicableTier != null) ...[
              const SizedBox(height: 16),
              const Divider(),
              const SizedBox(height: 16),
              _buildApplicablePriceDisplay(
                context,
                applicableTier,
                displayQuantity,
              ),
            ],
          ],
        ),
      ),
    );
  }

  /// Build the price tiers table
  Widget _buildPriceTiersTable(
    BuildContext context,
    List<PriceTier> priceTiers,
    PriceTier? applicableTier,
  ) {
    return Table(
      border: TableBorder.all(
        color: Colors.grey[300]!,
        width: 1,
      ),
      columnWidths: const {
        0: FlexColumnWidth(1.5),
        1: FlexColumnWidth(2),
        2: FlexColumnWidth(2),
      },
      children: [
        // Header row
        TableRow(
          decoration: BoxDecoration(
            color: Colors.grey[200],
          ),
          children: [
            _buildTableCell(
              context,
              'Quantity',
              isHeader: true,
            ),
            _buildTableCell(
              context,
              'USD Price',
              isHeader: true,
            ),
            _buildTableCell(
              context,
              'KHR Price',
              isHeader: true,
            ),
          ],
        ),
        
        // Data rows
        ...priceTiers.map((tier) {
          final isApplicable = tier == applicableTier;
          final quantityRange = tier.toQty == 0
              ? '${tier.fromQty}+'
              : '${tier.fromQty}-${tier.toQty}';
          
          return TableRow(
            decoration: BoxDecoration(
              color: isApplicable ? Colors.blue[50] : null,
            ),
            children: [
              _buildTableCell(
                context,
                quantityRange,
                isApplicable: isApplicable,
              ),
              _buildTableCell(
                context,
                '\$${tier.priceUsd.toStringAsFixed(2)}',
                isApplicable: isApplicable,
              ),
              _buildTableCell(
                context,
                '៛${tier.priceKhr.toStringAsFixed(0)}',
                isApplicable: isApplicable,
              ),
            ],
          );
        }).toList(),
      ],
    );
  }

  /// Build a single table cell
  Widget _buildTableCell(
    BuildContext context,
    String text, {
    bool isHeader = false,
    bool isApplicable = false,
  }) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Text(
        text,
        style: TextStyle(
          fontWeight: isHeader || isApplicable
              ? FontWeight.bold
              : FontWeight.normal,
          color: isApplicable
              ? Theme.of(context).primaryColor
              : null,
        ),
        textAlign: TextAlign.center,
      ),
    );
  }

  /// Build applicable price display
  Widget _buildApplicablePriceDisplay(
    BuildContext context,
    PriceTier applicableTier,
    int displayQuantity,
  ) {
    final totalUsd = applicableTier.priceUsd * displayQuantity;
    final totalKhr = applicableTier.priceKhr * displayQuantity;
    
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          'Price for $displayQuantity item(s)',
          style: Theme.of(context).textTheme.titleSmall?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Unit Price (USD):',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                Text(
                  '\$${applicableTier.priceUsd.toStringAsFixed(2)}',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Theme.of(context).primaryColor,
                  ),
                ),
              ],
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  'Unit Price (KHR):',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                Text(
                  '៛${applicableTier.priceKhr.toStringAsFixed(0)}',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Theme.of(context).primaryColor,
                  ),
                ),
              ],
            ),
          ],
        ),
        const SizedBox(height: 12),
        const Divider(),
        const SizedBox(height: 8),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Total (USD):',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                Text(
                  '\$${totalUsd.toStringAsFixed(2)}',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Colors.green[700],
                  ),
                ),
              ],
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  'Total (KHR):',
                  style: Theme.of(context).textTheme.bodyMedium,
                ),
                Text(
                  '៛${totalKhr.toStringAsFixed(0)}',
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: Colors.green[700],
                  ),
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }
}
