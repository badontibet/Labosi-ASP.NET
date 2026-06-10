(function () {
    "use strict";

    function setMessage(root, type, message) {
        var success = root.querySelector("[data-attachments-success]");
        var error = root.querySelector("[data-attachments-error]");

        if (success) {
            success.hidden = type !== "success" || !message;
            success.textContent = type === "success" ? message : "";
        }

        if (error) {
            error.hidden = type !== "error" || !message;
            error.textContent = type === "error" ? message : "";
        }
    }

    function setLoading(root, isLoading) {
        var loading = root.querySelector("[data-attachments-loading]");
        if (loading) {
            loading.hidden = !isLoading;
        }
    }

    function formatSize(bytes) {
        if (bytes < 1024) {
            return bytes + " B";
        }

        if (bytes < 1024 * 1024) {
            return (bytes / 1024).toFixed(1) + " KB";
        }

        return (bytes / (1024 * 1024)).toFixed(1) + " MB";
    }

    function formatDate(value) {
        var date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return value || "";
        }

        return date.toLocaleString();
    }

    function apiBase(fileId) {
        return "/api/files/" + encodeURIComponent(fileId) + "/attachments";
    }

    function renderList(root, attachments) {
        var target = root.querySelector("[data-attachments-list]");
        var canDelete = root.getAttribute("data-can-delete") === "true";

        if (!target) {
            return;
        }

        if (!attachments.length) {
            target.innerHTML = "<p class=\"empty-cell\">No attachments found.</p>";
            return;
        }

        var html = [
            "<div class=\"table-wrap attachment-table\">",
            "<table>",
            "<thead><tr>",
            "<th>File</th>",
            "<th>Type</th>",
            "<th>Size</th>",
            "<th>Created</th>"
        ];

        if (canDelete) {
            html.push("<th>Actions</th>");
        }

        html.push("</tr></thead><tbody>");

        attachments.forEach(function (attachment) {
            html.push("<tr data-attachment-id=\"" + attachment.id + "\">");
            html.push("<td>" + escapeHtml(attachment.originalFileName) + "</td>");
            html.push("<td>" + escapeHtml(attachment.contentType || "application/octet-stream") + "</td>");
            html.push("<td>" + formatSize(attachment.fileSize || 0) + "</td>");
            html.push("<td>" + escapeHtml(formatDate(attachment.createdAt)) + "</td>");

            if (canDelete) {
                html.push("<td class=\"action-cell\"><button type=\"button\" class=\"danger-action\" data-attachment-delete=\"" + attachment.id + "\">Delete</button></td>");
            }

            html.push("</tr>");
        });

        html.push("</tbody></table></div>");
        target.innerHTML = html.join("");

        target.querySelectorAll("[data-attachment-delete]").forEach(function (button) {
            button.addEventListener("click", function () {
                deleteAttachment(root, button.getAttribute("data-attachment-delete"));
            });
        });
    }

    function escapeHtml(value) {
        return String(value || "")
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function readError(response) {
        return response.text().then(function (text) {
            if (!text) {
                return response.statusText || "Request failed.";
            }

            try {
                var payload = JSON.parse(text);
                return payload.message || payload.title || text;
            } catch (_) {
                return text;
            }
        });
    }

    function loadAttachments(root) {
        var fileId = root.getAttribute("data-file-id");
        setLoading(root, true);

        fetch(apiBase(fileId), {
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (response) {
                if (response.status === 401) {
                    throw new Error("Sign in to view attachments.");
                }

                if (response.status === 403) {
                    throw new Error("You do not have access to view attachments.");
                }

                if (response.status === 404) {
                    throw new Error("File record was not found.");
                }

                if (!response.ok) {
                    throw new Error("Could not load attachments.");
                }

                return response.json();
            })
            .then(function (attachments) {
                renderList(root, attachments);
            })
            .catch(function (error) {
                var target = root.querySelector("[data-attachments-list]");
                if (target) {
                    target.innerHTML = "<p class=\"empty-cell\">Attachments unavailable.</p>";
                }
                setMessage(root, "error", error.message);
            })
            .finally(function () {
                setLoading(root, false);
            });
    }

    function uploadAttachment(root) {
        var fileInput = root.querySelector("[data-attachments-file]");
        var fileId = root.getAttribute("data-file-id");

        if (!fileInput || !fileInput.files.length) {
            setMessage(root, "error", "Choose a file before uploading.");
            return;
        }

        var data = new FormData();
        data.append("file", fileInput.files[0]);
        setLoading(root, true);
        setMessage(root, "", "");

        fetch(apiBase(fileId), {
            method: "POST",
            body: data,
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (response) {
                if (response.ok) {
                    return response.json();
                }

                if (response.status === 401) {
                    throw new Error("Sign in to upload attachments.");
                }

                if (response.status === 403) {
                    throw new Error("You do not have permission to upload attachments.");
                }

                if (response.status === 404) {
                    throw new Error("File record was not found.");
                }

                return readError(response).then(function (message) {
                    throw new Error(message);
                });
            })
            .then(function () {
                fileInput.value = "";
                setMessage(root, "success", "Attachment uploaded.");
                loadAttachments(root);
            })
            .catch(function (error) {
                setMessage(root, "error", error.message);
            })
            .finally(function () {
                setLoading(root, false);
            });
    }

    function deleteAttachment(root, attachmentId) {
        var fileId = root.getAttribute("data-file-id");

        if (!window.confirm("Delete this attachment?")) {
            return;
        }

        setLoading(root, true);
        setMessage(root, "", "");

        fetch(apiBase(fileId) + "/" + encodeURIComponent(attachmentId), {
            method: "DELETE",
            credentials: "same-origin",
            headers: { "X-Requested-With": "XMLHttpRequest" }
        })
            .then(function (response) {
                if (response.ok) {
                    setMessage(root, "success", "Attachment deleted.");
                    loadAttachments(root);
                    return;
                }

                if (response.status === 401) {
                    throw new Error("Sign in to delete attachments.");
                }

                if (response.status === 403) {
                    throw new Error("You do not have permission to delete attachments.");
                }

                if (response.status === 404) {
                    throw new Error("Attachment was not found.");
                }

                throw new Error("Could not delete attachment.");
            })
            .catch(function (error) {
                setMessage(root, "error", error.message);
            })
            .finally(function () {
                setLoading(root, false);
            });
    }

    function setupUpload(root) {
        var form = root.querySelector("[data-attachments-form]");
        var dropZone = root.querySelector("[data-attachments-drop-zone]");
        var fileInput = root.querySelector("[data-attachments-file]");

        if (form) {
            form.addEventListener("submit", function (event) {
                event.preventDefault();
                uploadAttachment(root);
            });
        }

        if (dropZone && fileInput) {
            ["dragenter", "dragover"].forEach(function (eventName) {
                dropZone.addEventListener(eventName, function (event) {
                    event.preventDefault();
                    dropZone.classList.add("is-dragging");
                });
            });

            ["dragleave", "drop"].forEach(function (eventName) {
                dropZone.addEventListener(eventName, function (event) {
                    event.preventDefault();
                    dropZone.classList.remove("is-dragging");
                });
            });

            dropZone.addEventListener("drop", function (event) {
                if (event.dataTransfer && event.dataTransfer.files.length) {
                    fileInput.files = event.dataTransfer.files;
                }
            });
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll("[data-file-attachments]").forEach(function (root) {
            setupUpload(root);
            loadAttachments(root);
        });
    });
})();
