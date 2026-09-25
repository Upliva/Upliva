# Catalog Edit/Save Fix

- UpdateCatalog now explicitly loads the catalog item with EF tracking.
- Every common catalog field is assigned from the posted edit model.
- Dynamic CustomAttributes are normalized and persisted as JSON.
- The entity is explicitly marked Modified before SaveChangesAsync.
- The row is re-read from SQL Server after save before success is reported.
- The edit form posts explicit Id and BusinessId hidden values.
- Dynamic dropdown selected values use standard selected="selected" markup.
- Product Name remains the only required catalog field.
- No database schema change is required for this fix.
