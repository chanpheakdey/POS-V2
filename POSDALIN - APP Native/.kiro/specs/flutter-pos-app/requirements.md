# Requirements Document

## Introduction

This document specifies the requirements for developing a Flutter mobile application that replicates the flow and functionality of an existing ASP.NET Web Forms Point of Sale (POS) application. The Flutter app will provide a native mobile experience for Android and iOS platforms while maintaining compatibility with the existing backend infrastructure, including SQL Server database and ASMX web services.

The application is designed exclusively for customer use, enabling them to browse product catalogs, manage shopping carts, place orders, and view order history through a mobile interface with offline capabilities and multi-language support.

## Glossary

- **POS_App**: The Flutter mobile application being developed for customer use
- **Backend_System**: The existing ASP.NET Web Forms application with ASMX web services
- **Authentication_Service**: The token-based authentication system managing customer sessions
- **Product_Catalog**: The hierarchical product organization system (Categories → Groups → Items) with local file storage and pre-bundled default data
- **Shopping_Cart**: The temporary collection of items selected by a customer before checkout
- **Order_Manager**: The system component responsible for order creation and management
- **Image_Handler**: The component responsible for loading, caching, and optimizing images with pre-bundled image assets
- **Token**: A unique authentication identifier stored in cookies and database
- **Customer**: A customer user who browses products and places orders
- **Item**: A product available for sale with associated pricing and images
- **Category**: A top-level classification for organizing items
- **Group**: A mid-level classification within a category for organizing items
- **Price_Tier**: A pricing structure based on quantity ranges (FromQty, ToQty)
- **Session**: An authenticated customer's active connection to the system
- **Database**: The SQL Server database storing all application data
- **Local_Storage**: File-based persistent storage on the device for product catalog data
- **Default_Data**: Pre-bundled product catalog data included with the app installation
- **Image_Assets**: Pre-bundled product images included with the app installation

## Requirements

### Requirement 1: Customer Authentication

**User Story:** As a customer, I want to securely log in to the mobile app with my username and password, so that I can access the POS system to browse and purchase products.

#### Acceptance Criteria

1. THE POS_App SHALL display a login screen with username and password input fields
2. WHEN a customer submits valid credentials, THE Authentication_Service SHALL validate the credentials against the Database for UserLevel='Customer'
3. WHEN credentials are valid, THE Authentication_Service SHALL generate a Token and store it locally with no expiration
4. WHEN credentials are invalid, THE POS_App SHALL display an error message "Password is incorrect!" or "User name is incorrect!"
5. WHEN a customer enters an incorrect password 5 times, THE Authentication_Service SHALL lock the customer account
6. WHEN a customer account is locked, THE POS_App SHALL display the message "Your user account has been locked! Please contact IT"
7. THE Authentication_Service SHALL only allow customer-level user authentication (UserLevel='Customer')
8. WHEN authentication succeeds, THE POS_App SHALL store the Token persistently on the device without expiration
9. THE POS_App SHALL navigate to the home screen after successful authentication

### Requirement 2: Token-Based Session Management

**User Story:** As a customer, I want my session to persist indefinitely on my device, so that I never have to log in again unless I explicitly log out.

#### Acceptance Criteria

1. WHEN the POS_App launches, THE Authentication_Service SHALL check for a valid Token in local storage
2. WHEN a valid Token exists, THE Authentication_Service SHALL validate the Token with the Backend_System
3. IF the Token is valid, THE POS_App SHALL navigate directly to the home screen
4. IF the Token is invalid or missing, THE POS_App SHALL display the login screen
5. THE Authentication_Service SHALL include the Token in all API requests to the Backend_System
6. THE Token SHALL persist on the device without expiration until the customer explicitly logs out
7. WHEN a customer logs out, THE POS_App SHALL remove the Token from local storage and mark it as expired in the Database

### Requirement 3: Category Browsing with Pre-bundled Data

**User Story:** As a customer, I want to browse product categories immediately after installing the app, so that I can start shopping without waiting for data downloads.

#### Acceptance Criteria

