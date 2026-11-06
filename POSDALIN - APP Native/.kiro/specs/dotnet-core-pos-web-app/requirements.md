# Requirements Document

## Introduction

This document specifies the requirements for developing a .NET Core Razor Pages web application that replicates the functionality of an existing Flutter mobile POS application. The web application will provide a browser-based Point of Sale experience optimized for both desktop and mobile browsers, using server-side rendering with ASP.NET Core 6.0+ and Razor Pages architecture.

The application targets staff users (cashiers, sales staff, managers) who assist customers by browsing product catalogs, managing shopping carts, processing sales transactions, and viewing sale history through a responsive web interface. Staff members authenticate individually but may share devices, with each staff session managing the current customer's cart and processing sales on their behalf. It integrates directly with an existing SQL Server database via stored procedures using ADO.NET, while adding Progressive Web App (PWA) capabilities for offline access and modern web features like real-time updates via SignalR.

## Glossary

- **POS_Web_App**: The ASP.NET Core Razor Pages web application for staff use
- **Identity_Manager**: The ASP.NET Core Identity system managing staff authentication and sessions
- **Product_Catalog**: The hierarchical product organization system (Categories → Groups → Items) with server-side caching
- **Current_Cart**: The session-based collection of items for the customer currently being served by staff
- **Sale_Manager**: The service layer responsible for sale transaction creation and retrieval
- **Image_Optimizer**: The component responsible for image resizing, compression, and caching
- **Cache_Manager**: The memory cache system for storing product catalog data
- **Session_Manager**: The server-side session state management system
- **Staff_User**: A staff member (cashier, sales staff, manager, admin) who assists customers and processes sales
- **Customer**: A customer being served by staff (not a system user)
- **Sale**: A completed transaction processed by staff for a customer
- **Sale_History**: Records of all sales processed through the system
- **Staff_Session**: Authenticated session for a staff member
- **Item**: A product available for sale with associated pricing and images
- **Category**: A top-level classification for organizing items
- **Group**: A mid-level classification within a category for organizing items
- **Price_Tier**: A pricing structure based on quantity ranges (FromQty, ToQty)
- **Database**: The SQL Server database accessed directly by the POS_Web_App via stored procedures using ADO.NET
- **Service_Client**: The data access component that calls SQL Server stored procedures via ADO.NET
- **PWA**: Progressive Web App features enabling offline capability and app-like experience
- **SignalR_Hub**: The real-time communication hub for cart badge updates
- **Resource_Manager**: The localization system managing multi-language content
- **Tag_Helper**: Razor Pages server-side component for HTML generation
- **Partial_View**: Reusable Razor Pages component for UI elements
- **Permission_Manager**: The authorization system managing role-based permissions
- **Receipt_Printer**: The component responsible for generating and printing sale receipts
- **Payment_Handler**: The service layer handling payment processing (cash, card)

## Requirements

### Requirement 1: Staff Authentication with ASP.NET Core Identity

**User Story:** As a staff member, I want to securely log in to the web application with my username and password, so that I can access the POS system to assist customers and process sales.

#### Acceptance Criteria

1. THE POS_Web_App SHALL render a login page with username and password input fields using Razor Pages
2. WHEN a staff member submits credentials, THE Identity_Manager SHALL call the sp_Login stored procedure via Service_Client, passing username and password, to validate credentials and retrieve UserID, UserLevel, and FullName
3. WHEN the sp_Login stored procedure returns a valid user with UserLevel IN ('Staff', 'Manager', 'Admin'), THE Identity_Manager SHALL create an authentication cookie with persistent session storage
4. WHEN credentials are invalid, THE POS_Web_App SHALL display ModelState error "Password is incorrect!" or "User name is incorrect!"
5. WHEN a staff member enters an incorrect password 5 times, THE Identity_Manager SHALL lock the staff account using ASP.NET Core Identity lockout
6. WHEN a staff account is locked, THE POS_Web_App SHALL display the message "Your user account has been locked! Please contact IT"
7. THE Identity_Manager SHALL only authenticate users with UserLevel IN ('Staff', 'Manager', 'Admin') role claims
8. WHEN authentication succeeds, THE Identity_Manager SHALL create a ClaimsPrincipal with UserID, UserLevel, and FullName claims sourced from the sp_Login stored procedure response
9. THE POS_Web_App SHALL redirect to the home page after successful authentication
10. THE Identity_Manager SHALL use secure cookie settings with HttpOnly, Secure, and SameSite attributes
11. THE POS_Web_App SHALL display the authenticated staff member's name in the navigation bar

### Requirement 2: Persistent Staff Session Management with Cookie Authentication

**User Story:** As a staff member, I want my session to persist across browser sessions, so that I remain logged in during my shift without repeated logins.

#### Acceptance Criteria

1. WHEN the POS_Web_App receives a request, THE Identity_Manager SHALL validate the authentication cookie
2. WHEN a valid authentication cookie exists, THE Identity_Manager SHALL authenticate the staff member automatically
3. IF the authentication cookie is valid, THE POS_Web_App SHALL render the requested page with authenticated context
4. IF the authentication cookie is invalid or missing, THE POS_Web_App SHALL redirect to the login page
5. THE Identity_Manager SHALL include the authentication cookie in all HTTP responses
6. THE authentication cookie SHALL persist until the staff member explicitly logs out or the cookie expires based on configured sliding expiration
7. WHEN a staff member logs out, THE Identity_Manager SHALL delete the authentication cookie and clear server-side session state
8. THE Identity_Manager SHALL implement sliding expiration with a configurable timeout period (default 8 hours for staff shifts)
9. WHEN a staff member logs out, THE Current_Cart SHALL be cleared to protect customer privacy

### Requirement 3: Category Browsing with Server-Side Caching

**User Story:** As a staff member, I want to browse product categories quickly on the home page, so that I can help customers find products efficiently.

#### Acceptance Criteria

