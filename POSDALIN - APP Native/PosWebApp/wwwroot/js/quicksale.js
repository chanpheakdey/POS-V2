/**
 * quicksale.js — Barcode scanner, number pad, and cart management for Quick Sale mode.
 *
 * Requirements: 33.2, 33.3, 33.4, 33.5, 33.6, 33.7, 33.8, 33.11
 *
 * Expects window.__quickSale to be set by QuickSale.cshtml:
 *   {
 *     scanUrl:     string  — Razor Pages handler URL for /QuickSale?handler=Scan
 *     removeUrl:   string  — Razor Pages handler URL for /QuickSale?handler=Remove
 *     checkoutUrl: string  — '/Checkout'
 *     khrRate:     number  — KHR per USD exchange rate
 *   }
 */

(function () {
    'use strict';

    // ── Config ────────────────────────────────────────────────────────────────

    var cfg = window.__quickSale || {};

    // ── DOM references ────────────────────────────────────────────────────────

    var barcodeInput  = document.getElementById('qs-barcode');
    var scanBtn       = document.getElementById('qs-scan-btn');
    var exitBtn       = document.getElementById('qs-exit-btn');
    var checkoutBtn   = document.getElementById('qs-checkout-btn');
    var alertBox      = document.getElementById('qs-alert');
    var cartTbody     = document.getElementById('qs-cart-tbody');
    var emptyState    = document.getElementById('qs-empty-state');
    var tableWrapper  = document.getElementById('qs-table-wrapper');
    var totalUsdEl    = document.getElementById('qs-total-usd');
    var totalKhrEl    = document.getElementById('qs-total-khr');
    var itemCountEl   = document.getElementById('qs-item-count');
    var itemUnitsEl   = document.getElementById('qs-item-count-units');
    var qtyDisplay    = document.getElementById('qs-qty-display');
    var numpadBtns    = document.querySelectorAll('.qs-numpad-btn');
    var backspaceBtn  = document.getElementById('qs-numpad-backspace');
    var clearBtn      = document.getElementById('qs-numpad-clear');

    // ── Utility: read anti-forgery token ─────────────────────────────────────

    function getAntiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    // ── Utility: format USD and KHR on the client side ────────────────────────

    function formatUsd(amount) {
        return '$' + Number(amount).toFixed(2);
    }

    function formatKhr(amount) {
        var khr = Math.round(Number(amount) * (cfg.khrRate || 4100));
        return khr.toLocaleString('en-US') + '៛';
    }

    // ── Alert helpers ─────────────────────────────────────────────────────────

    var alertTimer = null;

    /**
     * Show a brief status message above the barcode input.
     * @param {string} message
     * @param {'success'|'danger'|'warning'|'info'} type
     * @param {number} [autoDismissMs=3000]
     */
    function showAlert(message, type, autoDismissMs) {
        if (!alertBox) return;
        clearTimeout(alertTimer);
        alertBox.className = 'alert alert-' + (type || 'info');
        alertBox.textContent = message;
        alertBox.classList.remove('d-none');

        if (autoDismissMs !== 0) {
            alertTimer = setTimeout(function () {
                alertBox.classList.add('d-none');
            }, autoDismissMs != null ? autoDismissMs : 3000);
        }
    }

    function hideAlert() {
        if (alertBox) alertBox.classList.add('d-none');
        clearTimeout(alertTimer);
    }

    // ── Number pad — Req 33.6 ─────────────────────────────────────────────────

    /** Returns the current numeric quantity (never less than 1). */
    function getQty() {
        var v = parseInt(qtyDisplay ? qtyDisplay.value : '1', 10);
        return isNaN(v) || v < 1 ? 1 : v;
    }

    /** Sets the quantity display, clamped to 1–999. */
    function setQty(val) {
        if (!qtyDisplay) return;
        var n = parseInt(val, 10);
        if (isNaN(n) || n < 1) n = 1;
        if (n > 999) n = 999;
        qtyDisplay.value = String(n);
    }

    // Digit buttons
    numpadBtns.forEach(function (btn) {
        btn.addEventListener('click', function () {
            if (!qtyDisplay) return;
            var current = qtyDisplay.value === '1' ? '' : qtyDisplay.value;
            var next = current + btn.dataset.digit;
            setQty(next);
        });
    });

    // Backspace button
    if (backspaceBtn) {
        backspaceBtn.addEventListener('click', function () {
            if (!qtyDisplay) return;
            var current = qtyDisplay.value;
            var next = current.length > 1 ? current.slice(0, -1) : '1';
            setQty(next);
        });
    }

    // Clear button
    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            setQty(1);
        });
    }

    // ── Cart rendering helpers ────────────────────────────────────────────────

    /** Refreshes empty-state vs. table visibility. */
    function syncCartVisibility() {
        var rows = cartTbody ? cartTbody.querySelectorAll('tr').length : 0;
        if (emptyState && tableWrapper) {
            if (rows === 0) {
                emptyState.classList.remove('d-none');
                tableWrapper.classList.add('d-none');
            } else {
                emptyState.classList.add('d-none');
                tableWrapper.classList.remove('d-none');
            }
        }
    }

    /** Adds or updates a row in the cart table. */
    function upsertCartRow(item) {
        if (!cartTbody) return;

        var existing = cartTbody.querySelector('tr[data-item-id="' + item.itemId + '"]');
        var html = buildRowHtml(item);

        if (existing) {
            existing.outerHTML = html;
        } else {
            cartTbody.insertAdjacentHTML('beforeend', html);
        }

        // Re-attach remove listener for the (possibly new) row
        var row = cartTbody.querySelector('tr[data-item-id="' + item.itemId + '"]');
        if (row) {
            var removeBtn = row.querySelector('.qs-remove-btn');
            if (removeBtn) attachRemoveListener(removeBtn);
        }

        syncCartVisibility();
    }

    function buildRowHtml(item) {
        return '<tr data-item-id="' + escapeHtml(item.itemId) + '">' +
            '<td class="small text-muted">' + escapeHtml(item.itemCode) + '</td>' +
            '<td class="fw-semibold small">' + escapeHtml(item.itemName) + '</td>' +
            '<td class="text-center small">' + escapeHtml(item.quantity) + '</td>' +
            '<td class="text-end small">' + escapeHtml(item.unitPriceUsd) + '</td>' +
            '<td class="text-end small fw-semibold">' + escapeHtml(item.lineTotalUsd) + '</td>' +
            '<td class="text-center">' +
                '<button type="button" class="btn btn-sm btn-outline-danger qs-remove-btn" ' +
                        'data-item-id="' + escapeHtml(item.itemId) + '" ' +
                        'aria-label="Remove ' + escapeHtml(item.itemName) + ' from cart">' +
                    '<i class="bi bi-trash" aria-hidden="true"></i>' +
                '</button>' +
            '</td>' +
        '</tr>';
    }

    function escapeHtml(val) {
        return String(val)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /** Updates the running total display. */
    function updateTotals(totalUsd, totalKhr, badgeCount) {
        if (totalUsdEl) totalUsdEl.textContent = totalUsd;
        if (totalKhrEl) totalKhrEl.textContent = totalKhr;

        // Update item count badges
        var rows   = cartTbody ? cartTbody.querySelectorAll('tr').length : 0;
        var units  = 0;
        if (cartTbody) {
            cartTbody.querySelectorAll('tr').forEach(function (row) {
                var qtyCell = row.cells[2];
                if (qtyCell) units += parseInt(qtyCell.textContent, 10) || 0;
            });
        }
        if (itemCountEl)  itemCountEl.textContent  = rows;
        if (itemUnitsEl)  itemUnitsEl.textContent   = units;

        // Update navbar badge (shared across pages)
        document.querySelectorAll('#cart-badge').forEach(function (el) {
            el.textContent = badgeCount;
            el.classList.toggle('d-none', badgeCount === 0);
        });
    }

    // ── Scan / add-to-cart — Req 33.3, 33.4 ──────────────────────────────────

    /**
     * Sends the current barcode value + quantity to the server scan handler.
     * On success, updates the cart table and running total.
     */
    function doScan() {
        if (!barcodeInput) return;
        var code = barcodeInput.value.trim();
        if (!code) {
            showAlert('Please enter or scan a barcode.', 'warning');
            barcodeInput.focus();
            return;
        }

        var qty = getQty();
        hideAlert();

        // Visual feedback: disable input during request
        barcodeInput.disabled = true;
        if (scanBtn) scanBtn.disabled = true;

        var body = new URLSearchParams();
        body.set('barcode', code);
        body.set('quantity', qty);
        body.set('__RequestVerificationToken', getAntiForgeryToken());

        fetch(cfg.scanUrl, {
            method:  'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body:    body.toString(),
        })
        .then(function (res) { return res.json(); })
        .then(function (data) {
            if (data.success) {
                upsertCartRow(data);
                updateTotals(data.totalUsd, data.totalKhr, data.badgeCount);
                showAlert(data.message || 'Item added.', 'success', 2000);
                barcodeInput.value = '';
                setQty(1);   // reset quantity to 1 after each scan
            } else {
                showAlert(data.error || 'Item not found.', 'danger');
            }
        })
        .catch(function () {
            showAlert('Network error. Please try again.', 'danger');
        })
        .finally(function () {
            barcodeInput.disabled = false;
            if (scanBtn) scanBtn.disabled = false;
            barcodeInput.focus();
        });
    }

    // ── Remove from cart ──────────────────────────────────────────────────────

    function doRemove(itemId) {
        var body = new URLSearchParams();
        body.set('itemId', itemId);
        body.set('__RequestVerificationToken', getAntiForgeryToken());

        fetch(cfg.removeUrl, {
            method:  'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body:    body.toString(),
        })
        .then(function (res) { return res.json(); })
        .then(function (data) {
            if (data.success) {
                var row = cartTbody && cartTbody.querySelector('tr[data-item-id="' + itemId + '"]');
                if (row) row.remove();
                syncCartVisibility();
                updateTotals(data.totalUsd, data.totalKhr, data.badgeCount);
            } else {
                showAlert(data.error || 'Failed to remove item.', 'danger');
            }
        })
        .catch(function () {
            showAlert('Network error. Please try again.', 'danger');
        });
    }

    /** Attaches a remove-click listener to a single remove button. */
    function attachRemoveListener(btn) {
        btn.addEventListener('click', function () {
            doRemove(this.dataset.itemId);
        });
    }

    // Attach listeners to any server-rendered remove buttons
    document.querySelectorAll('.qs-remove-btn').forEach(attachRemoveListener);

    // ── Scan button click ─────────────────────────────────────────────────────

    if (scanBtn) {
        scanBtn.addEventListener('click', doScan);
    }

    // ── Keyboard shortcuts — Req 33.3, 33.7, 33.8, 33.11 ────────────────────

    document.addEventListener('keydown', function (e) {
        // Enter in barcode field → scan — Req 33.3
        if (e.key === 'Enter' && document.activeElement === barcodeInput) {
            e.preventDefault();
            var code = barcodeInput.value.trim();
            if (code) {
                doScan();
            } else {
                // Req 33.7, 33.8 — Enter with empty barcode field → proceed to checkout
                goToCheckout();
            }
            return;
        }

        // Enter anywhere else (except inside a form control) → checkout — Req 33.7, 33.8
        if (e.key === 'Enter' &&
            document.activeElement !== barcodeInput &&
            !isFormControl(document.activeElement)) {
            e.preventDefault();
            goToCheckout();
            return;
        }

        // Escape → exit quick sale mode — Req 33.11
        if (e.key === 'Escape') {
            e.preventDefault();
            window.location.href = '/';
        }
    });

    /**
     * Navigates to the Checkout page with Cash pre-selected.
     * Req 33.8 — Cash is the default payment method in quick sale mode.
     */
    function goToCheckout() {
        window.location.href = cfg.checkoutUrl + '?paymentMethod=Cash';
    }

    /** Returns true if the element is an interactive form control (input, select, textarea, button). */
    function isFormControl(el) {
        if (!el) return false;
        var tag = el.tagName.toLowerCase();
        return tag === 'input' || tag === 'select' || tag === 'textarea' || tag === 'button';
    }

    // ── Checkout button — Req 33.7, 33.8 ─────────────────────────────────────

    if (checkoutBtn) {
        checkoutBtn.addEventListener('click', function (e) {
            // The anchor already has the correct href; just let it navigate.
            // Override href to always include paymentMethod=Cash
            this.href = cfg.checkoutUrl + '?paymentMethod=Cash';
        });
    }

    // ── Autofocus barcode input on page load ──────────────────────────────────

    if (barcodeInput) {
        barcodeInput.focus();
    }

    // ── Initial cart visibility ───────────────────────────────────────────────

    syncCartVisibility();

}());
