import 'dart:async';
import 'package:riverpod_annotation/riverpod_annotation.dart';
import '../api/api.dart';
import '../shared/local_file_store.dart';
import '../shared/models/models.dart';

part 'catalog_service.g.dart';

/// Service for managing product catalog data with offline-first read strategy.
/// 
/// Read strategy:
/// 1. Check LocalFileStore first
/// 2. If non-empty, return immediately
/// 3. Fetch from ApiClient in background and append new records
/// 4. On first launch, copy pre-bundled asset files
/// 
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5,
/// 5.2, 5.3, 5.4, 10.1, 10.5**
abstract class CatalogService {
  /// Get all categories with offline-first strategy
  Future<List<Category>> getCategories();

  /// Get groups for a specific category with offline-first strategy
  Future<List<Group>> getGroups(int categoryId);

  /// Get items for a specific category and group with offline-first strategy
  Future<List<Item>> getItems(int categoryId, int groupId);

  /// Get detailed information for a specific item with offline-first strategy
  Future<ItemDetail> getItemDetail(int itemId);

  /// Get price tiers for a specific item with offline-first strategy
  Future<List<PriceTier>> getPriceTiers(int itemId);
}

/// Implementation of CatalogService with offline-first read strategy
class CatalogServiceImpl implements CatalogService {
  final LocalFileStore _localFileStore;
  final ApiClient _apiClient;

  // File paths for local storage
  static const String _categoriesPath = 'catalog/categories.json';
  static const String _groupsPathPrefix = 'catalog/groups/';
  static const String _itemsPathPrefix = 'catalog/items/';
  static const String _itemDetailsPathPrefix = 'catalog/item_details/';
  static const String _priceTiersPathPrefix = 'catalog/price_tiers/';

  // Asset paths for pre-bundled data
  static const String _categoriesAssetPath = 'assets/catalog/categories.json';
  static const String _groupsAssetPathPrefix = 'assets/catalog/groups/';
  static const String _itemsAssetPathPrefix = 'assets/catalog/items/';
  static const String _itemDetailsAssetPathPrefix =
      'assets/catalog/item_details/';
  static const String _priceTiersAssetPathPrefix =
      'assets/catalog/price_tiers/';

  CatalogServiceImpl({
    required LocalFileStore localFileStore,
    required ApiClient apiClient,
  })  : _localFileStore = localFileStore,
        _apiClient = apiClient;

  @override
  Future<List<Category>> getCategories() async {
    // Copy pre-bundled asset if absent
    await _localFileStore.copyAssetIfAbsent(
      _categoriesAssetPath,
      _categoriesPath,
    );

    // Read from local storage first
    final localCategories = await _localFileStore.readList<Category>(
      _categoriesPath,
      (json) => Category.fromJson(json),
    );

    // If we have local data, return it immediately
    if (localCategories.isNotEmpty) {
      // Fetch from API in background and append new records
      _fetchAndAppendCategories();
      return localCategories;
    }

    // No local data - fetch from API synchronously
    return await _fetchAndAppendCategories();
  }

  Future<List<Category>> _fetchAndAppendCategories() async {
    try {
      // Fetch from API
      final categoryDtos = await _apiClient.getCategories();

      // Convert DTOs to domain models
      final categories = categoryDtos
          .asMap()
          .entries
          .map((entry) => _categoryDtoToModel(entry.value, entry.key))
          .toList();

      // Append new categories to local storage
      await _localFileStore.appendItems<Category>(
        _categoriesPath,
        categories,
        (category) => category.categoryId.toString(),
        (json) => Category.fromJson(json),
        (category) => category.toJson(),
      );

      // Read back the complete list (existing + new)
      return await _localFileStore.readList<Category>(
        _categoriesPath,
        (json) => Category.fromJson(json),
      );
    } catch (e) {
      // On error, return whatever we have locally (might be empty)
      return await _localFileStore.readList<Category>(
        _categoriesPath,
        (json) => Category.fromJson(json),
      );
    }
  }

  @override
  Future<List<Group>> getGroups(int categoryId) async {
    final groupsPath = '$_groupsPathPrefix$categoryId.json';
    final groupsAssetPath = '$_groupsAssetPathPrefix$categoryId.json';

    // Copy pre-bundled asset if absent
    await _localFileStore.copyAssetIfAbsent(
      groupsAssetPath,
      groupsPath,
    );

    // Read from local storage first
    final localGroups = await _localFileStore.readList<Group>(
      groupsPath,
      (json) => Group.fromJson(json),
    );

    // If we have local data, return it immediately
    if (localGroups.isNotEmpty) {
      // Fetch from API in background and append new records
      _fetchAndAppendGroups(categoryId);
      return localGroups;
    }

    // No local data - fetch from API synchronously
    return await _fetchAndAppendGroups(categoryId);
  }