1. WHEN a staff member accesses the home page, THE Product_Catalog SHALL retrieve categories from the Cache_Manager
2. IF categories are not in cache, THE Product_Catalog SHALL call the sp_GetCategories stored procedure via Service_Client
3. WHEN categories are retrieved via Service_Client, THE Cache_Manager SHALL store them with a sliding expiration of 1 hour
4. THE POS_Web_App SHALL render each category with its name and image using Razor Pages server-side rendering
5. THE POS_Web_App SHALL apply color-coding to categories using a predefined array of 21 colors defined in appsettings.json
6. WHEN a staff member clicks on a category, THE POS_Web_App SHALL navigate to the group or item listing Razor Page for that category
7. THE POS_Web_App SHALL render categories in a responsive Bootstrap 5 grid layout
8. THE Image_Optimizer SHALL generate optimized category images at 300x200 pixels with WebP format support
9. THE POS_Web_App SHALL implement lazy loading for category images using browser native loading="lazy" attribute
10. THE Cache_Manager SHALL implement cache invalidation when product catalog data is refreshed via stored procedure calls

### Requirement 4: Group and Item Navigation with Partial Views

**User Story:** As a staff member, I want to navigate through groups within a category and view items, so that I can help customers find specific products efficiently.

#### Acceptance Criteria

1. WHEN a staff member selects a category with groups, THE Product_Catalog SHALL retrieve groups from Cache_Manager or the sp_GetGroups stored procedure via Service_Client
2. WHEN a staff member selects a category without groups, THE Product_Catalog SHALL retrieve items directly using the sp_GetSaleItems stored procedure via Service_Client
3. THE Product_Catalog SHALL pass CategoryID parameter to the sp_GetGroups stored procedure when retrieving groups
4. THE Product_Catalog SHALL pass CategoryID and GroupID parameters to the sp_GetSaleItems stored procedure when retrieving items
5. WHEN groups or items are retrieved via Service_Client, THE Cache_Manager SHALL store them with sliding expiration
6. WHEN a staff member clicks on a group, THE POS_Web_App SHALL render the items Razor Page for that group
7. THE POS_Web_App SHALL render groups and items using Partial_View components for reusability
8. THE POS_Web_App SHALL apply color-coded backgrounds to groups and items from the predefined color array
9. THE Image_Optimizer SHALL generate optimized group images at 300x200 pixels and item images at 200x110 pixels
10. THE POS_Web_App SHALL implement responsive grid layouts that adapt from 2 columns on mobile to 6 columns on desktop

### Requirement 5: Item Detail Modal with ViewData

**User Story:** As a staff member, I want to view detailed information about an item in a modal dialog, so that I can show customers product details and make informed sales decisions.

#### Acceptance Criteria

1. WHEN a staff member clicks on an item, THE POS_Web_App SHALL display a Bootstrap modal with item details
2. THE Product_Catalog SHALL retrieve item details from Cache_Manager or the sp_GetItemDetail stored procedure via Service_Client with ItemID parameter
3. THE Product_Catalog SHALL retrieve price tiers from Cache_Manager or the sp_GetItemUnits stored procedure via Service_Client
4. WHEN item details are retrieved, THE Cache_Manager SHALL store them with sliding expiration
5. THE modal SHALL display ItemName, ItemCode, full-resolution image, and available pricing tiers
6. THE modal SHALL display each price tier showing FromQty, ToQty, and Price formatted in both USD ($) and KHR (៛)
7. THE Image_Optimizer SHALL serve full-resolution item images with responsive srcset attributes for different viewport sizes
8. WHEN an image fails to load, THE POS_Web_App SHALL display a placeholder image from wwwroot/images/placeholder.png
9. THE modal SHALL provide quantity adjustment controls with increment buttons (+1, +5, +10, +50)
10. THE modal SHALL display the current quantity of the item in the Current_Cart retrieved from Session_Manager
11. THE POS_Web_App SHALL use ViewData to pass item detail data to the modal Partial_View

### Requirement 6: Cart Management for Customer Orders with Session State

**User Story:** As a staff member, I want to add items to the cart on behalf of the customer I am serving, so that I can build their order efficiently.

#### Acceptance Criteria

1. WHEN a staff member clicks "Add to Cart" on an item, THE Current_Cart SHALL add 1 quantity of that item to server-side session state
2. WHEN a staff member adds the same item multiple times, THE Current_Cart SHALL increment the quantity by the specified amount
3. THE POS_Web_App SHALL display a quantity badge on each item card showing the current quantity from session state
4. The quantity badge SHALL be rendered server-side and visible when quantity is greater than 0
5. THE Current_Cart SHALL store ItemID, ItemName, ItemCode, quantity, unit price, and selected Price_Tier in session state
6. WHEN a staff member changes quantity, THE Current_Cart SHALL recalculate the unit price based on applicable Price_Tier
7. WHEN a staff member removes an item from the cart view, THE Current_Cart SHALL delete the item from session state
8. THE Current_Cart SHALL calculate and display the real-time total for all items
9. THE Session_Manager SHALL persist Current_Cart contents across page requests using ASP.NET Core session state
10. WHEN quantity reaches a different Price_Tier threshold, THE Current_Cart SHALL automatically update the unit price
11. THE POS_Web_App SHALL display a Bootstrap toast notification when an item is added to the cart
12. THE POS_Web_App SHALL implement an AJAX endpoint for adding items without full page reload
13. THE Current_Cart SHALL belong to the staff member's session and represent the customer being served

### Requirement 7: Real-Time Cart Badge Updates with SignalR

**User Story:** As a staff member, I want to see the cart item count update immediately when I add items, so that I have real-time feedback on the customer's order.

#### Acceptance Criteria

