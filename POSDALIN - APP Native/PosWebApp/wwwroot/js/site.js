// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ── SignalR Cart Badge — Requirements 7.1 – 7.7 ──────────────────────────────
// Establishes a SignalR connection to /cartHub and listens for CartUpdated events
// to update the cart badge without a full page reload.  Falls back to 10-second
// interval polling via GET /Cart/Count when the SignalR connection is unavailable.
(function () {
    'use strict';

    // Only run for authenticated users (the badge element exists in the navbar).
    var badgeEl = document.getElementById('cart-badge');
    if (!badgeEl) return;

    // ── Badge update helper ──────────────────────────────────────────────────
    // Updates the visible badge text and ARIA attributes.  Hides the badge when
    // the count is zero (Req 17.9).
    // Also announces the update to screen readers via #cart-badge-live (Req 20.10).
    function updateBadge(count) {
        var n = parseInt(count, 10);
        if (isNaN(n) || n < 0) n = 0;

        badgeEl.textContent = n > 0 ? String(n) : '';
        badgeEl.setAttribute('data-cart-count', String(n));

        if (n > 0) {
            badgeEl.classList.remove('visually-hidden', 'bg-secondary');
            badgeEl.classList.add('bg-danger');
        } else {
            badgeEl.classList.add('visually-hidden', 'bg-secondary');
            badgeEl.classList.remove('bg-danger');
        }

        // Update the accessible label on the parent <a> (cart link).
        var cartLink = badgeEl.closest('a[aria-label]');
        if (cartLink) {
            cartLink.setAttribute(
                'aria-label',
                'Cart, ' + n + (n === 1 ? ' item' : ' items')
            );
        }

        // Requirement 20.10 — announce cart count change to screen readers.
        var liveRegion = document.getElementById('cart-badge-live');
        if (liveRegion) {
            liveRegion.textContent = n > 0
                ? 'Cart updated: ' + n + (n === 1 ? ' item' : ' items')
                : 'Cart is empty';
        }
    }

    // ── Polling fallback (Req 7.6) ────────────────────────────────────────────
    // A 10-second interval that fetches /Cart/Count when the SignalR connection
    // is not in a Connected state.
    var pollingInterval = null;

    function startPolling(connection) {
        if (pollingInterval !== null) return; // already running
        pollingInterval = setInterval(function () {
            // Only poll when SignalR is not connected (avoids duplicate updates).
            var isConnected = connection &&
                connection.state === signalR.HubConnectionState.Connected;
            if (isConnected) return;

            fetch('/Cart/Count', { credentials: 'same-origin' })
                .then(function (resp) {
                    if (!resp.ok) return;
                    return resp.json();
                })
                .then(function (data) {
                    if (data && typeof data.count === 'number') {
                        updateBadge(data.count);
                    }
                })
                .catch(function () {
                    // Silently ignore network errors in the fallback path.
                });
        }, 10000); // every 10 seconds (Req 7.6)
    }

    // Guard: signalR global must be present (loaded from CDN before site.js).
    if (typeof signalR === 'undefined') {
        // SignalR library not available — fall back to polling immediately.
        // Create a dummy object so startPolling's state check works.
        startPolling({ state: 'Disconnected' });
        return;
    }

    // ── SignalR hub connection (Req 7.1) ─────────────────────────────────────
    var connection = new signalR.HubConnectionBuilder()
        .withUrl('/cartHub')
        .withAutomaticReconnect([0, 2000, 5000, 10000]) // retry delays in ms
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    // ── CartUpdated event handler (Req 7.4) ──────────────────────────────────
    // Server sends: hub.Clients.User(userId).SendAsync("CartUpdated", itemCount)
    connection.on('CartUpdated', function (itemCount) {
        updateBadge(itemCount);
    });

    // Start polling fallback regardless of connection state so the badge stays
    // accurate if the connection degrades later.  The polling callback skips
    // the fetch whenever SignalR reports Connected (Req 7.6).
    startPolling(connection);

    // ── Start connection with retry (Req 7.1) ────────────────────────────────
    function startConnection() {
        connection.start()
            .catch(function (err) {
                // Connection failed on first attempt — automatic reconnect will
                // handle subsequent attempts; polling handles interim updates.
                console.warn('[CartHub] Initial connection failed:', err);
            });
    }

    startConnection();
}());
