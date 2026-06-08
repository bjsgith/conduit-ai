// ConduitAI – progressive enhancement.
// Forms tagged with `.js-confirm` ask for confirmation before submitting.
$(function () {
    $(document).on("submit", "form.js-confirm", function (e) {
        var message = $(this).data("confirm") || "Are you sure?";
        if (!window.confirm(message)) {
            e.preventDefault();
        }
    });
});
