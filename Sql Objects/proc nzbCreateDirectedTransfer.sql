USE [NZBS]
GO

/****** Object:  StoredProcedure [dbo].[nzbCreateDirectedTransfer]    Script Date: 23-Oct-24 11:57:06 AM ******/
DROP PROCEDURE [dbo].[nzbCreateDirectedTransfer]
GO

/****** Object:  StoredProcedure [dbo].[nzbCreateDirectedTransfer]    Script Date: 23-Oct-24 11:57:06 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO


CREATE proc [dbo].[nzbCreateDirectedTransfer]  @id varchar(50) as

set nocount on

--declare @id varchar(50)='00f9cf7d-1409-4c7b-9f01-7819da1e1fad'

declare @tranCode varchar(20), @lineCount int

BEGIN TRANSACTION;

BEGIN TRY
    set @tranCode=(select
	concat(
		rtrim(ObjectID), 
		format(PropertyValue+1, 'D8'))
	from SY90000 where PropertyName='PanaDirectedTransfer')

	update SY90000 set PropertyValue=PropertyValue+1 where PropertyName='PanaDirectedTransfer'
	update nzbDirectedTransferHdr set TransOrderCode=@tranCode where Id=@id
	update nzbDirectedTransferLne set TransOrderCode=@tranCode where Id=@id
	update nzbDirectedTransferEmailLne set TransOrderCode=@tranCode where Id=@id

	print @tranCode
	set @lineCount=(select count(*) from nzbDirectedTransferLne where TransOrderCode=@tranCode and QtyOrder !=0)
	
	if @lineCount>0 begin
		--create panatracker header
		insert into PanatrackerGP7_DirectedTransOrder
		select @tranCode,PickSiteID,null,POUSiteID,0,DocDate,null,null,DueDate,null,null,null,null,null,null,0,null,Id,UserID,null,null
		from nzbDirectedTransferHdr
		where TransOrderCode=@tranCode

		--create panatracker detail
		insert into PanatrackerGP7_DirectedTransOrderUnit
		select @tranCode,LineItemSeq,ItemNumber,ItemDescription,QtyOrder,0,0,UnitOfMeasure,null,QTYBaseUOFM,0,null,null
		from nzbDirectedTransferLne
		where TransOrderCode=@tranCode and QtyOrder !=0
	end

END TRY
BEGIN CATCH
    SELECT 
        ERROR_NUMBER() AS ErrorNumber
        ,ERROR_SEVERITY() AS ErrorSeverity
        ,ERROR_STATE() AS ErrorState
        ,ERROR_PROCEDURE() AS ErrorProcedure
        ,ERROR_LINE() AS ErrorLine
        ,ERROR_MESSAGE() AS ErrorMessage;

    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
END CATCH;

IF @@TRANCOUNT > 0
    COMMIT TRANSACTION;
GO


