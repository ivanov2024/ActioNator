/*
 * Goals Page (Alpine.js + ES2020)
 * A maintainable, modular refactor consolidating all Goals-related JS
 * previously spread across goal.js, read-more.js and modal-handler.js.
 *
 * Key ideas:
 * - Single responsibility modules (API, UI, Alpine component)
 * - Event delegation to avoid re-binding and cloning nodes
 * - Alpine component drives page lifecycle; works without markup changes too
 * - Zero regressions: preserves existing behaviors and server endpoints
 */
(() => {
  'use strict';

  // =============== Utilities ===============
  const qs = (sel, root = document) => root.querySelector(sel);
  const qsa = (sel, root = document) => Array.from(root.querySelectorAll(sel));
  const toISODate = (d) => (d instanceof Date ? d.toISOString().split('T')[0] : String(d));

  function getAntiForgeryToken() {
    return qs('input[name="__RequestVerificationToken"]')?.value || '';
  }

  // Toast emitter with loop guard (matches workout.js pattern)
  let _isEmittingToast = false;
  function showToast(message, type = 'info') {
    const mapped = type === 'info' ? 'success' : type;
    try {
      if (_isEmittingToast) return; // prevent loops
      _isEmittingToast = true;
      const evt = new CustomEvent('show-toast', { detail: { message, type: mapped } });
      window.dispatchEvent(evt);
    } catch (e) {
      // Fallback
      alert(`${mapped.toUpperCase()}: ${message}`);
    } finally {
      setTimeout(() => { _isEmittingToast = false; }, 0);
    }
  }

  // =============== API Layer ===============
  const GoalsAPI = {
    async getPartial(filter = 'all') {
      const res = await fetch(`/User/Goal/GetGoalPartial?filter=${encodeURIComponent(filter)}`, {
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error('Failed to load goals');
      return res.text();
    },

    async create(model) {
      const token = getAntiForgeryToken();
      const body = JSON.stringify({
        Title: model.title,
        Description: model.description,
        DueDate: model.dueDate,
        Completed: !!model.completed,
        __RequestVerificationToken: token
      });
      const res = await fetch('/User/Goal/Create', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': token,
          'RequestVerificationToken': token,
          'X-Requested-With': 'XMLHttpRequest'
        },
        credentials: 'same-origin',
        body
      });
      if (!res.ok) throw new Error(`Server error: ${res.status}`);
      return res.json();
    },

    async update(model) {
      const token = getAntiForgeryToken();
      const body = JSON.stringify({
        Id: model.id,
        Title: model.title,
        Description: model.description,
        DueDate: model.dueDate,
        Completed: !!model.completed,
        __RequestVerificationToken: token
      });
      const res = await fetch('/User/Goal/Update', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-CSRF-TOKEN': token,
          'RequestVerificationToken': token,
          'X-Requested-With': 'XMLHttpRequest'
        },
        credentials: 'same-origin',
        body
      });
      if (!res.ok) throw new Error(`Server error: ${res.status}`);
      return res.json();
    },

    async delete(id) {
      // Prefer route with id segment to match existing working code
      const token = getAntiForgeryToken();
      const res = await fetch(`/User/Goal/Delete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: {
          'RequestVerificationToken': token,
          'X-Requested-With': 'XMLHttpRequest'
        },
        credentials: 'same-origin'
      });

      if (res.ok) return { success: true };

      // Fallback to body-based endpoint if server expects JSON
      try {
        const resAlt = await fetch('/User/Goal/Delete', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': token
          },
          credentials: 'same-origin',
          body: JSON.stringify({ id })
        });
        if (!resAlt.ok) {
          const data = await resAlt.json().catch(() => ({}));
          throw new Error(data.message || 'Failed to delete goal');
        }
        return resAlt.json().catch(() => ({ success: true }));
      } catch (e) {
        throw e;
      }
    },

    async toggleComplete(id) {
      const token = getAntiForgeryToken();
      const res = await fetch(`/User/Goal/ToggleComplete/${encodeURIComponent(id)}`, {
        method: 'POST',
        headers: { 'RequestVerificationToken': token },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error('Failed to update goal status');
      return res.json().catch(() => ({ success: true }));
    },

    async getGoalsJson() {
      const token = getAntiForgeryToken();
      const res = await fetch('/User/Goal/GetGoals', {
        headers: {
          'Accept': 'application/json',
          'RequestVerificationToken': token
        },
        credentials: 'same-origin'
      });
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      return res.json();
    }
  };

  // =============== UI Layer ===============
  const UI = (() => {
    let documentClickBound = false;

    function setMinDueDates() {
      const today = new Date();
      const tomorrow = new Date(today);
      tomorrow.setDate(tomorrow.getDate() + 1);
      const minDate = toISODate(tomorrow);
      qsa('input[type="date"]').forEach(el => { el.min = minDate; });
    }

    function closeAllDropdowns() {
      qsa('.dropdown-menu').forEach(dd => {
        dd.style.opacity = '0';
        dd.style.transform = 'translateY(-10px)';
        dd.style.pointerEvents = 'none';
        dd.style.visibility = 'hidden';
      });
      window.activeDropdown = null;
    }

    function openDropdown(dropdown) {
      closeAllDropdowns();
      dropdown.style.display = 'block';
      dropdown.style.pointerEvents = 'auto';
      void dropdown.offsetHeight; // reflow
      dropdown.style.visibility = 'visible';
      dropdown.style.opacity = '1';
      dropdown.style.transform = 'translateY(0)';
      window.activeDropdown = dropdown;
    }

    function toggleCardDescription(card) {
      const description = qs('.goal-description', card);
      const btn = qs('.read-more-btn', card);
      if (!description || !btn) return;
      const readMoreText = qs('.read-more', btn);
      const readLessText = qs('.read-less', btn);

      card.classList.toggle('expanded');
      const expanded = card.classList.contains('expanded');

      if (expanded) {
        description.classList.add('expanded');
        // For safety, support maxHeight technique used previously
        description.style.maxHeight = `${description.scrollHeight}px`;
        description.style.overflow = 'visible';
        description.style.whiteSpace = 'normal';
        description.style.textOverflow = 'clip';
        readMoreText?.classList.add('hidden');
        readLessText?.classList.remove('hidden');
      } else {
        description.classList.remove('expanded');
        description.style.maxHeight = '1.5em';
        description.style.overflow = 'hidden';
        description.style.whiteSpace = 'nowrap';
        description.style.textOverflow = 'ellipsis';
        readMoreText?.classList.remove('hidden');
        readLessText?.classList.add('hidden');
      }
    }

    function parseDisplayDate(text) {
      // Parses "MMM d, yyyy" -> "yyyy-MM-dd"; returns empty string on failure
      if (!text) return '';
      const m = text.match(/([A-Za-z]+)\s+(\d{1,2}),\s*(\d{4})/);
      if (!m) return '';
      const monthMap = {
        Jan: '01', Feb: '02', Mar: '03', Apr: '04', May: '05', Jun: '06',
        Jul: '07', Aug: '08', Sep: '09', Oct: '10', Nov: '11', Dec: '12'
      };
      const monKey = m[1].slice(0, 3);
      const mm = monthMap[monKey] || '';
      const dd = String(m[2]).padStart(2, '0');
      const yyyy = m[3];
      return mm ? `${yyyy}-${mm}-${dd}` : '';
    }

    function openModal(modalId) {
      const modal = document.getElementById(modalId);
      if (!modal) return;
      document.body.style.overflow = 'hidden';
      document.body.style.paddingRight = '15px';
      modal.classList.remove('hidden');
      modal.setAttribute('aria-hidden', 'false');
      document.body.classList.add('modal-open');
      setTimeout(() => {
        const first = qsa('button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])', modal)[0];
        first?.focus();
      }, 100);
    }

    function closeModal(modalId) {
      const modal = document.getElementById(modalId);
      if (!modal) return;
      document.body.style.overflow = '';
      document.body.style.paddingRight = '';
      document.body.classList.remove('modal-open');
      modal.setAttribute('aria-hidden', 'true');
      modal.classList.add('hidden');
      // Clear error messages on close
      qsa('.error-message', modal).forEach(m => m.classList.add('hidden'));
    }

    function displayValidationErrors(errors, prefix = '') {
      clearValidationErrors(prefix);
      const formError = document.getElementById(`${prefix}form-error`);
      formError?.classList.remove('hidden');
      for (const field in errors) {
        const list = errors[field];
        if (Array.isArray(list) && list.length > 0) {
          const fieldName = field.charAt(0).toLowerCase() + field.slice(1);
          const el = document.getElementById(`${prefix}${fieldName}-error`);
          if (el) {
            el.textContent = list[0];
            el.classList.remove('hidden');
          }
        }
      }
    }

    function clearValidationErrors(prefix = '') {
      qsa('[id$="-error"]').forEach(el => {
        if (prefix && !el.id.startsWith(prefix)) return;
        el.classList.add('hidden');
        el.textContent = '';
      });
      const formError = document.getElementById(`${prefix}form-error`);
      formError?.classList.add('hidden');
    }

    function bindGlobalHandlers() {
      // Close dropdowns on outside click / ESC
      if (!documentClickBound) {
        document.addEventListener('click', (e) => {
          if (!e.target.closest('.dropdown-container') && !e.target.closest('.goal-menu-btn')) {
            closeAllDropdowns();
          }
          if (e.target.classList.contains('modal-overlay')) {
            const modal = e.target.closest('.modal');
            modal && closeModal(modal.id);
          }
        });
        document.addEventListener('keydown', (e) => {
          if (e.key === 'Escape') {
            // Close any open modal
            const openModalEl = qsa('.modal').find(m => m.getAttribute('aria-hidden') === 'false');
            if (openModalEl) closeModal(openModalEl.id);
            closeAllDropdowns();
          }
        });
        documentClickBound = true;
      }

      // Modal close buttons
      qsa('.modal-close-btn, .modal-cancel-btn').forEach(btn => {
        btn.addEventListener('click', () => {
          const modal = btn.closest('.modal');
          modal && closeModal(modal.id);
        });
      });
    }

    function bindContainerEvents() {
      const container = document.getElementById('goals-container');
      if (!container || container.dataset.listenersBound === 'true') return;

      // Delegated: dropdown toggle
      container.addEventListener('click', (e) => {
        const btn = e.target.closest('.goal-menu-btn');
        if (btn) {
          e.preventDefault();
          e.stopPropagation();
          const dd = btn.parentElement?.querySelector('.dropdown-menu');
          if (!dd) return;
          const visible = getComputedStyle(dd).visibility === 'visible' || parseFloat(getComputedStyle(dd).opacity) > 0.1;
          visible ? closeAllDropdowns() : openDropdown(dd);
          return;
        }
      });

      // Delegated: read more
      container.addEventListener('click', (e) => {
        const btn = e.target.closest('.read-more-btn');
        if (btn) {
          e.preventDefault();
          e.stopPropagation();
          const card = btn.closest('.goal-card');
          card && toggleCardDescription(card);
        }
      });

      // Delegated: card expand/collapse when clicking card but not controls
      container.addEventListener('click', (e) => {
        const card = e.target.closest('.goal-card');
        if (!card) return;
        if (
          e.target.closest('.goal-menu-btn') ||
          e.target.closest('.dropdown-menu') ||
          e.target.closest('.edit-goal-btn') ||
          e.target.closest('.delete-goal-btn') ||
          e.target.closest('.read-more-btn')
        ) return;
        toggleCardDescription(card);
      });

      // Delegated: edit button
      container.addEventListener('click', async (e) => {
        const editBtn = e.target.closest('.edit-goal-btn');
        if (!editBtn) return;
        e.stopPropagation();
        const card = editBtn.closest('.goal-card');
        const id = editBtn.dataset.id || card?.dataset.id;
        let goal = { id };
        if (card) {
          goal.title = qs('.goal-title', card)?.textContent?.trim() || '';
          goal.description = qs('.goal-description', card)?.textContent?.trim() || '';
          const dueText = qs('.goal-due-date span', card)?.textContent?.trim() || '';
          goal.dueDate = parseDisplayDate(dueText);
          goal.isCompleted = (card?.dataset?.status === 'completed');
        }
        if (!goal.dueDate) {
          // Fallback to JSON endpoint if available
          try {
            const list = await GoalsAPI.getGoalsJson();
            const found = Array.isArray(list) ? list.find(g => String(g.id) === String(id)) : null;
            if (found) {
              goal.title = found.title || goal.title;
              goal.description = found.description || goal.description;
              goal.dueDate = (typeof found.dueDate === 'string' && found.dueDate.includes('T')) ? found.dueDate.split('T')[0] : (found.dueDate || goal.dueDate);
              goal.isCompleted = !!found.completed;
            }
          } catch (err) {
            console.warn('Fallback JSON fetch failed:', err);
          }
        }
        // Fill and open edit modal
        const form = document.getElementById('edit-goal-form');
        if (!form) return;
        (qs('#edit-goal-id') || {}).value = goal.id || '';
        (qs('#edit-goal-title') || {}).value = goal.title || '';
        (qs('#edit-goal-description') || {}).value = goal.description || '';
        if (qs('#edit-due-date')) qs('#edit-due-date').value = goal.dueDate || '';
        if (qs('#edit-goal-completed')) qs('#edit-goal-completed').checked = !!goal.isCompleted;
        clearValidationErrors('edit-');
        openModal('edit-goal-modal');
      });

      // Delegated: delete button
      container.addEventListener('click', (e) => {
        const delBtn = e.target.closest('.delete-goal-btn');
        if (!delBtn) return;
        e.stopPropagation();
        const id = delBtn.dataset.id;
        confirmDelete(id);
      });

      // Delegated: toggle completion if a toggle exists
      container.addEventListener('click', async (e) => {
        const tgl = e.target.closest('[data-action="toggle-complete"]');
        if (!tgl) return;
        e.stopPropagation();
        const card = tgl.closest('.goal-card');
        const id = card?.dataset?.id;
        const isCompleted = card?.dataset?.status === 'completed';
        if (!id) return;
        try {
          await GoalsAPI.toggleComplete(id);
          const next = isCompleted ? 'active' : 'completed';
          if (card) card.dataset.status = next;
          const badge = qs('.status-badge', card);
          if (badge) {
            if (next === 'active') {
              badge.className = 'status-badge active bg-blue-100 text-blue-800 text-xs px-2.5 py-0.5 rounded-full';
              badge.innerHTML = '<i class="fas fa-spinner mr-1"></i>Active';
            } else {
              badge.className = 'status-badge completed bg-green-100 text-green-800 text-xs px-2.5 py-0.5 rounded-full';
              badge.innerHTML = '<i class="fas fa-check-circle mr-1"></i>Completed';
            }
          }
          showToast(`Goal marked as ${next}`, 'success');
        } catch (err) {
          console.error('Toggle complete error:', err);
          showToast('Error updating goal status', 'error');
        }
      });

      container.dataset.listenersBound = 'true';
    }

    function confirmDelete(goalId) {
      // Prefer global shared modal + handler flow
      if (typeof window.showDeleteGoalModal === 'function') {
        closeAllDropdowns();
        window.showDeleteGoalModal(goalId);
        return;
      }

      // Fallback: native confirm and direct deletion
      if (confirm('Delete this goal? This action cannot be undone.')) {
        (async () => {
          try {
            const res = await GoalsAPI.delete(goalId);
            if (res && res.success !== false) {
              showToast('Goal deleted successfully', 'success');
              document.dispatchEvent(new CustomEvent('goals-refreshed'));
              if (window.__goalsReload) window.__goalsReload();
            } else {
              showToast(res?.message || 'Failed to delete goal', 'error');
            }
          } catch (e) {
            console.error('Error deleting goal:', e);
            showToast(e?.message || 'Failed to delete goal', 'error');
          }
        })();
      }
    }

    function hydrate() {
      setMinDueDates();
      bindContainerEvents();
      bindGlobalHandlers();
      // Maintain compatibility with legacy listeners
      document.dispatchEvent(new CustomEvent('goals-refreshed'));
    }

    return {
      setMinDueDates,
      hydrate,
      openModal,
      closeModal,
      displayValidationErrors,
      clearValidationErrors,
      closeAllDropdowns,
      showToast
    };
  })();

  // =============== Alpine Component ===============
  function registerAlpine() {
    if (!window.Alpine || !window.Alpine.data) return false;
    window.Alpine.data('goalsPage', () => ({
      filter: 'all',
      loading: false,

      init() {
        // Filter buttons
        qsa('.filter-btn', this.$root).forEach(btn => {
          btn.addEventListener('click', (e) => {
            qsa('.filter-btn', this.$root).forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            const f = btn.dataset.filter || 'all';
            this.setFilter(f);
          });
        });

        // New goal button + empty state CTA
        qs('#new-goal-btn', this.$root)?.addEventListener('click', () => UI.openModal('goal-modal'));
        qs('#empty-state-cta', this.$root)?.addEventListener('click', () => UI.openModal('goal-modal'));

        // Create form submit
        const createForm = qs('#goal-form');
        if (createForm && !createForm.dataset.bound) {
          createForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const titleEl = qs('#goal-title');
            const descEl = qs('#goal-description');
            const dueEl = qs('#due-date');

            // Native validity checks
            let valid = true;
            if (!titleEl.checkValidity()) { qs('#title-error')?.classList.remove('hidden'); valid = false; } else { qs('#title-error')?.classList.add('hidden'); }
            if (descEl.value.trim() && !descEl.checkValidity()) { qs('#description-error')?.classList.remove('hidden'); valid = false; } else { qs('#description-error')?.classList.add('hidden'); }
            if (!dueEl.checkValidity()) { qs('#due-date-error')?.classList.remove('hidden'); valid = false; } else { qs('#due-date-error')?.classList.add('hidden'); }
            const formError = qs('#form-error');
            if (!valid) { formError?.classList.remove('hidden'); return; } else { formError?.classList.add('hidden'); }

            const submitBtn = createForm.querySelector('button[type="submit"]');
            const createText = qs('#create-text');
            const createLoading = qs('#create-loading');
            submitBtn && (submitBtn.disabled = true);
            createText?.classList.add('hidden');
            createLoading?.classList.remove('hidden');

            try {
              const model = {
                title: titleEl.value,
                description: descEl.value,
                dueDate: dueEl.value || toISODate(new Date()),
                completed: false
              };
              const result = await GoalsAPI.create(model);
              if (result.success) {
                UI.closeModal('goal-modal');
                createForm.reset();
                showToast(result.message || 'Goal created successfully!', 'success');
                this.reload();
              } else {
                showToast(result.message || 'Failed to create goal', 'error');
                if (result.errors) UI.displayValidationErrors(result.errors);
              }
            } catch (err) {
              console.error('Create error:', err);
              showToast('Failed to create goal. Please try again.', 'error');
            } finally {
              submitBtn && (submitBtn.disabled = false);
              createText?.classList.remove('hidden');
              createLoading?.classList.add('hidden');
            }
          });
          createForm.dataset.bound = 'true';
        }

        // Edit form submit
        const editForm = qs('#edit-goal-form');
        if (editForm && !editForm.dataset.bound) {
          editForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const id = qs('#edit-goal-id')?.value;
            const title = qs('#edit-goal-title')?.value;
            const description = qs('#edit-goal-description')?.value;
            const dueDate = qs('#edit-due-date')?.value;
            const completed = !!qs('#edit-goal-completed')?.checked;

            // Basic validation (reuse native validity)
            let valid = true;
            if (!qs('#edit-goal-title').checkValidity()) { qs('#edit-title-error')?.classList.remove('hidden'); valid = false; } else { qs('#edit-title-error')?.classList.add('hidden'); }
            const descEl = qs('#edit-goal-description');
            if (descEl.value.trim() && !descEl.checkValidity()) { qs('#edit-description-error')?.classList.remove('hidden'); valid = false; } else { qs('#edit-description-error')?.classList.add('hidden'); }
            if (!qs('#edit-due-date').checkValidity()) { qs('#edit-due-date-error')?.classList.remove('hidden'); valid = false; } else { qs('#edit-due-date-error')?.classList.add('hidden'); }
            const formError = qs('#edit-form-error');
            if (!valid) { formError?.classList.remove('hidden'); return; } else { formError?.classList.add('hidden'); }

            const submitBtn = editForm.querySelector('button[type="submit"]');
            const saveText = qs('#save-text');
            const saveLoading = qs('#save-loading');
            submitBtn && (submitBtn.disabled = true);
            saveText?.classList.add('hidden');
            saveLoading?.classList.remove('hidden');

            try {
              const result = await GoalsAPI.update({ id, title, description, dueDate, completed });
              if (result.success) {
                UI.closeModal('edit-goal-modal');
                showToast(result.message || 'Goal updated successfully!', 'success');
                this.reload();
              } else {
                showToast(result.message || 'Failed to update goal', 'error');
                if (result.errors) UI.displayValidationErrors(result.errors, 'edit-');
              }
            } catch (err) {
              console.error('Update error:', err);
              showToast('Failed to update goal. Please try again.', 'error');
            } finally {
              submitBtn && (submitBtn.disabled = false);
              saveText?.classList.remove('hidden');
              saveLoading?.classList.add('hidden');
            }
          });
          editForm.dataset.bound = 'true';
        }

        // Initial load
        this.reload();
      },

      async setFilter(filter) {
        this.filter = filter || 'all';
        await this.reload();
      },

      async reload() {
        try {
          this.loading = true;
          const container = document.getElementById('goals-container');
          const emptyState = document.getElementById('empty-state');
          const html = await GoalsAPI.getPartial(this.filter);
          container.innerHTML = html;
          const hasGoals = !!qs('.goal-card', container);
          emptyState?.classList.toggle('hidden', hasGoals);
          container.classList.toggle('hidden', !hasGoals);
          UI.hydrate();
        } catch (err) {
          console.error('Load goals error:', err);
          showToast('Failed to load goals. Please try again.', 'error');
        } finally {
          this.loading = false;
        }
      }
    }));

    return true;
  }

  // Register Alpine component
  document.addEventListener('alpine:init', registerAlpine);
  if (window.Alpine?.data) registerAlpine();

  // Legacy fallback for non-Alpine init (works without x-data)
  function legacyInit() {
    UI.setMinDueDates();
    // Attach a global reload used by confirmDelete
    window.__goalsReload = async () => {
      try {
        const container = document.getElementById('goals-container');
        const emptyState = document.getElementById('empty-state');
        const html = await GoalsAPI.getPartial('all');
        container.innerHTML = html;
        const hasGoals = !!qs('.goal-card', container);
        emptyState?.classList.toggle('hidden', hasGoals);
        container.classList.toggle('hidden', !hasGoals);
        UI.hydrate();
      } catch (e) {
        console.error(e);
      }
    };
    // Immediate load
    window.__goalsReload();

    // New Goal button
    qs('#new-goal-btn')?.addEventListener('click', () => UI.openModal('goal-modal'));
    qs('#empty-state-cta')?.addEventListener('click', () => UI.openModal('goal-modal'));

    // Filter buttons
    qsa('.filter-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        qsa('.filter-btn').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        // Use global reload with chosen filter (fallback keeps 'all')
        window.__goalsReload();
      });
    });
  }

  // Expose minimal global API for compatibility
  window.GoalsPage = {
    api: GoalsAPI,
    ui: UI,
    init: legacyInit,
    showToast
  };
})();
