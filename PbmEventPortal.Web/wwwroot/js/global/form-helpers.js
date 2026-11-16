(() => {
    const boundedNumberMessageTimers = new WeakMap();

    function setPasswordVisibility(input, button, shouldShow) {
        if (!input || !button) return;

        input.type = shouldShow ? "text" : "password";
        button.classList.toggle("is-revealing", shouldShow);
        button.setAttribute("aria-label", shouldShow ? "Release to hide password" : "Hold to show password");

        const icon = button.querySelector("[data-password-toggle-icon]");
        if (icon) {
            icon.classList.toggle("bi-eye", shouldShow);
            icon.classList.toggle("bi-eye-slash", !shouldShow);
        }
    }

    function setupPasswordRevealButtons() {
        document.querySelectorAll("[data-password-toggle-for]").forEach((button) => {
            const input = document.getElementById(button.getAttribute("data-password-toggle-for"));
            if (!input || button.dataset.passwordRevealEnhanced === "true") return;

            button.dataset.passwordRevealEnhanced = "true";
            button.addEventListener("pointerdown", (event) => {
                event.preventDefault();
                button.setPointerCapture?.(event.pointerId);
                setPasswordVisibility(input, button, true);
            });
            button.addEventListener("pointerup", () => setPasswordVisibility(input, button, false));
            button.addEventListener("pointercancel", () => setPasswordVisibility(input, button, false));
            button.addEventListener("pointerleave", () => setPasswordVisibility(input, button, false));
            button.addEventListener("blur", () => setPasswordVisibility(input, button, false));
            button.addEventListener("contextmenu", (event) => event.preventDefault());
        });
    }

    function clearValidationMessages(form) {
        if (!form) return;

        form.querySelectorAll(".text-danger").forEach((element) => element.textContent = "");
        form.querySelectorAll(".input-validation-error").forEach((element) => element.classList.remove("input-validation-error"));
        form.querySelectorAll(".field-validation-error").forEach((element) => {
            element.classList.remove("field-validation-error");
            element.classList.add("field-validation-valid");
        });
        form.querySelectorAll(".validation-summary-errors").forEach((element) => {
            element.classList.remove("validation-summary-errors");
            element.classList.add("validation-summary-valid");
            element.innerHTML = "";
        });
    }

    function createDirtyTracker(options) {
        const fields = options?.fields ?? [];

        function readFieldValue(field) {
            const element = document.getElementById(field.id);
            if (!element) return "";

            let value = "";
            if (field.type === "checkbox") {
                value = element.checked ? "true" : "false";
            } else {
                value = element.value ?? "";
            }

            return field.trim ? String(value).trim() : String(value);
        }

        function getSnapshot() {
            const snapshot = {};
            fields.forEach((field) => {
                snapshot[field.key ?? field.id] = readFieldValue(field);
            });
            return JSON.stringify(snapshot);
        }

        let cleanSnapshot = getSnapshot();

        return {
            getSnapshot,
            markClean: () => {
                cleanSnapshot = getSnapshot();
            },
            isDirty: () => getSnapshot() !== cleanSnapshot
        };
    }

    function getNumberAttribute(input, attributeName) {
        const rawValue = input.getAttribute(attributeName);
        if (rawValue === null || rawValue === "") return null;

        const numericValue = Number(rawValue);
        return Number.isNaN(numericValue) ? null : numericValue;
    }

    function formatLimitValue(value) {
        return Number(value).toString();
    }

    function getBoundedNumberUnit(input) {
        return input.dataset.boundedNumberUnit || "";
    }

    function buildBoundedNumberMessage(prefix, value, input) {
        const unit = getBoundedNumberUnit(input);
        return unit ? `${prefix} ${formatLimitValue(value)} ${unit} only` : `${prefix} ${formatLimitValue(value)} only`;
    }

    function findOrCreateBoundedNumberMessage(input) {
        let messageElement = input.parentElement?.querySelector("[data-bounded-number-message='true']");
        if (messageElement) return messageElement;

        messageElement = document.createElement("div");
        messageElement.className = "form-text text-warning small d-none";
        messageElement.setAttribute("data-bounded-number-message", "true");
        input.insertAdjacentElement("afterend", messageElement);
        return messageElement;
    }

    function showBoundedNumberMessage(input, message) {
        if (!message) return;

        const messageElement = findOrCreateBoundedNumberMessage(input);
        messageElement.textContent = message;
        messageElement.classList.remove("d-none");

        const existingTimer = boundedNumberMessageTimers.get(input);
        if (existingTimer) window.clearTimeout(existingTimer);

        const nextTimer = window.setTimeout(() => {
            messageElement.classList.add("d-none");
        }, 1800);
        boundedNumberMessageTimers.set(input, nextTimer);
    }

    function formatBoundedNumber(value, step, wholeNumber) {
        if (wholeNumber) return Math.floor(value).toString();

        const decimals = step && step.includes(".")
            ? Math.min(step.split(".")[1].length, 6)
            : 0;

        if (decimals === 0) return Math.round(value).toString();

        const rounded = Number(value.toFixed(decimals));
        return rounded.toString();
    }

    function sanitizeBoundedNumberInput(input, shouldCleanInvalid = true) {
        if (!input) return;

        const rawValue = input.value.trim().replace(",", ".");
        if (rawValue === "" || rawValue === "-" || rawValue === "." || rawValue === "-.") return;

        const numericValue = Number(rawValue);
        if (Number.isNaN(numericValue)) {
            if (shouldCleanInvalid) input.value = "";
            return;
        }

        const minValue = getNumberAttribute(input, "min");
        const maxValue = getNumberAttribute(input, "max");
        const wholeNumber = input.dataset.boundedNumberWhole === "true";
        let boundedValue = numericValue;
        let message = "";

        if (minValue !== null && numericValue < minValue) {
            boundedValue = minValue;
            message = buildBoundedNumberMessage("Minimum", minValue, input);
        } else if (maxValue !== null && numericValue > maxValue) {
            boundedValue = maxValue;
            message = buildBoundedNumberMessage("Maximum", maxValue, input);
        } else if (wholeNumber && !Number.isInteger(numericValue)) {
            boundedValue = Math.floor(numericValue);
            message = input.dataset.boundedNumberWholeMessage || "Whole numbers only";
        }

        if (boundedValue !== numericValue || shouldCleanInvalid) {
            input.value = formatBoundedNumber(boundedValue, input.getAttribute("step") ?? "1", wholeNumber);
        }

        showBoundedNumberMessage(input, message);
    }

    function setupBoundedNumberInputs(root = document) {
        root.querySelectorAll("input[data-bounded-number-input='true']").forEach((input) => {
            if (!input || input.dataset.boundedNumberEnhanced === "true") return;

            input.dataset.boundedNumberEnhanced = "true";
            input.addEventListener("input", () => sanitizeBoundedNumberInput(input, false));
            input.addEventListener("blur", () => sanitizeBoundedNumberInput(input, true));
        });
    }

    function setupSearchableSelects(root = document) {
        root.querySelectorAll("select[data-searchable-select='true']").forEach((select) => {
            if (!select || select.dataset.searchableEnhanced === "true") return;

            select.dataset.searchableEnhanced = "true";
            select.classList.add("app-searchable-select-native");

            const wrapper = document.createElement("div");
            wrapper.className = "dropdown app-searchable-select";

            const button = document.createElement("button");
            button.type = "button";
            button.className = "form-select text-start app-searchable-select-button";
            button.setAttribute("data-bs-toggle", "dropdown");
            button.setAttribute("aria-expanded", "false");

            const menu = document.createElement("div");
            menu.className = "dropdown-menu app-searchable-select-menu p-2";

            const searchInput = document.createElement("input");
            searchInput.type = "search";
            searchInput.className = "form-control form-control-sm app-searchable-select-search";
            searchInput.placeholder = select.dataset.searchablePlaceholder || "Search...";
            searchInput.autocomplete = "off";

            const optionsContainer = document.createElement("div");
            optionsContainer.className = "app-searchable-select-options mt-2";

            menu.appendChild(searchInput);
            menu.appendChild(optionsContainer);
            wrapper.appendChild(button);
            wrapper.appendChild(menu);
            select.insertAdjacentElement("afterend", wrapper);

            function getSelectedText() {
                const selectedOption = Array.from(select.options).find((option) => option.value === select.value);
                return selectedOption?.textContent?.trim() || select.dataset.placeholder || "Select...";
            }

            function updateButtonText() {
                button.textContent = getSelectedText();
                button.classList.toggle("text-muted", !select.value);
                button.disabled = select.disabled;
            }

            function renderOptions() {
                const filter = searchInput.value.trim().toLowerCase();
                optionsContainer.innerHTML = "";

                const options = Array.from(select.options)
                    .filter((option) => !option.hidden && !option.disabled)
                    .filter((option) => option.textContent.toLowerCase().includes(filter));

                if (options.length === 0) {
                    const empty = document.createElement("div");
                    empty.className = "dropdown-item text-muted small";
                    empty.textContent = "No matches found";
                    optionsContainer.appendChild(empty);
                    return;
                }

                options.forEach((option) => {
                    const item = document.createElement("button");
                    item.type = "button";
                    item.className = "dropdown-item app-searchable-select-option";
                    item.textContent = option.textContent;
                    item.classList.toggle("active", option.value === select.value);
                    item.addEventListener("click", () => {
                        select.value = option.value;
                        select.dispatchEvent(new Event("change", { bubbles: true }));
                        updateButtonText();
                        bootstrap.Dropdown.getOrCreateInstance(button).hide();
                    });
                    optionsContainer.appendChild(item);
                });
            }

            button.addEventListener("click", () => {
                searchInput.value = "";
                renderOptions();
                setTimeout(() => searchInput.focus(), 0);
            });
            searchInput.addEventListener("click", (event) => event.stopPropagation());
            searchInput.addEventListener("keydown", (event) => event.stopPropagation());
            searchInput.addEventListener("input", renderOptions);
            select.addEventListener("change", () => {
                updateButtonText();
                renderOptions();
            });

            updateButtonText();
            renderOptions();
        });
    }

    window.FormHelpers = {
        setupPasswordRevealButtons,
        clearValidationMessages,
        createDirtyTracker,
        sanitizeBoundedNumberInput,
        setupBoundedNumberInputs,
        setupSearchableSelects
    };
})();
