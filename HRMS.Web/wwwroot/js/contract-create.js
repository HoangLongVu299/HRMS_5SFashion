'use strict';
(() => {
  const checkbox = document.querySelector('[data-contract-rehire]');
  const fields = document.querySelector('[data-rehire-fields]');
  if (!checkbox || !fields) return;
  const form = checkbox.closest('form');
  const firstFields = fields.querySelector('[data-first-employment-fields]');
  const rehireNote = fields.querySelector('[data-rehire-note]');
  let firstIds = new Set(form.dataset.firstEmploymentIds.split(',').filter(Boolean));
  const employee = form.querySelector('[data-employee]');
  const status = form.querySelector('[data-contract-employee-status]');
  const retry = form.querySelector('[data-reload-contract-employees]');
  const issues = form.querySelector('[data-contract-issues]');
  const save = form.querySelector('button[type="submit"]');
  const start = form.querySelector('#NGAYBATDAU');
  const duration = form.querySelector('#DurationMonths');
  const expiry = form.querySelector('#contract-expiry-preview');
  const signing = form.querySelector('#contract-signing-preview');
  if (!employee || !status) return;
  let request = 0, signingRequest = 0, loading = false, failed = false;
  function updateExpiry() {
    expiry.value = '';
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(start.value);
    const months = Number(duration.value);
    if (!match || !Number.isInteger(months) || months < 1) return;
    const [, y, m, d] = match.map(Number);
    if (y < 1753 || months > (9999 - y) * 12 + 12 - m) return;
    const offset = m - 1 + months;
    const year = y + Math.floor(offset / 12);
    const month = offset % 12;
    const day = Math.min(d, new Date(Date.UTC(year, month + 1, 0)).getUTCDate());
    expiry.value = [day, month + 1].map(part => String(part).padStart(2, '0')).join('/') + '/' + year;
  }
  async function updateSigning() {
    const sequence = ++signingRequest;
    signing.value = '';
    if (!employee.value) return;
    try {
      const url = new URL(form.dataset.signingUrl, location.origin);
      url.searchParams.set('employeeId', employee.value);
      const response = await fetch(url, {headers: {Accept: 'application/json'}});
      if (!response.ok) return;
      const result = await response.json();
      if (sequence === signingRequest && Number.isInteger(result.signing) && result.signing > 0)
        signing.value = String(result.signing);
    } catch {
      // A failed preview does not prevent the server from assigning a number on save.
    }
  }
  function updateSave() {
    if (save) save.disabled = loading || failed || !employee.value;
  }
  function refreshFields() {
    const first = !checkbox.checked && firstIds.has(employee.value);
    const receiving = Boolean(employee.value);
    fields.hidden = !receiving;
    firstFields.hidden = !first;
    rehireNote.hidden = !checkbox.checked;
    for (const select of fields.querySelectorAll('select')) {
      select.disabled = !receiving;
      select.required = receiving && select.name !== 'RehireSectionId';
    }
  }
  async function loadEmployees() {
    const sequence = ++request;
    loading = true; failed = false;
    // Changing business mode invalidates the old employee selection immediately.
    employee.replaceChildren(new Option(employee.options[0].text, ''));
    updateSigning();
    refreshFields();
    employee.disabled = true; employee.setAttribute('aria-busy', 'true');
    if (retry) retry.hidden = true;
    if (issues) issues.hidden = true;
    status.textContent = 'Đang tải danh sách nhân viên…'; updateSave();
    try {
      const url = new URL(form.dataset.contractEmployeesUrl, location.origin);
      url.searchParams.set('rehire', String(checkbox.checked));
      const response = await fetch(url, {headers: {Accept: 'application/json'}});
      if (sequence !== request) return;
      if (response.status === 401) {
        location.assign('/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search)); return;
      }
      if (!response.ok) throw new Error();
      const result = await response.json();
      if (sequence !== request) return;
      if (!Array.isArray(result.choices) || !Array.isArray(result.issues) ||
          result.choices.some(x => !Number.isInteger(x.id) || x.id < 1 || typeof x.text !== 'string') ||
          result.issues.some(x => typeof x !== 'string')) throw new Error();
      firstIds = new Set(result.choices.filter(row => row.firstEmployment).map(row => String(row.id)));
      for (const row of result.choices) employee.add(new Option(row.text, String(row.id)));
      if (issues) {
        const list = issues.querySelector('ul');
        list.replaceChildren(...result.issues.map(text => {const item = document.createElement('li'); item.textContent = text; return item;}));
        issues.hidden = result.issues.length === 0;
      }
      status.textContent = result.choices.length
        ? (checkbox.checked ? 'Chỉ hiển thị nhân viên đã nghỉ để tiếp nhận lại.' : 'Chỉ hiển thị hồ sơ chờ tiếp nhận.')
        : (checkbox.checked ? 'Không có nhân viên đã nghỉ trong danh sách này.' : 'Không có hồ sơ chờ tiếp nhận.');
    } catch {
      if (sequence === request) {
        failed = true; status.textContent = 'Không tải được danh sách. Vui lòng thử lại.';
        if (retry) retry.hidden = false;
      }
    } finally {
      if (sequence === request) {
        loading = false; employee.disabled = failed;
        employee.removeAttribute('aria-busy'); updateSave();
      }
    }
  }
  checkbox.addEventListener('change', loadEmployees);
  retry?.addEventListener('click', loadEmployees);
  employee.addEventListener('change', () => { refreshFields(); updateSave(); return updateSigning(); });
  start.addEventListener('input', updateExpiry);
  duration.addEventListener('input', updateExpiry);
  form.addEventListener('submit', event => {
    if (!loading && !failed && employee.value) return;
    event.preventDefault();
    status.textContent = loading ? 'Vui lòng chờ tải danh sách.' : 'Vui lòng chọn nhân viên trong danh sách hiện tại.';
  }, true);
  // Initial choices are server-rendered and validated again on POST; no initial AJAX needed.
  refreshFields(); updateSave(); updateExpiry(); updateSigning();
})();
