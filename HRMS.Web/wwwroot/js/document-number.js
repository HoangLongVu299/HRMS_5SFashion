'use strict';
(() => {
  const form = document.querySelector('form[data-number-url]');
  if (!form) return;
  const number = form.querySelector('[name="' + form.dataset.numberField + '"]');
  const date = form.querySelector('[name="' + form.dataset.numberDate + '"]');
  const type = form.dataset.numberType ? form.querySelector('[name="' + form.dataset.numberType + '"]') : null;
  const status = form.querySelector('[data-number-status]');
  if (!number || !date || !status) return;
  let request = 0;
  async function refresh() {
    const sequence = ++request;
    number.value = ''; number.placeholder = 'Tự tạo khi lưu';
    if (type && !type.value) {status.textContent = 'Chọn loại Khen thưởng hoặc Kỷ luật để tạo số quyết định.'; return;}
    status.textContent = 'Đang xác định số văn bản dự kiến…';
    try {
      const url = new URL(form.dataset.numberUrl, location.origin);
      if (date.value) url.searchParams.set('date', date.value);
      if (type) url.searchParams.set('type', type.value);
      const response = await fetch(url, {headers: {Accept: 'application/json'}});
      if (sequence !== request) return;
      if (response.status === 401) {
        location.assign('/Account/Login?returnUrl=' + encodeURIComponent(location.pathname + location.search)); return;
      }
      if (!response.ok) throw new Error();
      const result = await response.json();
      if (sequence !== request) return;
      if (typeof result.number !== 'string' || !result.number) throw new Error();
      number.value = result.number;
      status.textContent = 'Số dự kiến theo năm của ngày văn bản; hệ thống xác nhận số chính thức khi lưu.';
    } catch {
      if (sequence === request) status.textContent = 'Chưa tải được số dự kiến. Số chính thức vẫn được tự tạo khi lưu hợp lệ.';
    }
  }
  date.addEventListener('change', refresh);
  type?.addEventListener('change', refresh);
})();
