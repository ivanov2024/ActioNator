// Create Post Modal Handler

// Function to close the Create Post modal
function closeCreatePostModal() {
    const modal = document.getElementById('createPostModal');
    if (modal) {
        // Remove the modal from the DOM
        modal.remove();
        
        // Re-enable body scrolling
        document.body.style.overflow = '';
        
        // Remove any event listeners added to document
        document.removeEventListener('keydown', escKeyHandler);
    }
}

// Handler for Escape key to avoid memory leaks
function escKeyHandler(e) {
    if (e.key === 'Escape') {
        closeCreatePostModal();
    }
}

// Function to open the Create Post modal
function openCreatePostModal() {
    // Create a custom modal that matches the Facebook-style design
    const modalHtml = `
    <div id="createPostModal" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
        <div class="flex items-center justify-center min-h-screen p-4 text-center sm:block sm:p-0">
            <!-- Background overlay -->
            <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true"></div>
            
            <!-- Modal panel -->
            <div class="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
                <!-- Modal header -->
                <div class="px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
                    <div class="flex justify-between items-center pb-3 border-b">
                        <h3 class="text-lg leading-6 font-medium text-gray-900" id="modal-title">Create Post</h3>
                        <button type="button" class="close-modal text-gray-400 hover:text-gray-500">
                            <svg class="h-6 w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    </div>
                </div>
                
                <!-- User info -->
                <div class="px-4 sm:px-6">
                    <div class="flex items-center space-x-3 mb-4">
                        <div class="h-10 w-10 rounded-full bg-purple-300 flex-shrink-0"></div>
                        <div>
                            <div class="font-medium">You</div>
                            <div class="flex items-center text-sm text-gray-500">
                                <svg class="h-4 w-4 mr-1" fill="currentColor" viewBox="0 0 20 20">
                                    <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm1-11a1 1 0 10-2 0v2H7a1 1 0 100 2h2v2a1 1 0 102 0v-2h2a1 1 0 100-2h-2V7z" clip-rule="evenodd"></path>
                                </svg>
                                <select id="postVisibility" class="bg-transparent border-none text-sm focus:ring-0 p-0 pr-6">
                                    <option value="Public">Public</option>
                                    <option value="Friends">Friends</option>
                                    <option value="Private">Private</option>
                                </select>
                            </div>
                        </div>
                    </div>
                </div>
                
                <!-- Post content -->
                <div class="px-4 sm:px-6">
                    <textarea 
                        id="postContent" 
                        placeholder="What's on your mind?" 
                        class="w-full border-none focus:ring-0 text-lg resize-none h-32"
                    ></textarea>
                    
                    <!-- Image preview area -->
                    <div id="imagePreviewContainer" class="hidden grid grid-cols-2 gap-2 mb-4">
                        <!-- Image previews will be inserted here -->
                    </div>
                </div>
                
                <!-- Add to post section -->
                <div class="px-4 sm:px-6 py-3 border-t border-b">
                    <div class="flex justify-between items-center">
                        <div class="text-sm font-medium">Add to your post:</div>
                        <div class="flex space-x-4">
                            <!-- Photo/Video button -->
                            <button type="button" id="uploadImageBtn" class="text-green-600 hover:bg-gray-100 p-2 rounded-full">
                                <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                </svg>
                            </button>
                            <input type="file" id="imageUpload" class="hidden" accept="image/*" multiple />
                            
                            <!-- Tag people button -->
                            <button type="button" id="tagPeopleBtn" class="text-blue-600 hover:bg-gray-100 p-2 rounded-full">
                                <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                                </svg>
                            </button>
                        </div>
                    </div>
                </div>
                
                <!-- User tagging dropdown (initially hidden) -->
                <div id="tagPeopleDropdown" class="hidden px-4 sm:px-6 py-3 border-b">
                    <div class="mb-2">
                        <input type="text" id="tagSearchInput" placeholder="Search for friends" class="w-full border-gray-300 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500">
                    </div>
                    <div id="taggedPeopleList" class="flex flex-wrap gap-2 mb-2">
                        <!-- Tagged people will appear here -->
                    </div>
                    <div id="tagSuggestionsList" class="max-h-32 overflow-y-auto">
                        <!-- Friend suggestions will appear here -->
                    </div>
                </div>
                
                <!-- Post button -->
                <div class="px-4 py-3 sm:px-6 sm:flex">
                    <button type="button" id="submitPostBtn" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:w-auto sm:text-sm">
                        Post
                    </button>
                </div>
            </div>
        </div>
    </div>
    `;
    
    // Add the modal to the DOM
    const modalContainer = document.createElement('div');
    modalContainer.innerHTML = modalHtml;
    document.body.appendChild(modalContainer.firstElementChild);
    
    // Set up event listeners
    setupCreatePostEventListeners();
    
    // Prevent body scrolling
    document.body.style.overflow = 'hidden';
}

