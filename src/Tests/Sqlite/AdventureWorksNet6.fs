namespace SqlHydra.Sqlite

module main =
    type Address =
        { AddressID: int
          AddressLine1: int
          AddressLine2: int
          City: int
          StateProvince: int
          CountryRegion: int
          PostalCode: int
          rowguid: int
          ModifiedDate: int }

    type BuildVersion =
        { SystemInformationID: int
          Database Version: int
          VersionDate: int
          ModifiedDate: int }

    type Customer =
        { CustomerID: int
          NameStyle: int
          Title: int
          FirstName: int
          MiddleName: int
          LastName: int
          Suffix: int
          CompanyName: int
          SalesPerson: int
          EmailAddress: int
          Phone: int
          PasswordHash: int
          PasswordSalt: int
          rowguid: int
          ModifiedDate: int }

    type CustomerAddress =
        { CustomerID: int
          AddressID: int
          AddressType: int
          rowguid: int
          ModifiedDate: int }

    type ErrorLog =
        { ErrorLogID: int
          ErrorTime: int
          UserName: int
          ErrorNumber: int
          ErrorSeverity: int
          ErrorState: int
          ErrorProcedure: int
          ErrorLine: int
          ErrorMessage: int }

    type Product =
        { ProductID: int
          Name: int
          ProductNumber: int
          Color: int
          StandardCost: int
          ListPrice: int
          Size: int
          Weight: int
          ProductCategoryID: int
          ProductModelID: int
          SellStartDate: int
          SellEndDate: int
          DiscontinuedDate: int
          ThumbNailPhoto: int
          ThumbnailPhotoFileName: int
          rowguid: int
          ModifiedDate: int }

    type ProductCategory =
        { ProductCategoryID: int
          ParentProductCategoryID: int
          Name: int
          rowguid: int
          ModifiedDate: int }

    type ProductDescription =
        { ProductDescriptionID: int
          Description: int
          rowguid: int
          ModifiedDate: int }

    type ProductModel =
        { ProductModelID: int
          Name: int
          CatalogDescription: int
          rowguid: int
          ModifiedDate: int }

    type ProductModelProductDescription =
        { ProductModelID: int
          ProductDescriptionID: int
          Culture: int
          rowguid: int
          ModifiedDate: int }

    type SalesOrderDetail =
        { SalesOrderID: int
          SalesOrderDetailID: int
          OrderQty: int
          ProductID: int
          UnitPrice: int
          UnitPriceDiscount: int
          LineTotal: int
          rowguid: int
          ModifiedDate: int }

    type SalesOrderHeader =
        { SalesOrderID: int
          RevisionNumber: int
          OrderDate: int
          DueDate: int
          ShipDate: int
          Status: int
          OnlineOrderFlag: int
          SalesOrderNumber: int
          PurchaseOrderNumber: int
          AccountNumber: int
          CustomerID: int
          ShipToAddressID: int
          BillToAddressID: int
          ShipMethod: int
          CreditCardApprovalCode: int
          SubTotal: int
          TaxAmt: int
          Freight: int
          TotalDue: int
          Comment: int
          rowguid: int
          ModifiedDate: int }
