# Implementation Plan: .NET Core POS Web Application

## Overview

Incremental implementation of an ASP.NET Core 6.0+ Razor Pages POS web application. All data access goes exclusively through `IServiceClient` (AsmxServiceClient) calling the existing ASMX Backend_Service endpoints — the POS_Web_App has no direct database connection. Tasks follow the layered architecture from the design: project scaffolding → infrastructure (HttpClient, caching, session) → service layer → Razor Pages UI → SignalR/PWA → cross-cutting concerns (security, localization, accessibility, reporting). Property-based tests (FsCheck) are placed close to the logic they verify to catch regressions early.

---

## Tasks

- [x] 1. Scaffold project structure and configure infrastructure
  - Create the `PosWebApp` ASP.NET Core 6.0+ Razor Pages project (Web SDK)
  - Add NuGet packages: `Microsoft.AspNetCore.Identity`, `Microsoft.AspNetCore.SignalR`, `SixLabors.ImageSharp`, `Polly`, `X.PagedList.Mvc.Core`, `FsCheck.Xunit`
  - Create directory skeleton matching the design: `Pages/`, `Services/`, `Models/ViewModels/`, `Models/DTOs/`, `Infrastructure/`, `Infrastructure/Exceptions/`, `TagHelpers/`, `Hubs/`, `Resources/`, `wwwroot/css/`, `wwwroot/js/`, `wwwroot/images/`
  - Add `appsettings.json` with all sections: `BackendService` (BaseUrl, TimeoutSeconds, RetryCount), `Cache`, `Auth`, `Catalog` (colors array, ItemsPerPage), `Payment` (KhrExchangeRate), `Image` — **no ConnectionStrings section**
  - Wire `Program.cs` middleware pipeline in the exact order from the design: `UseExceptionHandler → UseHsts → UseHttpsRedirection → UseStaticFiles → UseRouting → UseRequestLocalization → UseAuthentication → UseAuthorization → UseSession → UseResponseCompression → MapRazorPages → MapHub("/cartHub")`
  - Create `PosWebApp.Tests` project with `Unit/`, `Unit/Properties/`, `Unit/Services/`, and `Integration/` subfolders
  - _Requirements: 22.1, 22.6, 25.1, 25.2, 25.3_

- [x] 2. Implement session models and infrastructure helpers
  - Create `Models/DTOs/` DTO records matching ASMX service responses: `LoginResult`, `CategoryDto`, `GroupDto`, `ItemDto`, `ItemDetailDto`, `PriceTierDto`, `SaleCreationResult`, `SaleListItemDto`, `SaleDetailDto`, `SaleItemDto`, `CustomerDto`, `SaleHistoryQuery`
  - Create session model records in `Models/ViewModels/`: `CartItem` and `CartState` (no DB persistence — session only)
  - Create `Infrastructure/SessionExtensions.cs` with typed `SetObject<T>` / `GetObject<T>` helpers using `JsonSerializer`
  - Create `Infrastructure/CacheKeys.cs` with all cache key constant strings (`catalog:categories`, `catalog:groups:{categoryId}`, `catalog:items:{categoryId}:{groupId}`, `catalog:item:{itemId}`, `catalog:tiers:{itemId}`)
  - Create `Infrastructure/PriceTierResolver.cs` with pure static `Resolve(int qty, IReadOnlyList<PriceTierDto> tiers)` method (returns matching tier or lowest-tier fallback when no tier matches)
  - Create `Infrastructure/CurrencyFormatter.cs` with static `Format(decimal amount, decimal exchangeRate)` returning `(string usd, string khr)` tuple — USD with `$` symbol and 2 decimal places, KHR with `៛` symbol as integer
  - Create `Infrastructure/Exceptions/ServiceException.cs` hierarchy: `ServiceException`, `AuthenticationException`, `CartValidationException`, `SaleProcessingException`
  - _Requirements: 6.5, 6.6, 13.8_

  - [x]* 2.1 Write property test for PriceTierResolver (Property 4)
    - **Property 4: Price Tier Resolution** — for any positive integer quantity and any non-empty tier list, the resolved tier satisfies `FromQty <= qty` and (`ToQty == 0` OR `qty <= ToQty`)
    - File: `PosWebApp.Tests/Unit/Properties/PriceTierPropertyTests.cs`
    - **Validates: Requirements 6.6, 6.10**

  - [x]* 2.2 Write property test for CurrencyFormatter (Property 10)
    - **Property 10: Currency Formatting Completeness** — for any non-negative decimal amount and positive exchange rate, the USD string contains `$` with exactly 2 decimal places, and the KHR string contains `៛` as an integer
    - File: `PosWebApp.Tests/Unit/Properties/FormattingPropertyTests.cs`
    - **Validates: Requirements 5.6, 13.8, 17.7**

