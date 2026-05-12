USE [NZBS]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferLne]    Script Date: 23-Oct-24 11:55:07 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[nzbDirectedTransferLne]') AND type in (N'U'))
DROP TABLE [dbo].[nzbDirectedTransferLne]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferLne]    Script Date: 23-Oct-24 11:55:07 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[nzbDirectedTransferLne](
	[TransOrderCode] [varchar](21) NULL,
	[LineItemSeq] [int] NULL,
	[ItemNumber] [varchar](31) NULL,
	[ItemDescription] [varchar](101) NULL,
	[QtyOrder] [int] NULL,
	[UnitOfMeasure] [varchar](9) NULL,
	[BaseUOFM] [varchar](9) NULL,
	[QTYBaseUOFM] [int] NULL,
	[Id] [varchar](50) NOT NULL
) ON [PRIMARY]
GO


