const compareFrame = document.getElementById("compareFrame");
const compareOverlay = document.getElementById("compareOverlay");
const compareDivider = document.getElementById("compareDivider");

function clampPercent(value) {
  return Math.max(0, Math.min(100, value));
}

function setComparison(value) {
  const normalized = clampPercent(Number(value));
  const split = `${normalized}%`;

  if (!compareFrame || !compareOverlay || !compareDivider) {
    return;
  }

  compareFrame.style.setProperty("--split", split);
  compareDivider.style.left = split;
  compareDivider.setAttribute("aria-valuenow", String(Math.round(normalized)));
}

function setComparisonFromPointer(clientX) {
  if (!compareFrame) {
    return;
  }

  const rect = compareFrame.getBoundingClientRect();
  if (rect.width <= 0) {
    return;
  }

  const value = ((clientX - rect.left) / rect.width) * 100;
  setComparison(value);
}

if (compareFrame && compareDivider) {
  let isDragging = false;

  setComparison(55);

  const startDrag = (event) => {
    isDragging = true;
    compareFrame.classList.add("dragging");
    if (event instanceof PointerEvent) {
      compareFrame.setPointerCapture(event.pointerId);
      setComparisonFromPointer(event.clientX);
    }
  };

  const onDrag = (event) => {
    if (!isDragging || !(event instanceof PointerEvent)) {
      return;
    }

    setComparisonFromPointer(event.clientX);
  };

  const endDrag = () => {
    if (!isDragging) {
      return;
    }

    isDragging = false;
    compareFrame.classList.remove("dragging");
  };

  compareFrame.addEventListener("pointerdown", startDrag);
  compareFrame.addEventListener("pointermove", onDrag);
  compareFrame.addEventListener("pointerup", endDrag);
  compareFrame.addEventListener("pointercancel", endDrag);
  compareFrame.addEventListener("pointerleave", endDrag);

  compareDivider.addEventListener("keydown", (event) => {
    const current = Number(compareDivider.getAttribute("aria-valuenow") ?? "55");
    const step = event.shiftKey ? 10 : 2;

    if (event.key === "ArrowLeft") {
      event.preventDefault();
      setComparison(current - step);
      return;
    }

    if (event.key === "ArrowRight") {
      event.preventDefault();
      setComparison(current + step);
      return;
    }

    if (event.key === "Home") {
      event.preventDefault();
      setComparison(0);
      return;
    }

    if (event.key === "End") {
      event.preventDefault();
      setComparison(100);
    }
  });

  compareFrame.addEventListener("click", (event) => {
    if (!(event instanceof MouseEvent)) {
      return;
    }

    setComparisonFromPointer(event.clientX);
  });
}

const revealItems = document.querySelectorAll(".reveal, .reveal-delay, .reveal-stagger");

if ("IntersectionObserver" in window && revealItems.length > 0) {
  const observer = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("in-view");
          observer.unobserve(entry.target);
        }
      });
    },
    {
      threshold: 0.15,
      rootMargin: "0px 0px -40px 0px"
    }
  );

  revealItems.forEach((item) => observer.observe(item));
} else {
  revealItems.forEach((item) => item.classList.add("in-view"));
}
