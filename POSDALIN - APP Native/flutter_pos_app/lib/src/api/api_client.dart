import 'dart:async';
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:html/parser.dart' as html_parser;
import 'api_exception.dart';
import 'dtos/category_dto.dart';
import 'dtos/group_dto.dart';
import 'dtos/item_dto.dart';
import 'dtos/item_detail_dto.dart';
import 'dtos/price_tier_dto.dart';
import 'dtos/order_dto.dart';
import 'dtos/order_payload_dto.dart';
import 'dtos/order_result_dto.dart';

/// HTTP client for communicating with ASMX web service backend
class ApiClient {
  final String baseUrl;
  final http.Client _httpClient;
  final Duration _timeout = const Duration(seconds: 30);

  /// Optional token to attach to authenticated requests
  String? _token;

  ApiClient({
    required this.baseUrl,
    http.Client? httpClient,
    String? token,
  })  : _httpClient = httpClient ?? http.Client(),
        _token = token;

  /// Set authentication token for subsequent requests
  void setToken(String? token) {
    _token = token;
  }

  /// Get current authentication token
  String? get token => _token;

  /// Check if a token is valid
  Future<String> checkToken(String token) async {
    const endpoint = 'appservice.asmx/CheckToken';
    try {
      final uri = Uri.parse('$baseUrl/$endpoint');
      final response = await _httpClient.post(
        uri,
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'Token': token}),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        // Response is XML/JSON wrapped string
        final result = _extractResponseValue(response.body);
        return result; // "Valid" or "Invalid"
      } else {
        throw ApiException(
          message: 'Token validation failed',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Login with username and password
  /// Returns token on success, throws ApiException on failure
  /// 
  /// **Validates: Requirements 1.2, 1.3, 1.4, 1.5, 1.6**
  Future<String> login(String username, String password) async {
    const endpoint = 'APPLogin.aspx';
    try {
      // First, make the GET request to get the page (needed for ASP.NET ViewState)
      final getUri = Uri.parse('$baseUrl/$endpoint?app=1');
      final getResponse = await _httpClient.get(getUri).timeout(_timeout);
      
      if (getResponse.statusCode != 200) {
        throw ApiException(
          message: 'Failed to load login page',
          statusCode: getResponse.statusCode,
          endpoint: endpoint,
        );
      }
      
      // Parse ViewState and other required fields from the page
      final doc = html_parser.parse(getResponse.body);
      final viewState = doc.querySelector('input[name="__VIEWSTATE"]')?.attributes['value'] ?? '';
      final viewStateGenerator = doc.querySelector('input[name="__VIEWSTATEGENERATOR"]')?.attributes['value'] ?? '';
      final eventValidation = doc.querySelector('input[name="__EVENTVALIDATION"]')?.attributes['value'] ?? '';
      
      // Now make the POST request with credentials
      final postUri = Uri.parse('$baseUrl/$endpoint?app=1');
      final postResponse = await _httpClient.post(
        postUri,
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
          'Cookie': getResponse.headers['set-cookie'] ?? '',
        },
        body: {
          '__VIEWSTATE': viewState,
          '__VIEWSTATEGENERATOR': viewStateGenerator,
          '__EVENTVALIDATION': eventValidation,
          'txtUserName': username,
          'txtPassword': password,
          'btnSignIn': 'Sign In',
        },
      ).timeout(_timeout);

      // Check for error messages in the response
      if (postResponse.statusCode == 200) {
        final responseDoc = html_parser.parse(postResponse.body);
        
        // Check for error labels
        final errAllLabel = responseDoc.querySelector('#lblErrAll');
        final infoLabel = responseDoc.querySelector('#lblInfo');
        
        if (errAllLabel != null && errAllLabel.text.trim().isNotEmpty) {
          final errorMsg = errAllLabel.text.trim();
          throw ApiException(
            message: errorMsg,
            statusCode: 200,
            endpoint: endpoint,
          );
        }
        
        if (infoLabel != null && infoLabel.text.contains('locked')) {
          throw ApiException(
            message: 'Your user account has been locked! Please contact IT',
            statusCode: 200,
            endpoint: endpoint,
          );
        }
        
        if (infoLabel != null && infoLabel.text.contains('Failed')) {
          throw ApiException(
            message: 'Login Failed!',
            statusCode: 200,
            endpoint: endpoint,
          );
        }
        
        // Extract token from cookies
        final token = _extractTokenFromResponse(postResponse);
        
        if (token.isEmpty) {
          throw ApiException(
            message: 'Failed to obtain authentication token',
            statusCode: 200,
            endpoint: endpoint,
          );
        }
        
        return token;
      } else {
        throw ApiException(
          message: 'Login failed',
          statusCode: postResponse.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get all categories
  Future<List<CategoryDto>> getCategories() async {
    const endpoint = 'UserOrderV2.aspx/getcategory';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({}),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body);
        // ASMX returns: {"d": [...]}
        final List<dynamic> data = jsonData['d'] as List<dynamic>;
        return data.map((item) => CategoryDto.fromJson(item as Map<String, dynamic>)).toList();
      } else {
        throw ApiException(
          message: 'Failed to fetch categories',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get groups for a category
  /// Returns HTML string that needs to be parsed
  Future<List<GroupDto>> getGroups(int categoryId) async {
    const endpoint = 'appservice.asmx/getGroup';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({
          'CategoryID': categoryId,
          'UserID': 0, // Default user ID for customer
        }),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final htmlString = _extractResponseValue(response.body);
        return _parseGroupsFromHtml(htmlString, categoryId);
      } else {
        throw ApiException(
          message: 'Failed to fetch groups',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get sale items for a category and group
  Future<List<ItemDto>> getSaleItems(int categoryId, int groupId) async {
    const endpoint = 'appservice.asmx/getSaleItem';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({
          'CategoryID': categoryId,
          'GroupID': groupId,
          'UserID': 0,
        }),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final htmlString = _extractResponseValue(response.body);
        return _parseItemsFromHtml(htmlString, categoryId, groupId);
      } else {
        throw ApiException(
          message: 'Failed to fetch sale items',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get item detail
  Future<ItemDetailDto> getItemDetail(int itemId) async {
    // First get item units (price tiers)
    final priceTiers = await getItemUnits(itemId);
    
    // For full item details, we need additional info
    // This is a simplified implementation - actual implementation may vary
    // based on available endpoints
    return ItemDetailDto(
      itemId: itemId,
      itemName: '', // Would be fetched from actual endpoint
      itemCode: '',
      imageUrl: '',
      priceTiers: priceTiers,
    );
  }

  /// Get item units (price tiers)
  Future<List<PriceTierDto>> getItemUnits(int itemId) async {
    const endpoint = 'UserOrderV2.aspx/getitemunit';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({'ItemID': itemId}),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body);
        final List<dynamic> data = jsonData['d'] as List<dynamic>;
        return data.map((item) => PriceTierDto.fromJson(item as Map<String, dynamic>)).toList();
      } else {
        throw ApiException(
          message: 'Failed to fetch item units',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Submit an order
  Future<OrderResultDto> submitOrder(OrderPayloadDto payload) async {
    const endpoint = 'UserOrderV2.aspx/AddOrder';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({
          'UserID': payload.customerId,
          'JsonString': jsonEncode(payload.toJson()),
        }),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body);
        final result = jsonData['d'] as Map<String, dynamic>;
        return OrderResultDto.fromJson(result);
      } else {
        throw ApiException(
          message: 'Failed to submit order',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get order history for a customer
  Future<List<OrderDto>> getOrders(int customerId) async {
    const endpoint = 'UserOrderV2.aspx/LoadDetail';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({
          'UserID': customerId,
          'CancelList': false,
          'Page': 1,
          'RecordsPerPage': 100,
        }),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body);
        final List<dynamic> data = jsonData['d'] as List<dynamic>;
        return data.map((item) => OrderDto.fromJson(item as Map<String, dynamic>)).toList();
      } else {
        throw ApiException(
          message: 'Failed to fetch orders',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Get order details (line items) for a specific order
  Future<List<OrderDetailDto>> getOrderDetails(int orderId) async {
    const endpoint = 'UserOrderV2.aspx/LoadOrderDetail';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({
          'OrderID': orderId,
        }),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        final jsonData = jsonDecode(response.body);
        final List<dynamic> data = jsonData['d'] as List<dynamic>;
        return data.map((item) => OrderDetailDto.fromJson(item as Map<String, dynamic>)).toList();
      } else {
        throw ApiException(
          message: 'Failed to fetch order details',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Expire a token (logout)
  Future<String> expireToken(String token) async {
    const endpoint = 'appservice.asmx/removetoken';
    try {
      final uri = _buildUri(endpoint);
      final response = await _httpClient.post(
        uri,
        headers: _buildHeaders(),
        body: jsonEncode({}),
      ).timeout(_timeout);

      if (response.statusCode == 200) {
        return _extractResponseValue(response.body);
      } else {
        throw ApiException(
          message: 'Failed to expire token',
          statusCode: response.statusCode,
          endpoint: endpoint,
        );
      }
    } on TimeoutException {
      throw ApiTimeoutException(endpoint: endpoint);
    } catch (e) {
      if (e is ApiException) rethrow;
      throw ApiNetworkException(
        endpoint: endpoint,
        details: e.toString(),
      );
    }
  }

  /// Build URL for image retrieval with width/height parameters
  String getImageUrl(String imagePath, int width, int height) {
    // Clean up image path
    final cleanPath = imagePath.replaceAll('^', '/');
    return '$baseUrl/hdImage_all.ashx?ImagePath=$cleanPath&width=$width&height=$height';
  }

  // ==================== Private Helper Methods ====================

  Uri _buildUri(String endpoint) {
    final uri = Uri.parse('$baseUrl/$endpoint');
    if (_token != null) {
      return uri.replace(queryParameters: {'token': _token});
    }
    return uri;
  }

  Map<String, String> _buildHeaders() {
    return {
      'Content-Type': 'application/json',
      if (_token != null) 'Authorization': 'Bearer $_token',
    };
  }

  /// Extract the actual value from ASMX XML/JSON response
  /// ASMX services often wrap responses in XML or JSON with "d" property
  String _extractResponseValue(String responseBody) {
    try {
      // Try JSON format first: {"d": "value"}
      final jsonData = jsonDecode(responseBody);
      if (jsonData is Map && jsonData.containsKey('d')) {
        return jsonData['d'].toString();
      }
      return responseBody;
    } catch (e) {
      // Try XML format: <string>value</string>
      if (responseBody.contains('<string')) {
        final doc = html_parser.parse(responseBody);
        final stringElement = doc.querySelector('string');
        if (stringElement != null) {
          return stringElement.text;
        }
      }
      return responseBody;
    }
  }

  String _extractTokenFromResponse(http.Response response) {
    // Extract token from Set-Cookie header
    final cookies = response.headers['set-cookie'];
    if (cookies != null) {
      // The token cookie format is: token=<value>; expires=...; path=/
      final tokenMatch = RegExp(r'token=([^;]+)').firstMatch(cookies);
      if (tokenMatch != null) {
        final token = tokenMatch.group(1)!;
        // Decode URL-encoded token if necessary
        return Uri.decodeComponent(token);
      }
    }
    
    // If no token in Set-Cookie, it might be an error (login failed)
    return '';
  }

  /// Parse groups from HTML response
  List<GroupDto> _parseGroupsFromHtml(String html, int categoryId) {
    final document = html_parser.parse(html);
    final groups = <GroupDto>[];
    
    // Parse onclick attributes to extract group data
    final anchors = document.querySelectorAll('a[onclick*="viewSaleItem"]');
    
    int groupIdCounter = 1;
    for (final anchor in anchors) {
      final onclick = anchor.attributes['onclick'] ?? '';
      final match = RegExp(r'viewSaleItem\((\d+),(\d+),(\d+)\)').firstMatch(onclick);
      
      if (match != null) {
        final groupId = int.parse(match.group(2)!);
        final groupNameDiv = anchor.querySelector('div');
        final groupName = groupNameDiv?.text.trim() ?? '';
        
        // Extract image URL from style or img tag
        String imageUrl = '';
        final imgTag = anchor.querySelector('img');
        if (imgTag != null) {
          imageUrl = imgTag.attributes['src'] ?? '';
        }
        
        groups.add(GroupDto(
          groupId: groupId,
          categoryId: categoryId,
          groupName: groupName,
          imageUrl: imageUrl,
        ));
      }
      groupIdCounter++;
    }
    
    return groups;
  }

  /// Parse items from HTML response
  List<ItemDto> _parseItemsFromHtml(String html, int categoryId, int groupId) {
    final document = html_parser.parse(html);
    final items = <ItemDto>[];
    
    // Parse onclick attributes to extract item data
    final anchors = document.querySelectorAll('a[onclick*="viewitem"]');
    
    for (final anchor in anchors) {
      final onclick = anchor.attributes['onclick'] ?? '';
      final match = RegExp(r'viewitem\((\d+),(\d+),(\d+),(\d+)\)').firstMatch(onclick);
      
      if (match != null) {
        final itemId = int.parse(match.group(3)!);
        
        // Extract item name and code from the colored div
        final coloredDiv = anchor.querySelector('div[style*="background-color"]');
        String itemText = coloredDiv?.text.trim() ?? '';
        
        // Extract price if present
        double price = 0.0;
        final priceDiv = coloredDiv?.querySelectorAll('div').lastOrNull;
        if (priceDiv != null) {
          final priceText = priceDiv.text.trim();
          final priceMatch = RegExp(r'([\d.]+)').firstMatch(priceText);
          if (priceMatch != null) {
            price = double.tryParse(priceMatch.group(1)!) ?? 0.0;
          }
          // Remove price from item text
          itemText = itemText.replaceAll(priceText, '').trim();
        }
        
        // Split item code and name
        final parts = itemText.split(' ');
        final itemCode = parts.isNotEmpty ? parts[0] : '';
        final itemName = parts.length > 1 ? parts.sublist(1).join(' ') : itemText;
        
        // Extract image URL
        String imageUrl = '';
        final imgTag = anchor.querySelector('img');
        if (imgTag != null) {
          imageUrl = imgTag.attributes['src'] ?? '';
        }
        
        items.add(ItemDto(
          itemId: itemId,
          categoryId: categoryId,
          groupId: groupId,
          itemName: itemName,
          itemCode: itemCode,
          imageUrl: imageUrl,
          price: price,
        ));
      }
    }
    
    return items;
  }
}
