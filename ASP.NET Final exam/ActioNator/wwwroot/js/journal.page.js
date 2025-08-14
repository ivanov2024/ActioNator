/*
 * Journal Page Pagination + Search (Server-driven)
 * - Loads partial via /User/Journal/GetJournalPartial
 * - Handles Prev/Next/page buttons and debounced search
 * - Wires Edit/Delete from server-rendered cards
 * - Dispatches 'goals-refreshed' after updates to reapply Read More behavior
 */
(() => {
  'use strict';

  // ===== Utilities =====
  const qs = (sel, root = document) => root.querySelector(sel);
  const qsa = (sel, root = document) => Array.from(root.querySelectorAll(sel));

  function debounce(fn, wait = 300) {
    let t; return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), wait); };
  }

  function getAntiForgeryToken() {
    return qs('input[name="__RequestVerificationToken"]')?.value || '';
  }

  // Normalize and constrain user-provided search input on the client as a first line of defense.
  function sanitizeTerm(input) {
    if (!input) return '';
    let s = String(input).trim();
    // remove control characters (including DEL)
    s = s.replace(/[\u0000-\u001F\u007F]/g, '');
    const MAX = 100;
    if (s.length > MAX) s = s.substring(0, MAX);
    return s;
  }

  // Toast wrapper (fallback to alert)
  let _toastLock = false;
  function showToast(message, type = 'info') {
    try {
      if (_toastLock) return; _toastLock = true;
      const mapped = type === 'info' ? 'success' : type;
      const evt = new CustomEvent('show-toast', { detail: { message, type: mapped } });
      window.dispatchEvent(evt);
    } catch (e) {
      alert(`${type.toUpperCase()}: ${message}`);
    } finally {
      setTimeout(() => { _toastLock = false; }, 0);
    }
  }

  // ===== Pagination Loader =====
  const JournalPager = (() => {
    let currentFetch = null;
    let docListenersBound = false;

    function getState(root = document) {
      const meta = qs('#journalPaginationContainer', root);
      const page = parseInt(meta?.dataset.currentPage || '1', 10);
      const totalPages = parseInt(meta?.dataset.totalPages || '1', 10);
      const pageSize = parseInt(meta?.dataset.pageSize || '3', 10);
      const searchTerm = meta?.dataset.searchTerm || '';
      return { page, totalPages, pageSize, searchTerm };
    }

    async function loadPartial({ searchTerm, page, pageSize } = {}) {
      const container = qs('#journal-partial-container');
      if (!container) return;

      const params = new URLSearchParams({
        searchTerm: sanitizeTerm(searchTerm ?? qs('#journal-search-input')?.value ?? ''),
        page: String(page || 1),
        pageSize: String(pageSize || 3)
      });

      const url = `/User/Journal/GetJournalPartial?${params.toString()}`;
      try {
        if (currentFetch) { /* let previous complete; not aborting for simplicity */ }
        currentFetch = fetch(url, {
          headers: { 'X-Requested-With': 'XMLHttpRequest' },
          credentials: 'same-origin'
        });
        const res = await currentFetch;
        if (!res.ok) throw new Error(`Failed to load entries (${res.status})`);
        const html = await res.text();
        container.innerHTML = html;
        bindInsideContainer();
        // reapply read-more behavior if any shared script listens to this
        document.dispatchEvent(new Event('goals-refreshed'));
      } catch (e) {
        console.error('Journal partial load failed:', e);
        showToast(e.message || 'Failed to load journal entries', 'error');
      } finally {
        currentFetch = null;
      }
    }

    function bindPaginationClicks(rootEl) {
      // Delegate click events for Prev/Next and numbered buttons
      rootEl.addEventListener('click', (e) => {
        const btnPage = e.target.closest('button[data-page]');
        const btnAction = e.target.closest('button[data-action]');
        if (!btnPage && !btnAction) return;
        e.preventDefault();

        const { page, totalPages, pageSize } = getState(rootEl);
        let next = page;
        if (btnPage) {
          next = parseInt(btnPage.getAttribute('data-page') || `${page}`, 10);
        } else if (btnAction) {
          const action = btnAction.getAttribute('data-action');
          if (action === 'prev') next = Math.max(1, page - 1);
          if (action === 'next') next = Math.min(totalPages, page + 1);
        }
        const search = sanitizeTerm(qs('#journal-search-input')?.value || '');
        loadPartial({ searchTerm: search, page: next, pageSize });
      });
    }

    function bindDropdowns(rootEl) {
      // Toggle three-dots dropdowns
      rootEl.addEventListener('click', (e) => {
        const trigger = e.target.closest('.journal-menu-trigger');
        if (!trigger) return;
        e.preventDefault(); e.stopPropagation();
        const menu = trigger.parentElement?.querySelector('.journal-dropdown');
        if (!menu) return;
        const isHidden = menu.classList.contains('hidden');
        // close all
        qsa('.journal-dropdown', rootEl).forEach(m => m.classList.add('hidden'));
        if (isHidden) menu.classList.remove('hidden');
      });

      // Close dropdowns on outside click
      if (!docListenersBound) {
        document.addEventListener('click', (e) => {
          if (!e.target.closest('.journal-dropdown') && !e.target.closest('.journal-menu-trigger')) {
            qsa('.journal-dropdown', rootEl).forEach(m => m.classList.add('hidden'));
          }
        });
        document.addEventListener('keydown', (e) => {
          if (e.key === 'Escape') {
            qsa('.journal-dropdown', rootEl).forEach(m => m.classList.add('hidden'));
          }
        });
        docListenersBound = true;
      }
    }

    function bindReadMore(rootEl) {
      // If shared read-more.js is present, it may hook via goals-refreshed.
      // Also provide a basic fallback toggler here.
      rootEl.addEventListener('click', (e) => {
        const btn = e.target.closest('.read-more-btn');
        if (!btn) return;
        e.preventDefault();
        const card = btn.closest('.journal-card');
        const desc = qs('.goal-description', card);
        const more = qs('.read-more', btn);
        const less = qs('.read-less', btn);
        if (!desc) return;
        const expanded = desc.classList.toggle('expanded');
        if (expanded) {
          desc.style.maxHeight = `${desc.scrollHeight}px`;
          desc.style.overflow = 'visible';
          desc.style.whiteSpace = 'normal';
          desc.style.textOverflow = 'clip';
          more?.classList.add('hidden');
          less?.classList.remove('hidden');
        } else {
          desc.style.maxHeight = '1.5em';
          desc.style.overflow = 'hidden';
          desc.style.whiteSpace = 'nowrap';
          desc.style.textOverflow = 'ellipsis';
          more?.classList.remove('hidden');
          less?.classList.add('hidden');
        }
      });
    }

    function bindEditDelete(rootEl) {
      // Edit
      rootEl.addEventListener('click', (e) => {
        const btn = e.target.closest('.journal-edit-btn');
        if (!btn) return;
        e.preventDefault(); e.stopPropagation();
        const entry = {
          id: btn.dataset.id,
          title: btn.dataset.title || '',
          content: btn.dataset.content || '',
          moodTag: btn.dataset.mood || '',
          createdAt: btn.dataset.createdAt || new Date().toISOString()
        };
        // Notify existing modal listeners and open modal
        try {
          window.dispatchEvent(new CustomEvent('journal-edit-mode', { detail: { isEditMode: true, entry } }));
          if (typeof window.openJournalModal === 'function') {
            window.openJournalModal();
          } else if (typeof window.JournalUI?.openAddModal === 'function') {
            // Fallback: trigger add modal in edit mode consumers
            window.JournalUI.openAddModal();
          }
        } catch {}
      });

      // Delete
      rootEl.addEventListener('click', (e) => {
        const btn = e.target.closest('.journal-delete-btn');
        if (!btn) return;
        e.preventDefault(); e.stopPropagation();
        const id = btn.dataset.id;
        const proceed = () => doDelete(id);
        if (window.openModal) {
          window.openModal({
            type: 'delete',
            title: 'Delete Journal Entry',
            message: 'Are you sure you want to delete this journal entry? This action cannot be undone.',
            confirmText: 'Delete',
            cancelText: 'Cancel'
          });
          const once = (ev) => {
            if (ev?.detail?.type === 'delete') proceed();
            window.removeEventListener('modal-confirmed', once);
          };
          window.addEventListener('modal-confirmed', once);
        } else if (confirm('Delete this journal entry?')) {
          proceed();
        }
      });

      async function doDelete(id) {
        try {
          const token = getAntiForgeryToken();
          const res = await fetch('/User/Journal/Delete', {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'X-Requested-With': 'XMLHttpRequest',
              'X-CSRF-TOKEN': token
            },
            credentials: 'same-origin',
            body: JSON.stringify({ Id: id, __RequestVerificationToken: token })
          });
          if (!res.ok) {
            let msg = 'Failed to delete entry';
            try {
              const ct = res.headers.get('content-type');
              if (ct && ct.includes('application/json')) {
                const data = await res.json();
                msg = data.error || msg;
              } else {
                msg = await res.text();
              }
            } catch {}
            throw new Error(msg);
          }
          // Try parse json { success, message }
          let data = {};
          try { data = await res.json(); } catch {}
          showToast(data.message || 'Journal entry deleted successfully', 'success');
          window.dispatchEvent(new CustomEvent('journal-entries-changed', { detail: { action: 'delete', id } }));
        } catch (err) {
          console.error('Delete failed:', err);
          showToast(err.message || 'Failed to delete entry', 'error');
        }
      }
    }

    function bindInsideContainer() {
      const rootEl = qs('#journal-partial-container');
      if (!rootEl) return;
      if (rootEl.dataset.listenersBound === 'true') return;
      bindPaginationClicks(rootEl);
      bindDropdowns(rootEl);
      bindReadMore(rootEl);
      bindEditDelete(rootEl);
      rootEl.dataset.listenersBound = 'true';
    }

    return { loadPartial, bindInsideContainer, getState };
  })();

  // ===== Global UI helpers for header button =====
  window.JournalUI = window.JournalUI || {
    openAddModal() {
      try {
        window.dispatchEvent(new CustomEvent('journal-edit-mode', { detail: { isEditMode: false, entry: { id: '', title: '', content: '', moodTag: '' } } }));
        if (typeof window.openJournalModal === 'function') window.openJournalModal();
      } catch {}
    }
  };

  // ===== Wire up page =====
  function bindSearch() {
    const input = qs('#journal-search-input');
    if (!input) return;
    const onChange = debounce(() => {
      const term = sanitizeTerm(input.value || '');
      JournalPager.loadPartial({ searchTerm: term, page: 1 });
    }, 350);
    input.addEventListener('input', onChange);
  }

  function bindGlobalRefresh() {
    // When entries change (save/delete), refresh current page with current search term
    window.addEventListener('journal-entries-changed', () => {
      const { page, pageSize } = JournalPager.getState(document);
      const term = sanitizeTerm(qs('#journal-search-input')?.value || '');
      JournalPager.loadPartial({ searchTerm: term, page, pageSize });
    });
  }

  function init() {
    JournalPager.bindInsideContainer();
    bindSearch();
    bindGlobalRefresh();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