- [x] 3. Implement Identity_Manager — staff authentication via IServiceClient
  - [x] 3.1 Implement cookie authentication and Login Razor Page
    - Configure `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` with `HttpOnly=true`, `Secure=Always`, `SameSite=Strict`, `SlidingExpiration=true`, `ExpireTimeSpan=8h`, `LoginPath="/Account/Login"`, `LogoutPath="/Account/Logout"` in `Program.cs`
    - Create `Pages/Account/Login.cshtml` / `.cs` with `LoginInputModel` (`[Required]` annotations on `UserName` and `Password`)
    - Implement `OnPostAsync`: call `_serviceClient.LoginAsync(Input.UserName, Input.Password)`, read `LoginResult`
    - On `result.IsLockedOut`: add `ModelState` error "Your user account has been locked! Please contact IT", return `Page()`
    - On `!result.IsSuccess`: add `ModelState` error from `result.ErrorMessage`, return `Page()`
    - On success: build `ClaimsPrincipal` from `LoginResult` fields (`UserID`, `UserLevel`, `FullName`, `ClaimTypes.Name`, `ClaimTypes.Role`), call `HttpContext.SignInAsync`, redirect to home
    - Reject login if `result.UserLevel` is not in `('Staff', 'Manager', 'Admin')` even if `IsSuccess=true`
    - Implement rate limiting on `/Account/Login` POST using ASP.NET Core rate limiting middleware
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 2.1, 2.2, 2.6, 2.8, 18.3, 18.10_

  - [x] 3.2 Create Logout Razor Page and navbar staff display
    - `Pages/Account/Logout.cshtml` / `.cs` — `OnPostAsync` with `[ValidateAntiForgeryToken]`, calls `SignOutAsync()`, clears session and `localStorage` via JavaScript, redirects to login
    - Show logout confirmation modal when `Current_Cart` contains items
    - Display authenticated staff name (from `FullName` claim) in `_NavbarPartial`
    - _Requirements: 1.11, 2.7, 2.9, 15.1, 15.2, 15.3, 15.4, 15.5, 15.7, 15.8_

  - [x]* 3.3 Write unit tests for authentication
    - Test valid login (`IsSuccess=true`, valid `UserLevel`) redirects to home page
    - Test `IsSuccess=false` shows model error
    - Test `IsLockedOut=true` shows locked message
    - Test `UserLevel` outside allowed set is rejected even when `IsSuccess=true`
    - Mock `IServiceClient.LoginAsync` with controlled `LoginResult` responses
    - File: `PosWebApp.Tests/Integration/AuthenticationTests.cs`
    - _Requirements: 1.2, 1.4, 1.6, 1.7_

- [x] 4. Implement Service_Client (SqlServiceClient) for SQL Server stored procedure integration
  - [x] 4.1 Create SqlServiceClient with all IServiceClient methods
    - Implement `IServiceClient` interface with the complete method set from the design:
      - Auth: `LoginAsync(username, password) → LoginResult`
      - Catalog: `GetCategoriesAsync()`, `GetGroupsAsync(categoryId)`, `GetSaleItemsAsync(categoryId, groupId)`, `GetItemDetailAsync(itemId)`, `GetItemUnitsAsync(itemId)`, `SearchItemsAsync(keyword, page, pageSize)`
      - Sales: `CreateSaleAsync(request)`, `GetSaleHistoryAsync(query)`, `GetSaleDetailAsync(saleId)`, `VoidSaleAsync(saleId, reason, staffUserId)`
      - Customers: `SearchCustomersAsync(query)`
      - Images: `GetImageStreamAsync(imagePath)`
    - Register as scoped service in `Program.cs` with connection string from `ConnectionStrings:DefaultConnection`, 30-second command timeout
    - Configure Polly retry policy: 3 retries with exponential backoff (1s, 2s, 4s) on transient SQL errors (deadlocks, connection timeouts)
    - Add Polly circuit breaker to prevent cascading failures
    - Log all stored procedure calls via `ILogger`
    - Wrap all failures (connection timeout, deadlock, SqlException) in `ServiceException` — never propagate `SqlException` or `TimeoutException`
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.7, 24.8, 24.9, 24.10, 24.11, 19.1, 19.5_

  - [x]* 4.2 Write property test for ServiceClient exception wrapping (Property 11)
    - **Property 11: Service Client Exception Wrapping** — for any SQL failure scenario (connection timeout, deadlock, SqlException), `SqlServiceClient` always throws `ServiceException` and never propagates `SqlException` or `TimeoutException`
    - File: `PosWebApp.Tests/Unit/Properties/ServiceClientPropertyTests.cs`
    - **Validates: Requirements 19.1, 19.2**

