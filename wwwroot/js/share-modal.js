// Google Drive-like Share Modal Functionality

let currentItemId = null;
let currentItemName = null;

// Open share modal
function openShareModal(itemId, itemName) {
    currentItemId = itemId;
    currentItemName = itemName;
    
    document.getElementById('shareModalTitle').textContent = `Share "${itemName}"`;
    document.getElementById('shareModalItemId').value = itemId;
    
    // Load existing shares
    loadExistingShares(itemId);
    
    // Load or generate share link
    loadShareLink(itemId);
    
    // Show modal
    const modal = new bootstrap.Modal(document.getElementById('shareModal'));
    modal.show();
    
    // Switch to first tab
    showShareTab('people');
}

// Show specific tab in share modal
function showShareTab(tabName) {
    // Hide all tabs
    document.querySelectorAll('.share-tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    
    // Show selected tab
    document.getElementById(`tab-${tabName}`).classList.add('active');
    
    // Update tab buttons
    document.querySelectorAll('.share-tab-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    document.querySelector(`[onclick="showShareTab('${tabName}')"]`).classList.add('active');
}

// Share with specific person
async function shareWithPerson() {
    const email = document.getElementById('shareEmail').value.trim();
    const permission = document.getElementById('sharePermission').value;
    const allowDownload = document.getElementById('allowDownload').checked;
    const notify = document.getElementById('notifyPerson').checked;
    const message = document.getElementById('shareMessage').value.trim();
    
    if (!email) {
        showAlert('Please enter an email address', 'danger');
        return;
    }
    
    if (!isValidEmail(email)) {
        showAlert('Please enter a valid email address', 'danger');
        return;
    }
    
    const formData = new FormData();
    formData.append('ItemId', currentItemId);
    formData.append('ShareWithEmail', email);
    formData.append('Permission', permission);
    formData.append('CreatePublicLink', 'false');
    formData.append('AllowDownload', allowDownload);
    formData.append('Notify', notify);
    if (message) {
        formData.append('Message', message);
    }
    
    // Get anti-forgery token
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        const response = await fetch('/Storage/Share', {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token
            },
            body: formData
        });
        
        if (response.ok) {
            showAlert(`Successfully shared with ${email}`, 'success');
            document.getElementById('shareEmail').value = '';
            document.getElementById('shareMessage').value = '';
            
            // Reload shares
            loadExistingShares(currentItemId);
        } else {
            showAlert('Failed to share. Please try again.', 'danger');
        }
    } catch (error) {
        console.error('Error sharing:', error);
        showAlert('An error occurred while sharing', 'danger');
    }
}

// Load existing shares for the item
async function loadExistingShares(itemId) {
    try {
        const response = await fetch(`/Share/GetSharesForItem?itemId=${itemId}`);
        const data = await response.json();
        
        if (data.success && data.shares) {
            displayShares(data.shares);
        }
    } catch (error) {
        console.error('Error loading shares:', error);
    }
}

// Display shares in "Who has access" tab
function displayShares(shares) {
    const container = document.getElementById('sharesList');
    
    if (shares.length === 0) {
        container.innerHTML = '<p class="text-muted text-center py-3">Not shared with anyone yet</p>';
        return;
    }
    
    let html = '<div class="list-group">';
    
    shares.forEach(share => {
        const sharedWith = share.sharedWithEmail || 'Anyone with the link';
        const permission = getPermissionDisplay(share.permission);
        const isPublicLink = !share.sharedWithEmail;
        
        html += `
            <div class="list-group-item d-flex justify-content-between align-items-center">
                <div class="d-flex align-items-center flex-grow-1">
                    <div class="share-avatar me-3">
                        ${isPublicLink ? '🔗' : '👤'}
                    </div>
                    <div>
                        <div class="fw-bold">${sharedWith}</div>
                        <small class="text-muted">
                            ${permission}
                            ${share.allowDownload ? '' : ' • Download disabled'}
                            ${share.lastAccessedAt ? ` • Last accessed ${formatDate(share.lastAccessedAt)}` : ''}
                        </small>
                    </div>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm btn-outline-secondary dropdown-toggle" data-bs-toggle="dropdown">
                        ${permission}
                    </button>
                    <ul class="dropdown-menu">
                        <li><a class="dropdown-item" onclick="changePermission(${share.id}, 1)">Viewer</a></li>
                        <li><a class="dropdown-item" onclick="changePermission(${share.id}, 2)">Commenter</a></li>
                        <li><a class="dropdown-item" onclick="changePermission(${share.id}, 3)">Editor</a></li>
                        <li><hr class="dropdown-divider"></li>
                        <li><a class="dropdown-item text-danger" onclick="removeAccess(${share.id})">Remove access</a></li>
                    </ul>
                </div>
            </div>
        `;
    });
    
    html += '</div>';
    container.innerHTML = html;
}

