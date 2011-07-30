/****** Object:  Table [dbo].[SignalRSignals]    Script Date: 05/11/2011 18:18:37 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[SignalRSignals](
	[SignalID] [int] IDENTITY(1,1) NOT NULL,
	[EventKey] [nvarchar](4000) NOT NULL,
	[LastSignaledAt] [datetime] NOT NULL,
 CONSTRAINT [PK_SignalRSignals] PRIMARY KEY CLUSTERED 
(
	[SignalID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]

GO

/****** Object:  Index [IX_SignalRSignals_EventKey]    Script Date: 05/11/2011 18:18:53 ******/
CREATE UNIQUE NONCLUSTERED INDEX [IX_SignalRSignals_EventKey] ON [dbo].[SignalRSignals] 
(
	[EventKey] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO

/****** Object:  Index [IX_SignalRSignals_LastSignaledAt]    Script Date: 05/11/2011 18:19:08 ******/
CREATE NONCLUSTERED INDEX [IX_SignalRSignals_LastSignaledAt] ON [dbo].[SignalRSignals] 
(
	[LastSignaledAt] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
GO