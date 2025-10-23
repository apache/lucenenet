/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

/* 1. Mode controller (desktop vs mobile; width only with hysteresis) */
(function () {
  var ROOT_ID = 'autocollapse', COLLAPSED = 'collapsed', MODE = null;
  // Using the same range so both sides feel identical:
  var MOBILE_MAX = 1200, DESKTOP_MIN = 1200;
  var scheduled = false;

  function $(id) { return document.getElementById(id); }
  function root() { return $(ROOT_ID); }
  function nav() { return $('navbar'); }
  function toggleBtn() { return document.querySelector('#' + ROOT_ID + ' .navbar-toggle'); }
  function isPanelOpen() { var n = nav(); return !!(n && n.classList.contains('in')); }

  function setCollapseOpen(open) {
    var n = nav(), btn = toggleBtn(); if (!n) return;
    n.classList.add('collapse'); n.classList.remove('collapsing');
    if (open) {
      n.classList.add('in'); n.style.display = 'block'; n.style.height = '';
      n.setAttribute('aria-expanded', 'true');
      if (btn) { btn.classList.remove('collapsed'); btn.setAttribute('aria-expanded', 'true'); }
    } else {
      n.classList.remove('in'); n.style.display = ''; n.style.height = '';
      n.setAttribute('aria-expanded', 'false');
      if (btn) { btn.classList.add('collapsed'); btn.setAttribute('aria-expanded', 'false'); }
    }
  }

  function decide(from) {
    var w = window.innerWidth;
    if (from === 'mobile') return (w >= DESKTOP_MIN) ? 'desktop' : 'mobile';
    if (from === 'desktop') return (w < MOBILE_MAX) ? 'mobile' : 'desktop';
    if (w >= DESKTOP_MIN) return 'desktop';
    if (w < MOBILE_MAX) return 'mobile';
    return 'desktop';
  }

  function paint(mode) {
    var r = root(); if (!r) return;
    var isMobile = (mode === 'mobile');
    if (MODE !== mode) {
      r.classList.toggle(COLLAPSED, isMobile);
      setCollapseOpen(false);   // never auto-open when switching
      MODE = mode;
      return;
    }
    r.classList.toggle(COLLAPSED, isMobile);
  }

  function recalc() {
    scheduled = false;
    if (root() && root().classList.contains(COLLAPSED) && isPanelOpen()) return;
    paint(decide(MODE));
  }
  function schedule() { if (scheduled) return; scheduled = true; requestAnimationFrame(recalc); }

  if (document.readyState !== 'loading') schedule();
  else document.addEventListener('DOMContentLoaded', schedule);
  window.addEventListener('resize', schedule);
  window.addEventListener('orientationchange', schedule);
  window.addEventListener('load', function () { schedule(); setTimeout(schedule, 150); });
})();

