class PITagManager {
    constructor() {
        this.apiUrl = '/api/tags';
        this.currentTags = [];
        this.maxTags = 10;
        this.currentUser = null;
        
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
        
        // Allow Enter key in input field
        this.emailInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.saveAllTags();
            }
        });
    }

        async saveAllTags() {
        const email = this.emailInput.value.trim();
        const justification = this.justificationInput.value.trim();

        // Проверка обязательных полей
        if (!email) {
            this.showStatusMessage('Please enter email address', 'error');
            return;
        }

        // Проверка формата email
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            this.showStatusMessage('Please enter a valid email address', 'error');
            return;
        }

        if (!justification) {
            this.showStatusMessage('Please enter justification', 'error');
            return;
        }

        // Проверка тегов
        if (this.currentTags.length === 0) {
            this.showStatusMessage('Please add at least one tag', 'error');
            return;
        }

        // ... остальной код сохранения ...
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
        // Добавь этот метод в класс PITagManager

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

    async saveAllTags() {
        const email = this.emailInput.value.trim();
        const justification = this.justificationInput.value.trim();

        if (!email) {
            this.showStatusMessage('Please enter email address', 'error');
            return;
        }

        if (!justification) {
            this.showStatusMessage('Please enter justification', 'error');
            return;
        }

        if (this.currentTags.length === 0) {
            this.showStatusMessage('Please add at least one tag', 'error');
            return;
        }

        try {
            this.setLoading(true);
            
            // Update all tags
            const updatePromises = this.currentTags.map(async (tag) => {
                if (!tag.name) return null;
                
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
                        email: email,
                        justification: justification,
                        user: this.currentUser
                    })
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.error || `Failed to update tag: ${response.status}`);
                }

                return await response.json();
            });

            const results = await Promise.all(updatePromises);
            
            // Update displayed values and timestamps
            this.currentTags.forEach(tag => {
                if (tag.name) {
                    const statusSelect = document.getElementById(`statusSelect_${tag.index}`);
                    const newStatus = statusSelect ? statusSelect.value : 'OPEN';
                    
                    const tagValue = document.getElementById(`tagValue_${tag.index}`);
                    const tagTimestamp = document.getElementById(`tagTimestamp_${tag.index}`);
                    
                    if (tagValue) {
                        tagValue.textContent = newStatus;
                        tagValue.className = 'value value-good';
                    }
                    if (tagTimestamp) {
                        const now = new Date().toLocaleString('en-US');
                        tagTimestamp.textContent = now;
                    }
                }
            });

            this.showStatusMessage('All tags updated successfully');
        } catch (error) {
            console.error('Error updating tags:', error);
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