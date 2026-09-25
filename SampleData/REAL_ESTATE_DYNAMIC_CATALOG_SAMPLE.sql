/* Optional sample: attach business-specific real-estate values to an existing catalog item. */

DECLARE @CatalogItemId INT = NULL; -- set this to the item you just created

IF @CatalogItemId IS NOT NULL
BEGIN
    UPDATE dbo.BusinessCatalogItems
    SET CustomAttributesJson = N'{
      "propertyType":"Apartment",
      "bhk":"3",
      "area":"1650 sq.ft",
      "location":"Lalpur, Ranchi",
      "bedrooms":"3",
      "bathrooms":"3",
      "parking":"2 cars",
      "furnishing":"Semi Furnished",
      "facing":"East",
      "possession":"Ready to move"
    }'
    WHERE Id = @CatalogItemId;
END;