// Set up event listeners for the Create Post modal
function setupCreatePostEventListeners() {
    const modal = document.getElementById('createPostModal');
    const closeBtn = modal.querySelector('.close-modal');
    const uploadImageBtn = document.getElementById('uploadImageBtn');
    const imageUpload = document.getElementById('imageUpload');
    const postContent = document.getElementById('postContent');
    const imagePreviewContainer = document.getElementById('imagePreviewContainer');
    const submitPostBtn = document.getElementById('submitPostBtn');
    const tagPeopleBtn = document.getElementById('tagPeopleBtn');
    const tagPeopleDropdown = document.getElementById('tagPeopleDropdown');
    const tagSearchInput = document.getElementById('tagSearchInput');
    const taggedPeopleList = document.getElementById('taggedPeopleList');
    const tagSuggestionsList = document.getElementById('tagSuggestionsList');
    
    // Close modal button
    closeBtn.addEventListener('click', function() {
        closeCreatePostModal();
    });
    
    // Close modal when clicking outside
    modal.addEventListener('click', function(e) {
        if (e.target === modal) {
            closeCreatePostModal();
        }
    });
    
    // Close modal with Escape key
    document.addEventListener('keydown', escKeyHandler);
    
    // Image upload button
    uploadImageBtn.addEventListener('click', function() {
        imageUpload.click();
    });
    
    // Handle image upload
    imageUpload.addEventListener('change', function(e) {
        if (e.target.files.length > 0) {
            imagePreviewContainer.classList.remove('hidden');
            
            // Process each selected file
            Array.from(e.target.files).forEach(file => {
                const reader = new FileReader();
                
                reader.onload = function(event) {
                    const imageId = 'img_' + Date.now() + Math.floor(Math.random() * 1000);
                    const imagePreview = document.createElement('div');
                    imagePreview.className = 'relative';
                    imagePreview.innerHTML = `
                        <img src="${event.target.result}" class="w-full h-32 object-cover rounded" />
                        <button type="button" data-image-id="${imageId}" class="absolute top-1 right-1 bg-gray-800 bg-opacity-70 rounded-full p-1 text-white hover:bg-opacity-100">
                            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>
                    `;
                    
                    imagePreviewContainer.appendChild(imagePreview);
                    
                    // Add remove button functionality
                    const removeBtn = imagePreview.querySelector('button');
                    removeBtn.addEventListener('click', function() {
                        imagePreview.remove();
                        
                        // Hide container if no more images
                        if (imagePreviewContainer.children.length === 0) {
                            imagePreviewContainer.classList.add('hidden');
                        }
                    });
                };
                
                reader.readAsDataURL(file);
            });
        }
    });
    
    // Tag people button toggle
    tagPeopleBtn.addEventListener('click', function() {
        tagPeopleDropdown.classList.toggle('hidden');
    });
    
    // Mock friend data for tagging
    const mockFriends = [
        { id: 1, name: 'John Doe', avatar: 'bg-blue-300' },
        { id: 2, name: 'Jane Smith', avatar: 'bg-green-300' },
        { id: 3, name: 'Mike Johnson', avatar: 'bg-yellow-300' },
        { id: 4, name: 'Sarah Williams', avatar: 'bg-red-300' },
        { id: 5, name: 'David Brown', avatar: 'bg-purple-300' },
    ];
    
    // Populate friend suggestions
    function renderFriendSuggestions(query = '') {
        tagSuggestionsList.innerHTML = '';
        
        const filteredFriends = mockFriends.filter(friend => 
            friend.name.toLowerCase().includes(query.toLowerCase())
        );
        
        filteredFriends.forEach(friend => {
            const friendEl = document.createElement('div');
            friendEl.className = 'flex items-center p-2 hover:bg-gray-100 cursor-pointer';
            friendEl.innerHTML = `
                <div class="h-8 w-8 rounded-full ${friend.avatar} mr-2"></div>
                <span>${friend.name}</span>
            `;
            
            friendEl.addEventListener('click', function() {
                addTaggedPerson(friend);
            });
            
            tagSuggestionsList.appendChild(friendEl);
        });
    }
    
    // Search for friends to tag
    tagSearchInput.addEventListener('input', function() {
        renderFriendSuggestions(this.value);
    });
    
    // Initialize friend suggestions
    renderFriendSuggestions();
    
    // Add tagged person
    function addTaggedPerson(friend) {
        // Check if already tagged
        if (document.querySelector(`[data-friend-id="${friend.id}"]`)) {
            return;
        }
        
        const tagEl = document.createElement('div');
        tagEl.className = 'flex items-center bg-blue-100 rounded-full px-3 py-1';
        tagEl.setAttribute('data-friend-id', friend.id);
        tagEl.innerHTML = `
            <span class="mr-1">${friend.name}</span>
            <button type="button" class="text-gray-500 hover:text-gray-700">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
            </button>
        `;
        
        taggedPeopleList.appendChild(tagEl);
        
        // Remove tag functionality
        const removeBtn = tagEl.querySelector('button');
        removeBtn.addEventListener('click', function() {
            tagEl.remove();
        });
        
        // Clear search input
        tagSearchInput.value = '';
        renderFriendSuggestions();
    }
    
    // Submit post button
    submitPostBtn.addEventListener('click', function() {
        const content = postContent.value.trim();
        const visibility = document.getElementById('postVisibility').value;
        
        // Validate post content
        if (!content) {
            alert('Please enter some content for your post.');
            return;
        }
        
        // Get tagged friends
        const taggedFriends = Array.from(taggedPeopleList.querySelectorAll('[data-friend-id]'))
            .map(el => parseInt(el.getAttribute('data-friend-id')));
        
        // Submit the post
        submitPost(content, visibility, taggedFriends);
    });
}

