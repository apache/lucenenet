// /styles/custom.js
// This script makes the ASF links in toc.yml behave nicely:
// - On desktop: dropdown on hover (CSS handles the show/hide).
// - On mobile (hamburger): tap to expand/collapse (JS toggles the class).
// It attempts to keep the nav layout stable when the window is resized/zoomed.
(function () {

  // ---------- helpers ----------

  // Grab the outer navbar wrapper.
  function navbarEl() { return document.getElementById('autocollapse'); }
  // Get the top-level UL that holds the main menu items.
  function level1UL() { return document.querySelector('#navbar ul.level1'); }

  // Is the hamburger actually visible right now?
  function isHamburgerActive() {
    var t = document.querySelector('.navbar-header .navbar-toggle');
    return !!(t && window.getComputedStyle(t).display !== 'none');
  }

  // Mark top-level items that have a direct child so caret CSS applies immediately
  function markHasSubmenus() {
    var ul = level1UL();
    if (!ul) return;
    var lis = ul.children;
    for (var i = 0; i < lis.length; i++) {
      var li = lis[i];
      if (!li.classList) continue;
      if (li.classList.contains('has-submenu')) continue;

      // direct child UL in this item?
      var hasDirectUL = false;
      for (var j = 0; j < li.children.length; j++) {
        if (li.children[j].tagName === 'UL') { hasDirectUL = true; break; }
      }
      if (!hasDirectUL) continue;

      li.classList.add('has-submenu');
      var a = li.querySelector('a');
      if (a) {
        a.setAttribute('aria-haspopup', 'true');
        if (!a.hasAttribute('aria-expanded')) a.setAttribute('aria-expanded', 'false');
      }
    }
  }

  // Insert an invisible line break after the 3rd item, if not present
  // It is used by the .desktop-compact two-row layout 
  function ensureRowBreak() {
    var ul = level1UL(); if (!ul) return;
    var kids = ul.children;
    for (var i = 0; i < kids.length; i++) {
      if (kids[i].classList && kids[i].classList.contains('row-break')) return;
    }
    if (kids.length < 3) return;
    var br = document.createElement('li');
    br.className = 'row-break';
    kids[2].insertAdjacentElement('afterend', br);
  }

  // Removes any previous line break
  function removeRowBreak() {
    var ul = level1UL(); if (!ul) return;
    var kids = ul.children;
    for (var i = kids.length - 1; i >= 0; i--) {
      if (kids[i].classList && kids[i].classList.contains('row-break')) {
        kids[i].remove();
      }
    }
  }

  // Check if the unmodified menu has wrapped into two lines
  // (using the vertical position of the first and last visible items)
  function navHasWrappedNatural() {
    var ul = level1UL();
    if (!ul) return false;

    // Build a list of the visible (real) items
    var items = [];
    for (var i = 0; i < ul.children.length; i++) {
      var li = ul.children[i];
      if (!li.tagName || li.tagName !== 'LI') continue;
      if (li.classList && li.classList.contains('row-break')) continue;
      if (li.offsetParent === null) continue; // skips the invisible items
      items.push(li);
    }
    if (items.length < 2) return false;

    var firstTop = items[0].getBoundingClientRect().top;
    var lastTop = items[items.length - 1].getBoundingClientRect().top;
    return Math.abs(lastTop - firstTop) > 2; // different rows (wrapped)
  }

  // Close any open submenu states
  function closeAllOpens() {
    var ul = level1UL(); if (!ul) return;
    var lis = ul.querySelectorAll('li.open');
    for (var i = 0; i < lis.length; i++) {
      lis[i].classList.remove('open');
      var a = lis[i].querySelector('a');
      if (a) a.setAttribute('aria-expanded', 'false');
    }
  }

  // Decide and apply the layout mode:
  // - If hamburger is active -> normal mobile behavior; submenus tap-to-open (.open)
  // - Desktop one-row: normal hover dropdowns.
  // - Desktop “compact” two-row: still hover dropdowns, but we insert a line break.
  function updateLayoutState() {
    var ac = navbarEl(), ul = level1UL();
    if (!ac || !ul) return;

    if (isHamburgerActive()) {
      // mobile
      ac.classList.remove('desktop-compact');
      removeRowBreak();
      closeAllOpens();
      return;
    }

    // DESKTOP/TABLET path:
    // Reset the two-row compact, before measuring
    ac.classList.remove('desktop-compact');
    removeRowBreak();

    // Measuring; If it naturally wraps, we enable the .desktop.compact mode and insert the break
    if (navHasWrappedNatural()) {
      ensureRowBreak();
      ac.classList.add('desktop-compact');
    } else {
      // Otherwise it stays the original single row.
      ac.classList.remove('desktop-compact');
      removeRowBreak();
    }

    closeAllOpens(); // hover controls visibility on desktop
  }

  // Smoother zoom/resize
  var rafId = 0;
  function scheduleUpdate() {
    if (rafId) cancelAnimationFrame(rafId);
    rafId = requestAnimationFrame(function () {
      rafId = 0;
      // Let layout/fonts settle first before measuring.
      setTimeout(function () {
        markHasSubmenus();
        updateLayoutState();
      }, 0);
    });
  }

  // ---------- events ----------

  // MOBILE (hamburger): tap a parent with submenu to toggle
  document.addEventListener('click', function (e) {
    if (!isHamburgerActive()) return;

    var link = e.target.closest('#navbar ul.level1 > li > a');
    if (!link) return;

    var li = link.parentElement;
    // find direct UL child, if there is none, then it is a leaf link
    var submenu = null;
    for (var i = 0; i < li.children.length; i++) {
      if (li.children[i].tagName === 'UL') { submenu = li.children[i]; break; }
    }
    if (!submenu) return; // leaf 

    e.preventDefault();
    e.stopPropagation();

    // Ensure the caret is present
    li.classList.add('has-submenu');

    var isOpen = li.classList.toggle('open');
    link.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
  });

  // MOBILE (hamburger): clicking a leaf closes the drawer.
  document.addEventListener('click', function (e) {
    if (!isHamburgerActive()) return;
    var a = e.target.closest('.navbar-collapse a');
    if (!a) return;
    var li = a.closest('li');
    if (li && li.querySelector('ul')) return; // parent

    // Use Bootstrap’s jQuery collapse() if it is available,
    // else remove the .open class to close the drawer.
    var jq = window.$, collapse = document.querySelector('.navbar-collapse');
    if (jq && typeof jq('.navbar-collapse').collapse === 'function') {
      jq('.navbar-collapse').collapse('hide');
    } else if (collapse) {
      collapse.classList.remove('in');
    }
  });

  // init after everything is loaded (to capture late font/layout shifts)
  window.addEventListener('load', function () {
    markHasSubmenus();
    // Run a few times in case fonts or content cause layout to shift.
    updateLayoutState();
    setTimeout(updateLayoutState, 100);
    setTimeout(updateLayoutState, 300);

    // If the template rebuilds the navbar, runs checks again
    var root = document.getElementById('navbar');
    if (root && 'MutationObserver' in window) {
      var obs = new MutationObserver(scheduleUpdate);
      obs.observe(root, { childList: true, subtree: true });
    }
  });

  // Recalculate on resize, orientation change and when the tab becomes visible again.
  window.addEventListener('resize', scheduleUpdate);
  window.addEventListener('orientationchange', scheduleUpdate);
  document.addEventListener('visibilitychange', function () {
    if (!document.hidden) scheduleUpdate(); // catch zoom-induced relayout on tab return
  });

})();
