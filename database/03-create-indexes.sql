USE [RequestsManagement];
GO

-- Each index serves one filter and/or one sort field; CreatedAt is the trailing key so the
-- default sort (newest first) and date-range filter are served by the same index.

-- Status filter + aggregation "count by status" (Priority included -> covering for status/priority counts)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_Status_CreatedAt')
    CREATE INDEX IX_Requests_Status_CreatedAt ON dbo.Requests (Status, CreatedAt DESC) INCLUDE (Priority);

-- Priority filter + aggregation "count by priority"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_Priority_CreatedAt')
    CREATE INDEX IX_Requests_Priority_CreatedAt ON dbo.Requests (Priority, CreatedAt DESC);

-- AssignedTo filter (also supports the foreign key)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_AssignedToUserId_CreatedAt')
    CREATE INDEX IX_Requests_AssignedToUserId_CreatedAt ON dbo.Requests (AssignedToUserId, CreatedAt DESC);

-- OrganizationName equality / prefix filter (LIKE 'abc%'). The optimizer uses a seek only when the prefix is selective
-- (the index is not covering); otherwise it scans, which is cheaper (see docs/performance.md)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_OrganizationName_CreatedAt')
    CREATE INDEX IX_Requests_OrganizationName_CreatedAt ON dbo.Requests (OrganizationName, CreatedAt DESC);

-- Title sort. Free text search uses LIKE '%x%', which cannot use this index (see docs/performance.md)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_Title')
    CREATE INDEX IX_Requests_Title ON dbo.Requests (Title);

-- Date range filter and default sort
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_CreatedAt')
    CREATE INDEX IX_Requests_CreatedAt ON dbo.Requests (CreatedAt DESC);

-- Sort by UpdatedAt
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Requests_UpdatedAt')
    CREATE INDEX IX_Requests_UpdatedAt ON dbo.Requests (UpdatedAt DESC);
GO