/* 2. ASF dropdown wiring (mobile click/tap-to-toggle; caret normalization) */
(function () {
  function ready(fn) { if (document.readyState !== 'loading') fn(); else document.addEventListener('DOMContentLoaded', fn); }
  ready(function () {
    var r = document.getElementById('autocollapse'), wrap = document.getElementById('navbar');
    if (!r || !wrap) return;

    function first(li, tag) { tag = tag.toUpperCase(); for (var i = 0; i < li.children.length; i++) { var c = li.children[i]; if (c.tagName && c.tagName.toUpperCase() === tag) return c; } return null; }

    function wire() {
      var ul = wrap.querySelector('ul.navbar-nav'); if (!ul) return false;
      var asf = null;
      for (var i = 0; i < ul.children.length; i++) {
        var li = ul.children[i]; if (li.tagName !== 'LI') continue;
        var a = first(li, 'A'); if (a && a.textContent.trim().toUpperCase() === 'ASF') { asf = li; break; }
      }
      if (!asf) return false;
      if (asf.getAttribute('data-asf-wired') === '1') return true;
      asf.setAttribute('data-asf-wired', '1'); asf.classList.add('nav-asf');

      var stub = first(asf, 'SPAN'); if (stub && /\bexpand-stub\b/.test(stub.className)) stub.style.display = 'none';
      var aTop = first(asf, 'A');

      // Hide Bootstrap caret, ensure exactly one .lucene-caret
      var old = aTop.querySelectorAll('.caret'); for (var k = 0; k < old.length; k++) old[k].style.display = 'none';
      var mine = aTop.querySelectorAll('.lucene-caret'); for (var x = 1; x < mine.length; x++) mine[x].remove();
      if (mine.length === 0) { var sp = document.createElement('span'); sp.className = 'lucene-caret'; aTop.appendChild(sp); }

      // Find submenu UL, mark as asf-menu
      var sub = null;
      for (var j = 0; j < asf.children.length; j++) {
        var c = asf.children[j];
        if (c.tagName === 'UL' && /\bnav\b/.test(c.className) && /\blevel2\b/.test(c.className)) { sub = c; break; }
      }
      if (sub && sub.className.indexOf('asf-menu') === -1) sub.className += ' asf-menu';

      // Mobile: click toggles .is-open; Desktop uses CSS :hover
      aTop.addEventListener('click', function (ev) {
        if (r.classList.contains('collapsed')) { ev.preventDefault(); ev.stopPropagation(); asf.classList.toggle('is-open'); }
      });
      aTop.addEventListener('mousedown', function (ev) { if (r.classList.contains('collapsed')) ev.stopPropagation(); });

      document.addEventListener('keydown', function (e) { if (e.key === 'Escape') asf.classList.remove('is-open'); });
      return true;
    }

    if (!wire()) {
      var obs = new MutationObserver(function () { if (wire()) obs.disconnect(); });
      obs.observe(wrap, { childList: true, subtree: true });
    }
  });
})();

/* 3. Close hamburger when leaving mobile */
(function () {
  var ROOT_ID = 'autocollapse';
  var MOBILE_MAX = 1200, DESKTOP_MIN = 1200;
  var lastMode = null, rafScheduled = false;

  function $(sel, ctx) { return (ctx || document).querySelector(sel); }
  function $id(id) { return document.getElementById(id); }

  function modeFromWidth(w) {
    if (lastMode === 'mobile') return (w >= DESKTOP_MIN) ? 'desktop' : 'mobile';
    if (lastMode === 'desktop') return (w < MOBILE_MAX) ? 'mobile' : 'desktop';
    return (w >= DESKTOP_MIN) ? 'desktop' : 'mobile';
  }

  function setCollapsed(root, collapsed) {
    if (!root) return;
    root.classList.toggle('collapsed', collapsed);
  }

  function closeHamburger() {
    var nav = $id('navbar');
    var btn = $('#' + ROOT_ID + ' .navbar-toggle');
    if (nav) {
      nav.classList.add('collapse');
      nav.classList.remove('in', 'collapsing');
      nav.style.display = '';
      nav.style.height = '';
      nav.setAttribute('aria-expanded', 'false');
    }
    if (btn) {
      btn.classList.add('collapsed');
      btn.setAttribute('aria-expanded', 'false');
    }
    // also close ASF mobile submenu if it was open
    var asf = $('#' + ROOT_ID + ' .nav-asf');
    if (asf) asf.classList.remove('is-open');
  }

  function apply() {
    rafScheduled = false;
    var root = $id(ROOT_ID);
    if (!root) return;

    var w = window.innerWidth || document.documentElement.clientWidth;
    var mode = modeFromWidth(w);
    if (mode !== lastMode) {
      // toggle collapsed state by mode
      setCollapsed(root, mode === 'mobile');
      // when leaving mobile, make sure hamburger is closed
      if (mode === 'desktop') closeHamburger();
      lastMode = mode;
    } else {
      // keep class in sync even if mode didn't change
      setCollapsed(root, mode === 'mobile');
    }
  }

  function schedule() { if (rafScheduled) return; rafScheduled = true; requestAnimationFrame(apply); }

  // initial + after load (some CSS/DocFX bits settle after load)
  if (document.readyState !== 'loading') schedule(); else document.addEventListener('DOMContentLoaded', schedule);
  window.addEventListener('load', function () { schedule(); setTimeout(schedule, 120); });

  // on zoom/resize/orientation changes
  window.addEventListener('resize', schedule);
  window.addEventListener('orientationchange', schedule);
})();