- [x] 5. Implement Product_Catalog service and Cache_Manager
  - [x] 5.1 Implement ProductCatalogService
    - Implement `IProductCatalogService` with all methods using cache-first strategy via `IMemoryCache`
    - Apply cache key constants from `CacheKeys.cs`; use 1-hour sliding expiration for all catalog data
    - On cache miss, call `IServiceClient` methods to fetch from stored procedures: `GetCategoriesAsync`, `GetGroupsAsync`, `GetSaleItemsAsync`, `GetItemDetailAsync`, `GetItemUnitsAsync`
    - Implement `SearchItemsAsync(string query, int page, int pageSize)`: call `IServiceClient.SearchItemsAsync(keyword, page, pageSize)` — minimum 2 characters enforced, apply pagination with 50 items per page
    - Implement `InvalidateCatalogCacheAsync()` for cache eviction
    - **No EF Core, no inline SQL — all data comes from IServiceClient via stored procedures**
    - _Requirements: 3.1, 3.2, 3.3, 4.1, 4.2, 4.3, 4.4, 4.5, 10.2, 10.3, 10.7, 16.2, 16.3_

  - [x]* 5.2 Write property test for cache round-trip integrity (Property 6)
    - **Property 6: Cache Round-Trip Integrity** — for any catalog entity stored in `IMemoryCache`, a read with the same key before expiry returns a value with deep equality to what was stored
    - File: `PosWebApp.Tests/Unit/Properties/CachePropertyTests.cs`
    - **Validates: Requirements 3.1, 3.2, 3.3, 4.5**

  - [x]* 5.3 Write property test for search relevance (Property 8)
    - **Property 8: Search Result Relevance** — for any query of 2+ characters, every item in the result set has `ItemName` or `ItemCode` containing the query string (case-insensitive); calls `ProductCatalogService.SearchPure` static method with a mock item list
    - File: `PosWebApp.Tests/Unit/Properties/SearchPropertyTests.cs`
    - **Validates: Requirements 10.2, 10.3**

- [x] 6. Implement Cart service and session management
  - [x] 6.1 Implement CartService
    - Implement `ICartService` with all methods: `GetCart`, `AddItem`, `UpdateQuantity`, `RemoveItem`, `ClearCart`, `CalculateTotal`, `GetItemCount`
    - Use `SessionExtensions` for typed JSON serialization into ASP.NET Core session
    - Call `PriceTierResolver.Resolve` on every add/update to assign correct unit price
    - Implement `AddItemPure` static method (no session I/O) for testability
    - Auto-update unit price when quantity crosses a `Price_Tier` threshold
    - Accept `IHubContext<CartHub>` and call `NotifyCartUpdated` after each cart mutation
    - _Requirements: 6.1, 6.2, 6.3, 6.5, 6.6, 6.7, 6.9, 6.10, 6.13_

  - [x]* 6.2 Write property test for cart quantity accumulation (Property 3)
    - **Property 3: Cart Quantity Accumulation** — for any sequence of add-to-cart increments on the same item, the final quantity equals the arithmetic sum of all individual increments
    - File: `PosWebApp.Tests/Unit/Properties/CartPropertyTests.cs`
    - **Validates: Requirements 6.1, 6.2**

  - [x]* 6.3 Write property test for cart total correctness (Property 5)
    - **Property 5: Cart Total Correctness** — for any `CartState`, `Total` equals the exact sum of each item's `LineTotal = UnitPrice × (1 − Discount/100) × Quantity`, holding after every mutation
    - File: `PosWebApp.Tests/Unit/Properties/CartPropertyTests.cs`
    - **Validates: Requirements 6.8, 17.7**

  - [x]* 6.4 Write property test for discount price invariant (Property 13)
    - **Property 13: Discount Price Invariant** — for any price `P > 0` and discount `D` where `0 ≤ D ≤ 100`, discounted price equals `P × (1 − D/100)`, is ≤ P, and a discount of 0 yields the original price exactly
    - File: `PosWebApp.Tests/Unit/Properties/CartPropertyTests.cs`
    - **Validates: Requirements 30.1, 30.2, 30.3**

  - [x]* 6.5 Write unit tests for CartService edge cases
    - Test `RemoveItem` on a non-existent item is a no-op
    - Test `ClearCart` produces an empty `CartState`
    - Test quantity update to 0 removes the item
    - File: `PosWebApp.Tests/Unit/Services/CartServiceTests.cs`
    - _Requirements: 6.7_

- [x] 7. Checkpoint — Ensure all tests pass, ask the user if questions arise.

