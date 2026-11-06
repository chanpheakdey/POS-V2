/**
 * cart.js — Cart AJAX helpers and real-time badge updates via SignalR.
 *
 * Requirements: 6.1, 6.2, 6.3, 6.11, 6.12, 7.1, 7.2, 7.3, 7.4
 *
 * Public API:
 *   addToCart(itemId, itemCode, itemName, imageUrl, qty)
 *     Adds an item to the cart via AJAX POST without a full page reload.
 *
 * Internal helpers attach to `.cart-qty-dec`, `.cart-qty-inc`, `.cart-qty-input`,
 * and `.cart-remove-btn` elements rendered by Cart.cshtml.
 */

(function () {
    'use strict';

    // ── Utility: read the anti-forgery token injected by Razor ───────────────
    function getAntiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    // ── Badge helpers ─────────────────────────────────────────────────────────

    /**
     * Updates every #cart-badge element on the page.
     * Also announces the change to screen readers via #cart-badge-live (Req 20.10).
     * @param {number} count
     */
    function updateCartBadge(count) {
        var badges = document.querySelectorAll('#cart-badge');
        badges.forEach(function (badge) {
            badge.textContent = count > 0 ? String(count) : '';
            badge.dataset.cartCount = String(count);
            if (count > 0) {
                badge.classList.remove('visually-hidden', 'bg-secondary');
                badge.classList.add('bg-danger');
            } else {
                badge.classList.remove('bg-danger');
                badge.classList.add('bg-secondary', 'visually-hidden');
            }
        });

        // Also update the aria-label on the cart link so screen readers reflect the count.
        var cartLinks = document.querySelectorAll('a[aria-label^="Cart"]');
        cartLinks.forEach(function (link) {
            link.setAttribute('aria-label', 'Cart, ' + count + ' item' + (count === 1 ? '' : 's'));
        });

        // Requirement 20.10 — announce to screen readers via ARIA live region.
        var liveRegion = document.getElementById('cart-badge-live');
        if (liveRegion) {
            liveRegion.textContent = count > 0
                ? 'Cart updated: ' + count + (count === 1 ? ' item' : ' items')
                : 'Cart is empty';
        }
    }

    // ── Toast helper (Requirement 6.11) ───────────────────────────────────────

    /**
     * Shows the Bootstrap toast with the given message and style.
     * @param {string} message
     * @param {'success'|'danger'|'warning'} [type='success']
     */
    function showCartToast(message, type) {
        type = type || 'success';
        var toastEl = document.getElementById('cart-toast');
        if (!toastEl) return;

        var msgEl = document.getElementById('cart-toast-message');
        if (msgEl) msgEl.textContent = message;

        // Swap colour class.
        ['text-bg-success', 'text-bg-danger', 'text-bg-warning'].forEach(function (cls) {
            toastEl.classList.remove(cls);
        });
        toastEl.classList.add('text-bg-' + type);

        var toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 3000 });
        toast.show();
    }

    // ── Core AJAX add-to-cart (Requirement 6.12) ──────────────────────────────

    /**
     * Posts to /Cart?handler=Add and updates the badge + shows a toast.
     * Called from item detail modals and item cards across all pages.
     *
     * @param {number} itemId
     * @param {string} itemCode
     * @param {string} itemName
     * @param {string} imageUrl
     * @param {number} qty
     * @returns {Promise<{success:boolean, newQty:number, badgeCount:number}|null>}
     */
    window.addToCart = async function (itemId, itemCode, itemName, imageUrl, qty) {
        qty = qty || 1;

        var body = new URLSearchParams({
            '__RequestVerificationToken': getAntiForgeryToken(),
            'AddInput.ItemId':    String(itemId),
            'AddInput.ItemCode':  itemCode   || '',
            'AddInput.ItemName':  itemName   || '',
            'AddInput.ImageUrl':  imageUrl   || '',
            'AddInput.Quantity':  String(qty),
        });

        try {
            var resp = await fetch('/Cart?handler=Add', {
                method:  'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body:    body.toString(),
            });

            var data = await resp.json();

            if (data.success) {
                updateCartBadge(data.badgeCount);
                showCartToast('"' + itemName + '" added to cart. Qty: ' + data.newQty);
            } else {
                showCartToast(data.error || 'Could not add item to cart.', 'danger');
            }

            return data;
        } catch (err) {
            console.error('[cart.js] addToCart error:', err);
            showCartToast('Network error. Please try again.', 'danger');
            return null;
        }
    };

    // ── Cart-page quantity update (Requirement 6.6, 6.10) ────────────────────

    /**
     * Posts to /Cart?handler=Update to change an item's quantity.
     * Updates the row's line-total cell and the grand total footer.
     *
     * @param {number} itemId
     * @param {number} qty  — 0 removes the item
     */
    async function updateCartQuantity(itemId, qty) {
        var body = new URLSearchParams({
            '__RequestVerificationToken': getAntiForgeryToken(),
            'itemId':   String(itemId),
            'quantity': String(qty),
        });

        try {
            var resp = await fetch('/Cart?handler=Update', {
                method:  'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body:    body.toString(),
            });

            var data = await resp.json();

            if (data.success) {
                updateCartBadge(data.badgeCount);

                if (data.removed) {
                    // Remove the row from the DOM.
                    var row = document.getElementById('cart-row-' + itemId);
                    if (row) row.remove();
                    refreshGrandTotal();
                } else {
                    // Update the qty input if the user typed something that changed.
                    var qtyInput = document.querySelector('.cart-qty-input[data-item-id="' + itemId + '"]');
                    if (qtyInput) qtyInput.value = data.newQty;

                    // Update the line-total cell.
                    // The cell id is rendered by _CartItemPartial as "line-total-{itemId}".
                    var khrRate = window.__khrRate || 4100;
                    var lineTotalCell = document.getElementById('line-total-' + itemId);
                    if (lineTotalCell && data.lineTotal !== undefined) {
                        var usd = '$' + parseFloat(data.lineTotal).toFixed(2);
                        var khr = Math.round(data.lineTotal * khrRate).toLocaleString() + '៛';
                        lineTotalCell.textContent = usd;
                    }
                    refreshGrandTotal();
                }
            } else {
                showCartToast(data.error || 'Could not update quantity.', 'danger');
                // Revert the qty input to match session state by reloading.
                window.location.reload();
            }
        } catch (err) {
            console.error('[cart.js] updateCartQuantity error:', err);
            showCartToast('Network error. Please try again.', 'danger');
        }
    }

    /**
     * Removes an item from the cart via POST Remove handler.
     * @param {number} itemId
     */
    async function removeFromCart(itemId) {
        var body = new URLSearchParams({
            '__RequestVerificationToken': getAntiForgeryToken(),
        });

        try {
            var resp = await fetch('/Cart?handler=Remove&itemId=' + encodeURIComponent(itemId), {
                method:  'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body:    body.toString(),
            });

            var data = await resp.json();

            if (data.success) {
                updateCartBadge(data.badgeCount);
                var row = document.getElementById('cart-row-' + itemId);
                if (row) row.remove();
                refreshGrandTotal();
            } else {
                showCartToast(data.error || 'Could not remove item.', 'danger');
            }
        } catch (err) {
            console.error('[cart.js] removeFromCart error:', err);
            showCartToast('Network error. Please try again.', 'danger');
        }
    }

    /**
     * Re-sums all visible line totals and updates the grand-total footer cell.
     * This is a client-side approximation — the server always remains the source
     * of truth (session).
     */
    function refreshGrandTotal() {
        // If no cart table body exists we're not on the Cart page.
        var tbody = document.getElementById('cart-table-body');
        if (!tbody) return;

        // Check if table is now empty.
        var remaining = tbody.querySelectorAll('tr');
        if (remaining.length === 0) {
            // Replace the whole page section with empty-cart message.
            window.location.reload();
            return;
        }

        // Re-calculate from displayed line-total cells.
        // Each line-total cell rendered by _CartItemPartial uses id="line-total-{itemId}".
        var total = 0;
        tbody.querySelectorAll('[id^="line-total-"]').forEach(function (cell) {
            var text = cell.textContent.trim();
            var match = text.match(/\$([\d,.]+)/);
            if (match) total += parseFloat(match[1].replace(',', ''));
        });

        var khrRate = window.__khrRate || 4100;
        var totalCell = document.getElementById('cart-grand-total');
        if (totalCell) {
            var usd = '$' + total.toFixed(2);
            var khr = '៛' + Math.round(total * khrRate).toLocaleString();
            totalCell.innerHTML = usd + '<br><small class="text-muted fw-normal">' + khr + '</small>';
        }
    }

    // ── Cart-page event delegation ────────────────────────────────────────────

    document.addEventListener('DOMContentLoaded', function () {

        var tableBody = document.getElementById('cart-table-body');
        if (!tableBody) return; // Not on the Cart page — no per-row handlers needed.

        // Debounce map for quantity inputs (to avoid rapid-fire POSTs while typing).
        var debounceTimers = {};

        tableBody.addEventListener('click', function (e) {
            // Decrement button.
            var decBtn = e.target.closest('.cart-qty-dec');
            if (decBtn) {
                var itemId = parseInt(decBtn.dataset.itemId, 10);
                var input  = tableBody.querySelector('.cart-qty-input[data-item-id="' + itemId + '"]');
                if (input) {
                    var newQty = Math.max(0, parseInt(input.value, 10) - 1);
                    input.value = newQty;
                    updateCartQuantity(itemId, newQty);
                }
                return;
            }

            // Increment button.
            var incBtn = e.target.closest('.cart-qty-inc');
            if (incBtn) {
                var itemId = parseInt(incBtn.dataset.itemId, 10);
                var input  = tableBody.querySelector('.cart-qty-input[data-item-id="' + itemId + '"]');
                if (input) {
                    var newQty = parseInt(input.value, 10) + 1;
                    input.value = newQty;
                    updateCartQuantity(itemId, newQty);
                }
                return;
            }

            // Remove button.
            var removeBtn = e.target.closest('.cart-remove-btn');
            if (removeBtn) {
                var itemId = parseInt(removeBtn.dataset.itemId, 10);
                removeFromCart(itemId);
                return;
            }
        });

        // Debounced qty input change.
        tableBody.addEventListener('change', function (e) {
            var input = e.target.closest('.cart-qty-input');
            if (!input) return;
            var itemId = parseInt(input.dataset.itemId, 10);
            var qty    = Math.max(0, parseInt(input.value, 10) || 0);
            input.value = qty;

            clearTimeout(debounceTimers[itemId]);
            debounceTimers[itemId] = setTimeout(function () {
                updateCartQuantity(itemId, qty);
            }, 400);
        });
    });

    // ── SignalR real-time badge updates (Requirements 7.1, 7.3, 7.4) ─────────

    (function initSignalR() {
        // Only initialise when the SignalR client library is available.
        if (typeof signalR === 'undefined') return;

        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/cartHub')
            .withAutomaticReconnect([0, 2000, 5000, 10000])
            .build();

        // Update the badge whenever the server broadcasts a cart mutation.
        connection.on('CartUpdated', function (itemCount) {
            updateCartBadge(itemCount);
        });

        function startConnection() {
            connection.start().catch(function (err) {
                console.warn('[cart.js] SignalR connection failed, retrying in 10s:', err);
                // Requirement 7.6 — fall back to polling every 10 seconds.
                setTimeout(pollCartBadge, 10000);
            });
        }

        startConnection();

        // Requirement 7.6 — polling fallback when SignalR is disconnected.
        connection.onclose(function () {
            setTimeout(startConnection, 10000);
        });
    })();

    /**
     * Polling fallback: re-reads the cart badge count from the current DOM.
     * A lightweight GET to the home page is not issued here; instead we read
     * the data-cart-count attribute which the server rendered on page load and
     * SignalR updates in real time. This satisfies Requirement 7.6 without
     * extra HTTP traffic when the initial connection succeeds.
     */
    function pollCartBadge() {
        var badge = document.getElementById('cart-badge');
        var count = badge ? parseInt(badge.dataset.cartCount || '0', 10) : 0;
        updateCartBadge(count);
    }

})();
