const UserAuth = (function () {
    const baseUrl = "https://localhost:7293"; // API url

    function login(email, password) {
        return new Promise((resolve, reject) => {
            email = (email ?? "").toString().trim();
            password = (password ?? "").toString();
            if (!email || !password) {
                reject(new Error("E-posta ve şifre zorunludur."));
                return;
            }

            $.ajax({
                url: baseUrl + "/api/auth/login",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ email, password }),
                success: function (json, textStatus, jqXHR) {
                    if (!json?.status && !json?.Status) {
                        reject(new Error(json?.message || json?.Message || "Login failed"));
                        return;
                    }
                    const token = json?.data?.accessToken || json?.Data?.AccessToken || json?.data?.token || json?.Data?.Token;
                    if (token) {
                        localStorage.setItem("token", token);
                    }
                    resolve(json);
                },
                error: function (jqXHR) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    reject(new Error(json?.message || json?.Message || jqXHR.responseText || "Login failed"));
                }
            });
        });
    }

    function register(fullName, email, password) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: baseUrl + "/api/auth/register",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ fullName, email, password }),
                success: function (json, textStatus, jqXHR) {
                    if (!json?.status && !json?.Status) {
                        reject(new Error(json?.message || json?.Message || "Registration failed"));
                        return;
                    }
                    resolve(json);
                },
                error: function (jqXHR) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    reject(new Error(json?.message || json?.Message || jqXHR.responseText || "Registration failed"));
                }
            });
        });
    }

    function getToken() {
        return localStorage.getItem("token");
    }

    function clear() {
        localStorage.removeItem("token");
    }

    return { login, register, getToken, clear, apiBase: baseUrl };
})();

window.AppNotify = (function () {
    function getContainer() {
        let container = document.getElementById("appNotifyContainer");
        if (!container) {
            container = document.createElement("div");
            container.id = "appNotifyContainer";
            container.className = "app-notify-container";
            document.body.appendChild(container);
        }
        return container;
    }

    function show(message, type = "info", timeout = 3200) {
        const text = (message ?? "").toString().trim();
        if (!text) return;

        const item = document.createElement("div");
        item.className = `app-notify app-notify-${type}`;
        item.innerHTML = `
            <button type="button" class="app-notify-close" aria-label="Kapat">&times;</button>
            <div class="app-notify-body">${text}</div>
        `;

        const container = getContainer();
        container.appendChild(item);
        requestAnimationFrame(() => item.classList.add("show"));

        const remove = () => {
            item.classList.remove("show");
            setTimeout(() => item.remove(), 180);
        };

        item.querySelector(".app-notify-close")?.addEventListener("click", remove);

        if (timeout > 0) {
            setTimeout(remove, timeout);
        }
    }

    return {
        show,
        info: (message, timeout) => show(message, "info", timeout),
        success: (message, timeout) => show(message, "success", timeout),
        warning: (message, timeout) => show(message, "warning", timeout),
        error: (message, timeout) => show(message, "error", timeout)
    };
})();

// ── imgUrl: /uploads/... URL'lerini API base URL ile tamamlar ──────────────
// API ve Web farklı portlarda çalıştığı için relative upload URL'leri
// Web tarafında kırık olur. Bu helper otomatik düzeltir.
window.imgUrl = function (url, fallback) {
    if (!url) return fallback || "";
    // Zaten absolute URL ise olduğu gibi döndür
    if (url.startsWith("http://") || url.startsWith("https://")) return url;
    // /uploads/ ile başlıyorsa API base URL ekle
    if (url.startsWith("/uploads/") || url.startsWith("/images/")) {
        const apiBase = (window.UserAuth && typeof window.UserAuth.getBaseUrl === "function")
            ? window.UserAuth.getBaseUrl()
            : "https://localhost:7293";
        return apiBase + url;
    }
    return url;
};
