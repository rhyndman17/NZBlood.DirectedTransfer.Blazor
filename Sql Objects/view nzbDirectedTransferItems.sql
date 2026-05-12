USE [NZBS]
GO

/****** Object:  View [dbo].[nzbDirectedTransferItems]    Script Date: 23/10/2024 2:03:51 pm ******/
DROP VIEW [dbo].[nzbDirectedTransferItems]
GO

/****** Object:  View [dbo].[nzbDirectedTransferItems]    Script Date: 23/10/2024 2:03:52 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE view [dbo].[nzbDirectedTransferItems] as
select Extender_Key_Values_1 'ITEMNMBR',Extender_Key_Values_2 'LOCNCODE',cast(TOTAL as int) 'DirectedTransferItem'
from EXT01100 h 
join EXT01103 d on h.Extender_Record_ID=d.Extender_Record_ID
where Extender_Window_ID='ITEM_QTY_OPTION' and Field_ID=150
GO


