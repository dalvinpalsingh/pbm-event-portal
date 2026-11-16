(() => {
    const selectedTeamStorageKey = "pbm.teamManagement.selectedTeamId";
    const individualCompetitionSelectIds = ["individualCompetitionSlot1Select", "individualCompetitionSlot2Select"];
    const teamCompetitionSelectIds = ["teamCompetitionSlot1Select", "teamCompetitionSlot2Select"];
    const competitionTrackerIds = ["individualCompetitionSlot1Tracker", "individualCompetitionSlot2Tracker", "teamCompetitionSlot1Tracker", "teamCompetitionSlot2Tracker"];
    const tahapAgeRules = {
        TAHAP_1: { minAge: 7, maxAge: 9, label: "Tahap 1" },
        TAHAP_2: { minAge: 10, maxAge: 12, label: "Tahap 2" },
        TAHAP_3: { minAge: 13, maxAge: 15, label: "Tahap 3" },
        TAHAP_4: { minAge: 16, maxAge: 19, label: "Tahap 4" }
    };

    let initialized = false;
    let latestSaveErrorMessage = "";
    let latestSaveErrors = [];
    let currentTeamId = "";
    let selectedParticipantTahapCode = "";
    let selectedParticipantTahapName = "";
    let participantModalHelpers = null;
    let participantDirtyTracker = null;
    let allowParticipantModalHide = false;
    let competitionAssignmentOptions = [];
    let competitionAssignmentOptionsById = {};
    let competitionAssignmentsLoadPromise = null;
    let participantProfileMode = "view";
    let currentParticipantId = "";
    let originalParticipantDetails = null;
    let suppressParticipantActionButtons = false;

    function isTeamManagementPage() {
        return document.getElementById("teamManagementParticipantProfileModal") !== null;
    }

    if (!isTeamManagementPage()) return;

    const saveButton = document.getElementById("teamManagementParticipantSaveButton");
    const form = document.getElementById("teamManagementParticipantForm");
    const participantModalElement = document.getElementById("teamManagementParticipantProfileModal");
    const discardModalElement = document.getElementById("teamManagementParticipantDiscardModal");
    const resetConfirmModalElement = document.getElementById("teamManagementParticipantResetConfirmModal");

    function getValue(id) {
        return document.getElementById(id)?.value?.trim() || "";
    }

    function getNullableValue(id) {
        const value = getValue(id);
        return value === "" ? null : value;
    }

    function setValue(id, value) {
        const element = document.getElementById(id);
        if (element) element.value = value || "";
    }

    function setValueAndNotify(id, value) {
        const element = document.getElementById(id);
        if (!element) return;

        element.value = value || "";
        element.dispatchEvent(new Event("input", { bubbles: true }));
        element.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) element.textContent = value || "";
    }

    function getDigits(value) {
        return String(value || "").replace(/\D/g, "");
    }

    function escapeHtml(value) {
        return String(value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function cloneParticipantDetails(participant) {
        return participant ? JSON.parse(JSON.stringify(participant)) : null;
    }

    function getSaveFailureMessage() {
        return participantProfileMode === "edit"
            ? "Participant could not be updated."
            : "Participant could not be added.";
    }

    function getProfileHelpers() {
        return window.ProfileHelpers || {};
    }

    function deriveAge(dateValue) {
        const helper = getProfileHelpers().deriveAgeFromDateString;
        return typeof helper === "function" ? helper(dateValue) : null;
    }

    function deriveAgeText(dateValue) {
        const helper = getProfileHelpers().formatAgeFromDateString;
        return typeof helper === "function" ? helper(dateValue, { prefix: "" }) : "";
    }

    function getSelectedGuidValues(selectIds) {
        return selectIds
            .map((id) => getValue(id))
            .filter((value) => value !== "");
    }

    function getSelectedCompetitionCount() {
        return getSelectedGuidValues(individualCompetitionSelectIds).length + getSelectedGuidValues(teamCompetitionSelectIds).length;
    }

    function getCurrentTeamId() {
        return getValue("teamManagementParticipantTeamId") || currentTeamId || document.querySelector("[data-team-management-workspace='true']")?.getAttribute("data-team-id") || "";
    }

    function getCurrentTahapRule() {
        const tahapCode = getValue("teamManagementParticipantTahapCode").toUpperCase();
        return tahapAgeRules[tahapCode] || null;
    }

    function isAtLeastOneCompetitionError(error) {
        const normalized = String(error || "").toLowerCase();
        return normalized.includes("at least one") && normalized.includes("competition");
    }

    function isCompetitionError(error) {
        const normalized = String(error || "").toLowerCase();
        return normalized.includes("competition") ||
            normalized.includes("team size") ||
            normalized.includes("team entry") ||
            normalized.includes("individual participant") ||
            normalized.includes("same competition year");
    }

    function ensureSaveAlert() {
        let alert = document.getElementById("teamManagementParticipantSaveAlert");
        if (alert) return alert;

        alert = document.createElement("div");
        alert.id = "teamManagementParticipantSaveAlert";
        alert.className = "alert alert-danger d-none";

        const requiredLegend = form?.querySelector(".app-required-legend");
        if (requiredLegend) {
            requiredLegend.insertAdjacentElement("afterend", alert);
        } else {
            form?.prepend(alert);
        }

        return alert;
    }

    function ensureCompetitionSaveAlert() {
        let alert = document.getElementById("teamManagementCompetitionSaveAlert");
        if (alert) return alert;

        alert = document.createElement("div");
        alert.id = "teamManagementCompetitionSaveAlert";
        alert.className = "alert alert-danger d-none";

        const controls = document.getElementById("teamManagementCompetitionAssignmentsControls");
        const card = controls?.closest(".border.border-2");
        const rightScrollPane = card?.parentElement;

        if (rightScrollPane && card) {
            rightScrollPane.insertBefore(alert, card);
        } else if (controls) {
            controls.insertAdjacentElement("beforebegin", alert);
        }

        return alert;
    }

    function renderAlert(alert, message, errors) {
        const safeMessage = escapeHtml(message || "Participant could not be added.");
        const errorItems = Array.isArray(errors) && errors.length > 0
            ? "<ul class=\"mb-0 mt-2\">" + errors.map((error) => "<li>" + escapeHtml(error) + "</li>").join("") + "</ul>"
            : "";

        alert.innerHTML = "<div class=\"fw-semibold\">" + safeMessage + "</div>" + errorItems;
        alert.classList.remove("d-none");
    }

    function hideAlert(alert) {
        if (!alert) return;
        alert.innerHTML = "";
        alert.classList.add("d-none");
    }

    function renderSaveAlerts() {
        const participantAlert = ensureSaveAlert();
        const competitionAlert = ensureCompetitionSaveAlert();

        if (latestSaveErrors.length === 0 && !latestSaveErrorMessage) {
            hideAlert(participantAlert);
            hideAlert(competitionAlert);
            return;
        }

        const participantErrors = latestSaveErrors.filter((error) => !isCompetitionError(error));
        const competitionErrors = latestSaveErrors.filter(isCompetitionError);
        const hasMessageOnlyError = latestSaveErrors.length === 0 && latestSaveErrorMessage;

        if (participantErrors.length > 0 || hasMessageOnlyError) {
            renderAlert(participantAlert, latestSaveErrorMessage || "Participant could not be added.", participantErrors);
        } else {
            hideAlert(participantAlert);
        }

        if (competitionErrors.length > 0) {
            renderAlert(competitionAlert, "Competition assignments could not be saved.", competitionErrors);
        } else {
            hideAlert(competitionAlert);
        }
    }

    function clearFieldValidation() {
        if (!form) return;
        form.querySelectorAll(".is-invalid").forEach((field) => field.classList.remove("is-invalid"));
        form.querySelectorAll("[data-teammanagement-field-error='true']").forEach((feedback) => feedback.remove());
    }

    function showSaveError(message, errors) {
        latestSaveErrorMessage = message || "Participant could not be added.";
        latestSaveErrors = Array.isArray(errors) ? errors : [];

        renderSaveAlerts();
        applyFieldValidation(latestSaveErrors);

        const hasCompetitionErrors = latestSaveErrors.some(isCompetitionError);
        const hasParticipantErrors = latestSaveErrors.some((error) => !isCompetitionError(error)) || (!hasCompetitionErrors && !!latestSaveErrorMessage);
        scrollSaveErrorIntoView(hasCompetitionErrors, hasParticipantErrors);
    }

    function clearSaveError() {
        latestSaveErrorMessage = "";
        latestSaveErrors = [];
        renderSaveAlerts();
        clearFieldValidation();
    }

    function clearSpecificFieldValidation(fieldId) {
        const field = document.getElementById(fieldId);
        if (field) field.classList.remove("is-invalid");
        form?.querySelector("[data-teammanagement-field-error-for='" + fieldId + "']")?.remove();
    }

    function clearCompetitionFieldValidation() {
        individualCompetitionSelectIds.concat(teamCompetitionSelectIds).forEach(clearSpecificFieldValidation);
    }

    function clearCompetitionTrackers() {
        competitionTrackerIds.forEach((trackerId) => {
            const tracker = document.getElementById(trackerId);
            if (!tracker) return;
            tracker.innerHTML = "";
            tracker.className = "small mt-2 d-none";
        });
    }

    function resetAddParticipantValidationUi() {
        clearSaveError();
        clearCompetitionTrackers();
        clearFieldValidation();
    }

    function resetParticipantValidationUiForShownModal() {
        clearSaveError();
        clearFieldValidation();

        if (participantProfileMode === "view") {
            clearCompetitionTrackers();
            return;
        }

        renderSelectedCompetitionTracker();
    }

    function setParticipantProfileMode(mode) {
        participantProfileMode = mode || "view";

        const isViewMode = participantProfileMode === "view";
        const isEditMode = participantProfileMode === "edit";
        const cancelButton = document.getElementById("teamManagementParticipantCancelButton");
        const resetButton = document.getElementById("teamManagementParticipantResetButton");
        const draftButton = document.getElementById("teamManagementParticipantSaveDraftButton");

        if (cancelButton) {
            cancelButton.textContent = isViewMode ? "Close" : "Cancel";
            cancelButton.className = isViewMode ? "btn btn-primary" : "btn btn-outline-danger";
        }

        if (draftButton) draftButton.classList.toggle("d-none", isViewMode || isEditMode);
        if (saveButton) saveButton.classList.toggle("d-none", isViewMode);

        form?.querySelectorAll("input, select, textarea, button").forEach((control) => {
            control.disabled = isViewMode;
        });

        syncParticipantActionButtons();
    }

    function setFieldInvalid(fieldId, message) {
        const field = document.getElementById(fieldId);
        if (!field || !form) return;

        field.classList.add("is-invalid");

        const existingFeedback = form.querySelector("[data-teammanagement-field-error-for='" + fieldId + "']");
        if (existingFeedback) {
            existingFeedback.textContent = message;
            return;
        }

        const feedback = document.createElement("div");
        feedback.className = "invalid-feedback d-block";
        feedback.textContent = message;
        feedback.setAttribute("data-teammanagement-field-error", "true");
        feedback.setAttribute("data-teammanagement-field-error-for", fieldId);

        const phoneControl = field.closest(".app-phone-control");
        const insertAfter = phoneControl || field;
        insertAfter.insertAdjacentElement("afterend", feedback);
    }

    function setSelectedCompetitionFieldsInvalid(fieldIds, message) {
        fieldIds.forEach((fieldId) => {
            if (getValue(fieldId)) {
                setFieldInvalid(fieldId, message);
            }
        });
    }

    function getDuplicateCompetitionFieldIds(selectIds) {
        const selected = selectIds
            .map((fieldId) => ({ fieldId, value: getValue(fieldId) }))
            .filter((item) => item.value !== "");

        const duplicateValues = selected
            .map((item) => item.value)
            .filter((value, index, values) => values.indexOf(value) !== index);

        return selected
            .filter((item) => duplicateValues.includes(item.value))
            .map((item) => item.fieldId);
    }

    function setDuplicateCompetitionFieldsInvalid(selectIds, message) {
        getDuplicateCompetitionFieldIds(selectIds).forEach((fieldId) => setFieldInvalid(fieldId, message));
    }

    function getDuplicateCompetitionValidationErrors() {
        const errors = [];

        if (getDuplicateCompetitionFieldIds(individualCompetitionSelectIds).length > 0) {
            errors.push("The same individual competition cannot be selected twice.");
        }

        if (getDuplicateCompetitionFieldIds(teamCompetitionSelectIds).length > 0) {
            errors.push("The same team competition cannot be selected twice.");
        }

        return errors;
    }

    function getAtLeastOneCompetitionValidationErrors() {
        if (getSelectedCompetitionCount() > 0) return [];
        return ["Please select at least one individual or team competition before submitting for approval."];
    }

    function clearResolvedCompetitionSaveErrors() {
        if (latestSaveErrors.length === 0) return;

        const originalCount = latestSaveErrors.length;
        const selectedCompetitionCount = getSelectedCompetitionCount();
        const duplicateIndividualFieldIds = getDuplicateCompetitionFieldIds(individualCompetitionSelectIds);
        const duplicateTeamFieldIds = getDuplicateCompetitionFieldIds(teamCompetitionSelectIds);

        latestSaveErrors = latestSaveErrors.filter((error) => {
            const normalized = String(error || "").toLowerCase();

            if (isAtLeastOneCompetitionError(error) && selectedCompetitionCount > 0) return false;
            if (normalized.includes("same individual competition") && duplicateIndividualFieldIds.length === 0) return false;
            if (normalized.includes("same team competition") && duplicateTeamFieldIds.length === 0) return false;

            return true;
        });

        if (latestSaveErrors.length === 0) {
            latestSaveErrorMessage = "";
        }

        if (latestSaveErrors.length !== originalCount) {
            renderSaveAlerts();
        }
    }

    function validateCompetitionSelectionsLive() {
        if (participantProfileMode === "view") {
            clearCompetitionFieldValidation();
            clearCompetitionTrackers();
            return [];
        }

        clearCompetitionFieldValidation();

        const errors = getDuplicateCompetitionValidationErrors();
        errors.forEach((error) => {
            if (error.toLowerCase().includes("individual")) {
                setDuplicateCompetitionFieldsInvalid(individualCompetitionSelectIds, error);
            }

            if (error.toLowerCase().includes("team")) {
                setDuplicateCompetitionFieldsInvalid(teamCompetitionSelectIds, error);
            }
        });

        clearResolvedCompetitionSaveErrors();
        renderSelectedCompetitionTracker();
        return errors;
    }

    function getAgeRangeError() {
        const dateInput = document.getElementById("dateOfBirthInput");
        const rule = getCurrentTahapRule();
        if (!dateInput || !rule || !dateInput.value) return "";

        const age = deriveAge(dateInput.value);
        if (age === null) return "Date of birth is invalid.";

        if (age < rule.minAge || age > rule.maxAge) {
            return `${rule.label} participant age must be between ${rule.minAge} and ${rule.maxAge}. Current age is ${age}.`;
        }

        return "";
    }

    function clearAgeValidation() {
        clearSpecificFieldValidation("dateOfBirthInput");
        clearSpecificFieldValidation("participantAgeInput");
        clearSpecificFieldValidation("nricInput");
    }

    function applyAgeValidation() {
        clearAgeValidation();

        const error = getAgeRangeError();
        if (!error) return "";

        setFieldInvalid("dateOfBirthInput", error);
        setFieldInvalid("participantAgeInput", error);

        if (getValue("idTypeSelect").toUpperCase() === "NRIC") {
            setFieldInvalid("nricInput", error);
        }

        return error;
    }

    function applyFieldValidation(errors) {
        clearFieldValidation();

        errors.forEach((error) => {
            const message = String(error || "");
            const normalized = message.toLowerCase();

            if (normalized.includes("full name as per id")) setFieldInvalid("fullNameAsPerIdInput", message);
            if (normalized.includes("full name in gurmukhi")) setFieldInvalid("fullNameGurmukhiInput", message);
            if (normalized.includes("id type")) setFieldInvalid("idTypeSelect", message);
            if (normalized.includes("nric")) setFieldInvalid("nricInput", message);
            if (normalized.includes("passport number")) setFieldInvalid("passportNumberInput", message);
            if (normalized.includes("passport expiry")) setFieldInvalid("passportExpiryDateInput", message);
            if (normalized.includes("gender")) setFieldInvalid("genderSelect", message);
            if (normalized.includes("date of birth")) setFieldInvalid("dateOfBirthInput", message);
            if (normalized.includes("email")) setFieldInvalid("emailInput", message);
            if (normalized.includes("mobile number") && !normalized.includes("parent") && !normalized.includes("guardian")) setFieldInvalid("mobileLocalNumberInput", message);
            if (normalized.includes("parent / guardian name")) setFieldInvalid("parentGuardianNameInput", message);
            if (normalized.includes("parent / guardian mobile")) setFieldInvalid("guardianLocalNumberInput", message);

            if (normalized.includes("age must be")) {
                setFieldInvalid("dateOfBirthInput", message);
                setFieldInvalid("participantAgeInput", message);
                if (getValue("idTypeSelect").toUpperCase() === "NRIC") setFieldInvalid("nricInput", message);
            }

            if (normalized.includes("same individual competition")) {
                setDuplicateCompetitionFieldsInvalid(individualCompetitionSelectIds, message);
            } else if (normalized.includes("individual competition") || normalized.includes("individual participant")) {
                setSelectedCompetitionFieldsInvalid(individualCompetitionSelectIds, message);
            }

            if (normalized.includes("same team competition")) {
                setDuplicateCompetitionFieldsInvalid(teamCompetitionSelectIds, message);
            } else if (normalized.includes("team competition") || normalized.includes("team size") || normalized.includes("team entry")) {
                setSelectedCompetitionFieldsInvalid(teamCompetitionSelectIds, message);
            }

            if (normalized.includes("selected competition") || normalized.includes("competition category") || normalized.includes("same competition year")) {
                setSelectedCompetitionFieldsInvalid(individualCompetitionSelectIds.concat(teamCompetitionSelectIds), message);
            }
        });
    }

    function scrollSaveErrorIntoView(hasCompetitionErrors, hasParticipantErrors) {
        const participantAlert = ensureSaveAlert();
        const leftScrollPane = participantAlert.closest("[style*='overflow-y']");
        const competitionAlert = ensureCompetitionSaveAlert();
        const rightScrollPane = competitionAlert.closest("[style*='overflow-y']");

        window.requestAnimationFrame(() => {
            if (hasParticipantErrors && leftScrollPane) {
                leftScrollPane.scrollTo({ top: 0, behavior: "smooth" });
            }

            if (hasCompetitionErrors && rightScrollPane) {
                rightScrollPane.scrollTo({ top: 0, behavior: "smooth" });
            }

            if (hasParticipantErrors) {
                participantAlert.scrollIntoView({ behavior: "smooth", block: "start" });
            }
        });
    }

    function splitDateOfBirthAndAgeFields() {
        const dateInput = document.getElementById("dateOfBirthInput");
        if (!dateInput || dateInput.dataset.ageSplitEnhanced === "true") return null;

        const originalWrapper = dateInput.closest(".mb-3");
        if (!originalWrapper) return null;

        dateInput.dataset.ageSplitEnhanced = "true";

        const dateLabel = originalWrapper.querySelector("label")?.cloneNode(true);
        const row = document.createElement("div");
        row.className = "row g-2 mb-3";
        row.id = "dateOfBirthAgeRow";

        const dateCol = document.createElement("div");
        dateCol.className = "col-md-8";

        const ageCol = document.createElement("div");
        ageCol.className = "col-md-4";

        if (dateLabel) dateCol.appendChild(dateLabel);
        dateCol.appendChild(dateInput);

        const ageLabel = document.createElement("label");
        ageLabel.className = "form-label";
        ageLabel.setAttribute("for", "participantAgeInput");
        ageLabel.textContent = "Age";

        const ageInput = document.createElement("input");
        ageInput.className = "form-control";
        ageInput.id = "participantAgeInput";
        ageInput.type = "text";
        ageInput.readOnly = true;
        ageInput.tabIndex = -1;
        ageInput.setAttribute("aria-readonly", "true");
        ageInput.setAttribute("data-derived-age-field", "true");

        ageCol.appendChild(ageLabel);
        ageCol.appendChild(ageInput);
        row.appendChild(dateCol);
        row.appendChild(ageCol);

        originalWrapper.replaceWith(row);
        return ageInput;
    }

    function ensureDateOfBirthAgePreview() {
        const dateInput = document.getElementById("dateOfBirthInput");
        if (!dateInput) return null;

        const ageInput = document.getElementById("participantAgeInput") || splitDateOfBirthAndAgeFields();
        if (!ageInput) return null;

        function updateAgePreview() {
            const ageText = deriveAgeText(dateInput.value);
            ageInput.value = ageText || "";
            applyAgeValidation();
        }

        function scheduleAgePreviewUpdate() {
            window.setTimeout(updateAgePreview, 0);
        }

        if (dateInput.dataset.agePreviewEnhanced !== "true") {
            dateInput.dataset.agePreviewEnhanced = "true";
            dateInput.addEventListener("input", scheduleAgePreviewUpdate);
            dateInput.addEventListener("change", scheduleAgePreviewUpdate);

            const nricInput = document.getElementById("nricInput");
            nricInput?.addEventListener("input", scheduleAgePreviewUpdate);
            nricInput?.addEventListener("change", scheduleAgePreviewUpdate);

            document.getElementById("idTypeSelect")?.addEventListener("change", scheduleAgePreviewUpdate);
            participantModalElement?.addEventListener("shown.bs.modal", scheduleAgePreviewUpdate);
        }

        updateAgePreview();
        return { update: updateAgePreview };
    }

    function getProfileValidationErrors() {
        const errors = [];
        const idType = getValue("idTypeSelect").toUpperCase();
        const email = getValue("emailInput");
        const mobileLocalNumber = getDigits(getValue("mobileLocalNumberInput"));
        const guardianLocalNumber = getDigits(getValue("guardianLocalNumberInput"));

        if (!getValue("fullNameAsPerIdInput")) errors.push("Full name as per ID is required.");
        if (!getValue("fullNameGurmukhiInput")) errors.push("Full name in Gurmukhi is required.");

        if (!idType) {
            errors.push("ID type must be NRIC or Passport.");
        } else if (idType === "NRIC") {
            const nricNumber = getDigits(getValue("nricInput"));
            if (!nricNumber) errors.push("NRIC number is required.");
            else if (nricNumber.length !== 12) errors.push("NRIC number must be exactly 12 digits.");
        } else if (idType === "PASSPORT") {
            if (!getValue("passportNumberInput")) errors.push("Passport number is required.");
            if (!getValue("passportExpiryDateInput")) errors.push("Passport expiry date is required.");
            if (!getValue("genderSelect")) errors.push("Gender must be Male or Female.");
            if (!getValue("dateOfBirthInput")) errors.push("Date of birth is required.");
        } else {
            errors.push("ID type must be NRIC or Passport.");
        }

        if (mobileLocalNumber && mobileLocalNumber.length < 6) errors.push("Mobile number is too short.");
        if (email && !/^\S+@\S+\.\S+$/.test(email)) errors.push("Please enter a valid email address.");
        if (!getValue("parentGuardianNameInput")) errors.push("Parent / guardian name is required.");
        if (!guardianLocalNumber) errors.push("Parent / guardian mobile is required.");
        else if (guardianLocalNumber.length < 6) errors.push("Parent / guardian mobile is too short.");

        return errors;
    }

    function getClientValidationErrors() {
        const errors = getProfileValidationErrors()
            .concat(getAtLeastOneCompetitionValidationErrors())
            .concat(validateCompetitionSelectionsLive());

        const ageError = applyAgeValidation();
        if (ageError) errors.push(ageError);

        return errors;
    }

    function getCompetitionAssignmentSelects() {
        return Array.from(document.querySelectorAll(".team-management-competition-select"));
    }

    function setCompetitionSelectOptions(selectId, options, placeholder, preserveSelectedValue) {
        const select = document.getElementById(selectId);
        if (!select) return;

        const selectedValue = preserveSelectedValue ? select.value : "";
        const optionHtml = (options || []).map((option) => {
            const id = option.competitionCategoryId || "";
            const text = option.displayText || "";
            return "<option value=\"" + escapeHtml(id) + "\">" + escapeHtml(text) + "</option>";
        }).join("");

        select.innerHTML = "<option value=\"\">" + escapeHtml(placeholder) + "</option>" + optionHtml;
        select.value = selectedValue && Array.from(select.options).some((option) => option.value === selectedValue) ? selectedValue : "";
        select.disabled = false;
    }

    function setSelectValueWhenAvailable(selectId, value, attempt) {
        const select = document.getElementById(selectId);
        if (!select || !value) return;

        const hasOption = Array.from(select.options).some((option) => option.value === value);
        if (hasOption) {
            select.value = value;
            select.dispatchEvent(new Event("change", { bubbles: true }));
            return;
        }

        if ((attempt || 0) >= 20) return;

        window.setTimeout(() => {
            setSelectValueWhenAvailable(selectId, value, (attempt || 0) + 1);
        }, 100);
    }

    function setCompetitionAssignmentsLoading(isLoading) {
        const loading = document.getElementById("teamManagementCompetitionAssignmentsLoading");
        if (loading) loading.classList.toggle("d-none", !isLoading);

        getCompetitionAssignmentSelects().forEach((select) => {
            select.disabled = isLoading;
        });
    }

    function clearCompetitionAssignmentsError() {
        const error = document.getElementById("teamManagementCompetitionAssignmentsError");
        if (!error) return;
        error.textContent = "";
        error.classList.add("d-none");
    }

    function showCompetitionAssignmentsError(message) {
        setCompetitionAssignmentsLoading(false);

        const error = document.getElementById("teamManagementCompetitionAssignmentsError");
        if (!error) return;
        error.textContent = message || "Competition assignments could not be loaded.";
        error.classList.remove("d-none");
    }

    function resetCompetitionAssignmentPanel() {
        competitionAssignmentOptions = [];
        competitionAssignmentOptionsById = {};
        competitionAssignmentsLoadPromise = null;

        setCompetitionSelectOptions("individualCompetitionSlot1Select", [], "Select individual event...", false);
        setCompetitionSelectOptions("individualCompetitionSlot2Select", [], "Select individual event...", false);
        setCompetitionSelectOptions("teamCompetitionSlot1Select", [], "Select team event...", false);
        setCompetitionSelectOptions("teamCompetitionSlot2Select", [], "Select team event...", false);

        clearCompetitionAssignmentsError();
        renderSelectedCompetitionTracker();
    }

    function renderCompetitionAssignmentOptions(data) {
        clearCompetitionAssignmentsError();

        competitionAssignmentOptions = data.trackerRows || [];
        competitionAssignmentOptionsById = {};

        competitionAssignmentOptions.forEach((option) => {
            competitionAssignmentOptionsById[String(option.competitionCategoryId)] = option;
        });

        setCompetitionSelectOptions("individualCompetitionSlot1Select", data.individualCompetitions || [], "Select individual event...", false);
        setCompetitionSelectOptions("individualCompetitionSlot2Select", data.individualCompetitions || [], "Select individual event...", false);
        setCompetitionSelectOptions("teamCompetitionSlot1Select", data.teamCompetitions || [], "Select team event...", false);
        setCompetitionSelectOptions("teamCompetitionSlot2Select", data.teamCompetitions || [], "Select team event...", false);

        setCompetitionAssignmentsLoading(false);
        renderSelectedCompetitionTracker();
    }

    function loadCompetitionAssignments() {
        if (!getCurrentTeamId() || !selectedParticipantTahapCode) return Promise.resolve();

        setCompetitionAssignmentsLoading(true);
        clearCompetitionAssignmentsError();

        let url = "/TeamManagement?handler=CompetitionAssignments&teamId=" +
            encodeURIComponent(getCurrentTeamId()) +
            "&tahapCode=" +
            encodeURIComponent(selectedParticipantTahapCode);

        if (participantProfileMode === "edit" && currentParticipantId) {
            url += "&participantId=" + encodeURIComponent(currentParticipantId);
        }

        competitionAssignmentsLoadPromise = fetch(url, {
            method: "GET",
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        })
            .then((response) => {
                return response.json().then((data) => {
                    if (!response.ok || !data.success) throw new Error(data.message || "Competition assignments could not be loaded.");
                    return data;
                });
            })
            .then(renderCompetitionAssignmentOptions)
            .catch((loadError) => {
                competitionAssignmentsLoadPromise = null;
                showCompetitionAssignmentsError(loadError.message);
            });

        return competitionAssignmentsLoadPromise;
    }

    function getSelectedCompetitionCounts() {
        const counts = {};

        getCompetitionAssignmentSelects().forEach((select) => {
            if (!select.value) return;
            counts[select.value] = (counts[select.value] || 0) + 1;
        });

        return counts;
    }

    function trackerStatusIconHtml(row, previewCount) {
        const eventType = String(row.eventType || "").toUpperCase();
        const targetCount = Number(row.targetCount || 0);
        let iconClass = "";
        let tooltip = "";

        if (previewCount <= 0) return "";

        if (eventType === "INDIVIDUAL") {
            if (targetCount <= 0 || previewCount <= targetCount) {
                iconClass = "bi-check-circle-fill text-success";
                tooltip = "Within allowed quota";
            } else {
                iconClass = "bi-x-circle-fill text-danger";
                tooltip = "Over allowed quota";
            }
        } else if (eventType === "TEAM") {
            if (targetCount <= 0) {
                iconClass = "bi-exclamation-circle-fill text-warning";
                tooltip = "Team size is not configured";
            } else if (previewCount < targetCount) {
                iconClass = "bi-exclamation-circle-fill text-warning";
                tooltip = "Team requires more participants";
            } else if (previewCount === targetCount) {
                iconClass = "bi-check-circle-fill text-success";
                tooltip = "Team size complete";
            } else {
                iconClass = "bi-x-circle-fill text-danger";
                tooltip = "Too many participants";
            }
        }

        return iconClass
            ? "<i class=\"bi " + iconClass + "\" role=\"img\" aria-label=\"" + escapeHtml(tooltip) + "\" title=\"" + escapeHtml(tooltip) + "\" data-bs-toggle=\"tooltip\"></i>"
            : "";
    }

    function initializeTooltips(root) {
        if (!window.bootstrap || !root) return;
        root.querySelectorAll("[data-bs-toggle='tooltip']").forEach((element) => {
            bootstrap.Tooltip.getOrCreateInstance(element);
        });
    }

    function renderCompetitionTrackerForSelect(selectId, trackerId) {
        const select = document.getElementById(selectId);
        const tracker = document.getElementById(trackerId);

        if (!select || !tracker) return;

        if (!select.value) {
            tracker.innerHTML = "";
            tracker.className = "small mt-2 d-none";
            return;
        }

        const row = competitionAssignmentOptionsById[select.value];
        if (!row) {
            tracker.innerHTML = "";
            tracker.className = "small mt-2 d-none";
            return;
        }

        const selectedCounts = getSelectedCompetitionCounts();
        const previewCount = Number(row.currentCount || 0) + (participantProfileMode === "view" ? 0 : Number(selectedCounts[select.value] || 0));
        const targetCount = Number(row.targetCount || 0);
        const countText = previewCount + "/" + targetCount;

        tracker.innerHTML =
            "<span class=\"badge text-bg-light border\">" + escapeHtml(countText) + "</span>" +
            trackerStatusIconHtml(row, previewCount);

        tracker.className = "small mt-2 d-flex align-items-center gap-2";
        initializeTooltips(tracker);
    }

    function renderSelectedCompetitionTracker() {
        if (participantProfileMode === "view") {
            clearCompetitionTrackers();
            return;
        }

        renderCompetitionTrackerForSelect("individualCompetitionSlot1Select", "individualCompetitionSlot1Tracker");
        renderCompetitionTrackerForSelect("individualCompetitionSlot2Select", "individualCompetitionSlot2Tracker");
        renderCompetitionTrackerForSelect("teamCompetitionSlot1Select", "teamCompetitionSlot1Tracker");
        renderCompetitionTrackerForSelect("teamCompetitionSlot2Select", "teamCompetitionSlot2Tracker");
    }

    function refreshCompetitionTrackers() {
        if (participantProfileMode === "view") {
            clearCompetitionTrackers();
            return Promise.resolve();
        }

        const renderAfterCurrentUpdates = () => new Promise((resolve) => {
            window.setTimeout(() => {
                renderSelectedCompetitionTracker();
                resolve();
            }, 0);
        });

        if (competitionAssignmentsLoadPromise) {
            return competitionAssignmentsLoadPromise.then(renderAfterCurrentUpdates);
        }

        return renderAfterCurrentUpdates();
    }

    function setupParticipantModalHelpers() {
        if (participantModalHelpers || !window.ProfileHelpers) return;

        participantModalHelpers = {
            identityFields: window.ProfileHelpers.setupIdentityFields({
                idTypeSelectId: "idTypeSelect",
                nricInputId: "nricInput",
                genderHiddenId: "genderHidden",
                genderSelectId: "genderSelect",
                dateOfBirthInputId: "dateOfBirthInput",
                resetGenderValue: "",
                clearNricDerivedGenderWhenIncomplete: true
            }),
            mobileControl: window.ProfileHelpers.setupPhoneControl({
                countryCodeInputId: "mobileCountryCodeInput",
                fullNumberInputId: "mobileFullNumberInput",
                localNumberInputId: "mobileLocalNumberInput",
                selectedCountryFlagId: "selectedCountryFlag",
                selectedCountryCodeId: "selectedCountryCode",
                searchInputId: "countrySearchInput",
                optionsContainerId: "countryOptions",
                previewId: "mobilePreview"
            }),
            guardianMobileControl: window.ProfileHelpers.setupPhoneControl({
                countryCodeInputId: "guardianCountryCodeInput",
                fullNumberInputId: "guardianFullNumberInput",
                localNumberInputId: "guardianLocalNumberInput",
                selectedCountryFlagId: "guardianSelectedCountryFlag",
                selectedCountryCodeId: "guardianSelectedCountryCode",
                searchInputId: "guardianCountrySearchInput",
                optionsContainerId: "guardianCountryOptions",
                previewId: "guardianMobilePreview"
            }),
            emailFeedback: window.ProfileHelpers.setupEmailFeedback({
                emailInputId: "emailInput",
                feedbackId: "emailFeedback"
            })
        };
    }

    function createParticipantDirtyTracker() {
        if (!window.FormHelpers?.createDirtyTracker) {
            return {
                markClean: function () { },
                isDirty: function () { return false; }
            };
        }

        return window.FormHelpers.createDirtyTracker({
            fields: [
                { key: "teamId", id: "teamManagementParticipantTeamId" },
                { key: "tahapCode", id: "teamManagementParticipantTahapCode" },
                { key: "fullName", id: "fullNameAsPerIdInput", trim: true },
                { key: "fullNameGurmukhi", id: "fullNameGurmukhiInput", trim: true },
                { key: "idType", id: "idTypeSelect" },
                { key: "nric", id: "nricInput", trim: true },
                { key: "passportNumber", id: "passportNumberInput", trim: true },
                { key: "passportExpiryDate", id: "passportExpiryDateInput" },
                { key: "gender", id: "genderSelect" },
                { key: "dateOfBirth", id: "dateOfBirthInput" },
                { key: "mobileCountryCode", id: "mobileCountryCodeInput" },
                { key: "mobileLocalNumber", id: "mobileLocalNumberInput", trim: true },
                { key: "email", id: "emailInput", trim: true },
                { key: "parentGuardianName", id: "parentGuardianNameInput", trim: true },
                { key: "guardianCountryCode", id: "guardianCountryCodeInput" },
                { key: "guardianLocalNumber", id: "guardianLocalNumberInput", trim: true },
                { key: "individualCompetitionSlot1", id: "individualCompetitionSlot1Select" },
                { key: "individualCompetitionSlot2", id: "individualCompetitionSlot2Select" },
                { key: "teamCompetitionSlot1", id: "teamCompetitionSlot1Select" },
                { key: "teamCompetitionSlot2", id: "teamCompetitionSlot2Select" }
            ]
        });
    }

    function getParticipantDirtyTracker() {
        if (!participantDirtyTracker) {
            participantDirtyTracker = createParticipantDirtyTracker();
        }

        return participantDirtyTracker;
    }

    function isParticipantModalDirty() {
        return getParticipantDirtyTracker().isDirty();
    }

    function hasUnsavedParticipantChanges() {
        return isParticipantModalDirty();
    }

    function isDraftEditMode() {
        return participantProfileMode === "add" && getValue("teamManagementParticipantDraftId") !== "";
    }

    function isUndoChangesMode() {
        return participantProfileMode === "edit" || isDraftEditMode();
    }

    function getSubmitButtonText() {
        return participantProfileMode === "edit" ? "Resubmit for Approval" : "Submit for Approval";
    }

    function setButtonBusy(button, isBusy) {
        if (!button) return;
        button.dataset.teammanagementBusy = isBusy ? "true" : "false";
    }

    function isButtonBusy(button) {
        return button?.dataset.teammanagementBusy === "true";
    }

    function setResetButtonLabel(label) {
        const resetButton = document.getElementById("teamManagementParticipantResetButton");
        if (resetButton) resetButton.textContent = label || "Reset";
    }

    function syncResetButtonLabel() {
        setResetButtonLabel(isUndoChangesMode() ? "Undo changes" : "Reset");
    }

    function syncResetConfirmModalCopy() {
        const title = document.getElementById("teamManagementParticipantResetConfirmModalLabel");
        const body = document.getElementById("teamManagementParticipantResetConfirmModalBody");
        const confirmButton = document.getElementById("teamManagementParticipantConfirmResetButton");
        const isUndoMode = isUndoChangesMode();

        if (title) title.textContent = isUndoMode ? "Undo changes?" : "Reset form?";
        if (body) {
            body.textContent = isUndoMode
                ? "This will undo changes and reload original participant details."
                : "This will clear all entered participant details and return the form to its default state.";
        }
        if (confirmButton) confirmButton.textContent = isUndoMode ? "Undo changes" : "Reset form";
    }

    function syncParticipantActionButtons() {
        const resetButton = document.getElementById("teamManagementParticipantResetButton");
        const draftButton = document.getElementById("teamManagementParticipantSaveDraftButton");
        const isViewMode = participantProfileMode === "view";
        const isEditMode = participantProfileMode === "edit";
        const isDirty = !suppressParticipantActionButtons && hasUnsavedParticipantChanges();

        syncResetButtonLabel();
        syncResetConfirmModalCopy();

        if (resetButton) {
            resetButton.classList.toggle("d-none", isViewMode);
            resetButton.disabled = isViewMode || !isDirty;
        }

        if (draftButton) {
            const hideDraftButton = isViewMode || isEditMode;
            draftButton.classList.toggle("d-none", hideDraftButton);
            if (!isButtonBusy(draftButton)) {
                draftButton.innerHTML = "Save as Draft";
            }
            draftButton.disabled = hideDraftButton || !isDirty || isButtonBusy(draftButton);
        }

        if (saveButton) {
            saveButton.classList.toggle("d-none", isViewMode);
            if (!isButtonBusy(saveButton)) {
                saveButton.innerHTML = getSubmitButtonText();
            }
            saveButton.disabled = isViewMode || !isDirty || isButtonBusy(saveButton);
        }
    }

    function markParticipantModalClean() {
        getParticipantDirtyTracker().markClean();
        syncParticipantActionButtons();
    }

    function setActionButtonsSuppressed(isSuppressed) {
        suppressParticipantActionButtons = isSuppressed;
        syncParticipantActionButtons();
    }

    function closeParticipantModalWithoutWarning() {
        allowParticipantModalHide = true;
        bootstrap.Modal.getInstance(participantModalElement)?.hide();
    }

    function resetParticipantModalForm() {
        form?.reset();
        setValue("teamManagementParticipantTeamId", currentTeamId);
        setValue("teamManagementParticipantTahapCode", selectedParticipantTahapCode);
        resetCompetitionAssignmentPanel();

        if (participantModalHelpers) {
            participantModalHelpers.identityFields?.resetCreateDefaults();
            participantModalHelpers.mobileControl?.reset();
            participantModalHelpers.guardianMobileControl?.reset();
            participantModalHelpers.emailFeedback?.reset();
        }

        window.FormHelpers?.clearValidationMessages(form);
        clearSaveError();
        window.TeamManagementParticipantDraft?.clearDraftAlert?.();
        ensureDateOfBirthAgePreview()?.update?.();
    }

    function applyParticipantDetailsToModal(participant) {
        const details = participant || {};

        currentParticipantId = details.participantId || currentParticipantId || "";
        setValue("teamManagementParticipantTeamId", details.teamId || "");
        setValue("teamManagementParticipantTahapCode", details.tahapCode || "");
        setValue("fullNameAsPerIdInput", details.fullName || "");
        setValue("fullNameGurmukhiInput", details.fullNameGurmukhi || "");
        setValueAndNotify("idTypeSelect", details.idType || "NRIC");
        setValueAndNotify("nricInput", details.nricNumber || "");
        setValue("passportNumberInput", details.passportNumber || "");
        setValue("passportExpiryDateInput", details.passportExpiryDate || "");
        setValue("mobileCountryCodeInput", details.mobileCountryCode || "+60");
        setValue("mobileLocalNumberInput", details.mobileLocalNumber || "");
        setValue("emailInput", details.email || "");
        setValue("parentGuardianNameInput", details.parentGuardianName || "");
        setValue("guardianCountryCodeInput", details.parentGuardianMobileCountryCode || "+60");
        setValue("guardianLocalNumberInput", details.parentGuardianMobileLocalNumber || "");
        setValueAndNotify("genderSelect", details.gender || "");
        setValue("genderHidden", details.gender || "");
        setValueAndNotify("dateOfBirthInput", details.dateOfBirth || "");

        participantModalHelpers?.mobileControl?.update?.();
        participantModalHelpers?.guardianMobileControl?.update?.();
        participantModalHelpers?.emailFeedback?.update?.();
        ensureDateOfBirthAgePreview()?.update?.();

        const individualIds = details.individualCompetitionCategoryIds || [];
        const teamIds = details.teamCompetitionCategoryIds || [];

        setSelectValueWhenAvailable("individualCompetitionSlot1Select", individualIds[0] || "");
        setSelectValueWhenAvailable("individualCompetitionSlot2Select", individualIds[1] || "");
        setSelectValueWhenAvailable("teamCompetitionSlot1Select", teamIds[0] || "");
        setSelectValueWhenAvailable("teamCompetitionSlot2Select", teamIds[1] || "");
    }

    function restoreOriginalParticipantDetails() {
        if (!originalParticipantDetails) return;

        currentTeamId = originalParticipantDetails.teamId || "";
        currentParticipantId = originalParticipantDetails.participantId || "";
        selectedParticipantTahapCode = originalParticipantDetails.tahapCode || "";
        selectedParticipantTahapName = originalParticipantDetails.tahapName || originalParticipantDetails.tahapCode || "";

        resetParticipantModalForm();
        setParticipantProfileMode(participantProfileMode);
        resetAddParticipantValidationUi();

        loadCompetitionAssignments().then(() => {
            applyParticipantDetailsToModal(originalParticipantDetails);
            setParticipantProfileMode(participantProfileMode);
            markParticipantModalClean();
            resetAddParticipantValidationUi();
            renderSelectedCompetitionTracker();
        });

        applyParticipantDetailsToModal(originalParticipantDetails);
        markParticipantModalClean();
    }

    function openParticipantModal(options) {
        if (!participantModalElement) return;

        const settings = options || {};
        const participant = settings.participant || null;
        const mode = settings.mode || "view";

        currentTeamId = settings.teamId || participant?.teamId || "";
        currentParticipantId = participant?.participantId || settings.participantId || "";
        selectedParticipantTahapCode = settings.tahapCode || participant?.tahapCode || "";
        selectedParticipantTahapName = settings.tahapName || participant?.tahapName || "";
        originalParticipantDetails = mode === "edit" ? cloneParticipantDetails(participant) : null;

        setupParticipantModalHelpers();
        setText("teamManagementParticipantProfileModalLabel", mode === "add"
            ? "Add " + selectedParticipantTahapName + " Participant"
            : mode === "edit"
                ? "Edit Participant"
                : "View Participant");
        if (saveButton) saveButton.innerHTML = getSubmitButtonText();

        resetParticipantModalForm();
        setParticipantProfileMode(mode);
        resetAddParticipantValidationUi();
        loadCompetitionAssignments().then(() => {
            if (participant) applyParticipantDetailsToModal(participant);
            setParticipantProfileMode(mode);
            markParticipantModalClean();
            if (mode === "view") resetAddParticipantValidationUi();
            refreshCompetitionTrackers();
        });
        if (participant) applyParticipantDetailsToModal(participant);
        markParticipantModalClean();
        if (mode === "view") resetAddParticipantValidationUi();
        bootstrap.Modal.getOrCreateInstance(participantModalElement).show();
    }

    function requestCloseParticipantModal() {
        if (participantProfileMode === "view") {
            closeParticipantModalWithoutWarning();
            return;
        }

        if (!hasUnsavedParticipantChanges()) {
            closeParticipantModalWithoutWarning();
            return;
        }

        if (!discardModalElement) return;
        bootstrap.Modal.getOrCreateInstance(discardModalElement).show();
    }

    function requestResetParticipantModal() {
        if (participantProfileMode === "view") return;
        if (!resetConfirmModalElement) return;
        syncResetConfirmModalCopy();
        bootstrap.Modal.getOrCreateInstance(resetConfirmModalElement).show();
    }

    async function getAntiForgeryToken() {
        const response = await fetch("/Security/AntiforgeryToken", {
            method: "GET",
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        });

        const data = await response.json();
        if (!response.ok || !data.success || !data.token) {
            throw new Error(data.message || "Security token could not be generated.");
        }

        return data.token;
    }

    function isResultDtoSuccess(data) {
        return data?.isSuccess === true || data?.success === true;
    }

    function getResultDtoNotice(data, fallback) {
        return data?.noticeTitle || data?.noticeDetails || data?.message || fallback;
    }

    function getResultDtoErrors(data) {
        const errors = Array.isArray(data?.errors) ? data.errors.slice() : [];
        if (data?.noticeDetails && data.noticeDetails !== data?.noticeTitle && !errors.includes(data.noticeDetails)) {
            errors.unshift(data.noticeDetails);
        }

        return errors;
    }

    function buildPayload() {
        const payload = {
            draftId: getNullableValue("teamManagementParticipantDraftId"),
            teamId: getCurrentTeamId(),
            tahapCode: getValue("teamManagementParticipantTahapCode"),
            fullNameAsPerId: getValue("fullNameAsPerIdInput"),
            fullNameGurmukhi: getValue("fullNameGurmukhiInput"),
            idType: getValue("idTypeSelect"),
            nricNumber: getNullableValue("nricInput"),
            passportNumber: getNullableValue("passportNumberInput"),
            passportExpiryDate: getNullableValue("passportExpiryDateInput"),
            mobileCountryCode: getNullableValue("mobileCountryCodeInput"),
            mobileLocalNumber: getNullableValue("mobileLocalNumberInput"),
            email: getNullableValue("emailInput"),
            parentGuardianName: getValue("parentGuardianNameInput"),
            parentGuardianMobileCountryCode: getNullableValue("guardianCountryCodeInput"),
            parentGuardianMobileLocalNumber: getNullableValue("guardianLocalNumberInput"),
            gender: getValue("genderSelect") || getValue("genderHidden"),
            dateOfBirth: getNullableValue("dateOfBirthInput"),
            individualCompetitionCategoryIds: getSelectedGuidValues(individualCompetitionSelectIds),
            teamCompetitionCategoryIds: getSelectedGuidValues(teamCompetitionSelectIds)
        };

        if (participantProfileMode === "edit") {
            payload.participantId = currentParticipantId || null;
        }

        return payload;
    }

    function setSaving(isSaving) {
        if (!saveButton) return;
        setButtonBusy(saveButton, isSaving);
        saveButton.innerHTML = isSaving
            ? "<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\" aria-hidden=\"true\"></span>Saving..."
            : getSubmitButtonText();
        syncParticipantActionButtons();
    }

    function reloadWorkspaceAfterSuccess(teamId) {
        window.TeamManagementRoutes?.navigate?.(teamId);
    }

    async function submitParticipantProfile() {
        if (participantProfileMode !== "add" && participantProfileMode !== "edit") return;

        clearSaveError();
        const teamId = getCurrentTeamId();
        const failureMessage = getSaveFailureMessage();

        if (!teamId) {
            showSaveError("Please select a team first.");
            return;
        }

        if (participantProfileMode === "edit" && !currentParticipantId) {
            showSaveError("Participant could not be updated.");
            return;
        }

        const clientValidationErrors = getClientValidationErrors();
        if (clientValidationErrors.length > 0) {
            showSaveError(failureMessage, clientValidationErrors);
            return;
        }

        setSaving(true);

        try {
            const token = await getAntiForgeryToken();
            const response = await fetch("/TeamManagement?handler=SubmitParticipant", {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin",
                body: JSON.stringify(buildPayload())
            });

            const data = await response.json();
            if (!response.ok || !isResultDtoSuccess(data)) {
                showSaveError(getResultDtoNotice(data, failureMessage), getResultDtoErrors(data));
                setSaving(false);
                return;
            }

            reloadWorkspaceAfterSuccess(teamId);
        } catch (error) {
            showSaveError(error.message || failureMessage);
            setSaving(false);
        }
    }

    function validateAgeBeforeSubmit(event) {
        if (participantProfileMode !== "add" && participantProfileMode !== "edit") return;

        const clickedSaveButton = event.target.closest?.("#teamManagementParticipantSaveButton, #teamManagementParticipantSaveDraftButton");
        if (!clickedSaveButton) return;

        const error = applyAgeValidation();
        if (!error) return;

        event.preventDefault();
        event.stopImmediatePropagation();
        showSaveError(getSaveFailureMessage(), [error]);
    }

    function setupFormFieldValidationClear() {
        if (!form) return;

        form.addEventListener("input", (event) => {
            if (event.target.classList.contains("is-invalid")) {
                event.target.classList.remove("is-invalid");
                form.querySelector("[data-teammanagement-field-error-for='" + event.target.id + "']")?.remove();
            }

            syncParticipantActionButtons();
        });

        form.addEventListener("change", (event) => {
            if (event.target.closest(".team-management-competition-select")) {
                if (participantProfileMode === "view") {
                    clearCompetitionFieldValidation();
                    clearCompetitionTrackers();
                    return;
                }

                validateCompetitionSelectionsLive();
                syncParticipantActionButtons();
                return;
            }

            if (event.target.classList.contains("is-invalid")) {
                event.target.classList.remove("is-invalid");
                form.querySelector("[data-teammanagement-field-error-for='" + event.target.id + "']")?.remove();
            }

            syncParticipantActionButtons();
        });

        document.addEventListener("click", (event) => {
            if (!event.target.closest?.(".app-phone-country-option")) return;
            window.requestAnimationFrame(syncParticipantActionButtons);
        });
    }

    function setupModalEventHandlers() {
        participantModalElement?.addEventListener("hide.bs.modal", (event) => {
            if (allowParticipantModalHide) return;

            event.preventDefault();
            requestCloseParticipantModal();
        });

        participantModalElement?.addEventListener("hidden.bs.modal", () => {
            allowParticipantModalHide = false;
            resetAddParticipantValidationUi();
        });

        participantModalElement?.addEventListener("shown.bs.modal", resetParticipantValidationUiForShownModal);
        document.getElementById("teamManagementParticipantProfileModalCloseButton")?.addEventListener("click", requestCloseParticipantModal);
        document.getElementById("teamManagementParticipantCancelButton")?.addEventListener("click", requestCloseParticipantModal);
        document.getElementById("teamManagementParticipantResetButton")?.addEventListener("click", requestResetParticipantModal);
        document.getElementById("teamManagementParticipantContinueEditingButton")?.addEventListener("click", () => {
            bootstrap.Modal.getInstance(discardModalElement)?.hide();
        });
        document.getElementById("teamManagementParticipantDiscardChangesButton")?.addEventListener("click", () => {
            allowParticipantModalHide = true;
            bootstrap.Modal.getInstance(discardModalElement)?.hide();
            bootstrap.Modal.getInstance(participantModalElement)?.hide();
            resetParticipantModalForm();
            markParticipantModalClean();
        });
        document.getElementById("teamManagementParticipantKeepEditingResetButton")?.addEventListener("click", () => {
            bootstrap.Modal.getInstance(resetConfirmModalElement)?.hide();
        });
        document.getElementById("teamManagementParticipantConfirmResetButton")?.addEventListener("click", () => {
            bootstrap.Modal.getInstance(resetConfirmModalElement)?.hide();
            if (participantProfileMode === "edit") {
                restoreOriginalParticipantDetails();
                return;
            }

            resetParticipantModalForm();
            loadCompetitionAssignments();
            markParticipantModalClean();
        });
        form?.addEventListener("submit", (event) => {
            event.preventDefault();
        });
    }

    function init() {
        if (initialized) return;
        initialized = true;

        ensureDateOfBirthAgePreview();
        setupFormFieldValidationClear();
        setupModalEventHandlers();

        document.addEventListener("click", validateAgeBeforeSubmit, true);
        saveButton?.addEventListener("click", submitParticipantProfile);

        syncParticipantActionButtons();
    }

    window.TeamManagementParticipantProfileModal = {
        open: openParticipantModal,
        markClean: markParticipantModalClean,
        refreshCompetitionTrackers,
        syncActionButtons: syncParticipantActionButtons,
        setButtonBusy: setButtonBusy,
        isDirty: isParticipantModalDirty,
        setActionButtonsSuppressed: setActionButtonsSuppressed,
        setResetButtonLabel: setResetButtonLabel
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