// Function to submit the post to the server
function submitPost(content, visibility, taggedFriends) {
    // Show loading state
    const submitBtn = document.getElementById('submitPostBtn');
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = `
        <svg class="animate-spin -ml-1 mr-2 h-4 w-4 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        Posting...
    `;
    
    // Create FormData object for file uploads
    const postFormData = new FormData();
    postFormData.append('Content', content);
    postFormData.append('Visibility', visibility || 'Public');
    
    // Add tagged friends
    if (taggedFriends && taggedFriends.length > 0) {
        taggedFriends.forEach((friendId, index) => {
            postFormData.append(`TaggedFriends[${index}]`, friendId);
        });
    }
    
    // Get images from preview container
    const imagePreviewContainer = document.getElementById('imagePreviewContainer');
    const imagePreviews = Array.from(imagePreviewContainer.querySelectorAll('img'));
    const imagePromises = [];
    
    // Compress and convert data URLs to Blobs and add to FormData
    if (imagePreviews.length > 0) {
        imagePreviews.forEach((img, index) => {
            const dataUrl = img.src;
            if (dataUrl.startsWith('data:')) {
                const promise = compressImage(dataUrl)
                    .then(compressedBlob => {
                        // Get file extension from mime type
                        const mimeType = compressedBlob.type;
                        const extension = mimeType.split('/')[1] || 'jpg';
                        postFormData.append(`Images[${index}]`, compressedBlob, `image${index}.${extension}`);
                    });
                imagePromises.push(promise);
            }
        });
    }
    
    // Wait for all image conversions to complete
    Promise.all(imagePromises)
        .then(() => {
            // Get CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            
            // Add the token to the form data instead of using it in headers
            if (token) {
                postFormData.append('__RequestVerificationToken', token);
            }
            
            // Send the form data
            return fetch('/User/Community/CreatePost', {
                method: 'POST',
                body: postFormData
            });
        })
        .then(async response => {
            const data = await response.json();
            
            if (!response.ok) {
                // Server returned an error with details
                throw new Error(data.message || 'Failed to create post');
            }
            
            return data;
        })
        .then(data => {
            console.log('Post created successfully:', data);
            
            // Show success message with the message from the server
            showToast(data.message || 'Post created successfully!', 'success');
            
            // Close the modal
            closeCreatePostModal();
            
            // Refresh the posts list or add the new post to the DOM
            // This could be handled by SignalR or by manually adding the post
            setTimeout(() => {
                window.location.reload();
            }, 500);
        })
        .catch(error => {
            console.error('Error creating post:', error);
            
            // Show detailed error message
            let errorMessage = error.message || 'Error creating post. Please try again.';
            
            // Check for specific error types
            if (errorMessage.includes('size')) {
                errorMessage = 'One or more images exceed the maximum allowed size of 5MB.';
            } else if (errorMessage.includes('format') || errorMessage.includes('type')) {
                errorMessage = 'One or more images have an invalid format. Allowed formats: JPG, PNG, GIF, WEBP.';
            }
            
            showToast(errorMessage, 'error');
            
            // Reset button
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalBtnText;
        });
}

