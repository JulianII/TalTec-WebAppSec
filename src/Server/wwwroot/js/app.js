console.log("app.js loaded");

fetch("/api/test")
    .then(response => response.json())
    .then(data => {
        document.getElementById("api-status").textContent = data.message;
    });

const loginForm = document.getElementById("login-form");

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;

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

    console.log("Login status:", response.status);
});