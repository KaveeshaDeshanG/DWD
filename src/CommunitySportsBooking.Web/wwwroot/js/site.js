// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Generic password-visibility toggle. Any <button data-password-toggle="inputId">
// wrapping/adjacent to a password <input id="inputId"> gets a working show/hide
// control with no page-specific script needed.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-password-toggle]').forEach(function (button) {
        button.addEventListener('click', function () {
            var input = document.getElementById(button.getAttribute('data-password-toggle'));
            if (!input) {
                return;
            }
            var isCurrentlyHidden = input.type === 'password';
            input.type = isCurrentlyHidden ? 'text' : 'password';

            var icon = button.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye', !isCurrentlyHidden);
                icon.classList.toggle('bi-eye-slash', isCurrentlyHidden);
            }
            button.setAttribute('aria-label', isCurrentlyHidden ? 'Hide password' : 'Show password');
        });
    });
});

// Live password-requirements checklist (Register page). Purely a UI preview
// of the same rules RegisterViewModel already enforces server-side
// ([StringLength(MinimumLength = 8)], letter+digit [RegularExpression]) —
// this never replaces or bypasses that validation.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-password-requirements-target]').forEach(function (input) {
        var list = document.getElementById(input.getAttribute('data-password-requirements-target'));
        if (!list) {
            return;
        }
        var update = function () {
            var value = input.value;
            var checks = {
                length: value.length >= 8,
                letter: /[A-Za-z]/.test(value),
                digit: /\d/.test(value)
            };
            list.querySelectorAll('li').forEach(function (li) {
                var rule = li.getAttribute('data-rule');
                li.classList.toggle('met', !!checks[rule]);
            });
        };
        input.addEventListener('input', update);
        update();
    });
});

// Quick time-slot picker (Booking Create) — client-side convenience only,
// fills the real Start/End time <input>s; the server still runs the full
// three-layer availability check on submit regardless of how those fields
// were populated, so this never bypasses or duplicates that logic.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-slot-group]').forEach(function (group) {
        var startInput = document.getElementById(group.getAttribute('data-start-target'));
        var endInput = document.getElementById(group.getAttribute('data-end-target'));
        group.querySelectorAll('.slot').forEach(function (btn) {
            btn.addEventListener('click', function () {
                if (startInput) {
                    startInput.value = btn.getAttribute('data-start');
                    startInput.dispatchEvent(new Event('input'));
                }
                if (endInput) {
                    endInput.value = btn.getAttribute('data-end');
                    endInput.dispatchEvent(new Event('input'));
                }
                group.querySelectorAll('.slot').forEach(function (s) {
                    s.classList.remove('slot-selected');
                    s.setAttribute('aria-pressed', 'false');
                });
                btn.classList.add('slot-selected');
                btn.setAttribute('aria-pressed', 'true');
            });
        });
    });
});

// Live booking summary (Booking Create) — reflects whatever is currently in
// the Date/Start/End inputs, purely a display convenience; the values it
// reads are the same ones the form submits, nothing is duplicated server-side.
document.addEventListener('DOMContentLoaded', function () {
    var summary = document.getElementById('bookingSummary');
    if (!summary) {
        return;
    }
    var dateInput = document.getElementById('BookingDate');
    var startInput = document.getElementById('StartTime');
    var endInput = document.getElementById('EndTime');
    var dateOut = document.getElementById('summaryDate');
    var timeOut = document.getElementById('summaryTime');

    var update = function () {
        dateOut.textContent = dateInput.value ? dateInput.value : 'Not selected';
        if (startInput.value && endInput.value) {
            timeOut.textContent = startInput.value + ' – ' + endInput.value;
        } else {
            timeOut.textContent = 'Not selected';
        }
    };
    [dateInput, startInput, endInput].forEach(function (input) {
        input.addEventListener('input', update);
    });
    update();
});

// Booking submission loading protection (Task 8) — UI/UX polish only, opt-in
// via a [data-loading-protect] form. The real duplicate-booking defense is
// entirely server/database-side (BookingController's availability pre-check
// plus the three-layer sp_getapplock transaction + trigger backstop inside
// usp_CreateBooking) — this never replaces or is relied on for that. It only
// stops a user from firing a second identical POST by clicking again before
// the page navigates away, and gives clear feedback that the click registered.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form[data-loading-protect]').forEach(function (form) {
        form.addEventListener('submit', function () {
            // If jQuery Validate has parsed this form and it is currently
            // invalid, do nothing: leave the button exactly as it was so the
            // normal validation summary/messages show and the user can fix
            // the errors and try again immediately — never leave it disabled
            // when the submission didn't actually go anywhere.
            if (window.jQuery && typeof jQuery.fn.valid === 'function' && !jQuery(form).valid()) {
                return;
            }

            var submitButton = form.querySelector('button[type="submit"]');
            if (!submitButton || submitButton.disabled) {
                return;
            }

            submitButton.dataset.originalHtml = submitButton.innerHTML;
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Submitting…';
        });
    });
});
