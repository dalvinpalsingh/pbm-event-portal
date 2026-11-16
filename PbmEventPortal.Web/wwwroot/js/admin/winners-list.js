(() => {
    let currentWorkspace = null;

    function isWinnersListPage() {
        return document.querySelector("[data-winners-list-page='true']") !== null;
    }

    function getElementById(id) {
        return document.getElementById(id);
    }

    function escapeHtml(value) {
        return String(value || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    function pointsLabel(points) {
        return Number(points) === 1 ? "point" : "points";
    }

    function formatWholeScore(value) {
        const number = Number(value || 0);
        return Number.isInteger(number) ? String(number) : number.toFixed(2);
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('#winnersAntiForgeryForm input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : "";
    }

    function setText(id, value) {
        const element = getElementById(id);
        if (element) element.textContent = value;
    }

    function setHtml(id, value) {
        const element = getElementById(id);
        if (element) element.innerHTML = value;
    }

    function buildParticipantNamesHtml(participants) {
        if (!participants || participants.length === 0) {
            return "<span class=\"text-muted\">No participants</span>";
        }

        return participants.map(function (participant) {
            return "<div>" + escapeHtml(participant.participantName) + "</div>";
        }).join("");
    }

    function buildCompetitionWinnerRowsHtml(rows) {
        if (!rows || rows.length === 0) {
            return "<tr><td colspan=\"6\" class=\"text-muted\">No winner rows found.</td></tr>";
        }

        return rows.map(function (row) {
            return "<tr>" +
                "<td class=\"text-center text-nowrap align-middle fw-semibold\">" + row.rankNo + "</td>" +
                "<td class=\"align-middle\">" + buildParticipantNamesHtml(row.participants) + "</td>" +
                "<td class=\"align-middle\"><div class=\"fw-semibold\">" + escapeHtml(row.teamCode) + "</div><div class=\"text-muted small\">" + escapeHtml(row.teamName) + "</div></td>" +
                "<td class=\"text-center text-nowrap align-middle\">" + formatWholeScore(row.entryTotalScore) + "</td>" +
                "<td class=\"text-center text-nowrap align-middle\"><span class=\"badge bg-success\">" + row.pointsAwarded + " " + pointsLabel(row.pointsAwarded) + "</span></td>" +
                "<td class=\"text-center text-nowrap align-middle\"><span class=\"app-pill\">Winner</span></td>" +
                "</tr>";
        }).join("");
    }

    function buildCompetitionWinnerGroupsHtml(groups) {
        if (!groups || groups.length === 0) {
            return "<p class=\"text-muted mb-0\">No completed competition results found for this competition year.</p>";
        }

        return groups.map(function (group) {
            return "<div class=\"border rounded-3 p-3 mb-3\">" +
                "<div class=\"d-flex flex-wrap justify-content-between align-items-start gap-2 mb-3\">" +
                "<div><h3 class=\"h5 mb-1\">" + escapeHtml(group.competitionName) + " <span class=\"text-muted fw-normal\">• " + escapeHtml(group.categoryDisplayName) + "</span></h3>" +
                "<div class=\"d-flex flex-wrap gap-2 align-items-center\"><span class=\"app-pill\">" + escapeHtml(group.eventType) + "</span><span class=\"badge bg-success\">Top " + group.winnerCount + "</span></div></div>" +
                "<div class=\"text-muted small text-end\">Calculated: " + escapeHtml(group.calculatedAt) + "</div></div>" +
                "<div class=\"table-responsive\"><table class=\"table app-table align-middle\" style=\"table-layout: fixed;\">" +
                "<colgroup><col style=\"width: 4.5rem;\" /><col style=\"width: 36%;\" /><col style=\"width: 30%;\" /><col style=\"width: 7.5rem;\" /><col style=\"width: 7rem;\" /><col style=\"width: 8rem;\" /></colgroup>" +
                "<thead><tr><th class=\"text-center\">Rank</th><th>Participant(s)</th><th>Team</th><th class=\"text-center\">Score</th><th class=\"text-center\">Points</th><th class=\"text-center\">Status</th></tr></thead>" +
                "<tbody>" + buildCompetitionWinnerRowsHtml(group.rows) + "</tbody></table></div></div>";
        }).join("");
    }

    function buildOverallBreakdownHtml(rows) {
        if (!rows || rows.length === 0) {
            return "<span class=\"text-muted\">-</span>";
        }

        return rows.map(function (row) {
            return "<div class=\"small\"><span class=\"fw-semibold\">" + escapeHtml(row.competitionName) + " • " + escapeHtml(row.categoryDisplayName) + "</span> " +
                "<span class=\"text-muted\">(" + escapeHtml(row.eventType) + ")</span> — " + row.pointsAwarded + " " + pointsLabel(row.pointsAwarded) + "</div>";
        }).join("");
    }

    function buildTieBadgeHtml(row) {
        if (row.isTieBreakerWinner) {
            return "<span class=\"badge bg-warning text-dark mt-1\">Tie-breaker winner</span>";
        }

        if (row.isTiedOnTotalPoints) {
            return "<span class=\"badge bg-warning-subtle text-warning-emphasis border border-warning-subtle mt-1\">Tied on total points</span>";
        }

        return "";
    }

    function buildOverallRowsHtml(rows) {
        if (!rows || rows.length === 0) {
            return "<tr><td colspan=\"8\" class=\"text-muted\">No overall standings found.</td></tr>";
        }

        return rows.map(function (row) {
            const rowClass = row.isTopScore ? (row.hasTopScoreTie ? "best-participant-row-tied-top" : "best-participant-row-top") : "";
            return "<tr class=\"" + rowClass + "\">" +
                "<td class=\"text-center text-nowrap align-middle fw-semibold\">" + row.rankNo + "</td>" +
                "<td class=\"align-middle\"><div>" + escapeHtml(row.participantName) + "</div>" + buildTieBadgeHtml(row) + "</td>" +
                "<td class=\"align-middle\"><div class=\"fw-semibold\">" + escapeHtml(row.teamCode) + "</div><div class=\"text-muted small\">" + escapeHtml(row.teamName) + "</div></td>" +
                "<td class=\"text-center text-nowrap align-middle\"><span class=\"badge bg-success\">" + row.totalPoints + " " + pointsLabel(row.totalPoints) + "</span></td>" +
                "<td class=\"text-center text-nowrap align-middle\">" + Number(row.averagePoints).toFixed(2) + "</td>" +
                "<td class=\"text-center text-nowrap align-middle\">" + row.eventsParticipated + "</td>" +
                "<td class=\"text-center text-nowrap align-middle\"><div>" + row.individualEventPoints + " individual</div><div class=\"text-muted small\">" + row.teamEventPoints + " team</div></td>" +
                "<td class=\"align-middle\">" + buildOverallBreakdownHtml(row.pointsBreakdown) + "</td>" +
                "</tr>";
        }).join("");
    }

    function buildOverallGroupsHtml(groups) {
        if (!groups || groups.length === 0) {
            return "<p class=\"text-muted mb-0\">No participant points found from completed competition results.</p>";
        }

        return groups.map(function (group) {
            const tieWarning = group.hasTopScoreTie
                ? "<div class=\"alert alert-warning py-2 px-3 small mb-3\"><strong>Tie detected:</strong> " + group.topScoreTieCount + " participants share the highest total points. Ranking is resolved using the configured tie-breaker policy.</div>"
                : "";

            return "<div class=\"border rounded-3 p-3 mb-3\">" +
                "<div class=\"d-flex flex-wrap justify-content-between align-items-center gap-2 mb-3\"><div><div class=\"text-muted small\">Best participants</div><h3 class=\"h5 mb-0\">" + escapeHtml(group.tahapDisplayName) + "</h3></div>" +
                "<span class=\"badge bg-primary\">Top " + Math.min(5, group.rows.length) + " of " + group.totalParticipantCount + "</span></div>" + tieWarning +
                "<div class=\"table-responsive\"><table class=\"table app-table align-middle\" style=\"table-layout: fixed;\">" +
                "<colgroup><col style=\"width: 4.5rem;\" /><col style=\"width: 22%;\" /><col style=\"width: 20%;\" /><col style=\"width: 7.5rem;\" /><col style=\"width: 6rem;\" /><col style=\"width: 6rem;\" /><col style=\"width: 9rem;\" /><col /></colgroup>" +
                "<thead><tr><th class=\"text-center\">Rank</th><th>Participant</th><th>Team</th><th class=\"text-center\">Total Points</th><th class=\"text-center\">Avg</th><th class=\"text-center\">Events</th><th class=\"text-center\">Split</th><th>Breakdown</th></tr></thead>" +
                "<tbody>" + buildOverallRowsHtml(group.rows) + "</tbody></table></div></div>";
        }).join("");
    }

    function renderWorkspace(workspace) {
        const hasYear = Boolean(workspace && workspace.competitionYearId && workspace.competitionYearId !== "00000000-0000-0000-0000-000000000000");
        const emptySelection = getElementById("winnersEmptySelection");
        const workspaceContainer = getElementById("winnersWorkspace");

        if (emptySelection) emptySelection.classList.toggle("d-none", hasYear);
        if (workspaceContainer) workspaceContainer.classList.toggle("d-none", !hasYear);
        if (!hasYear) return;

        setText("workspaceCompetitionYearDisplay", workspace.competitionYearDisplay || "");
        setText("completedCompetitionResultCount", String(workspace.completedCompetitionResultCount || 0));
        setText("pendingCompetitionResultCount", String(workspace.pendingCompetitionResultCount || 0));
        setText("overallCurrentRowCount", String(workspace.overallCurrentRowCount || 0) + " rows");
        setText("lastOverallGeneratedAt", "Last generated: " + (workspace.lastOverallGeneratedAt || "-"));
        setText("tieBreakerRuleDisplay", workspace.tieBreakerRuleDisplay || "");

        const overallBadge = getElementById("overallStatusBadge");
        if (overallBadge) {
            overallBadge.textContent = workspace.overallStatus || "";
            overallBadge.className = "badge " + (workspace.overallStatusClass || "bg-secondary");
        }

        setHtml("competitionWinnersList", buildCompetitionWinnerGroupsHtml(workspace.competitionWinnerGroups));
        setHtml("overallTahapWinnersList", buildOverallGroupsHtml(workspace.overallTahapGroups));
    }

    async function loadCompetitionYearPreview(competitionYearId) {
        if (!competitionYearId) {
            currentWorkspace = null;
            renderWorkspace(null);
            return;
        }

        const response = await fetch("?handler=YearPreview&competitionYearId=" + encodeURIComponent(competitionYearId), {
            headers: { "Accept": "application/json" }
        });

        if (!response.ok) {
            throw new Error("Failed to load winners list preview.");
        }

        currentWorkspace = await response.json();
        renderWorkspace(currentWorkspace);
    }

    function showResultModal(isSuccess, title, message) {
        const content = getElementById("winnersResultModalContent");
        const icon = getElementById("winnersResultModalIcon");
        const modal = getElementById("winnersResultModal");

        if (content) {
            content.classList.remove("app-result-modal-success", "app-result-modal-danger");
            content.classList.add(isSuccess ? "app-result-modal-success" : "app-result-modal-danger");
        }

        if (icon) icon.className = "bi " + (isSuccess ? "bi-check-circle-fill" : "bi-exclamation-triangle-fill");
        setText("winnersResultModalTitle", title);
        setText("winnersResultModalMessage", message);

        if (modal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    async function generateOverallStandings() {
        const yearSelect = getElementById("competitionYearSelect");
        const selectedYearId = yearSelect ? yearSelect.value : "";
        if (!selectedYearId) return;

        const confirmButton = getElementById("confirmGenerateOverallStandingsButton");
        if (confirmButton) {
            confirmButton.disabled = true;
            confirmButton.textContent = "Generating...";
        }

        try {
            const body = new URLSearchParams();
            body.append("competitionYearId", selectedYearId);

            const response = await fetch("?handler=GenerateOverallStandings", {
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/x-www-form-urlencoded; charset=UTF-8",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: body.toString()
            });

            const result = await response.json().catch(function () {
                return {
                    success: false,
                    title: "Generation failed",
                    message: "The generation response could not be read."
                };
            });

            const confirmModal = getElementById("generateOverallStandingsModal");
            if (confirmModal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(confirmModal).hide();

            if (!response.ok || !result.success) {
                showResultModal(false, result.title || "Generation failed", result.message || "Overall standings could not be generated.");
                return;
            }

            await loadCompetitionYearPreview(selectedYearId);
            showResultModal(true, result.title || "Overall standings generated", result.message || "Overall standings were generated successfully.");
        } catch (error) {
            const confirmModal = getElementById("generateOverallStandingsModal");
            if (confirmModal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(confirmModal).hide();
            showResultModal(false, "Generation failed", error.message || "Overall standings could not be generated.");
        } finally {
            if (confirmButton) {
                confirmButton.disabled = false;
                confirmButton.textContent = "Generate Overall Standings";
            }
        }
    }

    function setupWinnersListPage() {
        if (!isWinnersListPage()) return;

        const yearSelect = getElementById("competitionYearSelect");
        const selectedYearId = yearSelect ? yearSelect.value : "";

        if (yearSelect) {
            yearSelect.addEventListener("change", async function (event) {
                try {
                    await loadCompetitionYearPreview(event.target.value);
                } catch (error) {
                    showResultModal(false, "Load failed", error.message || "The selected winners list could not be loaded.");
                }
            });
        }

        const generateButton = getElementById("generateOverallStandingsButton");
        if (generateButton) {
            generateButton.addEventListener("click", function () {
                setText("generateOverallStandingsModalText", currentWorkspace && currentWorkspace.competitionYearDisplay ? currentWorkspace.competitionYearDisplay : "Selected competition year");
                const modal = getElementById("generateOverallStandingsModal");
                if (modal && window.bootstrap) bootstrap.Modal.getOrCreateInstance(modal).show();
            });
        }

        const confirmButton = getElementById("confirmGenerateOverallStandingsButton");
        if (confirmButton) confirmButton.addEventListener("click", generateOverallStandings);

        loadCompetitionYearPreview(selectedYearId).catch(function (error) {
            showResultModal(false, "Load failed", error.message || "The selected winners list could not be loaded.");
        });
    }

    document.addEventListener("DOMContentLoaded", setupWinnersListPage);
})();
