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
window.imgUrl = (function() {
    const apiBase = "https://localhost:7293";
    return function (url, fallback) {
        if (!url) return fallback || "";
        if (url.startsWith("http")) return url;
        if (url.startsWith("/")) return apiBase + url;
        return url;
    };
})();

// ── Theme Toggle Logic ──────────────────────────────────────────
document.addEventListener("DOMContentLoaded", function() {
    const themeBtn = document.getElementById("themeToggleBtn");
    const themeIcon = document.getElementById("themeToggleIcon");
    const htmlEl = document.documentElement;

    function setTheme(theme) {
        if (theme === "dark") {
            htmlEl.setAttribute("data-theme", "dark");
            if (themeIcon) themeIcon.className = "bi bi-sun";
        } else {
            htmlEl.removeAttribute("data-theme");
            if (themeIcon) themeIcon.className = "bi bi-moon-stars";
        }
        localStorage.setItem("appTheme", theme);
    }

    // Initialize theme (default to dark)
    const savedTheme = localStorage.getItem("appTheme") || "dark";
    setTheme(savedTheme);

    if (themeBtn) {
        themeBtn.addEventListener("click", function() {
            const currentTheme = htmlEl.hasAttribute("data-theme") ? "dark" : "light";
            setTheme(currentTheme === "dark" ? "light" : "dark");
        });
    }
});
