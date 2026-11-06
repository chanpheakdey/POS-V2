/**
 * checkout.js — Checkout page interactivity
 *
 * Handles:
 *  1. Payment method panel toggling (Cash/Card/Other)
 *  2. Live change-due calculation for Cash (Requirement 27.5)
 *  3. Customer AJAX autocomplete (Requirement 8.4)
 *
 * Reads window.__checkout for server-injected values:
 *   { total, khrRate, customerSearchUrl, initialMethod }
 */
(function () {
    'use strict';

    const cfg = window.__checkout || {};
    const TOTAL    = parseFloat(cfg.total)   || 0;
    const KHR_RATE = parseFloat(cfg.khrRate) || 4100;

    // ── Payment method toggling ────────────────────────────────────────────────

    const pmRadios        = document.querySelectorAll('input[data-pm]');
    const fieldsCash      = document.getElementById('pm-fields-cash');
    const fieldsCard      = document.getElementById('pm-fields-card');
    const fieldsOther     = document.getElementById('pm-fields-other');
    const amountInput     = document.getElementById('amount-tendered-input');

    function showPaymentFields(method) {
        const map = {
            Cash:  fieldsCash,
            Card:  fieldsCard,
            Other: fieldsOther,
        };
        Object.values(map).forEach(el => {
            if (el) el.classList.add('d-none');
        });
        const target = map[method];
        if (target) target.classList.remove('d-none');

        // Reset Cash-specific warnings when switching away.
        if (method !== 'Cash') {
            hideChangeDisplay();
        } else {
            // Recalculate if there's already a value.
            updateChangeDisplay();
        }
    }

    pmRadios.forEach(radio => {
        radio.addEventListener('change', () => {
            if (radio.checked) showPaymentFields(radio.value);
        });
    });

    // Initialise on page load using the server-bound value.
    showPaymentFields(cfg.initialMethod || 'Cash');

    // ── Live change calculation ────────────────────────────────────────────────

    const changeDisplay  = document.getElementById('change-display');
    const changeWarning  = document.getElementById('change-warning');
    const changeUsdSpan  = document.getElementById('change-usd');
    const changeKhrSpan  = document.getElementById('change-khr');

    function hideChangeDisplay() {
        if (changeDisplay) changeDisplay.classList.add('d-none');
        if (changeWarning) changeWarning.classList.add('d-none');
    }

    function formatUsd(amount) {
        return '$' + amount.toFixed(2);
    }

    function formatKhr(usdAmount) {
        const raw = Math.round(usdAmount * KHR_RATE);
        return raw.toLocaleString() + '៛';
    }

    function updateChangeDisplay() {
        if (!amountInput || !changeDisplay || !changeWarning) return;

        const tendered = parseFloat(amountInput.value);
        if (isNaN(tendered) || tendered <= 0) {
            hideChangeDisplay();
            return;
        }

        const change = tendered - TOTAL;
        if (change < 0) {
            changeDisplay.classList.add('d-none');
            changeWarning.classList.remove('d-none');
        } else {
            changeWarning.classList.add('d-none');
            changeDisplay.classList.remove('d-none');
            if (changeUsdSpan) changeUsdSpan.textContent = formatUsd(change);
            if (changeKhrSpan) changeKhrSpan.textContent = formatKhr(change);
        }
    }

    if (amountInput) {
        amountInput.addEventListener('input', updateChangeDisplay);
        // Run once on load in case the field has a model-bound value after a re-render.
        updateChangeDisplay();
    }

    // ── Customer autocomplete (AJAX) — Requirement 8.4 ────────────────────────

    const customerSearch   = document.getElementById('customer-search');
    const suggestions      = document.getElementById('customer-suggestions');
    const customerIdInput  = document.getElementById('customer-id-input');
    const selectedDisplay  = document.getElementById('customer-selected-display');
    const selectedName     = document.getElementById('customer-selected-name');
    const clearBtn         = document.getElementById('clear-customer-btn');

    let debounceTimer = null;

    function clearCustomer() {
        if (customerIdInput)    customerIdInput.value = '';
        if (customerSearch)     customerSearch.value  = '';
        if (selectedDisplay)    selectedDisplay.classList.add('d-none');
        if (suggestions) {
            suggestions.innerHTML = '';
            suggestions.classList.add('d-none');
        }
        if (customerSearch) {
            customerSearch.setAttribute('aria-expanded', 'false');
        }
    }

    function selectCustomer(id, name) {
        if (customerIdInput)  customerIdInput.value = id;
        if (customerSearch)   customerSearch.value  = name;
        if (selectedName)     selectedName.textContent = name;
        if (selectedDisplay)  selectedDisplay.classList.remove('d-none');
        if (suggestions) {
            suggestions.innerHTML = '';
            suggestions.classList.add('d-none');
        }
        if (customerSearch) {
            customerSearch.setAttribute('aria-expanded', 'false');
        }
    }

    function renderSuggestions(customers) {
        if (!suggestions) return;
        suggestions.innerHTML = '';

        if (!customers || customers.length === 0) {
            suggestions.classList.add('d-none');
            customerSearch && customerSearch.setAttribute('aria-expanded', 'false');
            return;
        }

        customers.forEach(c => {
            const li = document.createElement('li');
            li.className = 'list-group-item list-group-item-action d-flex justify-content-between align-items-center';
            li.setAttribute('role', 'option');
            li.setAttribute('tabindex', '0');
            li.setAttribute('aria-selected', 'false');
            li.dataset.id   = c.id;
            li.dataset.name = c.name;

            const nameSpan  = document.createElement('span');
            nameSpan.textContent = c.name;

            const phoneSpan = document.createElement('small');
            phoneSpan.className = 'text-muted';
            phoneSpan.textContent = c.phone || '';

            li.appendChild(nameSpan);
            li.appendChild(phoneSpan);

            li.addEventListener('click', () => selectCustomer(c.id, c.name));
            li.addEventListener('keydown', e => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    selectCustomer(c.id, c.name);
                }
            });

            suggestions.appendChild(li);
        });

        suggestions.classList.remove('d-none');
        customerSearch && customerSearch.setAttribute('aria-expanded', 'true');
    }

    async function searchCustomers(query) {
        if (!cfg.customerSearchUrl) return;
        try {
            const url  = cfg.customerSearchUrl + '&q=' + encodeURIComponent(query);
            const resp = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!resp.ok) return;
            const data = await resp.json();
            renderSuggestions(data);
        } catch (err) {
            // Network failure — silently swallow, don't block checkout.
            console.warn('Customer search failed:', err);
        }
    }

    if (customerSearch) {
        customerSearch.addEventListener('input', () => {
            const q = customerSearch.value.trim();
            clearTimeout(debounceTimer);
            if (q.length < 2) {
                if (suggestions) {
                    suggestions.innerHTML = '';
                    suggestions.classList.add('d-none');
                }
                customerSearch.setAttribute('aria-expanded', 'false');
                return;
            }
            debounceTimer = setTimeout(() => searchCustomers(q), 300);
        });

        // Hide suggestions when focus leaves the search area.
        customerSearch.addEventListener('blur', () => {
            // Delay so click on suggestion is processed first.
            setTimeout(() => {
                if (suggestions) suggestions.classList.add('d-none');
                customerSearch.setAttribute('aria-expanded', 'false');
            }, 200);
        });

        // Keyboard navigation within the suggestions list.
        customerSearch.addEventListener('keydown', e => {
            if (!suggestions || suggestions.classList.contains('d-none')) return;
            const items = suggestions.querySelectorAll('[role="option"]');
            if (!items.length) return;
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                items[0].focus();
            }
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', clearCustomer);
    }

    // Close suggestions when clicking outside.
    document.addEventListener('click', e => {
        if (!suggestions) return;
        if (!e.target.closest('#customer-search') &&
            !e.target.closest('#customer-suggestions')) {
            suggestions.classList.add('d-none');
        }
    });

})();