/* 4. Sticky header toggler (desktop only; non-sticky in mobile) */
(function () {
  var ROOT_ID = 'autocollapse';

  function updateSticky() {
    var head = document.querySelector('#wrapper > header') || document.querySelector('header');
    var auto = document.getElementById(ROOT_ID);
    if (!head || !auto) return;
    var desktop = !auto.classList.contains('collapsed');
    head.classList.toggle('is-sticky', desktop);
  }

  // Run now and whenever the collapsed state changes / viewport changes
  if (document.readyState !== 'loading') updateSticky();
  else document.addEventListener('DOMContentLoaded', updateSticky);
  window.addEventListener('load', updateSticky);
  window.addEventListener('resize', updateSticky);
  window.addEventListener('orientationchange', updateSticky);

  // Watch for class changes on #autocollapse
  var auto = document.getElementById(ROOT_ID);
  if (auto) {
    new MutationObserver(updateSticky).observe(auto, { attributes: true, attributeFilter: ['class'] });
  }
})();

/* 5. Body tagging for TOC presence (adds .has-sidetoc if a left TOC exists) */
(function () {
  function tagIfHasTOC() {
    if (!document.body) return false;
    if (document.querySelector('.sidetoc')) {
      document.body.classList.add('has-sidetoc');
      return true;}
    return false;
  }
  function watchUntilFound() {
        if (tagIfHasTOC()) return;
        new MutationObserver(function (m, obs) {
            if (tagIfHasTOC()) obs.disconnect();
        }).observe(document.documentElement, { childList: true, subtree: true });
    }
    if (document.readyState !== 'loading') watchUntilFound();
    else document.addEventListener('DOMContentLoaded', watchUntilFound);
})();

