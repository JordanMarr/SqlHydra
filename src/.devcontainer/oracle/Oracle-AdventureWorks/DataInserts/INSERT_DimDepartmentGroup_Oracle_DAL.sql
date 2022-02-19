TRUNCATE TABLE DimDepartmentGroup;

INSERT INTO DimDepartmentGroup(DepartmentGroupKey, ParentDepartmentGroupKey, DepartmentGroupName)
SELECT 1, NULL, N'Corporate' FROM DUAL UNION ALL
SELECT 2, 1, N'Executive General and Administration' FROM DUAL UNION ALL
SELECT 3, 1, N'Inventory Management' FROM DUAL UNION ALL
SELECT 4, 1, N'Manufacturing' FROM DUAL UNION ALL
SELECT 5, 1, N'Quality Assurance' FROM DUAL UNION ALL
SELECT 6, 1, N'Research and Development' FROM DUAL UNION ALL
SELECT 7, 1, N'Sales and Marketing' FROM DUAL;
COMMIT;
