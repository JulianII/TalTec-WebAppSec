fetch("/api/test")
    .then(response => response.json())
    .then(data => {
        document.getElementById("api-status").textContent = data.message;
    });