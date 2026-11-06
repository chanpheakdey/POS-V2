import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../shared/models/cart_item.dart';
import '../shared/models/item.dart';
import '../shared/models/price_tier.dart';
import '../shared/local_file_store.dart';

part 'cart_service.g.dart';

/// Service for managing the shopping cart state.
/// 
/// Handles adding items, quantity management, price tier resolution,
/// and persistence to local storage.
/// 
/// **Validates: Requirements 6.1, 6.2, 6.5, 6.6, 6.7, 6.8, 6.9, 6.10, 19.4, 19.5**
abstract class CartService {
  /// Get all items currently in the cart
  List<CartItem> get items;

  /// Calculate and return the grand total in USD
  double get total;

  /// Add one unit of an item to the cart.
  /// 
  /// If the item is already in the cart, increment quantity by 1.
  /// Updates unit price based on applicable price tier.
  /// 
  /// **Validates: Requirements 6.1, 6.2**
  Future<void> addItem(Item item, List<PriceTier> priceTiers);

  /// Set a specific quantity for an item.
  /// 
  /// If quantity is 0, removes the item from cart.
  /// Updates unit price based on applicable price tier.
  /// 
  /// **Validates: Requirements 6.6, 6.10**
  Future<void> setQuantity(int itemId, int quantity, List<PriceTier> priceTiers);

  /// Remove an item from the cart completely.
  /// 
  /// **Validates: Requirements 6.7, 19.6**
  Future<void> removeItem(int itemId);

  /// Empty the entire cart.
  /// 
  /// **Validates: Requirements 19.5**
  Future<void> clear();

  /// Get the quantity of a specific item in the cart.
  /// Returns 0 if the item is not in the cart.
  /// 
  /// **Validates: Requirements 6.3, 6.4**
  int quantityFor(int itemId);

  /// Pure function to find the applicable price tier based on quantity.
  /// 
  /// Finds tier where `fromQty ≤ quantity ≤ toQty` (toQty=0 means unlimited).
  /// Returns the tier with the highest matching `fromQty`.
  /// 
  /// **Property 1: Price tier selection is deterministic and correct**
  /// **Validates: Requirements 6.6, 6.10**
  static PriceTier resolvePrice(int quantity, List<PriceTier> tiers) {
    if (tiers.isEmpty) {
      throw ArgumentError('Price tier list cannot be empty');
    }
    if (quantity <= 0) {
      throw ArgumentError('Quantity must be positive');
    }

    // Filter tiers where quantity is within range
    final matchingTiers = tiers.where((tier) {
      final isAboveMin = quantity >= tier.fromQty;
      final isBelowMax = tier.toQty == 0 || quantity <= tier.toQty;
      return isAboveMin && isBelowMax;
    }).toList();

    if (matchingTiers.isEmpty) {
      // Fallback: use the first tier (lowest quantity tier)
      return tiers.first;
    }

    // Return tier with highest fromQty among matching tiers
    matchingTiers.sort((a, b) => b.fromQty.compareTo(a.fromQty));
    return matchingTiers.first;
  }
}

/// Implementation of CartService with local file persistence.
class CartServiceImpl extends CartService {
  final LocalFileStore _fileStore;
  final List<CartItem> _items = [];
  
  static const String _cartFilePath = 'cart.json';

  CartServiceImpl(this._fileStore);

  /// Initialize cart by loading from local storage
  Future<void> init() async {
    final loadedItems = await _fileStore.readList<CartItem>(
      _cartFilePath,
      CartItem.fromJson,
    );
    _items.clear();
    _items.addAll(loadedItems);
  }

  /// Persist cart contents to local storage
  Future<void> _persist() async {
    await _fileStore.writeList<CartItem>(
      _cartFilePath,
      _items,
      (item) => item.toJson(),
    );
  }

  @override
  List<CartItem> get items => List.unmodifiable(_items);

  @override
  double get total {
    return _items.fold(0.0, (sum, item) => sum + item.lineTotalUsd);
  }

