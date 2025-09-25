-- ============================================
-- CommerceDB - Complete Database Script
-- Baseado nas tabelas criadas pelo usuário
-- ============================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'CommerceDB')
BEGIN
    DROP DATABASE CommerceDB;
END
GO

-- Create database
CREATE DATABASE CommerceDB;
GO

USE CommerceDB;
GO

-- ============================================
-- CREATE TABLES
-- ============================================

-- 1. Categories Table
CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    ParentId UNIQUEIDENTIFIER NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Categories_Parent FOREIGN KEY (ParentId) REFERENCES Categories(Id)
);

-- 2. Brands Table
CREATE TABLE Brands (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 3. Users Table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(256) NOT NULL UNIQUE,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(20) DEFAULT 'Customer',
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- 4. Products Table
CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NTEXT,
    Price DECIMAL(18,2) NOT NULL,
    Inventory INT DEFAULT 0,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    BrandId UNIQUEIDENTIFIER,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Products_Category FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT FK_Products_Brand FOREIGN KEY (BrandId) REFERENCES Brands(Id)
);

-- 5. ProductImages Table
CREATE TABLE ProductImages (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Url NVARCHAR(500) NOT NULL,
    IsMain BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_ProductImages_Product FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- 6. Addresses Table
CREATE TABLE Addresses (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Street NVARCHAR(200) NOT NULL,
    Number NVARCHAR(20) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    State NVARCHAR(100) NOT NULL,
    ZipCode NVARCHAR(20) NOT NULL,
    IsDefault BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Addresses_User FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 7. Carts Table
CREATE TABLE Carts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Carts_User FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- 8. CartItems Table
CREATE TABLE CartItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CartId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_CartItems_Cart FOREIGN KEY (CartId) REFERENCES Carts(Id),
    CONSTRAINT FK_CartItems_Product FOREIGN KEY (ProductId) REFERENCES Products(Id),
    CONSTRAINT UQ_CartItems UNIQUE (CartId, ProductId)
);

-- 9. Orders Table
CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    TotalAmount DECIMAL(18,2) NOT NULL,
    ShippingAddressId UNIQUEIDENTIFIER NOT NULL,
    PlacedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Orders_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    CONSTRAINT FK_Orders_Address FOREIGN KEY (ShippingAddressId) REFERENCES Addresses(Id)
);

-- 10. OrderItems Table
CREATE TABLE OrderItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    TotalPrice DECIMAL(18,2) NOT NULL,
    
    CONSTRAINT FK_OrderItems_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id),
    CONSTRAINT FK_OrderItems_Product FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- 11. Payments Table
CREATE TABLE Payments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Method NVARCHAR(20) NOT NULL,
    Status NVARCHAR(20) DEFAULT 'Pending',
    ProcessedAt DATETIME2,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Payments_Order FOREIGN KEY (OrderId) REFERENCES Orders(Id)
);

-- ============================================
-- CREATE INDEXES FOR PERFORMANCE
-- ============================================

CREATE INDEX IX_Categories_ParentId ON Categories(ParentId);
CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_BrandId ON Products(BrandId);
CREATE INDEX IX_Products_IsActive ON Products(IsActive);
CREATE INDEX IX_ProductImages_ProductId ON ProductImages(ProductId);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role ON Users(Role);
CREATE INDEX IX_Addresses_UserId ON Addresses(UserId);
CREATE INDEX IX_Carts_UserId ON Carts(UserId);
CREATE INDEX IX_CartItems_CartId ON CartItems(CartId);
CREATE INDEX IX_CartItems_ProductId ON CartItems(ProductId);
CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_Status ON Orders(Status);
CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_Payments_OrderId ON Payments(OrderId);

-- ============================================
-- SAMPLE DATA INSERTS
-- ============================================

-- Categories
DECLARE @ElectronicsId UNIQUEIDENTIFIER = NEWID();
DECLARE @ClothingId UNIQUEIDENTIFIER = NEWID();
DECLARE @HomeId UNIQUEIDENTIFIER = NEWID();
DECLARE @SportsId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Categories (Id, Name, ParentId) VALUES 
(@ElectronicsId, 'Eletrônicos', NULL),
(@ClothingId, 'Roupas', NULL),
(@HomeId, 'Casa e Decoração', NULL),
(@SportsId, 'Esportes', NULL),
(NEWID(), 'Smartphones', @ElectronicsId),
(NEWID(), 'Notebooks', @ElectronicsId),
(NEWID(), 'Camisetas', @ClothingId),
(NEWID(), 'Calças', @ClothingId),
(NEWID(), 'Móveis', @HomeId),
(NEWID(), 'Tênis', @SportsId);

