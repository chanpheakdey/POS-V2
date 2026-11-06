/// Route path constants for the application.
/// 
/// Centralizes all route definitions to avoid magic strings throughout the codebase.
/// 
/// **Validates: Requirements 1.9, 2.3, 2.4**
abstract class Routes {
  // Authentication
  static const String login = '/login';
  
  // Main navigation
  static const String home = '/home';
  static const String cart = '/cart';
  static const String orders = '/orders';
  static const String search = '/search';
  
  // Category navigation (parameterized routes)
  static const String categoryRoute = '/category/:id';
  static const String groupRoute = '/group/:categoryId/:id';
  static const String itemsRoute = '/items/:categoryId/:groupId';
  static const String itemDetailRoute = '/item-detail/:id';
  static const String orderDetailRoute = '/order-detail/:id';
  
  // Helper methods to build parameterized routes
  static String category(int categoryId) => '/category/$categoryId';
  static String group(int categoryId, int groupId) => '/group/$categoryId/$groupId';
  static String items(int categoryId, int groupId) => '/items/$categoryId/$groupId';
  static String itemDetail(int itemId) => '/item-detail/$itemId';
  static String orderDetail(int orderId) => '/order-detail/$orderId';
}