1. WHEN the POS_App is installed, THE Product_Catalog SHALL include pre-bundled default category data in local files
2. WHEN a customer accesses the home screen for the first time, THE Product_Catalog SHALL display categories from the pre-bundled default data
3. WHEN the POS_App connects to the Backend_System, THE Product_Catalog SHALL retrieve the latest categories using the getcategory endpoint
4. WHEN new categories are found in the Backend_System, THE Product_Catalog SHALL append only the new categories to the local file
5. THE Product_Catalog SHALL not overwrite or replace existing category data, only append new categories
6. THE POS_App SHALL display each category with its name and image
7. THE POS_App SHALL apply color-coding to categories using a predefined color array of 21 colors
8. WHEN a customer taps on a category, THE POS_App SHALL navigate to the group or item listing for that category
9. THE POS_App SHALL display categories in a scrollable grid layout
10. THE Image_Handler SHALL optimize category images to 300x200 pixels for display

### Requirement 4: Group and Item Navigation

**User Story:** As a customer, I want to navigate through groups within a category and view items, so that I can find specific products efficiently.

#### Acceptance Criteria

1. WHEN a customer selects a category with groups, THE Product_Catalog SHALL display all groups within that category
2. WHEN a customer selects a category without groups, THE Product_Catalog SHALL display all items directly
3. THE Product_Catalog SHALL retrieve groups from local files if available, otherwise from the Backend_System using the getgroup endpoint with CategoryID parameter
4. THE Product_Catalog SHALL retrieve items from local files if available, otherwise from the Backend_System using the getSaleItem endpoint with CategoryID and GroupID parameters
5. WHEN groups or items are retrieved from the Backend_System, THE Product_Catalog SHALL save them to local files
6. WHEN a customer taps on a group, THE POS_App SHALL display all items within that group
7. THE POS_App SHALL display groups and items in a scrollable grid layout with color-coded backgrounds
8. THE Image_Handler SHALL optimize group images to 300x200 pixels and item images to 200x110 pixels

### Requirement 5: Item Detail Viewing

**User Story:** As a customer, I want to view detailed information about an item and manage quantities in my cart, so that I can make informed purchase decisions and adjust my order.

#### Acceptance Criteria

1. WHEN a customer long-presses or accesses item details (context-specific), THE POS_App SHALL display a detail view with the item's full information
2. THE Product_Catalog SHALL retrieve item details from local files if available, otherwise from the Backend_System using the getitemdetail endpoint with ItemID parameter
3. THE Product_Catalog SHALL retrieve price tiers from local files if available, otherwise from the Backend_System using the getitemunit endpoint
4. WHEN item details or price tiers are retrieved from the Backend_System, THE Product_Catalog SHALL save them to local files
5. THE POS_App SHALL display ItemName, ItemCode, ImageUrl, and available pricing tiers
6. THE POS_App SHALL display each price tier showing FromQty, ToQty, and Price
7. THE POS_App SHALL display prices in both US Dollars ($) and Cambodian Riel (៛) currency formats
8. THE Image_Handler SHALL display the full-resolution item image with lazy loading
9. THE item detail view MAY provide quick quantity adjustment buttons (+1, +5, +10, +50) for faster bulk ordering
10. THE item detail view SHALL display the current quantity of the item in the cart

### Requirement 6: Shopping Cart Management

**User Story:** As a customer, I want to quickly add items to my cart by tapping them, so that I can build my order efficiently.

#### Acceptance Criteria

1. WHEN a customer taps on an item, THE Shopping_Cart SHALL immediately add 1 quantity of that item to the cart
2. WHEN a customer taps the same item multiple times, THE Shopping_Cart SHALL increment the quantity by 1 for each tap
3. THE POS_App SHALL display a quantity badge on each item image showing the current quantity in the cart
4. THE quantity badge SHALL be visible on the item thumbnail when quantity is greater than 0
5. THE Shopping_Cart SHALL store ItemID, ItemName, ItemCode, quantity, unit price, and selected price tier
6. WHEN a customer changes quantity, THE Shopping_Cart SHALL update the unit price based on applicable price tier
7. WHEN a customer removes an item from the cart view, THE Shopping_Cart SHALL delete the item from the cart and remove the quantity badge
8. THE Shopping_Cart SHALL calculate and display the real-time total for all items
9. THE POS_App SHALL persist the Shopping_Cart contents locally until checkout or manual clearing
10. WHEN quantity reaches a different price tier, THE Shopping_Cart SHALL automatically update the unit price
11. THE POS_App SHALL provide visual feedback (animation or color change) when an item is added to the cart
12. THE quantity badge SHALL update immediately after each tap without requiring navigation to the cart view

