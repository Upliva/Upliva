/*
    UplivaAI Dynamic Catalog Engine
    Adds the JSON extension column used by business-specific catalog templates.

    Run this once against the existing UplivaAI database if the column does not exist.
    This is a schema change, so do NOT run it repeatedly.
*/

IF COL_LENGTH('dbo.BusinessCatalogItems', 'CustomAttributesJson') IS NULL
BEGIN
    ALTER TABLE dbo.BusinessCatalogItems
    ADD CustomAttributesJson nvarchar(max) NOT NULL
        CONSTRAINT DF_BusinessCatalogItems_CustomAttributesJson DEFAULT N'{}';
END;
GO

UPDATE dbo.BusinessCatalogItems
SET CustomAttributesJson = N'{}'
WHERE CustomAttributesJson IS NULL;
GO