1. WHEN the POS_Web_App initializes, THE POS_Web_App SHALL establish a SignalR connection to the SignalR_Hub
2. WHEN a staff member adds an item to the cart, THE Current_Cart SHALL send a cart update notification to the SignalR_Hub
3. WHEN the SignalR_Hub receives a cart update, THE SignalR_Hub SHALL broadcast the new item count to the staff member's browser
4. WHEN the browser receives the cart update notification, THE POS_Web_App SHALL update the cart badge count without page reload
5. THE SignalR_Hub SHALL use the authenticated staff member's connection ID to ensure updates are sent to the correct user
6. THE POS_Web_App SHALL fall back to polling every 10 seconds if SignalR connection fails
7. THE cart badge SHALL display the total number of items across all cart entries

### Requirement 8: Sale Transaction Processing with Service Client

**User Story:** As a staff member, I want to complete checkout and process the sale for the customer, so that their purchase is recorded in the system.

#### Acceptance Criteria

1. WHEN a staff member initiates checkout, THE Sale_Manager SHALL validate that the Current_Cart contains at least one item
2. THE Sale_Manager SHALL submit the sale via Service_Client by calling the sp_CreateSale stored procedure with StaffUserID, CustomerID (if known), sale date, items, quantities, and total
3. WHEN the sale is successfully created, THE sp_CreateSale stored procedure SHALL return a SaleID and InvoiceNo
4. WHEN sale creation succeeds, THE POS_Web_App SHALL display a success message using TempData and clear the Current_Cart from session state
5. WHEN sale creation fails, THE POS_Web_App SHALL display ModelState error and retain the Current_Cart contents
6. THE sp_CreateSale stored procedure SHALL store the CreatedDate timestamp and StaffUserID for each sale record created
7. THE POS_Web_App SHALL redirect to the sale confirmation Razor Page after successful checkout
8. THE Service_Client SHALL implement retry logic with exponential backoff for transient SQL errors
9. THE Sale_Manager SHALL wrap Service_Client calls in try-catch blocks and log exceptions
10. THE POS_Web_App SHALL provide an option to print the receipt after successful sale processing

### Requirement 9: Sale History Viewing with Role-Based Filtering

**User Story:** As a staff member, I want to view sale history, so that I can review transactions and assist customers with inquiries.

#### Acceptance Criteria

1. WHEN a staff member accesses the sale history Razor Page, THE Sale_Manager SHALL retrieve sales based on the staff member's role
2. WHEN a staff member with 'Staff' role views history, THE Sale_Manager SHALL call the sp_GetSaleHistory stored procedure via Service_Client, passing the staff member's UserID and UserLevel, so that only sales processed by that staff member are returned
3. WHEN a staff member with 'Manager' or 'Admin' role views history, THE Sale_Manager SHALL call the sp_GetSaleHistory stored procedure via Service_Client without a StaffUserID filter, so that all sales are returned
4. THE Sale_Manager SHALL request results filtered by Deleted=false by the sp_GetSaleHistory stored procedure
5. THE Sale_Manager SHALL request results sorted by CreatedDate in descending order by the sp_GetSaleHistory stored procedure
6. THE POS_Web_App SHALL display SaleID, InvoiceNo, CreatedDate, StaffUserName, CustomerName (if available), and Total for each sale in a Bootstrap table
7. WHEN a staff member clicks on a sale, THE POS_Web_App SHALL navigate to the sale detail Razor Page by calling the sp_GetSaleDetail stored procedure via Service_Client with the SaleID, showing all items
8. THE Sale_Manager SHALL pass page and pageSize parameters to the sp_GetSaleHistory stored procedure to support pagination; if the stored procedure does not support server-side pagination, THE Sale_Manager SHALL perform client-side pagination on the returned results
9. THE POS_Web_App SHALL implement server-side pagination controls showing 20 sales per page using Tag_Helper for page navigation
10. THE POS_Web_App SHALL display page navigation controls using Tag_Helper for pagination
11. THE POS_Web_App SHALL provide filter controls for date range, customer, and staff member (Manager/Admin only)
12. THE Sale_Manager SHALL pass date range parameters to the sp_GetSaleHistory stored procedure when date filters are provided

### Requirement 10: Product Search with Server-Side Filtering

**User Story:** As a staff member, I want to search for items by name or code, so that I can quickly find specific products to help customers.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide a search form accessible from the navigation bar on all pages
2. WHEN a staff member submits search text, THE Product_Catalog SHALL call the sp_SearchItems stored procedure via Service_Client, passing the keyword as a search parameter
3. THE sp_SearchItems stored procedure SHALL perform case-insensitive partial matching on ItemName and ItemCode
4. THE POS_Web_App SHALL render search results in the same responsive grid layout as category browsing
5. WHEN search returns no results, THE POS_Web_App SHALL display "No items found" message
6. WHEN a staff member clears the search, THE POS_Web_App SHALL redirect to the home page
7. THE Product_Catalog SHALL call the sp_SearchItems stored procedure only when a minimum of 2 characters are entered
8. THE Product_Catalog SHALL pass page and pageSize parameters to the sp_SearchItems stored procedure to support paginated results
9. THE POS_Web_App SHALL implement search autocomplete using a Razor Page handler that calls the sp_SearchItems stored procedure and returns JSON results
10. THE search form SHALL use Tag_Helper for model binding and client-side validation

### Requirement 11: Progressive Web App Offline Capability

**User Story:** As a staff member, I want to browse products and view the cart when offline, so that I can continue assisting customers during internet outages.

#### Acceptance Criteria

1. THE POS_Web_App SHALL include a service worker that caches the application shell (HTML, CSS, JavaScript)
2. The service worker SHALL cache product catalog data in browser Cache Storage API when pages are visited
3. The service worker SHALL cache product images in browser Cache Storage API with a maximum size limit of 50MB
4. WHILE the browser is offline, THE service worker SHALL serve cached pages and resources from Cache Storage
5. WHILE the browser is offline, THE POS_Web_App SHALL retrieve Current_Cart contents from browser localStorage
6. WHILE the browser is offline, THE Current_Cart SHALL remain functional for adding and removing items using JavaScript
7. WHEN the browser is offline and staff attempts checkout, THE POS_Web_App SHALL display "No internet connection. Sale will be queued for processing when online."
8. WHEN the browser reconnects, THE service worker SHALL synchronize pending sales using Background Sync API
9. THE POS_Web_App SHALL display a visual indicator showing online/offline status using the navigator.onLine API
10. WHILE offline, THE Sale_Manager SHALL queue sale submissions in browser IndexedDB for transmission when connectivity resumes
11. THE POS_Web_App SHALL include a web app manifest file with icons, theme colors, and display mode for PWA installation
12. The service worker SHALL implement a cache-first strategy for static assets and network-first strategy for dynamic data
13. WHEN connectivity resumes, THE POS_Web_App SHALL notify staff of queued sales and process them automatically

