(() => {
    const selectedTeamStorageKey = "pbm.teamManagement.selectedTeamId";
    const individualCompetitionSelectIds = ["individualCompetitionSlot1Select", "individualCompetitionSlot2Select"];
    const teamCompetitionSelectIds = ["teamCompetitionSlot1Select", "teamCompetitionSlot2Select"];

    let draftsPanelElement = null;
    let draftsContentElement = null;
    let draftsStatusBadgeElement = null;
    let draftsErrorElement = null;
    let draftsLoadingElement = null;
    let selectedDeleteDraftId = "";

    function getValue(id) {
        return document.getElementById(id)?.value?.trim() || "";
    }

    function getNullableValue(id) {
        const value = getValue(id);
        return value === "" ? null : value;
    }

    function setValue(id, value) {
        const element = document.getElementById(id);
        if (!element) return;

        element.value = value || "";
        element.dispatchEvent(new Event("input", { bubbles: true }));
        element.dispatchEvent(new Event("change", { bubbles: true }));
    }

    function getSelectedGuidValues(selectIds) {
        return selectIds
            .map((id) => getValue(id))
            .filter((value) => value !== "");
    }

    function getCurrentTeamId() {
        return getValue("teamManagementParticipantTeamId") ||
            getValue("teamManagementSelector") ||
            document.querySelector("[data-team-management-workspace='true']")?.getAttribute("data-team-id") ||
            "";
    }

    function cacheDraftElements() {
        draftsPanelElement = document.getElementById("teamManagementDraftApplicationsPanel");
        draftsContentElement = document.getElementById("teamManagementDraftApplicationsContent");
        draftsStatusBadgeElement = document.getElementById("teamManagementDraftApplicationsStatusBadge");
        draftsErrorElement = document.getElementById("teamManagementDraftApplicationsError");
        draftsLoadingElement = document.getElementById("teamManagementDraftApplicationsLoading");
    }

    function ensureDraftIdInput() {
        let input = document.getElementById("teamManagementParticipantDraftId");
        if (input) return input;

        const form = document.getElementById("teamManagementParticipantForm");
        if (!form) return null;

        input = document.createElement("input");
        input.type = "hidden";
        input.id = "teamManagementParticipantDraftId";
        form.prepend(input);
        return input;
    }

    function clearDraftIdInput() {
        const input = ensureDraftIdInput();
        if (input) input.value = "";
    }

    function escapeHtml(value) {
        return String(value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function ensureDraftAlert() {
        let alert = document.getElementById("teamManagementParticipantDraftAlert");
        if (alert) return alert;

        alert = document.createElement("div");
        alert.id = "teamManagementParticipantDraftAlert";
        alert.className = "alert alert-danger d-none";

        const form = document.getElementById("teamManagementParticipantForm");
        const requiredLegend = form?.querySelector(".app-required-legend");
        if (requiredLegend) {
            requiredLegend.insertAdjacentElement("afterend", alert);
        } else {
            form?.prepend(alert);
        }

        return alert;
    }

    function showDraftError(message, errors) {
        const alert = ensureDraftAlert();
        const items = Array.isArray(errors) && errors.length > 0
            ? "<ul class=\"mb-0 mt-2\">" + errors.map((error) => "<li>" + escapeHtml(error) + "</li>").join("") + "</ul>"
            : "";

        alert.className = "alert alert-danger";
        alert.innerHTML = "<div class=\"fw-semibold\">" + escapeHtml(message || "Draft could not be saved.") + "</div>" + items;
        alert.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function clearDraftsError() {
        if (draftsErrorElement) {
            draftsErrorElement.textContent = "";
            draftsErrorElement.classList.add("d-none");
        }
    }

    function getDeleteDraftConfirmModal() {
        const modal = document.getElementById("teamManagementDeleteDraftConfirmModal");
        if (!modal || !window.bootstrap) return null;

        return bootstrap.Modal.getOrCreateInstance(modal);
    }

    function setDeletingDraft(isDeleting) {
        const confirmButton = document.getElementById("teamManagementConfirmDeleteDraftButton");
        const keepButton = document.getElementById("teamManagementKeepDraftButton");

        if (confirmButton) {
            confirmButton.disabled = isDeleting;
            confirmButton.innerHTML = isDeleting
                ? "<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\" aria-hidden=\"true\"></span>Deleting..."
                : "Delete draft";
        }

        if (keepButton) {
            keepButton.disabled = isDeleting;
        }
    }

    function requestDeleteDraft(draftId) {
        if (!draftId) return;

        selectedDeleteDraftId = draftId;
        clearDraftsError();
        setDeletingDraft(false);
        getDeleteDraftConfirmModal()?.show();
    }

    async function deleteSelectedDraft() {
        const draftId = selectedDeleteDraftId;
        const teamId = getCurrentTeamId();

        if (!draftId || !teamId) {
            showDraftsError("Draft could not be deleted.");
            return;
        }

        setDeletingDraft(true);

        try {
            const token = await getAntiForgeryToken();
            const response = await fetch("/TeamManagement?handler=DeleteDraft", {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin",
                body: JSON.stringify({
                    draftId,
                    teamId
                })
            });

            const data = await response.json();
            if (!response.ok || !isResultDtoSuccess(data)) {
                getDeleteDraftConfirmModal()?.hide();
                showDraftsError(getResultDtoNotice(data, "Draft could not be deleted."));
                setDeletingDraft(false);
                return;
            }

            window.TeamManagementRoutes?.navigate?.(teamId);
        } catch (error) {
            getDeleteDraftConfirmModal()?.hide();
            showDraftsError(error.message || "Draft could not be deleted.");
            setDeletingDraft(false);
        }
    }

    function showDraftSuccess(message) {
        const alert = ensureDraftAlert();
        alert.className = "alert alert-success";
        alert.innerHTML = "<div class=\"fw-semibold\">" + escapeHtml(message || "Draft saved successfully.") + "</div>";
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

    function clearDraftAlert() {
        const alert = document.getElementById("teamManagementParticipantDraftAlert");
        if (!alert) return;

        alert.innerHTML = "";
        alert.className = "alert alert-danger d-none";
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

    function buildDraftPayload() {
        return {
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
    }

    function validateDraftBeforeSave(payload) {
        if (payload.fullNameAsPerId || payload.fullNameGurmukhi) return [];
        return ["Please enter at least one participant name before saving as draft."];
    }

    function setDraftSaving(isSaving) {
        const draftButton = document.getElementById("teamManagementParticipantSaveDraftButton");
        if (!draftButton) return;

        window.TeamManagementParticipantProfileModal?.setButtonBusy?.(draftButton, isSaving);
        draftButton.innerHTML = isSaving
            ? "<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\" aria-hidden=\"true\"></span>Saving draft..."
            : "Save as Draft";

        if (isSaving) {
            draftButton.disabled = true;
            return;
        }

        window.TeamManagementParticipantProfileModal?.syncActionButtons?.();
    }

    function syncDraftButtonState() {
        const draftButton = document.getElementById("teamManagementParticipantSaveDraftButton");
        if (!draftButton) return;

        draftButton.innerHTML = "Save as Draft";
        window.TeamManagementParticipantProfileModal?.syncActionButtons?.();
    }

    async function saveFromModal() {
        clearDraftAlert();

        const payload = buildDraftPayload();
        const validationErrors = validateDraftBeforeSave(payload);
        if (validationErrors.length > 0) {
            showDraftError("Draft could not be saved.", validationErrors);
            return;
        }

        if (!payload.teamId) {
            showDraftError("Please select a team first.");
            return;
        }

        setDraftSaving(true);

        try {
            const token = await getAntiForgeryToken();
            const response = await fetch("/TeamManagement?handler=SaveDraft", {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin",
                body: JSON.stringify(payload)
            });

            const data = await response.json();
            if (!response.ok || !isResultDtoSuccess(data)) {
                showDraftError(getResultDtoNotice(data, "Draft could not be saved."), getResultDtoErrors(data));
                setDraftSaving(false);
                return;
            }

            showDraftSuccess(getResultDtoNotice(data, "Draft saved successfully."));

            window.setTimeout(() => {
                window.TeamManagementRoutes.navigate(payload.teamId);
            }, 350);
        } catch (error) {
            showDraftError(error.message || "Draft could not be saved.");
            setDraftSaving(false);
        }
    }

    function setDraftsLoading(isLoading) {
        if (draftsLoadingElement) draftsLoadingElement.classList.toggle("d-none", !isLoading);
        if (draftsStatusBadgeElement && isLoading) draftsStatusBadgeElement.textContent = "Loading";
    }

    function showDraftsError(message) {
        if (!draftsPanelElement) return;

        draftsPanelElement.classList.remove("d-none");
        setDraftsLoading(false);

        if (draftsErrorElement) {
            draftsErrorElement.textContent = message || "Draft applications could not be loaded.";
            draftsErrorElement.classList.remove("d-none");
        }

        if (draftsContentElement) draftsContentElement.classList.add("d-none");
        if (draftsStatusBadgeElement) draftsStatusBadgeElement.textContent = "Load failed";
    }

    function parseDraftPayload(payloadJson) {
        if (!payloadJson) return {};
        if (typeof payloadJson === "object") return payloadJson;

        try {
            return JSON.parse(payloadJson) || {};
        } catch {
            return {};
        }
    }

    function setSelectValueWhenAvailable(selectId, value, attempt) {
        const select = document.getElementById(selectId);
        if (!select || !value) return Promise.resolve();

        const hasOption = Array.from(select.options).some((option) => option.value === value);
        if (hasOption) {
            select.value = value;
            select.dispatchEvent(new Event("change", { bubbles: true }));
            return Promise.resolve();
        }

        if ((attempt || 0) >= 20) return Promise.resolve();

        return new Promise((resolve) => {
            window.setTimeout(() => {
                resolve(setSelectValueWhenAvailable(selectId, value, (attempt || 0) + 1));
            }, 100);
        });
    }

    function applyDraftToModal(draft) {
        const payload = parseDraftPayload(draft.draftPayloadJson);
        const draftIdInput = ensureDraftIdInput();
        if (draftIdInput) draftIdInput.value = draft.id || "";

        window.TeamManagementParticipantProfileModal?.setActionButtonsSuppressed?.(true);
        window.TeamManagementParticipantProfileModal?.setResetButtonLabel?.("Undo changes");

        setValue("teamManagementParticipantTeamId", draft.teamId || "");
        setValue("teamManagementParticipantTahapCode", draft.tahapCode || "");
        setValue("fullNameAsPerIdInput", payload.FullNameAsPerId || "");
        setValue("fullNameGurmukhiInput", payload.FullNameGurmukhi || "");
        setValue("idTypeSelect", payload.IdType || "NRIC");
        setValue("nricInput", payload.NricNumber || "");
        setValue("passportNumberInput", payload.PassportNumber || "");
        setValue("passportExpiryDateInput", payload.PassportExpiryDate || "");
        setValue("mobileCountryCodeInput", payload.MobileCountryCode || "+60");
        setValue("mobileLocalNumberInput", payload.MobileLocalNumber || "");
        setValue("emailInput", payload.Email || "");
        setValue("parentGuardianNameInput", payload.ParentGuardianName || "");
        setValue("guardianCountryCodeInput", payload.ParentGuardianMobileCountryCode || "+60");
        setValue("guardianLocalNumberInput", payload.ParentGuardianMobileLocalNumber || "");
        setValue("genderSelect", payload.Gender || "");
        setValue("genderHidden", payload.Gender || "");
        setValue("dateOfBirthInput", payload.DateOfBirth || "");

        const individualIds = draft.individualCompetitionCategoryIds || [];
        const teamIds = draft.teamCompetitionCategoryIds || [];

        const selectionPromises = [
            setSelectValueWhenAvailable("individualCompetitionSlot1Select", individualIds[0] || ""),
            setSelectValueWhenAvailable("individualCompetitionSlot2Select", individualIds[1] || ""),
            setSelectValueWhenAvailable("teamCompetitionSlot1Select", teamIds[0] || ""),
            setSelectValueWhenAvailable("teamCompetitionSlot2Select", teamIds[1] || "")
        ];

        Promise.all(selectionPromises)
            .then(() => window.TeamManagementParticipantProfileModal?.refreshCompetitionTrackers?.())
            .finally(() => {
                window.TeamManagementParticipantProfileModal?.markClean?.();
                window.TeamManagementParticipantProfileModal?.setActionButtonsSuppressed?.(false);
            });
    }

    async function continueEditingDraft(draftId) {
        const teamId = getCurrentTeamId();
        if (!teamId || !draftId) return;

        try {
            const response = await fetch("/TeamManagement?handler=DraftDetails&teamId=" + encodeURIComponent(teamId) + "&draftId=" + encodeURIComponent(draftId), {
                method: "GET",
                headers: { "Accept": "application/json" },
                credentials: "same-origin"
            });

            const data = await response.json();
            if (!response.ok || !isResultDtoSuccess(data) || !data.draft) {
                showDraftsError(getResultDtoNotice(data, "Draft could not be loaded."));
                return;
            }

            const draft = data.draft;
            window.TeamManagementParticipantProfileModal?.open({
                mode: "add",
                teamId: draft.teamId,
                tahapCode: draft.tahapCode,
                tahapName: draft.tahapName || draft.tahapCode
            });

            window.setTimeout(() => applyDraftToModal(draft), 250);
        } catch (error) {
            showDraftsError(error.message || "Draft could not be loaded.");
        }
    }

    function setupDraftClickHandler() {
        window.addEventListener("click", (event) => {
            const draftButton = event.target.closest?.("#teamManagementParticipantSaveDraftButton");
            if (!draftButton) return;

            event.preventDefault();
            event.stopPropagation();
            event.stopImmediatePropagation();

            saveFromModal();
        }, true);
    }

    function setupDraftActionHandler() {
        document.addEventListener("click", (event) => {
            const actionButton = event.target.closest?.("[data-draft-action]");
            if (!actionButton) return;

            event.preventDefault();
            event.stopPropagation();

            const action = actionButton.getAttribute("data-draft-action");
            const draftId = actionButton.getAttribute("data-draft-id");

            if (action === "continue") {
                continueEditingDraft(draftId);
                return;
            }

            if (action === "cancel") {
                requestDeleteDraft(draftId);
            }
        });

    }

    function setupAddParticipantDraftReset() {
        document.addEventListener("click", (event) => {
            const addButton = event.target.closest?.("[data-add-participant-tahap]");
            if (!addButton) return;
            clearDraftIdInput();
        });
    }

    function setupDeleteDraftConfirmModal() {
        document.getElementById("teamManagementKeepDraftButton")?.addEventListener("click", () => {
            selectedDeleteDraftId = "";
            setDeletingDraft(false);
        });

        document.getElementById("teamManagementConfirmDeleteDraftButton")?.addEventListener("click", deleteSelectedDraft);
    }

    function initializeServerRenderedDrafts() {
        setDraftsLoading(false);

        if (draftsErrorElement) {
            draftsErrorElement.classList.add("d-none");
        }

        if (draftsContentElement && draftsContentElement.dataset.draftCount !== "0") {
            draftsContentElement.classList.remove("d-none");
        }

        if (draftsStatusBadgeElement && draftsContentElement?.dataset.draftCount) {
            const count = Number(draftsContentElement.dataset.draftCount || 0);
            draftsStatusBadgeElement.textContent = count + " draft" + (count === 1 ? "" : "s");
        }
    }

    function init() {
        cacheDraftElements();
        ensureDraftIdInput();
        syncDraftButtonState();
        initializeServerRenderedDrafts();
        setupDraftActionHandler();
        setupDeleteDraftConfirmModal();
        setupAddParticipantDraftReset();
    }

    window.TeamManagementParticipantDraft = {
        syncDraftButtonState,
        saveFromModal,
        continueEditingDraft,
        clearDraftAlert
    };

    setupDraftClickHandler();

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
