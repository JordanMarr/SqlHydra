TRUNCATE TABLE DimSalesTerritory;

INSERT INTO DimSalesTerritory(SalesTerritoryKey, SalesTerritoryAlternateKey, SalesTerritoryRegion, SalesTerritoryCountry, SalesTerritoryGroup)
SELECT 1, 1, N'Northwest', N'United States', N'North America' FROM DUAL UNION ALL
SELECT 2, 2, N'Northeast', N'United States', N'North America' FROM DUAL UNION ALL
SELECT 3, 3, N'Central', N'United States', N'North America' FROM DUAL UNION ALL
SELECT 4, 4, N'Southwest', N'United States', N'North America' FROM DUAL UNION ALL
SELECT 5, 5, N'Southeast', N'United States', N'North America' FROM DUAL UNION ALL
SELECT 6, 6, N'Canada', N'Canada', N'North America' FROM DUAL UNION ALL
SELECT 7, 7, N'France', N'France', N'Europe' FROM DUAL UNION ALL
SELECT 8, 8, N'Germany', N'Germany', N'Europe' FROM DUAL UNION ALL
SELECT 9, 9, N'Australia', N'Australia', N'Pacific' FROM DUAL UNION ALL
SELECT 10, 10, N'United Kingdom', N'United Kingdom', N'Europe' FROM DUAL UNION ALL
SELECT 11, 0, N'NA', N'NA', N'NA' FROM DUAL;

COMMIT;

