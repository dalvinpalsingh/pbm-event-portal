(() => {
    const countries = [
        ["Malaysia", "+60", "MY"], ["Singapore", "+65", "SG"], ["India", "+91", "IN"],
        ["Australia", "+61", "AU"], ["United Kingdom", "+44", "UK"], ["Sweden", "+46", "SE"],
        ["United States", "+1", "US"], ["Canada", "+1", "CA"], ["Indonesia", "+62", "ID"], ["Thailand", "+66", "TH"]
    ].map(([name, code, flag]) => ({ name, code, flag }));

    function onlyDigits(value) {
        return (value ?? "").replace(/\D/g, "");
    }

    function formatNric(value) {
        const digits = onlyDigits(value).slice(0, 12);
        if (digits.length <= 6) return digits;
        if (digits.length <= 8) return `${digits.slice(0, 6)}-${digits.slice(6)}`;
        return `${digits.slice(0, 6)}-${digits.slice(6, 8)}-${digits.slice(8)}`;
    }

    function inferDateFromNric(value) {
        const digits = onlyDigits(value);
        if (digits.length < 6) return null;

        const yy = Number(digits.slice(0, 2));
        const mm = Number(digits.slice(2, 4));
        const dd = Number(digits.slice(4, 6));
        const currentYear = new Date().getFullYear();
        const yyyy = yy <= currentYear % 100 ? 2000 + yy : 1900 + yy;
        const date = new Date(yyyy, mm - 1, dd);

        if (date.getFullYear() !== yyyy || date.getMonth() !== mm - 1 || date.getDate() !== dd) return null;
        return `${yyyy}-${String(mm).padStart(2, "0")}-${String(dd).padStart(2, "0")}`;
    }

    function inferGenderFromNric(value) {
        const digits = onlyDigits(value);
        if (digits.length < 12) return null;
        return Number(digits.slice(-1)) % 2 === 1 ? "M" : "F";
    }

    function setSectionInputsValidationEnabled(section, isEnabled) {
        section.querySelectorAll("input, select, textarea").forEach((input) => {
            if (isEnabled) {
                if (input.dataset.wasRequired === "true") input.required = true;
                return;
            }

            input.dataset.wasRequired = input.required ? "true" : input.dataset.wasRequired ?? "false";
            input.required = false;
            input.setCustomValidity("");
            input.classList.remove("input-validation-error");
        });
    }

    function setupIdentityFields(options) {
        const idTypeSelect = document.getElementById(options.idTypeSelectId);
        const nricInput = document.getElementById(options.nricInputId);
        const genderHidden = document.getElementById(options.genderHiddenId);
        const genderSelect = document.getElementById(options.genderSelectId);
        const dateOfBirthInput = document.getElementById(options.dateOfBirthInputId);
        const nricSections = document.querySelectorAll(options.nricSectionSelector ?? "[data-id-section='nric']");
        const passportSections = document.querySelectorAll(options.passportSectionSelector ?? "[data-id-section='passport']");
        const resetGenderValue = options.resetGenderValue ?? "M";
        const clearIncompleteNricGender = options.clearNricDerivedGenderWhenIncomplete === true;

        if (!idTypeSelect || !genderHidden || !genderSelect || !dateOfBirthInput) {
            return { update: () => { }, resetCreateDefaults: () => { } };
        }

        function updateDerivedFieldsFromNric() {
            if (idTypeSelect.value !== "NRIC") {
                genderHidden.value = genderSelect.value;
                return;
            }

            const inferredGender = inferGenderFromNric(nricInput?.value);
            const inferredDate = inferDateFromNric(nricInput?.value);

            if (inferredGender) {
                genderSelect.value = inferredGender;
                genderHidden.value = inferredGender;
            } else if (clearIncompleteNricGender) {
                genderSelect.value = "";
                genderHidden.value = "";
            }

            dateOfBirthInput.value = inferredDate ?? "";
        }

        function updateIdFields() {
            const isNric = idTypeSelect.value === "NRIC";

            nricSections.forEach((section) => {
                section.classList.toggle("d-none", !isNric);
                setSectionInputsValidationEnabled(section, isNric);
            });

            passportSections.forEach((section) => {
                section.classList.toggle("d-none", isNric);
                setSectionInputsValidationEnabled(section, !isNric);
            });

            if (nricInput) nricInput.required = isNric;
            genderSelect.disabled = isNric;
            dateOfBirthInput.readOnly = isNric;
            updateDerivedFieldsFromNric();
        }

        idTypeSelect.addEventListener("change", updateIdFields);
        nricInput?.addEventListener("input", () => {
            nricInput.value = formatNric(nricInput.value);
            updateDerivedFieldsFromNric();
        });
        genderSelect.addEventListener("change", () => genderHidden.value = genderSelect.value);

        updateIdFields();

        return {
            update: updateIdFields,
            resetCreateDefaults: () => {
                idTypeSelect.value = "NRIC";
                if (nricInput) nricInput.value = "";
                genderSelect.value = resetGenderValue;
                genderHidden.value = resetGenderValue;
                dateOfBirthInput.value = "";
                updateIdFields();
            }
        };
    }

    function setupPhoneControl(options) {
        const countryCodeInput = document.getElementById(options.countryCodeInputId);
        const fullNumberInput = document.getElementById(options.fullNumberInputId);
        const localNumberInput = document.getElementById(options.localNumberInputId);
        const selectedCountryFlag = document.getElementById(options.selectedCountryFlagId);
        const selectedCountryCode = document.getElementById(options.selectedCountryCodeId);
        const searchInput = document.getElementById(options.searchInputId);
        const optionsContainer = document.getElementById(options.optionsContainerId);
        const preview = document.getElementById(options.previewId);

        if (!countryCodeInput || !fullNumberInput || !localNumberInput || !selectedCountryFlag || !selectedCountryCode || !optionsContainer) {
            return { update: () => { }, reset: () => { } };
        }

        function getSelectedCountry() {
            return countries.find((country) => country.code === countryCodeInput.value) ?? countries[0];
        }

        function updateSelectedCountryDisplay() {
            const country = getSelectedCountry();
            selectedCountryFlag.textContent = country.flag;
            selectedCountryCode.textContent = country.code;
        }

        function updateFullNumber() {
            const digits = onlyDigits(localNumberInput.value);
            const countryCode = countryCodeInput.value || "+60";
            localNumberInput.value = digits;
            fullNumberInput.value = digits ? `${countryCode}${digits}` : "";
            if (preview) preview.textContent = digits ? `Number is ${countryCode}${digits}` : `Number is ${countryCode}...`;
        }

        function renderCountryOptions(filter = "") {
            const normalizedFilter = filter.trim().toLowerCase();
            optionsContainer.innerHTML = "";

            countries
                .filter((country) => country.name.toLowerCase().includes(normalizedFilter) || country.code.includes(normalizedFilter))
                .forEach((country) => {
                    const button = document.createElement("button");
                    button.type = "button";
                    button.className = "dropdown-item app-phone-country-option";
                    button.innerHTML = `<span>${country.flag}</span><span>${country.name}</span><span class="text-muted ms-auto">${country.code}</span>`;
                    button.addEventListener("click", () => {
                        countryCodeInput.value = country.code;
                        updateSelectedCountryDisplay();
                        updateFullNumber();
                    });
                    optionsContainer.appendChild(button);
                });
        }

        localNumberInput.addEventListener("input", updateFullNumber);
        searchInput?.addEventListener("input", () => renderCountryOptions(searchInput.value));
        searchInput?.addEventListener("click", (event) => event.stopPropagation());

        updateSelectedCountryDisplay();
        renderCountryOptions();
        updateFullNumber();

        return {
            update: () => {
                updateSelectedCountryDisplay();
                updateFullNumber();
            },
            reset: () => {
                countryCodeInput.value = "+60";
                localNumberInput.value = "";
                updateSelectedCountryDisplay();
                updateFullNumber();
            }
        };
    }

    function setupEmailFeedback(options) {
        const emailInput = document.getElementById(options.emailInputId);
        const feedback = document.getElementById(options.feedbackId);

        if (!emailInput || !feedback) return { update: () => { }, reset: () => { } };

        function updateEmailFeedback() {
            const email = emailInput.value.trim();
            if (!email) {
                feedback.textContent = "";
                feedback.className = "app-field-feedback";
                return;
            }

            const isValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
            feedback.textContent = isValid ? "Email format looks correct." : "Please enter a valid email address.";
            feedback.className = isValid ? "app-field-feedback is-valid" : "app-field-feedback is-invalid";
        }

        emailInput.addEventListener("input", updateEmailFeedback);
        updateEmailFeedback();

        return {
            update: updateEmailFeedback,
            reset: () => {
                feedback.textContent = "";
                feedback.className = "app-field-feedback";
            }
        };
    }

    function parseDateParts(dateValue) {
        const value = String(dateValue || "").trim();
        const match = value.match(/^(\d{4})-(\d{2})-(\d{2})/);
        if (!match) return null;

        const year = Number(match[1]);
        const month = Number(match[2]);
        const day = Number(match[3]);
        const date = new Date(year, month - 1, day);

        if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) return null;
        return { year, month, day };
    }

    function deriveAgeFromDateString(dateValue, referenceDate = new Date()) {
        const parts = parseDateParts(dateValue);
        if (!parts) return null;

        const age = referenceDate.getFullYear() - parts.year;
        return age >= 0 ? age : null;
    }

    function formatAgeFromDateString(dateValue, options = {}) {
        const age = deriveAgeFromDateString(dateValue, options.referenceDate);
        if (age === null) return "";

        const prefix = options.prefix ?? "Age: ";
        const suffix = options.suffix ?? "";
        return `${prefix}${age}${suffix}`;
    }

    function formatDateWithYearAge(dateValue, options = {}) {
        const value = String(dateValue || "").trim();
        if (!value) return "";

        const ageText = formatAgeFromDateString(value, options);
        return ageText ? `${value} (${ageText})` : value;
    }

    function setupAgePreview(options) {
        const dateInput = document.getElementById(options.dateInputId);
        if (!dateInput) return { update: () => { }, reset: () => { } };

        let preview = options.previewId ? document.getElementById(options.previewId) : null;
        if (!preview) {
            preview = document.createElement("div");
            preview.id = options.previewId || `${options.dateInputId}AgePreview`;
            preview.className = options.previewClassName || "app-field-feedback text-muted";
            dateInput.insertAdjacentElement("afterend", preview);
        }

        function update() {
            const ageText = formatAgeFromDateString(dateInput.value, options);
            preview.textContent = ageText || "";
            preview.classList.toggle("d-none", !ageText);
        }

        dateInput.addEventListener("input", update);
        dateInput.addEventListener("change", update);
        update();

        return {
            update,
            reset: () => {
                preview.textContent = "";
                preview.classList.add("d-none");
            }
        };
    }

    window.ProfileHelpers = {
        onlyDigits,
        formatNric,
        inferDateFromNric,
        inferGenderFromNric,
        deriveAgeFromDateString,
        formatAgeFromDateString,
        formatDateWithYearAge,
        setupIdentityFields,
        setupPhoneControl,
        setupEmailFeedback,
        setupAgePreview
    };
})();
