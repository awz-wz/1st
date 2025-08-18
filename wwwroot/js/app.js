class PITagManager {
    constructor() {
        this.apiUrl = '/api/tags';
        this.currentTag = null;
        
        // DOM Elements
        this.tagNameInput = document.getElementById('tagName');
        this.getTagBtn = document.getElementById('getTagBtn');
        this.tagInfoSection = document.getElementById('tagInfoSection');
        this.controlSection = document.getElementById('controlSection');
        this.statusSelect = document.getElementById('statusSelect');
        this.updateTagBtn = document.getElementById('updateTagBtn');
        this.loading = document.getElementById('loading');
        this.statusMessage = document.getElementById('statusMessage');
        
        this.init();
    }

    init() {
        // Проверяем, что все элементы найдены
        if (!this.tagNameInput || !this.getTagBtn || !this.tagInfoSection || !this.controlSection || !this.statusSelect || !this.updateTagBtn || !this.loading || !this.statusMessage) {
            console.error('Один или несколько элементов DOM не найдены');
            return;
        }

        
        // Event Listeners
        this.getTagBtn.addEventListener('click', () => this.getTagInfo());
        
        // Проверяем другие элементы перед добавлением обработчиков
        if (this.updateTagBtn) {
            this.updateTagBtn.addEventListener('click', () => this.updateTagStatus());
        }
        
        // Allow Enter key in input field
        this.tagNameInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.getTagInfo();
            }
        });
    }

    setLoading(loading) {
        if (this.loading) {
            this.loading.style.display = loading ? 'block' : 'none';
        }
        if (this.getTagBtn) {
            this.getTagBtn.disabled = loading;
        }
        if (this.updateTagBtn) {
            this.updateTagBtn.disabled = loading;
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

    async getTagInfo() {
        if (!this.tagNameInput) return;
        
        const tagName = this.tagNameInput.value.trim();
        
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
            this.currentTag = tagName;
            
            this.displayTagInfo(tagName, data);
            
            if (this.controlSection) {
                this.controlSection.style.display = 'block';
            }
            
            this.showStatusMessage('Tag information retrieved successfully');
        } catch (error) {
            console.error('Error getting tag info:', error);
            this.showStatusMessage(`Error: ${error.message}`, 'error');
            
            if (this.tagInfoSection) {
                this.tagInfoSection.style.display = 'none';
            }
            if (this.controlSection) {
                this.controlSection.style.display = 'none';
            }
        } finally {
            this.setLoading(false);
        }
    }

    displayTagInfo(tagName, data) {
        if (!this.tagInfoSection) return;
        
        // Update tag name display
        const tagNameDisplay = document.getElementById('tagNameDisplay');
        if (tagNameDisplay) {
            tagNameDisplay.textContent = tagName;
        }
        
        // Update status badge
        const statusBadge = document.getElementById('tagStatus');
        if (statusBadge) {
            statusBadge.textContent = data.good ? 'GOOD' : 'BAD';
            statusBadge.className = `status-badge ${data.good ? 'status-good' : 'status-bad'}`;
        }
        
        // Update tag details
        const tagValue = document.getElementById('tagValue');
        if (tagValue) {
            tagValue.textContent = data.displayValue || data.value;
            tagValue.className = `value ${data.good ? 'value-good' : 'value-bad'}`;
        }
        
        // Format timestamp
        const tagTimestamp = document.getElementById('tagTimestamp');
        if (tagTimestamp) {
            const timestamp = data.timestamp ? new Date(data.timestamp).toLocaleString('en-US') : '-';
            tagTimestamp.textContent = timestamp;
        }
        
        const tagUnits = document.getElementById('tagUnits');
        if (tagUnits) {
            tagUnits.textContent = data.unitsAbbreviation || '-';
        }
        
        // Show tag info section
        this.tagInfoSection.style.display = 'block';
        
        // Set the current status in the dropdown
        if (this.statusSelect && data.displayValue) {
            if (data.displayValue.toUpperCase() === 'OPEN') {
                this.statusSelect.value = 'OPEN';
            } else if (data.displayValue.toUpperCase() === 'CLOSED') {
                this.statusSelect.value = 'CLOSED';
            }
        }
    }

    async updateTagStatus() {
        if (!this.currentTag) {
            this.showStatusMessage('Please get tag information first', 'error');
            return;
        }

        if (!this.statusSelect) return;
        const newStatus = this.statusSelect.value;
        
        try {
            this.setLoading(true);
            
            const response = await fetch('/api/tags/update', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    tagName: this.currentTag,
                    newState: newStatus
                })
            });
            
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.error || `Failed to update tag: ${response.status}`);
            }
            
            const result = await response.json();
            
            if (result.success) {
                this.showStatusMessage(`Tag successfully updated to ${newStatus}`);
                
                // Update the displayed value
                const tagValue = document.getElementById('tagValue');
                if (tagValue) {
                    tagValue.textContent = newStatus;
                    tagValue.className = 'value value-good';
                }
                
                // Update timestamp
                const tagTimestamp = document.getElementById('tagTimestamp');
                if (tagTimestamp) {
                    const now = new Date().toLocaleString('en-US');
                    tagTimestamp.textContent = now;
                }
            } else {
                throw new Error(result.message || 'Failed to update tag');
            }
        } catch (error) {
            console.error('Error updating tag:', error);
            this.showStatusMessage(`Error: ${error.message}`, 'error');
        } finally {
            this.setLoading(false);
        }
    }
}

// Initialize the application when DOM is loaded

    document.addEventListener('DOMContentLoaded', () => {
        new PITagManager();
    });