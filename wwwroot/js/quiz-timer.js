document.addEventListener('DOMContentLoaded', function () {
    // 1. Timer Logic
    const timerDisplay = document.getElementById('countdown-display');
    const timerContainer = document.getElementById('timer-container');
    const timerIcon = document.getElementById('timer-icon');
    const quizForm = document.getElementById('quiz-form');
    
    if (window.quizConfig && timerDisplay) {
        const startedAt = new Date(window.quizConfig.startedAt);
        // Sumar minutos del límite al tiempo de inicio para obtener la hora límite
        const endTime = new Date(startedAt.getTime() + window.quizConfig.timeLimitMinutes * 60000);
        
        let isSubmitted = false;

        const timerInterval = setInterval(updateTimer, 1000);
        updateTimer(); // Initial call

        function updateTimer() {
            if (isSubmitted) return;

            const now = new Date();
            let timeLeftMs = endTime - now;

            if (timeLeftMs <= 0) {
                clearInterval(timerInterval);
                timerDisplay.textContent = "00:00";
                forceSubmit();
                return;
            }

            // Calculo
            const totalSecondsLeft = Math.floor(timeLeftMs / 1000);
            const minutes = Math.floor(totalSecondsLeft / 60);
            const seconds = totalSecondsLeft % 60;

            // Formato MM:SS
            timerDisplay.textContent = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;

            // Alerta visual últimos 60 segundos
            if (totalSecondsLeft <= 60 && !timerContainer.classList.contains('border-red-500/50')) {
                timerContainer.classList.remove('border-slate-800', 'bg-slate-900/95');
                timerContainer.classList.add('border-red-500/50', 'bg-red-950/90');
                
                timerDisplay.classList.remove('text-white');
                timerDisplay.classList.add('text-red-400');
                
                timerIcon.classList.remove('text-indigo-400');
                timerIcon.classList.add('text-red-400', 'animate-pulse');
            }
        }

        function forceSubmit() {
            if (isSubmitted) return;
            isSubmitted = true;
            
            // Mostrar estado bloqueado
            timerDisplay.textContent = "¡Tiempo Agotado!";
            timerDisplay.classList.add('text-sm');
            
            // Disable all inputs
            document.querySelectorAll('.question-input').forEach(el => el.disabled = true);
            
            // Submit form
            quizForm.submit();
        }
    }

    // 2. Question Map Logic
    const questionInputs = document.querySelectorAll('.question-input');
    const navLinks = document.querySelectorAll('.question-nav');

    // Update map on input change
    questionInputs.forEach(input => {
        input.addEventListener('change', function() {
            const qid = this.getAttribute('data-qid');
            updateMapLinkStatus(qid);
        });
    });

    function updateMapLinkStatus(qid) {
        // Find if this question has any checked input
        const blockInputs = document.querySelectorAll(`input[data-qid="${qid}"]`);
        const isAnswered = Array.from(blockInputs).some(input => input.checked);
        
        const navLink = document.querySelector(`.question-nav[data-target-qid="${qid}"]`);
        if (navLink) {
            if (isAnswered) {
                navLink.classList.remove('border-slate-700', 'bg-slate-800/50', 'text-slate-400');
                navLink.classList.add('border-indigo-500', 'bg-indigo-500/20', 'text-indigo-300');
            } else {
                navLink.classList.remove('border-indigo-500', 'bg-indigo-500/20', 'text-indigo-300');
                navLink.classList.add('border-slate-700', 'bg-slate-800/50', 'text-slate-400');
            }
        }
    }

    // Active state tracking via IntersectionObserver
    const blocks = document.querySelectorAll('.question-block');
    if ('IntersectionObserver' in window) {
        const observerOptions = {
            root: null,
            rootMargin: '-50% 0px -50% 0px',
            threshold: 0
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const id = entry.target.id.replace('q-', '');
                    
                    // Remove active from all nav links
                    navLinks.forEach(link => link.classList.remove('ring-2', 'ring-white', 'ring-offset-2', 'ring-offset-slate-900'));
                    
                    // Add to current
                    const activeLink = document.querySelector(`.question-nav[data-target-qid="${id}"]`);
                    if (activeLink) {
                        activeLink.classList.add('ring-2', 'ring-white', 'ring-offset-2', 'ring-offset-slate-900');
                    }
                }
            });
        }, observerOptions);

        blocks.forEach(block => observer.observe(block));
    }

    // Smooth scrolling for nav links
    navLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            e.preventDefault();
            const targetId = `q-${this.getAttribute('data-target-qid')}`;
            const targetEl = document.getElementById(targetId);
            if (targetEl) {
                // Offset for the sticky header
                const headerOffset = 150;
                const elementPosition = targetEl.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - headerOffset;
  
                window.scrollTo({
                     top: offsetPosition,
                     behavior: "smooth"
                });
            }
        });
    });

    // 3. Modal Confirmation Logic
    const btnSubmitInit = document.getElementById('btn-submit-quiz');
    const modal = document.getElementById('confirm-modal');
    const panel = document.getElementById('confirm-modal-panel');
    const btnCancel = document.getElementById('btn-cancel-submit');
    const btnConfirm = document.getElementById('btn-confirm-submit');
    const realSubmit = document.getElementById('real-submit-btn');
    const confirmMessage = document.getElementById('confirm-message');

    if (btnSubmitInit && modal) {
        btnSubmitInit.addEventListener('click', () => {
            // Check unanswered
            let answeredCount = 0;
            const totalQ = blocks.length;
            
            blocks.forEach(block => {
                if (block.querySelectorAll('.question-input:checked').length > 0) {
                    answeredCount++;
                }
            });

            const unanswered = totalQ - answeredCount;
            
            if (unanswered > 0) {
                confirmMessage.innerHTML = `Tienes <strong class="text-red-400">${unanswered} preguntas sin responder</strong>. ¿Estás seguro de que deseas enviar el examen ahora? No podrás modificar tus respuestas.`;
            } else {
                confirmMessage.innerHTML = `Has respondido todas las preguntas. ¿Estás listo para enviar y terminar el examen?`;
            }

            // Show modal
            modal.classList.remove('hidden');
            // Trigger reflow
            void modal.offsetWidth;
            modal.classList.remove('opacity-0');
            panel.classList.remove('scale-95', 'opacity-0');
            panel.classList.add('scale-100', 'opacity-100');
        });

        const hideModal = () => {
            modal.classList.add('opacity-0');
            panel.classList.remove('scale-100', 'opacity-100');
            panel.classList.add('scale-95', 'opacity-0');
            setTimeout(() => {
                modal.classList.add('hidden');
            }, 300);
        };

        btnCancel.addEventListener('click', hideModal);
        
        btnConfirm.addEventListener('click', () => {
            hideModal();
            // Prevent double submission via UI
            btnConfirm.disabled = true;
            btnConfirm.innerHTML = 'Enviando...';
            realSubmit.click();
        });
    }
});
