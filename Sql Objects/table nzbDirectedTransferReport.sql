USE [NZBS]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferReport]    Script Date: 12/05/2026 2:07:08 pm ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[nzbDirectedTransferReport]') AND type in (N'U'))
DROP TABLE [dbo].[nzbDirectedTransferReport]
GO

/****** Object:  Table [dbo].[nzbDirectedTransferReport]    Script Date: 12/05/2026 2:07:08 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[nzbDirectedTransferReport](
	[UserID] [varchar](100) NULL,
	[PickSiteID] [varchar](11) NULL,
	[POUSiteID] [varchar](11) NULL,
	[ItemNumber] [varchar](31) NULL,
	[ItemDescription] [varchar](101) NULL,
	[UnitOfMeasure] [varchar](9) NULL,
	[POUSiteOrderUpToLevel] [int] NULL,
	[QtyPending] [int] NULL,
	[QtyAvailableAtPickLocation] [int] NULL,
	[QtyOrder] [int] NULL,
	[OrderReference] [varchar](200) NULL,
	[VendorItemNumber] [varchar](30) NULL
) ON [PRIMARY]
GO
