'use strict';
(() => {
  const reset=document.querySelector('[data-reset-contract-template]');
  const target=document.querySelector('textarea[name="NOIDUNG"]');
  const source=document.querySelector('[data-default-contract-template]');
  if(!reset||!target||!source)return;
  reset.addEventListener('click',()=>{
    if(target.value.trim()!==source.value.trim() && !window.confirm('Thay nội dung đang soạn bằng mẫu hợp đồng chung? Thay đổi chỉ được lưu khi bạn bấm Lưu.'))return;
    target.value=source.value;target.dispatchEvent(new Event('input',{bubbles:true}));target.focus();
  });
})();
