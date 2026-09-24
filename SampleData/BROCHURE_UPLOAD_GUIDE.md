# Brochure / PDF Catalog Upload

1. Login as Admin or Business Owner.
2. Open Business → Website Content.
3. Scroll to **Brochures / PDF catalog**.
4. Choose a PDF and click **Upload PDF**.
5. The file is stored under `wwwroot/uploads/business/{BusinessId}/brochures/`.
6. The public business website automatically displays a **Brochures & PDF Catalog** section when at least one brochure exists.
7. Click **View brochure** to open the PDF in a new browser tab.
8. Delete the brochure from Website Content when the business replaces it.

Security/operational limits for the MVP:
- PDF only.
- Maximum 20 MB per file.
- PDF header is checked for `%PDF-` before storage.
- Files are stored inside the business-specific folder.
- File names are generated with a GUID to avoid collisions.
- No database migration is required for brochure files.

For a production deployment with multiple servers/containers, move this file storage to Azure Blob Storage later. The UI/controller contract can remain the same.
