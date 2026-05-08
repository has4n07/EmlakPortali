window.AdminApi = (function () {
    // API base URL (Swagger portu)
    const baseUrl = "https://localhost:7293";

    function request(method, url, body) {
        const token = window.AdminAuth?.getToken?.() || localStorage.getItem("emlak_admin_jwt") || localStorage.getItem("token");
        const headers = {
            "Content-Type": "application/json"
        };
        if (token) headers["Authorization"] = "Bearer " + token;

        return new Promise((resolve, reject) => {
            $.ajax({
                url: baseUrl + url,
                type: method,
                headers: headers,
                data: body ? JSON.stringify(body) : undefined,
                success: function (data, textStatus, jqXHR) {
                    resolve(data);
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    const msg = json?.message || json?.Message || jqXHR.responseText || (jqXHR.status + " " + jqXHR.statusText);
                    reject(new Error(msg));
                }
            });
        });
    }

    async function uploadFile(url, file) {
        // Client-side validation before upload
        if (window.ImageValidator) {
            try {
                const validation = await window.ImageValidator.validate(file);
                if (!validation.valid) {
                    throw new Error(validation.message || "Dosya doğrulama hatası.");
                }
            } catch (validationError) {
                throw new Error("Dosya doğrulama hatası: " + (validationError?.message || validationError));
            }
        }

        const token = window.AdminAuth?.getToken?.() || localStorage.getItem("emlak_admin_jwt") || localStorage.getItem("token");

        return new Promise((resolve, reject) => {
            const formData = new FormData();
            formData.append("file", file);

            $.ajax({
                url: baseUrl + url,
                type: "POST",
                headers: token ? { "Authorization": "Bearer " + token } : {},
                data: formData,
                processData: false,
                contentType: false,
                success: function (data) {
                    resolve(data);
                },
                error: function (jqXHR) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    const msg = json?.message || json?.Message || jqXHR.responseText || (jqXHR.status + " " + jqXHR.statusText);
                    reject(new Error(msg));
                }
            });
        });
    }


    return {
        get: (url) => request("GET", url),
        post: (url, body) => request("POST", url, body),
        put: (url, body) => request("PUT", url, body),
        del: (url) => request("DELETE", url),
        delete: (url) => request("DELETE", url),
        upload: (url, file) => uploadFile(url, file)
    };
})();



// ── AdminConfirm: native confirm() yerine kullanilir ─────────────
window.AdminConfirm = function(options) {
    return new Promise(function(resolve) {
        var modal  = document.getElementById('adminConfirmModal');
        var title  = document.getElementById('acm-title');
        var msg    = document.getElementById('acm-msg');
        var icon   = document.getElementById('acm-icon');
        var btnOk  = document.getElementById('acm-ok');
        var btnCan = document.getElementById('acm-cancel');

        if (!modal) { resolve(window.confirm(options.message || 'Emin misiniz?')); return; }

        title.textContent  = options.title   || 'Emin misiniz?';
        msg.textContent    = options.message || '';
        icon.innerHTML     = options.icon    || '&#x1F5D1;';
        btnOk.textContent  = options.okText  || 'Evet, Sil';
        btnCan.textContent = options.cancelText || 'Vazgec';
        btnOk.style.background = options.okBg || 'linear-gradient(135deg,#ef4444,#dc2626)';

        modal.style.display = 'flex';
        // tekrar animasyon icin ic kutuyu yeniden clonla
        var inner = modal.querySelector('div');
        inner.style.animation = 'none';
        void inner.offsetWidth;
        inner.style.animation = '';

        function cleanup() {
            modal.style.display = 'none';
            btnOk.removeEventListener('click', onOk);
            btnCan.removeEventListener('click', onCan);
            document.removeEventListener('keydown', onKey);
        }
        function onOk()  { cleanup(); resolve(true);  }
        function onCan() { cleanup(); resolve(false); }
        function onKey(e) { if (e.key === 'Escape') { cleanup(); resolve(false); } }

        btnOk.addEventListener('click', onOk);
        btnCan.addEventListener('click', onCan);
        document.addEventListener('keydown', onKey);
    });
};