### Requirement 12: Image Optimization and CDN-Ready Caching

**User Story:** As a customer, I want product images to load quickly, so that I can browse products without waiting.

#### Acceptance Criteria

1. WHEN an image is requested, THE Image_Optimizer SHALL check server-side cache for the optimized version
2. IF the optimized image exists in cache, THE Image_Optimizer SHALL serve it with Cache-Control headers for browser caching
3. IF the optimized image does not exist, THE Image_Optimizer SHALL retrieve the original image data from the Database via Service_Client
4. WHEN an original image is retrieved, THE Image_Optimizer SHALL resize and compress it to appropriate dimensions
5. THE Image_Optimizer SHALL resize images to 300x200 for categories/groups, 200x110 for item thumbnails, and configurable sizes for detail views
6. THE Image_Optimizer SHALL generate WebP format with JPEG fallback for browser compatibility
7. THE Image_Optimizer SHALL implement lazy loading using browser native loading="lazy" attribute on img tags
8. WHEN an image fails to load, THE POS_Web_App SHALL display a placeholder image using Tag_Helper with fallback src
9. THE Image_Optimizer SHALL set Cache-Control headers with max-age of 7 days for optimized images
10. THE Image_Optimizer SHALL generate responsive srcset attributes with multiple image sizes for different viewport widths
11. THE Image_Optimizer SHALL implement ETags for efficient cache validation
12. WHERE a CDN is configured, THE Image_Optimizer SHALL set appropriate cache headers for CDN edge caching

### Requirement 13: Multi-Language Support with Resource Files

**User Story:** As a staff member, I want the app to display content in Khmer or English, so that I can assist customers in their preferred language.

#### Acceptance Criteria

1. THE POS_Web_App SHALL use Resource_Manager to load localized strings from .resx resource files
2. THE Resource_Manager SHALL support two cultures: "en-US" for English and "km-KH" for Khmer
3. THE POS_Web_App SHALL provide a language selector in the navigation bar allowing staff to switch languages
4. WHEN a staff member selects a language, THE POS_Web_App SHALL store the culture preference in a cookie
5. THE POS_Web_App SHALL apply the selected culture to all Razor Pages using RequestLocalizationMiddleware
6. WHERE Khmer content exists in resource files, THE POS_Web_App SHALL render Khmer text with proper Unicode encoding
7. THE POS_Web_App SHALL use Khmer-compatible fonts (Nokora, Battambang) defined in CSS
8. THE POS_Web_App SHALL display currency symbols for both US Dollars ($) and Cambodian Riel (៛) using culture-specific formatting
9. THE POS_Web_App SHALL use IStringLocalizer injection in Razor Pages for accessing localized strings
10. THE Resource_Manager SHALL fall back to English if a Khmer translation is not available

### Requirement 14: Responsive Design with Bootstrap 5

**User Story:** As a staff member, I want the web app to work well on desktop, tablet, and mobile devices, so that I can assist customers from any device.

#### Acceptance Criteria

1. THE POS_Web_App SHALL use Bootstrap 5 grid system for responsive layouts
2. THE POS_Web_App SHALL adapt layout to viewport sizes: xs (mobile), sm, md, lg, xl (desktop)
3. THE POS_Web_App SHALL provide touch-friendly buttons of at least 44x44 pixels for mobile browsers
4. THE POS_Web_App SHALL use Bootstrap responsive utility classes to hide/show elements based on screen size
5. THE POS_Web_App SHALL implement mobile-first CSS with min-width media queries
6. THE POS_Web_App SHALL use viewport meta tag with width=device-width for proper mobile rendering
7. THE POS_Web_App SHALL adjust grid column counts from 2 on mobile to 6 on desktop for product listings
8. THE POS_Web_App SHALL use Bootstrap navbar with collapse toggle for mobile navigation
9. THE POS_Web_App SHALL test layouts on Chrome DevTools device emulation for various screen sizes
10. THE POS_Web_App SHALL use CSS clamp() for fluid typography scaling

### Requirement 15: Staff Logout with Session Clearing

**User Story:** As a staff member, I want to log out of the web app, so that I can protect customer privacy and my account when leaving the terminal.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide a logout button accessible from the navigation bar
2. WHEN a staff member clicks logout, THE Identity_Manager SHALL call SignOutAsync() to remove the authentication cookie
3. WHEN logout completes, THE Session_Manager SHALL clear all session state including Current_Cart
4. WHEN logout completes, THE POS_Web_App SHALL clear any customer-specific data from browser localStorage
5. WHEN logout completes, THE POS_Web_App SHALL redirect to the login page
6. THE POS_Web_App SHALL not retain any customer-specific cart data after logout except cached public product data
7. The logout action SHALL use POST method with anti-forgery token validation to prevent CSRF attacks
8. THE POS_Web_App SHALL display a confirmation message before logout if Current_Cart contains items

### Requirement 16: Performance Optimization with Caching Strategy

**User Story:** As a staff member, I want pages to load quickly and respond smoothly, so that I can assist customers efficiently without delays.

#### Acceptance Criteria

