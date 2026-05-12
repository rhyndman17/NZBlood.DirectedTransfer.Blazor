USE [NZBS]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferHdr]    Script Date: 12/05/2026 2:07:56 pm ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[nzbDirectedTransferHdr]') AND type in (N'U'))
DROP TABLE [dbo].[nzbDirectedTransferHdr]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferHdr]    Script Date: 12/05/2026 2:07:56 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[nzbDirectedTransferHdr](
	[TransOrderCode] [varchar](21) NULL,
	[PickSiteID] [varchar](11) NULL,
	[POUSiteID] [varchar](11) NULL,
	[DocDate] [datetime] NULL,
	[DueDate] [datetime] NULL,
	[UserID] [varchar](100) NULL,
	[PrintOnly] [int] NULL,
	[Id] [varchar](50) NULL,
	[Reference] [varchar](200) NULL
) ON [PRIMARY]
GO


