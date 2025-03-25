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

/**
 * Prints the CBS table from the PublisherManager
 * 
 */
document.getElementById("printCBSButton").addEventListener("click", function () {
    const tableHtml = document.getElementById("serviceGroupsTable").outerHTML;
    const newWindow = window.open("", "_blank");
    newWindow.document.write(`
            <html>
            <head>
                <title>Print Service Groups</title>
                <style>
                @media print {
                    @page { margin: 0; } /* Removes headers & footers */
                    body { 
                        font-family: Arial, sans-serif; 
                        padding: 20px; 
                        margin: 0; 
                        text-align: center;
                    }
                    .dynamic-groups-table { 
                        width: 100%; 
                        border-collapse: collapse; 
                        margin-top: 20px;
                    }
                    .dynamic-groups-table th, .dynamic-groups-table td { 
                        border: 1px solid black; 
                        padding: 8px; 
                        text-align: left; 
                    }
                    .btn-print {
                        display: none !important; /* Hide print button when printing */
                    }
                }
                body { font-family: Arial, sans-serif; padding: 20px; }
                .dynamic-groups-table { width: 100%; border-collapse: collapse; }
                .dynamic-groups-table th, .dynamic-groups-table td { border: 1px solid black; padding: 8px; text-align: left; }
                .btn-print { background-color: black; color: white; padding: 10px; margin-top: 10px; cursor: pointer; }
            </style>
            </head>
            <body>
                <h2><center>Service Groups</center></h2>
                ${tableHtml}
                <br>
                <button onclick="window.print()" class="btn-print">Print</button>
            </body>
            </html>
        `);
    newWindow.document.close();
});

/**
 * Prints the CBS table from the PublisherManager
 * 
 */
document.getElementById("printPubButton").addEventListener("click", function () {
    const tableHtml = document.getElementById("all-publishers-table").outerHTML;
    const newWindow = window.open("", "_blank");
    newWindow.document.write(`
            <html>
            <head>
                <title>Print All Publishers</title>
                <style>
                @media print {
                    @page { margin: 0; } /* Removes headers & footers */
                    body { 
                        font-family: Arial, sans-serif; 
                        padding: 20px; 
                        margin: 0; 
                        text-align: center;
                    }
                    .dynamic-groups-table { 
                        width: 100%; 
                        border-collapse: collapse; 
                        margin-top: 20px;
                    }
                    .dynamic-groups-table th, .dynamic-groups-table td { 
                        border: 1px solid black; 
                        padding: 8px; 
                        text-align: left; 
                    }
                    .btn-print {
                        display: none !important; /* Hide print button when printing */
                    }
                }
                body { font-family: Arial, sans-serif; padding: 20px; }
                .dynamic-groups-table { width: 100%; border-collapse: collapse; }
                .dynamic-groups-table th, .dynamic-groups-table td { border: 1px solid black; padding: 8px; text-align: left; }
                .btn-print { background-color: black; color: white; padding: 10px; margin-top: 10px; cursor: pointer; }
            </style>
            </head>
            <body>
                <h2><center>Service Groups</center></h2>
                ${tableHtml}
                <br>
                <button onclick="window.print()" class="btn-print">Print</button>
            </body>
            </html>
        `);
    newWindow.document.close();
});

/**
 * Downloads table as Word document from the PublisherManager
 * 
 */
document.getElementById("downloadWordButton").addEventListener("click", function () {
    const tableHtml = document.getElementById("serviceGroupsTable").outerHTML;

    fetch("/PrintView?handler=DownloadWord", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ html: tableHtml })
    })
        .then(response => response.blob())
        .then(blob => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement("a");
            a.href = url;
            a.download = "ServiceGroups.docx";
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
        })
        .catch(error => console.error("Error generating Word file:", error));
});

/**
 * Hides/shows the CBS checkboxes dynamically
 * 
 */
document.querySelectorAll(".privilege-dropdown").forEach(dropdown => {
    dropdown.addEventListener("change", function () {
        let rowId = this.getAttribute("data-row");
        let cbsOptions = document.getElementById("cbs-options-" + rowId);

        if (this.value === "Elder" || this.value === "Servant") {
            cbsOptions.style.display = "block";
        } else {
            cbsOptions.style.display = "none";
            cbsOptions.querySelectorAll("input").forEach(checkbox => checkbox.checked = false);
        }
    });
});