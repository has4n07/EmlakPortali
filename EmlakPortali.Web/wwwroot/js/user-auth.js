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

    function login(email, password) {
        return new Promise((resolve, reject) => {
            email = (email ?? "").toString().trim();
            password = (password ?? "").toString();
            if (!email || !password) {
                reject(new Error("E-posta ve şifre zorunludur."));
                return;
            }

            $.ajax({
                url: apiBase + "/api/auth/login",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ email, password }),
                success: function (json, textStatus, jqXHR) {
                    if (!json?.status && !json?.Status) {
                        reject(new Error(json?.message || json?.Message || "Giriş başarısız."));
                        return;
                    }
                    const token = json.data?.accessToken || json.Data?.AccessToken;
                    if (!token) {
                        reject(new Error("Token alınamadı."));
                        return;
                    }
                    setToken(token);

                    // kullanıcı bilgisi
                    $.ajax({
                        url: apiBase + "/api/auth/me",
                        type: "GET",
                        headers: { "Authorization": "Bearer " + token },
                        success: function (meJson) {
                            const u = meJson?.data || meJson?.Data;
                            if (u) setUserInfo(u);
                            resolve(json);
                        },
                        error: function (jqXHR) {
                            resolve(json); // Token alındı, profil bilgisi alınamasa da devam et
                        }
                    });
                },
                error: function (jqXHR) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    reject(new Error(json?.message || json?.Message || jqXHR.responseText || "Giriş başarısız."));
                }
            });
        });
    }

    function register(fullName, email, password) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: apiBase + "/api/auth/register",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ fullName, email, password }),
                success: function (json, textStatus, jqXHR) {
                    if (!json?.status && !json?.Status) {
                        reject(new Error(json?.message || json?.Message || "Kayıt başarısız."));
                        return;
                    }
                    resolve(json);
                },
                error: function (jqXHR) {
                    let json = null;
                    try { json = jqXHR.responseText ? JSON.parse(jqXHR.responseText) : null; } catch { }
                    reject(new Error(json?.message || json?.Message || jqXHR.responseText || "Kayıt başarısız."));
                }
            });
        });
    }

    return { getToken, setToken, clear, setUserInfo, getUserInfo, login, register, apiBase };
})();