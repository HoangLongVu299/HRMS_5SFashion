'use strict';
(() => {
  const limit = 50;
  const normalize = text => text.toLowerCase().normalize('NFD').replace(/\p{M}/gu, '').replace(/đ/g, 'd');

  for (const select of document.querySelectorAll('select[data-employee-search="true"]')) {
    const id = `${select.id}-search`;
    const label = select.labels[0];
    const wrapper = document.createElement('div');
    wrapper.className = 'employee-search';

    const input = document.createElement('input');
    input.type = 'text';
    input.id = id;
    input.className = 'form-select';
    input.placeholder = select.options[0].text;
    input.autocomplete = 'off';
    input.spellcheck = false;
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-controls', `${id}-results`);
    input.setAttribute('aria-expanded', 'false');
    for (const attribute of ['aria-required', 'aria-invalid', 'aria-describedby']) {
      if (select.hasAttribute(attribute)) input.setAttribute(attribute, select.getAttribute(attribute));
    }

    const menu = document.createElement('div');
    menu.className = 'employee-search-menu';
    menu.hidden = true;

    const list = document.createElement('div');
    list.id = `${id}-results`;
    list.setAttribute('role', 'listbox');
    list.setAttribute('aria-label', 'Nhân viên');

    const status = document.createElement('div');
    status.className = 'employee-search-status';
    status.setAttribute('role', 'status');
    const error = document.createElement('div');
    error.className = 'text-danger';
    error.id = `${id}-required`;
    error.setAttribute('role', 'alert');
    error.hidden = true;
    error.textContent = 'Vui lòng chọn nhân viên trong danh sách.';
    const description = input.getAttribute('aria-describedby');
    input.setAttribute('aria-describedby', [description, error.id].filter(Boolean).join(' '));

    menu.append(list, status);
    wrapper.append(input, menu);
    select.after(wrapper, error);
    select.hidden = true;
    if (label) label.htmlFor = input.id;

    let entries = [];
    let matches = [];
    let active = -1;

    function setActive(index) {
      list.children[active]?.classList.remove('is-active');
      active = index;
      const item = list.children[active];
      if (item) {
        item.classList.add('is-active');
        input.setAttribute('aria-activedescendant', item.id);
        item.scrollIntoView({block: 'nearest'});
      } else {
        input.removeAttribute('aria-activedescendant');
      }
    }

    function close() {
      menu.hidden = true;
      input.setAttribute('aria-expanded', 'false');
      input.removeAttribute('aria-activedescendant');
      input.value = select.value ? select.selectedOptions[0].text : '';
    }

    function render() {
      const query = normalize(input.value).trim();
      const terms = query ? query.split(/\s+/) : [];
      const filtered = entries.filter(entry => terms.every(term =>
        /^\d+$/.test(term) ? entry.value.startsWith(term) : entry.search.includes(term)));
      const exact = filtered.findIndex(entry => entry.value === query);
      if (exact > 0) filtered.unshift(filtered.splice(exact, 1)[0]);
      matches = filtered.slice(0, limit);
      if (select.value && !query) matches.unshift({value: '', text: 'Bỏ chọn nhân viên'});

      const items = matches.map((entry, index) => {
        const item = document.createElement('div');
        item.id = `${id}-option-${index}`;
        item.dataset.index = index;
        item.setAttribute('role', 'option');
        item.setAttribute('aria-selected', String(entry.value === select.value));
        item.textContent = entry.text;
        return item;
      });
      list.replaceChildren(...items);
      list.scrollTop = 0;
      setActive(-1);
      status.textContent = filtered.length === 0
        ? (query ? 'Không tìm thấy nhân viên phù hợp.' : 'Danh sách hiện không có nhân viên.')
        : filtered.length > limit ? `Hiển thị ${limit}/${filtered.length}. Nhập tên hoặc mã để thu hẹp kết quả.` : '';
      status.hidden = !status.textContent;
    }

    function open(clear) {
      if (input.disabled || !menu.hidden) return;
      if (clear) input.value = '';
      menu.hidden = false;
      input.setAttribute('aria-expanded', 'true');
      render();
    }

    function sync() {
      input.disabled = select.disabled;
      if (!error.hidden && (select.value || select.disabled)) {
        error.hidden = true;
        input.removeAttribute('aria-invalid');
      }
      close();
    }

    function readOptions() {
      entries = Array.from(select.options)
        .filter(option => option.value && !option.disabled)
        .map(({value, text}) => ({value, text, search: normalize(text)}));
      sync();
    }

    function choose(index) {
      if (select.disabled || !matches[index]) return;
      select.value = matches[index].value;
      error.hidden = true;
      input.removeAttribute('aria-invalid');
      select.dispatchEvent(new Event('change', {bubbles: true}));
    }

    input.addEventListener('focus', () => open(true));
    input.addEventListener('click', () => open(true));
    input.addEventListener('blur', close);
    input.addEventListener('input', () => {
      if (menu.hidden) open(false);
      else render();
    });
    input.addEventListener('keydown', event => {
      if (event.isComposing || event.keyCode === 229) return;
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        open(true);
        if (!matches.length) return;
        const next = event.key === 'ArrowDown'
          ? Math.min(active + 1, matches.length - 1)
          : active <= 0 ? matches.length - 1 : active - 1;
        setActive(next);
      } else if (event.key === 'Enter' && !menu.hidden) {
        event.preventDefault();
        choose(active >= 0 ? active : matches.length === 1 ? 0 : -1);
      } else if (event.key === 'Escape' && !menu.hidden) {
        event.preventDefault();
        event.stopPropagation();
        close();
        input.select();
      }
    });

    // Keep focus on the combobox when clicking a result.
    list.addEventListener('mousedown', event => {
      if (event.target.closest('[role="option"]')) event.preventDefault();
    });
    list.addEventListener('click', event => {
      const item = event.target.closest('[role="option"]');
      if (item) choose(Number(item.dataset.index));
    });

    select.addEventListener('change', sync);
    select.addEventListener('invalid', event => {
      event.preventDefault();
      input.setAttribute('aria-invalid', 'true');
      error.hidden = false;
      input.focus();
    });
    select.form?.addEventListener('reset', () => queueMicrotask(sync));
    // Refresh the search list whenever a form updates the available options.
    new MutationObserver(readOptions).observe(select, {
      childList: true, subtree: true, attributes: true, attributeFilter: ['disabled']
    });
    readOptions();
  }
})();
