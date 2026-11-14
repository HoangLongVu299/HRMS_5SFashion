'use strict';
(() => {
  const form = document.querySelector('[data-personnel]');
  if (!form) return;
  const employee = form.querySelector('[data-employee]');
  const period = form.querySelector('[data-period]');
  const company = form.querySelector('[data-company]');
  const effective = form.querySelector('[name="NGAYHIEULUC"]');
  const old = form.querySelector('[data-old-department]');
  const currentDecision = form.hasAttribute('data-current-decision');
  const warning = form.querySelector('[data-eligibility-warning]');
  const save = form.querySelector('button[type="submit"]');
  if (!employee || !period || !company) return;
  let choices = [], request = 0, departmentRequest = 0, loading = false;
  let loadFailed = false;
  function updateSave() {
    if (currentDecision && save) save.disabled = loading || loadFailed || !employee.value || !period.value || !choices.length;
  }
  function updateWarning() {
    if (!warning) return;
    warning.textContent = choices.find(x => String(x.id) === period.value)?.warning || choices[0]?.warning || '';
    warning.hidden = !warning.textContent;
  }
  async function department() {
    const sequence = ++departmentRequest;
    if (!old || !effective) return;
    if (!employee.value || !period.value || !effective.value) {
      old.textContent = 'Chọn đủ nhân viên, đợt và ngày hiệu lực.'; return;
    }
    old.textContent = 'Đang tải phòng ban cũ…';
    try {
      const url = new URL(form.dataset.departmentUrl, location.origin);
      url.search = new URLSearchParams({ employee: employee.value, period: period.value, date: effective.value });
      const response = await fetch(url, { headers: { Accept: 'application/json' } }); if (response.status === 401) { location.assign('/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search)); return; } if (!response.ok) throw new Error();
      const result = await response.json();
      if (sequence === departmentRequest) old.textContent = result.text;
    } catch {
      if (sequence === departmentRequest) old.textContent = 'Không tải được phòng ban cũ. Kiểm tra kết nối và thử lại.';
    }
  }
  async function periods(keep) {
    const sequence = ++request, selected = keep ? period.value : '';
    // Preserve server-rendered options on an initial refresh failure (including validation redisplay).
    const previous = keep ? Array.from(period.options, option => ({text: option.text, value: option.value})) : [];
    const previousCompany = company.textContent;
    ++departmentRequest; choices = [];
    loadFailed = false;
    if (warning) { warning.textContent = ''; warning.hidden = true; }
    if (old) old.textContent = 'Chọn đủ nhân viên, đợt và ngày hiệu lực.';
    period.replaceChildren(new Option('— Chọn đợt —', '')); period.disabled = true;
    loading = true; period.setAttribute('aria-busy', 'true');
    updateSave();
    company.textContent = 'Đang tải đợt…';
    if (!employee.value) {
      period.disabled = false; loading = false; period.removeAttribute('aria-busy');
      updateSave();
      company.textContent = 'Chọn nhân viên trước'; await department(); return;
    }
    try {
      const url = new URL(form.dataset.periodUrl, location.origin); url.searchParams.set('employee', employee.value);
      const response = await fetch(url, { headers: { Accept: 'application/json' } }); if (response.status === 401) { location.assign('/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search)); return; } if (!response.ok) throw new Error();
      const result = await response.json(); if (sequence !== request) return;
      if (!Array.isArray(result)) throw new Error();
      choices = result;
      for (const row of choices) period.add(new Option(row.text, String(row.id)));
      period.value = selected; period.disabled = false;
      company.textContent = choices.find(x => String(x.id) === period.value)?.company || (choices.length ? 'Chọn đợt để xem' : (currentDecision ? 'Nhân viên không còn đợt làm việc hợp lệ. Vui lòng chọn lại nhân viên.' : 'Nhân viên chưa có đợt làm việc'));
      if (currentDecision && !choices.length) {
        const rejected = employee.selectedOptions[0];
        employee.value = '';
        if (rejected?.value) rejected.remove();
      }
      updateWarning();
      await department();
    } catch {
      if (sequence === request) {
        loadFailed = true;
        if (keep && !currentDecision) {
          period.replaceChildren(...previous.map(option => new Option(option.text, option.value)));
          period.value = selected;
          choices = selected ? [{id: selected, company: previousCompany}] : [];
        }
        period.disabled = false;
        company.textContent = 'Không tải được đợt; tải lại trang để thử lại.';
      }
    } finally {
      if (sequence === request) { loading = false; period.removeAttribute('aria-busy'); updateSave(); }
    }
  }
  employee.addEventListener('change', () => periods(false));
  period.addEventListener('change', () => {
    company.textContent = choices.find(x => String(x.id) === period.value)?.company || 'Chọn đợt để xem';
    updateWarning(); updateSave();
    department();
  });
  effective?.addEventListener('change', department);
  // A disabled select is omitted from POST: wait for loading instead of losing IDDOT.
  form.addEventListener('submit', event => {
    if (!loading && (!currentDecision || (!loadFailed && employee.value && period.value && choices.some(x => String(x.id) === period.value)))) return;
    event.preventDefault();
    company.textContent = loading ? 'Đang tải dữ liệu. Vui lòng chờ trước khi lưu.' : 'Vui lòng chọn nhân viên và đợt hợp lệ; tải lại trang nếu không tải được dữ liệu.';
  }, true);
  periods(true);
})();