  @override
  Future<void> addItem(Item item, List<PriceTier> priceTiers) async {
    final existingIndex = _items.indexWhere((ci) => ci.itemId == item.itemId);

    if (existingIndex >= 0) {
      // Item already in cart - increment quantity
      final existingItem = _items[existingIndex];
      final newQuantity = existingItem.quantity + 1;
      
      // Resolve new price tier
      final tier = CartService.resolvePrice(newQuantity, priceTiers);
      
      // Update the item
      _items[existingIndex] = existingItem.copyWith(
        quantity: newQuantity,
        unitPriceUsd: tier.priceUsd,
        unitPriceKhr: tier.priceKhr,
        appliedSaleId: tier.saleId,
      );
    } else {
      // New item - add to cart with quantity 1
      final tier = CartService.resolvePrice(1, priceTiers);
      
      _items.add(CartItem(
        itemId: item.itemId,
        itemName: item.itemName,
        itemCode: item.itemCode,
        quantity: 1,
        unitPriceUsd: tier.priceUsd,
        unitPriceKhr: tier.priceKhr,
        appliedSaleId: tier.saleId,
      ));
    }

    await _persist();
  }

  @override
  Future<void> setQuantity(int itemId, int quantity, List<PriceTier> priceTiers) async {
    if (quantity <= 0) {
      // Remove item if quantity is zero or negative
      await removeItem(itemId);
      return;
    }

    final existingIndex = _items.indexWhere((ci) => ci.itemId == itemId);
    
    if (existingIndex < 0) {
      // Item not in cart - nothing to do
      return;
    }

    final existingItem = _items[existingIndex];
    
    // Resolve new price tier
    final tier = CartService.resolvePrice(quantity, priceTiers);
    
    // Update the item
    _items[existingIndex] = existingItem.copyWith(
      quantity: quantity,
      unitPriceUsd: tier.priceUsd,
      unitPriceKhr: tier.priceKhr,
      appliedSaleId: tier.saleId,
    );

    await _persist();
  }

  @override
  Future<void> removeItem(int itemId) async {
    _items.removeWhere((item) => item.itemId == itemId);
    await _persist();
  }

  @override
  Future<void> clear() async {
    _items.clear();
    await _persist();
  }

  @override
  int quantityFor(int itemId) {
    final item = _items.firstWhere(
      (ci) => ci.itemId == itemId,
      orElse: () => CartItem(
        itemId: -1,
        itemName: '',
        itemCode: '',
        quantity: 0,
        unitPriceUsd: 0.0,
        unitPriceKhr: 0.0,
        appliedSaleId: 0,
      ),
    );
    return item.quantity;
  }
}

/// Riverpod notifier for CartService.
/// 
/// Loads cart from storage on initialization and provides reactive state updates.
/// 
/// **Validates: Requirements 6.9, 19.2**
@riverpod
class Cart extends _$Cart {
  @override
  Future<CartService> build() async {
    final fileStore = ref.watch(localFileStoreProvider);
    final service = CartServiceImpl(fileStore);
    await service.init();
    return service;
  }

  /// Get all items in the cart (convenience accessor)
  List<CartItem> get items {
    final service = state.valueOrNull;
    return service?.items ?? [];
  }

  /// Get cart total (convenience accessor)
  double get total {
    final service = state.valueOrNull;
    return service?.total ?? 0.0;
  }

  /// Add one unit of an item to the cart
  Future<void> addItem(Item item, List<PriceTier> priceTiers) async {
    final service = state.valueOrNull;
    if (service == null) return;
    
    await service.addItem(item, priceTiers);
    // Notify listeners of state change
    ref.invalidateSelf();
  }

  /// Set specific quantity for an item
  Future<void> setQuantity(int itemId, int quantity, List<PriceTier> priceTiers) async {
    final service = state.valueOrNull;
    if (service == null) return;
    
    await service.setQuantity(itemId, quantity, priceTiers);
    // Notify listeners of state change
    ref.invalidateSelf();
  }

  /// Remove an item from the cart
  Future<void> removeItem(int itemId) async {
    final service = state.valueOrNull;
    if (service == null) return;
    
    await service.removeItem(itemId);
    // Notify listeners of state change
    ref.invalidateSelf();
  }

  /// Clear the entire cart
  Future<void> clear() async {
    final service = state.valueOrNull;
    if (service == null) return;
    
    await service.clear();
    // Notify listeners of state change
    ref.invalidateSelf();
  }

  /// Get quantity of a specific item
  int quantityFor(int itemId) {
    final service = state.valueOrNull;
    return service?.quantityFor(itemId) ?? 0;
  }
}
