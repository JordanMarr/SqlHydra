CREATE TABLE DimAccount(
	AccountKey int PRIMARY KEY NOT NULL,
	ParentAccountKey int NULL,
	AccountCodeAlternateKey int NULL,
	ParentAccountCodeAlternateKey int NULL,
	AccountDescription varchar2(50) NULL,
	AccountType varchar2(50) NULL,
	Operator varchar2(50) NULL,
	CustomMembers varchar2(300) NULL,
	ValueType varchar2(50) NULL,
	CustomMemberOptions varchar2(200) NULL
	);
	
CREATE TABLE DimCurrency(
	CurrencyKey int PRIMARY KEY NOT NULL,
	CurrencyAlternateKey nchar(3) NOT NULL,
	CurrencyName varchar2(50) NOT NULL
	);



CREATE TABLE DimCustomer(
	CustomerKey int PRIMARY KEY NOT NULL,
	GeographyKey int NULL,
	CustomerAlternateKey varchar2(15) NOT NULL,
	Title varchar2(8) NULL,
	FirstName varchar2(50) NULL,
	MiddleName varchar2(50) NULL,
	LastName varchar2(50) NULL,
	NameStyle NUMBER NULL,
	BirthDate TIMESTAMP NULL,
	MaritalStatus nchar(1) NULL,
	Suffix varchar2(10) NULL,
	Gender varchar2(1) NULL,
	EmailAddress varchar2(50) NULL,
	YearlyIncome numeric NULL,
	TotalChildren NUMBER NULL,
	NumberChildrenAtHome NUMBER NULL,
	EnglishEducation varchar2(40) NULL,
	EnglishOccupation varchar2(100) NULL,
	HouseOwnerFlag nchar(1) NULL,
	NumberCarsOwned NUMBER NULL,
	AddressLine1 varchar2(120) NULL,
	AddressLine2 varchar2(120) NULL,
	Phone varchar2(20) NULL,
	DateFirstPurchase TIMESTAMP NULL,
	CommuteDistance varchar2(15) NULL
	);

CREATE TABLE DimDepartmentGroup(
	DepartmentGroupKey int PRIMARY KEY NOT NULL,
	ParentDepartmentGroupKey int NULL,
	DepartmentGroupName varchar2(50) NULL
	);
	
CREATE TABLE DimEmployee(
	EmployeeKey int PRIMARY KEY NOT NULL,
	ParentEmployeeKey int NULL,
	EmployeeNationalIDAlternateKey varchar2(15) NULL,
	ParentEmployeeNationalIDAltKey varchar2(15) NULL,
	SalesTerritoryKey int NULL,
	FirstName varchar2(50) NOT NULL,
	LastName varchar2(50) NOT NULL,
	MiddleName varchar2(50) NULL,
	NameStyle NUMBER NOT NULL,
	Title varchar2(50) NULL,
	HireDate NUMBER NULL,
	BirthDate NUMBER NULL,
	LoginID varchar2(256) NULL,
	EmailAddress varchar2(50) NULL,
	Phone varchar2(25) NULL,
	MaritalStatus nchar(1) NULL,
	EmergencyContactName varchar2(50) NULL,
	EmergencyContactPhone varchar2(25) NULL,
	SalariedFlag NUMBER NULL,
	Gender nchar(1) NULL,
	PayFrequency NUMBER NULL,
	BaseRate numeric NULL,
	VacationHours NUMBER NULL,
	SickLeaveHours NUMBER NULL,
	CurrentFlag NUMBER NOT NULL,
	SalesPersonFlag NUMBER NOT NULL,
	DepartmentName varchar2(50) NULL,
	StartDate NUMBER NULL,
	EndDate NUMBER NULL,
	Status varchar2(50) NULL
	);
	
