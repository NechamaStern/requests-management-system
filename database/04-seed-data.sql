USE [RequestsManagement];
GO
SET NOCOUNT ON;

-- Users: idempotent by UserName
MERGE dbo.Users AS t
USING (VALUES
    (N'dana.cohen',   N'Dana Cohen',   N'dana.cohen@example.com'),
    (N'yossi.levi',   N'Yossi Levi',   N'yossi.levi@example.com'),
    (N'noa.mizrahi',  N'Noa Mizrahi',  N'noa.mizrahi@example.com'),
    (N'amir.peretz',  N'Amir Peretz',  N'amir.peretz@example.com'),
    (N'tal.shapira',  N'Tal Shapira',  N'tal.shapira@example.com'),
    (N'ron.katz',     N'Ron Katz',     N'ron.katz@example.com'),
    (N'maya.friedman',N'Maya Friedman',N'maya.friedman@example.com')
) AS s (UserName, DisplayName, Email)
ON t.UserName = s.UserName
WHEN NOT MATCHED THEN
    INSERT (UserName, DisplayName, Email) VALUES (s.UserName, s.DisplayName, s.Email);
GO

-- Requests: tops up to @Target rows, so re-running never exceeds the target
DECLARE @Target INT = 100000;
DECLARE @Missing INT = @Target - (SELECT COUNT(*) FROM dbo.Requests);

IF @Missing > 0
BEGIN
    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @UserCount INT = (SELECT COUNT(*) FROM dbo.Users);

    SELECT TOP (@Missing) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n,
           ABS(CHECKSUM(NEWID())) AS r1, ABS(CHECKSUM(NEWID())) AS r2,
           ABS(CHECKSUM(NEWID())) AS r3, ABS(CHECKSUM(NEWID())) AS r4,
           ABS(CHECKSUM(NEWID())) AS r5, ABS(CHECKSUM(NEWID())) AS r6
    INTO #Rnd
    FROM sys.all_objects a CROSS JOIN sys.all_objects b;

    WITH Rnd AS (
        SELECT * FROM #Rnd
    ),
    Topics (Id, Topic) AS (
        SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, v FROM (VALUES
            (N'Payment issue'),(N'Login problem'),(N'Contract renewal'),(N'Invoice correction'),
            (N'Access request'),(N'Data export'),(N'Service outage'),(N'Account update'),
            (N'Feature request'),(N'Complaint')) x(v)
    ),
    Orgs (Id, Org) AS (
        SELECT ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, v FROM (VALUES
            (N'Acme'),(N'Globex'),(N'Initech'),(N'Umbrella'),(N'Hooli'),(N'Stark Industries'),
            (N'Wayne Enterprises'),(N'Wonka'),(N'Cyberdyne'),(N'Soylent')) x(v)
    ),
    Base AS (
        SELECT r.n,
               tp.Topic, og.Org, r.r3 % 20 AS OrgBranch,
               -- 40% New, 25% InProgress, 15% Waiting, 20% Completed
               CASE WHEN r.r2 % 100 < 40 THEN 0 WHEN r.r2 % 100 < 65 THEN 1
                    WHEN r.r2 % 100 < 80 THEN 2 ELSE 3 END AS Status,
               -- 20% High, 50% Medium, 30% Low
               CASE WHEN r.r4 % 100 < 20 THEN 2 WHEN r.r4 % 100 < 70 THEN 1 ELSE 0 END AS Priority,
               -- ~10% unassigned
               CASE WHEN r.r5 % 10 = 0 THEN NULL ELSE r.r5 % @UserCount END AS UserIdx,
               DATEADD(MINUTE, -(r.r6 % 525600), @Now) AS CreatedAt,   -- last 365 days
               r.r1
        FROM Rnd r
        JOIN Topics tp ON tp.Id = r.r1 % 10
        JOIN Orgs   og ON og.Id = (r.r3 / 7) % 10
    )
    INSERT INTO dbo.Requests (Title, OrganizationName, Status, Priority, AssignedToUserId, CreatedAt, UpdatedAt)
    SELECT b.Topic + N' #' + CAST(b.n AS NVARCHAR(10)),
           b.Org + N' Branch ' + CAST(b.OrgBranch AS NVARCHAR(10)),
           b.Status, b.Priority, u.UserId, b.CreatedAt,
           CASE WHEN b.Status = 0 THEN b.CreatedAt
                ELSE LEAST(@Now, DATEADD(MINUTE, b.r1 % 10080, b.CreatedAt)) END
    FROM Base b
    LEFT JOIN (SELECT Id AS UserId, ROW_NUMBER() OVER (ORDER BY Id) - 1 AS Idx FROM dbo.Users) u
           ON u.Idx = b.UserIdx;

    DROP TABLE #Rnd;
END;
GO

SELECT (SELECT COUNT(*) FROM dbo.Users) AS Users, (SELECT COUNT(*) FROM dbo.Requests) AS Requests;
GO