- [x] 8. Build home page (category browsing) and shared layout
  - [x] 8.1 Create _Layout and shared partials
    - Create `Pages/Shared/_Layout.cshtml` with Bootstrap 5 CDN, viewport meta tag, Khmer font links (Nokora/Battambang), skip navigation link, and ARIA landmarks (`<nav>`, `<main>`)
    - Create `Pages/Shared/_NavbarPartial.cshtml` with staff name display, cart icon + badge (served from session), language selector, and logout button
    - Create `Pages/Shared/_ItemCardPartial.cshtml` for reusable product card with `loading="lazy"`, alt text, and color-coded background
    - Configure Bootstrap 5 responsive grid (2 columns mobile → 6 columns desktop) via `col-6 col-sm-4 col-md-3 col-lg-2`
    - Apply touch-friendly button sizes (min 44×44 px) via CSS
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5, 14.6, 14.8, 20.2, 20.4, 20.8, 22.2, 22.10_

  - [x] 8.2 Create Home (Index) Razor Page — category grid
    - Implement `Index.cshtml` / `Index.cshtml.cs` with `OnGetAsync` calling `IProductCatalogService.GetCategoriesAsync()`
    - Build `IndexViewModel` with `CategoryViewModel` list; apply color array from `appsettings.json` via `IOptions<CatalogOptions>`
    - Render category grid using `_ItemCardPartial` with responsive Bootstrap grid
    - Add "Quick Sale" button linking to quick sale page
    - _Requirements: 3.1, 3.4, 3.5, 3.6, 3.7, 33.1_

  - [x] 8.3 Create Groups and Items Razor Pages
    - Implement `Groups.cshtml` / `.cs` for `/Groups/{categoryId}` route — `OnGetAsync` calls `IProductCatalogService.GetGroupsAsync(categoryId)`
    - Implement `Items.cshtml` / `.cs` for `/Items/{categoryId}/{groupId?}` — `OnGetAsync` calls `GetItemsAsync` or `GetSaleItemsAsync` based on whether `groupId` is present
    - Render groups/items using `_ItemCardPartial`; apply color-coded backgrounds from color array
    - Handle click navigation to items page or to item detail modal
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.6, 4.7, 4.8_

  - [x] 8.4 Create ImageTagHelper and Image_Optimizer service
    - Implement `IImageOptimizerService` with `GetOptimizedAsync` and `BuildSrcSet`
    - Processing pipeline: check `IMemoryCache` → fetch from `hdImage_all.ashx` via `IServiceClient.GetImageStreamAsync` → resize with `SixLabors.ImageSharp` → encode WebP (JPEG fallback) → cache with 24-hour absolute expiration
    - Set `Cache-Control: public, max-age=604800` and `ETag` (SHA256) on responses
    - Limit concurrency with `SemaphoreSlim(5)`
    - Create `TagHelpers/ImageTagHelper.cs` emitting `<img>` with `srcset`, `loading="lazy"`, `onerror` fallback to `/images/placeholder.png`, and `alt` attribute
    - Register `/Images/{*path}` route handler (minimal API or middleware) calling `IImageOptimizerService`
    - _Requirements: 3.8, 3.9, 4.9, 5.7, 5.8, 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 12.8, 12.9, 12.10, 12.11, 16.4, 16.6_

  - [x]* 8.5 Write property test for image dimension invariant (Property 9)
    - **Property 9: Image Dimension Invariant** — for any source image and requested `(W, H)`, the output image has `outputWidth ≤ W AND outputHeight ≤ H` and is encoded in WebP format
    - File: `PosWebApp.Tests/Unit/Properties/ImagePropertyTests.cs`
    - **Validates: Requirements 12.4, 12.5, 12.6**

- [x] 9. Implement Item Detail Modal and add-to-cart AJAX flow
  - [x] 9.1 Create _ItemDetailModal partial and AJAX detail handler
    - Create `Pages/Shared/_ItemDetailModal.cshtml` displaying `ItemName`, `ItemCode`, full-resolution `<img srcset>`, price tiers with USD/KHR formatting, quantity controls (+1, +5, +10, +50), current cart quantity
    - Implement `OnGetDetailAsync(int itemId)` handler on `Items` PageModel returning the modal partial as a PartialViewResult
    - Pass `ItemDetailViewModel` via `ViewData`
    - Show Bootstrap toast notification on successful add
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8, 5.9, 5.10, 5.11_

  - [x] 9.2 Implement Cart AJAX endpoint (`/Cart/Add`)
    - Implement `OnPostAddAsync` on `Cart` PageModel: validate `AddToCartInputModel`, call `ICartService.AddItem`, broadcast SignalR notification, return JSON `{ success, newQty, badgeCount }`
    - Implement `OnPostUpdateAsync` for quantity updates without full page reload
    - Write `wwwroot/js/cart.js` to handle add-to-cart AJAX, update item quantity badge, and show Bootstrap toast
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.11, 6.12, 7.4_

- [x] 10. Implement SignalR CartHub for real-time badge updates
  - Create `Hubs/CartHub.cs` with `[Authorize]` and `NotifyCartUpdated(int itemCount)` broadcasting to `Clients.User()`
  - Register hub in `Program.cs` at `/cartHub`
  - Add JavaScript in `site.js`: establish `HubConnection`, listen for `CartUpdated` event to update badge, fall back to 10-second polling if connection fails
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

- [x] 11. Implement Cart View and Cart Management Page
  - Create `Pages/Cart.cshtml` / `.cs` with `OnGetAsync` loading `CartViewModel` from `ICartService`
  - Create `Pages/Shared/_CartItemPartial.cshtml` for individual cart rows (ItemName, ItemCode, qty, unit price, line total)
  - Implement quantity adjustment form controls (Tag_Helper bound) with AJAX update via `cart.js`
  - Implement `OnPostRemoveAsync(int itemId)` and `OnPostClearAsync()` handlers with `[ValidateAntiForgeryToken]`
  - Display grand total in USD and KHR using `CurrencyFormatter`
  - Display "Cart is empty" message with browse link when `CartState` is empty
  - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5, 17.6, 17.7, 17.8, 17.9, 17.10, 17.11, 17.12_