(function () {
    /* 6. Band detection & shared refs */
    var BAND_MIN = 1200, BAND_MAX = 1400;
    var overlay, triggerLi, triggerA;
    var originalParent = null, originalNext = null;

    function inBand() {
        var w = window.innerWidth || document.documentElement.clientWidth;
        return (w >= BAND_MIN && w <= BAND_MAX);
    }

    /* 6.1 Ensure the navbar trigger LI ("Search…") exists; wire click to open overlay */
    function ensureTrigger() {
        if (triggerLi && document.body.contains(triggerLi)) return triggerLi;

        var navUl = document.querySelector('#navbar ul.navbar-nav');
        if (!navUl) return null;

        triggerLi = document.createElement('li');
        triggerLi.className = 'nav-search-li'; // styled in APIDOCS CSS

        triggerA = document.createElement('a');
        triggerA.className = 'nav-search-trigger';
        triggerA.href = '#';
        triggerA.setAttribute('role', 'button');
        triggerA.setAttribute('aria-label', 'Open search');
        triggerA.textContent = 'Search…';      // placeholder-like label to match the inline search style

        triggerA.addEventListener('click', function (e) {
            e.preventDefault();
            openOverlay();
        });

        navUl.appendChild(triggerLi);
        triggerLi.appendChild(triggerA);
        return triggerLi;
    }

    /* 6.2 Ensure the overlay container exists; create panel/close button once */
    function ensureOverlay() {
        if (overlay && document.body.contains(overlay)) return overlay;

        overlay = document.createElement('div');
        overlay.id = 'lucene-search-overlay';  // styled in APIDOCS CSS

        var panel = document.createElement('div');
        panel.className = 'panel';

        var row = document.createElement('div');
        row.className = 'row';

        var closeBtn = document.createElement('button');
        closeBtn.className = 'overlay-close';
        closeBtn.type = 'button';
        closeBtn.setAttribute('aria-label', 'Close search');
        closeBtn.title = 'Close';
        closeBtn.textContent = '×';
        closeBtn.addEventListener('click', closeOverlay);

        row.appendChild(closeBtn);
        panel.appendChild(row);
        overlay.appendChild(panel);

        /* 6.2.1 Click outside panel closes overlay */
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeOverlay();
        });

        document.body.appendChild(overlay);
        return overlay;
    }

    /* 6.3 Open overlay: move DocFX form into overlay, enforce visibility, focus input */
    function openOverlay() {
        var form = document.getElementById('search');
        if (!form) return;

        ensureOverlay();

        if (!originalParent) {
            originalParent = form.parentNode;
            originalNext = form.nextSibling;
        }

        // Move real form into overlay
        var row = overlay.querySelector('.row');
        row.appendChild(form);

        overlay.classList.add('open');  // CSS displays the overlay

        // Ensure form is visible while in overlay
        form.style.display = 'block';

        // Focus the text input
        var input = form.querySelector('#search-query');
        if (input) { setTimeout(function () { input.focus(); input.select && input.select(); }, 0); }

        // ESC closes overlay
        document.addEventListener('keydown', onEsc);

        // Enter/submit closes overlay (one-shot)
        form.addEventListener('submit', onSubmitOnce, { once: true });
    }

    /* 6.4 Close overlay: restore and resync visibility */
    function closeOverlay() {
        var form = document.getElementById('search');

        if (overlay) overlay.classList.remove('open');

        // Put form back exactly where it was in the header.
        if (form && originalParent) {
            if (originalNext) originalParent.insertBefore(form, originalNext);
            else originalParent.appendChild(form);
        }

        // After move, make sure visibility matches current band.
        updateVisibility();

        // Remove ESC listener added at open.
        document.removeEventListener('keydown', onEsc);
    }

    /* 6.4.1 — Keyboard & submit handlers */
    function onEsc(e) { if (e.key === 'Escape') { e.preventDefault(); closeOverlay(); } }
    function onSubmitOnce() { closeOverlay(); }  // close after pressing Enter

    /* 6.5 — Sync trigger vs inline search visibility according to the range (1200–1400): show trigger, hide inline form
    Out of range: hide trigger, show the inline form; if overlay open, close it. */
    function updateVisibility() {
        ensureTrigger();

        var form = document.getElementById('search');
        if (!form) return;

        if (inBand()) {
            if (triggerLi) triggerLi.style.display = '';
            form.style.display = 'none';           // hidden inline; shown when overlay opens
        } else {
            if (triggerLi) triggerLi.style.display = 'none';
            form.style.display = '';               // normal inline search
            if (overlay && overlay.classList.contains('open')) closeOverlay();
        }
    }

    /* 6.6 — Init & watchers: run once, then watch viewport + DocFX rebuilds + mode flips */
    function initOnce() {
        ensureTrigger();
        updateVisibility();
    }

    // Run early, then again after layout settles.
    if (document.readyState !== 'loading') initOnce();
    else document.addEventListener('DOMContentLoaded', initOnce);

    // Re-check for late load.
    window.addEventListener('load', function () {
        updateVisibility();
        setTimeout(updateVisibility, 60);
        setTimeout(updateVisibility, 200);
        setTimeout(updateVisibility, 600);
    });

    // Normal viewport changes (zoom/resize/orientation).
    window.addEventListener('resize', updateVisibility);
    window.addEventListener('orientationchange', updateVisibility);

    // Watch your mobile/desktop class flip on #autocollapse
    var auto = document.getElementById('autocollapse');
    if (auto) {
        new MutationObserver(updateVisibility)
            .observe(auto, { attributes: true, attributeFilter: ['class'] });
    }

    // Making sure our search button trigger is made
    var navbar = document.getElementById('navbar');
    if (navbar) {
        new MutationObserver(function () {
            ensureTrigger();
            updateVisibility();
        }).observe(navbar, { childList: true, subtree: true });
    }
})();
