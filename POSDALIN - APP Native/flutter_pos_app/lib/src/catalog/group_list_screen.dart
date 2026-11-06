import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../shared/constants.dart';
import '../shared/models/models.dart';
import '../shared/image_handler.dart';
import '../shared/responsive_layout.dart';
import 'catalog_service.dart';

/// Screen displaying a scrollable grid of groups within a category.
/// 
/// Features:
/// - Color-coded cards with group name and image (300×200)
/// - Load via CatalogService.getGroups
/// - Tap → navigate to item list for that group
/// 
/// **Validates: Requirements 4.1, 4.7, 14.3**
class GroupListScreen extends ConsumerWidget {
  final int categoryId;
  final String? categoryName;

  const GroupListScreen({
    super.key,
    required this.categoryId,
    this.categoryName,
  });

  /// Fetch category name from catalog service if not provided
  Future<String> _getCategoryName(CatalogService catalogService) async {
    if (categoryName != null) {
      return categoryName!;
    }
    
    try {
      final categories = await catalogService.getCategories();
      final category = categories.firstWhere(
        (c) => c.categoryId == categoryId,
        orElse: () => throw Exception('Category not found'),
      );
      return category.categoryName;
    } catch (e) {
      return 'Category $categoryId';
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final catalogService = ref.watch(catalogServiceProvider);

    return FutureBuilder<String>(
      future: _getCategoryName(catalogService),
      builder: (context, categorySnapshot) {
        // If we have category name from parameter, use it directly
        final displayName = categoryName ?? 
            (categorySnapshot.hasData ? categorySnapshot.data! : 'Loading...');

        return Scaffold(
          appBar: AppBar(
            title: Text(displayName),
            backgroundColor: colorForIndex(categoryId),
          ),
          body: FutureBuilder<List<Group>>(
            future: catalogService.getGroups(categoryId),
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
                        'Error loading groups',
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
                          (context as Element).markNeedsBuild();
                        },
                        icon: const Icon(Icons.refresh),
                        label: const Text('Retry'),
                      ),
                    ],
                  ),
                );
              }

              // Empty state
              final groups = snapshot.data ?? [];
              if (groups.isEmpty) {
                return Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(
                        Icons.folder_open,
                        size: 64,
                        color: Colors.grey,
                      ),
                      const SizedBox(height: 16),
                      Text(
                        'No groups found',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ],
                  ),
                );
              }

              // Success state - display grid
              return GroupGrid(
                groups: groups,
                categoryId: categoryId,
              );
            },
          ),
        );
      },
    );
  }
}

/// Grid widget for displaying groups
class GroupGrid extends ConsumerWidget {
  final List<Group> groups;
  final int categoryId;

  const GroupGrid({
    super.key,
    required this.groups,
    required this.categoryId,
  });

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final imageHandler = ref.watch(imageHandlerProvider);

    return GridView.builder(
      padding: context.responsivePadding,
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: context.gridColumns,
        crossAxisSpacing: context.responsiveSpacing,
        mainAxisSpacing: context.responsiveSpacing,
        childAspectRatio: 300 / 200, // 300×200 dimensions
      ),
      itemCount: groups.length,
      itemBuilder: (context, index) {
        final group = groups[index];
        return GroupCard(
          group: group,
          colorIndex: index,
          imageHandler: imageHandler,
          onTap: () {
            // Navigate to item list for this group
            _navigateToItemList(context, group);
          },
        );
      },
    );
  }

  void _navigateToItemList(BuildContext context, Group group) {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (context) => ItemListScreen(
          categoryId: categoryId,
          groupId: group.groupId,
          groupName: group.groupName,
        ),
      ),
    );
  }
}

/// Individual card widget for a group
class GroupCard extends StatelessWidget {
  final Group group;
  final int colorIndex;
  final ImageHandler imageHandler;
  final VoidCallback onTap;

  const GroupCard({
    super.key,
    required this.group,
    required this.colorIndex,
    required this.imageHandler,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final backgroundColor = colorForIndex(colorIndex);
    final textScale = context.textScale;

    return Card(
      clipBehavior: Clip.antiAlias,
      elevation: 4,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
      ),
      child: InkWell(
        onTap: onTap,
        child: ResponsiveLayout.ensureTouchTarget(
          child: Container(
            color: backgroundColor,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Image section
                Expanded(
                  flex: 3,
                  child: imageHandler.buildImage(
                    group.imageUrl,
                    ImageDimensions.groupWidth,
                    ImageDimensions.groupHeight,
                    placeholder: Container(
                      color: backgroundColor.withOpacity(0.5),
                      child: const Icon(
                        Icons.image,
                        size: 48,
                        color: Colors.white70,
                      ),
                    ),
                  ),
                ),
                // Group name section
                Expanded(
                  flex: 1,
                  child: Container(
                    padding: const EdgeInsets.symmetric(
                      horizontal: 8,
                      vertical: 4,
                    ),
                    color: backgroundColor.withOpacity(0.9),
                    child: Center(
                      child: Text(
                        group.groupName,
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