// Load or generate share link
async function loadShareLink(itemId) {
    try {
        const response = await fetch(`/Share/GetShareLink?itemId=${itemId}`);
        const data = await response.json();
        
        if (data.success && data.link) {
            document.getElementById('shareLinkUrl').value = data.link;
            document.getElementById('shareLinkContainer').style.display = 'block';
        } else {
            // No link exists yet - show button to create one
            document.getElementById('shareLinkContainer').innerHTML = `
                <button class="btn btn-primary mt-2" onclick="createPublicLink()">
                    <i class="fas fa-link me-2"></i>Create Public Link
                </button>
            `;
            document.getElementById('shareLinkContainer').style.display = 'block';
        }
    } catch (error) {
        console.error('Error loading share link:', error);
    }
}

// Create public link
async function createPublicLink() {
    const permission = document.getElementById('linkPermission').value;
    const allowDownload = document.getElementById('linkAllowDownload').checked;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        const response = await fetch('/Share/CreatePublicLink', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `itemId=${currentItemId}&permission=${permission}&allowDownload=${allowDownload}`
        });
        
        const data = await response.json();
        
        if (data.success) {
            // Show the link
            document.getElementById('shareLinkUrl').value = data.link;
            document.getElementById('shareLinkContainer').innerHTML = `
                <div class="share-link-input-group">
                    <input type="text" 
                           id="shareLinkUrl" 
                           class="form-control" 
                           value="${data.link}"
                           readonly>
                    <button type="button" 
                            id="copyLinkBtn" 
                            class="btn btn-primary" 
                            onclick="copyShareLink()">
                        <i class="fas fa-copy me-2"></i>Copy link
                    </button>
                </div>
            `;
            showAlert('Public link created successfully!', 'success');
            
            // Refresh shares list
            loadExistingShares(currentItemId);
        } else {
            showAlert(data.message || 'Failed to create link', 'danger');
        }
    } catch (error) {
        console.error('Error creating public link:', error);
        showAlert('An error occurred', 'danger');
    }
}

// Copy share link to clipboard
function copyShareLink() {
    const linkInput = document.getElementById('shareLinkUrl');
    linkInput.select();
    linkInput.setSelectionRange(0, 99999);
    
    navigator.clipboard.writeText(linkInput.value).then(() => {
        const btn = document.getElementById('copyLinkBtn');
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i class="fas fa-check"></i> Copied!';
        btn.classList.add('btn-success');
        btn.classList.remove('btn-primary');
        
        setTimeout(() => {
            btn.innerHTML = originalText;
            btn.classList.remove('btn-success');
            btn.classList.add('btn-primary');
        }, 2000);
    });
}

// Change permission for a share
async function changePermission(shareId, permission) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        const response = await fetch('/Share/ChangePermission', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `shareId=${shareId}&permission=${permission}&allowDownload=true`
        });
        
        const data = await response.json();
        
        if (data.success) {
            showAlert('Permission updated successfully', 'success');
            loadExistingShares(currentItemId);
        } else {
            showAlert(data.message || 'Failed to update permission', 'danger');
        }
    } catch (error) {
        console.error('Error changing permission:', error);
        showAlert('An error occurred', 'danger');
    }
}

// Remove access for a share
async function removeAccess(shareId) {
    if (!confirm('Are you sure you want to remove access?')) {
        return;
    }
    
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    
    try {
        const response = await fetch('/Share/RemoveAccess', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                'RequestVerificationToken': token
            },
            body: `shareId=${shareId}`
        });
        
        const data = await response.json();
        
        if (data.success) {
            showAlert('Access removed successfully', 'success');
            loadExistingShares(currentItemId);
        } else {
            showAlert(data.message || 'Failed to remove access', 'danger');
        }
    } catch (error) {
        console.error('Error removing access:', error);
        showAlert('An error occurred', 'danger');
    }
}

// Utility functions
function isValidEmail(email) {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}

function getPermissionDisplay(permission) {
    const permissions = {
        1: 'Viewer',
        2: 'Commenter',
        3: 'Editor',
        4: 'Owner'
    };
    return permissions[permission] || 'Unknown';
}

function formatDate(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    
    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    if (diffHours < 24) return `${diffHours} hours ago`;
    if (diffDays < 7) return `${diffDays} days ago`;
    return date.toLocaleDateString();
}

function showAlert(message, type) {
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show position-fixed top-0 start-50 translate-middle-x mt-3`;
    alertDiv.style.zIndex = '9999';
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(alertDiv);
    
    setTimeout(() => {
        alertDiv.remove();
    }, 3000);
}
