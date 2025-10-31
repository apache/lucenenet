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

/* 1. Mode controller (desktop vs mobile) — use matchMedia to align with CSS */
(function () {
  var ROOT_ID = 'autocollapse', COLLAPSED = 'collapsed', MODE = null;
  // Keep readable constant in case you want to change breakpoint in one place
  var DESKTOP_QUERY = '(min-width: 768px)';
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

  // Use matchMedia to determine desktop/mobile to match the CSS
  function isDesktop() {
    if (window.matchMedia) return window.matchMedia(DESKTOP_QUERY).matches;
    // Fallback: numeric check (rarely used)
    return (window.innerWidth || document.documentElement.clientWidth) >= 768;
  }

  function decide() {
    return isDesktop() ? 'desktop' : 'mobile';
  }

  function paint(mode) {
    var r = root(); if (!r) return;
    var isMobile = (mode === 'mobile');
    if (MODE !== mode) {
      r.classList.toggle(COLLAPSED, isMobile);
      setCollapseOpen(false);   // never auto-open when switching
      // When switching to desktop, ensure any mobile-only open states are cleared
      if (!isMobile) {
        try {
          var opens = r.querySelectorAll('.nav-asf.is-open');
          for (var i = 0; i < opens.length; i++) opens[i].classList.remove('is-open');
        } catch (e) { /* defensive */ }
      }
      MODE = mode;
      return;
    }
    r.classList.toggle(COLLAPSED, isMobile);
  }

  function recalc() {
    scheduled = false;
    if (root() && root().classList.contains(COLLAPSED) && isPanelOpen()) return;
    paint(decide());
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
  var DESKTOP_QUERY = '(min-width: 768px)';
  var lastMode = null, rafScheduled = false;

  function $(sel, ctx) { return (ctx || document).querySelector(sel); }
  function $id(id) { return document.getElementById(id); }

  function isDesktop() {
    if (window.matchMedia) return window.matchMedia(DESKTOP_QUERY).matches;
    return (window.innerWidth || document.documentElement.clientWidth) >= 768;
  }

  function modeFromWidth() {
    return isDesktop() ? 'desktop' : 'mobile';
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

    var mode = modeFromWidth();
    if (mode !== lastMode) {
      setCollapsed(root, mode === 'mobile');
      if (mode === 'desktop') closeHamburger();
      lastMode = mode;
    } else {
      setCollapsed(root, mode === 'mobile');
    }
  }

  function schedule() { if (rafScheduled) return; rafScheduled = true; requestAnimationFrame(apply); }

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

