USE [RequestsManagement];
GO

-- Status:   0=New, 1=InProgress, 2=Waiting, 3=Completed
-- Priority: 0=Low, 1=Medium, 2=High

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        UserName    NVARCHAR(100)     NOT NULL,
        DisplayName NVARCHAR(200)     NOT NULL,
        Email       NVARCHAR(256)     NULL,
        IsActive    BIT               NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt   DATETIME2(3)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Users_UserName UNIQUE (UserName)
    );
END;
GO

IF OBJECT_ID(N'dbo.Requests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Requests
    (
        Id               INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Requests PRIMARY KEY,
        Title            NVARCHAR(200)     NOT NULL,
        OrganizationName NVARCHAR(200)     NOT NULL,
        Status           TINYINT           NOT NULL CONSTRAINT DF_Requests_Status DEFAULT (0),
        Priority         TINYINT           NOT NULL CONSTRAINT DF_Requests_Priority DEFAULT (1),
        AssignedToUserId INT               NULL,
        CreatedAt        DATETIME2(3)      NOT NULL CONSTRAINT DF_Requests_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        DATETIME2(3)      NOT NULL CONSTRAINT DF_Requests_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        RowVersion       ROWVERSION        NOT NULL, -- optimistic concurrency token
        CONSTRAINT CK_Requests_Status   CHECK (Status   BETWEEN 0 AND 3),
        CONSTRAINT CK_Requests_Priority CHECK (Priority BETWEEN 0 AND 2),
        CONSTRAINT FK_Requests_Users_AssignedTo FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.RequestStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RequestStatusHistory
    (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RequestStatusHistory PRIMARY KEY,
        RequestId        INT          NOT NULL,
        PreviousStatus   TINYINT      NOT NULL,
        NewStatus        TINYINT      NOT NULL,
        ChangedAt        DATETIME2(3) NOT NULL CONSTRAINT DF_RSH_ChangedAt DEFAULT (SYSUTCDATETIME()),
        ChangedByUserId  INT          NOT NULL,
        CONSTRAINT CK_RSH_PreviousStatus CHECK (PreviousStatus BETWEEN 0 AND 3),
        CONSTRAINT CK_RSH_NewStatus      CHECK (NewStatus      BETWEEN 0 AND 3),
        CONSTRAINT FK_RSH_Requests FOREIGN KEY (RequestId)       REFERENCES dbo.Requests (Id),
        CONSTRAINT FK_RSH_Users    FOREIGN KEY (ChangedByUserId) REFERENCES dbo.Users (Id)
    );

    -- History is always read per request, newest first
    CREATE INDEX IX_RequestStatusHistory_RequestId_ChangedAt
        ON dbo.RequestStatusHistory (RequestId, ChangedAt DESC);
END;
GO
