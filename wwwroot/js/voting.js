const Voting = {
    userVotes: {},

    init(initialVotes = {}) {
        this.userVotes = initialVotes;
        this.applyInitialHighlights();
        this.attachListeners();
    },

    applyInitialHighlights() {
        for (const [key, value] of Object.entries(this.userVotes)) {
            const [type, id] = key.split('-');
            this.highlightButton(type, id, value);
        }
    },

    attachListeners() {
        document.querySelectorAll('.vote-btn').forEach(button => {
            button.addEventListener('click', (e) => this.handleVote(e, button));
        });
    },

    async handleVote(e, button) {
        e.preventDefault();

        // Check auth locally (though server also checks)
        // We rely on the button NOT being disabled, or we can check a global flag?
        // Details.cshtml checks User.Identity.IsAuthenticated.
        // If we want detailed auth check here, we need a flag.
        // For now, if the button exists and handles click, we assume auth or server returns 401.
        // But the original code had an alert for unauth users.
        // We'll trust the server response or check specific data attribute if added.

        // Actually, Details.cshtml renders logic to alert if not authenticated.
        // We can check if body has class "logged-in" or similar?
        // Or just try and catch 401.

        const type = button.dataset.type;
        const id = button.dataset.id;
        const clickedValue = parseInt(button.dataset.value);
        const voteKey = `${type}-${id}`;
        const currentVote = this.userVotes[voteKey] || 0;
        let newValue = (currentVote === clickedValue) ? 0 : clickedValue;

        // UI Optimistic Update or Loading State?
        // Let's use Loading State to be safe and rigorous (Request -> Success -> Update)
        // Disable buttons
        const buttons = document.querySelectorAll(`.vote-btn[data-type="${type}"][data-id="${id}"]`);
        buttons.forEach(b => b.classList.add('opacity-50', 'pointer-events-none'));

        try {
            const url = type === 'question'
                ? '/Questions/VoteQuestion'
                : '/Answers/VoteAnswer';

            const formData = new FormData();
            formData.append(type === 'question' ? 'questionId' : 'answerId', id);
            formData.append('value', newValue);

            const token = document.querySelector('input[name="__RequestVerificationToken"]');
            if (token) formData.append('__RequestVerificationToken', token.value);

            const response = await fetch(url, {
                method: 'POST',
                body: formData
            });

            if (response.status === 401) {
                Toast.error('Vui lòng đăng nhập để bình chọn!');
                return;
            }

            const result = await response.json();

            if (result.success) {
                // Update Score
                const scoreElement = document.querySelector(`.vote-score[data-type="${type}"][data-id="${id}"]`);
                if (scoreElement) scoreElement.textContent = result.score;

                // Update State
                if (newValue === 0) {
                    delete this.userVotes[voteKey];
                    this.removeHighlight(type, id);
                } else {
                    this.userVotes[voteKey] = newValue;
                    this.removeHighlight(type, id);
                    this.highlightButton(type, id, newValue);
                }

                // Optional: Toast success? "Đã vote!" - maybe too noisy.
                // Highlight is enough feedback usually.
            } else {
                Toast.error(result.message || 'Có lỗi xảy ra.');
            }

        } catch (error) {
            console.error(error);
            Toast.error('Không thể kết nối đến máy chủ.');
        } finally {
            // Enable buttons
            buttons.forEach(b => b.classList.remove('opacity-50', 'pointer-events-none'));
        }
    },

    highlightButton(type, id, value) {
        const upBtn = document.querySelector(`.vote-btn.vote-up[data-type="${type}"][data-id="${id}"]`);
        const downBtn = document.querySelector(`.vote-btn.vote-down[data-type="${type}"][data-id="${id}"]`);

        if (value === 1 && upBtn) {
            upBtn.classList.add('voted', 'bg-orange-100'); // Added bg color for clarity
            upBtn.querySelector('svg').classList.add('text-orange-500');
            upBtn.querySelector('svg').classList.remove('text-slate-300');
        } else if (value === -1 && downBtn) {
            downBtn.classList.add('voted', 'bg-blue-100');
            downBtn.querySelector('svg').classList.add('text-blue-500');
            downBtn.querySelector('svg').classList.remove('text-slate-300');
        }
    },

    removeHighlight(type, id) {
        const upBtn = document.querySelector(`.vote-btn.vote-up[data-type="${type}"][data-id="${id}"]`);
        const downBtn = document.querySelector(`.vote-btn.vote-down[data-type="${type}"][data-id="${id}"]`);

        if (upBtn) {
            upBtn.classList.remove('voted', 'bg-orange-100');
            upBtn.querySelector('svg').classList.remove('text-orange-500');
            upBtn.querySelector('svg').classList.add('text-slate-300');
        }
        if (downBtn) {
            downBtn.classList.remove('voted', 'bg-blue-100');
            downBtn.querySelector('svg').classList.remove('text-blue-500');
            downBtn.querySelector('svg').classList.add('text-slate-300');
        }
    },

    acceptAnswer: function (btn) {
        const answerId = btn.getAttribute('data-answer-id');
        if (!answerId) return;

        const isCurrentlyAccepted = btn.classList.contains('text-green-600');

        // Optimistic UI update (optional, but let's wait for server for simplicity or add loading spinner)
        // Let's create a visual loading effect?
        btn.classList.add('opacity-50', 'cursor-wait');

        fetch(`/Answers/Accept/${answerId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            }
        })
            .then(response => response.json())
            .then(data => {
                btn.classList.remove('opacity-50', 'cursor-wait');

                if (data.success) {
                    // Determine new state
                    const newState = !isCurrentlyAccepted;

                    if (newState) {
                        Toast.success('Đã chấp nhận câu trả lời!');

                        // Reset all other buttons
                        document.querySelectorAll('.accept-btn').forEach(b => {
                            b.classList.remove('text-green-600', 'bg-green-50');
                            b.classList.add('text-slate-200');
                            b.setAttribute('title', 'Chấp nhận câu trả lời này');
                            // Remove green border from container if exists (logic from Details.cshtml)
                            const container = document.getElementById(`answer-${b.getAttribute('data-answer-id')}`);
                            if (container) {
                                container.classList.remove('bg-green-50/40', '-mx-4', 'px-4', 'rounded-lg', 'border', 'border-green-100', 'pt-4');
                            }
                        });

                        // Set active
                        btn.classList.remove('text-slate-200');
                        btn.classList.add('text-green-600', 'bg-green-50');
                        btn.setAttribute('title', 'Bỏ chấp nhận');

                        const container = document.getElementById(`answer-${answerId}`);
                        if (container) {
                            container.classList.add('bg-green-50/40', '-mx-4', 'px-4', 'rounded-lg', 'border', 'border-green-100', 'pt-4');
                        }

                    } else {
                        Toast.success('Đã bỏ chấp nhận câu trả lời.');
                        btn.classList.remove('text-green-600', 'bg-green-50');
                        btn.classList.add('text-slate-200');
                        btn.setAttribute('title', 'Chấp nhận câu trả lời này');

                        const container = document.getElementById(`answer-${answerId}`);
                        if (container) {
                            container.classList.remove('bg-green-50/40', '-mx-4', 'px-4', 'rounded-lg', 'border', 'border-green-100', 'pt-4');
                        }
                    }
                } else {
                    Toast.error(data.message || 'Có lỗi xảy ra.');
                }
            })
            .catch(error => {
                btn.classList.remove('opacity-50', 'cursor-wait');
                Toast.error('Lỗi kết nối.');
                console.error('Error:', error);
            });
    }
};