CREATE TABLE DimGeography(
	GeographyKey int PRIMARY KEY NOT NULL,
	City varchar2(30) NULL,
	StateProvinceCode varchar2(3) NULL,
	StateProvinceName varchar2(50) NULL,
	CountryRegionCode varchar2(3) NULL,
	EnglishCountryRegionName varchar2(50) NULL,
	SpanishCountryRegionName varchar2(50) NULL,
	FrenchCountryRegionName varchar2(50) NULL,
	PostalCode varchar2(15) NULL,
	SalesTerritoryKey int NULL);
	
	
CREATE TABLE DimOrganization(
	OrganizationKey int PRIMARY KEY NOT NULL,
	ParentOrganizationKey int NULL,
	PercentageOfOwnership varchar2(16) NULL,
	OrganizationName varchar2(50) NULL,
	CurrencyKey int NULL);
	
	
CREATE TABLE DimProduct(
	ProductKey int PRIMARY KEY NOT NULL,
	ProductAlternateKey varchar2(25) NULL,
	ProductSubcategoryKey int NULL,
	WeightUnitMeasureCode nchar(3) NULL,
	SizeUnitMeasureCode nchar(3) NULL,
	EnglishProductName varchar2(50) NOT NULL,
	StandardCost numeric NULL,
	FinishedGoodsFlag NUMBER NOT NULL,
	Color varchar2(15) NOT NULL,
	SafetyStockLevel NUMBER NULL,
	ReorderPoint NUMBER NULL,
	ListPrice decimal(13,2) NULL,
	SizeActual varchar2(50) NULL,
	SizeRange varchar2(50) NULL,
	Weight float NULL,
	DaysToManufacture int NULL,
	ProductLine nchar(2) NULL,
	DealerPrice numeric NULL,
	Class nchar(2) NULL,
	Style nchar(2) NULL,
	ModelName varchar2(50) NULL,
	EnglishDescription varchar2(400) NULL,
	StartDate TIMESTAMP NULL,
	EndDate TIMESTAMP NULL,
	Status varchar2(7) NULL);

CREATE TABLE DimProductCategory(
	ProductCategoryKey int PRIMARY KEY NOT NULL,
	ProductCategoryAlternateKey int NULL,
	EnglishProductCategoryName varchar2(50) NOT NULL,
	SpanishProductCategoryName varchar2(50) NOT NULL,
	FrenchProductCategoryName varchar2(50) NOT NULL);
	
CREATE TABLE DimProductSubcategory(
	ProductSubcategoryKey int PRIMARY KEY NOT NULL,
	ProductSubcategoryAlternateKey int NULL,
	EnglishProductSubcategoryName varchar2(50) NOT NULL,
	SpanishProductSubcategoryName varchar2(50) NOT NULL,
	FrenchProductSubcategoryName varchar2(50) NOT NULL,
	ProductCategoryKey int NULL);
	
CREATE TABLE DimPromotion(
	PromotionKey int PRIMARY KEY NOT NULL,
	PromotionAlternateKey int NULL,
	EnglishPromotionName varchar2(255) NULL,
	SpanishPromotionName varchar2(255) NULL,
	FrenchPromotionName varchar2(255) NULL,
	DiscountPct float NULL,
	EnglishPromotionType varchar2(50) NULL,
	SpanishPromotionType varchar2(50) NULL,
	FrenchPromotionType varchar2(50) NULL,
	EnglishPromotionCategory varchar2(50) NULL,
	SpanishPromotionCategory varchar2(50) NULL,
	FrenchPromotionCategory varchar2(50) NULL,
	StartDate NUMBER NOT NULL,
	EndDate NUMBER NULL,
	MinQty int NULL,
	MaxQty int NULL
	);
	
