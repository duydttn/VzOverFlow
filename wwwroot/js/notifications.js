document.addEventListener('DOMContentLoaded', () => {
    // --- NOTIFICATION LOGIC ---
    const notifBtn = document.getElementById('notif-btn');
    const notifDropdown = document.getElementById('notif-dropdown');
    const notifList = document.getElementById('notif-list');
    const notifBadge = document.getElementById('notif-badge');

    if (notifBtn) {
        notifBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            notifDropdown.classList.toggle('hidden');

            if (!notifDropdown.classList.contains('hidden')) {
                loadNotifications();
            }
        });

        document.addEventListener('click', function (e) {
            if (!notifDropdown.contains(e.target) && !notifBtn.contains(e.target)) {
                notifDropdown.classList.add('hidden');
            }
        });

        function loadNotifications() {
            fetch('/Notifications/GetLatest')
                .then(response => response.json())
                .then(data => {
                    if (data.length === 0) {
                        notifList.innerHTML = '<div class="p-8 text-center text-xs text-slate-500 flex flex-col items-center gap-2"><svg class="w-8 h-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" /></svg>Không có thông báo mới</div>';
                    } else {
                        notifList.innerHTML = data.map(n => `
                            <a href="${n.link}" class="block p-4 border-b border-slate-100 transition-colors ${!n.isRead ? 'bg-blue-50 border-l-4 border-l-blue-500' : 'bg-white text-slate-600 hover:bg-slate-50'}">
                                <div class="text-sm ${!n.isRead ? 'font-semibold text-slate-900' : ''} line-clamp-2 mb-1">${escapeHtml(n.message)}</div>
                                <div class="text-[10px] text-slate-400">${n.time}</div>
                            </a>
                        `).join('');
                    }
                })
                .catch(err => {
                    console.error(err);
                    notifList.innerHTML = '<div class="p-4 text-center text-xs text-red-500">Lỗi tải thông báo</div>';
                });
        }

        function checkUnread() {
            fetch('/Notifications/CheckUnread')
                .then(res => res.json())
                .then(data => {
                    if (data.count > 0) {
                        notifBadge.innerText = data.count > 99 ? '99+' : data.count;
                        notifBadge.classList.remove('hidden');
                    } else {
                        notifBadge.classList.add('hidden');
                    }
                });
        }

        // Initial check
        checkUnread();
        // Fallback polling (every 1 minutes)
        setInterval(checkUnread, 60000);

        // --- SIGNALR INTEGRATION ---
        if (typeof signalR !== 'undefined') {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/notificationHub")
                .withAutomaticReconnect()
                .build();

            connection.on("ReceiveNotification", function (data) {
                // 1. Show Toast
                if (typeof Toast !== 'undefined') {
                    Toast.info(data.message);
                }

                // 2. Update Badge
                let currentCount = parseInt(notifBadge.innerText);
                if (isNaN(currentCount)) currentCount = 0;

                // If badge is hidden (count 0), show it
                if (notifBadge.classList.contains('hidden')) {
                    currentCount = 0;
                    notifBadge.classList.remove('hidden');
                }

                const newCount = currentCount + 1;
                notifBadge.innerText = newCount > 99 ? '99+' : newCount;

                // 3. Update Dropdown if open
                if (!notifDropdown.classList.contains('hidden')) {
                    const emptyState = notifList.querySelector('.text-center');
                    if (emptyState) emptyState.remove();

                    const html = `
                        <a href="${data.link}" class="block p-4 border-b border-slate-100 transition-colors bg-blue-50 border-l-4 border-l-blue-500 animate-fade-in-down">
                            <div class="text-sm font-semibold text-slate-900 line-clamp-2 mb-1">${escapeHtml(data.message)}</div>
                            <div class="text-[10px] text-slate-400">Vừa xong</div>
                        </a>
                    `;
                    notifList.insertAdjacentHTML('afterbegin', html);
                }
            });

            connection.start()
                .then(() => console.log("SignalR Connected"))
                .catch(err => console.error("SignalR Connection Error: ", err));
        }
    }
});

function markAllRead() {
    fetch('/Notifications/MarkAsRead', {
        method: 'POST',
        headers: {
            'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
        }
    })
        .then(() => {
            document.getElementById('notif-badge').classList.add('hidden');
            // Reload list to update visual state (remove blue background)
            const notifList = document.getElementById('notif-list');
            const notifDropdown = document.getElementById('notif-dropdown');
            if (notifList && notifDropdown && !notifDropdown.classList.contains('hidden')) {
                // We can't reach loadNotifications here easily without refactoring scopes.
                // But usually user will close and reopen, or navigate.
                // Just hiding badge is enough for now.
            }
        });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}