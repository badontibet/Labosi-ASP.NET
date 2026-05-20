(function () {
    "use strict";

    var debounceDelay = 275;

    function debounce(callback, delay) {
        var handle;
        return function () {
            var args = arguments;
            window.clearTimeout(handle);
            handle = window.setTimeout(function () {
                callback.apply(null, args);
            }, delay);
        };
    }

    function setMessage(input, message) {
        var targetSelector = input.getAttribute("data-lab4-error-target");
        var target = targetSelector
            ? document.querySelector(targetSelector)
            : document.querySelector("[data-valmsg-for='" + input.name + "']");

        input.classList.toggle("input-validation-error", Boolean(message));

        if (target) {
            target.textContent = message || "";
            target.classList.toggle("field-validation-error", Boolean(message));
            target.classList.toggle("field-validation-valid", !message);
        }
    }

    function getDateTimeParts(value) {
        if (!value) {
            return null;
        }

        var formats = [
            { pattern: /^(\d{1,2})\.(\d{1,2})\.(\d{4})\s+(\d{2}):(\d{2})$/, year: 3, month: 2, day: 1, hour: 4, minute: 5 },
            { pattern: /^(\d{1,2})\/(\d{1,2})\/(\d{4})\s+(\d{2}):(\d{2})$/, year: 3, month: 1, day: 2, hour: 4, minute: 5 },
            { pattern: /^(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2})$/, year: 1, month: 2, day: 3, hour: 4, minute: 5 }
        ];

        for (var index = 0; index < formats.length; index += 1) {
            var format = formats[index];
            var match = value.match(format.pattern);
            if (!match) {
                continue;
            }

            var year = Number(match[format.year]);
            var month = Number(match[format.month]);
            var day = Number(match[format.day]);
            var hour = Number(match[format.hour]);
            var minute = Number(match[format.minute]);

            if (month < 1 || month > 12 || day < 1 || day > 31 || hour < 0 || hour > 23 || minute < 0 || minute > 59) {
                return null;
            }

            var date = new Date(year, month - 1, day, hour, minute, 0, 0);
            if (
                date.getFullYear() !== year ||
                date.getMonth() !== month - 1 ||
                date.getDate() !== day ||
                date.getHours() !== hour ||
                date.getMinutes() !== minute
            ) {
                return null;
            }

            return date;
        }

        return null;
    }

    function isValidDateTime(value) {
        return Boolean(getDateTimeParts(value));
    }

    function validateInput(input) {
        var rule = input.getAttribute("data-lab4-validate");
        var value = input.value.trim();

        if (rule === "required" && !value) {
            setMessage(input, "This field is required.");
            return false;
        }

        if (rule === "optional-autocomplete") {
            setMessage(input, "");
            return true;
        }

        var maxLength = Number(input.getAttribute("data-lab4-max-length"));
        if (maxLength && value.length > maxLength) {
            setMessage(input, "Use " + maxLength + " characters or fewer.");
            return false;
        }

        if (rule === "hexcolor" && value && !/^#[0-9A-Fa-f]{6}$/.test(value)) {
            setMessage(input, "Use a hex color like #AABBCC.");
            return false;
        }

        if (rule === "datetime") {
            var isRequired = input.getAttribute("data-lab4-required") === "true";
            if (isRequired && !value) {
                setMessage(input, "Date and time are required.");
                return false;
            }

            if (value && !isValidDateTime(value)) {
                setMessage(input, "Use dd.MM.yyyy HH:mm, MM/dd/yyyy HH:mm, or yyyy-MM-dd HH:mm.");
                return false;
            }

            var form = input.closest("form");
            if (form) {
                var invalidRange = validateDateRange(
                    form,
                    "StartTimeText",
                    "EndTimeText",
                    "End time cannot be before start time.");

                if (invalidRange && input.name === "EndTimeText") {
                    return false;
                }

                invalidRange = validateDateRange(
                    form,
                    "CreatedDateText",
                    "ModifiedDateText",
                    "Modified date cannot be before created date.");

                if (invalidRange && input.name === "ModifiedDateText") {
                    return false;
                }
            }
        }

        if (rule === "nonnegative") {
            var number = Number(value);
            if (Number.isNaN(number) || number < 0) {
                setMessage(input, "Value cannot be negative.");
                return false;
            }

            var maxField = input.getAttribute("data-lab4-max-field");
            var form = input.closest("form");
            var maxInput = form && maxField ? form.querySelector("[name='" + maxField + "']") : null;
            var maxValue = maxInput ? Number(maxInput.value) : null;
            if (maxInput && !Number.isNaN(maxValue) && number > maxValue) {
                setMessage(input, "Processed files cannot be greater than total files.");
                return false;
            }
        }

        setMessage(input, "");
        return true;
    }

    function validateDateRange(form, startFieldName, endFieldName, message) {
        var startInput = form.querySelector("[name='" + startFieldName + "']");
        var endInput = form.querySelector("[name='" + endFieldName + "']");

        if (!startInput || !endInput || !startInput.value.trim() || !endInput.value.trim()) {
            return false;
        }

        var startDate = getDateTimeParts(startInput.value.trim());
        var endDate = getDateTimeParts(endInput.value.trim());

        if (startDate && endDate && endDate < startDate) {
            setMessage(endInput, message);
            return true;
        }

        if (endDate) {
            setMessage(endInput, "");
        }

        return false;
    }

    function setupValidation() {
        document.querySelectorAll("[data-lab4-validate]").forEach(function (input) {
            input.addEventListener("blur", function () {
                validateInput(input);
            });

            if (input.hasAttribute("data-lab4-color-input")) {
                input.addEventListener("input", function () {
                    updateColorPreview(input);
                });
                updateColorPreview(input);
            }
        });

        document.querySelectorAll("[data-lab4-form]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                var isValid = true;
                form.querySelectorAll("[data-lab4-validate]").forEach(function (input) {
                    isValid = validateInput(input) && isValid;
                });

                if (!isValid) {
                    event.preventDefault();
                }
            });
        });
    }

    function setupSearch() {
        document.querySelectorAll("[data-lab4-search]").forEach(function (input) {
            var target = document.querySelector(input.getAttribute("data-target"));
            var loading = document.querySelector(input.getAttribute("data-loading"));
            var url = input.getAttribute("data-url");

            if (!target || !url) {
                return;
            }

            function buildSearchUrl() {
                var parameters = new URLSearchParams();
                var searchForm = input.closest("[data-lab4-search-form]");

                if (searchForm) {
                    searchForm.querySelectorAll("[name]").forEach(function (field) {
                        if (field.value) {
                            parameters.set(field.name, field.value);
                        }
                    });
                } else {
                    parameters.set(input.name || "query", input.value);
                }

                return url + "?" + parameters.toString();
            }

            var runSearch = debounce(function () {
                var table = target.closest("table");
                var columnCount = table ? table.querySelectorAll("thead th").length : 1;
                if (loading) {
                    loading.hidden = false;
                }

                fetch(buildSearchUrl(), {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                })
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error("Search failed.");
                        }
                        return response.text();
                    })
                    .then(function (html) {
                        target.innerHTML = html;
                        highlightRows(target);
                    })
                    .catch(function () {
                        target.innerHTML = "<tr><td colspan=\"" + columnCount + "\" class=\"empty-cell\">Search failed. Try again.</td></tr>";
                    })
                    .finally(function () {
                        if (loading) {
                            loading.hidden = true;
                        }
                    });
            }, debounceDelay);

            input.addEventListener("input", runSearch);
            input.addEventListener("change", runSearch);
        });
    }

    function setupAutocomplete() {
        document.querySelectorAll("[data-lab4-autocomplete]").forEach(function (root) {
            var endpoint = root.getAttribute("data-endpoint");
            var textInput = root.querySelector("[data-lab4-autocomplete-text]");
            var idInput = root.querySelector("[data-lab4-autocomplete-id]");
            var menu = root.querySelector("[data-lab4-autocomplete-menu]");
            var loading = root.querySelector("[data-lab4-autocomplete-loading]");
            var selectedText = textInput ? textInput.value : "";

            if (!endpoint || !textInput || !idInput || !menu) {
                return;
            }

            function closeMenu() {
                menu.classList.remove("is-open");
                window.setTimeout(function () {
                    menu.hidden = true;
                }, 150);
            }

            function openMenu() {
                menu.hidden = false;
                window.requestAnimationFrame(function () {
                    menu.classList.add("is-open");
                });
            }

            function renderResults(items) {
                menu.innerHTML = "";

                if (!items.length) {
                    menu.innerHTML = "<div class=\"autocomplete-empty\">No matches found.</div>";
                    openMenu();
                    return;
                }

                items.forEach(function (item) {
                    var option = document.createElement("button");
                    option.type = "button";
                    option.className = "autocomplete-option";
                    option.textContent = item.text;
                    option.addEventListener("click", function () {
                        idInput.value = item.id;
                        textInput.value = item.text;
                        selectedText = item.text;
                        setMessage(textInput, "");
                        closeMenu();
                    });
                    menu.appendChild(option);
                });

                openMenu();
            }

            function clearStaleId() {
                var term = textInput.value.trim();

                if (term !== selectedText) {
                    idInput.value = "";
                }
            }

            var runLookup = debounce(function () {
                var term = textInput.value.trim();
                clearStaleId();

                if (term.length < 2) {
                    closeMenu();
                    return;
                }

                if (loading) {
                    loading.hidden = false;
                }

                fetch(endpoint + "?term=" + encodeURIComponent(term), {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                })
                    .then(function (response) {
                        if (!response.ok) {
                            throw new Error("Autocomplete failed.");
                        }
                        return response.json();
                    })
                    .then(renderResults)
                    .catch(function () {
                        menu.innerHTML = "<div class=\"autocomplete-error\">Could not load autocomplete results.</div>";
                        openMenu();
                    })
                    .finally(function () {
                        if (loading) {
                            loading.hidden = true;
                        }
                    });
            }, debounceDelay);

            textInput.addEventListener("input", function () {
                clearStaleId();
                runLookup();
            });
            textInput.addEventListener("blur", function () {
                window.setTimeout(function () {
                    var isRequired = textInput.getAttribute("data-lab4-autocomplete-required") === "true";
                    if (isRequired && !idInput.value) {
                        setMessage(textInput, textInput.getAttribute("data-lab4-autocomplete-message") || "Choose an item from the list.");
                    } else if (!isRequired) {
                        setMessage(textInput, "");
                    }
                    closeMenu();
                }, 180);
            });
        });
    }

    function setupDeleteConfirmation() {
        document.querySelectorAll("[data-lab4-delete-form]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                var message = form.getAttribute("data-confirm-message") || "Delete this item?";
                if (!window.confirm(message)) {
                    event.preventDefault();
                }
            });
        });
    }

    function updateColorPreview(input) {
        var root = input.closest(".form-field");
        var preview = root ? root.querySelector("[data-lab4-color-preview]") : null;
        var value = input.value.trim();

        if (!preview) {
            return;
        }

        preview.style.backgroundColor = /^#[0-9A-Fa-f]{6}$/.test(value) ? value : "transparent";
    }

    function highlightRows(root) {
        root.querySelectorAll("[data-lab4-row]").forEach(function (row) {
            row.classList.add("row-highlight");
            window.setTimeout(function () {
                row.classList.remove("row-highlight");
            }, 1300);
        });
    }

    function highlightInitialRow() {
        var table = document.querySelector("table[data-highlight-id]");
        var highlightId = table ? table.getAttribute("data-highlight-id") : "";

        if (!highlightId) {
            return;
        }

        var row = document.querySelector("[data-row-id='" + highlightId + "']");
        if (row) {
            row.classList.add("row-highlight");
            row.scrollIntoView({ behavior: "smooth", block: "center" });
            window.setTimeout(function () {
                row.classList.remove("row-highlight");
            }, 1400);
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        setupSearch();
        setupAutocomplete();
        setupValidation();
        setupDeleteConfirmation();
        highlightInitialRow();
    });
})();
