/**
 * discount.js
 *
 * Handles the Manager/Admin discount section on Cart.cshtml.
 *
 * Features:
 * - Selecting an item from the row "%" button pre-selects the item in the panel.
 * - "Apply" submits an AJAX POST to OnPostApplyDiscountAsync.
 * - Discounts > 20% show an authorisation modal (Requirement 28.7).
 * - Updates the line total cell in the cart table with strikethrough original
 *   price and highlighted discounted price (Requirements 28.5, 28.6).
 *
 * Requirements: 28.1–28.9
 */

(function () {
    'use strict';

    // ── Anti-forgery token helper ─────────────────────────────────────────────
    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    // ── DOM references ────────────────────────────────────────────────────────
    const itemSelect       = document.getElementById('discount-item-select');
    const typeSelect       = document.getElementById('discount-type-select');
    const amountInput      = document.getElementById('discount-amount-input');
    const unitLabel        = document.getElementById('discount-unit-label');
    const applyBtn         = document.getElementById('apply-discount-btn');
    const alertBox         = document.getElementById('discount-alert');

    const authModal        = document.getElementById('discountAuthModal');
    const authUserIdInput  = document.getElementById('auth-user-id-input');
    const confirmAuthBtn   = document.getElementById('confirm-auth-btn');
    const authModalError   = document.getElementById('auth-modal-error');

    if (!itemSelect || !applyBtn) return; // discount section not rendered

    let bsAuthModal = authModal ? new bootstrap.Modal(authModal) : null;

    // ── Update unit label when discount type changes ──────────────────────────
    typeSelect.addEventListener('change', () => {
        if (typeSelect.value === 'Percentage') {
            amountInput.max = '100';
            unitLabel.textContent = '(0–100%)';
        } else {
            amountInput.removeAttribute('max');
            unitLabel.textContent = '(USD)';
        }
    });

    // ── Row-level "%" buttons pre-select the item ─────────────────────────────
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.apply-discount-row-btn');
        if (!btn) return;
        const itemId = btn.dataset.itemId;
        if (itemSelect && itemId) {
            itemSelect.value = itemId;
            // Scroll the discount section into view
            document.getElementById('discount-section')?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            amountInput.focus();
        }
    });

    // ── Show/hide alert ───────────────────────────────────────────────────────
    function showAlert(message, type) {
        alertBox.textContent = message;
        alertBox.className   = `alert alert-${type}`;
        alertBox.classList.remove('d-none');
    }
    function hideAlert() {
        alertBox.classList.add('d-none');
    }

    // ── Submit discount (with optional authorizingUserId) ─────────────────────
    function submitDiscount(authorizingUserId) {
        hideAlert();

        const itemId          = parseInt(itemSelect.value, 10);
        const discountPercent = parseFloat(amountInput.value);
        const discountType    = typeSelect.value;

        if (!itemId) {
            showAlert('Please select an item to discount.', 'warning');
            return;
        }
        if (isNaN(discountPercent) || discountPercent < 0) {
            showAlert('Please enter a valid discount amount (0 or greater).', 'warning');
            return;
        }
        if (discountType === 'Percentage' && discountPercent > 100) {
            showAlert('Percentage discount cannot exceed 100%.', 'warning');
            return;
        }

        const token = getAntiForgeryToken();

        fetch('?handler=ApplyDiscount', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token,
            },
            body: JSON.stringify({
                'DiscountInput.ItemId':          itemId,
                'DiscountInput.DiscountPercent': discountPercent,
                'DiscountInput.DiscountType':    discountType,
                'DiscountInput.AuthorizingUserId': authorizingUserId || 0,
            }),
        })
        .then(r => r.json().then(data => ({ status: r.status, data })))
        .then(({ status, data }) => {
            if (data.requiresAuth) {
                // Requirement 28.7 — show auth modal for > 20% discounts
                if (authModalError) {
                    authModalError.textContent = '';
                    authModalError.classList.add('d-none');
                }
                if (authUserIdInput) authUserIdInput.value = '';
                bsAuthModal?.show();
                return;
            }
            if (!data.success) {
                showAlert(data.error || 'Failed to apply discount.', 'danger');
                return;
            }
            // ── Update the cart row in the DOM ────────────────────────────────
            updateLineTotalCell(itemId, data);
            updateGrandTotal(data.grandTotalUsd, data.grandTotalKhr);
            showAlert(`Discount of ${discountPercent}% applied successfully.`, 'success');
        })
        .catch(() => showAlert('An error occurred. Please try again.', 'danger'));
    }

    // ── Main apply button ─────────────────────────────────────────────────────
    applyBtn.addEventListener('click', () => submitDiscount(0));

    // ── Auth modal confirm ────────────────────────────────────────────────────
    if (confirmAuthBtn) {
        confirmAuthBtn.addEventListener('click', () => {
            const uid = parseInt(authUserIdInput.value, 10);
            if (!uid || uid <= 0) {
                if (authModalError) {
                    authModalError.textContent = 'Please enter a valid UserID.';
                    authModalError.classList.remove('d-none');
                }
                return;
            }
            bsAuthModal?.hide();
            submitDiscount(uid);
        });
    }

    // ── DOM update helpers ─────────────────────────────────────────────────────

    /**
     * Updates the line-total cell for the given itemId with the discounted price
     * and strikethrough original.
     */
    function updateLineTotalCell(itemId, data) {
        const cell = document.getElementById(`line-total-${itemId}`);
        if (!cell) return;

        if (data.discount > 0) {
            cell.innerHTML =
                `<span class="text-muted text-decoration-line-through small d-block"
                       id="original-line-total-${itemId}"
                       aria-label="Original price ${data.originalLineTotalUsd}">
                    ${data.originalLineTotalUsd}
                 </span>
                 <span class="text-success" aria-label="Discounted price ${data.lineTotalUsd}">
                    ${data.lineTotalUsd}
                 </span>
                 <span class="badge bg-success ms-1 small"
                       title="${data.discount.toFixed(1)}% discount applied">
                    ${data.discount.toFixed(1)}%
                 </span>`;
        } else {
            cell.innerHTML = data.lineTotalUsd;
        }
    }

    /**
     * Updates the grand total cells.
     */
    function updateGrandTotal(totalUsd, totalKhr) {
        const grandTotal = document.getElementById('cart-grand-total');
        if (!grandTotal) return;
        grandTotal.innerHTML =
            `${totalUsd}<br /><small class="text-muted fw-normal">${totalKhr}</small>`;
    }

}());