### Requirement 7: Order Placement

**User Story:** As a customer, I want to complete checkout and place my order, so that my purchase is recorded in the system.

#### Acceptance Criteria

1. WHEN a customer initiates checkout, THE Order_Manager SHALL validate that the Shopping_Cart contains at least one item
2. THE Order_Manager SHALL submit the order to the Backend_System with CustomerID, order date, items, quantities, and total
3. WHEN the order is successfully created, THE Backend_System SHALL return an OrderID and InvoiceNo
4. WHEN order creation succeeds, THE POS_App SHALL display a success message and clear the Shopping_Cart
5. WHEN order creation fails, THE POS_App SHALL display an error message and retain the Shopping_Cart contents
6. THE Order_Manager SHALL store the CreatedDate timestamp for each order
7. THE POS_App SHALL navigate to the order history screen after successful checkout

### Requirement 8: Order History Viewing

**User Story:** As a customer, I want to view my order history, so that I can review past purchases.

#### Acceptance Criteria

1. WHEN a customer accesses order history, THE Order_Manager SHALL retrieve all orders for the authenticated customer
2. THE Order_Manager SHALL display orders sorted by CreatedDate in descending order
3. THE POS_App SHALL display OrderID, InvoiceNo, CreatedDate, and Total for each order
4. WHEN a customer taps on an order, THE POS_App SHALL display the complete order details including all items
5. THE Order_Manager SHALL filter out orders where Deleted is true
6. THE POS_App SHALL support pagination or infinite scrolling for large order lists

### Requirement 9: Search Functionality

**User Story:** As a customer, I want to search for items by name or code, so that I can quickly find specific products.

#### Acceptance Criteria

1. THE POS_App SHALL provide a search input field accessible from the home screen
2. WHEN a customer enters search text, THE Product_Catalog SHALL filter items from local files matching ItemName or ItemCode
3. THE Product_Catalog SHALL perform case-insensitive partial matching on search terms
4. THE POS_App SHALL display search results in the same grid format as category browsing
5. WHEN search returns no results, THE POS_App SHALL display "No items found" message
6. WHEN a customer clears the search, THE POS_App SHALL return to the category view
7. THE Product_Catalog SHALL execute search queries with a minimum of 2 characters entered
8. THE Product_Catalog SHALL search against locally stored catalog data for instant results

### Requirement 10: Offline Capability with Local File Storage

**User Story:** As a customer, I want to browse products and view my cart when offline, so that I can continue using the app without an internet connection.

#### Acceptance Criteria

1. WHEN the POS_App is installed, THE Product_Catalog SHALL include pre-bundled default data for categories, groups, and items
2. WHEN the POS_App is installed, THE Image_Handler SHALL include pre-bundled images for all default products
3. WHEN the POS_App loads data from the Backend_System, THE Product_Catalog SHALL persist only new data to local files without overwriting existing data
4. THE Product_Catalog SHALL append new categories, groups, items, and price tiers to existing local files
5. WHILE the device is offline, THE Product_Catalog SHALL retrieve category, group, and item data from local files
6. WHILE the device is offline, THE Image_Handler SHALL display images from local storage
7. WHILE the device is offline, THE Shopping_Cart SHALL remain functional for adding and removing items
8. WHEN the device is offline and customer attempts checkout, THE POS_App SHALL display "No internet connection. Please try again when online."
9. WHEN the device reconnects, THE Product_Catalog SHALL synchronize with the Backend_System and append new data to local files
10. THE POS_App SHALL display a visual indicator showing online/offline status
11. WHILE offline, THE Order_Manager SHALL queue order submissions in local files for transmission when connectivity resumes
12. THE Product_Catalog SHALL use JSON format for storing catalog data in local files

### Requirement 11: Image Loading and Caching with Pre-bundled Assets

**User Story:** As a customer, I want product images to load instantly without waiting for downloads, so that I can quickly browse products.

