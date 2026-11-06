import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../catalog/widgets/category_grid.dart';
import '../../routing/routes.dart';
import '../../shared/models/models.dart';
import '../../shared/responsive_layout.dart';

/// Home screen that displays the category grid.
/// 
/// This is the landing screen after successful authentication where customers
/// browse product categories. Tapping a category navigates to its groups or items.
/// 
/// Features:
/// - Displays all categories in a scrollable grid
/// - Color-coded category cards with names and images
/// - Navigation to category details on tap
/// - Loading, error, and empty states handled by CategoryGrid
/// - Search and cart access from app bar
/// 
/// **Validates: Requirements 3.2, 3.6, 3.7, 3.8, 3.9, 14.3, 16.4**
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Categories'),
        actions: [
          // Search icon button with proper touch target
          ResponsiveLayout.touchTargetIconButton(
            icon: const Icon(Icons.search),
            tooltip: 'Search Products',
            onPressed: () {
              context.push(Routes.search);
            },
          ),
          // Cart icon button with proper touch target
          ResponsiveLayout.touchTargetIconButton(
            icon: const Icon(Icons.shopping_cart),
            tooltip: 'View Cart',
            onPressed: () {
              context.push(Routes.cart);
            },
          ),
        ],
      ),
      body: CategoryGrid(
        onCategoryTap: (category) {
          // Navigate to category screen which will determine
          // whether to show groups or items directly
          context.push(Routes.category(category.categoryId));
        },
      ),
    );
  }
}