1. WHEN the POS_Web_App renders a page, THE POS_Web_App SHALL complete server-side rendering within 500 milliseconds
2. THE Cache_Manager SHALL use IMemoryCache to store frequently accessed catalog data
3. THE Cache_Manager SHALL implement sliding expiration of 1 hour for cached product data
4. THE Cache_Manager SHALL implement absolute expiration of 24 hours for cached images
5. THE Product_Catalog SHALL implement pagination with 50 items per page to limit data transfer
6. THE Image_Optimizer SHALL limit concurrent image processing to 5 simultaneous requests using SemaphoreSlim
7. THE POS_Web_App SHALL implement Razor Pages compilation caching for faster page rendering
8. THE POS_Web_App SHALL use response compression middleware with Gzip for reducing payload size
9. THE POS_Web_App SHALL implement asynchronous Service_Client calls using async/await throughout all stored procedure interactions
10. THE POS_Web_App SHALL achieve a Lighthouse performance score of at least 90 on desktop and 70 on mobile

### Requirement 17: Cart View Management with Partial Views

**User Story:** As a staff member, I want to view and manage all items in the customer's cart, so that I can review their order before processing the sale.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide a cart Razor Page accessible from the navigation bar
2. The cart page SHALL display all items currently in the Current_Cart retrieved from Session_Manager
3. FOR each item in the cart, THE POS_Web_App SHALL display ItemName, ItemCode, quantity, unit price, and line total
4. THE POS_Web_App SHALL provide quantity adjustment form controls (+/-) for each item using Tag_Helper
5. THE POS_Web_App SHALL provide a remove button for each item that posts to a Razor Page handler
6. WHEN a staff member clicks remove, THE Current_Cart SHALL delete the item from session state and re-render the cart
7. The cart page SHALL display the grand total formatted in both USD ($) and KHR (៛)
8. The cart page SHALL display a checkout button that posts to the sale processing handler
9. THE POS_Web_App SHALL display a cart icon with badge showing total item count in the navigation bar
10. WHEN the Current_Cart is empty, THE cart page SHALL display "Cart is empty" message with a link to continue browsing
11. THE POS_Web_App SHALL use Partial_View components for rendering individual cart items for reusability
12. The cart page SHALL implement AJAX for quantity updates without full page reload

### Requirement 18: Security and Data Protection with HTTPS

**User Story:** As a staff member, I want customer and transaction data to be secure, so that sensitive information is protected.

#### Acceptance Criteria

1. THE POS_Web_App SHALL enforce HTTPS for all requests using RequireHttpsAttribute
2. THE Identity_Manager SHALL hash passwords using ASP.NET Core Identity PasswordHasher with PBKDF2
3. THE Identity_Manager SHALL store authentication cookies with Secure, HttpOnly, and SameSite=Strict attributes
4. THE POS_Web_App SHALL never log or display passwords in plain text
5. THE POS_Web_App SHALL validate all input using ModelState and Tag_Helper validation attributes
6. THE Service_Client SHALL validate all parameters before passing to stored procedures to prevent injection attacks; SQL injection prevention is handled by parameterized SqlCommand inputs
7. THE POS_Web_App SHALL implement anti-forgery token validation on all POST requests using ValidateAntiForgeryToken
8. THE POS_Web_App SHALL set security headers including X-Content-Type-Options, X-Frame-Options, and Content-Security-Policy
9. THE Session_Manager SHALL encrypt session state when using distributed cache
10. THE POS_Web_App SHALL implement rate limiting on authentication endpoints to prevent brute force attacks
11. THE Service_Client SHALL use encrypted connections (SSL/TLS) when connecting to SQL Server

### Requirement 19: Error Handling and Validation with ModelState

**User Story:** As a staff member, I want clear error messages when something goes wrong, so that I understand what action to take to resolve issues.

#### Acceptance Criteria

1. WHEN a stored procedure call fails, THE Service_Client SHALL throw a custom ServiceException with user-friendly message
2. WHEN a stored procedure returns an error, THE Service_Client SHALL parse the SqlException and add the error message to ModelState
3. WHEN required fields are empty during form submission, THE POS_Web_App SHALL display validation error messages using Tag_Helper validation summaries
4. WHEN the app encounters an unexpected exception, THE POS_Web_App SHALL log the error using ILogger and display "An unexpected error occurred"
5. THE Service_Client SHALL implement a command timeout of 30 seconds for stored procedure calls
6. WHEN authentication fails, THE POS_Web_App SHALL redirect to the login page with error message in TempData
7. THE POS_Web_App SHALL validate quantity inputs using Range data annotation to ensure positive integers only
8. THE POS_Web_App SHALL use custom error pages for 404 and 500 status codes
9. THE POS_Web_App SHALL implement global exception handling using UseExceptionHandler middleware
10. THE POS_Web_App SHALL display validation errors inline next to form fields using Tag_Helper validation messages

### Requirement 20: Accessibility Compliance with WCAG 2.1 Level AA

**User Story:** As a staff member with disabilities, I want the web app to be accessible with assistive technologies, so that I can use all features independently to assist customers.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide text alternatives for all images using alt attributes on img tags
2. THE POS_Web_App SHALL ensure keyboard navigation for all interactive elements with visible focus indicators
3. THE POS_Web_App SHALL maintain a logical tab order for form controls and navigation links
4. THE POS_Web_App SHALL use semantic HTML5 elements (nav, main, section, article) for proper document structure
5. THE POS_Web_App SHALL provide ARIA labels for icon buttons and interactive elements without visible text
6. THE POS_Web_App SHALL ensure color contrast ratios of at least 4.5:1 for normal text and 3:1 for large text
7. THE POS_Web_App SHALL not rely solely on color to convey information (use icons or text as well)
8. THE POS_Web_App SHALL provide skip navigation links for keyboard users to bypass repetitive content
9. THE POS_Web_App SHALL ensure form labels are properly associated with inputs using for attribute or aria-labelledby
10. THE POS_Web_App SHALL announce dynamic content changes to screen readers using ARIA live regions
11. THE POS_Web_App SHALL provide text resize support up to 200% without loss of functionality
12. THE POS_Web_App SHALL ensure modal dialogs trap focus and can be closed with Escape key

### Requirement 21: Data Synchronization with Background Services

