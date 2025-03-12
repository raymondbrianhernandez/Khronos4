/**
 * Initializes theme toggle functionality on page load.
 * Retrieves saved theme from localStorage and applies it.
 */
document.addEventListener("DOMContentLoaded", function () {
    const toggleButton = document.getElementById("toggleTheme");
    const body = document.body;
    const navbar = document.querySelector(".navbar");
    const dropdownMenus = document.querySelectorAll(".dropdown-menu");

    // Check saved theme from localStorage
    const currentTheme = localStorage.getItem("theme");
    if (currentTheme === "dark") {
        body.classList.add("dark-mode");
        navbar.classList.add("dark-mode");
        dropdownMenus.forEach(menu => menu.classList.add("dark-mode"));
        toggleButton.textContent = "☀️ Light Mode";
    }

    // Toggle Dark Mode
    toggleButton.addEventListener("click", function () {
        body.classList.toggle("dark-mode");
        navbar.classList.toggle("dark-mode");
        dropdownMenus.forEach(menu => menu.classList.toggle("dark-mode"));

        if (body.classList.contains("dark-mode")) {
            localStorage.setItem("theme", "dark");
            toggleButton.textContent = "☀️ Light Mode";
        } else {
            localStorage.setItem("theme", "light");
            toggleButton.textContent = "🌙 Dark Mode";
        }
    });
});


/**
 * Toggles password visibility for input fields.
 * 
 * @param {string} inputId - The ID of the password input field.
 * @param {string} iconId - The ID of the icon element that indicates visibility state.
 */
function togglePassword(inputId, iconId) {
    let passwordField = document.getElementById(inputId);
    let toggleIcon = document.getElementById(iconId);

    if (passwordField.type === "password") {
        passwordField.type = "text";
        toggleIcon.classList.remove("fa-eye");
        toggleIcon.classList.add("fa-eye-slash");
    } else {
        passwordField.type = "password";
        toggleIcon.classList.remove("fa-eye-slash");
        toggleIcon.classList.add("fa-eye");
    }
}


/**
 * Sanitizes phone number input by removing invalid characters and ensuring it starts with '+'.
 * 
 * @param {HTMLInputElement} input - The input field element containing the phone number.
 */
function sanitizePhone(input) {
    // Remove all characters that are not digits or '+'
    input.value = input.value.replace(/[^+\d]/g, '');

    /*if (!input.value.startsWith("+") && input.value.length > 0) {
        input.value = "+" + input.value;
    }*/

}