- [x] 12. Implement Sale_Manager and Checkout flow
  - [x] 12.1 Implement SaleManagerService
    - Implement `ISaleManagerService` with `ProcessSaleAsync`, `GetSaleHistoryAsync`, `GetSaleDetailAsync`, `VoidSaleAsync`
    - `ProcessSaleAsync`: validate cart non-empty, call `IServiceClient.CreateSaleAsync(request)`, on success clear cart and return `SaleResult`, on failure wrap in `ModelState` error
    - `GetSaleHistoryAsync(SaleHistoryQuery query)`: call `IServiceClient.GetSaleHistoryAsync(query)` — pass `StaffUserID` in query when `UserLevel='Staff'` (Backend_Service filters to own sales), omit `StaffUserID` for Manager/Admin (Backend_Service returns all sales); return `PagedResult<SaleListItemDto>`
    - `GetSaleDetailAsync(int saleId)`: call `IServiceClient.GetSaleDetailAsync(saleId)`, return `SaleDetailDto`
    - `VoidSaleAsync(int saleId, string reason, int staffUserId)`: call `IServiceClient.VoidSaleAsync(saleId, reason, staffUserId)`; validate non-empty reason before calling
    - **No EF Core, no LINQ Include/ThenInclude, no audit log DbSet — all operations go through IServiceClient**
    - _Requirements: 8.1, 8.2, 8.3, 8.5, 8.6, 8.8, 8.9, 9.1, 9.2, 9.3, 9.4, 9.5, 9.8, 29.3, 29.4, 29.5, 29.6, 29.7, 29.9, 29.10_

  - [x] 12.2 Create Checkout Razor Pages
    - `Pages/Checkout.cshtml` / `.cs` — `OnGetAsync` loads cart and payment method selection (Cash/Card/Other); `OnPostAsync` calls `ISaleManagerService.ProcessSaleAsync`, redirects to confirmation on success or re-renders with `ModelState` errors
    - `OnGetConfirmationAsync` — displays `SaleID`, `InvoiceNo`, payment method, change amount, print button
    - Implement payment method form: Cash shows amount-tendered field + change calculation; Card shows card type + last 4 digits
    - Include optional customer lookup dropdown (AJAX autocomplete via `IServiceClient.SearchCustomersAsync`)
    - _Requirements: 8.1, 8.2, 8.4, 8.7, 8.10, 27.1, 27.2, 27.3, 27.4, 27.5, 27.6, 27.7, 27.8, 27.9, 32.1, 32.2, 32.3, 32.4, 32.5_

  - [x]* 12.3 Write unit tests for SaleManagerService
    - Test `GetSaleHistoryAsync` with `UserLevel='Staff'` passes `StaffUserID` in `SaleHistoryQuery` to mock `IServiceClient`
    - Test `GetSaleHistoryAsync` with `UserLevel='Manager'` passes no `StaffUserID` to mock `IServiceClient`
    - Test `VoidSaleAsync` with empty reason throws `CartValidationException` before calling `IServiceClient`
    - Mock `IServiceClient` to verify correct method and parameters are called
    - File: `PosWebApp.Tests/Unit/Services/SaleManagerServiceTests.cs`
    - _Requirements: 9.1, 9.2, 9.3, 29.3, 29.9_

- [x] 13. Implement property-based test for role-based sale history visibility (Property 7)
  - [x]* 13.1 Write property test for role-based sale history (Property 7)
    - **Property 7: Role-Based Sale History Visibility** — for any Staff user, `GetSaleHistoryAsync` call to `IServiceClient` includes the staff member's `UserID` in `SaleHistoryQuery.StaffUserID`; for Manager/Admin, `SaleHistoryQuery.StaffUserID` is null
    - File: `PosWebApp.Tests/Unit/Properties/SalePropertyTests.cs`
    - **Validates: Requirements 9.1, 9.2, 9.3**

- [x] 14. Implement Sale History, Sale Detail, and Void Pages
  - Create `Pages/Sale/Index.cshtml` / `.cs` — `OnGetAsync` calls `ISaleManagerService.GetSaleHistoryAsync`; render Bootstrap table with `SaleID`, `InvoiceNo`, `CreatedDate`, staff name, customer name, total; server-side pagination using `X.PagedList`; date range / customer / staff filters (Manager/Admin only); Tag_Helper pagination controls
  - Create `Pages/Sale/Detail.cshtml` / `.cs` — `OnGetAsync` calls `ISaleManagerService.GetSaleDetailAsync(saleId)` to load `SaleDetailDto` with items; display sale items table; show void button for Manager/Admin with `[Authorize(Roles="Manager,Admin")]`; `OnPostVoidAsync` calls `VoidSaleAsync` with confirmation dialog and reason input
  - Display "VOIDED" status indicator on voided sales; show voided-by, date, reason from `SaleDetailDto`
  - _Requirements: 9.5, 9.6, 9.7, 9.8, 9.9, 9.10, 9.11, 9.12, 28.6, 29.1, 29.2, 29.8, 29.11_