**User Story:** As a staff member, I want the web app to have up-to-date product information, so that I can show customers current pricing and availability.

#### Acceptance Criteria

1. THE POS_Web_App SHALL implement a background service using IHostedService to periodically synchronize catalog data
2. The background service SHALL execute synchronization every 15 minutes when the application is running
3. WHEN synchronization runs, THE Product_Catalog SHALL call stored procedures via Service_Client to retrieve latest categories, groups, and items
4. WHEN new data is retrieved, THE Cache_Manager SHALL invalidate cached entries for updated products
5. The background service SHALL use a distributed lock mechanism to prevent concurrent synchronization in multi-instance deployments
6. The background service SHALL log synchronization start, completion, and any errors using ILogger
7. WHEN synchronization fails, THE background service SHALL implement exponential backoff retry with maximum 3 attempts
8. THE POS_Web_App SHALL continue serving cached data when synchronization fails
9. The background service SHALL record a last synchronization timestamp via a stored procedure call or in application-level cache/configuration
10. WHERE distributed caching is configured, THE Cache_Manager SHALL use Redis for shared cache across multiple web server instances

### Requirement 22: Razor Pages Structure and Tag Helpers

**User Story:** As a developer, I want a well-structured Razor Pages application with reusable components, so that the codebase is maintainable and follows best practices.

#### Acceptance Criteria

1. THE POS_Web_App SHALL organize Razor Pages in the Pages folder following feature-based folder structure
2. THE POS_Web_App SHALL use Partial_View components in Pages/Shared for reusable UI elements (cart item, product card)
3. THE POS_Web_App SHALL implement custom Tag_Helper components for complex UI patterns (product grid, pagination)
4. THE POS_Web_App SHALL use ViewComponents for server-side rendered components with logic (cart summary, category menu)
5. THE POS_Web_App SHALL use PageModel classes with dependency injection for service layer access
6. THE POS_Web_App SHALL implement Razor Page handlers (OnGetAsync, OnPostAsync) for HTTP method routing
7. THE POS_Web_App SHALL use TempData for passing messages across redirects
8. THE POS_Web_App SHALL use ViewData for passing data from PageModel to Razor view
9. THE POS_Web_App SHALL use strongly-typed models for form binding with Tag_Helper
10. THE POS_Web_App SHALL implement layout pages in Pages/Shared for consistent page structure

### Requirement 23: SQL Server Stored Procedure Definitions

**User Story:** As a developer, I want the database to expose stored procedures for all data operations needed by the POS_Web_App, so that the web app can access data through a consistent, secure stored procedure interface.

#### Acceptance Criteria

1. THE Database SHALL expose a `sp_Login(@Username, @Password)` stored procedure that validates staff credentials, and WHEN credentials are valid, THE stored procedure SHALL return UserID, UserLevel, and FullName; IF credentials are invalid, THE stored procedure SHALL return an error response indicating invalid credentials
2. THE Database SHALL expose a `sp_GetSaleHistory(@StaffUserID, @UserLevel, @StartDate, @EndDate, @Page, @PageSize)` stored procedure that returns a filtered, paginated list of sales; WHEN UserLevel is 'Staff', THE stored procedure SHALL filter results to the specified StaffUserID; WHEN UserLevel is 'Manager' or 'Admin', THE stored procedure SHALL return sales for all staff
3. THE Database SHALL expose a `sp_GetSaleDetail(@SaleID)` stored procedure that returns full sale details including all associated sale items for the specified SaleID
4. THE Database SHALL expose a `sp_SearchItems(@Keyword, @Page, @PageSize)` stored procedure that returns items matching the keyword against ItemName or ItemCode using case-insensitive partial matching
5. THE Database SHALL expose a `sp_GetCategories()` stored procedure that returns the list of all active categories
6. THE Database SHALL expose a `sp_GetItemDetail(@ItemID)` stored procedure that returns full details for the specified item
7. THE Database SHALL expose a `sp_GetItemUnits(@ItemID)` stored procedure that returns all price tiers (FromQty, ToQty, Price) for the specified item
8. THE Database SHALL expose a `sp_CreateSale(@StaffUserID, @CustomerID, @Items, @PaymentMethod, @Total)` stored procedure that processes a new sale transaction and returns SaleID and InvoiceNo
9. THE Database SHALL expose a `sp_VoidSale(@SaleID, @Reason, @StaffUserID)` stored procedure that marks the specified sale as voided, records the voiding staff member and reason, and IF the SaleID is not found, THE stored procedure SHALL return an error response indicating the sale was not found
10. THE existing stored procedures `sp_GetGroups(@CategoryID, @UserID)`, `sp_GetSaleItems(@CategoryID, @GroupID, @UserID)`, and `sp_GetOrderList(@PaymentType, @StartDate, @EndDate, @OrderID)` SHALL remain available and be called via Service_Client
11. THE Database SHALL use parameterized inputs for all stored procedures to prevent SQL injection

### Requirement 24: Direct SQL Server Stored Procedure Data Access

**User Story:** As a developer, I want the POS_Web_App to access SQL Server data directly via stored procedures using ADO.NET with a robust service client, so that data access is efficient, secure, resilient to transient failures, and maintains separation of concerns through the Service_Client abstraction.

#### Acceptance Criteria

