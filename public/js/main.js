/* ============================================
   BusGo — Shared Application JavaScript
   ============================================ */

document.addEventListener('DOMContentLoaded', function () {
  // Initialize Lucide icons
  if (typeof lucide !== 'undefined') {
    lucide.createIcons();
  }

  // Initialize Bootstrap tooltips
  const tooltipTriggerList = document.querySelectorAll('[data-bs-toggle="tooltip"]');
  tooltipTriggerList.forEach(function (el) {
    new bootstrap.Tooltip(el);
  });

  // Navbar active link
  const currentPage = window.location.pathname.split('/').pop();
  document.querySelectorAll('.navbar-busgo .nav-link').forEach(function (link) {
    const href = link.getAttribute('href');
    if (href === currentPage) {
      link.classList.add('active');
    }
  });
});

/* ── Utility Functions ── */
function formatCurrency(amount) {
  return '$' + parseFloat(amount).toFixed(2);
}

function formatDate(dateStr) {
  const d = new Date(dateStr);
  return d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric', year: 'numeric' });
}

function formatTime(time) {
  const [h, m] = time.split(':');
  const hour = parseInt(h);
  const ampm = hour >= 12 ? 'PM' : 'AM';
  const displayHour = hour % 12 || 12;
  return displayHour + ':' + m + ' ' + ampm;
}

function showToast(message, type) {
  type = type || 'success';
  const toastContainer = document.getElementById('toastContainer');
  if (!toastContainer) return;

  const icons = {
    success: 'check-circle',
    danger: 'alert-circle',
    warning: 'alert-triangle',
    info: 'info'
  };
  const colors = {
    success: 'var(--success)',
    danger: 'var(--danger)',
    warning: 'var(--warning)',
    info: 'var(--info)'
  };

  const toast = document.createElement('div');
  toast.className = 'toast show fade-in';
  toast.setAttribute('role', 'alert');
  toast.innerHTML =
    '<div class="toast-body d-flex align-items-center gap-2" style="font-weight:500;">' +
      '<i data-lucide="' + icons[type] + '" style="width:18px;height:18px;color:' + colors[type] + '"></i>' +
      '<span>' + message + '</span>' +
      '<button type="button" class="btn-close ms-auto" data-bs-dismiss="toast" style="font-size:0.65rem;"></button>' +
    '</div>';
  toastContainer.appendChild(toast);
  if (typeof lucide !== 'undefined') { lucide.createIcons(); }
  setTimeout(function () {
    toast.classList.remove('show');
    setTimeout(function () { toast.remove(); }, 300);
  }, 4000);
}

/* ── Loading Skeleton Generator ── */
function generateSkeletonCards(container, count) {
  count = count || 3;
  var html = '';
  for (var i = 0; i < count; i++) {
    html +=
      '<div class="skeleton-card mb-3 fade-in" style="animation-delay:' + (i * 0.1) + 's">' +
        '<div class="d-flex gap-3 mb-3">' +
          '<div class="skeleton skeleton-avatar"></div>' +
          '<div class="flex-grow-1">' +
            '<div class="skeleton skeleton-title"></div>' +
            '<div class="skeleton skeleton-text w-75"></div>' +
          '</div>' +
        '</div>' +
        '<div class="skeleton skeleton-box"></div>' +
      '</div>';
  }
  container.innerHTML = html;
}

/* ── Admin Sidebar Toggle ── */
function toggleAdminSidebar() {
  const sidebar = document.getElementById('adminSidebar');
  const overlay = document.getElementById('adminOverlay');
  if (sidebar) {
    sidebar.classList.toggle('open');
  }
  if (overlay) {
    overlay.classList.toggle('show');
  }
}
