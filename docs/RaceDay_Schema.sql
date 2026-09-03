CREATE DATABASE RaceDayDB;
GO
USE RaceDayDB;
GO
-- ============================================
-- RaceDay Database Schema
-- Part 1 - Section C: SQL Database Script
-- SQL Server Management Studio (SSMS)
-- ============================================

-- ============================================
-- 1. CREATE TABLES
-- ============================================

CREATE TABLE [User] (
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL DEFAULT 'Participant', -- 'Organiser' or 'Participant'
    PhoneNumber VARCHAR(20) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

CREATE TABLE Event (
    EventID INT IDENTITY(1,1) PRIMARY KEY,
    OrganiserID INT NOT NULL,
    EventName VARCHAR(150) NOT NULL,
    EventDate DATE NOT NULL,
    Location VARCHAR(150) NOT NULL,
    Description VARCHAR(500) NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Event_Organiser FOREIGN KEY (OrganiserID) REFERENCES [User](UserID)
);

CREATE TABLE Category (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    EventID INT NOT NULL,
    CategoryName VARCHAR(100) NOT NULL,
    MaxParticipants INT NOT NULL DEFAULT 100,
    EntryFee DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_Category_Event FOREIGN KEY (EventID) REFERENCES Event(EventID)
);

CREATE TABLE Enrolment (
    EnrolmentID INT IDENTITY(1,1) PRIMARY KEY,
    UserID INT NOT NULL,
    CategoryID INT NOT NULL,
    EnrolmentDate DATE NOT NULL DEFAULT GETDATE(),
    Status VARCHAR(20) NOT NULL DEFAULT 'Confirmed', -- 'Confirmed' or 'Pending'
    CONSTRAINT FK_Enrolment_User FOREIGN KEY (UserID) REFERENCES [User](UserID),
    CONSTRAINT FK_Enrolment_Category FOREIGN KEY (CategoryID) REFERENCES Category(CategoryID),
    CONSTRAINT UQ_Enrolment_UserCategory UNIQUE (UserID, CategoryID) -- prevents double-enrolling in same category
);

CREATE TABLE Result (
    ResultID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE, -- one-to-one with Enrolment
    FinishTime VARCHAR(20) NOT NULL, -- e.g. '01:45:32'
    Position INT NULL,
    RecordedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Result_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES Enrolment(EnrolmentID)
);

CREATE TABLE Payment (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentID INT NOT NULL UNIQUE, -- one-to-one with Enrolment
    Amount DECIMAL(10,2) NOT NULL,
    PaymentDate DATE NOT NULL DEFAULT GETDATE(),
    PaymentMethod VARCHAR(50) NULL,
    PaymentStatus VARCHAR(20) NOT NULL DEFAULT 'Paid', -- 'Paid' or 'Failed'
    CONSTRAINT FK_Payment_Enrolment FOREIGN KEY (EnrolmentID) REFERENCES Enrolment(EnrolmentID)
);

-- ============================================
-- 2. INSERT SAMPLE DATA
-- ============================================

-- Users: 2 Organisers, 2 Participants (minimum required)
INSERT INTO [User] (FullName, Email, PasswordHash, Role, PhoneNumber) VALUES
('Thabo Mokoena', 'thabo.mokoena@raceday.co.za', 'hashed_pw_1', 'Organiser', '0821234567'),
('Lindiwe Nkosi', 'lindiwe.nkosi@raceday.co.za', 'hashed_pw_2', 'Organiser', '0827654321'),
('Sipho Dlamini', 'sipho.dlamini@gmail.com', 'hashed_pw_3', 'Participant', '0731112222'),
('Aisha Patel', 'aisha.patel@gmail.com', 'hashed_pw_4', 'Participant', '0739998888');

-- Events: 3 Events (minimum required)
INSERT INTO Event (OrganiserID, EventName, EventDate, Location, Description) VALUES
(1, 'Johannesburg City Marathon', '2026-11-15', 'Johannesburg, Gauteng', 'Annual road race through the city centre.'),
(1, 'Soweto Fun Walk', '2026-10-05', 'Soweto, Gauteng', 'Community walk supporting local charities.'),
(2, 'Cape Town Cycle Challenge', '2026-12-01', 'Cape Town, Western Cape', 'Scenic cycling route along the coast.');

-- Categories: at least one per event
INSERT INTO Category (EventID, CategoryName, MaxParticipants, EntryFee) VALUES
(1, '10km', 500, 150.00),
(1, '21km', 300, 250.00),
(2, '5km Fun Walk', 1000, 50.00),
(3, '40km Road Cycle', 400, 300.00);

-- Enrolments: sample participants entering categories
INSERT INTO Enrolment (UserID, CategoryID, Status) VALUES
(3, 1, 'Confirmed'), -- Sipho enters 10km
(4, 2, 'Confirmed'), -- Aisha enters 21km
(3, 3, 'Confirmed'), -- Sipho enters Fun Walk
(4, 4, 'Confirmed'); -- Aisha enters Cycle Challenge

-- Results: sample finish data
INSERT INTO Result (EnrolmentID, FinishTime, Position) VALUES
(1, '00:52:14', 12),
(2, '01:48:37', 5);

-- Payments: sample payments for enrolments
INSERT INTO Payment (EnrolmentID, Amount, PaymentDate, PaymentMethod, PaymentStatus) VALUES
(1, 150.00, '2026-09-01', 'Card', 'Paid'),
(2, 250.00, '2026-09-01', 'EFT', 'Paid'),
(3, 50.00, '2026-09-02', 'Card', 'Paid'),
(4, 300.00, '2026-09-02', 'Card', 'Paid');