- [x] 15. Checkpoint — Ensure all tests pass, ask the user if questions arise.

- [x] 16. Implement Product Search Page
  - Create `Pages/Search.cshtml` / `.cs` — `OnGetAsync` calls `IProductCatalogService.SearchItemsAsync`; render results in same grid as category browsing; display "No items found" on empty results; redirect to home on cleared search
  - Implement `OnGetAutocompleteAsync(string q)` returning JSON array of item name/code suggestions (min 2 chars)
  - Search form in `_NavbarPartial` using Tag_Helper for model binding; trigger autocomplete via `search.js`
  - _Requirements: 10.1, 10.4, 10.5, 10.6, 10.7, 10.9, 10.10_

- [x] 17. Implement Receipt Generation and Printing
  - Implement `ReceiptPrinterService` with `GenerateHtmlReceipt` (Razor-to-string rendering) and `GenerateTextReceipt`
  - Receipt content: business info from `appsettings.json`, `SaleID`, `InvoiceNo`, date/time, staff name, customer name, line items (name/qty/unit price/line total), subtotal, grand total in USD and KHR, Khmer Unicode via Nokora/Battambang fonts
  - Add `@media print` CSS in `_Layout.cshtml` to hide navigation/headers; format for 80mm thermal paper
  - Implement "Print" button on `Checkout/Confirmation` triggering `window.print()`; add PDF export via browser print-to-PDF
  - Add "Reprint" button on `Sale/Detail` for Manager/Admin roles
  - _Requirements: 8.10, 26.1, 26.2, 26.3, 26.4, 26.5, 26.6, 26.7, 26.8, 26.9, 26.10, 26.11, 26.12_

  - [x]* 17.1 Write property test for receipt completeness (Property 12)
    - **Property 12: Receipt Completeness** — for any `SaleDetailDto` with one or more `SaleItem` entries, the HTML receipt contains `InvoiceNo`, each item's `ItemName`, `Quantity`, `UnitPrice`, `LineTotal`, and the overall `Total`
    - File: `PosWebApp.Tests/Unit/Services/ReceiptPrinterTests.cs`
    - **Validates: Requirements 26.1, 26.2, 26.3**

- [x] 18. Implement Multi-Language Support (Localization)
  - Create `Resources/SharedResource.en-US.resx` and `SharedResource.km-KH.resx` with all UI strings
  - Configure `AddLocalization`, `RequestLocalizationMiddleware`, `CookieRequestCultureProvider` in `Program.cs`; support `en-US` and `km-KH`
  - Add language selector in `_NavbarPartial` — POST to `/Culture/Set` controller action, stores `.AspNetCore.Culture` cookie, redirects back
  - Inject `IStringLocalizer<SharedResource>` in all PageModels; fall back to English if Khmer translation missing
  - Apply Khmer Unicode fonts (Nokora, Battambang) in `site.css` using Google Fonts or self-hosted
  - Display `$` and `៛` symbols using `CurrencyFormatter` throughout all pages
  - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9, 13.10_

- [x] 19. Implement Role-Based Authorization, Discounts, and Permissions
  - Configure ASP.NET Core authorization policies for `Manager`, `Admin` roles in `Program.cs`
  - Apply `[Authorize(Roles="Manager,Admin")]` to restricted handlers: void sale, apply discount, end-of-day report
  - Add discount UI in `Cart.cshtml` for Manager/Admin — percentage and fixed amount discount fields; authorization password prompt for discounts > 20%
  - Implement discount logic in `CartService.ApplyDiscount`; validate 0–100% range; store discount type, amount, authorizing UserID
  - Display original price with strikethrough and discounted price in cart and receipts
  - Hide restricted UI elements (void button, discount controls) for Staff role using Razor `@if (User.IsInRole(...))`
  - Log unauthorized access attempts via `ILogger`
  - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.5, 28.6, 28.7, 28.8, 28.9, 28.10, 28.11, 30.1, 30.2, 30.3, 30.4, 30.5, 30.6, 30.7, 30.8, 30.9, 30.10, 30.11_

- [x] 20. Implement property-based test for role-based authentication (Property 1) and lockout (Property 2)
  - [x]* 20.1 Write property test for role-based authentication (Property 1)
    - **Property 1: Role-Based Authentication** — authentication succeeds iff `LoginResult.IsSuccess=true` AND `UserLevel IN ('Staff','Manager','Admin')` AND `IsLockedOut=false`; all other combinations result in no cookie issued
    - Mock `IServiceClient.LoginAsync` to return controlled `LoginResult` values
    - File: `PosWebApp.Tests/Integration/AuthenticationTests.cs`
    - **Validates: Requirements 1.2, 1.7**

  - [x]* 20.2 Write property test for account lockout (Property 2)
    - **Property 2: Account Lockout** — after 5 consecutive `IsSuccess=false` responses, the Backend_Service returns `IsLockedOut=true`; the `Login` page renders the locked message regardless of subsequent password correctness
    - Mock `IServiceClient.LoginAsync` to simulate lockout progression
    - File: `PosWebApp.Tests/Integration/AuthenticationTests.cs`
    - **Validates: Requirements 1.5, 1.6**

