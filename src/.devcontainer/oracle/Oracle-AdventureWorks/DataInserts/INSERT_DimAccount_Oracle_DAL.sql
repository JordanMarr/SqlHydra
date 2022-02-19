TRUNCATE TABLE DimAccount;

INSERT INTO DimAccount(AccountKey, ParentAccountKey, AccountCodeAlternateKey, ParentAccountCodeAlternateKey, AccountDescription, AccountType, Operator, CustomMembers, ValueType, CustomMemberOptions)
SELECT 1, NULL, 1, NULL, N'Balance Sheet', NULL, N'~', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 2, 1, 10, 1, N'Assets', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 3, 2, 110, 10, N'Current Assets', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 4, 3, 1110, 110, N'Cash', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 5, 3, 1120, 110, N'Receivables', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 6, 5, 1130, 1120, N'Trade Receivables', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 7, 5, 1140, 1120, N'Other Receivables', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 8, 3, 1150, 110, N'Allowance for Bad Debt', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 9, 3, 1160, 110, N'Inventory', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 10, 9, 1162, 1160, N'Raw Materials', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 11, 9, 1164, 1160, N'Work in Process', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 12, 9, 1166, 1160, N'Finished Goods', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 13, 3, 1170, 110, N'Deferred Taxes', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 14, 3, 1180, 110, N'Prepaid Expenses', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 15, 3, 1185, 110, N'Intercompany Receivables', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 17, 2, 1200, 10, N'Property, Plant, Equipment', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 18, 17, 1210, 1200, N'Land and Improvements', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 19, 17, 1220, 1200, N'Buildings and Improvements', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 20, 17, 1230, 1200, N'Machinery and Equipment', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 21, 17, 1240, 1200, N'Office Furniture and Equipment', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 22, 17, 1250, 1200, N'Leasehold Improvements', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 23, 17, 1260, 1200, N'Construction In Progress', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 24, 2, 1300, 10, N'Other Assets', N'Assets', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 25, 1, 20, 1, N'Liabilities and Owners Equity', N'Liabilities', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 26, 25, 210, 20, N'Liabilities', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 27, 26, 2200, 210, N'Current Liabilities', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 28, 27, 2210, 2200, N'Notes Payable', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 29, 27, 2230, 2200, N'Accounts Payable', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 30, 27, 2300, 2200, N'Accrued Expenses', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 31, 30, 2310, 2300, N'Salary and Other Comp', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 32, 30, 2320, 2300, N'Insurance', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 33, 30, 2330, 2300, N'Warranties', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 34, 27, 2340, 2200, N'Intercompany Payables', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 35, 27, 2350, 2200, N'Other Current Liabilities', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 36, 26, 2400, 210, N'Long Term Liabilities', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 37, 36, 2410, 2400, N'Long Term Obligations', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 38, 36, 2420, 2400, N'Pension Liability', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 39, 36, 2430, 2400, N'Other Retirement Benefits', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 40, 36, 2440, 2400, N'Other Long Term Liabilities', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 41, 25, 300, 20, N'Owners Equity', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 42, 41, 3010, 300, N'Partner Capital', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 43, 41, 3020, 300, N'Additional Paid In Capital', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 44, 41, 3030, 300, N'Retained Earnings', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 45, 44, 3540, 3030, N'Prior Year Retained Earnings', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 46, 44, 3550, 3030, N'Current Retained Earnings', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 47, NULL, 4, NULL, N'Net Income', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 48, 47, 40, 4, N'Operating Profit', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 49, 48, 400, 40, N'Gross Margin', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 50, 49, 4100, 400, N'Net Sales', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 51, 50, 4110, 4100, N'Gross Sales', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 52, 51, 4500, 4110, N'Intercompany Sales', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 53, 50, 4130, 4100, N'Returns and Adjustments', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 54, 50, 4140, 4100, N'Discounts', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 55, 49, 5000, 400, N'Total Cost of Sales', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 56, 55, 5020, 5000, N'Standard Cost of Sales', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 57, 55, 5050, 5000, N'Variances', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 58, 48, 60, 40, N'Operating Expenses', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 59, 58, 600, 60, N'Labor Expenses', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 60, 59, 6000, 600, N'Salaries', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 61, 59, 6020, 600, N'Payroll Taxes', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 62, 59, 6040, 600, N'Employee Benefits', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 63, 58, 6100, 60, N'Commissions', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 64, 58, 620, 60, N'Travel Expenses', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 65, 64, 6200, 620, N'Travel Transportation', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 66, 64, 6210, 620, N'Travel Lodging', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 67, 64, 6220, 620, N'Meals', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 68, 64, 6230, 620, N'Entertainment', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 69, 64, 6240, 620, N'Other Travel Related', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 70, 58, 630, 60, N'Marketing', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 71, 70, 6300, 630, N'Conferences', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 72, 70, 6310, 630, N'Marketing Collateral', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 73, 58, 6400, 60, N'Office Supplies', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 74, 58, 6500, 60, N'Professional Services', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 75, 58, 660, 60, N'Telephone and Utilities', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 76, 75, 6610, 660, N'Telephone', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 77, 75, 6620, 660, N'Utilities', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 78, 58, 6700, 60, N'Other Expenses', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 79, 58, 680, 60, N'Depreciation', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 80, 79, 6810, 680, N'Building Leasehold', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 81, 79, 6820, 680, N'Vehicles', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 82, 79, 6830, 680, N'Equipment', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 83, 79, 6840, 680, N'Furniture and Fixtures', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 84, 79, 6850, 680, N'Other Assets', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 85, 79, 6860, 680, N'Amortization of Goodwill', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 87, 58, 6920, 60, N'Rent', N'Expenditures', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 88, 47, 80, 4, N'Other Income and Expense', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 89, 88, 8000, 80, N'Interest Income', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 90, 88, 8010, 80, N'Interest Expense', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 91, 88, 8020, 80, N'Gain/Loss on Sales of Asset', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 92, 88, 8030, 80, N'Other Income', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 93, 88, 8040, 80, N'Curr Xchg Gain/(Loss)', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 94, 47, 8500, 4, N'Taxes', N'Expenditures', N'-', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 95, NULL, 9500, NULL, N'Statistical Accounts', N'Statistical', N'~', NULL, N'Units', NULL FROM DUAL UNION ALL
SELECT 96, 95, 9510, 9500, N'Headcount', N'Balances', N'~', NULL, N'Units', NULL FROM DUAL UNION ALL
SELECT 97, 95, 9520, 9500, N'Units', N'Flow', N'~', NULL, N'Units', NULL FROM DUAL UNION ALL
SELECT 98, 95, 9530, 9500, N'Average Unit Price', N'Balances', N'~', N'Account.Accounts.Account Level 04.and50/Account.Accounts.Account Level 02.and97', N'Currency', NULL FROM DUAL UNION ALL
SELECT 99, 95, 9540, 9500, N'Square Footage', N'Balances', N'~', NULL, N'Units', NULL FROM DUAL UNION ALL
SELECT 100, 27, 2220, 2200, N'Current Installments of Long-term Debt', N'Liabilities', N'+', NULL, N'Currency', NULL FROM DUAL UNION ALL
SELECT 101, 51, 4200, 4110, N'Trade Sales', N'Revenue', N'+', NULL, N'Currency', NULL FROM DUAL;

commit;
