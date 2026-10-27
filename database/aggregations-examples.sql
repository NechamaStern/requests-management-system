USE [RequestsManagement];
GO

-- Aggregation 1: count by status
SELECT Status, COUNT(*) AS Total FROM dbo.Requests GROUP BY Status;

-- Aggregation 2: count by priority
SELECT Priority, COUNT(*) AS Total FROM dbo.Requests GROUP BY Priority;

-- Aggregation 3 (optional): open requests per assigned user
SELECT AssignedToUserId, COUNT(*) AS OpenTotal
FROM dbo.Requests
WHERE Status <> 3
GROUP BY AssignedToUserId;
GO
