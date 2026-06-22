/* Noor theme — turns home-page item grids into horizontal carousels
   with prev/next navigation. Direction-aware (works in RTL and LTR). */
(function () {
  "use strict";

  function buildCarousel(grid) {
    if (grid.dataset.noorCarousel === "1") return;
    grid.dataset.noorCarousel = "1";

    // wrap the grid so the arrows can be positioned relative to it
    var wrapper = document.createElement("div");
    wrapper.className = "noor-carousel";
    grid.parentNode.insertBefore(wrapper, grid);
    wrapper.appendChild(grid);

    var isRtl = getComputedStyle(grid).direction === "rtl";

    var prev = makeButton("noor-carousel__nav--prev", isRtl ? "›" : "‹", "Previous");
    var next = makeButton("noor-carousel__nav--next", isRtl ? "‹" : "›", "Next");
    wrapper.appendChild(prev);
    wrapper.appendChild(next);

    function makeButton(modifier, glyph, label) {
      var b = document.createElement("button");
      b.type = "button";
      b.className = "noor-carousel__nav " + modifier;
      b.setAttribute("aria-label", label);
      b.textContent = glyph;
      return b;
    }

    function stepSize() {
      var card = grid.querySelector(".item-box");
      var cardWidth = card ? card.getBoundingClientRect().width : 280;
      var styles = getComputedStyle(grid);
      var gap = parseInt(styles.columnGap || styles.gap, 10) || 20;
      var per = Math.max(1, Math.floor(grid.clientWidth / (cardWidth + gap)));
      return (cardWidth + gap) * per;
    }

    // visual "previous" = toward the start of the row
    prev.addEventListener("click", function () {
      grid.scrollBy({ left: (isRtl ? 1 : -1) * stepSize(), behavior: "smooth" });
    });
    next.addEventListener("click", function () {
      grid.scrollBy({ left: (isRtl ? -1 : 1) * stepSize(), behavior: "smooth" });
    });

    function update() {
      var maxScroll = grid.scrollWidth - grid.clientWidth;
      var pos = Math.abs(grid.scrollLeft); // RTL scrollLeft can be negative
      prev.disabled = pos <= 1;
      next.disabled = pos >= maxScroll - 1;
    }

    grid.addEventListener("scroll", update, { passive: true });
    window.addEventListener("resize", update);
    // re-check after images load and lay out
    window.addEventListener("load", update);
    update();
  }

  function init() {
    var grids = document.querySelectorAll(".home-page .item-grid");
    Array.prototype.forEach.call(grids, buildCarousel);
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
