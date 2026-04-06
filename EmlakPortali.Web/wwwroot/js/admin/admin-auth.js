window.AdminAuth = (function () {
    const tokenKey = "emlak_admin_jwt";
    const userKey = "emlak_admin_user";

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
        const res = await window.AdminApi.post("/api/auth/login", { email, password });
        if (res.status === false || res.Status === false) {
            throw new Error(res.message || res.Message || "Giriş başarısız.");
        }
        const token = res?.data?.accessToken || res?.Data?.AccessToken;
        if (!token) throw new Error("Token alınamadı.");
        setToken(token);

        try {
            const me = await window.AdminApi.get("/api/auth/me");
            const u = me?.data || me?.Data;
            if (u) setUserInfo(u);
        } catch { /* ignore */ }

        return res;
    }

    return { getToken, setToken, clear, login, setUserInfo, getUserInfo };
})();

$(function () {
    $("#btnAdminLogout").on("click", function () {
        window.AdminAuth.clear();
        window.location.href = "/Admin/Account/Login";
    });
});

