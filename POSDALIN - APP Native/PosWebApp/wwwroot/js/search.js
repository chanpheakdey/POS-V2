/**
 * search.js — Product search autocomplete
 *
 * Attaches an autocomplete dropdown to the navbar search input.
 * - Triggers at 2+ characters (Requirement 10.7)
 * - Debounces requests by 300 ms (Requirement 10.9)
 * - Calls GET /Search?handler=Autocomplete&q={query} (Requirement 10.9)
 * - Returns {id, name, code} suggestions
 *
 * Requirements: 10.7, 10.9, 10.10
 */
(function () {
    'use strict';

    var MIN_CHARS    = 2;
    var DEBOUNCE_MS  = 300;
    var MAX_RESULTS  = 10;

    var input        = document.getElementById('navbar-search-input');
    var dropdown     = document.getElementById('search-autocomplete-dropdown');

    // Guard: only activate when both elements are present (authenticated users).
    if (!input || !dropdown) return;

    var debounceTimer = null;
    var lastQuery     = '';
    var activeIndex   = -1;   // keyboard navigation index

    // ── Fetch suggestions ────────────────────────────────────────────────────

    function fetchSuggestions(query) {
        var url = '/Search?handler=Autocomplete&q=' + encodeURIComponent(query);

        fetch(url, { credentials: 'same-origin' })
            .then(function (resp) {
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                return resp.json();
            })
            .then(function (items) {
                renderDropdown(items.slice(0, MAX_RESULTS), query);
            })
            .catch(function (err) {
                console.warn('[search.js] Autocomplete fetch failed:', err);
                closeDropdown();
            });
    }

    // ── Render dropdown items ─────────────────────────────────────────────────

    function renderDropdown(items, query) {
        dropdown.innerHTML = '';

        if (items.length === 0) {
            closeDropdown();
            return;
        }

        var ul = document.createElement('ul');
        ul.className = 'list-unstyled mb-0';
        ul.setAttribute('role', 'listbox');
        ul.setAttribute('aria-label', 'Search suggestions');

        items.forEach(function (item, idx) {
            var li = document.createElement('li');
            li.className = 'search-suggestion-item px-3 py-2 d-flex align-items-center gap-2';
            li.setAttribute('role', 'option');
            li.setAttribute('aria-selected', 'false');
            li.setAttribute('data-index', String(idx));
            li.setAttribute('data-name', item.name);
            li.setAttribute('data-code', item.code || '');

            // Highlight matching text
            var highlighted = highlightMatch(item.name, query);

            li.innerHTML =
                '<span class="search-suggestion-name flex-grow-1">' + highlighted + '</span>' +
                '<span class="search-suggestion-code text-muted small">' +
                escapeHtml(item.code || '') +
                '</span>';

            li.addEventListener('mousedown', function (e) {
                // Use mousedown so blur on the input does not close dropdown first
                e.preventDefault();
                selectSuggestion(item.name);
            });

            ul.appendChild(li);
        });

        dropdown.appendChild(ul);
        activeIndex = -1;
        openDropdown();
    }

    // ── Highlight matching characters ─────────────────────────────────────────

    function highlightMatch(text, query) {
        if (!query) return escapeHtml(text);
        var idx = text.toLowerCase().indexOf(query.toLowerCase());
        if (idx === -1) return escapeHtml(text);
        return escapeHtml(text.substring(0, idx)) +
            '<mark class="search-highlight p-0">' +
            escapeHtml(text.substring(idx, idx + query.length)) +
            '</mark>' +
            escapeHtml(text.substring(idx + query.length));
    }

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // ── Dropdown open / close ─────────────────────────────────────────────────

    function openDropdown() {
        dropdown.classList.remove('d-none');
        dropdown.setAttribute('aria-hidden', 'false');
        input.setAttribute('aria-expanded', 'true');
    }

    function closeDropdown() {
        dropdown.classList.add('d-none');
        dropdown.setAttribute('aria-hidden', 'true');
        input.setAttribute('aria-expanded', 'false');
        activeIndex = -1;
    }

    // ── Select a suggestion ───────────────────────────────────────────────────

    function selectSuggestion(name) {
        input.value = name;
        closeDropdown();
        // Navigate to Search page with the chosen value
        var form = input.closest('form');
        if (form) form.submit();
    }

    // ── Keyboard navigation ───────────────────────────────────────────────────

    function getItems() {
        return dropdown.querySelectorAll('[role="option"]');
    }

    function setActive(index) {
        var items = getItems();
        items.forEach(function (el) {
            el.classList.remove('active');
            el.setAttribute('aria-selected', 'false');
        });

        if (index >= 0 && index < items.length) {
            items[index].classList.add('active');
            items[index].setAttribute('aria-selected', 'true');
            input.setAttribute('aria-activedescendant', 'suggestion-' + index);
        } else {
            input.removeAttribute('aria-activedescendant');
        }

        activeIndex = index;
    }

    input.addEventListener('keydown', function (e) {
        var items = getItems();
        var isOpen = !dropdown.classList.contains('d-none');

        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (!isOpen) return;
            setActive(Math.min(activeIndex + 1, items.length - 1));
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (!isOpen) return;
            setActive(Math.max(activeIndex - 1, -1));
        } else if (e.key === 'Enter') {
            if (isOpen && activeIndex >= 0 && items[activeIndex]) {
                e.preventDefault();
                var name = items[activeIndex].getAttribute('data-name');
                if (name) selectSuggestion(name);
            }
            // else: let the form submit normally
        } else if (e.key === 'Escape') {
            closeDropdown();
        }
    });

    // ── Input event — debounced fetch ─────────────────────────────────────────

    input.addEventListener('input', function () {
        var query = input.value.trim();

        clearTimeout(debounceTimer);

        // Req 10.7 — minimum 2 characters
        if (query.length < MIN_CHARS) {
            closeDropdown();
            lastQuery = '';
            return;
        }

        // Avoid redundant fetches for the same query
        if (query === lastQuery) return;

        lastQuery = query;

        // Req 10.9 — debounce ~300 ms
        debounceTimer = setTimeout(function () {
            fetchSuggestions(query);
        }, DEBOUNCE_MS);
    });

    // ── Close dropdown on outside click ──────────────────────────────────────

    document.addEventListener('click', function (e) {
        if (!input.contains(e.target) && !dropdown.contains(e.target)) {
            closeDropdown();
        }
    });

    // Close on input blur (delay so mousedown on suggestion fires first)
    input.addEventListener('blur', function () {
        setTimeout(closeDropdown, 150);
    });

    input.addEventListener('focus', function () {
        var query = input.value.trim();
        if (query.length >= MIN_CHARS && dropdown.children.length > 0) {
            openDropdown();
        }
    });

}());
