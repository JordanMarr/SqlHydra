

ALTER TABLE DimAccount ADD  CONSTRAINT FK_DimAccount_DimAccount FOREIGN KEY(ParentAccountKey)
REFERENCES DimAccount (AccountKey)
;

ALTER TABLE DimCustomer ADD  CONSTRAINT FK_DimCustomer_DimGeography FOREIGN KEY(GeographyKey)
REFERENCES DimGeography (GeographyKey)
;



ALTER TABLE DimDepartmentGroup ADD  CONSTRAINT FK_DimDeptGroup_DimDeptGroup FOREIGN KEY(ParentDepartmentGroupKey)
REFERENCES DimDepartmentGroup (DepartmentGroupKey)
;



ALTER TABLE DimEmployee ADD  CONSTRAINT FK_DimEmployee_DimEmployee FOREIGN KEY(ParentEmployeeKey)
REFERENCES DimEmployee (EmployeeKey)
;



ALTER TABLE DimEmployee ADD  CONSTRAINT FK_DimEmployee_DimSalesTerr FOREIGN KEY(SalesTerritoryKey)
REFERENCES DimSalesTerritory (SalesTerritoryKey)
;



ALTER TABLE DimGeography ADD  CONSTRAINT FK_DimGeography_DimSalesTerr FOREIGN KEY(SalesTerritoryKey)
REFERENCES DimSalesTerritory (SalesTerritoryKey)
;



ALTER TABLE DimOrganization ADD  CONSTRAINT FK_DimOrg_DimCurrency FOREIGN KEY(CurrencyKey)
REFERENCES DimCurrency (CurrencyKey)
;


ALTER TABLE DimOrganization ADD  CONSTRAINT FK_DimOrg_DimOrg FOREIGN KEY(ParentOrganizationKey)
REFERENCES DimOrganization (OrganizationKey)
;



ALTER TABLE DimProduct ADD  CONSTRAINT FK_DimProduct_DimProductSubcat FOREIGN KEY(ProductSubcategoryKey)
REFERENCES DimProductSubcategory (ProductSubcategoryKey)
;



ALTER TABLE DimProductSubcategory ADD  CONSTRAINT FK_DimProdSubcat_DimProdCat FOREIGN KEY(ProductCategoryKey)
REFERENCES DimProductCategory (ProductCategoryKey)
;


ALTER TABLE DimReseller ADD  CONSTRAINT FK_DimReseller_DimGeo FOREIGN KEY(GeographyKey)
REFERENCES DimGeography (GeographyKey)
;



ALTER TABLE FactCurrencyRate ADD  CONSTRAINT FK_FactCurrRate_DimCurr FOREIGN KEY(CurrencyKey)
REFERENCES DimCurrency (CurrencyKey)
;



ALTER TABLE FactCurrencyRate ADD  CONSTRAINT FK_FactCurrRate_DimTime FOREIGN KEY(TimeKey)
REFERENCES DimTime (TimeKey)
;



ALTER TABLE FactFinance ADD  CONSTRAINT FK_FactFinance_DimAccount FOREIGN KEY(AccountKey)
REFERENCES DimAccount (AccountKey)
;



ALTER TABLE FactFinance ADD  CONSTRAINT FK_FactFinance_DimDeptGroup FOREIGN KEY(DepartmentGroupKey)
REFERENCES DimDepartmentGroup (DepartmentGroupKey)
;



ALTER TABLE FactFinance ADD  CONSTRAINT FK_FactFinance_DimOrg FOREIGN KEY(OrganizationKey)
REFERENCES DimOrganization (OrganizationKey)
;



ALTER TABLE FactFinance ADD  CONSTRAINT FK_FactFinance_DimScenario FOREIGN KEY(ScenarioKey)
REFERENCES DimScenario (ScenarioKey)
;



ALTER TABLE FactFinance ADD  CONSTRAINT FK_FactFinance_DimTime FOREIGN KEY(TimeKey)
REFERENCES DimTime (TimeKey)
;



ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactInternetSales_DimCurr FOREIGN KEY(CurrencyKey)
REFERENCES DimCurrency (CurrencyKey)
;



ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactInternetSales_DimCust FOREIGN KEY(CustomerKey)
REFERENCES DimCustomer (CustomerKey)
;


ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactInternetSales_DimProd FOREIGN KEY(ProductKey)
REFERENCES DimProduct (ProductKey)
;



ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactInternetSales_DimPromo FOREIGN KEY(PromotionKey)
REFERENCES DimPromotion (PromotionKey)
;



ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactNetSales_DimSalesTerr FOREIGN KEY(SalesTerritoryKey)
REFERENCES DimSalesTerritory (SalesTerritoryKey)
;



ALTER TABLE FactInternetSales ADD  CONSTRAINT FK_FactInternetSales_DimTime FOREIGN KEY(OrderDateKey)
REFERENCES DimTime (TimeKey)
;

