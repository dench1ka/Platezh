CREATE DATABASE PlatezhDB;
GO
USE PlatezhDB;
GO

CREATE TABLE Role (
    RoleID   INT IDENTITY PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL
);

CREATE TABLE Users (
    UserID       INT IDENTITY PRIMARY KEY,
    Login        NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash CHAR(64) NOT NULL,         -- SHA-256 hex
    RoleID       INT NOT NULL REFERENCES Role(RoleID),
    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE(),
    IsActive     BIT NOT NULL DEFAULT 1
);

CREATE TABLE Clients (
    ClientID       INT IDENTITY PRIMARY KEY,
    Name           NVARCHAR(100) NOT NULL,
    Surname        NVARCHAR(100) NOT NULL,
    FatherName     NVARCHAR(100),
    PassportNumber NVARCHAR(20),
    IssuedBy       NVARCHAR(200),
    IssuedDate     DATE,
    Address        NVARCHAR(300)
);

CREATE TABLE Units (
    UnitID INT IDENTITY PRIMARY KEY,
    Name   NVARCHAR(50) NOT NULL
);

CREATE TABLE Materials (
    MaterialID INT IDENTITY PRIMARY KEY,
    Name       NVARCHAR(200) NOT NULL,
    UnitID     INT NOT NULL REFERENCES Units(UnitID),
    IsActive   BIT NOT NULL DEFAULT 1
);

CREATE TABLE MaterialPrices (
    PriceID         INT IDENTITY PRIMARY KEY,
    MaterialID      INT NOT NULL REFERENCES Materials(MaterialID),
    PriceWithoutNds DECIMAL(10, 2) NOT NULL,
    NdsPercent      DECIMAL(5, 2) NOT NULL DEFAULT 0,
    ValidFrom       DATE NOT NULL,
    ValidTo         DATE,
    StockAmount     INT NOT NULL DEFAULT 0,
    IsActive        BIT NOT NULL DEFAULT 1
);

-- ServiceId присваивается вручную в приложении, не IDENTITY
CREATE TABLE Services (
    ServiceId    INT PRIMARY KEY,
    Name         NVARCHAR(200) NOT NULL,
    BasePrice    DECIMAL(10, 2) NOT NULL,
    IsActive     BIT NOT NULL DEFAULT 1,
    AddMaterials DECIMAL(10, 2) NOT NULL DEFAULT 0  -- доп. стоимость материалов к услуге
);

CREATE TABLE PaymentType (
    PaymentTypeID INT IDENTITY PRIMARY KEY,
    PaymentName   NVARCHAR(100) NOT NULL
);

CREATE TABLE Contracts (
    ContractID       INT IDENTITY PRIMARY KEY,
    ContractNumber   NVARCHAR(50) NOT NULL,
    ClientID         INT NOT NULL REFERENCES Clients(ClientID),
    ContractDate     DATETIME NOT NULL DEFAULT GETDATE(),
    TotalWithoutNds  DECIMAL(12, 2) NOT NULL,
    TotalNds         DECIMAL(12, 2) NOT NULL DEFAULT 0,
    TotalAmount      DECIMAL(12, 2) NOT NULL,
    Status           NVARCHAR(50) NOT NULL,
    CreatedBy        INT NOT NULL REFERENCES Users(UserID)
);

CREATE TABLE ContractItems (
    ItemID          INT IDENTITY PRIMARY KEY,
    ContractID      INT NOT NULL REFERENCES Contracts(ContractID),
    ItemType        NVARCHAR(20) NOT NULL,   -- 'Material' или 'Service'
    Quantity        INT NOT NULL DEFAULT 1,
    PriceWithoutNds DECIMAL(10, 2) NOT NULL,
    NdsPercent      DECIMAL(5, 2) NOT NULL DEFAULT 0,
    Total           DECIMAL(12, 2) NOT NULL,
    MaterialName    NVARCHAR(200),
    ServiceName     NVARCHAR(200)
);

CREATE TABLE Payments (
    PaymentID     INT IDENTITY PRIMARY KEY,
    ContractID    INT NOT NULL REFERENCES Contracts(ContractID),
    PaymentDate   DATETIME NOT NULL DEFAULT GETDATE(),
    Amount        DECIMAL(12, 2) NOT NULL,
    PaymentTypeID INT NOT NULL REFERENCES PaymentType(PaymentTypeID)
);
