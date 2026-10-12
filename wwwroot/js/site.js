// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// AI Summary - Toggle Details
document.addEventListener('DOMContentLoaded', function () {
  const toggleBtn = document.getElementById('toggleDetailsBtn');
  const detailsSection = document.getElementById('detailsSection');

  if (toggleBtn && detailsSection) {
    toggleBtn.addEventListener('click', function () {
      if (detailsSection.classList.contains('hidden')) {
        detailsSection.classList.remove('hidden');
        toggleBtn.textContent = 'Ẩn chi tiết';
      } else {
        detailsSection.classList.add('hidden');
        toggleBtn.textContent = 'Xem chi tiết từng bước';
      }
    });
  }

  // AI Summary - Feedback Buttons
  const helpfulBtn = document.getElementById('feedbackHelpful');
  const notRelevantBtn = document.getElementById('feedbackNotRelevant');
  const feedbackMessage = document.getElementById('feedbackMessage');

  if (helpfulBtn && feedbackMessage) {
    helpfulBtn.addEventListener('click', function () {
      // Highlight the selected button
      helpfulBtn.classList.add('bg-green-600');
      helpfulBtn.classList.remove('bg-slate-800');
      notRelevantBtn.classList.remove('bg-red-600');
      notRelevantBtn.classList.add('bg-slate-800');

      // Show feedback message
      feedbackMessage.textContent = 'Cảm ơn phản hồi của bạn!';
      feedbackMessage.classList.remove('hidden', 'text-red-300');
      feedbackMessage.classList.add('text-green-300');

      // Optional: Send to server
      fetch('/api/feedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ helpful: true })
      });
    });
  }

  if (notRelevantBtn && feedbackMessage) {
    notRelevantBtn.addEventListener('click', function () {
      // Highlight the selected button
      notRelevantBtn.classList.add('bg-red-600');
      notRelevantBtn.classList.remove('bg-slate-800');
      helpfulBtn.classList.remove('bg-green-600');
      helpfulBtn.classList.add('bg-slate-800');

      // Show feedback message
      feedbackMessage.textContent = 'Đã ghi nhận phản hồi';
      feedbackMessage.classList.remove('hidden', 'text-green-300');
      feedbackMessage.classList.add('text-red-300');

      // Optional: Send to server
      fetch('/api/feedback', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ helpful: false })
      });
    });
  }
});

document.addEventListener('DOMContentLoaded', () => {
  // Syntax Highlight
  if (typeof hljs !== 'undefined') {
    document.querySelectorAll('pre code').forEach((el) => {
      hljs.highlightElement(el);
    });
  }

  // Mobile Search
  const mobileSearchToggle = document.getElementById('mobile-search-toggle');
  const mobileSearchBar = document.getElementById('mobile-search-bar');
  if (mobileSearchToggle && mobileSearchBar) {
    mobileSearchToggle.addEventListener('click', () => {
      mobileSearchBar.classList.toggle('hidden');
      if (!mobileSearchBar.classList.contains('hidden')) {
        const input = mobileSearchBar.querySelector('input');
        if (input) input.focus();
      }
    });
  }

  // Mobile Menu
  const mobileMenuToggle = document.getElementById('mobile-menu-toggle');
  const mobileMenu = document.getElementById('mobile-menu');
  if (mobileMenuToggle && mobileMenu) {
    mobileMenuToggle.addEventListener('click', (e) => {
      e.stopPropagation();
      mobileMenu.classList.toggle('hidden');
    });
    document.addEventListener('click', (e) => {
      if (!mobileMenu.contains(e.target) && !mobileMenuToggle.contains(e.target)) {
        mobileMenu.classList.add('hidden');
      }
    });
  }

  // Share Logic
  document.querySelectorAll('.share-btn').forEach(button => {
    button.addEventListener('click', async function (e) {
      e.preventDefault();
      const url = this.dataset.url;

      if (navigator.share) {
        try {
          await navigator.share({
            title: document.title,
            url: url
          });
        } catch (error) {
          if (error.name !== 'AbortError') {
            copyToClipboard(url, this);
          }
        }
      } else {
        copyToClipboard(url, this);
      }
    });
  });

  function copyToClipboard(text, button) {
    navigator.clipboard.writeText(text).then(() => {
      const originalHTML = button.innerHTML;
      button.innerHTML = '<span class="text-green-600 flex items-center gap-1"><svg xmlns="http://www.w3.org/2000/svg" class="h-3 w-3" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" /></svg> Đã copy</span>';
      setTimeout(() => {
        button.innerHTML = originalHTML;
      }, 2000);
    }).catch(err => {
      console.error('Không thể sao chép:', err);
      // Fallback if Toast exists
      if (typeof Toast !== 'undefined') Toast.error('Lỗi sao chép');
      else alert('Lỗi sao chép.');
    });
  }
});

// --- DARK MODE LOGIC ---
document.addEventListener('DOMContentLoaded', () => {
  const themeToggleBtn = document.getElementById('theme-toggle');
  const htmlElement = document.documentElement;
  const darkIcon = document.getElementById('theme-toggle-dark-icon');
  const lightIcon = document.getElementById('theme-toggle-light-icon');

  // Sync icons with current state
  if (htmlElement.classList.contains('dark')) {
    darkIcon?.classList.add('hidden');
    lightIcon?.classList.remove('hidden');
  } else {
    lightIcon?.classList.add('hidden');
    darkIcon?.classList.remove('hidden');
  }

  if (themeToggleBtn) {
    themeToggleBtn.addEventListener('click', function () {
      // Toggle icons
      darkIcon.classList.toggle('hidden');
      lightIcon.classList.toggle('hidden');

      // Toggle class
      if (htmlElement.classList.contains('dark')) {
        htmlElement.classList.remove('dark');
        localStorage.setItem('color-theme', 'light');
      } else {
        htmlElement.classList.add('dark');
        localStorage.setItem('color-theme', 'dark');
      }
    });
  }
});