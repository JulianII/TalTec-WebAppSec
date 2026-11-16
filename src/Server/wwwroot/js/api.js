window.secureApiRequest = async function (url, method = 'GET', data = null) {
    const options = {
        method,
        credentials: 'include',
        cache: 'no-store',
        headers: {}
    };

    if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS') {
        try {
            const tokenResponse = await fetch('/api/auth/csrf-token', {
                credentials: 'include',
                cache: 'no-store'
            });
            if (!tokenResponse.ok) {
                return { ok: false, status: tokenResponse.status, data: null };
            }

            const tokenData = await tokenResponse.json();
            if (!tokenData.token) {
                return { ok: false, status: 0, data: null };
            }
            options.headers['X-CSRF-TOKEN'] = tokenData.token;
        } catch {
            return { ok: false, status: 0, data: null };
        }
    }

    if (data !== null) {
        options.headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(url, options);
        const text = await response.text();
        let responseData = null;

        if (text) {
            try {
                responseData = JSON.parse(text);
            } catch {
                responseData = { message: text };
            }
        }

        return { ok: response.ok, status: response.status, data: responseData };
    } catch {
        return { ok: false, status: 0, data: null };
    }
};