  Future<List<Group>> _fetchAndAppendGroups(int categoryId) async {
    final groupsPath = '$_groupsPathPrefix$categoryId.json';

    try {
      // Fetch from API
      final groupDtos = await _apiClient.getGroups(categoryId);

      // Convert DTOs to domain models
      final groups = groupDtos.map(_groupDtoToModel).toList();

      // Append new groups to local storage
      await _localFileStore.appendItems<Group>(
        groupsPath,
        groups,
        (group) => group.groupId.toString(),
        (json) => Group.fromJson(json),
        (group) => group.toJson(),
      );

      // Read back the complete list (existing + new)
      return await _localFileStore.readList<Group>(
        groupsPath,
        (json) => Group.fromJson(json),
      );
    } catch (e) {
      // On error, return whatever we have locally (might be empty)
      return await _localFileStore.readList<Group>(
        groupsPath,
        (json) => Group.fromJson(json),
      );
    }
  }

  @override
  Future<List<Item>> getItems(int categoryId, int groupId) async {
    final itemsPath = '$_itemsPathPrefix${categoryId}_$groupId.json';
    final itemsAssetPath = '$_itemsAssetPathPrefix${categoryId}_$groupId.json';

    // Copy pre-bundled asset if absent
    await _localFileStore.copyAssetIfAbsent(
      itemsAssetPath,
      itemsPath,
    );

    // Read from local storage first
    final localItems = await _localFileStore.readList<Item>(
      itemsPath,
      (json) => Item.fromJson(json),
    );

    // If we have local data, return it immediately
    if (localItems.isNotEmpty) {
      // Fetch from API in background and append new records
      _fetchAndAppendItems(categoryId, groupId);
      return localItems;
    }

    // No local data - fetch from API synchronously
    return await _fetchAndAppendItems(categoryId, groupId);
  }

  Future<List<Item>> _fetchAndAppendItems(int categoryId, int groupId) async {
    final itemsPath = '$_itemsPathPrefix${categoryId}_$groupId.json';

    try {
      // Fetch from API
      final itemDtos = await _apiClient.getSaleItems(categoryId, groupId);

      // Convert DTOs to domain models
      final items = itemDtos.map(_itemDtoToModel).toList();

      // Append new items to local storage
      await _localFileStore.appendItems<Item>(
        itemsPath,
        items,
        (item) => item.itemId.toString(),
        (json) => Item.fromJson(json),
        (item) => item.toJson(),
      );

      // Read back the complete list (existing + new)
      return await _localFileStore.readList<Item>(
        itemsPath,
        (json) => Item.fromJson(json),
      );
    } catch (e) {
      // On error, return whatever we have locally (might be empty)
      return await _localFileStore.readList<Item>(
        itemsPath,
        (json) => Item.fromJson(json),
      );
    }
  }

  @override
  Future<ItemDetail> getItemDetail(int itemId) async {
    final itemDetailPath = '$_itemDetailsPathPrefix$itemId.json';
    final itemDetailAssetPath = '$_itemDetailsAssetPathPrefix$itemId.json';

    // Copy pre-bundled asset if absent
    await _localFileStore.copyAssetIfAbsent(
      itemDetailAssetPath,
      itemDetailPath,
    );

    // Read from local storage first
    final localDetails = await _localFileStore.readList<ItemDetail>(
      itemDetailPath,
      (json) => ItemDetail.fromJson(json),
    );

    // If we have local data, return it immediately
    if (localDetails.isNotEmpty) {
      // Fetch from API in background to refresh
      _fetchAndStoreItemDetail(itemId);
      return localDetails.first;
    }

    // No local data - fetch from API synchronously
    final itemDetail = await _fetchAndStoreItemDetail(itemId);
    if (itemDetail != null) {
      return itemDetail;
    }

    // Fallback: create a minimal ItemDetail if all else fails
    throw Exception('Failed to load item detail for itemId: $itemId');
  }

  Future<ItemDetail?> _fetchAndStoreItemDetail(int itemId) async {
    final itemDetailPath = '$_itemDetailsPathPrefix$itemId.json';

    try {
      // Fetch price tiers first
      final priceTiers = await getPriceTiers(itemId);

      // For now, we need to construct ItemDetail from available data
      // In a real implementation, we might need to fetch additional item info
      // from the items list or another endpoint
      
      // Try to find the item in any of the local items files to get full details
      ItemDetail? itemDetail;
      
      // Fetch from API getItemDetail endpoint
      final itemDetailDto = await _apiClient.getItemDetail(itemId);
      
      itemDetail = ItemDetail(
        itemId: itemId,
        itemName: itemDetailDto.itemName,
        itemCode: itemDetailDto.itemCode,
        imageUrl: itemDetailDto.imageUrl,
        priceTiers: priceTiers,
      );

      // Store as a single-item list
      await _localFileStore.writeList<ItemDetail>(
        itemDetailPath,
        [itemDetail],
        (detail) => detail.toJson(),
      );

      return itemDetail;
    } catch (e) {
      // On error, return null
      return null;
    }
  }

