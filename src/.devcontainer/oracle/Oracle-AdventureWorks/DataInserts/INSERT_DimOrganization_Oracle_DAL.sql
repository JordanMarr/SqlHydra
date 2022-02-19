TRUNCATE TABLE DimOrganization;


INSERT INTO DimOrganization(OrganizationKey, ParentOrganizationKey, PercentageOfOwnership, OrganizationName, CurrencyKey)
SELECT 1, NULL, N'1', N'AdventureWorks Cycle', 100 FROM DUAL UNION ALL
SELECT 2, 1, N'1', N'North America Operations', 100 FROM DUAL UNION ALL
SELECT 3, 14, N'1', N'Northeast Division', 100 FROM DUAL UNION ALL
SELECT 4, 14, N'1', N'Northwest Division', 100 FROM DUAL UNION ALL
SELECT 5, 14, N'1', N'Central Division', 100 FROM DUAL UNION ALL
SELECT 6, 14, N'1', N'Southeast Division', 100 FROM DUAL UNION ALL
SELECT 7, 14, N'1', N'Southwest Division', 100 FROM DUAL UNION ALL
SELECT 8, 2, N'.75', N'Canadian Division', 19 FROM DUAL UNION ALL
SELECT 9, 1, N'1', N'European Operations', 36 FROM DUAL UNION ALL
SELECT 10, 1, N'.75', N'Pacific Operations', 6 FROM DUAL UNION ALL
SELECT 11, 9, N'.50', N'France', 36 FROM DUAL UNION ALL
SELECT 12, 9, N'.25', N'Germany', 36 FROM DUAL UNION ALL
SELECT 13, 10, N'.50', N'Australia', 6 FROM DUAL UNION ALL
SELECT 14, 2, N'1', N'USA Operations', 100 FROM DUAL;

COMMIT;