-- Brands
DECLARE @AppleId UNIQUEIDENTIFIER = NEWID();
DECLARE @SamsungId UNIQUEIDENTIFIER = NEWID();
DECLARE @NikeId UNIQUEIDENTIFIER = NEWID();
DECLARE @AdidasId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Brands (Id, Name) VALUES 
(@AppleId, 'Apple'),
(@SamsungId, 'Samsung'),
(@NikeId, 'Nike'),
(@AdidasId, 'Adidas'),
(NEWID(), 'Dell'),
(NEWID(), 'HP'),
(NEWID(), 'Zara'),
(NEWID(), 'H&M');

-- Admin User
DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, PasswordHash, Role) VALUES 
(@AdminId, 'admin@commercecore.com', 'Admin', 'System', 'AQAAAAEAACcQAAAAEJ8+fGX...', 'Admin');

-- Sample Customer
DECLARE @CustomerId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Users (Id, Email, FirstName, LastName, PasswordHash, Role) VALUES 
(@CustomerId, 'cliente@email.com', 'João', 'Silva', 'AQAAAAEAACcQAAAAEJ8+fGX...', 'Customer');

-- Sample Address
DECLARE @AddressId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Addresses (Id, UserId, Street, Number, City, State, ZipCode, IsDefault) VALUES 
(@AddressId, @CustomerId, 'Rua das Flores', '123', 'São Paulo', 'SP', '01234-567', 1);

-- Sample Products
DECLARE @iPhoneId UNIQUEIDENTIFIER = NEWID();
DECLARE @SamsungId_Product UNIQUEIDENTIFIER = NEWID();
DECLARE @NikeShoeId UNIQUEIDENTIFIER = NEWID();

-- Get category IDs for products
DECLARE @SmartphonesCatId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = 'Smartphones');
DECLARE @TenisCatId UNIQUEIDENTIFIER = (SELECT Id FROM Categories WHERE Name = 'Tênis');

INSERT INTO Products (Id, Name, Description, Price, Inventory, CategoryId, BrandId) VALUES 
(@iPhoneId, 'iPhone 15 Pro', 'iPhone 15 Pro 256GB Titânio Natural', 8999.00, 50, @SmartphonesCatId, @AppleId),
(@SamsungId_Product, 'Galaxy S24 Ultra', 'Samsung Galaxy S24 Ultra 512GB', 7499.00, 30, @SmartphonesCatId, @SamsungId),
(@NikeShoeId, 'Air Max 270', 'Tênis Nike Air Max 270 Masculino', 599.90, 100, @TenisCatId, @NikeId);

-- Sample Product Images
INSERT INTO ProductImages (ProductId, Url, IsMain) VALUES 
(@iPhoneId, 'https://example.com/iphone15pro-main.jpg', 1),
(@iPhoneId, 'https://example.com/iphone15pro-2.jpg', 0),
(@SamsungId_Product, 'https://example.com/galaxy-s24-main.jpg', 1),
(@NikeShoeId, 'https://example.com/airmax270-main.jpg', 1);

-- Sample Cart
DECLARE @CartId UNIQUEIDENTIFIER = NEWID();
INSERT INTO Carts (Id, UserId) VALUES (@CartId, @CustomerId);

-- Sample Cart Items
INSERT INTO CartItems (CartId, ProductId, Quantity) VALUES 
(@CartId, @iPhoneId, 1),
(@CartId, @NikeShoeId, 2);

GO

-- ============================================
-- VERIFICATION QUERIES
-- ============================================

-- Check record counts
SELECT 'Categories' as TableName, COUNT(*) as RecordCount FROM Categories
UNION ALL
SELECT 'Brands', COUNT(*) FROM Brands
UNION ALL
SELECT 'Users', COUNT(*) FROM Users
UNION ALL
SELECT 'Products', COUNT(*) FROM Products
UNION ALL
SELECT 'ProductImages', COUNT(*) FROM ProductImages
UNION ALL
SELECT 'Addresses', COUNT(*) FROM Addresses
UNION ALL
SELECT 'Carts', COUNT(*) FROM Carts
UNION ALL
SELECT 'CartItems', COUNT(*) FROM CartItems;

PRINT 'CommerceDB created successfully with sample data!';