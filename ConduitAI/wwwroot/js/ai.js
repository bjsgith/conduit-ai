// ConduitAI – lead analysis generation.
// Explicit, user-triggered AJAX call to the AI endpoint. AI output is treated
// as untrusted and inserted with .text()/DOM APIs only (never raw HTML).
$(function () {
    var $panel = $("#aiPanel");
    if ($panel.length === 0) {
        return;
    }

    var leadId = $panel.data("lead-id");
    var $btn = $("#btnAnalyze");
    var $error = $("#aiError");
    var $result = $("#aiResult");

    function levelClass(level) {
        switch ((level || "").toLowerCase()) {
            case "high": return "lvl-high";
            case "medium": return "lvl-medium";
            case "low": return "lvl-low";
            default: return "lvl-medium";
        }
    }

    function badge(text, cls) {
        return $('<span class="badge"></span>').addClass(cls).text(text);
    }

    function renderAnalysis(data) {
        var $scoreline = $('<div class="ai-scoreline"></div>')
            .append($('<span class="ai-bigscore"></span>')
                .text(data.leadScore)
                .append($('<small></small>').text("/100")))
            .append(badge((data.urgencyLevel || "") + " urgency", levelClass(data.urgencyLevel)))
            .append(badge((data.buyingIntent || "") + " intent", levelClass(data.buyingIntent)));

        var $summary = $('<p class="ai-summary"></p>').text(data.summary || "");

        var $next = $('<div class="ai-next"></div>')
            .append($("<b></b>").text("Recommended next action"))
            .append(document.createTextNode(data.recommendedNextAction || ""));

        var stamp = "Generated just now";
        if (data.modelName) {
            stamp += " · " + data.modelName;
        }
        var $stamp = $('<div class="ai-stamp"></div>').text(stamp);

        $result.empty().append($scoreline, $summary, $next, $stamp);
    }

    $btn.on("click", function () {
        var token = $panel.find('input[name="__RequestVerificationToken"]').val();

        $error.hide().text("");
        $btn.prop("disabled", true).html('<span class="spinner"></span> Analyzing…');

        $.ajax({
            url: "/Ai/AnalyzeLead/" + encodeURIComponent(leadId),
            method: "POST",
            headers: { "RequestVerificationToken": token },
            data: { leadId: leadId }
        }).done(function (resp) {
            if (resp && resp.success) {
                renderAnalysis(resp);
                $btn.text("Regenerate");
            } else {
                $error.text((resp && resp.message) || "AI analysis could not be generated.").show();
                $btn.text("Generate Analysis");
            }
        }).fail(function () {
            $error.text("Could not reach the server. Please try again.").show();
            $btn.text("Generate Analysis");
        }).always(function () {
            $btn.prop("disabled", false);
        });
    });
});
