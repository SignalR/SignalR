SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

/****** Object:  Table [dbo].[SignalRMessages] ******/
CREATE TABLE [dbo].[SignalRMessages](
	[MessageID] [bigint] IDENTITY(1,1) NOT NULL,
	[EventKey] [nvarchar](400) NOT NULL,
	[SmallValue] [nvarchar](4000) NULL,
	[BigValue] [nvarchar](max) NULL,
	[Created] [datetime] NOT NULL,
 CONSTRAINT [PK_SignalRMessages] PRIMARY KEY CLUSTERED 
(
	[MessageID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Index [IX_SignalRMessages_EventKey] ******/
CREATE NONCLUSTERED INDEX [IX_SignalRMessages_EventKey] ON [dbo].[SignalRMessages] 
(
	[EventKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

/****** Object:  Trigger [SignalR_DeleteExpiredMessages] ******/
CREATE TRIGGER [dbo].[SignalR_DeleteExpiredMessages] 
   ON [dbo].[SignalRMessages]
   AFTER INSERT
AS 
IF (SELECT MessageID FROM Inserted) % 5000 = 0 -- Trigger every 5000th insert
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Delete messages older than 60 seconds
	DELETE FROM [dbo].[SignalRMessages]
	WHERE DATEDIFF(second, [Created], GETDATE()) > 60
END
GO