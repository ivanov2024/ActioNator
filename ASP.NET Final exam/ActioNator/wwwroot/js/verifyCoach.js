(function() {
    window.fileUploadHandler = function() {
        return {
            files: [],
            dragover: false,
            isUploading: false,
            uploadProgress: 0,
            uploadComplete: false,
            isSubmitted: false,
            fileTypeError: false,
            
            get hasFiles() {
                return this.files.length > 0;
            },
            
            get hasImageFiles() {
                return this.files.some(file => this.isImageFile(file));
            },
            
            get hasPdfFiles() {
                return this.files.some(file => this.isPdfFile(file));
            },
            
            get canSubmit() {
                return this.hasFiles && !this.fileTypeError && !this.isUploading;
            },
            
            isImageFile(file) {
                const imageTypes = ['image/png', 'image/jpeg', 'image/gif', 'image/webp'];
                return imageTypes.includes(file.type);
            },
            
            isPdfFile(file) {
                return file.type === 'application/pdf';
            },
            
            handleFileSelect(event) {
                const newFiles = Array.from(event.target.files);
                this.addFiles(newFiles);
                // Reset the input so the same file can be selected again if removed
                event.target.value = '';
            },
            
            handleDrop(event) {
                this.dragover = false;
                const newFiles = Array.from(event.dataTransfer.files);
                this.addFiles(newFiles);
            },
            
            addFiles(newFiles) {
                if (newFiles.length === 0) return;
                
                // Check file type consistency
                const hasImages = [...this.files, ...newFiles].some(file => this.isImageFile(file));
                const hasPdfs = [...this.files, ...newFiles].some(file => this.isPdfFile(file));
                
                if (hasImages && hasPdfs) {
                    this.fileTypeError = true;
                    return;
                }
                
                this.fileTypeError = false;
                
                // Check total file size
                const totalSize = [...this.files, ...newFiles].reduce((sum, file) => sum + file.size, 0);
                const maxSize = 100 * 1024 * 1024; // 100MB
                
                if (totalSize > maxSize) {
                    alert('Total file size exceeds the maximum limit of 100MB');
                    return;
                }
                
                // Add files to the list
                this.files = [...this.files, ...newFiles];
            },
            
            removeFile(index) {
                this.files = this.files.filter((_, i) => i !== index);
                if (this.files.length === 0) {
                    this.fileTypeError = false;
                }
            },
            
            getFilePreview(file) {
                if (this.isImageFile(file)) {
                    return URL.createObjectURL(file);
                }
                return null;
            },
            
            formatFileSize(bytes) {
                if (bytes === 0) return '0 Bytes';
                
                const k = 1024;
                const sizes = ['Bytes', 'KB', 'MB', 'GB'];
                const i = Math.floor(Math.log(bytes) / Math.log(k));
                
                return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
            },
            
            downloadFile(file) {
                const url = URL.createObjectURL(file);
                const a = document.createElement('a');
                a.href = url;
                a.download = file.name;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            },
            
            submitFiles() {
                if (!this.canSubmit) return;
                
                this.isUploading = true;
                this.uploadProgress = 0;
                
                // Create form data
                const formData = new FormData();
                this.files.forEach(file => {
                    formData.append('files', file);
                });
                
                // Get the anti-forgery token
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
                
                // Send the files to the server - using window.location.origin to ensure correct protocol and port
                fetch(window.location.origin + '/User/CoachVerification/UploadDocuments', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': token
                    },
                    body: formData,
                    credentials: 'include'
                })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Network response was not ok');
                    }
                    return response.json();
                })
                .then(data => {
                    if (data.success) {
                        this.uploadComplete = true;
                        setTimeout(() => {
                            this.isSubmitted = true;
                        }, 500);
                    } else {
                        alert('Error: ' + data.message);
                        this.isUploading = false;
                    }
                })
                .catch(error => {
                    console.error('Error:', error);
                    alert('An error occurred while uploading files. Please try again.');
                    this.isUploading = false;
                });
                
                // Simulate upload progress
                const interval = setInterval(() => {
                    if (this.uploadProgress < 95) {
                        this.uploadProgress += 5;
                    } else if (!this.uploadComplete) {
                        // Hold at 95% until complete
                        clearInterval(interval);
                    }
                }, 200);
            }
        };
    };
})();