1. THE POS_Web_App SHALL connect directly to SQL Server using ADO.NET (SqlConnection, SqlCommand) with CommandType.StoredProcedure
2. THE Service_Client SHALL implement all data operations by calling SQL Server stored procedures via ADO.NET — no inline SQL, no EF Core LINQ queries
3. THE Service_Client SHALL use SqlParameter objects for all stored procedure parameters to prevent SQL injection
4. THE POS_Web_App SHALL define C# model classes (DTOs) mirroring the data returned by stored procedures (e.g., SaleModel, ItemModel, CategoryModel)
5. THE POS_Web_App SHALL NOT use EF Core migrations or manage database schema; the database schema is managed externally
6. THE Service_Client SHALL execute all stored procedure calls asynchronously using async/await (OpenAsync, ExecuteReaderAsync, ReadAsync)
7. THE Service_Client SHALL manage database connections using ADO.NET connection pooling (SqlConnection is opened, used, and disposed per operation)
8. THE Service_Client SHALL read result sets using SqlDataReader and map columns to C# DTO objects
9. THE POS_Web_App SHALL register Service_Client as a scoped service (not HttpClient — uses SqlConnection instead)
10. THE Service_Client SHALL read the connection string from ConnectionStrings configuration section
11. THE Service_Client SHALL implement methods matching each stored procedure (sp_Login, sp_GetCategories, sp_GetGroups, sp_GetSaleItems, sp_GetItemDetail, sp_GetItemUnits, sp_SearchItems, sp_CreateSale, sp_GetSaleHistory, sp_GetSaleDetail, sp_VoidSale)
12. THE Service_Client SHALL implement a configurable command timeout (default 30 seconds) with override capability per operation
13. THE Service_Client SHALL implement Polly retry policies with exponential backoff for transient SQL errors (deadlocks, connection timeouts) with a maximum of 3 retry attempts
14. THE Service_Client SHALL wrap SqlException into custom ServiceException with user-friendly error messages
15. THE Service_Client SHALL log all stored procedure calls and errors using ILogger for debugging
16. THE Service_Client SHALL ensure SqlConnection and SqlCommand are properly disposed using await using or using statements

### Requirement 25: Configuration Management

**User Story:** As an administrator, I want to configure application settings without recompiling, so that I can adjust behavior for different environments.

#### Acceptance Criteria

1. THE POS_Web_App SHALL use appsettings.json for configuration settings
2. THE POS_Web_App SHALL support environment-specific configuration files (appsettings.Development.json, appsettings.Production.json)
3. THE POS_Web_App SHALL define configuration sections for ConnectionStrings, Database, Caching, Authentication, and Localization
4. THE POS_Web_App SHALL use IOptions pattern for strongly-typed configuration access
5. THE POS_Web_App SHALL support configuration from environment variables for containerized deployments
6. THE POS_Web_App SHALL protect sensitive configuration values using Secret Manager in development and Azure Key Vault in production
7. THE POS_Web_App SHALL validate configuration settings at startup using IValidateOptions
8. THE POS_Web_App SHALL log configuration values at startup (excluding sensitive data) for diagnostics
9. THE POS_Web_App SHALL support configuration reload without application restart for non-sensitive settings
10. THE POS_Web_App SHALL define configuration for cache expiration times, image optimization settings, and synchronization intervals

### Requirement 26: Receipt Generation and Printing

**User Story:** As a staff member, I want to print receipts after completing a sale, so that customers have proof of purchase.

#### Acceptance Criteria

1. WHEN a sale is successfully processed, THE Receipt_Printer SHALL generate a receipt with sale details
2. The receipt SHALL display business name, address, phone number, and tax ID from configuration
3. The receipt SHALL display SaleID, InvoiceNo, date, time, and staff member name
4. FOR each item, THE receipt SHALL display ItemName, quantity, unit price, and line total
5. The receipt SHALL display subtotal, tax amount (if applicable), and grand total in both USD and KHR
6. THE Receipt_Printer SHALL format the receipt for standard 80mm thermal printer width
7. THE POS_Web_App SHALL provide a print button on the sale confirmation page
8. WHEN staff clicks print, THE POS_Web_App SHALL open browser print dialog with receipt-optimized CSS
9. THE Receipt_Printer SHALL generate a PDF version of the receipt for email or download
10. THE POS_Web_App SHALL implement print-specific CSS using @media print to hide navigation and headers
11. The receipt SHALL include a barcode or QR code containing the SaleID for easy lookup
12. THE Receipt_Printer SHALL support reprint functionality from sale history for Manager/Admin roles

### Requirement 27: Payment Processing with Multiple Methods

**User Story:** As a staff member, I want to record payment information during checkout, so that sales are properly documented with payment method.

#### Acceptance Criteria

1. WHEN checkout is initiated, THE POS_Web_App SHALL display payment method selection (Cash, Card, Other)
2. THE Payment_Handler SHALL validate that a payment method is selected before completing the sale
3. WHEN Cash is selected, THE Payment_Handler SHALL provide fields for amount tendered and calculate change
4. WHEN Card is selected, THE Payment_Handler SHALL provide fields for card type (Visa, Mastercard, etc.) and last 4 digits
5. THE Payment_Handler SHALL store payment method, amount, and transaction details with the sale record
6. THE Sale_Manager SHALL include payment information when submitting via Service_Client
7. THE POS_Web_App SHALL display calculated change amount when cash payment exceeds total
8. THE Payment_Handler SHALL validate that amount tendered is greater than or equal to sale total
9. The sale confirmation page SHALL display payment method and change amount (if cash)
10. THE sp_CreateSale stored procedure SHALL record payment timestamp when a sale is created

### Requirement 28: Role-Based Authorization with Permission Manager

**User Story:** As an administrator, I want different permission levels for staff members, so that sensitive operations are restricted to authorized users.

#### Acceptance Criteria

1. THE Permission_Manager SHALL implement three role levels: Staff, Manager, Admin
2. THE Permission_Manager SHALL use ASP.NET Core authorization policies for role-based access control
3. THE POS_Web_App SHALL decorate Razor Page handlers with [Authorize(Roles="Manager,Admin")] for restricted operations
4. WHEN a staff member with 'Staff' role views sale history, THE Permission_Manager SHALL filter to show only their own sales
5. WHEN a staff member with 'Manager' or 'Admin' role views sale history, THE Permission_Manager SHALL allow viewing all sales
6. THE Permission_Manager SHALL restrict void/cancel operations to Manager and Admin roles only
7. THE Permission_Manager SHALL restrict discount application to Manager and Admin roles only
8. THE Permission_Manager SHALL restrict end-of-day reporting to Manager and Admin roles only
9. WHEN unauthorized access is attempted, THE POS_Web_App SHALL display "Access Denied" and log the attempt
10. THE Permission_Manager SHALL check permissions before rendering UI elements (hide void button for Staff role)
11. THE Identity_Manager SHALL store role claims in the authentication cookie for efficient permission checks