- [x] 21. Implement End-of-Day Reporting and Quick Sale Mode
  - [x] 21.1 Create End-of-Day Report Page
    - `Pages/Reports/EndOfDay.cshtml` / `.cs` — `[Authorize(Roles="Manager,Admin")]`, date range picker, `OnGetAsync` builds report data by calling `IServiceClient.GetSaleHistoryAsync` with `StartDate`, `EndDate`, and no `StaffUserID` filter
    - Report sections: total sales count, total by payment method, total discounts, voided sales, breakdown by staff, top 10 items by qty/revenue — all aggregated client-side from the `SaleListItemDto` list returned by `IServiceClient`
    - Implement PDF export (using `PdfSharpCore` or `QuestPDF`) and Excel export (using `ClosedXML`)
    - **No EF aggregation queries — data comes from IServiceClient.GetSaleHistoryAsync**
    - _Requirements: 31.1, 31.2, 31.3, 31.4, 31.5, 31.6, 31.7, 31.8, 31.9, 31.10, 31.11, 31.12, 31.13_

  - [x] 21.2 Create Quick Sale Mode Page
    - `Pages/QuickSale.cshtml` / `.cs` — simplified UI with barcode scanner input field (auto-submits on scan), running total, number pad quantity entry
    - Lookup item by `ItemCode` via `IProductCatalogService.SearchItemsAsync`; auto-add to cart
    - Enter key → skip to payment screen with Cash pre-selected; Escape key → exit to home
    - Auto-print receipt after payment recorded
    - _Requirements: 33.1, 33.2, 33.3, 33.4, 33.5, 33.6, 33.7, 33.8, 33.9, 33.10, 33.11_

- [x] 22. Implement Data Synchronization Background Service
  - Create `BackgroundServices/CatalogSyncService : BackgroundService` (IHostedService)
  - Execute sync every 15 minutes: call `IProductCatalogService.InvalidateCatalogCacheAsync()` and re-prime from `IServiceClient`
  - Implement distributed lock via `IDistributedCache` to prevent concurrent sync in multi-instance deployments
  - Log sync start, completion, errors; implement exponential backoff (3 attempts) on failure
  - Store `LastSyncTimestamp` in `IMemoryCache` (key `"sync:lastTimestamp"`) after each successful sync — **no database write**
  - Continue serving cached data on sync failure
  - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5, 21.6, 21.7, 21.8, 21.9, 21.10_

- [x] 23. Implement Configuration Management and IOptions pattern
  - Define strongly-typed options classes: `BackendServiceOptions`, `CacheOptions`, `AuthOptions`, `CatalogOptions`, `PaymentOptions`, `ImageOptions`
  - Register all with `services.Configure<T>()` and `IValidateOptions<T>` startup validation
  - Support environment variable overrides for containerized deployments
  - Support environment-specific files: `appsettings.Development.json`, `appsettings.Production.json`
  - **No `ConnectionStrings` section — POS_Web_App has no direct database connection**
  - Document sensitive values (Backend_Service URL, API keys) with placeholder; integrate Secret Manager in development
  - Log non-sensitive config at startup
  - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5, 25.6, 25.7, 25.8, 25.9, 25.10_

- [x] 24. Checkpoint — Ensure all tests pass, ask the user if questions arise.

- [x] 25. Implement Progressive Web App (PWA) Offline Capability
  - Create `wwwroot/manifest.webmanifest` with icons, theme color, `display: standalone`
  - Create `wwwroot/js/sw.js` service worker:
    - Cache-first strategy for static assets (HTML/CSS/JS shell)
    - Network-first strategy for dynamic data
    - Cache product catalog pages and images on visit (max 50MB image cache)
    - Serve cached pages when offline
    - Background Sync API for queued sale submissions
    - IndexedDB queue for pending checkouts
  - Implement `cart.js` offline cart using `localStorage` when `navigator.onLine` is false
  - Add online/offline status indicator in `_NavbarPartial` using `navigator.onLine` and `window.addEventListener('online'/'offline')`
  - Display "No internet connection. Sale will be queued for processing when online." on offline checkout attempt
  - Auto-process queued sales on reconnect with staff notification
  - Register service worker in `_Layout.cshtml`
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 11.11, 11.12, 11.13_

