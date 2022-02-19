TRUNCATE TABLE DimSalesReason;

INSERT INTO DimSalesReason(SalesReasonKey, SalesReasonAlternateKey, SalesReasonName, SalesReasonReasonType)
SELECT 1, 1, N'Price', N'Other' FROM DUAL UNION ALL
SELECT 2, 2, N'On Promotion', N'Promotion' FROM DUAL UNION ALL
SELECT 3, 3, N'Magazine Advertisement', N'Marketing' FROM DUAL UNION ALL
SELECT 4, 4, N'Television  Advertisement', N'Marketing' FROM DUAL UNION ALL
SELECT 5, 5, N'Manufacturer', N'Other' FROM DUAL UNION ALL
SELECT 6, 6, N'Review', N'Other' FROM DUAL UNION ALL
SELECT 7, 7, N'Demo Event', N'Marketing' FROM DUAL UNION ALL
SELECT 8, 8, N'Sponsorship', N'Marketing' FROM DUAL UNION ALL
SELECT 9, 9, N'Quality', N'Other' FROM DUAL UNION ALL
SELECT 10, 10, N'Other', N'Other' FROM DUAL;

COMMIT;