  @override
  Future<List<PriceTier>> getPriceTiers(int itemId) async {
    final priceTiersPath = '$_priceTiersPathPrefix$itemId.json';
    final priceTiersAssetPath = '$_priceTiersAssetPathPrefix$itemId.json';

    // Copy pre-bundled asset if absent
    await _localFileStore.copyAssetIfAbsent(
      priceTiersAssetPath,
      priceTiersPath,
    );

    // Read from local storage first
    final localTiers = await _localFileStore.readList<PriceTier>(
      priceTiersPath,
      (json) => PriceTier.fromJson(json),
    );

    // If we have local data, return it immediately
    if (localTiers.isNotEmpty) {
      // Fetch from API in background and append new records
      _fetchAndAppendPriceTiers(itemId);
      return localTiers;
    }

    // No local data - fetch from API synchronously
    return await _fetchAndAppendPriceTiers(itemId);
  }

  Future<List<PriceTier>> _fetchAndAppendPriceTiers(int itemId) async {
    final priceTiersPath = '$_priceTiersPathPrefix$itemId.json';

    try {
      // Fetch from API
      final priceTierDtos = await _apiClient.getItemUnits(itemId);

      // Convert DTOs to domain models
      final priceTiers = priceTierDtos.map(_priceTierDtoToModel).toList();

      // Append new price tiers to local storage
      await _localFileStore.appendItems<PriceTier>(
        priceTiersPath,
        priceTiers,
        (tier) => tier.saleId.toString(),
        (json) => PriceTier.fromJson(json),
        (tier) => tier.toJson(),
      );

      // Read back the complete list (existing + new)
      return await _localFileStore.readList<PriceTier>(
        priceTiersPath,
        (json) => PriceTier.fromJson(json),
      );
    } catch (e) {
      // On error, return whatever we have locally (might be empty)
      return await _localFileStore.readList<PriceTier>(
        priceTiersPath,
        (json) => PriceTier.fromJson(json),
      );
    }
  }

  // ==================== DTO to Domain Model Converters ====================

  // Default exchange rate: 1 USD = 4100 KHR (approximation)
  // In a production system, this would be fetched from the backend via GetExchangeRate endpoint
  static const double _defaultExchangeRate = 4100.0;

  Category _categoryDtoToModel(CategoryDto dto, int index) {
    return Category(
      categoryId: dto.categoryId,
      categoryName: dto.categoryName,
      imageUrl: '', // Image URL will be constructed via ImageHandler
      colorIndex: index % 21, // Cycle through 21 colors
    );
  }

  Group _groupDtoToModel(GroupDto dto) {
    return Group(
      groupId: dto.groupId,
      categoryId: dto.categoryId,
      groupName: dto.groupName,
      imageUrl: dto.imageUrl,
    );
  }

  Item _itemDtoToModel(ItemDto dto) {
    return Item(
      itemId: dto.itemId,
      categoryId: dto.categoryId,
      groupId: dto.groupId,
      itemName: dto.itemName,
      itemCode: dto.itemCode,
      imageUrl: dto.imageUrl,
      price: dto.price,
    );
  }

  PriceTier _priceTierDtoToModel(PriceTierDto dto) {
    // Convert the price to USD and KHR based on currency field
    double priceUsd;
    double priceKhr;
    
    if (dto.currency == '\$' || dto.currency.toUpperCase().contains('USD')) {
      // Price is in USD
      priceUsd = dto.price;
      priceKhr = dto.price * _defaultExchangeRate;
    } else {
      // Price is in KHR (Riel)
      priceKhr = dto.price;
      priceUsd = dto.price / _defaultExchangeRate;
    }
    
    return PriceTier(
      saleId: dto.saleId,
      itemId: dto.itemId,
      fromQty: dto.fromQty,
      toQty: dto.toQty,
      priceUsd: priceUsd,
      priceKhr: priceKhr,
    );
  }
}

/// Riverpod provider for CatalogService.
/// 
/// **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5,
/// 5.2, 5.3, 5.4, 10.1, 10.5**
@riverpod
CatalogService catalogService(CatalogServiceRef ref) {
  final localFileStore = ref.watch(localFileStoreProvider);
  final apiClient = ref.watch(apiClientProvider);

  return CatalogServiceImpl(
    localFileStore: localFileStore,
    apiClient: apiClient,
  );
}
