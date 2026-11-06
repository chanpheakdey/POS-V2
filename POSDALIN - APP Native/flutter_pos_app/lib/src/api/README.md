# API Client Module

This module provides HTTP communication with the ASMX web service backend.

## Overview

The `ApiClient` class implements all required endpoints for the Flutter POS application:

- **Authentication**: `checkToken`, `login`, `expireToken`
- **Catalog**: `getCategories`, `getGroups`, `getSaleItems`, `getItemDetail`, `getItemUnits`
- **Orders**: `submitOrder`, `getOrders`
- **Images**: `getImageUrl`

## Features

### 30-Second Timeout
All HTTP requests have a 30-second timeout. When timeout occurs, an `ApiTimeoutException` is thrown.

### Typed Exceptions
- `ApiException`: Base exception for all API errors
- `ApiTimeoutException`: Request timeout after 30 seconds
- `ApiNetworkException`: Network connectivity failure

### Token Management
The client supports attaching authentication tokens to requests:
```dart
final client = ApiClient(baseUrl: 'https://api.example.com');
client.setToken('your-token-here');
```

Tokens can be attached as:
- Query parameters in the URL
- Authorization header (Bearer token)

### HTML and JSON Response Parsing
The ASMX service returns mixed response formats:
- **JSON responses**: `getCategories`, `getItemUnits`, `submitOrder`, `getOrders`
- **HTML responses**: `getGroups`, `getSaleItems` (parsed using HTML parser)

The client automatically handles both formats.

## Usage

### Basic Setup
```dart
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pos_app/src/api/api.dart';

void main() {
  runApp(ProviderScope(
    overrides: [
      // Configure base URL
      baseUrlProvider.overrideWithValue('https://your-backend.com'),
    ],
    child: MyApp(),
  ));
}
```

### Using with Riverpod
```dart
class CatalogService {
  final ApiClient _apiClient;
  
  CatalogService(this._apiClient);
  
  Future<List<CategoryDto>> getCategories() async {
    try {
      return await _apiClient.getCategories();
    } on ApiTimeoutException catch (e) {
      // Handle timeout
      throw ServiceException('Request timed out');
    } on ApiNetworkException catch (e) {
      // Handle network error
      throw ServiceException('Network error: ${e.message}');
    } on ApiException catch (e) {
      // Handle general API error
      throw ServiceException(e.message);
    }
  }
}
```

### Direct Usage
```dart
final client = ApiClient(
  baseUrl: 'https://your-backend.com',
  token: 'optional-token',
);

// Check token validity
final status = await client.checkToken('token-to-check');
print(status); // "Valid" or "Invalid"

// Get categories
final categories = await client.getCategories();
for (final category in categories) {
  print('${category.categoryName} (ID: ${category.categoryId})');
}

// Get groups for a category
final groups = await client.getGroups(categoryId: 1);

// Get items for a category and group
final items = await client.getSaleItems(
  categoryId: 1,
  groupId: 2,
);

// Get price tiers for an item
final priceTiers = await client.getItemUnits(itemId: 123);

// Build image URL
final imageUrl = client.getImageUrl(
  'path/to/image.jpg',
  width: 300,
  height: 200,
);
```

## Configuration

### Base URL
Configure the base URL through the `baseUrlProvider`:

```dart
final baseUrlProvider = Provider<String>((ref) {
  return 'https://production-api.com';
});
```

### Token Provider
The auth token is managed through the `authTokenProvider`:

```dart
final authTokenProvider = Provider<String?>((ref) {
  // Read from secure storage
  final authState = ref.watch(authServiceProvider);
  return authState.token;
});
```

## Data Transfer Objects (DTOs)

All DTOs support JSON serialization:

### CategoryDto
```dart
{
  "CategoryID": 1,
  "CategoryName": "Electronics"
}
```

### GroupDto
```dart
{
  "GroupID": 1,
  "CategoryID": 1,
  "GroupName": "Phones",
  "ImageUrl": "images/phones.jpg"
}
```

### ItemDto
```dart
{
  "ItemID": 123,
  "CategoryID": 1,
  "GroupID": 1,
  "ItemName": "iPhone 15",
  "ItemCode": "IP15",
  "ImageUrl": "images/iphone15.jpg",
  "Price": 999.99
}
```

### PriceTierDto
```dart
{
  "ID": 1,
  "ItemID": 123,
  "FromQty": 1,
  "ToQty": 10,
  "Price": 999.99,
  "Currency": "$"
}
```

### OrderPayloadDto
```dart
{
  "CustomerID": 456,
  "Items": [
    {
      "ItemID": 123,
      "ItemName": "iPhone 15",
      "ItemCode": "IP15",
      "Quantity": 2,
      "UnitPrice": 999.99,
      "SaleID": 1
    }
  ],
  "Total": 1999.98
}
```

### OrderResultDto
```dart
{
  "OrderID": 789,
  "InvoiceNo": "INV-2024-001",
  "CreatedDate": "2024-01-15T10:30:00Z"
}
```

## Error Handling

All methods can throw:
- `ApiTimeoutException`: Request took longer than 30 seconds
- `ApiNetworkException`: Network connectivity issue
- `ApiException`: General API error (wrong credentials, server error, etc.)

Example error handling:
```dart
try {
  final result = await apiClient.submitOrder(payload);
  print('Order placed: ${result.invoiceNo}');
} on ApiTimeoutException catch (e) {
  showError('Request timed out. Please try again.');
} on ApiNetworkException catch (e) {
  showError('No internet connection. Please check your network.');
} on ApiException catch (e) {
  showError('Failed to place order: ${e.message}');
}
```

## HTML Response Parsing

The `getGroups` and `getSaleItems` endpoints return HTML strings that need parsing. The client handles this automatically:

1. Parses HTML using the `html` package
2. Extracts data from `onclick` attributes and DOM elements
3. Constructs typed DTOs from the extracted data

## Image URL Construction

The `getImageUrl` method constructs URLs for the image handler endpoint:

```dart
final url = client.getImageUrl(
  'images^products^item123.jpg', // Path with ^ separators
  width: 300,
  height: 200,
);
// Returns: https://api.com/hdImage_all.ashx?ImagePath=images/products/item123.jpg&width=300&height=200
```

## Testing

Mock the ApiClient in tests:

```dart
class MockApiClient extends Mock implements ApiClient {}

void main() {
  test('should fetch categories', () async {
    final mockClient = MockApiClient();
    when(() => mockClient.getCategories()).thenAnswer(
      (_) async => [
        CategoryDto(categoryId: 1, categoryName: 'Electronics'),
      ],
    );
    
    final categories = await mockClient.getCategories();
    expect(categories, hasLength(1));
    expect(categories[0].categoryName, 'Electronics');
  });
}
```

## Requirements Validation

This implementation validates:
- **Requirement 1.2**: Authentication service communication for login and token validation
- **Requirement 2.5**: Token included in authenticated requests
- **Requirement 13.5**: 30-second timeout on all requests with typed exceptions

## Next Steps

1. Configure the actual backend URL in `baseUrlProvider`
2. Implement the auth service to manage tokens
3. Wire up the catalog service to use these endpoints
4. Add integration tests with real backend
