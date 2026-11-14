'use strict';
(() => {
  const form = document.querySelector('form[data-current-employment]');
  if (!form) return;
  const employee = form.querySelector('[data-employee]');
  const company = form.querySelector('[data-company]');
  const effective = form.querySelector('[name="NGAYHIEULUC"]');
  const department = form.querySelector('[data-old-department]');
  const warning = form.querySelector('[data-eligibility-warning]');
  const save = form.querySelector('button[type="submit"]');
  if (!employee || !company) return;
  let request = 0, loading = false, available = false;
  function updateSave() {if (save) save.disabled = loading || !available || !employee.value;}
  async function refresh() {
    const sequence = ++request;
    available = false; loading = false;
    if (warning) {warning.textContent = ''; warning.hidden = true;}
    if (department) department.textContent = 'Chọn nhân viên và ngày hiệu lực để xem.';
    if (!employee.value) {company.textContent = 'Chọn nhân viên để xem'; updateSave(); return;}
    loading = true; company.textContent = 'Đang tải thông tin nhân viên…'; updateSave();
    try {
      const url = new URL(form.dataset.infoUrl, location.origin);
      url.searchParams.set('employee', employee.value);
      if (effective?.value) url.searchParams.set('date', effective.value);
      const response = await fetch(url, {headers: {Accept: 'application/json'}});
      if (sequence !== request) return;
      if (response.status === 401) {
        location.assign('/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search)); return;
      }
      if (!response.ok) throw new Error();
      const result = await response.json();
      if (sequence !== request) return;
      if (typeof result.available !== 'boolean') throw new Error();
      available = result.available;
      company.textContent = result.company || 'Không có thông tin công ty hợp lệ.';
      if (department) department.textContent = result.department || 'Không xác định được phòng ban tại ngày đã chọn.';
      if (warning) {warning.textContent = result.warning || ''; warning.hidden = !warning.textContent;}
      if (!available) {
        const rejected = employee.selectedOptions[0]; employee.value = '';
        if (rejected?.value) rejected.remove();
      }
    } catch {
      if (sequence === request) {
        company.textContent = 'Không tải được thông tin. Chọn lại nhân viên hoặc tải lại trang để thử lại.';
        available = false;
      }
    } finally {if (sequence === request) {loading = false; updateSave();}}
  }
  employee.addEventListener('change', refresh);
  effective?.addEventListener('change', refresh);
  form.addEventListener('submit', event => {
    if (!loading && available && employee.value) return;
    event.preventDefault();
    company.textContent = loading ? 'Vui lòng chờ tải thông tin.' : 'Vui lòng chọn nhân viên đang làm hợp lệ; chọn lại để thử tải thông tin.';
  }, true);
  refresh();
})();