### Requirement 29: Sale Void and Cancellation with Audit Trail

**User Story:** As a manager, I want to void incorrect sales, so that errors can be corrected while maintaining an audit trail.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide a void button on sale detail pages for Manager and Admin roles only
2. WHEN a manager clicks void, THE POS_Web_App SHALL display a confirmation dialog requiring a reason
3. THE Sale_Manager SHALL validate that the void reason is not empty before processing
4. WHEN void is confirmed, THE Sale_Manager SHALL call the sp_VoidSale stored procedure via Service_Client with SaleID, reason, and staffUserID to mark the sale as voided
5. THE Sale_Manager SHALL store the voiding staff member's UserID and reason by passing them to the sp_VoidSale stored procedure
6. The sp_VoidSale stored procedure SHALL NOT physically delete sale records from the Database; it SHALL set Deleted=true and VoidedDate=current timestamp
7. WHEN a sale is voided, THE Sale_Manager SHALL call the sp_VoidSale stored procedure which creates an audit log entry with UserID, SaleID, timestamp, and reason
8. The voided sale SHALL remain visible in sale history with "VOIDED" status indicator
9. THE Sale_Manager SHALL prevent voiding of sales older than 7 days (configurable)
10. WHEN a sale is voided, THE sp_VoidSale stored procedure SHALL update inventory
11. THE POS_Web_App SHALL display void information on sale detail page (voided by, date, reason)

### Requirement 30: Discount Application with Authorization

**User Story:** As a manager, I want to apply discounts to items or entire sales, so that I can provide price adjustments to customers.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide discount options in the cart view for Manager and Admin roles only
2. THE POS_Web_App SHALL support percentage discounts (e.g., 10% off) and fixed amount discounts (e.g., $5 off)
3. WHEN a manager applies an item discount, THE Current_Cart SHALL recalculate the line total with discount applied
4. WHEN a manager applies a sale-level discount, THE Current_Cart SHALL recalculate the grand total
5. THE Sale_Manager SHALL validate that discount percentage is between 0 and 100
6. THE Sale_Manager SHALL validate that fixed discount amount does not exceed item or sale total
7. THE Sale_Manager SHALL store discount type, amount, and authorizing staff UserID with the sale record
8. The receipt SHALL display discount amounts for each discounted item and sale-level discount
9. THE POS_Web_App SHALL require manager to enter authorization password before applying discounts over 20%
10. THE Sale_Manager SHALL log all discount applications with UserID, amount, and timestamp
11. THE Current_Cart SHALL display original price with strikethrough and discounted price for discounted items

### Requirement 31: End-of-Day Reporting

**User Story:** As a manager, I want to generate end-of-day reports, so that I can reconcile cash and review daily sales performance.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide an end-of-day report page accessible to Manager and Admin roles only
2. THE Sale_Manager SHALL generate reports for a specified date range (default: current business day)
3. The report SHALL display total number of sales for the period
4. The report SHALL display total sales amount by payment method (Cash, Card, Other)
5. The report SHALL display total discounts applied during the period
6. The report SHALL display total voided sales and reasons
7. The report SHALL display sales breakdown by staff member showing count and total amount
8. The report SHALL display top 10 selling items by quantity and revenue
9. THE Sale_Manager SHALL calculate expected cash on hand (opening balance + cash sales - cash paid outs)
10. THE POS_Web_App SHALL provide export functionality to PDF and Excel formats
11. THE Sale_Manager SHALL retrieve report data by calling the sp_GetSaleHistory and related stored procedures via Service_Client with date range parameters and aggregating the results
12. The report SHALL display hourly sales distribution to identify peak times
13. THE POS_Web_App SHALL provide report comparison with previous day or same day last week

### Requirement 32: Customer Lookup and Association

**User Story:** As a staff member, I want to associate sales with known customers, so that we can track customer purchase history.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide optional customer lookup during checkout
2. THE POS_Web_App SHALL provide search functionality by customer name, phone number, or ID
3. WHEN a customer is found, THE POS_Web_App SHALL display customer name and contact information
4. THE Sale_Manager SHALL associate the CustomerID with the sale record when selected
5. WHEN no customer is selected, THE Sale_Manager SHALL process the sale as a walk-in customer with NULL CustomerID
6. THE POS_Web_App SHALL provide quick access to recent customers for faster selection
7. THE Sale_Manager SHALL store customer selection in Current_Cart session state during order building
8. The receipt SHALL display customer name when a customer is associated with the sale
9. The sale history filter SHALL support filtering by customer name or ID
10. THE POS_Web_App SHALL implement customer autocomplete using AJAX for search-as-you-type

### Requirement 33: Quick Sale Mode for Express Transactions

**User Story:** As a staff member, I want a quick sale mode for simple transactions, so that I can serve customers faster during busy periods.

#### Acceptance Criteria

1. THE POS_Web_App SHALL provide a "Quick Sale" button accessible from the home page
2. WHEN quick sale mode is activated, THE POS_Web_App SHALL display a simplified interface with item search and quantity input
3. THE POS_Web_App SHALL support barcode scanner input for item lookup using ItemCode
4. WHEN an ItemCode is scanned, THE Product_Catalog SHALL retrieve the item and add it to Current_Cart
5. The quick sale interface SHALL display running total prominently at the top
6. The quick sale interface SHALL provide number pad for quantity entry
7. The quick sale interface SHALL support pressing Enter to complete the sale
8. WHEN Enter is pressed, THE POS_Web_App SHALL skip to payment screen with Cash pre-selected
9. The quick sale mode SHALL calculate change automatically when amount is entered
10. The quick sale mode SHALL print receipt automatically after payment is recorded
11. The quick sale interface SHALL support Escape key to exit quick sale mode and return to normal browse mode