// Function to compress images before upload
async function compressImage(dataUrl, maxWidth = 1200, maxHeight = 1200, quality = 0.8) {
    return new Promise((resolve, reject) => {
        // Create an image object
        const img = new Image();
        img.onload = function() {
            // Create a canvas element
            const canvas = document.createElement('canvas');
            
            // Calculate new dimensions while maintaining aspect ratio
            let width = img.width;
            let height = img.height;
            
            // Only resize if the image is larger than the max dimensions
            if (width > maxWidth || height > maxHeight) {
                const ratio = Math.min(maxWidth / width, maxHeight / height);
                width = Math.floor(width * ratio);
                height = Math.floor(height * ratio);
            }
            
            // Set canvas dimensions
            canvas.width = width;
            canvas.height = height;
            
            // Draw image on canvas
            const ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0, width, height);
            
            // Get the image type from the data URL
            const mimeType = dataUrl.split(';')[0].split(':')[1];
            
            // Convert canvas to blob with compression
            canvas.toBlob(blob => {
                if (blob) {
                    resolve(blob);
                } else {
                    // Fallback if compression fails
                    fetch(dataUrl)
                        .then(res => res.blob())
                        .then(resolve)
                        .catch(reject);
                }
            }, mimeType, quality);
        };
        
        img.onerror = function() {
            // Fallback if loading fails
            fetch(dataUrl)
                .then(res => res.blob())
                .then(resolve)
                .catch(reject);
        };
        
        // Load the image
        img.src = dataUrl;
    });
}

// Function to show toast notifications
function showToast(message, type = 'info') {
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `fixed bottom-4 right-4 px-4 py-2 rounded-lg shadow-lg text-white ${type === 'success' ? 'bg-green-500' : 'bg-red-500'} transition-opacity duration-300`;
    toast.style.zIndex = '9999';
    toast.textContent = message;
    
    // Add to DOM
    document.body.appendChild(toast);
    
    // Remove after delay
    setTimeout(() => {
        toast.classList.add('opacity-0');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Initialize when the document is loaded
document.addEventListener('DOMContentLoaded', function() {
    console.log('Create Post module initialized');
    
    // Find the post creation trigger element
    const createPostTrigger = document.querySelector('.create-post-trigger');
    
    // Add click event to open the modal
    if (createPostTrigger) {
        console.log('Found create post trigger, attaching event listener');
        createPostTrigger.addEventListener('click', function() {
            console.log('Create post trigger clicked');
            openCreatePostModal();
        });
    } else {
        console.log('Create post trigger not found');
    }
    
    // Also attach to the "What's on your mind?" input if it exists
    const whatOnMindInput = document.querySelector('input[placeholder="What\'s on your mind?"]');
    if (whatOnMindInput) {
        whatOnMindInput.addEventListener('click', function(e) {
            e.preventDefault();
            openCreatePostModal();
        });
    }
});
