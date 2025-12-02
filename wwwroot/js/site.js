// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// AI Summary - Toggle Details
document.addEventListener('DOMContentLoaded', function() {
    const toggleBtn = document.getElementById('toggleDetailsBtn');
    const detailsSection = document.getElementById('detailsSection');
    
    if (toggleBtn && detailsSection) {
  toggleBtn.addEventListener('click', function() {
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
   helpfulBtn.addEventListener('click', function() {
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
   // fetch('/api/feedback', { method: 'POST', body: JSON.stringify({ helpful: true }) });
        });
    }
    
    if (notRelevantBtn && feedbackMessage) {
        notRelevantBtn.addEventListener('click', function() {
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
     // fetch('/api/feedback', { method: 'POST', body: JSON.stringify({ helpful: false }) });
        });
}
});
