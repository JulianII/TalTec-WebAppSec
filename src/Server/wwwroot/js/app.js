console.log("app.js loaded");

// API connection test
fetch("/api/test")
    .then(response => response.json())
    .then(data => {
        const apiStatus = document.getElementById("api-status");

        if (apiStatus) {
            apiStatus.textContent = data.message;
        }
    })
    .catch(error => {
        console.error("API test failed:", error);
    });


// Login
const loginForm = document.getElementById("login-form");

if (loginForm) {
    loginForm.addEventListener("submit", async (event) => {
        event.preventDefault();

        const username = document.getElementById("username").value;
        const password = document.getElementById("password").value;
        const loginStatus = document.getElementById("login-status");

        try {
            const response = await fetch("/api/auth/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    username: username,
                    password: password
                })
            });

            if (response.ok) {
                console.log("Login successful");

                if (loginStatus) {
                    loginStatus.textContent = "Login successful!";
                }
            } else {
                console.log("Login failed:", response.status);

                if (loginStatus) {
                    loginStatus.textContent = `Login failed (${response.status})`;
                }
            }
        } catch (error) {
            console.error("Login request failed:", error);

            if (loginStatus) {
                loginStatus.textContent = "Login request failed.";
            }
        }
    });
}


// Registration
const registerForm = document.getElementById("register-form");

if (registerForm) {
    registerForm.addEventListener("submit", async (event) => {
        event.preventDefault();

        const username = document.getElementById("register-username").value;
        const password = document.getElementById("register-password").value;
        const verifyPassword = document.getElementById("register-verify-password").value;
        const registerStatus = document.getElementById("register-status");

        try {
            const response = await fetch("/api/auth/register", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    username: username,
                    password: password,
                    verifyPassword: verifyPassword
                })
            });

            if (response.ok) {
                console.log("Registration successful");

                if (registerStatus) {
                    registerStatus.textContent = "Registration successful!";
                }
            } else {
                console.log("Registration failed:", response.status);

                if (registerStatus) {
                    registerStatus.textContent =
                        `Registration failed (${response.status})`;
                }
            }
        } catch (error) {
            console.error("Registration request failed:", error);

            if (registerStatus) {
                registerStatus.textContent = "Registration request failed.";
            }
        }
    });
}