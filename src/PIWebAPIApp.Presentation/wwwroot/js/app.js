class PITagManager {
    constructor() {
        this.apiUrl = '/api/tags';
        this.currentTags = [];
        this.maxTags = 10;
        this.currentUser = null;
        this.emailErrorDiv = null; // Для хранения элемента ошибки email

        // DOM Elements
        this.userDisplay = document.getElementById('currentUser');
        this.emailInput = document.getElementById('email');
        this.justificationInput = document.getElementById('justification');
        this.tagsContainer = document.getElementById('tagsContainer');
        this.addTagBtn = document.getElementById('addTagBtn');
        this.saveBtn = document.getElementById('saveBtn');
        this.exportBtn = document.getElementById('exportBtn');
        this.loading = document.getElementById('loading');
        this.statusMessage = document.getElementById('statusMessage');

        this.init();
    }

    async init() {
        // Получаем текущего пользователя
        await this.getCurrentUser();

        // Добавляем первое поле тега
        this.addTagField();

        // Event Listeners
        this.addTagBtn.addEventListener('click', () => this.addTagField());
        this.saveBtn.addEventListener('click', () => this.saveAllTags());
        this.exportBtn.addEventListener('click', () => this.exportToPdf());

        // Добавляем обработчик ввода для поля email для очистки ошибки
        if (this.emailInput) {
            this.emailInput.addEventListener('input', () => {
                if (this.emailInput.classList.contains('invalid')) {
                    this.clearFieldError(this.emailInput);
                }
            });
        }
    }

    // === МЕТОДЫ ДЛЯ ВАЛИДАЦИИ ===
    isValidEmail(email) {
        // Простая, но эффективная проверка формата email
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email);
    }

    showFieldError(inputElement, message) {
        // Добавляем класс для визуального выделения ошибки
        inputElement.classList.add('invalid');

        // Создаем или обновляем элемент с сообщением об ошибке
        let errorDiv = inputElement.parentNode.querySelector('.field-error-message');
        if (!errorDiv) {
            errorDiv = document.createElement('div');
            errorDiv.className = 'field-error-message';
            inputElement.parentNode.appendChild(errorDiv);
            // Сохраняем ссылку на элемент ошибки для последующего удаления
            if (inputElement === this.emailInput) {
                this.emailErrorDiv = errorDiv;
            }
        }
        errorDiv.textContent = message;
    }

    clearFieldError(inputElement) {
        inputElement.classList.remove('invalid');
        const errorDiv = inputElement.parentNode.querySelector('.field-error-message');
        if (errorDiv) {
            errorDiv.remove();
            if (inputElement === this.emailInput && this.emailErrorDiv === errorDiv) {
                this.emailErrorDiv = null;
            }
        }
    }
    // === КОНЕЦ МЕТОДОВ ДЛЯ ВАЛИДАЦИИ ===

    async saveAllTags() {
        // === НАЧАЛО ВАЛИДАЦИИ ===
        const email = this.emailInput.value.trim();
        const justification = this.justificationInput.value.trim();

        // --- Валидация Email ---
        // Сначала уберем возможные предыдущие ошибки для email
        this.emailInput.classList.remove('invalid');
        if (this.emailErrorDiv) {
            this.emailErrorDiv.remove();
            this.emailErrorDiv = null;
        }

        // Проверка обязательного поля Email
        if (!email) {
            this.showFieldError(this.emailInput, 'Email is required');
            this.showStatusMessage('Please enter email address', 'error');
            return; // Прерываем выполнение, если email не заполнен
        }

        // Проверка формата Email
        if (!this.isValidEmail(email)) {
            this.showFieldError(this.emailInput, 'Please enter a valid email address (e.g., user@example.com)');
            this.showStatusMessage('Please enter a valid email address', 'error');
            return; // Прерываем выполнение, если email не валиден
        }
        // --- Конец валидации Email ---

        // Проверка обязательного поля Justification
        if (!justification) {
            this.showStatusMessage('Please enter justification', 'error');
            // Можно также подсветить поле justification, если добавить ему обработку
            return; // Прерываем выполнение, если justification не заполнено
        }

        // Проверка наличия тегов с информацией
        const validTags = this.currentTags.filter(tag => tag.name); // Фильтруем сразу здесь
        if (validTags.length === 0) {
            this.showStatusMessage('Please get information for at least one tag', 'error');
            return; // Прерываем выполнение, если нет тегов с информацией
        }
        // === КОНЕЦ ВАЛИДАЦИИ ===

        try {
            this.setLoading(true);

            // Подготавливаем данные для обновления
            const updatePromises = validTags.map(async (tag) => {
                const statusSelect = document.getElementById(`statusSelect_${tag.index}`);
                const newStatus = statusSelect ? statusSelect.value : 'OPEN';

                const response = await fetch('/api/tags/update', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        tagName: tag.name,
                        newState: newStatus,
                        email: email, // Используем проверенное значение
                        justification: justification, // Используем проверенное значение
                        user: this.currentUser // Используем this.currentUser, как в exportToPdf
                    })
                });

                // === ОБРАБОТКА ОТВЕТА ОТ СЕРВЕРА ===
                if (!response.ok) {
                    // Сервер вернул ошибку (4xx, 5xx)
                    let errorMessage = `Failed to update tag: ${response.status} ${response.statusText}`;
                    try {
                        // Пытаемся получить детали ошибки из тела ответа
                        const errorData = await response.json();
                        // Предполагаем, что сервер возвращает { "success": false, "message": "..." }
                        // или { "error": "..." }
                        errorMessage = errorData.message || errorData.error || errorMessage;
                    } catch (parseError) {
                        // Если не удалось распарсить JSON, используем дефолтное сообщение
                        console.warn(`Could not parse error response for tag ${tag.name}:`, parseError);
                    }
                    // Выбрасываем ошибку, чтобы она была поймана в Promise.allSettled
                    throw new Error(errorMessage);
                }

                // Если ответ успешный (2xx), парсим JSON
                const result = await response.json(); // Получаем JSON-ответ

                // Проверяем логику успеха от сервера (на случай, если сервер вернул 200 OK, но success=false)
                if (result.success && result.updatedTagData) {
                    // Если сервер вернул обновленные данные, используем их
                    return {
                        index: tag.index,
                        success: true,
                        updatedData: result.updatedTagData // Новые value, timestamp, good
                    };
                } else if (result.success) {
                    // Сервер сказал успех, но данных нет (старый формат ответа или ошибка получения данных)
                    return {
                        index: tag.index,
                        success: true,
                        message: result.message || 'Tag updated, but detailed data not available.'
                    };
                } else {
                    // Сервер вернул success=false
                    throw new Error(result.message || 'Server reported update failure.');
                }
                // === КОНЕЦ ОБРАБОТКИ ОТВЕТА ===
            });

            const results = await Promise.allSettled(updatePromises);

            // Обновляем отображение для каждого тега на основе результата
            let overallSuccess = true; // Флаг для общего статуса
            for (let i = 0; i < results.length; i++) {
                const resultPromise = results[i];
                const tag = validTags[i]; // Получаем тег по индексу из отфильтрованного массива

                if (resultPromise.status === 'fulfilled') {
                    const result = resultPromise.value; // Результат промиса

                    if (result.success && result.updatedData) {
                        // === Обновляем UI данными от сервера ===
                        const tagValueElement = document.getElementById(`tagValue_${tag.index}`);
                        const tagTimestampElement = document.getElementById(`tagTimestamp_${tag.index}`);
                        const statusBadgeElement = document.getElementById(`tagStatus_${tag.index}`); // Обновляем и статус

                        if (tagValueElement) {
                            tagValueElement.textContent = result.updatedData.value;
                            // Обновляем класс в зависимости от состояния Good
                            tagValueElement.className = `value ${result.updatedData.good ? 'value-good' : 'value-bad'}`;
                        }
                        if (tagTimestampElement) {
                            // Преобразуем ISO строку времени из PI в локальный формат
                            try {
                                const dateObj = new Date(result.updatedData.timestamp);
                                if (isNaN(dateObj)) {
                                    // Если дата некорректна, отображаем как есть
                                    tagTimestampElement.textContent = result.updatedData.timestamp;
                                } else {
                                    tagTimestampElement.textContent = dateObj.toLocaleString('en-US');
                                }
                            } catch (dateError) {
                                console.error(`Error formatting timestamp for tag ${tag.name}:`, dateError);
                                tagTimestampElement.textContent = result.updatedData.timestamp; // fallback
                            }
                        }
                        if (statusBadgeElement) {
                            statusBadgeElement.textContent = result.updatedData.good ? 'GOOD' : 'BAD';
                            statusBadgeElement.className = `status-badge ${result.updatedData.good ? 'status-good' : 'status-bad'}`;
                        }
                        // === Конец обновления UI ===
                    } else if (result.success) {
                        // Обновление прошло, но данные не получены или в старом формате
                        // Можно обновить только статус UI или оставить как есть
                        console.warn(`Tag ${tag.name} updated, but detailed data not available or in old format.`, result.message);
                        // Простой вариант: обновить только значение в UI на newStatus
                        const statusSelect = document.getElementById(`statusSelect_${tag.index}`);
                        const newStatus = statusSelect ? statusSelect.value : 'OPEN';
                        const tagValue = document.getElementById(`tagValue_${tag.index}`);
                        const tagTimestamp = document.getElementById(`tagTimestamp_${tag.index}`);
                        if (tagValue) {
                            tagValue.textContent = newStatus;
                            tagValue.className = 'value value-good'; // Предполагаем успех
                        }
                        if (tagTimestamp) {
                            const now = new Date().toLocaleString('en-US');
                            tagTimestamp.textContent = now;
                        }
                    } else {
                        // Этот случай маловероятен, так как ошибка должна была быть выброшена выше
                        console.error(`Unexpected result structure for tag ${tag.name}:`, result);
                        this.showStatusMessage(`Unexpected response for tag ${tag.name}.`, 'error');
                        overallSuccess = false;
                    }
                } else {
                    // Обработка ошибки для конкретного тега (rejected promise)
                    console.error(`Error updating tag ${tag.name}:`, resultPromise.reason);
                    this.showStatusMessage(`Error updating tag ${tag.name}: ${resultPromise.reason.message}`, 'error');
                    overallSuccess = false;
                }
            }

            if (overallSuccess) {
                this.showStatusMessage('All tags updated successfully');
            } else {
                this.showStatusMessage('Some tags failed to update. Check logs.', 'warning');
            }

        } catch (error) {
            // Этот catch ловит ошибки, которые могли уйти мимо Promise.allSettled
            // или ошибки в самой логике выше (хотя маловероятно)
            console.error('Unexpected error in saveAllTags:', error);
            this.showStatusMessage(`Unexpected Error: ${error.message}`, 'error');
        } finally {
            this.setLoading(false);
        }
    }

    async getCurrentUser() {
        try {
            const response = await fetch(`${this.apiUrl}/current-user`);
            if (response.ok) {
                const data = await response.json();
                this.currentUser = data.user;
                if (this.userDisplay) {
                    this.userDisplay.textContent = this.currentUser;
                }
            }
        } catch (error) {
            console.error('Error getting current user:', error);
        }
    }

    addTagField() {
        if (this.currentTags.length >= this.maxTags) {
            this.showStatusMessage(`Maximum ${this.maxTags} tags allowed`, 'error');
            return;
        }

        const tagIndex = this.currentTags.length;
        const tagDiv = document.createElement('div');
        tagDiv.className = 'tag-field';
        tagDiv.innerHTML = `
            <div class="tag-input-group">
                <input type="text" id="tagName_${tagIndex}" placeholder="Enter tag name" class="tag-input">
                <div class="autocomplete-suggestions" id="suggestions_${tagIndex}"></div>
                <select id="statusSelect_${tagIndex}" class="status-select">
                    <option value="OPEN">OPEN</option>
                    <option value="CLOSED">CLOSED</option>
                </select>
                <button type="button" class="remove-tag-btn" onclick="tagManager.removeTagField(${tagIndex})">×</button>
            </div>
            <div class="tag-info" id="tagInfo_${tagIndex}" style="display: none;">
                <div class="tag-details">
                    <span class="tag-name" id="tagNameDisplay_${tagIndex}"></span>
                    <span class="status-badge" id="tagStatus_${tagIndex}"></span>
                </div>
                <div class="tag-value">
                    <span class="value" id="tagValue_${tagIndex}"></span>
                    <span class="timestamp" id="tagTimestamp_${tagIndex}"></span>
                </div>
            </div>
        `;

        this.tagsContainer.appendChild(tagDiv);
        this.currentTags.push({
            index: tagIndex,
            name: null,
            value: null,
            infoElement: document.getElementById(`tagInfo_${tagIndex}`)
        });

        // Setup autocomplete for new field
        this.setupAutocomplete(tagIndex);
    }

    removeTagField(index) {
        const tagDiv = document.querySelector(`#tagName_${index}`).closest('.tag-field');
        if (tagDiv) {
            tagDiv.remove();
            this.currentTags = this.currentTags.filter(tag => tag.index !== index);
        }
    }

    setupAutocomplete(tagIndex) {
        const input = document.getElementById(`tagName_${tagIndex}`);
        const suggestionsDiv = document.getElementById(`suggestions_${tagIndex}`);

        if (!input || !suggestionsDiv) return;

        let timeoutId;

        input.addEventListener('input', async (e) => {
            const value = e.target.value.trim();

            // Clear previous timeout
            clearTimeout(timeoutId);

            if (value.length < 2) {
                suggestionsDiv.style.display = 'none';
                return;
            }

            // Debounce requests
            timeoutId = setTimeout(async () => {
                try {
                    const response = await fetch(`${this.apiUrl}/search/${encodeURIComponent(value)}`);
                    if (response.ok) {
                        const tags = await response.json();
                        this.showSuggestions(tags, suggestionsDiv, input, tagIndex);
                    }
                } catch (error) {
                    console.error('Error fetching suggestions:', error);
                }
            }, 300);
        });

        // Hide suggestions when clicking outside
        document.addEventListener('click', (e) => {
            if (!input.contains(e.target) && !suggestionsDiv.contains(e.target)) {
                suggestionsDiv.style.display = 'none';
            }
        });
    }

    showSuggestions(tags, suggestionsDiv, input, tagIndex) {
        if (tags.length === 0) {
            suggestionsDiv.style.display = 'none';
            return;
        }

        suggestionsDiv.innerHTML = '';
        tags.forEach(tag => {
            const div = document.createElement('div');
            div.className = 'suggestion-item';
            div.textContent = tag;
            div.addEventListener('click', () => {
                input.value = tag;
                suggestionsDiv.style.display = 'none';
                this.getTagInfo(tagIndex);
            });
            suggestionsDiv.appendChild(div);
        });

        suggestionsDiv.style.display = 'block';
    }

    async getTagInfo(tagIndex) {
        const input = document.getElementById(`tagName_${tagIndex}`);
        const tagName = input.value.trim();

        if (!tagName) {
            this.showStatusMessage('Please enter a tag name', 'error');
            return;
        }

        try {
            this.setLoading(true);

            const response = await fetch(`${this.apiUrl}/${encodeURIComponent(tagName)}`);

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Failed to get tag: ${response.status}`);
            }

            const data = await response.json();

            // Update tag info display
            this.displayTagInfo(tagIndex, tagName, data);

            this.showStatusMessage('Tag information retrieved successfully');
        } catch (error) {
            console.error('Error getting tag info:', error);
            this.showStatusMessage(`Error: ${error.message}`, 'error');
        } finally {
            this.setLoading(false);
        }
    }

    displayTagInfo(tagIndex, tagName, data) {
        const tagInfo = this.currentTags.find(tag => tag.index === tagIndex);
        if (tagInfo) {
            tagInfo.name = tagName;
            tagInfo.value = data;
        }

        // Update display elements
        const tagNameDisplay = document.getElementById(`tagNameDisplay_${tagIndex}`);
        const statusBadge = document.getElementById(`tagStatus_${tagIndex}`);
        const tagValue = document.getElementById(`tagValue_${tagIndex}`);
        const tagTimestamp = document.getElementById(`tagTimestamp_${tagIndex}`);
        const tagInfoDiv = document.getElementById(`tagInfo_${tagIndex}`);

        if (tagNameDisplay) tagNameDisplay.textContent = tagName;
        if (statusBadge) {
            statusBadge.textContent = data.good ? 'GOOD' : 'BAD';
            statusBadge.className = `status-badge ${data.good ? 'status-good' : 'status-bad'}`;
        }
        if (tagValue) {
            const displayValue = data.displayValue || data.value;
            tagValue.textContent = displayValue;
            tagValue.className = `value ${data.good ? 'value-good' : 'value-bad'}`;
        }
        if (tagTimestamp) {
            const timestamp = data.timestamp ? new Date(data.timestamp).toLocaleString('en-US') : '-';
            tagTimestamp.textContent = timestamp;
        }
        if (tagInfoDiv) {
            tagInfoDiv.style.display = 'block';
        }

        // Set the current status in the dropdown
        const statusSelect = document.getElementById(`statusSelect_${tagIndex}`);
        if (statusSelect && data.displayValue) {
            if (data.displayValue.toUpperCase() === 'OPEN') {
                statusSelect.value = 'OPEN';
            } else if (data.displayValue.toUpperCase() === 'CLOSED') {
                statusSelect.value = 'CLOSED';
            }
        }
    }

    async exportToPdf() {
        const email = this.emailInput.value.trim();
        const justification = this.justificationInput.value.trim();

        if (this.currentTags.length === 0) {
            this.showStatusMessage('Please add at least one tag', 'error');
            return;
        }

        if (!this.currentUser) {
            this.showStatusMessage('Unable to get current user', 'error');
            return;
        }

        try {
            this.setLoading(true);

            // Подготавливаем данные для экспорта
            const tagsToExport = this.currentTags
                .filter(tag => tag.name)
                .map(tag => {
                    const statusSelect = document.getElementById(`statusSelect_${tag.index}`);
                    return {
                        tagName: tag.name,
                        newState: statusSelect ? statusSelect.value : 'OPEN'
                    };
                });

            if (tagsToExport.length === 0) {
                this.showStatusMessage('No valid tags to export', 'error');
                return;
            }

            const exportData = {
                tags: tagsToExport,
                user: this.currentUser,
                email: email,
                justification: justification
            };

            // Отправляем запрос на сервер
            const response = await fetch('/api/tags/export-pdf', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(exportData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Failed to export PDF: ${response.status}`);
            }

            // Получаем PDF файл
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `pi_tags_report_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.pdf`;
            document.body.appendChild(a);
            a.click();
            window.URL.revokeObjectURL(url);
            document.body.removeChild(a);

            this.showStatusMessage('PDF exported successfully');
        } catch (error) {
            console.error('Error exporting to PDF:', error);
            this.showStatusMessage(`Error: ${error.message}`, 'error');
        } finally {
            this.setLoading(false);
        }
    }

    setLoading(loading) {
        if (this.loading) {
            this.loading.style.display = loading ? 'block' : 'none';
        }
        if (this.addTagBtn) {
            this.addTagBtn.disabled = loading;
        }
        if (this.saveBtn) {
            this.saveBtn.disabled = loading;
        }
        if (this.exportBtn) {
            this.exportBtn.disabled = loading;
        }
    }

    showStatusMessage(message, type = 'success') {
        if (!this.statusMessage) return;

        this.statusMessage.textContent = message;
        this.statusMessage.className = `status-message ${type} show`;

        setTimeout(() => {
            this.statusMessage.classList.remove('show');
        }, 5000);
    }
}

// Initialize the application when DOM is loaded
let tagManager;
document.addEventListener('DOMContentLoaded', () => {
    tagManager = new PITagManager();
});