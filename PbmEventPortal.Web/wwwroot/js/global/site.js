(() => {

    const defaultLoadingMinimumMilliseconds = 500;

    function navigateWithLoading(url, message, minimumMilliseconds) {
        if (!url) return;

        const delay = Number.isFinite(Number(minimumMilliseconds))
            ? Number(minimumMilliseconds)
            : defaultLoadingMinimumMilliseconds;

        showLoading(message || "Loading...");

        window.setTimeout(() => {
            window.location.href = url;
        }, delay);
    }
    function getOverlay() {
        return document.getElementById("appLoadingOverlay");
    }

    function getMessageElement() {
        return document.getElementById("appLoadingMessage");
    }

    function showLoading(message) {
        const overlay = getOverlay();
        if (!overlay) return;

        const messageElement = getMessageElement();
        if (messageElement) {
            messageElement.textContent = message || "Loading...";
        }

        overlay.classList.remove("d-none");
        overlay.setAttribute("aria-hidden", "false");
    }

    function hideLoading() {
        const overlay = getOverlay();
        if (!overlay) return;

        overlay.classList.add("d-none");
        overlay.setAttribute("aria-hidden", "true");
    }

    function setupLoadingTriggers() {
        document.addEventListener("click", (event) => {
            const trigger = event.target.closest?.("[data-app-loading]");
            if (!trigger) return;

            if (trigger.matches("button") && trigger.disabled) return;
            if (trigger.matches("a") && trigger.getAttribute("href") === "#") return;
            if (event.defaultPrevented) return;

            const message = trigger.getAttribute("data-app-loading") || "Loading...";

            if (trigger.matches("a")) {
                const href = trigger.getAttribute("href");

                if (
                    href &&
                    !trigger.hasAttribute("download") &&
                    trigger.getAttribute("target") !== "_blank" &&
                    !event.ctrlKey &&
                    !event.metaKey &&
                    !event.shiftKey &&
                    !event.altKey
                ) {
                    event.preventDefault();
                    navigateWithLoading(href, message);
                    return;
                }
            }

            showLoading(message);
        });
    }

    function setupSidebarToggle() {
        const sidebar = document.getElementById("appSidebar");
        const toggle = document.querySelector("[data-app-sidebar-toggle='true']");
        if (!sidebar || !toggle) return;

        function setExpanded(isExpanded) {
            sidebar.classList.toggle("is-expanded", isExpanded);
            toggle.setAttribute("aria-expanded", isExpanded ? "true" : "false");
            toggle.setAttribute("aria-label", isExpanded ? "Collapse sidebar" : "Expand sidebar");
        }

        setExpanded(false);

        toggle.addEventListener("click", () => {
            setExpanded(!sidebar.classList.contains("is-expanded"));
        });

        document.addEventListener("click", (event) => {
            if (!sidebar.classList.contains("is-expanded")) return;
            if (sidebar.contains(event.target)) return;

            setExpanded(false);
        });

        document.addEventListener("keydown", (event) => {
            if (event.key !== "Escape") return;
            if (!sidebar.classList.contains("is-expanded")) return;

            setExpanded(false);
        });
    }

    window.AppLoading = {
        show: showLoading,
        hide: hideLoading,
        navigate: navigateWithLoading
    };

    document.addEventListener("DOMContentLoaded", () => {
        window.FormHelpers?.setupPasswordRevealButtons?.();
        window.FormHelpers?.setupSearchableSelects?.();
        window.FormHelpers?.setupBoundedNumberInputs?.();

        setupLoadingTriggers();
        setupSidebarToggle();
        hideLoading();
    });
})();
