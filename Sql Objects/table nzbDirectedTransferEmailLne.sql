USE [NZBS]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferEmailLne]    Script Date: 12/05/2026 2:08:27 pm ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[nzbDirectedTransferEmailLne]') AND type in (N'U'))
DROP TABLE [dbo].[nzbDirectedTransferEmailLne]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferEmailLne]    Script Date: 12/05/2026 2:08:27 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[nzbDirectedTransferEmailLne](
	[TransOrderCode] [varchar](21) NULL,
	[LineStatus] [int] NULL,
	[ItemNumber] [varchar](31) NULL,
	[ItemDescription] [varchar](101) NULL,
	[QtyOrder] [int] NULL,
	[UnitOfMeasure] [varchar](9) NULL,
	[Id] [varchar](50) NOT NULL
) ON [PRIMARY]
GO


