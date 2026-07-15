/* =============================================================================
   Noor AI sales assistant — storefront client
   -----------------------------------------------------------------------------
   Vanilla JS, no dependencies, no build step. It talks to /api/chat/* and renders
   what comes back.

   On XSS: every string that reaches the DOM goes through escapeHtml() FIRST, and
   only then through the markdown transforms. That ordering is the whole defence,
   and it is why it is safe to render markdown from the FAQ table (authored by an
   admin) and from the provider (one day, a language model): by the time the
   markdown rules run, there are no angle brackets left to form a tag with. The
   only HTML in the output is HTML this file wrote. Nothing is ever assigned from
   an untrusted string without escaping — there is no innerHTML = userText here.
   ========================================================================== */

(function () {
    'use strict';

    var root = document.getElementById('noor-ai');
    if (!root) return;

    var API = '/api/chat';
    var THEME_KEY = 'noor-ai-theme';

    // ---------------------------------------------------------------------
    // DOM
    // ---------------------------------------------------------------------

    var el = {
        launcher: document.getElementById('noor-ai-launcher'),
        panel: document.getElementById('noor-ai-panel'),
        close: document.getElementById('noor-ai-close'),
        theme: document.getElementById('noor-ai-theme'),
        newChat: document.getElementById('noor-ai-new'),
        historyToggle: document.getElementById('noor-ai-history-toggle'),
        history: document.getElementById('noor-ai-history'),
        historyList: document.getElementById('noor-ai-history-list'),
        clear: document.getElementById('noor-ai-clear'),
        messages: document.getElementById('noor-ai-messages'),
        empty: document.getElementById('noor-ai-empty'),
        welcome: document.getElementById('noor-ai-welcome'),
        suggestions: document.getElementById('noor-ai-suggestions'),
        form: document.getElementById('noor-ai-form'),
        input: document.getElementById('noor-ai-input'),
        send: document.getElementById('noor-ai-send')
    };

    // localised strings, handed over from Razor as data-* attributes
    var t = {
        thinking: root.dataset.tThinking,
        copy: root.dataset.tCopy,
        copied: root.dataset.tCopied,
        view: root.dataset.tView,
        cart: root.dataset.tCart,
        off: root.dataset.tOff,
        recommended: root.dataset.tRecommended,
        noHistory: root.dataset.tNohistory,
        clearConfirm: root.dataset.tClearconfirm,
        error: root.dataset.tError
    };

    var state = {
        open: false,
        busy: false,
        sessionId: null,
        loaded: false
    };

    // ---------------------------------------------------------------------
    // Utilities
    // ---------------------------------------------------------------------

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /* A link is only rendered if it points somewhere safe. Everything else
       (javascript:, data:, vbscript:) is dropped back to plain text — an admin
       writing a FAQ answer should not be able to script a shopper's page, even
       by accident, and a future language model certainly should not. */
    function safeUrl(url) {
        var trimmed = String(url || '').trim();
        if (/^(https?:)?\/\//i.test(trimmed)) return trimmed;
        if (/^\//.test(trimmed)) return trimmed;
        if (/^mailto:/i.test(trimmed)) return trimmed;
        return null;
    }

    function inlineMarkdown(escaped) {
        return escaped
            // `code`
            .replace(/`([^`]+)`/g, '<code>$1</code>')
            // **bold**
            .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
            // *italic* and _italic_
            .replace(/(^|[^*])\*([^*\n]+)\*(?!\*)/g, '$1<em>$2</em>')
            .replace(/(^|\s)_([^_\n]+)_/g, '$1<em>$2</em>')
            // [text](url)
            .replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, function (match, text, url) {
                var href = safeUrl(url);
                if (!href) return text;
                return '<a href="' + escapeHtml(href) + '" target="_blank" rel="noopener noreferrer">' + text + '</a>';
            });
    }

    /* Block-level markdown: paragraphs and bullet lists. Deliberately small —
       the assistant only ever emits these, and a fuller parser would be more
       surface area for no benefit. */
    function renderMarkdown(source) {
        var lines = escapeHtml(source).split('\n');
        var html = '';
        var listOpen = false;
        var paragraph = [];

        function flushParagraph() {
            if (!paragraph.length) return;
            html += '<p>' + inlineMarkdown(paragraph.join('<br>')) + '</p>';
            paragraph = [];
        }

        function closeList() {
            if (!listOpen) return;
            html += '</ul>';
            listOpen = false;
        }

        lines.forEach(function (line) {
            var trimmed = line.trim();
            var bullet = trimmed.match(/^[-*]\s+(.*)$/);

            if (bullet) {
                flushParagraph();
                if (!listOpen) { html += '<ul>'; listOpen = true; }
                html += '<li>' + inlineMarkdown(bullet[1]) + '</li>';
                return;
            }

            closeList();

            if (!trimmed) { flushParagraph(); return; }
            paragraph.push(trimmed);
        });

        closeList();
        flushParagraph();

        return html;
    }

    function antiForgeryToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function request(path, options) {
        options = options || {};

        var headers = { 'Accept': 'application/json' };
        if (options.body) headers['Content-Type'] = 'application/json';

        /* nopCommerce emits the antiforgery input on every page. The default
           header name ASP.NET Core validates is RequestVerificationToken, which
           is what lets a JSON body (rather than a form post) carry the token. */
        if (options.method && options.method !== 'GET') {
            headers['RequestVerificationToken'] = antiForgeryToken();
        }

        return fetch(API + path, {
            method: options.method || 'GET',
            headers: headers,
            credentials: 'same-origin',
            keepalive: !!options.keepalive,
            body: options.body ? JSON.stringify(options.body) : undefined
        }).then(function (response) {
            if (response.status === 204) return null;

            return response.json().catch(function () { return null; }).then(function (data) {
                if (!response.ok) {
                    var error = new Error((data && data.error) || t.error);
                    error.status = response.status;
                    throw error;
                }
                return data;
            });
        });
    }

    function scrollToEnd() {
        // rAF, so the scroll happens after the new node has been laid out
        requestAnimationFrame(function () {
            el.messages.scrollTop = el.messages.scrollHeight;
        });
    }

    function icon(path) {
        return '<svg viewBox="0 0 24 24" aria-hidden="true">' + path + '</svg>';
    }

    // ---------------------------------------------------------------------
    // Rendering
    // ---------------------------------------------------------------------

    function starIcon() {
        return icon('<path d="m12 2 3.1 6.3 6.9 1-5 4.9 1.2 6.8-6.2-3.3-6.2 3.3 1.2-6.8-5-4.9 6.9-1z"/>');
    }

    function renderProductCard(product) {
        var card = document.createElement('div');
        card.className = 'noor-ai__card';

        var media = '<div class="noor-ai__card-media">';
        if (product.pictureUrl) {
            media += '<img src="' + escapeHtml(product.pictureUrl) + '" alt="' + escapeHtml(product.pictureAlt || product.name) + '" loading="lazy">';
        }
        if (product.discountPercent > 0) {
            media += '<span class="noor-ai__badge">-' + product.discountPercent + '%</span>';
        }
        media += '</div>';

        var meta = '';
        if (product.price) {
            meta += '<span class="noor-ai__price">' + escapeHtml(product.price) + '</span>';
        }
        if (product.oldPrice) {
            meta += '<span class="noor-ai__price-old">' + escapeHtml(product.oldPrice) + '</span>';
        }
        if (product.reviewCount > 0) {
            meta += '<span class="noor-ai__rating">' + starIcon() + product.rating + '</span>';
        }
        if (product.stockStatus) {
            meta += '<span class="noor-ai__stock ' + (product.inStock ? 'noor-ai__stock--in' : 'noor-ai__stock--out') + '">' +
                escapeHtml(product.stockStatus) + '</span>';
        }

        var actions = '<a class="noor-ai__btn" href="' + escapeHtml(safeUrl(product.url) || '#') + '" data-product-id="' + product.id + '">' +
            escapeHtml(t.view) + '</a>';

        if (product.canAddToCart && product.addToCartUrl) {
            actions += '<button type="button" class="noor-ai__btn noor-ai__btn--primary" data-add-to-cart="' +
                escapeHtml(product.addToCartUrl) + '">' + escapeHtml(t.cart) + '</button>';
        }

        card.innerHTML =
            media +
            '<div class="noor-ai__card-body">' +
                '<span class="noor-ai__card-name">' + escapeHtml(product.name) + '</span>' +
                (product.shortDescription
                    ? '<span class="noor-ai__card-desc">' + escapeHtml(product.shortDescription) + '</span>'
                    : '') +
                '<span class="noor-ai__card-meta">' + meta + '</span>' +
                '<span class="noor-ai__card-actions">' + actions + '</span>' +
            '</div>';

        return card;
    }

    function renderComparison(comparison) {
        var wrap = document.createElement('div');
        wrap.className = 'noor-ai__compare';

        var head =
            '<thead><tr>' +
                '<th></th>' +
                '<th>' + escapeHtml(comparison.productA.name) + '</th>' +
                '<th>' + escapeHtml(comparison.productB.name) + '</th>' +
            '</tr></thead>';

        var body = '<tbody>';
        (comparison.rows || []).forEach(function (row) {
            // winner: -1 = A, 1 = B, 0 = neither (brand, or a spec we refuse to rank)
            body +=
                '<tr>' +
                    '<th>' + escapeHtml(row.label) + '</th>' +
                    '<td class="' + (row.winner === -1 ? 'noor-ai__win' : '') + '">' + escapeHtml(row.valueA) + '</td>' +
                    '<td class="' + (row.winner === 1 ? 'noor-ai__win' : '') + '">' + escapeHtml(row.valueB) + '</td>' +
                '</tr>';
        });
        body += '</tbody>';

        wrap.innerHTML =
            '<div class="noor-ai__compare-scroll"><table>' + head + body + '</table></div>' +
            (comparison.recommendation
                ? '<div class="noor-ai__compare-verdict">' + renderMarkdown(comparison.recommendation) + '</div>'
                : '');

        return wrap;
    }

    function renderStats(stats) {
        var grid = document.createElement('div');
        grid.className = 'noor-ai__plan';

        grid.innerHTML = stats.map(function (stat) {
            return '<div class="noor-ai__stat">' +
                '<div class="noor-ai__stat-value">' + escapeHtml(stat.value) + '</div>' +
                '<div class="noor-ai__stat-label">' + escapeHtml(stat.label) + '</div>' +
            '</div>';
        }).join('');

        return grid;
    }

    function renderPayload(container, payload) {
        if (!payload) return;

        if (payload.lightingPlan) {
            var plan = payload.lightingPlan;
            container.appendChild(renderStats([
                { value: plan.fixtureCount + '', label: plan.recommendedCategory || '' },
                { value: plan.wattsPerFixture + 'W', label: plan.totalWatts + 'W total' },
                { value: plan.colorTemperatureKelvin + 'K', label: plan.suggestedLayout || '' }
            ]));
        }

        if (payload.comparison) {
            container.appendChild(renderComparison(payload.comparison));
        }

        if (payload.products && payload.products.length) {
            var list = document.createElement('div');
            list.className = 'noor-ai__products';
            payload.products.forEach(function (product) {
                list.appendChild(renderProductCard(product));
            });
            container.appendChild(list);
        }

        if (payload.followUps && payload.followUps.length) {
            var chips = document.createElement('div');
            chips.className = 'noor-ai__chips';
            chips.style.justifyContent = 'flex-start';

            payload.followUps.forEach(function (text) {
                var chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'noor-ai__chip';
                chip.textContent = text;          // textContent, not innerHTML — no escaping needed, none possible
                chip.addEventListener('click', function () { submit(text); });
                chips.appendChild(chip);
            });

            container.appendChild(chips);
        }
    }

    function addMessage(message) {
        hideEmpty();

        var isUser = message.role === 'user';

        var wrap = document.createElement('div');
        wrap.className = 'noor-ai__msg ' + (isUser ? 'noor-ai__msg--user' : 'noor-ai__msg--bot');

        var bubble = document.createElement('div');
        bubble.className = 'noor-ai__bubble';

        if (isUser) {
            // the shopper's own words: never markdown, always plain text
            bubble.textContent = message.text;
        } else {
            bubble.innerHTML = renderMarkdown(message.text || '');
        }

        wrap.appendChild(bubble);

        if (!isUser) {
            renderPayload(wrap, message.payload);

            var copy = document.createElement('button');
            copy.type = 'button';
            copy.className = 'noor-ai__copy';
            copy.innerHTML = icon('<rect x="9" y="9" width="12" height="12" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>') +
                '<span>' + escapeHtml(t.copy) + '</span>';

            copy.addEventListener('click', function () {
                copyText(message.text || '', copy);
            });

            wrap.appendChild(copy);
        }

        el.messages.appendChild(wrap);
        scrollToEnd();
    }

    function copyText(text, button) {
        var done = function () {
            button.classList.add('is-copied');
            button.querySelector('span').textContent = t.copied;

            setTimeout(function () {
                button.classList.remove('is-copied');
                button.querySelector('span').textContent = t.copy;
            }, 1800);
        };

        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(done).catch(function () { fallbackCopy(text, done); });
        } else {
            // clipboard API needs HTTPS; a store still on HTTP should not lose the button
            fallbackCopy(text, done);
        }
    }

    function fallbackCopy(text, done) {
        var area = document.createElement('textarea');
        area.value = text;
        area.setAttribute('readonly', '');
        area.style.position = 'fixed';
        area.style.opacity = '0';
        document.body.appendChild(area);
        area.select();

        try { document.execCommand('copy'); done(); } catch (e) { /* nothing else to try */ }

        document.body.removeChild(area);
    }

    function showTyping() {
        var wrap = document.createElement('div');
        wrap.className = 'noor-ai__msg noor-ai__msg--bot';
        wrap.id = 'noor-ai-typing';
        wrap.setAttribute('aria-label', t.thinking);
        wrap.innerHTML = '<div class="noor-ai__typing"><span></span><span></span><span></span></div>';

        el.messages.appendChild(wrap);
        scrollToEnd();
    }

    function hideTyping() {
        var typing = document.getElementById('noor-ai-typing');
        if (typing) typing.remove();
    }

    function showError(text) {
        var error = document.createElement('div');
        error.className = 'noor-ai__error';
        error.textContent = text;

        el.messages.appendChild(error);
        scrollToEnd();
    }

    function hideEmpty() {
        if (el.empty) el.empty.hidden = true;
    }

    // ---------------------------------------------------------------------
    // Conversation
    // ---------------------------------------------------------------------

    function setBusy(busy) {
        state.busy = busy;
        root.classList.toggle('is-busy', busy);
        el.send.disabled = busy || !el.input.value.trim();
        el.input.disabled = busy;
    }

    function submit(text) {
        if (state.busy) return;

        var message = String(text || '').trim();
        if (!message) return;

        addMessage({ role: 'user', text: message });

        el.input.value = '';
        autoGrow();
        setBusy(true);
        showTyping();

        request('/send', {
            method: 'POST',
            body: { sessionId: state.sessionId, message: message }
        }).then(function (data) {
            hideTyping();
            state.sessionId = data.sessionId;
            addMessage(data.reply);
            loadHistoryList();
        }).catch(function (error) {
            hideTyping();
            showError(error.message || t.error);
        }).then(function () {
            setBusy(false);
            el.input.focus();
        });
    }

    function loadConversation(sessionId) {
        var query = sessionId ? '?sessionId=' + encodeURIComponent(sessionId) : '';

        return request('/history' + query).then(function (data) {
            el.messages.querySelectorAll('.noor-ai__msg, .noor-ai__error').forEach(function (node) {
                node.remove();
            });

            state.sessionId = data.sessionId || null;

            if (data.messages && data.messages.length) {
                data.messages.forEach(addMessage);
            } else if (el.empty) {
                el.empty.hidden = false;
            }

            renderHistoryList(data.sessions || []);
        });
    }

    function loadHistoryList() {
        return request('/history').then(function (data) {
            renderHistoryList(data.sessions || []);
        }).catch(function () { /* the history panel is a convenience; never block the chat on it */ });
    }

    function renderHistoryList(sessions) {
        el.historyList.innerHTML = '';

        if (!sessions.length) {
            var empty = document.createElement('li');
            empty.className = 'noor-ai__history-empty';
            empty.textContent = t.noHistory;
            el.historyList.appendChild(empty);
            return;
        }

        sessions.forEach(function (session) {
            var item = document.createElement('li');

            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'noor-ai__history-item' + (session.sessionId === state.sessionId ? ' is-active' : '');
            button.textContent = session.title || '…';

            button.addEventListener('click', function () {
                loadConversation(session.sessionId);
                el.history.hidden = true;
            });

            item.appendChild(button);
            el.historyList.appendChild(item);
        });
    }

    function loadSuggestions() {
        return request('/suggestions').then(function (data) {
            if (data.welcome) el.welcome.textContent = data.welcome;

            el.suggestions.innerHTML = '';
            (data.suggestions || []).forEach(function (text) {
                var chip = document.createElement('button');
                chip.type = 'button';
                chip.className = 'noor-ai__chip';
                chip.textContent = text;
                chip.addEventListener('click', function () { submit(text); });
                el.suggestions.appendChild(chip);
            });
        }).catch(function () { /* chips are optional */ });
    }

    // ---------------------------------------------------------------------
    // Panel
    // ---------------------------------------------------------------------

    function open() {
        state.open = true;
        root.classList.add('is-open');
        el.panel.hidden = false;
        el.launcher.setAttribute('aria-expanded', 'true');

        if (!state.loaded) {
            state.loaded = true;
            Promise.all([loadSuggestions(), loadConversation(null)]).catch(function () { /* handled per-call */ });
        }

        // on a phone the panel is the whole screen, so stop the page behind it scrolling
        if (window.matchMedia('(max-width: 560px)').matches) {
            document.body.style.overflow = 'hidden';
        }

        setTimeout(function () { el.input.focus(); }, 120);
    }

    function close() {
        state.open = false;
        root.classList.add('is-closing');
        root.classList.remove('is-open');
        el.launcher.setAttribute('aria-expanded', 'false');
        document.body.style.overflow = '';

        setTimeout(function () {
            el.panel.hidden = true;
            root.classList.remove('is-closing');
        }, 180);
    }

    function autoGrow() {
        el.input.style.height = 'auto';
        el.input.style.height = Math.min(el.input.scrollHeight, 116) + 'px';
    }

    function applyTheme(theme) {
        root.dataset.theme = theme;
        try { localStorage.setItem(THEME_KEY, theme); } catch (e) { /* private mode */ }
    }

    function initTheme() {
        var stored = null;
        try { stored = localStorage.getItem(THEME_KEY); } catch (e) { /* private mode */ }

        // no stored choice: follow the OS, the way a native app would
        var prefersDark = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        applyTheme(stored || (prefersDark ? 'dark' : 'light'));
    }

    // ---------------------------------------------------------------------
    // Events
    // ---------------------------------------------------------------------

    el.launcher.addEventListener('click', function () {
        state.open ? close() : open();
    });

    el.close.addEventListener('click', close);

    el.theme.addEventListener('click', function () {
        applyTheme(root.dataset.theme === 'dark' ? 'light' : 'dark');
    });

    el.newChat.addEventListener('click', function () {
        state.sessionId = null;
        el.messages.querySelectorAll('.noor-ai__msg, .noor-ai__error').forEach(function (node) { node.remove(); });
        if (el.empty) el.empty.hidden = false;
        el.history.hidden = true;
        el.input.focus();
    });

    el.historyToggle.addEventListener('click', function () {
        el.history.hidden = !el.history.hidden;
        if (!el.history.hidden) loadHistoryList();
    });

    el.clear.addEventListener('click', function () {
        if (!window.confirm(t.clearConfirm)) return;

        request('/history', { method: 'DELETE' }).then(function () {
            state.sessionId = null;
            el.messages.querySelectorAll('.noor-ai__msg, .noor-ai__error').forEach(function (node) { node.remove(); });
            if (el.empty) el.empty.hidden = false;
            renderHistoryList([]);
            el.history.hidden = true;
        }).catch(function (error) {
            showError(error.message || t.error);
        });
    });

    el.form.addEventListener('submit', function (event) {
        event.preventDefault();
        submit(el.input.value);
    });

    el.input.addEventListener('input', function () {
        autoGrow();
        el.send.disabled = state.busy || !el.input.value.trim();
    });

    el.input.addEventListener('keydown', function (event) {
        // Enter sends; Shift+Enter is a newline. On a phone Enter must stay a
        // newline, or the on-screen keyboard's return key becomes a send button.
        if (event.key !== 'Enter' || event.shiftKey) return;
        if (window.matchMedia('(max-width: 560px)').matches) return;

        event.preventDefault();
        submit(el.input.value);
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && state.open) close();
    });

    /* Card actions are bound once, here, rather than per card: the message list
       is rebuilt constantly (new answers, replayed history), and per-node
       listeners would leak with it. */
    el.messages.addEventListener('click', function (event) {
        var cartButton = event.target.closest('[data-add-to-cart]');
        if (cartButton) {
            var url = cartButton.getAttribute('data-add-to-cart');

            // reuse the storefront's own cart plumbing, so the flyout, the totals
            // and the notification all behave exactly as they do elsewhere
            if (window.AjaxCart && typeof window.AjaxCart.addproducttocart_catalog === 'function') {
                window.AjaxCart.addproducttocart_catalog(url);
            } else {
                window.location.href = url;
            }
            return;
        }

        var link = event.target.closest('[data-product-id]');
        if (link && state.sessionId) {
            // fire-and-forget: keepalive lets it survive the navigation that follows
            request('/track-view?sessionId=' + encodeURIComponent(state.sessionId) +
                '&productId=' + encodeURIComponent(link.getAttribute('data-product-id')),
                { method: 'POST', keepalive: true }
            ).catch(function () { /* analytics must never block a click-through */ });
        }
    });

    initTheme();
    autoGrow();
})();
