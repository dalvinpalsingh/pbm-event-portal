(() => {
    var selectedTeamStorageKey = "pbm.teamManagement.selectedTeamId";
    var workspace = document.querySelector("[data-team-management-workspace='true']");
    var teamSelector = document.getElementById("teamManagementSelector");
    var currentTeamId = workspace ? (workspace.getAttribute("data-team-id") || "") : (teamSelector ? teamSelector.value : "");
    var workspaceContent = document.getElementById("teamManagementWorkspaceContent");
    var noticeCard = document.getElementById("teamManagementNoticeCard");
    var panel = document.getElementById("teamManagementParticipantsPanel");
    var loading = document.getElementById("teamManagementParticipantsLoading");
    var error = document.getElementById("teamManagementParticipantsError");
    var content = document.getElementById("teamManagementParticipantsContent");
    var statusBadge = document.getElementById("teamManagementParticipantsStatusBadge");
    var selectedDeleteParticipantId = "";

    function isTeamManagementPage() {
        return workspace !== null || content !== null;
    }

    if (!isTeamManagementPage()) return;

    function teamManagementRoute(teamId) {
        return teamId ? "/TeamManagement/" + encodeURIComponent(teamId) : "/TeamManagement";
    }

    function navigateToTeamManagement(teamId, message) {
        const url = teamManagementRoute(teamId);

        if (window.AppLoading?.navigate) {
            window.AppLoading.navigate(url, message || "Loading team workspace...");
            return;
        }

        window.location.href = url;
    }

    window.TeamManagementRoutes = {
        teamManagement: teamManagementRoute,
        navigate: navigateToTeamManagement
    };

    function ensureTeamRouteInUrl() {
        if (!currentTeamId || !window.history?.replaceState) return;

        var expectedPath = teamManagementRoute(currentTeamId);
        if (window.location.pathname.toLowerCase() === expectedPath.toLowerCase()) return;

        window.history.replaceState(null, "", expectedPath);
    }

    function getProfileHelpers() {
        return window.ProfileHelpers || {};
    }

    function formatDateWithAge(dateValue) {
        var helper = getProfileHelpers().formatDateWithYearAge;
        return typeof helper === "function" ? helper(dateValue) : dateValue || "";
    }

    function findDateTextNode(container) {
        if (!container) return null;

        return Array.from(container.childNodes).find(function (node) {
            return node.nodeType === Node.TEXT_NODE && /\d{4}-\d{2}-\d{2}/.test(node.textContent || "");
        });
    }

    function enhanceParticipantListAges(root) {
        (root || document).querySelectorAll(".bi-cake2-fill").forEach(function (icon) {
            var dateContainer = icon.parentElement;
            if (!dateContainer || dateContainer.dataset.ageEnhanced === "true") return;

            var dateTextNode = findDateTextNode(dateContainer);
            var dateValue = dateTextNode?.textContent?.trim();
            if (!dateValue) return;

            var formatted = formatDateWithAge(dateValue);
            if (!formatted || formatted === dateValue) return;

            dateTextNode.textContent = formatted;
            dateContainer.dataset.ageEnhanced = "true";
        });
    }

    function observeParticipantList() {
        if (!content || content.dataset.ageObserverAttached === "true") return;

        content.dataset.ageObserverAttached = "true";
        enhanceParticipantListAges(content);

        var observer = new MutationObserver(function () {
            enhanceParticipantListAges(content);
        });

        observer.observe(content, { childList: true, subtree: true });
    }

    function initializeTooltips(root) {
        if (!window.bootstrap || !root) return;
        root.querySelectorAll("[data-bs-toggle='tooltip']").forEach(function (element) {
            bootstrap.Tooltip.getOrCreateInstance(element);
        });
    }

    function setupDashboardScrollCards() {
        document.querySelectorAll("[data-teammanagement-scroll-target]").forEach(function (card) {
            if (card.dataset.scrollHandlerAttached === "true") return;

            card.dataset.scrollHandlerAttached = "true";
            card.addEventListener("click", function (event) {
                event.preventDefault();

                var count = Number(card.getAttribute("data-teammanagement-scroll-count") || 0);
                if (count <= 0) return;

                var targetId = card.getAttribute("data-teammanagement-scroll-target");
                var target = targetId ? document.getElementById(targetId) : null;
                if (!target || target.classList.contains("d-none")) return;

                target.scrollIntoView({ behavior: "smooth", block: "start" });
            });
        });
    }

    function setupCollapsibleSections() {
        const collapsibleSections = {};

        document.querySelectorAll("[data-teammanagement-collapsible='true']").forEach(function (section) {
            if (section.dataset.collapseHandlerAttached === "true") return;

            var toggle = section.querySelector("[data-teammanagement-collapse-toggle='true']");
            var body = section.querySelector("[data-teammanagement-collapse-body='true']");
            var chevron = section.querySelector(".teammanagement-collapse-chevron");
            if (!toggle || !body) return;

            function updateChevron(isCollapsed) {
                if (!chevron) return;
                chevron.classList.toggle("bi-chevron-down", isCollapsed);
                chevron.classList.toggle("bi-chevron-up", !isCollapsed);
            }

            function finalizeExpanded(event) {
                if (event.propertyName !== "max-height" || section.dataset.collapsed === "true") return;
                body.style.maxHeight = "none";
            }

            function setCollapsed(isCollapsed, animate) {
                section.dataset.collapsed = isCollapsed ? "true" : "false";
                section.classList.toggle("is-collapsed", isCollapsed);
                section.classList.toggle("is-expanded", !isCollapsed);
                toggle.setAttribute("aria-expanded", isCollapsed ? "false" : "true");
                updateChevron(isCollapsed);

                body.removeEventListener("transitionend", finalizeExpanded);

                if (!animate) {
                    body.style.maxHeight = isCollapsed ? "0px" : "none";
                    body.style.opacity = isCollapsed ? "0" : "1";
                    return;
                }

                if (isCollapsed) {
                    if (!body.style.maxHeight || body.style.maxHeight === "none") {
                        body.style.maxHeight = body.scrollHeight + "px";
                    }

                    body.offsetHeight;
                    body.style.maxHeight = "0px";
                    body.style.opacity = "0";
                    return;
                }

                body.style.maxHeight = "0px";
                body.style.opacity = "0";

                window.requestAnimationFrame(function () {
                    body.style.maxHeight = body.scrollHeight + "px";
                    body.style.opacity = "1";
                    body.addEventListener("transitionend", finalizeExpanded);
                });
            }

            function isStickyHeaderStuck() {
                if (!toggle.classList.contains("teammanagement-sticky-section-header")) return false;

                var stickyTop = parseFloat(window.getComputedStyle(toggle).top) || 0;
                var headerRect = toggle.getBoundingClientRect();
                var sectionRect = section.getBoundingClientRect();

                return sectionRect.top < stickyTop && Math.abs(headerRect.top - stickyTop) <= 2;
            }

            function scrollHeaderIntoViewAfterCollapse() {
                var hasScrolled = false;
                var fallbackTimerId = null;

                function scrollOnce() {
                    if (hasScrolled) return;

                    hasScrolled = true;
                    body.removeEventListener("transitionend", handleTransitionEnd);
                    if (fallbackTimerId) window.clearTimeout(fallbackTimerId);

                    window.requestAnimationFrame(function () {
                        section.scrollIntoView({ behavior: "smooth", block: "start" });
                    });
                }

                function handleTransitionEnd(event) {
                    if (event.propertyName !== "max-height") return;

                    scrollOnce();
                }

                body.addEventListener("transitionend", handleTransitionEnd);
                fallbackTimerId = window.setTimeout(scrollOnce, 260);
            }

            section.dataset.collapseHandlerAttached = "true";
            setCollapsed(section.dataset.collapsed === "true", false);
            collapsibleSections[section.id] = {
                expand: function () { setCollapsed(false, true); },
                isCollapsed: function () { return section.dataset.collapsed === "true"; }
            };

            toggle.addEventListener("click", function () {
                var shouldCollapse = section.dataset.collapsed !== "true";
                var shouldScrollAfterCollapse = shouldCollapse &&
                    toggle.classList.contains("teammanagement-sticky-section-header") &&
                    !isStickyHeaderStuck();

                setCollapsed(shouldCollapse, true);

                if (shouldScrollAfterCollapse) {
                    scrollHeaderIntoViewAfterCollapse();
                }
            });
        });

        document.querySelectorAll("[data-teammanagement-expand-section]").forEach(function (card) {
            if (card.dataset.expandHandlerAttached === "true") return;

            card.dataset.expandHandlerAttached = "true";
            card.addEventListener("click", function (event) {
                if (event.target.closest("a, button")) return;

                var sectionId = card.getAttribute("data-teammanagement-expand-section");
                var section = sectionId ? collapsibleSections[sectionId] : null;
                if (!section || !section.isCollapsed()) return;

                section.expand();
            });
        });
    }

    function setLoading(isLoading) {
        if (loading) loading.classList.toggle("d-none", !isLoading);
        if (statusBadge && isLoading) statusBadge.textContent = "Loading";
    }

    function showParticipantsError(message) {
        if (!error) return;

        error.textContent = message || "Participant action could not be completed.";
        error.classList.remove("d-none");
        error.scrollIntoView({ behavior: "smooth", block: "start" });
    }

    function clearParticipantsError() {
        if (!error) return;

        error.textContent = "";
        error.classList.add("d-none");
    }

    function getDeleteParticipantConfirmModal() {
        var modal = document.getElementById("teamManagementDeleteParticipantConfirmModal");
        if (!modal || !window.bootstrap) return null;

        return bootstrap.Modal.getOrCreateInstance(modal);
    }

    function setDeletingParticipant(isDeleting) {
        var confirmButton = document.getElementById("teamManagementConfirmDeleteParticipantButton");
        var keepButton = document.getElementById("teamManagementKeepParticipantButton");

        if (confirmButton) {
            confirmButton.disabled = isDeleting;
            confirmButton.innerHTML = isDeleting
                ? "<span class=\"spinner-border spinner-border-sm me-2\" role=\"status\" aria-hidden=\"true\"></span>Deleting..."
                : "Delete Participant";
        }

        if (keepButton) {
            keepButton.disabled = isDeleting;
        }
    }

    function requestDeleteParticipant(participantId) {
        if (!participantId) return;

        selectedDeleteParticipantId = participantId;
        clearParticipantsError();
        setDeletingParticipant(false);
        getDeleteParticipantConfirmModal()?.show();
    }

    async function getAntiForgeryToken() {
        var response = await fetch("/Security/AntiforgeryToken", {
            method: "GET",
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        });

        var data = await response.json();
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

    async function deleteSelectedParticipant() {
        var participantId = selectedDeleteParticipantId;

        if (!currentTeamId || !participantId) {
            showParticipantsError("Participant could not be deleted.");
            return;
        }

        setDeletingParticipant(true);

        try {
            var token = await getAntiForgeryToken();
            var url = "/TeamManagement?handler=Participant&teamId=" +
                encodeURIComponent(currentTeamId) +
                "&participantId=" +
                encodeURIComponent(participantId);

            var response = await fetch(url, {
                method: "DELETE",
                headers: {
                    "Accept": "application/json",
                    "RequestVerificationToken": token
                },
                credentials: "same-origin"
            });

            var data = await response.json();
            if (!response.ok || !isResultDtoSuccess(data)) {
                getDeleteParticipantConfirmModal()?.hide();
                showParticipantsError(getResultDtoNotice(data, "Participant could not be deleted."));
                setDeletingParticipant(false);
                return;
            }

            window.TeamManagementRoutes?.navigate?.(currentTeamId);
        } catch (deleteError) {
            getDeleteParticipantConfirmModal()?.hide();
            showParticipantsError(deleteError.message || "Participant could not be deleted.");
            setDeletingParticipant(false);
        }
    }

    async function loadParticipantDetails(participantId) {
        if (!currentTeamId || !participantId) {
            throw new Error("Participant could not be loaded.");
        }

        var url = "/TeamManagement?handler=Participant&teamId=" +
            encodeURIComponent(currentTeamId) +
            "&participantId=" +
            encodeURIComponent(participantId);

        var response = await fetch(url, {
            method: "GET",
            headers: { "Accept": "application/json" },
            credentials: "same-origin"
        });

        var data = await response.json();
        if (!response.ok || !isResultDtoSuccess(data)) {
            throw new Error(getResultDtoNotice(data, "Participant could not be loaded."));
        }

        return data;
    }

    async function openParticipantProfile(mode, participantId) {
        clearParticipantsError();

        try {
            var data = await loadParticipantDetails(participantId);
            window.TeamManagementParticipantProfileModal?.open({
                mode: mode,
                participant: data
            });
        } catch (loadError) {
            showParticipantsError(loadError.message || "Participant could not be loaded.");
        }
    }

    function initializeServerRenderedParticipants() {
        if (!currentTeamId) return;

        if (panel) panel.classList.remove("d-none");
        setLoading(false);
        if (error) error.classList.add("d-none");

        if (content) {
            content.classList.remove("d-none");
            initializeTooltips(content);
            enhanceParticipantListAges(content);
        }

        if (statusBadge && content?.dataset.participantCount) {
            var count = Number(content.dataset.participantCount || 0);
            statusBadge.textContent = count + " participant" + (count === 1 ? "" : "s");
        }
    }

    function restoreSelectedTeamFromSession() {
        if (!teamSelector) return;

        var selectedTeamId = sessionStorage.getItem(selectedTeamStorageKey);
        if (!selectedTeamId) return;

        sessionStorage.removeItem(selectedTeamStorageKey);

        var hasOption = Array.from(teamSelector.options).some(function (option) {
            return option.value === selectedTeamId;
        });

        if (!hasOption) return;

        teamSelector.value = selectedTeamId;
        navigateToTeamManagement(selectedTeamId);
    }

    function reloadRestoredSystemAdminTeam() {
        window.setTimeout(function () {
            if (!teamSelector || !teamSelector.value) return;

            var workspaceVisible = workspaceContent && !workspaceContent.classList.contains("d-none");
            var noticeVisible = noticeCard && !noticeCard.classList.contains("d-none");

            if (workspaceVisible && !noticeVisible) return;

            navigateToTeamManagement(teamSelector.value);
        }, 0);
    }

    if (content) {
        content.addEventListener("click", function (event) {
            var participantActionButton = event.target.closest("[data-participant-action]");
            if (!participantActionButton) return;

            event.preventDefault();
            event.stopPropagation();

            var action = participantActionButton.getAttribute("data-participant-action");
            if (action === "add") {
                window.TeamManagementParticipantProfileModal?.open({
                    mode: "add",
                    teamId: currentTeamId,
                    tahapCode: participantActionButton.getAttribute("data-add-participant-tahap"),
                    tahapName: participantActionButton.getAttribute("data-add-participant-tahap-name")
                });
                return;
            }

            if (action === "view") {
                openParticipantProfile("view", participantActionButton.getAttribute("data-participant-id"));
                return;
            }

            if (action === "edit") {
                openParticipantProfile("edit", participantActionButton.getAttribute("data-participant-id"));
                return;
            }

            if (action === "delete") {
                requestDeleteParticipant(participantActionButton.getAttribute("data-participant-id"));
                return;
            }

            window.alert(action + " participant is coming next.");
        });
    }

    var keepParticipantButton = document.getElementById("teamManagementKeepParticipantButton");
    if (keepParticipantButton) {
        keepParticipantButton.addEventListener("click", function () {
            selectedDeleteParticipantId = "";
            setDeletingParticipant(false);
        });
    }

    var confirmDeleteParticipantButton = document.getElementById("teamManagementConfirmDeleteParticipantButton");
    if (confirmDeleteParticipantButton) {
        confirmDeleteParticipantButton.addEventListener("click", function () {
            deleteSelectedParticipant();
        });
    }

    if (teamSelector) {
        teamSelector.addEventListener("change", function () {
            navigateToTeamManagement(teamSelector.value);
        });
    }

    ensureTeamRouteInUrl();
    restoreSelectedTeamFromSession();
    reloadRestoredSystemAdminTeam();
    setupDashboardScrollCards();
    setupCollapsibleSections();
    observeParticipantList();
    initializeServerRenderedParticipants();
})();