#### Acceptance Criteria

1. WHEN the POS_App is installed, THE Image_Handler SHALL include pre-bundled Image_Assets for all default products
2. WHEN an image is requested, THE Image_Handler SHALL check local storage first for the image
3. IF the image exists locally, THE Image_Handler SHALL load it directly from local storage
4. IF the image does not exist locally, THE Image_Handler SHALL download it from the Backend_System
5. WHEN an image is downloaded from the Backend_System, THE Image_Handler SHALL save it to local storage for future use
6. THE Image_Handler SHALL resize images to appropriate dimensions: 300x200 for categories/groups, 200x110 for item thumbnails
7. THE Image_Handler SHALL implement lazy loading for images in scrollable lists
8. WHEN an image fails to load, THE POS_App SHALL display a placeholder image
9. THE Image_Handler SHALL compress images to reduce bandwidth usage while maintaining acceptable quality
10. THE Image_Handler SHALL use the hdImage_all.ashx endpoint with width and height parameters for dynamic resizing when downloading from Backend_System
11. WHEN the Backend_System indicates an image has been updated, THE Image_Handler SHALL download the updated image and replace the local copy
12. THE Image_Handler SHALL store images in organized folders by type (categories, groups, items) in local storage

### Requirement 12: Multi-Language Support

**User Story:** As a customer, I want the app to display Khmer language text where present, so that I can use the app in my preferred language.

#### Acceptance Criteria

1. WHERE Khmer language content exists in the Database, THE POS_App SHALL display Khmer text correctly
2. THE POS_App SHALL support Unicode rendering for Khmer script (កម្ពុជា)
3. THE POS_App SHALL display currency symbols for both US Dollars ($) and Cambodian Riel (៛)
4. THE POS_App SHALL use appropriate fonts that support Khmer glyphs
5. WHERE English and Khmer translations both exist, THE POS_App SHALL display both or allow customer selection

### Requirement 13: Error Handling and Validation

**User Story:** As a customer, I want clear error messages when something goes wrong, so that I understand what action to take.

#### Acceptance Criteria

1. WHEN a network request fails, THE POS_App SHALL display a user-friendly error message
2. WHEN the Backend_System returns an error, THE POS_App SHALL parse and display the error message
3. WHEN required fields are empty during login, THE POS_App SHALL highlight the field with red border and show validation message
4. WHEN the app encounters an unexpected error, THE POS_App SHALL log the error details and display "An unexpected error occurred"
5. THE POS_App SHALL implement timeout handling for API requests with a 30-second timeout
6. WHEN Token validation fails, THE POS_App SHALL redirect to the login screen
7. THE POS_App SHALL validate quantity inputs to ensure positive integers only

### Requirement 14: Responsive UI Design

**User Story:** As a customer, I want the app to work well on different screen sizes, so that I have a good experience on any device.

#### Acceptance Criteria

1. THE POS_App SHALL adapt layout to portrait and landscape orientations
2. THE POS_App SHALL provide touch targets of at least 48x48 logical pixels for all interactive elements
3. THE POS_App SHALL use responsive grid layouts that adjust column count based on screen width
4. THE POS_App SHALL scale text appropriately for different screen densities
5. THE POS_App SHALL test layouts on small phones (4-5 inches), standard phones (5-6 inches), and tablets (7+ inches)
6. THE POS_App SHALL maintain consistent spacing and padding across different screen sizes

### Requirement 15: Customer Logout

**User Story:** As a customer, I want to log out of the app, so that I can protect my account when sharing a device.

#### Acceptance Criteria

1. THE POS_App SHALL provide a logout button accessible from the main navigation
2. WHEN a customer taps logout, THE Authentication_Service SHALL expire the Token in the Database
3. WHEN logout completes, THE POS_App SHALL remove the Token from local storage
4. WHEN logout completes, THE POS_App SHALL clear the Shopping_Cart contents
5. WHEN logout completes, THE POS_App SHALL navigate to the login screen
6. THE POS_App SHALL not retain any customer-specific data after logout except cached public product data

### Requirement 16: Performance Optimization

**User Story:** As a customer, I want the app to load quickly and respond smoothly, so that I can complete tasks efficiently.

