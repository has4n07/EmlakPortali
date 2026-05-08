/**
 * Client-side image validation utility
 * Provides pre-upload validation to improve UX and reduce server load
 */
window.ImageValidator = (function () {
    // Allowed file extensions
    const ALLOWED_EXTENSIONS = ['jpg', 'jpeg', 'png', 'webp'];
    const ALLOWED_MIME_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
    const MAX_FILE_SIZE_MB = 10;
    const MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024;

    // Magic bytes for file type detection
    const FILE_SIGNATURES = {
        'image/jpeg': [0xFF, 0xD8, 0xFF],
        'image/png': [0x89, 0x50, 0x4E, 0x47],
        'image/webp': [0x52, 0x49, 0x46, 0x46] // RIFF header (needs additional WEBP check)
    };

    /**
     * Validates file extension
     */
    function validateExtension(file) {
        const fileName = file.name || '';
        const extension = fileName.split('.').pop()?.toLowerCase() || '';
        
        if (!extension) {
            return { valid: false, message: 'Dosya uzantısı bulunamadı.' };
        }

        if (!ALLOWED_EXTENSIONS.includes(extension)) {
            return { 
                valid: false, 
                message: `"${extension}" uzantısı desteklenmiyor. Sadece JPG, PNG ve WebP yükleyebilirsiniz.` 
            };
        }

        return { valid: true };
    }

    /**
     * Validates MIME type
     */
    function validateMimeType(file) {
        const mimeType = file.type?.toLowerCase() || '';

        if (!mimeType) {
            return { valid: false, message: 'Dosya türü belirlenemedi.' };
        }

        if (!ALLOWED_MIME_TYPES.includes(mimeType)) {
            return { 
                valid: false, 
                message: `Desteklenmeyen dosya türü: ${mimeType}. Sadece JPEG, PNG ve WebP kabul edilir.` 
            };
        }

        return { valid: true };
    }

    /**
     * Validates file size
     */
    function validateFileSize(file) {
        if (file.size === 0) {
            return { valid: false, message: 'Dosya boş.' };
        }

        if (file.size > MAX_FILE_SIZE_BYTES) {
            const fileSizeMB = (file.size / (1024 * 1024)).toFixed(2);
            return { 
                valid: false, 
                message: `Dosya boyutu çok büyük (${fileSizeMB}MB). Maksimum ${MAX_FILE_SIZE_MB}MB yükleyebilirsiniz.` 
            };
        }

        if (file.size < 1024) { // Less than 1KB is suspicious
            return { 
                valid: false, 
                message: 'Dosya boyutu çok küçük. Dosya bozuk olabilir.' 
            };
        }

        return { valid: true };
    }

    /**
     * Validates file signature (magic bytes) using FileReader
     */
    function validateFileSignature(file) {
        return new Promise((resolve) => {
            const reader = new FileReader();
            
            reader.onload = function(e) {
                try {
                    const buffer = e.target?.result;
                    if (!buffer || !(buffer instanceof ArrayBuffer)) {
                        resolve({ valid: false, message: 'Dosya okunamadı.' });
                        return;
                    }

                    const bytes = new Uint8Array(buffer);
                    const mimeType = file.type?.toLowerCase() || '';
                    
                    // Check JPEG signature
                    if (mimeType === 'image/jpeg') {
                        if (bytes.length < 3 || 
                            bytes[0] !== 0xFF || 
                            bytes[1] !== 0xD8 || 
                            bytes[2] !== 0xFF) {
                            resolve({ 
                                valid: false, 
                                message: 'Geçersiz JPEG dosyası. Dosya bozuk veya uzantısı yanlış.' 
                            });
                            return;
                        }
                    }
                    // Check PNG signature
                    else if (mimeType === 'image/png') {
                        if (bytes.length < 4 || 
                            bytes[0] !== 0x89 || 
                            bytes[1] !== 0x50 || 
                            bytes[2] !== 0x4E || 
                            bytes[3] !== 0x47) {
                            resolve({ 
                                valid: false, 
                                message: 'Geçersiz PNG dosyası. Dosya bozuk veya uzantısı yanlış.' 
                            });
                            return;
                        }
                    }
                    // Check WebP signature (RIFF + WEBP)
                    else if (mimeType === 'image/webp') {
                        if (bytes.length < 12 || 
                            bytes[0] !== 0x52 || bytes[1] !== 0x49 || 
                            bytes[2] !== 0x46 || bytes[3] !== 0x46 ||
                            bytes[8] !== 0x57 || bytes[9] !== 0x45 || 
                            bytes[10] !== 0x42 || bytes[11] !== 0x50) {
                            resolve({ 
                                valid: false, 
                                message: 'Geçersiz WebP dosyası. Dosya bozuk olabilir.' 
                            });
                            return;
                        }
                    }

                    resolve({ valid: true });
                } catch (ex) {
                    resolve({ valid: false, message: 'Dosya imzası doğrulanamadı.' });
                }
            };

            reader.onerror = function() {
                resolve({ valid: false, message: 'Dosya okuma hatası.' });
            };

            // Read first 12 bytes for signature check
            const blob = file.slice(0, 12);
            reader.readAsArrayBuffer(blob);
        });
    }

    /**
     * Validates image integrity by loading it
     */
    function validateImageIntegrity(file) {
        return new Promise((resolve) => {
            const img = new Image();
            const url = URL.createObjectURL(file);

            img.onload = function() {
                URL.revokeObjectURL(url);
                
                // Check dimensions
                if (img.width < 1 || img.height < 1) {
                    resolve({ valid: false, message: 'Geçersiz görsel boyutları.' });
                    return;
                }

                if (img.width > 10000 || img.height > 10000) {
                    resolve({ 
                        valid: false, 
                        message: 'Görsel boyutları çok büyük. Maksimum 10000x10000 piksel.' 
                    });
                    return;
                }

                resolve({ 
                    valid: true,
                    width: img.width,
                    height: img.height
                });
            };

            img.onerror = function() {
                URL.revokeObjectURL(url);
                resolve({ 
                    valid: false, 
                    message: 'Dosya geçerli bir görsel değil. Dosya bozuk veya formatı desteklenmiyor.' 
                });
            };

            img.src = url;
        });
    }

    /**
     * Comprehensive file validation
     * @param {File} file - The file to validate
     * @returns {Promise<{valid: boolean, message?: string, details?: object}>}
     */
    async function validate(file) {
        // Step 1: Extension check
        const extResult = validateExtension(file);
        if (!extResult.valid) {
            return extResult;
        }

        // Step 2: MIME type check
        const mimeResult = validateMimeType(file);
        if (!mimeResult.valid) {
            return mimeResult;
        }

        // Step 3: File size check
        const sizeResult = validateFileSize(file);
        if (!sizeResult.valid) {
            return sizeResult;
        }

        // Step 4: File signature check (magic bytes)
        const sigResult = await validateFileSignature(file);
        if (!sigResult.valid) {
            return sigResult;
        }

        // Step 5: Image integrity check
        const integrityResult = await validateImageIntegrity(file);
        if (!integrityResult.valid) {
            return integrityResult;
        }

        return { 
            valid: true, 
            details: { 
                width: integrityResult.width, 
                height: integrityResult.height 
            } 
        };
    }

    /**
     * Format file size for display
     */
    function formatFileSize(bytes) {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
    }

    return {
        validate,
        validateExtension,
        validateMimeType,
        validateFileSize,
        validateFileSignature,
        validateImageIntegrity,
        formatFileSize,
        ALLOWED_EXTENSIONS,
        ALLOWED_MIME_TYPES,
        MAX_FILE_SIZE_MB
    };
})();
