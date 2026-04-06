window.AdminApi = (function () {
    // API base URL (Swagger portu)
    const baseUrl = "https://localhost:7293";

    async function request(method, url, body) {
        const token = window.AdminAuth?.getToken?.();
        const headers = {
            "Content-Type": "application/json"
        };
        if (token) headers["Authorization"] = "Bearer " + token;

        const res = await fetch(baseUrl + url, {
            method,
            headers,
            body: body ? JSON.stringify(body) : undefined
        });

        const text = await res.text();
        let json = null;
        try { json = text ? JSON.parse(text) : null; } catch { }

        if (!res.ok) {
            const msg = json?.message || json?.Message || text || (res.status + " " + res.statusText);
            throw new Error(msg);
        }
        return json;
    }

    async function uploadFile(url, file) {
        const token = window.AdminAuth?.getToken?.();
        const headers = {};
        if (token) headers["Authorization"] = "Bearer " + token;

        const formData = new FormData();
        formData.append("file", file);

        const res = await fetch(baseUrl + url, {
            method: "POST",
            headers,
            body: formData
        });

        const text = await res.text();
        let json = null;
        try { json = text ? JSON.parse(text) : null; } catch { }

        if (!res.ok) {
            const msg = json?.message || json?.Message || text || (res.status + " " + res.statusText);
            throw new Error(msg);
        }
        return json;
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