#### Acceptance Criteria

1. WHEN the POS_App launches, THE POS_App SHALL display the splash screen for no more than 2 seconds
2. WHEN navigating between screens, THE POS_App SHALL complete transitions within 300 milliseconds
3. WHEN scrolling through item lists, THE POS_App SHALL maintain 60 frames per second
4. THE Product_Catalog SHALL implement pagination for categories with more than 50 items
5. THE Image_Handler SHALL limit concurrent image downloads to 5 simultaneous requests
6. THE POS_App SHALL initialize in under 3 seconds on a mid-range device with network connectivity
7. THE Product_Catalog SHALL load catalog data from local files within 500 milliseconds
8. THE Product_Catalog SHALL perform background synchronization with the Backend_System without blocking the UI

### Requirement 18: Data Synchronization Strategy

**User Story:** As a customer, I want the app to efficiently update product data in the background, so that I always have access to the latest products without losing existing data.

#### Acceptance Criteria

1. WHEN the POS_App is first installed, THE Product_Catalog SHALL initialize with pre-bundled Default_Data for categories, groups, and items
2. WHEN the POS_App is first installed, THE Image_Handler SHALL initialize with pre-bundled Image_Assets for all default products
3. WHEN the POS_App connects to the Backend_System, THE Product_Catalog SHALL compare local data with backend data to identify new items
4. THE Product_Catalog SHALL only append new categories to the local file, never overwriting or deleting existing categories
5. THE Product_Catalog SHALL track the last synchronization timestamp to optimize data retrieval
6. WHEN synchronizing, THE Product_Catalog SHALL request only data newer than the last synchronization timestamp from the Backend_System
7. THE Product_Catalog SHALL perform incremental updates by appending new data rather than replacing the entire dataset
8. THE Product_Catalog SHALL handle data conflicts by preferring backend data for existing items while preserving local-only data
9. WHEN synchronization completes, THE Product_Catalog SHALL update the last synchronization timestamp
10. THE POS_App SHALL perform background synchronization periodically when connected to the internet
11. THE Product_Catalog SHALL not require full data downloads after the initial app installation
12. WHEN synchronizing images, THE Image_Handler SHALL compare local images with backend image metadata to identify new or updated images
13. THE Image_Handler SHALL download only new or updated images from the Backend_System
14. WHEN an image is updated on the Backend_System, THE Image_Handler SHALL replace the local copy with the updated version

### Requirement 19: Cart View and Management

**User Story:** As a customer, I want to view and manage all items in my cart, so that I can review my order before checkout.

#### Acceptance Criteria

1. THE POS_App SHALL provide a cart view accessible from the main navigation
2. THE cart view SHALL display all items currently in the Shopping_Cart
3. FOR each item in the cart, THE POS_App SHALL display ItemName, ItemCode, quantity, unit price, and line total
4. THE POS_App SHALL provide quantity adjustment controls (+/-) for each item in the cart view
5. THE POS_App SHALL provide a delete/remove button for each item in the cart view
6. WHEN a customer taps the remove button, THE Shopping_Cart SHALL delete the item and update the quantity badge to 0
7. THE cart view SHALL display the grand total for all items
8. THE cart view SHALL display a checkout button to initiate order placement
9. THE POS_App SHALL display a cart icon with the total number of items in the main navigation
10. WHEN the Shopping_Cart is empty, THE cart view SHALL display "Your cart is empty" message

### Requirement 17: Security and Data Protection

**User Story:** As a customer, I want my data to be secure, so that my personal information and transactions are protected.

#### Acceptance Criteria

1. THE POS_App SHALL transmit all data to the Backend_System using HTTPS protocol
2. THE Authentication_Service SHALL never log or display passwords in plain text
3. THE POS_App SHALL store the Token in secure storage using platform-specific secure storage APIs
4. THE POS_App SHALL not cache sensitive customer data such as passwords locally
5. THE POS_App SHALL validate all customer inputs to prevent injection attacks
6. THE Authentication_Service SHALL use parameterized queries when communicating with the Database
7. WHEN a Token expires, THE POS_App SHALL immediately require re-authentication
8. THE POS_App SHALL store the Token persistently on the device without automatic expiration