- [x] 26. Implement Security Hardening and Error Handling
  - Add security headers middleware: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Content-Security-Policy`; enforce HTTPS with `RequireHttpsAttribute`
  - Configure `UseExceptionHandler("/Error")` and `UseStatusCodePagesWithReExecute("/Error", "?statusCode={0}")`
  - Create `Pages/Error.cshtml` / `.cs` — generic user message for 500; "Not Found" for 404; never expose stack trace to browser
  - Validate all form inputs via `ModelState`; display errors inline with Tag_Helper validation messages and `<div asp-validation-summary>`
  - Apply `[ValidateAntiForgeryToken]` on all POST handlers
  - Encrypt session state in distributed cache
  - Configure `UseResponseCompression` with Gzip in `Program.cs`
  - _Requirements: 18.1, 18.2, 18.4, 18.5, 18.6, 18.7, 18.8, 18.9, 18.11, 19.3, 19.4, 19.6, 19.7, 19.8, 19.9, 19.10_

- [x] 27. Implement Accessibility Compliance (WCAG 2.1 AA)
  - Audit all Razor Pages: add `alt` attributes to all `<img>`, ARIA labels for icon-only buttons, ARIA live regions for dynamic content (cart badge, toast notifications)
  - Ensure logical tab order and visible focus indicators via CSS (`:focus-visible` outline)
  - Associate all form labels with inputs using `for` / `asp-for` Tag_Helper
  - Verify color contrast ratios ≥ 4.5:1 for normal text, ≥ 3:1 for large text; add text/icons alongside color indicators
  - Bootstrap modal: confirm focus trap and Escape-key dismissal
  - Add `<link rel="skip-navigation">` skip link at top of `_Layout.cshtml`
  - Support 200% text resize via relative units (`rem`, CSS `clamp()`) without content overflow
  - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5, 20.6, 20.7, 20.8, 20.9, 20.10, 20.11, 20.12, 14.10_

- [x] 28. Write integration and end-to-end tests
  - [x]* 28.1 Write end-to-end sale flow integration test
    - Use `WebApplicationFactory<Program>` with mocked `IServiceClient` (no in-memory SQLite, no EF Core): login (mock `LoginAsync`) → browse → add to cart → checkout → verify `CreateSaleAsync` called with correct parameters
    - File: `PosWebApp.Tests/Integration/SaleFlowTests.cs`
    - _Requirements: 8.1, 8.2, 8.3_

  - [x]* 28.2 Write Razor Page rendering tests
    - Use `WebApplicationFactory` to render login page (assert input fields present), home page (assert category grid), cart page (assert empty-cart message)
    - File: `PosWebApp.Tests/Integration/AuthenticationTests.cs` + `SaleFlowTests.cs`
    - _Requirements: 1.1, 17.10_

  - [x]* 28.3 Write ImageOptimizerService unit tests
    - Test semaphore throttling (max 5 concurrent), ETag generation, correct MIME type (`image/webp`), JPEG fallback
    - File: `PosWebApp.Tests/Integration/ImageOptimizerTests.cs`
    - _Requirements: 12.6, 12.11, 16.6_

- [x] 29. Final Checkpoint — Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for a faster MVP delivery
- **All data access goes through `IServiceClient` / `AsmxServiceClient` — no EF Core, no PosDbContext, no direct SQL connection**
- Property-based tests use **FsCheck** (via `FsCheck.Xunit`) with minimum 100 iterations per property
- Each task references specific requirements clauses for full traceability
- The design uses pure static methods (`PriceTierResolver.Resolve`, `CartService.AddItemPure`, `ProductCatalogService.SearchPure`) to maximize testability without session/HTTP dependencies
- Checkpoints at tasks 7, 15, 24, and 29 validate incremental progress
- The middleware pipeline order in task 1 is non-negotiable — changing it will cause auth/session bugs
- Sensitive configuration (Backend_Service URL, API secrets) must use Secret Manager (dev) and environment variables (prod); never commit to source control
- `LastSyncTimestamp` is stored in `IMemoryCache`, not in any database

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1"] },
    { "id": 1, "tasks": ["2", "2.1", "2.2"] },
    { "id": 2, "tasks": ["3.1", "4.1"] },
    { "id": 3, "tasks": ["3.2", "3.3", "4.2", "5.1"] },
    { "id": 4, "tasks": ["5.2", "5.3", "6.1"] },
    { "id": 5, "tasks": ["6.2", "6.3", "6.4", "6.5", "8.1"] },
    { "id": 6, "tasks": ["8.2", "8.4", "10"] },
    { "id": 7, "tasks": ["8.3", "8.5", "9.1"] },
    { "id": 8, "tasks": ["9.2", "11"] },
    { "id": 9, "tasks": ["12.1", "16"] },
    { "id": 10, "tasks": ["12.2", "12.3", "13.1"] },
    { "id": 11, "tasks": ["14", "17"] },
    { "id": 12, "tasks": ["17.1", "18", "20.1", "20.2"] },
    { "id": 13, "tasks": ["19", "21.1", "21.2", "22", "23"] },
    { "id": 14, "tasks": ["25", "26", "27"] },
    { "id": 15, "tasks": ["28.1", "28.2", "28.3"] }
  ]
}
```
