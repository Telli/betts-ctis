class ChatSystem {
    constructor() {
        this.connection = null;
        this.currentRoomId = null;
        this.currentUser = null;
        this.replyToMessage = null;
        this.typingTimer = null;
        this.isTyping = false;
        
        this.init();
    }

    async init() {
        await this.setupSignalR();
        this.setupEventListeners();
        await this.loadRooms();
        await this.loadCurrentUser();
        await this.loadClients();
        await this.loadUsers();
    }

    async setupSignalR() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/chatHub")
            .withAutomaticReconnect()
            .build();

        // Message received
        this.connection.on("ReceiveMessage", (message) => {
            this.displayMessage(message);
            this.updateRoomLastMessage(message.ChatRoomId, message);
        });

        // Message edited
        this.connection.on("MessageEdited", (editData) => {
            this.updateMessage(editData);
        });

        // Message deleted
        this.connection.on("MessageDeleted", (deleteData) => {
            this.removeMessage(deleteData.MessageId);
        });

        // User typing
        this.connection.on("UserTyping", (data) => {
            this.showTypingIndicator(data);
        });

        // User stopped typing
        this.connection.on("UserStoppedTyping", (data) => {
            this.hideTypingIndicator(data);
        });

        // User presence changed
        this.connection.on("UserPresenceChanged", (data) => {
            this.updateUserPresence(data);
        });

        // Room assigned
        this.connection.on("RoomAssigned", (data) => {
            this.showNotification(`Room assigned to ${data.AssignedToUserId}`, 'info');
        });

        // Error handling
        this.connection.on("Error", (message) => {
            this.showNotification(message, 'error');
        });

        try {
            await this.connection.start();
            console.log("SignalR Connected");
        } catch (err) {
            console.error("SignalR Connection Error: ", err);
            this.showNotification("Failed to connect to chat server", 'error');
        }
    }

    setupEventListeners() {
        // Send message
        document.getElementById('sendMessageBtn').addEventListener('click', () => this.sendMessage());
        document.getElementById('messageInput').addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            } else {
                this.handleTyping();
            }
        });

        // Auto-resize textarea
        document.getElementById('messageInput').addEventListener('input', (e) => {
            e.target.style.height = 'auto';
            e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px';
        });

        // Room search
        document.getElementById('roomSearch').addEventListener('input', (e) => {
            this.filterRooms(e.target.value);
        });

        // Create room
        document.getElementById('createRoomBtn').addEventListener('click', () => this.createRoom());

        // Assign room
        document.getElementById('assignRoomBtn').addEventListener('click', () => {
            document.getElementById('assignRoomModal').querySelector('.modal').show();
        });
        document.getElementById('assignRoomConfirmBtn').addEventListener('click', () => this.assignRoom());

        // Search messages
        document.getElementById('searchMessagesBtn').addEventListener('click', () => {
            document.getElementById('searchMessagesModal').querySelector('.modal').show();
        });
        document.getElementById('searchForm').addEventListener('submit', (e) => {
            e.preventDefault();
            this.searchMessages();
        });

        // Room participants
        document.getElementById('roomParticipantsBtn').addEventListener('click', () => this.showParticipants());

        // Cancel reply
        document.getElementById('cancelReply').addEventListener('click', () => this.cancelReply());

        // Internal message toggle
        document.getElementById('internalMessageToggle').addEventListener('change', (e) => {
            if (e.target.checked) {
                this.showNotification('Internal note mode enabled - only visible to moderators and admins', 'warning');
            }
        });
    }

    async loadCurrentUser() {
        try {
            const response = await fetch('/api/account/profile');
            if (response.ok) {
                this.currentUser = await response.json();
            }
        } catch (error) {
            console.error('Error loading current user:', error);
        }
    }

    async loadRooms() {
        try {
            const response = await fetch('/api/chat/rooms');
            if (response.ok) {
                const rooms = await response.json();
                this.displayRooms(rooms);
            }
        } catch (error) {
            console.error('Error loading rooms:', error);
            this.showNotification('Failed to load chat rooms', 'error');
        }
    }

    async loadClients() {
        try {
            const response = await fetch('/api/clients');
            if (response.ok) {
                const clients = await response.json();
                const select = document.getElementById('clientSelect');
                clients.forEach(client => {
                    const option = document.createElement('option');
                    option.value = client.id;
                    option.textContent = `${client.firstName} ${client.lastName}`;
                    select.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading clients:', error);
        }
    }

    async loadUsers() {
        try {
            const response = await fetch('/api/users');
            if (response.ok) {
                const users = await response.json();
                const select = document.getElementById('assignToUserSelect');
                users.filter(u => u.roles.includes('Admin') || u.roles.includes('Associate'))
                     .forEach(user => {
                    const option = document.createElement('option');
                    option.value = user.id;
                    option.textContent = `${user.firstName} ${user.lastName}`;
                    select.appendChild(option);
                });
            }
        } catch (error) {
            console.error('Error loading users:', error);
        }
    }

    displayRooms(rooms) {
        const roomList = document.getElementById('roomList');
        roomList.innerHTML = '';

        rooms.forEach(room => {
            const roomElement = document.createElement('div');
            roomElement.className = 'room-item';
            roomElement.dataset.roomId = room.id;
            
            const unreadBadge = room.unreadCount > 0 ? 
                `<span class="unread-badge">${room.unreadCount}</span>` : '';
            
            const clientInfo = room.clientName ? ` - ${room.clientName}` : '';
            const taxInfo = room.taxYear ? ` (${room.taxYear})` : '';
            
            roomElement.innerHTML = `
                <div class="room-name">
                    ${room.name}${clientInfo}${taxInfo}
                    ${unreadBadge}
                </div>
                <div class="room-preview">${room.topic || 'No recent messages'}</div>
            `;

            roomElement.addEventListener('click', () => this.selectRoom(room.id));
            roomList.appendChild(roomElement);
        });
    }

    filterRooms(query) {
        const rooms = document.querySelectorAll('.room-item');
        rooms.forEach(room => {
            const name = room.querySelector('.room-name').textContent.toLowerCase();
            const preview = room.querySelector('.room-preview').textContent.toLowerCase();
            const matches = name.includes(query.toLowerCase()) || preview.includes(query.toLowerCase());
            room.style.display = matches ? 'block' : 'none';
        });
    }

    async selectRoom(roomId) {
        if (this.currentRoomId === roomId) return;

        // Leave current room
        if (this.currentRoomId) {
            await this.connection.invoke("LeaveRoom", this.currentRoomId);
        }

        this.currentRoomId = roomId;

        // Update UI
        document.querySelectorAll('.room-item').forEach(item => {
            item.classList.remove('active');
        });
        document.querySelector(`[data-room-id="${roomId}"]`).classList.add('active');

        // Join new room
        await this.connection.invoke("JoinRoom", roomId);

        // Load room details and messages
        await this.loadRoomDetails(roomId);
        await this.loadMessages(roomId);

        // Show chat interface
        document.getElementById('chatRoomHeader').style.display = 'flex';
        document.getElementById('chatInputContainer').style.display = 'block';
    }

    async loadRoomDetails(roomId) {
        try {
            const response = await fetch(`/api/chat/rooms/${roomId}`);
            if (response.ok) {
                const room = await response.json();
                document.getElementById('roomName').textContent = room.name;
                
                let details = `${room.currentParticipants} participants`;
                if (room.clientName) details += ` • ${room.clientName}`;
                if (room.taxYear) details += ` • ${room.taxYear}`;
                
                document.getElementById('roomDetails').textContent = details;

                // Show assign button for admins/associates
                const canAssign = this.currentUser && 
                    (this.currentUser.roles.includes('Admin') || this.currentUser.roles.includes('Associate'));
                document.getElementById('assignRoomBtn').style.display = canAssign ? 'inline-block' : 'none';
            }
        } catch (error) {
            console.error('Error loading room details:', error);
        }
    }

    async loadMessages(roomId, page = 1) {
        try {
            const response = await fetch(`/api/chat/rooms/${roomId}/messages?page=${page}&pageSize=50&includeInternal=true`);
            if (response.ok) {
                const result = await response.json();
                this.displayMessages(result.items);
            }
        } catch (error) {
            console.error('Error loading messages:', error);
            this.showNotification('Failed to load messages', 'error');
        }
    }

    displayMessages(messages) {
        const messagesContainer = document.getElementById('chatMessages');
        messagesContainer.innerHTML = '';

        messages.forEach(message => {
            this.displayMessage(message, false);
        });

        this.scrollToBottom();
    }

    displayMessage(message, animate = true) {
        const messagesContainer = document.getElementById('chatMessages');
        const messageElement = document.createElement('div');
        messageElement.className = `message ${message.senderId === this.currentUser?.id ? 'own' : ''}`;
        messageElement.dataset.messageId = message.id;

        const avatar = this.getAvatarInitials(message.senderName);
        const time = new Date(message.sentAt).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'});
        
        let badges = '';
        if (message.isInternal) badges += '<span class="message-badge badge-internal">Internal</span>';
        if (message.isImportant) badges += '<span class="message-badge badge-important">Important</span>';
        if (message.editedAt) badges += '<span class="message-badge badge-edited">Edited</span>';

        let replyContent = '';
        if (message.replyToMessage) {
            replyContent = `
                <div class="reply-indicator">
                    <strong>${message.replyToMessage.senderName}:</strong>
                    ${message.replyToMessage.content}
                </div>
            `;
        }

        messageElement.innerHTML = `
            <div class="message-avatar">${avatar}</div>
            <div class="message-content">
                <div class="message-header">
                    <span class="message-sender">${message.senderName}</span>
                    <span class="message-time">${time}</span>
                </div>
                ${replyContent}
                <div class="message-text">${this.formatMessageContent(message.content)}</div>
                ${badges ? `<div class="message-badges">${badges}</div>` : ''}
            </div>
        `;

        // Add context menu for own messages
        if (message.senderId === this.currentUser?.id) {
            messageElement.addEventListener('contextmenu', (e) => {
                e.preventDefault();
                this.showMessageContextMenu(e, message);
            });
        }

        // Add reply functionality
        messageElement.addEventListener('dblclick', () => {
            this.replyToMessage(message);
        });

        if (animate) {
            messageElement.style.opacity = '0';
            messageElement.style.transform = 'translateY(20px)';
        }

        messagesContainer.appendChild(messageElement);

        if (animate) {
            setTimeout(() => {
                messageElement.style.transition = 'all 0.3s ease';
                messageElement.style.opacity = '1';
                messageElement.style.transform = 'translateY(0)';
            }, 10);
        }

        this.scrollToBottom();
    }

    updateMessage(editData) {
        const messageElement = document.querySelector(`[data-message-id="${editData.MessageId}"]`);
        if (messageElement) {
            const textElement = messageElement.querySelector('.message-text');
            textElement.textContent = editData.NewContent;
            
            let badgesElement = messageElement.querySelector('.message-badges');
            if (!badgesElement) {
                badgesElement = document.createElement('div');
                badgesElement.className = 'message-badges';
                messageElement.querySelector('.message-content').appendChild(badgesElement);
            }
            
            if (!badgesElement.querySelector('.badge-edited')) {
                badgesElement.innerHTML += '<span class="message-badge badge-edited">Edited</span>';
            }
        }
    }

    removeMessage(messageId) {
        const messageElement = document.querySelector(`[data-message-id="${messageId}"]`);
        if (messageElement) {
            messageElement.style.transition = 'all 0.3s ease';
            messageElement.style.opacity = '0';
            messageElement.style.transform = 'translateY(-20px)';
            setTimeout(() => messageElement.remove(), 300);
        }
    }

    async sendMessage() {
        const input = document.getElementById('messageInput');
        const content = input.value.trim();
        
        if (!content || !this.currentRoomId) return;

        const isInternal = document.getElementById('internalMessageToggle').checked;
        const isImportant = document.getElementById('importantMessageToggle').checked;

        try {
            await this.connection.invoke("SendMessage", 
                this.currentRoomId, 
                content, 
                isInternal, 
                this.replyToMessage?.id
            );

            // Clear input and reset options
            input.value = '';
            input.style.height = 'auto';
            document.getElementById('internalMessageToggle').checked = false;
            document.getElementById('importantMessageToggle').checked = false;
            this.cancelReply();

            // Stop typing indicator
            if (this.isTyping) {
                await this.connection.invoke("StopTyping", this.currentRoomId);
                this.isTyping = false;
            }

        } catch (error) {
            console.error('Error sending message:', error);
            this.showNotification('Failed to send message', 'error');
        }
    }

    async handleTyping() {
        if (!this.currentRoomId) return;

        if (!this.isTyping) {
            this.isTyping = true;
            await this.connection.invoke("StartTyping", this.currentRoomId);
        }

        clearTimeout(this.typingTimer);
        this.typingTimer = setTimeout(async () => {
            if (this.isTyping) {
                await this.connection.invoke("StopTyping", this.currentRoomId);
                this.isTyping = false;
            }
        }, 2000);
    }

    showTypingIndicator(data) {
        const indicator = document.getElementById('typingIndicator');
        const textElement = indicator.querySelector('.typing-text');
        textElement.textContent = `${data.UserName} is typing...`;
        indicator.style.display = 'block';
    }

    hideTypingIndicator(data) {
        const indicator = document.getElementById('typingIndicator');
        indicator.style.display = 'none';
    }

    replyToMessage(message) {
        this.replyToMessage = message;
        const preview = document.getElementById('replyPreview');
        document.getElementById('replyToUser').textContent = message.senderName;
        document.getElementById('replyToMessage').textContent = message.content;
        preview.style.display = 'flex';
        document.getElementById('messageInput').focus();
    }

    cancelReply() {
        this.replyToMessage = null;
        document.getElementById('replyPreview').style.display = 'none';
    }

    async createRoom() {
        const name = document.getElementById('roomNameInput').value.trim();
        const description = document.getElementById('roomDescriptionInput').value.trim();
        const type = document.getElementById('roomTypeSelect').value;
        const clientId = document.getElementById('clientSelect').value || null;
        const taxYear = document.getElementById('taxYearInput').value.trim() || null;

        if (!name) {
            this.showNotification('Room name is required', 'error');
            return;
        }

        try {
            const response = await fetch('/api/chat/rooms', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    name,
                    description,
                    type,
                    clientId: clientId ? parseInt(clientId) : null,
                    taxYear
                })
            });

            if (response.ok) {
                const room = await response.json();
                this.showNotification('Room created successfully', 'success');
                document.getElementById('createRoomModal').querySelector('.btn-close').click();
                document.getElementById('createRoomForm').reset();
                await this.loadRooms();
                this.selectRoom(room.id);
            } else {
                const error = await response.text();
                this.showNotification(error || 'Failed to create room', 'error');
            }
        } catch (error) {
            console.error('Error creating room:', error);
            this.showNotification('Failed to create room', 'error');
        }
    }

    async assignRoom() {
        const assignToUserId = document.getElementById('assignToUserSelect').value;
        const notes = document.getElementById('assignmentNotes').value.trim();

        if (!assignToUserId || !this.currentRoomId) return;

        try {
            const response = await fetch(`/api/chat/rooms/${this.currentRoomId}/assign`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    assignToUserId,
                    notes
                })
            });

            if (response.ok) {
                this.showNotification('Room assigned successfully', 'success');
                document.getElementById('assignRoomModal').querySelector('.btn-close').click();
                document.getElementById('assignRoomForm').reset();
            } else {
                const error = await response.text();
                this.showNotification(error || 'Failed to assign room', 'error');
            }
        } catch (error) {
            console.error('Error assigning room:', error);
            this.showNotification('Failed to assign room', 'error');
        }
    }

    async searchMessages() {
        const query = document.getElementById('searchQuery').value.trim();
        const fromDate = document.getElementById('searchFromDate').value;
        const toDate = document.getElementById('searchToDate').value;
        const isInternal = document.getElementById('searchInternal').checked;
        const isImportant = document.getElementById('searchImportant').checked;

        const params = new URLSearchParams();
        if (query) params.append('query', query);
        if (this.currentRoomId) params.append('roomId', this.currentRoomId);
        if (fromDate) params.append('fromDate', fromDate);
        if (toDate) params.append('toDate', toDate);
        if (isInternal) params.append('isInternal', 'true');
        if (isImportant) params.append('isImportant', 'true');

        try {
            const response = await fetch(`/api/chat/search?${params}`);
            if (response.ok) {
                const result = await response.json();
                this.displaySearchResults(result.items);
            }
        } catch (error) {
            console.error('Error searching messages:', error);
            this.showNotification('Search failed', 'error');
        }
    }

    displaySearchResults(messages) {
        const resultsContainer = document.getElementById('searchResults');
        resultsContainer.innerHTML = '';

        if (messages.length === 0) {
            resultsContainer.innerHTML = '<p class="text-muted">No messages found.</p>';
            return;
        }

        messages.forEach(message => {
            const messageElement = document.createElement('div');
            messageElement.className = 'search-result-item p-3 border-bottom';
            
            const time = new Date(message.sentAt).toLocaleString();
            
            messageElement.innerHTML = `
                <div class="d-flex justify-content-between align-items-start">
                    <div>
                        <strong>${message.senderName}</strong>
                        <small class="text-muted ms-2">${time}</small>
                    </div>
                    <div>
                        ${message.isInternal ? '<span class="badge bg-warning">Internal</span>' : ''}
                        ${message.isImportant ? '<span class="badge bg-danger">Important</span>' : ''}
                    </div>
                </div>
                <div class="mt-2">${this.formatMessageContent(message.content)}</div>
            `;

            resultsContainer.appendChild(messageElement);
        });
    }

    async showParticipants() {
        if (!this.currentRoomId) return;

        try {
            const response = await fetch(`/api/chat/rooms/${this.currentRoomId}/participants`);
            if (response.ok) {
                const participants = await response.json();
                this.displayParticipants(participants);
                document.getElementById('participantsModal').querySelector('.modal').show();
            }
        } catch (error) {
            console.error('Error loading participants:', error);
            this.showNotification('Failed to load participants', 'error');
        }
    }

    displayParticipants(participants) {
        const container = document.getElementById('participantsList');
        container.innerHTML = '';

        participants.forEach(participant => {
            const participantElement = document.createElement('div');
            participantElement.className = 'd-flex justify-content-between align-items-center p-2 border-bottom';
            
            const onlineStatus = participant.isOnline ? 
                '<span class="badge bg-success">Online</span>' : 
                '<span class="badge bg-secondary">Offline</span>';
            
            participantElement.innerHTML = `
                <div>
                    <strong>${participant.userName}</strong>
                    <small class="text-muted d-block">${participant.role}</small>
                </div>
                <div>
                    ${onlineStatus}
                </div>
            `;

            container.appendChild(participantElement);
        });
    }

    getAvatarInitials(name) {
        return name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
    }

    formatMessageContent(content) {
        // Basic formatting - you can enhance this with markdown support, etc.
        return content.replace(/\n/g, '<br>');
    }

    updateRoomLastMessage(roomId, message) {
        const roomElement = document.querySelector(`[data-room-id="${roomId}"]`);
        if (roomElement) {
            const preview = roomElement.querySelector('.room-preview');
            preview.textContent = message.content.substring(0, 50) + (message.content.length > 50 ? '...' : '');
        }
    }

    updateUserPresence(data) {
        // Update presence indicators in participant lists, etc.
        console.log('User presence updated:', data);
    }

    scrollToBottom() {
        const messagesContainer = document.getElementById('chatMessages');
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    showNotification(message, type = 'info') {
        // Create a simple toast notification
        const toast = document.createElement('div');
        toast.className = `alert alert-${type === 'error' ? 'danger' : type} position-fixed`;
        toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        toast.textContent = message;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
}

// Initialize chat system when page loads
document.addEventListener('DOMContentLoaded', () => {
    new ChatSystem();
});
