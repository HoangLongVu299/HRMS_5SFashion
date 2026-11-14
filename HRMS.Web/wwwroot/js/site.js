'use strict';
// UI only: the server remains responsible for validation and all business rules.
(() => {
  const toggle = document.querySelector('[data-sidebar-toggle]');
  const sidebar = document.getElementById('app-sidebar');
  const narrow = window.matchMedia('(max-width: 767px)');
  const main = document.querySelector('.page-content');
  const footer = document.querySelector('.app-footer');
  function syncMenu() {
    const open = narrow.matches ? document.body.classList.contains('sidebar-open') : !document.body.classList.contains('sidebar-collapsed');
    toggle?.setAttribute('aria-expanded', String(open));
    toggle?.setAttribute('aria-label', open ? 'Thu gọn menu' : 'Mở menu');
    // Closed mobile menu has display:none; opened menu temporarily hides content.
    if (main) main.inert = narrow.matches && open;
    if (footer) footer.inert = narrow.matches && open;
  }
  toggle?.addEventListener('click', () => {
    document.body.classList.toggle(narrow.matches ? 'sidebar-open' : 'sidebar-collapsed');
    syncMenu();
    if (narrow.matches && document.body.classList.contains('sidebar-open')) sidebar?.querySelector('a')?.focus();
  });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && narrow.matches && document.body.classList.contains('sidebar-open')) {
      document.body.classList.remove('sidebar-open'); syncMenu(); toggle?.focus();
    }
  });
  narrow.addEventListener('change', () => {
    document.body.classList.remove('sidebar-open'); syncMenu();
    if (sidebar?.contains(document.activeElement) && narrow.matches) toggle?.focus();
  });
  syncMenu();
  document.querySelectorAll('label.required').forEach(label => {
    // aria-required describes existing server rules; it does not replace validation.
    const input = document.getElementById(label.htmlFor);
    if (input) input.setAttribute('aria-required', 'true');
  });
  document.querySelectorAll('.field-validation-error').forEach(error => {
    const field = document.getElementById(error.dataset.valmsgFor);
    if (!field) return;
    error.id ||= `${field.id}-error`;
    field.setAttribute('aria-invalid', 'true');
    field.setAttribute('aria-describedby', `${field.getAttribute('aria-describedby') || ''} ${error.id}`.trim());
  });
  document.querySelectorAll('.validation-summary-errors').forEach(summary => {
    summary.setAttribute('role', 'alert'); summary.setAttribute('tabindex', '-1');
  });
  document.querySelectorAll('form[method="post"]').forEach(form => {
    form.addEventListener('submit', event => {
      if (event.defaultPrevented || !form.checkValidity()) return;
      const button = event.submitter || form.querySelector('button[type="submit"]');
      if (button) { button.disabled = true; button.textContent = 'Đang xử lý…'; }
    });
  });
})();
window.addEventListener('pageshow', event => { if (event.persisted) location.reload(); });

// Revalidate a page restored from the browser back/forward cache after logout.
window.addEventListener('pageshow', event => { if (event.persisted) location.reload(); });