CREATE TABLE DimReseller(
	ResellerKey int PRIMARY KEY NOT NULL,
	GeographyKey int NULL,
	ResellerAlternateKey varchar2(15) NULL,
	Phone varchar2(25) NULL,
	BusinessType varchar(20) NOT NULL,
	ResellerName varchar2(50) NOT NULL,
	NumberEmployees int NULL,
	OrderFrequency char(1) NULL,
	OrderMonth NUMBER NULL,
	FirstOrderYear int NULL,
	LastOrderYear int NULL,
	ProductLine varchar2(50) NULL,
	AddressLine1 varchar2(60) NULL,
	AddressLine2 varchar2(60) NULL,
	AnnualSales numeric NULL,
	BankName varchar2(50) NULL,
	MinPaymentType NUMBER NULL,
	MinPaymentAmount numeric NULL,
	AnnualRevenue numeric NULL,
	YearOpened int NULL);
	
CREATE TABLE DimSalesReason(
	SalesReasonKey int PRIMARY KEY NOT NULL,
	SalesReasonAlternateKey int NOT NULL,
	SalesReasonName varchar2(50) NOT NULL,
	SalesReasonReasonType varchar2(50) NOT NULL);
	
CREATE TABLE DimSalesTerritory(
	SalesTerritoryKey int PRIMARY KEY NOT NULL,
	SalesTerritoryAlternateKey int NULL,
	SalesTerritoryRegion varchar2(50) NOT NULL,
	SalesTerritoryCountry varchar2(50) NOT NULL,
	SalesTerritoryGroup varchar2(50) NULL);
	
CREATE TABLE DimScenario(
	ScenarioKey int PRIMARY KEY NOT NULL,
	ScenarioName varchar2(50) NULL
	);
	

CREATE TABLE DimTime(
	TimeKey int PRIMARY KEY NOT NULL,
	FullDateAlternateKey TIMESTAMP NULL,
	DayNumberOfWeek NUMBER NULL,
	EnglishDayNameOfWeek varchar2(10) NULL,
	SpanishDayNameOfWeek varchar2(10) NULL,
	FrenchDayNameOfWeek varchar2(10) NULL,
	DayNumberOfMonth NUMBER NULL,
	DayNumberOfYear NUMBER NULL,
	WeekNumberOfYear NUMBER NULL,
	EnglishMonthName varchar2(10) NULL,
	SpanishMonthName varchar2(10) NULL,
	FrenchMonthName varchar2(10) NULL,
	MonthNumberOfYear NUMBER NULL,
	CalendarQuarter NUMBER NULL,
	CalendarYear char(4) NULL,
	CalendarSemester NUMBER NULL,
	FiscalQuarter NUMBER NULL,
	FiscalYear char(4) NULL,
	FiscalSemester NUMBER NULL
	);
  
  
CREATE TABLE FactCurrencyRate
(
	CurrencyKey int NOT NULL,
	TimeKey int NOT NULL,
	AverageRate float NOT NULL,
	EndOfDayRate float NOT NULL
);
  
	
CREATE TABLE FactFinance(
	TimeKey int NULL,
	OrganizationKey int NULL,
	DepartmentGroupKey int NULL,
	ScenarioKey int NULL,
	AccountKey int NULL,
	Amount float NULL
) ;

CREATE TABLE FactInternetSales(
	ProductKey int NOT NULL,
	OrderDateKey int NOT NULL,
	DueDateKey int NOT NULL,
	ShipDateKey int NOT NULL,
	CustomerKey int NOT NULL,
	PromotionKey int NOT NULL,
	CurrencyKey int NOT NULL,
	SalesTerritoryKey int NOT NULL,
	SalesOrderNumber varchar2(20) NOT NULL,
	SalesOrderLineNumber NUMBER NOT NULL,
	RevisionNumber NUMBER NULL,
	OrderQuantity NUMBER NULL,
	UnitPrice numeric NULL,
	ExtendedAmount numeric NULL,
	UnitPriceDiscountPct float NULL,
	DiscountAmount float NULL,
	ProductStandardCost numeric NULL,
	TotalProductCost numeric NULL,
	SalesAmount numeric NULL,
	TaxAmt numeric NULL,
	Freight numeric NULL,
	CarrierTrackingNumber varchar2(25) NULL,
	CustomerPONumber varchar2(25) NULL
	);