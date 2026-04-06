window.UserAuth = (function () {
    const tokenKey = "emlak_user_jwt";
    const userKey = "emlak_user_info";
    const apiBase = "https://localhost:7293";

    function getToken() {
        return localStorage.getItem(tokenKey);
    }

    function setToken(token) {
        localStorage.setItem(tokenKey, token);
    }

    function clear() {
        localStorage.removeItem(tokenKey);
        localStorage.removeItem(userKey);
    }

    function setUserInfo(user) {
        localStorage.setItem(userKey, JSON.stringify(user));
    }

    function getUserInfo() {
        const raw = localStorage.getItem(userKey);
        if (!raw) return null;
        try { return JSON.parse(raw); } catch { return null; }
    }

    async function login(email, password) {
        const res = await fetch(apiBase + "/api/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ email, password })
        });
        const text = await res.text();
        const json = text ? JSON.parse(text) : null;
        if (!res.ok || !json?.status && !json?.Status) {
            throw new Error(json?.message || json?.Message || text || "Giriş başarısız.");
        }
        const token = json.data?.accessToken || json.Data?.AccessToken;
        if (!token) throw new Error("Token alınamadı.");
        setToken(token);

        // kullanıcı bilgisi
        const meRes = await fetch(apiBase + "/api/auth/me", {
            headers: { "Authorization": "Bearer " + token }
        });
        const meText = await meRes.text();
        const meJson = meText ? JSON.parse(meText) : null;
        const u = meJson?.data || meJson?.Data;
        if (u) setUserInfo(u);

        return json;
    }

    async function register(fullName, email, password) {
        const res = await fetch(apiBase + "/api/auth/register", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ fullName, email, password })
        });
        const text = await res.text();
        const json = text ? JSON.parse(text) : null;
        if (!res.ok || !json?.status && !json?.Status) {
            throw new Error(json?.message || json?.Message || text || "Kayıt başarısız.");
        }
        return json;
    }

    return { getToken, setToken, clear, setUserInfo, getUserInfo, login, register, apiBase };
})();