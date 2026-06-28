/* ============================================================================
   Noor — "Visualize Before You Buy" room visualizer
   Vanilla JS, no runtime dependencies (jQuery is only reused for the existing
   AjaxCart add-to-cart path). Pointer Events power drag / resize / rotate so the
   same code works for mouse and touch.
   ========================================================================== */
(function () {
    'use strict';

    var cfg = window.NoorVisualizeConfig || {};
    var PRODUCTS_URL = cfg.productsUrl || '/visualize/products';
    var ADD_TO_CART_BASE = cfg.addToCartUrlBase || '/addproducttocart/catalog/';
    var STORE_KEY = 'noor-visualize-designs-v1';
    var AUTOSAVE_KEY = 'noor-visualize-autosave-v1';

    // ----- small DOM / math helpers ------------------------------------------
    var $ = function (sel, root) { return (root || document).querySelector(sel); };
    var $$ = function (sel, root) { return Array.prototype.slice.call((root || document).querySelectorAll(sel)); };
    var clamp = function (v, lo, hi) { return Math.max(lo, Math.min(hi, v)); };
    var uid = (function () { var n = 0; return function () { return 'it' + (++n) + '_' + (performance.now() | 0); }; })();
    var rad = function (deg) { return deg * Math.PI / 180; };

    function debounce(fn, ms) {
        var t; return function () { var a = arguments, c = this; clearTimeout(t); t = setTimeout(function () { fn.apply(c, a); }, ms); };
    }

    // ----- engine state ------------------------------------------------------
    var els = {};
    var state = {
        bg: null,            // background data URL
        bgW: 0, bgH: 0,      // background natural size
        bgLum: 0.5,          // average background luminance (for lighting match)
        items: [],           // placed products
        lightDir: 135,       // global light azimuth (deg)
        zoom: 1, panX: 0, panY: 0,
        selected: null
    };
    var undoStack = [], redoStack = [];
    var picker = { term: '', categoryId: 0, page: 0, loading: false, hasNext: false };

    // ========================================================================
    //  INITIALISATION
    // ========================================================================
    function init() {
        els.app = $('#vzApp');
        if (!els.app) return;
        els.fileInput = $('#vzFile');
        els.viewport = $('#vzViewport');
        els.stage = $('#vzStage');
        els.canvas = $('#vzCanvas');
        els.bgImg = $('#vzBg');
        els.empty = $('#vzEmpty');
        els.zoomLabel = $('#vzZoomLabel');
        els.sidebar = $('#vzSidebar');
        els.search = $('#vzSearch');
        els.category = $('#vzCategory');
        els.grid = $('#vzGrid');
        els.loadMore = $('#vzLoadMore');
        els.gridStatus = $('#vzGridStatus');
        els.lightDir = $('#vzLight');
        els.modal = $('#vzModal');
        els.modalBody = $('#vzModalBody');
        els.modalTitle = $('#vzModalTitle');

        bindToolbar();
        bindStage();
        bindPicker();
        bindInspector();
        bindKeyboard();

        // observe canvas size so items stay proportional on resize / orientation
        if (window.ResizeObserver) {
            new ResizeObserver(function () { renderAllItems(); }).observe(els.canvas);
        } else {
            window.addEventListener('resize', renderAllItems);
        }

        loadProducts(true);
        restoreAutosave();
        updateUI();
    }

    // ========================================================================
    //  TOOLBAR
    // ========================================================================
    function bindToolbar() {
        on(els.app, 'click', '[data-vz]', function (e, t) {
            var act = t.getAttribute('data-vz');
            switch (act) {
                case 'upload': els.fileInput.click(); break;
                case 'undo': undo(); break;
                case 'redo': redo(); break;
                case 'zoomin': setZoom(state.zoom * 1.2); break;
                case 'zoomout': setZoom(state.zoom / 1.2); break;
                case 'zoomreset': resetView(); break;
                case 'save': saveDesignDialog(); break;
                case 'load': loadDesignDialog(); break;
                case 'download': downloadImage(); break;
                case 'share': shareImage(); break;
                case 'addall': addAllToCart(); break;
                case 'clear': clearAll(); break;
                case 'toggle-products': els.app.classList.toggle('vz-show-picker'); break;
                case 'close-picker': els.app.classList.remove('vz-show-picker'); break;
                case 'modal-close': closeModal(); break;
            }
        });

        els.fileInput.addEventListener('change', function () {
            if (this.files && this.files[0]) loadBackground(this.files[0]);
            this.value = '';
        });

        els.lightDir.addEventListener('input', function () {
            state.lightDir = parseInt(this.value, 10) || 0;
            renderAllItems();
        });
        els.lightDir.addEventListener('change', function () { commit(); });
    }

    // ========================================================================
    //  BACKGROUND PHOTO
    // ========================================================================
    function loadBackground(file) {
        var reader = new FileReader();
        reader.onload = function (e) {
            var img = new Image();
            img.onload = function () {
                // downscale large uploads for snappy editing + reasonable storage
                var max = 1600;
                var scale = Math.min(1, max / Math.max(img.width, img.height));
                var w = Math.round(img.width * scale), h = Math.round(img.height * scale);
                var c = document.createElement('canvas');
                c.width = w; c.height = h;
                var ctx = c.getContext('2d');
                ctx.drawImage(img, 0, 0, w, h);
                state.bg = c.toDataURL('image/jpeg', 0.88);
                state.bgW = w; state.bgH = h;
                state.bgLum = averageLuminance(ctx, w, h);
                applyBackground();
                commit();
                updateUI();
            };
            img.src = e.target.result;
        };
        reader.readAsDataURL(file);
    }

    function applyBackground() {
        if (state.bg) {
            els.bgImg.src = state.bg;
            els.canvas.style.aspectRatio = state.bgW + ' / ' + state.bgH;
            els.app.classList.add('vz-has-bg');
        } else {
            els.bgImg.removeAttribute('src');
            els.app.classList.remove('vz-has-bg');
        }
    }

    function averageLuminance(ctx, w, h) {
        try {
            var sw = 40, sh = Math.max(1, Math.round(40 * h / w));
            var tmp = document.createElement('canvas'); tmp.width = sw; tmp.height = sh;
            tmp.getContext('2d').drawImage(ctx.canvas, 0, 0, sw, sh);
            var d = tmp.getContext('2d').getImageData(0, 0, sw, sh).data, sum = 0, n = 0;
            for (var i = 0; i < d.length; i += 4) { sum += (0.2126 * d[i] + 0.7152 * d[i + 1] + 0.0722 * d[i + 2]); n++; }
            return (sum / n) / 255;
        } catch (e) { return 0.5; }
    }

    // ========================================================================
    //  PRODUCT PICKER
    // ========================================================================
    function bindPicker() {
        els.search.addEventListener('input', debounce(function () {
            picker.term = els.search.value.trim();
            loadProducts(true);
        }, 350));
        els.category.addEventListener('change', function () {
            picker.categoryId = parseInt(this.value, 10) || 0;
            loadProducts(true);
        });
        els.loadMore.addEventListener('click', function () { loadProducts(false); });

        els.grid.addEventListener('click', function (e) {
            var card = e.target.closest('.vz-pcard');
            if (!card) return;
            addProductToStage({
                id: parseInt(card.getAttribute('data-id'), 10),
                name: card.getAttribute('data-name'),
                price: card.getAttribute('data-price'),
                image: card.getAttribute('data-image'),
                requiresConfiguration: card.getAttribute('data-config') === '1'
            });
        });
    }

    function loadProducts(reset) {
        if (picker.loading) return;
        if (reset) { picker.page = 0; els.grid.innerHTML = ''; }
        else { picker.page++; }
        picker.loading = true;
        els.gridStatus.textContent = 'جارٍ التحميل…';
        els.loadMore.hidden = true;

        var url = PRODUCTS_URL + '?term=' + encodeURIComponent(picker.term) +
            '&categoryId=' + picker.categoryId + '&pageIndex=' + picker.page + '&pageSize=24';

        fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.json(); })
            .then(function (data) {
                picker.loading = false;
                picker.hasNext = !!data.hasNextPage;
                (data.products || []).forEach(function (p) { els.grid.appendChild(productCard(p)); });
                els.gridStatus.textContent = els.grid.children.length ? '' :
                    'لا توجد منتجات مطابقة';
                els.loadMore.hidden = !picker.hasNext;
            })
            .catch(function () {
                picker.loading = false;
                els.gridStatus.textContent = 'تعذّر تحميل المنتجات';
            });
    }

    function productCard(p) {
        var card = document.createElement('button');
        card.type = 'button';
        card.className = 'vz-pcard';
        card.setAttribute('data-id', p.id);
        card.setAttribute('data-name', p.name || '');
        card.setAttribute('data-price', p.price || '');
        card.setAttribute('data-image', p.imageUrl || '');
        card.setAttribute('data-config', p.requiresConfiguration ? '1' : '0');
        card.innerHTML =
            '<span class="vz-pcard__img"><img loading="lazy" src="' + esc(p.imageUrl) + '" alt=""></span>' +
            '<span class="vz-pcard__name">' + esc(p.name) + '</span>' +
            '<span class="vz-pcard__price">' + esc(p.price || '') + '</span>' +
            '<span class="vz-pcard__add">+ أضف للتصميم</span>';
        return card;
    }

    // ========================================================================
    //  PLACING PRODUCTS
    // ========================================================================
    function addProductToStage(p) {
        if (!state.bg) { toast('ارفع صورة الغرفة أولًا لتبدأ التصميم', 'error'); flashUpload(); return; }
        var img = new Image();
        img.crossOrigin = 'anonymous';
        img.onload = function () {
            var processed = removeBackground(img);   // {src, lum, natW, natH}
            var natW = processed.natW, natH = processed.natH;
            var ar = natH / natW;
            // start at ~28% of canvas width, centred a bit low (as if on the floor)
            var item = {
                id: uid(),
                productId: p.id,
                name: p.name,
                price: p.price,
                requiresConfiguration: !!p.requiresConfiguration,
                origSrc: p.image,
                origAr: img.naturalHeight / img.naturalWidth,
                procSrc: processed.src,
                procAr: ar,
                src: processed.src,
                bgRemoved: true,
                natW: natW, natH: natH, ar: ar,
                cx: 0.5, cy: 0.58,
                w: 0.28,
                rot: 0, flipX: false,
                opacity: 1,
                tilt: 0,             // perspective tilt toward floor (deg)
                shadow: true,
                matchLight: true,
                lum: processed.lum,
                z: nextZ()
            };
            state.items.push(item);
            buildItemEl(item);
            select(item.id);
            renderItem(item);
            commit();
            updateUI();
            els.app.classList.remove('vz-show-picker');
            toast('تمت إضافة "' + (p.name || 'المنتج') + '" — اسحب لتحريكه', 'success');
        };
        img.onerror = function () { toast('تعذّر تحميل صورة المنتج', 'error'); };
        img.src = p.image;
    }

    function nextZ() {
        return state.items.reduce(function (m, it) { return Math.max(m, it.z); }, 0) + 1;
    }

    // ----- in-browser background removal (border flood-fill) -----------------
    function removeBackground(img) {
        var max = 1000;
        var scale = Math.min(1, max / Math.max(img.width, img.height));
        var w = Math.max(1, Math.round(img.width * scale));
        var h = Math.max(1, Math.round(img.height * scale));
        var c = document.createElement('canvas'); c.width = w; c.height = h;
        var ctx = c.getContext('2d');
        ctx.drawImage(img, 0, 0, w, h);
        var lum = averageLuminance(ctx, w, h);
        var imgData, data;
        try { imgData = ctx.getImageData(0, 0, w, h); data = imgData.data; }
        catch (e) { return { src: img.src, lum: lum, natW: img.width, natH: img.height }; } // tainted -> use original

        // estimate background colour from the four corners
        var corners = [[0, 0], [w - 1, 0], [0, h - 1], [w - 1, h - 1]];
        var br = 0, bg2 = 0, bb = 0;
        corners.forEach(function (pt) {
            var i = (pt[1] * w + pt[0]) * 4; br += data[i]; bg2 += data[i + 1]; bb += data[i + 2];
        });
        br /= 4; bg2 /= 4; bb /= 4;

        var tol = 42;                 // colour distance tolerance
        var tol2 = tol * tol * 3;
        var visited = new Uint8Array(w * h);
        var stack = [];
        // seed from every border pixel
        for (var x = 0; x < w; x++) { stack.push(x); stack.push((h - 1) * w + x); }
        for (var y = 0; y < h; y++) { stack.push(y * w); stack.push(y * w + (w - 1)); }

        function isBg(idx) {
            var i = idx * 4;
            var dr = data[i] - br, dg = data[i + 1] - bg2, db = data[i + 2] - bb;
            return (dr * dr + dg * dg + db * db) <= tol2;
        }

        while (stack.length) {
            var idx = stack.pop();
            if (idx < 0 || idx >= w * h || visited[idx]) continue;
            visited[idx] = 1;
            if (!isBg(idx)) continue;
            data[idx * 4 + 3] = 0;     // make transparent
            var px = idx % w, py = (idx / w) | 0;
            if (px > 0) stack.push(idx - 1);
            if (px < w - 1) stack.push(idx + 1);
            if (py > 0) stack.push(idx - w);
            if (py < h - 1) stack.push(idx + w);
        }

        // soften the cut edge: fade alpha of pixels adjacent to transparent ones
        featherEdges(data, w, h);

        ctx.putImageData(imgData, 0, 0);
        // crop to the visible bounds so the product fills its box
        var cropped = cropToContent(c);
        return { src: cropped.canvas.toDataURL('image/png'), lum: lum, natW: cropped.w, natH: cropped.h };
    }

    function featherEdges(data, w, h) {
        var copy = new Uint8ClampedArray(data); // alpha snapshot
        for (var y = 1; y < h - 1; y++) {
            for (var x = 1; x < w - 1; x++) {
                var i = (y * w + x) * 4;
                if (copy[i + 3] === 0) continue;
                // if any 4-neighbour is transparent, this is an edge pixel → soften
                if (copy[i - 4 + 3] === 0 || copy[i + 4 + 3] === 0 ||
                    copy[i - w * 4 + 3] === 0 || copy[i + w * 4 + 3] === 0) {
                    data[i + 3] = Math.min(data[i + 3], 170);
                }
            }
        }
    }

    function cropToContent(c) {
        var w = c.width, h = c.height, ctx = c.getContext('2d');
        var d = ctx.getImageData(0, 0, w, h).data;
        var minX = w, minY = h, maxX = 0, maxY = 0, found = false;
        for (var y = 0; y < h; y++) {
            for (var x = 0; x < w; x++) {
                if (d[(y * w + x) * 4 + 3] > 12) {
                    found = true;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            }
        }
        if (!found) return { canvas: c, w: w, h: h };
        var pad = 2;
        minX = Math.max(0, minX - pad); minY = Math.max(0, minY - pad);
        maxX = Math.min(w - 1, maxX + pad); maxY = Math.min(h - 1, maxY + pad);
        var cw = maxX - minX + 1, ch = maxY - minY + 1;
        var out = document.createElement('canvas'); out.width = cw; out.height = ch;
        out.getContext('2d').drawImage(c, minX, minY, cw, ch, 0, 0, cw, ch);
        return { canvas: out, w: cw, h: ch };
    }

    // ========================================================================
    //  ITEM ELEMENTS + RENDERING
    // ========================================================================
    function buildItemEl(item) {
        var el = document.createElement('div');
        el.className = 'vz-item';
        el.setAttribute('data-id', item.id);
        // only the cut-out image + edge handles live over the photo — all the
        // action icons are in the inspector panel so they never cover the image
        el.innerHTML =
            '<img class="vz-item__img" draggable="false" src="' + item.src + '" alt="">' +
            '<div class="vz-item__frame">' +
            '  <span class="vz-h vz-h--rot" data-h="rot" title="تدوير"></span>' +
            '  <span class="vz-h vz-h--tl" data-h="resize"></span>' +
            '  <span class="vz-h vz-h--tr" data-h="resize"></span>' +
            '  <span class="vz-h vz-h--bl" data-h="resize"></span>' +
            '  <span class="vz-h vz-h--br" data-h="resize"></span>' +
            '</div>' +
            '<span class="vz-item__cfg" title="يحتاج خيارات قبل الشراء">!</span>';
        el.classList.toggle('vz-item--cfg', item.requiresConfiguration);
        els.canvas.appendChild(el);
        item._el = el;

        // pointer interactions: edge handle = resize/rotate, body = drag
        el.addEventListener('pointerdown', function (e) {
            var handle = e.target.closest('[data-h]');
            select(item.id);
            if (handle) startHandle(e, item, handle.getAttribute('data-h'));
            else startDrag(e, item);
        });
    }

    function itemAction(item, act) {
        switch (act) {
            case 'front': item.z = nextZ(); renderItem(item); commit(); break;
            case 'back': item.z = Math.min.apply(null, state.items.map(function (i) { return i.z; })) - 1; renderItem(item); commit(); break;
            case 'flip': item.flipX = !item.flipX; renderItem(item); commit(); break;
            case 'bg': toggleBg(item); break;
            case 'floor': item.tilt = item.tilt > 0 ? 0 : 20; renderItem(item); commit(); break;
            case 'shadow': item.shadow = !item.shadow; renderItem(item); commit(); break;
            case 'dup': duplicateItem(item); break;
            case 'del': removeItem(item); break;
        }
    }

    // switch a placed product between its cut-out and its original (with background)
    function toggleBg(item) {
        item.bgRemoved = !item.bgRemoved;
        item.src = item.bgRemoved ? item.procSrc : item.origSrc;
        item.ar = item.bgRemoved ? item.procAr : item.origAr;
        if (item._el) {
            var img = item._el.querySelector('.vz-item__img');
            if (img) img.src = item.src;
        }
        renderItem(item);
        commit();
        toast(item.bgRemoved ? 'تمت إزالة الخلفية' : 'تم إرجاع الخلفية الأصلية', 'info');
    }

    function duplicateItem(item) {
        var copy = JSON.parse(JSON.stringify(stripEl(item)));
        copy.id = uid(); copy.cx = clamp(item.cx + 0.04, 0, 1); copy.cy = clamp(item.cy + 0.04, 0, 1);
        copy.z = nextZ();
        state.items.push(copy);
        buildItemEl(copy);
        select(copy.id);
        renderItem(copy);
        commit();
    }

    function removeItem(item) {
        if (item._el) item._el.remove();
        state.items = state.items.filter(function (i) { return i.id !== item.id; });
        if (state.selected === item.id) { state.selected = null; hideInspector(); }
        commit(); updateUI();
    }

    function renderItem(item) {
        var el = item._el; if (!el) return;
        var cw = els.canvas.clientWidth, ch = els.canvas.clientHeight;
        if (!cw || !ch) return;
        var wpx = item.w * cw;
        el.style.left = (item.cx * cw) + 'px';
        el.style.top = (item.cy * ch) + 'px';
        el.style.width = wpx + 'px';
        el.style.height = (wpx * item.ar) + 'px';
        el.style.zIndex = item.z;
        el.style.opacity = item.opacity;
        el.style.transform = 'translate(-50%,-50%)' +
            (item.tilt ? ' perspective(900px) rotateX(' + item.tilt + 'deg)' : '') +
            ' rotate(' + item.rot + 'deg)' +
            ' scaleX(' + (item.flipX ? -1 : 1) + ')';

        var img = el.querySelector('.vz-item__img');
        img.style.filter = itemFilter(item, wpx);

        if (item.id === state.selected) refreshInspector(item);
    }

    // brightness (lighting match) + light-aware drop shadow
    function itemFilter(item, wpx) {
        var f = '';
        if (item.matchLight) {
            var b = clamp((state.bgLum + 0.45) / (item.lum + 0.45), 0.82, 1.16);
            f += 'brightness(' + b.toFixed(3) + ') saturate(0.98) ';
        }
        if (item.shadow) {
            var dist = wpx * 0.05;
            var a = rad(state.lightDir);
            var dx = -Math.cos(a) * dist;
            var dy = Math.sin(a) * dist + wpx * 0.015;
            var blur = Math.max(6, wpx * 0.06);
            f += 'drop-shadow(' + dx.toFixed(1) + 'px ' + dy.toFixed(1) + 'px ' + blur.toFixed(1) + 'px rgba(0,0,0,0.38))';
        }
        return f || 'none';
    }

    function renderAllItems() { state.items.forEach(renderItem); }

    // ========================================================================
    //  SELECTION + INSPECTOR (controls live below/above the photo, never over it)
    // ========================================================================
    function getSelected() {
        return state.items.filter(function (i) { return i.id === state.selected; })[0] || null;
    }

    function select(id) {
        state.selected = id;
        $$('.vz-item', els.canvas).forEach(function (el) {
            el.classList.toggle('is-selected', el.getAttribute('data-id') === id);
        });
        var it = getSelected();
        if (it) showInspector(it); else hideInspector();
    }
    function deselect() { select(null); }

    function bindInspector() {
        els.insp = $('#vzInspector');
        if (!els.insp) return;
        els.inspThumb = $('#vzInspThumb');
        els.inspName = $('#vzInspName');
        els.rot = $('#vzRot');
        els.rotVal = $('#vzRotVal');
        els.size = $('#vzSize');

        els.rot.addEventListener('input', function () {
            var it = getSelected(); if (!it) return;
            it.rot = parseInt(this.value, 10) || 0;
            els.rotVal.textContent = it.rot + '°';
            renderItem(it); debouncedCommit();
        });
        els.size.addEventListener('input', function () {
            var it = getSelected(); if (!it) return;
            it.w = clamp((parseInt(this.value, 10) || 28) / 100, 0.04, 2.5);
            renderItem(it); debouncedCommit();
        });

        // nudge + action buttons (delegated)
        els.insp.addEventListener('click', function (e) {
            var it = getSelected(); if (!it) return;
            var mv = e.target.closest('[data-move]');
            var ac = e.target.closest('[data-i]');
            if (mv) {
                var s = 0.03, d = mv.getAttribute('data-move');
                if (d === 'left') it.cx -= s;
                else if (d === 'right') it.cx += s;
                else if (d === 'up') it.cy -= s;
                else if (d === 'down') it.cy += s;
                it.cx = clamp(it.cx, -0.1, 1.1); it.cy = clamp(it.cy, -0.1, 1.1);
                renderItem(it); debouncedCommit();
            } else if (ac) {
                itemAction(it, ac.getAttribute('data-i'));
            }
        });
    }

    function showInspector(item) {
        if (!els.insp) return;
        els.insp.hidden = false;
        els.app.classList.add('vz-has-sel');
        if (els.inspThumb) els.inspThumb.src = item.src;
        if (els.inspName) els.inspName.textContent = item.name || '';
        refreshInspector(item);
        // reflect toggle states
        setActive('shadow', item.shadow);
        setActive('floor', item.tilt > 0);
        setActive('bg', item.bgRemoved);
    }
    function refreshInspector(item) {
        if (!els.insp || els.insp.hidden) return;
        els.rot.value = item.rot;
        els.rotVal.textContent = Math.round(item.rot) + '°';
        els.size.value = Math.round(clamp(item.w, 0.04, 2.5) * 100);
        setActive('shadow', item.shadow);
        setActive('floor', item.tilt > 0);
        setActive('bg', item.bgRemoved);
    }
    function hideInspector() {
        if (!els.insp) return;
        els.insp.hidden = true;
        els.app.classList.remove('vz-has-sel');
    }
    function setActive(act, on) {
        var b = els.insp && els.insp.querySelector('[data-i="' + act + '"]');
        if (b) b.classList.toggle('is-active', !!on);
    }

    // ========================================================================
    //  DRAG / RESIZE / ROTATE  (pointer based)
    // ========================================================================
    function startDrag(e, item) {
        e.preventDefault();
        var rect = els.canvas.getBoundingClientRect();
        var startCx = item.cx, startCy = item.cy;
        var sx = e.clientX, sy = e.clientY;
        capture(e, function (ev) {
            item.cx = clamp(startCx + (ev.clientX - sx) / rect.width, -0.1, 1.1);
            item.cy = clamp(startCy + (ev.clientY - sy) / rect.height, -0.1, 1.1);
            renderItem(item);
        }, commit);
    }

    function startHandle(e, item, type) {
        e.preventDefault(); e.stopPropagation();
        var rect = els.canvas.getBoundingClientRect();
        var centerX = rect.left + item.cx * rect.width;
        var centerY = rect.top + item.cy * rect.height;

        if (type === 'rot') {
            var startAngle = Math.atan2(e.clientY - centerY, e.clientX - centerX);
            var startRot = item.rot;
            capture(e, function (ev) {
                var a = Math.atan2(ev.clientY - centerY, ev.clientX - centerX);
                var deg = startRot + (a - startAngle) * 180 / Math.PI;
                if (ev.shiftKey) deg = Math.round(deg / 15) * 15;
                item.rot = Math.round(deg);
                renderItem(item);
            }, commit);
        } else { // resize — scale by distance from centre
            var startDist = Math.hypot(e.clientX - centerX, e.clientY - centerY);
            var startW = item.w;
            capture(e, function (ev) {
                var d = Math.hypot(ev.clientX - centerX, ev.clientY - centerY);
                item.w = clamp(startW * (d / Math.max(8, startDist)), 0.04, 2.5);
                renderItem(item);
            }, commit);
        }
    }

    // generic pointer capture loop
    function capture(downEvt, onMove, onUp) {
        var moved = false;
        function move(ev) { moved = true; onMove(ev); }
        function up() {
            document.removeEventListener('pointermove', move);
            document.removeEventListener('pointerup', up);
            document.removeEventListener('pointercancel', up);
            if (moved && onUp) onUp();
        }
        document.addEventListener('pointermove', move);
        document.addEventListener('pointerup', up);
        document.addEventListener('pointercancel', up);
    }

    // ========================================================================
    //  STAGE: zoom / pan / deselect
    // ========================================================================
    function bindStage() {
        // wheel zoom (ctrl/cmd or plain wheel over stage)
        els.viewport.addEventListener('wheel', function (e) {
            if (!state.bg) return;
            e.preventDefault();
            setZoom(state.zoom * (e.deltaY < 0 ? 1.12 : 1 / 1.12));
        }, { passive: false });

        // click empty area deselects
        els.viewport.addEventListener('pointerdown', function (e) {
            if (e.target === els.viewport || e.target === els.stage || e.target === els.canvas || e.target === els.bgImg) {
                deselect();
                if (state.bg && state.zoom > 1) startPan(e);
            }
        });

        // pinch zoom (two pointers on the viewport)
        var pts = {};
        var pinchStart = null;
        els.viewport.addEventListener('pointerdown', function (e) { pts[e.pointerId] = e; });
        els.viewport.addEventListener('pointermove', function (e) {
            if (!(e.pointerId in pts)) return;
            pts[e.pointerId] = e;
            var ids = Object.keys(pts);
            if (ids.length === 2) {
                var a = pts[ids[0]], b = pts[ids[1]];
                var dist = Math.hypot(a.clientX - b.clientX, a.clientY - b.clientY);
                if (pinchStart == null) { pinchStart = { dist: dist, zoom: state.zoom }; }
                else { setZoom(pinchStart.zoom * (dist / pinchStart.dist)); }
            }
        });
        function clearPt(e) { delete pts[e.pointerId]; if (Object.keys(pts).length < 2) pinchStart = null; }
        els.viewport.addEventListener('pointerup', clearPt);
        els.viewport.addEventListener('pointercancel', clearPt);
    }

    function startPan(e) {
        var sx = e.clientX, sy = e.clientY, px = state.panX, py = state.panY;
        capture(e, function (ev) {
            state.panX = px + (ev.clientX - sx);
            state.panY = py + (ev.clientY - sy);
            applyView();
        });
    }

    function setZoom(z) {
        state.zoom = clamp(z, 1, 5);
        if (state.zoom === 1) { state.panX = 0; state.panY = 0; }
        applyView();
        if (els.zoomLabel) els.zoomLabel.textContent = Math.round(state.zoom * 100) + '%';
    }
    function resetView() { state.zoom = 1; state.panX = 0; state.panY = 0; applyView(); if (els.zoomLabel) els.zoomLabel.textContent = '100%'; }
    function applyView() {
        els.stage.style.transform = 'translate(' + state.panX + 'px,' + state.panY + 'px) scale(' + state.zoom + ')';
    }

    // ========================================================================
    //  HISTORY (undo / redo)
    // ========================================================================
    function snapshot() {
        return JSON.stringify({
            bg: state.bg, bgW: state.bgW, bgH: state.bgH, bgLum: state.bgLum,
            lightDir: state.lightDir,
            items: state.items.map(stripEl)
        });
    }
    function stripEl(item) { var o = {}; for (var k in item) if (k !== '_el') o[k] = item[k]; return o; }

    function commit() {
        undoStack.push(snapshot());
        if (undoStack.length > 50) undoStack.shift();
        redoStack.length = 0;
        autosave();
        updateUI();
    }

    function restoreFrom(json) {
        var s = JSON.parse(json);
        state.bg = s.bg; state.bgW = s.bgW; state.bgH = s.bgH; state.bgLum = s.bgLum != null ? s.bgLum : 0.5;
        state.lightDir = s.lightDir != null ? s.lightDir : 135;
        if (els.lightDir) els.lightDir.value = state.lightDir;
        // rebuild items
        $$('.vz-item', els.canvas).forEach(function (el) { el.remove(); });
        state.items = (s.items || []).map(function (o) { return o; });
        applyBackground();
        state.items.forEach(function (it) { buildItemEl(it); renderItem(it); });
        state.selected = null;
        updateUI();
    }

    function undo() {
        if (undoStack.length < 2) return;
        redoStack.push(undoStack.pop());
        restoreFrom(undoStack[undoStack.length - 1]);
        autosave();
    }
    function redo() {
        if (!redoStack.length) return;
        var j = redoStack.pop();
        undoStack.push(j);
        restoreFrom(j);
        autosave();
    }

    // ========================================================================
    //  PERSISTENCE (localStorage) + autosave
    // ========================================================================
    function autosave() {
        try { localStorage.setItem(AUTOSAVE_KEY, snapshot()); } catch (e) { }
    }
    function restoreAutosave() {
        try {
            var j = localStorage.getItem(AUTOSAVE_KEY);
            if (j) { restoreFrom(j); undoStack = [j]; }
        } catch (e) { }
    }
    function getDesigns() {
        try { return JSON.parse(localStorage.getItem(STORE_KEY) || '{}'); } catch (e) { return {}; }
    }
    function saveDesignDialog() {
        if (!state.bg) { toast('لا يوجد تصميم لحفظه بعد', 'error'); return; }
        var name = (prompt('اسم التصميم:', 'تصميمي ' + (new Date().toLocaleDateString('ar-EG'))) || '').trim();
        if (!name) return;
        var d = getDesigns();
        d[name] = snapshot();
        try { localStorage.setItem(STORE_KEY, JSON.stringify(d)); toast('تم حفظ التصميم "' + name + '"', 'success'); }
        catch (e) { toast('تعذّر الحفظ (مساحة التخزين ممتلئة)', 'error'); }
    }
    function loadDesignDialog() {
        var d = getDesigns();
        var names = Object.keys(d);
        var html = '';
        if (!names.length) html = '<p class="vz-modal__empty">لا توجد تصاميم محفوظة بعد.</p>';
        else {
            html = '<ul class="vz-designs">';
            names.forEach(function (n) {
                html += '<li><span>' + esc(n) + '</span>' +
                    '<span class="vz-designs__btns">' +
                    '<button type="button" class="vz-btn vz-btn--sm" data-load="' + esc(n) + '">فتح</button>' +
                    '<button type="button" class="vz-btn vz-btn--sm vz-btn--ghost" data-del="' + esc(n) + '">حذف</button>' +
                    '</span></li>';
            });
            html += '</ul>';
        }
        openModal('تصاميمي المحفوظة', html);
        els.modalBody.onclick = function (e) {
            var lo = e.target.closest('[data-load]'), de = e.target.closest('[data-del]');
            if (lo) { restoreFrom(d[lo.getAttribute('data-load')]); undoStack = [snapshot()]; closeModal(); toast('تم فتح التصميم', 'success'); }
            else if (de) {
                delete d[de.getAttribute('data-del')];
                localStorage.setItem(STORE_KEY, JSON.stringify(d));
                loadDesignDialog();
            }
        };
    }

    // ========================================================================
    //  EXPORT — compose to canvas
    // ========================================================================
    function composeCanvas() {
        return new Promise(function (resolve, reject) {
            if (!state.bg) { reject('no-bg'); return; }
            var W = state.bgW, H = state.bgH;
            // cap export size
            var cap = 2000, sc = Math.min(1, cap / Math.max(W, H));
            W = Math.round(W * sc); H = Math.round(H * sc);
            var c = document.createElement('canvas'); c.width = W; c.height = H;
            var ctx = c.getContext('2d');
            var bg = new Image();
            bg.onload = function () {
                ctx.drawImage(bg, 0, 0, W, H);
                var ordered = state.items.slice().sort(function (a, b) { return a.z - b.z; });
                var loaded = 0;
                if (!ordered.length) { resolve(c); return; }
                ordered.forEach(function (item) {
                    var im = new Image();
                    im.onload = function () {
                        drawItem(ctx, item, im, W, H);
                        if (++loaded === ordered.length) resolve(c);
                    };
                    im.onerror = function () { if (++loaded === ordered.length) resolve(c); };
                    im.src = item.src;
                });
            };
            bg.onerror = function () { reject('bg-load'); };
            bg.src = state.bg;
        });
    }

    function drawItem(ctx, item, im, W, H) {
        var wpx = item.w * W;
        var hpx = wpx * item.ar;
        ctx.save();
        ctx.translate(item.cx * W, item.cy * H);
        if (item.tilt) ctx.scale(1, Math.cos(rad(item.tilt))); // approximate floor foreshortening
        ctx.rotate(rad(item.rot));
        if (item.flipX) ctx.scale(-1, 1);
        ctx.globalAlpha = item.opacity;
        ctx.filter = itemFilter(item, wpx);
        ctx.drawImage(im, -wpx / 2, -hpx / 2, wpx, hpx);
        ctx.restore();
    }

    function downloadImage() {
        if (!state.bg) { toast('لا يوجد تصميم لتنزيله', 'error'); return; }
        toast('جارٍ تجهيز الصورة…', 'info');
        composeCanvas().then(function (c) {
            c.toBlob(function (blob) {
                var url = URL.createObjectURL(blob);
                var a = document.createElement('a');
                a.href = url; a.download = 'noor-visualize.png';
                document.body.appendChild(a); a.click(); a.remove();
                setTimeout(function () { URL.revokeObjectURL(url); }, 4000);
                toast('تم تنزيل الصورة', 'success');
            }, 'image/png');
        }).catch(function () { toast('تعذّر إنشاء الصورة', 'error'); });
    }

    function shareImage() {
        if (!state.bg) { toast('لا يوجد تصميم للمشاركة', 'error'); return; }
        composeCanvas().then(function (c) {
            c.toBlob(function (blob) {
                var file = new File([blob], 'noor-visualize.png', { type: 'image/png' });
                if (navigator.canShare && navigator.canShare({ files: [file] })) {
                    navigator.share({
                        files: [file],
                        title: 'تصميمي من نور',
                        text: 'شاهد كيف ستبدو هذه المنتجات في غرفتي ✨'
                    }).catch(function () { });
                } else {
                    // fallback: download + tell the user
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a'); a.href = url; a.download = 'noor-visualize.png';
                    document.body.appendChild(a); a.click(); a.remove();
                    setTimeout(function () { URL.revokeObjectURL(url); }, 4000);
                    toast('المشاركة المباشرة غير مدعومة — تم تنزيل الصورة لمشاركتها', 'info');
                }
            }, 'image/png');
        }).catch(function () { toast('تعذّر تجهيز الصورة', 'error'); });
    }

    // ========================================================================
    //  ADD ALL TO CART  (reuses the site's AjaxCart + antiforgery helpers)
    // ========================================================================
    function addAllToCart() {
        if (!state.items.length) { toast('أضف منتجًا واحدًا على الأقل للتصميم', 'error'); return; }

        // aggregate quantities per product, separating configurable items
        var qty = {}, names = {}, skipped = [];
        state.items.forEach(function (it) {
            if (it.requiresConfiguration) { skipped.push(it.name); return; }
            qty[it.productId] = (qty[it.productId] || 0) + 1;
            names[it.productId] = it.name;
        });
        var ids = Object.keys(qty);
        if (!ids.length) {
            toast('المنتجات المختارة تحتاج اختيار خيارات من صفحتها قبل الإضافة', 'error');
            return;
        }

        var hasJq = typeof window.jQuery !== 'undefined' && typeof window.addAntiForgeryToken === 'function';
        toast('جارٍ الإضافة إلى السلة…', 'info');

        var i = 0, added = 0, failed = [];
        function next() {
            if (i >= ids.length) { finish(); return; }
            var pid = ids[i++], q = qty[pid];
            var url = ADD_TO_CART_BASE + pid + '/1/' + q;   // shoppingCartTypeId=1 (cart)

            var postData = {};
            if (hasJq) window.addAntiForgeryToken(postData);
            else {
                var tok = document.querySelector('input[name="__RequestVerificationToken"]');
                postData['__RequestVerificationToken'] = tok ? tok.value : '';
            }

            var done = function (resp) {
                if (resp && resp.redirect) failed.push(names[pid]);     // needs options
                else if (resp && resp.success) { added++; applyCartResponse(resp); }
                else failed.push(names[pid]);
                next();
            };

            if (hasJq) {
                window.jQuery.ajax({ cache: false, url: url, type: 'POST', data: postData, success: done, error: function () { failed.push(names[pid]); next(); } });
            } else {
                var body = new URLSearchParams(postData);
                fetch(url, { method: 'POST', headers: { 'X-Requested-With': 'XMLHttpRequest' }, body: body })
                    .then(function (r) { return r.json(); }).then(done)
                    .catch(function () { failed.push(names[pid]); next(); });
            }
        }
        function finish() {
            if (added && !failed.length) toast('تمت إضافة كل المنتجات إلى السلة 🛒', 'success');
            else if (added) toast('تمت إضافة ' + added + ' منتج. بعض المنتجات تحتاج خيارات: ' + failed.join('، '), 'info');
            else toast('تعذّرت الإضافة. قد تحتاج المنتجات لاختيار خيارات أولًا.', 'error');
        }
        next();
    }

    // reflect cart count in header (and the mobile tab-bar badge via its observer)
    function applyCartResponse(resp) {
        try {
            if (window.AjaxCart && typeof window.AjaxCart.success_process === 'function') {
                window.AjaxCart.success_process(resp);
            } else if (resp.updatetopcartsectionhtml && window.jQuery) {
                window.jQuery('.header-links .cart-qty').html(resp.updatetopcartsectionhtml);
            }
        } catch (e) { }
    }

    // ========================================================================
    //  KEYBOARD
    // ========================================================================
    function bindKeyboard() {
        document.addEventListener('keydown', function (e) {
            if (/^(INPUT|TEXTAREA|SELECT)$/.test((e.target.tagName || ''))) return;
            var sel = state.selected ? state.items.filter(function (i) { return i.id === state.selected; })[0] : null;
            var meta = e.ctrlKey || e.metaKey;
            if (meta && e.key.toLowerCase() === 'z' && !e.shiftKey) { e.preventDefault(); undo(); }
            else if (meta && (e.key.toLowerCase() === 'y' || (e.key.toLowerCase() === 'z' && e.shiftKey))) { e.preventDefault(); redo(); }
            else if (sel && (e.key === 'Delete' || e.key === 'Backspace')) { e.preventDefault(); removeItem(sel); }
            else if (sel && e.key.indexOf('Arrow') === 0) {
                e.preventDefault();
                var step = e.shiftKey ? 0.02 : 0.005;
                if (e.key === 'ArrowLeft') sel.cx -= step;
                if (e.key === 'ArrowRight') sel.cx += step;
                if (e.key === 'ArrowUp') sel.cy -= step;
                if (e.key === 'ArrowDown') sel.cy += step;
                renderItem(sel); debouncedCommit();
            }
        });
    }
    var debouncedCommit = debounce(commit, 400);

    // ========================================================================
    //  MISC UI
    // ========================================================================
    function clearAll() {
        if (!state.bg && !state.items.length) return;
        if (!confirm('مسح التصميم الحالي والبدء من جديد؟')) return;
        state.bg = null; state.bgW = state.bgH = 0; state.items = [];
        $$('.vz-item', els.canvas).forEach(function (el) { el.remove(); });
        applyBackground(); resetView(); commit(); updateUI();
    }

    function updateUI() {
        $$('[data-vz="undo"]').forEach(function (b) { b.disabled = undoStack.length < 2; });
        $$('[data-vz="redo"]').forEach(function (b) { b.disabled = !redoStack.length; });
        var hasItems = state.items.length > 0;
        $$('[data-vz="addall"]').forEach(function (b) { b.disabled = !hasItems; });
        $$('[data-vz="download"],[data-vz="share"],[data-vz="save"],[data-vz="clear"]').forEach(function (b) { b.disabled = !state.bg; });
        var count = $('#vzItemCount'); if (count) count.textContent = state.items.length;
        els.empty.hidden = !!state.bg;
    }

    function openModal(title, html) {
        els.modalTitle.textContent = title;
        els.modalBody.innerHTML = html;
        els.modal.hidden = false;
    }
    function closeModal() { els.modal.hidden = true; els.modalBody.onclick = null; }

    // lightweight toast (uses nop notification bar if available, else our own)
    var toastTimer;
    function toast(msg, type) {
        var bar = $('#vzToast');
        if (!bar) return;
        bar.textContent = msg;
        bar.className = 'vz-toast vz-toast--' + (type || 'info') + ' is-on';
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { bar.classList.remove('is-on'); }, 3200);
    }
    function flashUpload() {
        $$('[data-vz="upload"]').forEach(function (b) { b.classList.add('vz-pulse'); setTimeout(function () { b.classList.remove('vz-pulse'); }, 1200); });
    }

    // event delegation helper
    function on(root, evt, sel, handler) {
        root.addEventListener(evt, function (e) {
            var t = e.target.closest(sel);
            if (t && root.contains(t)) handler(e, t);
        });
    }
    function esc(s) { return String(s == null ? '' : s).replace(/[&<>"']/g, function (m) { return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[m]; }); }

    // ----- boot --------------------------------------------------------------
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
