document.addEventListener('DOMContentLoaded', function () {
    const container = document.getElementById('questionsContainer');
    const btnAddQuestion = document.getElementById('btnAddQuestion');
    const emptyState = document.getElementById('emptyQuestionsState');
    const form = document.getElementById('quizForm');
    const quizModeSelect = document.getElementById('quizSelectionMode');

    let questionIndex = 0;

    // Leer modo del quiz (1 = Unica, 2 = Multiple)
    function getQuizMode() {
        return quizModeSelect ? quizModeSelect.value : '1';
    }

    function isSingleMode() {
        return getQuizMode() === '1';
    }

    // Inicializar si hay preguntas existentes
    if (window.existingQuestions && window.existingQuestions.length > 0) {
        window.existingQuestions.forEach(q => addQuestionBlock(q));
        toggleEmptyState();
    }

    btnAddQuestion.addEventListener('click', () => {
        addQuestionBlock();
        toggleEmptyState();
    });

    // Cambio de modo del quiz: actualizar todas las preguntas existentes
    if (quizModeSelect) {
        quizModeSelect.addEventListener('change', () => {
            const blocks = container.querySelectorAll('.question-block');
            blocks.forEach(block => {
                const typeSelect = block.querySelector('.question-type-select');
                if (isSingleMode()) {
                    // Forzar a Unica
                    typeSelect.value = 'Unica';
                    typeSelect.disabled = true;
                    typeSelect.classList.add('opacity-50', 'cursor-not-allowed');
                    // Convertir inputs a radio y limpiar exceso de seleccionados
                    updateQuestionInputs(block, 'Unica');
                } else {
                    typeSelect.disabled = false;
                    typeSelect.classList.remove('opacity-50', 'cursor-not-allowed');
                }
            });
        });
    }

    container.addEventListener('click', function (e) {
        if (e.target.closest('.btn-remove-question')) {
            const block = e.target.closest('.question-block');
            block.remove();
            toggleEmptyState();
            reindexQuestions();
        }

        if (e.target.closest('.btn-add-option')) {
            const block = e.target.closest('.question-block');
            const optionsContainer = block.querySelector('.options-container');
            const qIndex = block.dataset.index;
            const qType = block.querySelector('.question-type-select').value;
            addOptionRow(optionsContainer, qIndex, qType);
        }

        if (e.target.closest('.btn-remove-option')) {
            const row = e.target.closest('.option-row');
            const optionsContainer = e.target.closest('.options-container');
            if (optionsContainer.querySelectorAll('.option-row').length > 2) {
                row.remove();
                reindexOptions(optionsContainer, e.target.closest('.question-block').dataset.index);
            } else {
                alert('Una pregunta debe tener al menos 2 opciones.');
            }
        }
    });

    container.addEventListener('change', function (e) {
        if (e.target.classList.contains('question-type-select')) {
            const block = e.target.closest('.question-block');
            // Si el quiz está en modo único, ignorar cambios a Multiple
            if (isSingleMode() && e.target.value === 'Multiple') {
                e.target.value = 'Unica';
                alert('Este quiz está configurado solo para selección única.');
                return;
            }
            updateQuestionInputs(block, e.target.value);
        }
    });

    form.addEventListener('submit', function (e) {
        const blocks = container.querySelectorAll('.question-block');
        if (blocks.length === 0) {
            e.preventDefault();
            alert('Debes agregar al menos una pregunta al quiz.');
            return;
        }

        let isValid = true;
        blocks.forEach((block, idx) => {
            const text = block.querySelector('.question-text-input').value.trim();
            if (!text) {
                isValid = false;
                alert(`La pregunta ${idx + 1} no tiene texto.`);
            }

            const correctChecked = block.querySelectorAll('.is-correct-input:checked');
            if (correctChecked.length === 0) {
                isValid = false;
                alert(`La pregunta ${idx + 1} debe tener al menos una opción marcada como correcta.`);
            }
        });

        if (!isValid) e.preventDefault();
        reindexQuestions();
    });

    function toggleEmptyState() {
        if (container.querySelectorAll('.question-block').length === 0) {
            emptyState.style.display = 'flex';
        } else {
            emptyState.style.display = 'none';
        }
    }

    function addQuestionBlock(data = null) {
        const idx = questionIndex++;
        const singleMode = isSingleMode();
        const forcedType = singleMode ? 'Unica' : (data ? data.QuestionType : 'Unica');
        const isDisabled = singleMode ? 'disabled' : '';
        const disabledClass = singleMode ? 'opacity-50 cursor-not-allowed' : '';

        const html = `
            <div class="question-block border border-slate-700 bg-slate-800/40 rounded-xl p-5 mb-5 relative transition-all" data-index="${idx}">
                <div class="flex justify-between items-start gap-4 mb-4">
                    <div class="flex-grow grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div class="md:col-span-3">
                            <label class="block text-xs font-medium text-slate-400 mb-1 uppercase tracking-wider">Enunciado de la Pregunta</label>
                            <input type="text" name="Questions[${idx}].QuestionText" value="${data ? escapeHtml(data.QuestionText) : ''}" required
                                class="question-text-input w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500" 
                                placeholder="Escribe aquí la pregunta..." />
                        </div>
                        <div>
                            <label class="block text-xs font-medium text-slate-400 mb-1 uppercase tracking-wider">Tipo</label>
                            <select name="Questions[${idx}].QuestionType" class="question-type-select w-full rounded-lg border border-slate-700 bg-slate-900 px-3 py-2 text-sm text-white focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 ${disabledClass}" ${isDisabled}>
                                <option value="Unica" ${forcedType === 'Unica' ? 'selected' : ''}>Selección Única</option>
                                <option value="Multiple" ${forcedType === 'Multiple' ? 'selected' : ''}>Selección Múltiple</option>
                            </select>
                        </div>
                    </div>
                    <button type="button" class="btn-remove-question mt-6 text-slate-500 hover:text-red-400 p-1 rounded-md hover:bg-slate-800 transition-colors" title="Eliminar pregunta">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                        </svg>
                    </button>
                </div>

                <div class="pl-0 md:pl-4 border-l-2 border-slate-700/50">
                    <label class="block text-xs font-medium text-slate-400 mb-2 uppercase tracking-wider">Opciones de Respuesta (Marca la correcta)</label>
                    <div class="options-container space-y-2"></div>
                    <button type="button" class="btn-add-option mt-3 inline-flex items-center gap-1 text-xs font-medium text-indigo-400 hover:text-indigo-300 bg-indigo-500/10 px-2.5 py-1.5 rounded-md hover:bg-indigo-500/20 transition-colors">
                        <svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
                        Añadir Opción
                    </button>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', html);
        const block = container.lastElementChild;
        const optionsContainer = block.querySelector('.options-container');

        if (data && data.Options && data.Options.length > 0) {
            data.Options.forEach(opt => {
                addOptionRow(optionsContainer, idx, forcedType, opt);
            });
        } else {
            addOptionRow(optionsContainer, idx, forcedType);
            addOptionRow(optionsContainer, idx, forcedType);
        }
    }

    function addOptionRow(container, qIndex, qType, optData = null) {
        const oIndex = container.querySelectorAll('.option-row').length;
        const inputType = qType === 'Multiple' ? 'checkbox' : 'radio';
        const isChecked = optData && optData.IsCorrect ? 'checked' : '';
        const radioName = inputType === 'radio' ? `radio_${qIndex}` : `dummy_${qIndex}_${oIndex}`;

        const html = `
            <div class="option-row flex items-center gap-3 bg-slate-900/50 p-2 rounded-lg border border-slate-800">
                <div class="flex items-center justify-center pt-1">
                    <input type="${inputType}" name="${radioName}" class="is-correct-input h-4 w-4 border-slate-600 bg-slate-900 text-emerald-500 focus:ring-emerald-500" ${isChecked} onchange="syncHiddenBool(this)" />
                    <input type="hidden" name="Questions[${qIndex}].Options[${oIndex}].IsCorrect" value="${optData ? optData.IsCorrect : 'false'}" class="hidden-bool" />
                </div>
                <input type="text" name="Questions[${qIndex}].Options[${oIndex}].OptionText" value="${optData ? escapeHtml(optData.OptionText) : ''}" required
                    class="option-text-input flex-grow rounded-md border border-slate-700 bg-slate-800 px-3 py-1.5 text-sm text-white focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500" 
                    placeholder="Escribe la opción..." />
                <button type="button" class="btn-remove-option text-slate-500 hover:text-red-400 p-1 transition-colors" title="Eliminar opción">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" /></svg>
                </button>
            </div>
        `;
        container.insertAdjacentHTML('beforeend', html);
    }

    function updateQuestionInputs(block, newType) {
        const optionsContainer = block.querySelector('.options-container');
        const rows = optionsContainer.querySelectorAll('.option-row');
        const qIndex = block.dataset.index;
        const inputType = newType === 'Multiple' ? 'checkbox' : 'radio';

        rows.forEach((row, oIdx) => {
            const radioCheckbox = row.querySelector('.is-correct-input');
            const hiddenBool = row.querySelector('.hidden-bool');

            // Cambiar tipo de input
            const newInput = document.createElement('input');
            newInput.type = inputType;
            newInput.className = radioCheckbox.className;
            newInput.name = inputType === 'radio' ? `radio_${qIndex}` : `dummy_${qIndex}_${oIdx}`;
            newInput.checked = radioCheckbox.checked;
            newInput.setAttribute('onchange', 'syncHiddenBool(this)');

            radioCheckbox.parentNode.insertBefore(newInput, radioCheckbox);
            radioCheckbox.remove();

            // Si cambia a radio, dejar solo el primero marcado
            if (inputType === 'radio' && oIdx > 0) {
                newInput.checked = false;
                hiddenBool.value = 'false';
            }
        });
    }

    function reindexQuestions() {
        const blocks = container.querySelectorAll('.question-block');
        blocks.forEach((block, qIdx) => {
            block.dataset.index = qIdx;
            block.querySelector('.question-text-input').name = `Questions[${qIdx}].QuestionText`;
            block.querySelector('.question-type-select').name = `Questions[${qIdx}].QuestionType`;
            reindexOptions(block.querySelector('.options-container'), qIdx);
        });
    }

    function reindexOptions(optionsContainer, qIdx) {
        const rows = optionsContainer.querySelectorAll('.option-row');
        const qType = optionsContainer.closest('.question-block').querySelector('.question-type-select').value;
        const inputType = qType === 'Multiple' ? 'checkbox' : 'radio';

        rows.forEach((row, oIdx) => {
            const radioCheckbox = row.querySelector('.is-correct-input');
            const hiddenBool = row.querySelector('.hidden-bool');
            const textInput = row.querySelector('.option-text-input');

            if (inputType === 'radio') {
                radioCheckbox.name = `radio_${qIdx}`;
            } else {
                radioCheckbox.name = `dummy_${qIdx}_${oIdx}`;
            }

            hiddenBool.name = `Questions[${qIdx}].Options[${oIdx}].IsCorrect`;
            textInput.name = `Questions[${qIdx}].Options[${oIdx}].OptionText`;
        });
    }

    window.syncHiddenBool = function (element) {
        const row = element.closest('.option-row');
        row.querySelector('.hidden-bool').value = element.checked ? 'true' : 'false';

        if (element.type === 'radio') {
            const container = element.closest('.options-container');
            const allRadios = container.querySelectorAll('.is-correct-input');
            const allHidden = container.querySelectorAll('.hidden-bool');

            allRadios.forEach((rad, idx) => {
                allHidden[idx].value = rad.checked ? 'true' : 'false';
            });
        }
    };

    function escapeHtml(unsafe) {
        if (!unsafe) return '';
        return unsafe
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }
});