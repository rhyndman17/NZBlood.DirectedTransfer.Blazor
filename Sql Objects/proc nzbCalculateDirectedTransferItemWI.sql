USE [NZBS]
GO
/****** Object:  StoredProcedure [dbo].[nzbCalculateDirectedTransferItemWI]    Script Date: 12/05/2026 2:05:20 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE OR ALTER proc [dbo].[nzbCalculateDirectedTransferItemWI] @orderFormId varchar(50),@pickFromSiteId varchar(20),@pouSiteId varchar(15), @itemNumber varchar(30) as

with resourcePlanning as (
	select q.ITEMNMBR,i.ITEMDESC,q.LOCNCODE 'POUSiteID',rtrim(isnull(d.ZONE,'')) ZONE,PLANNERID,QTYONHND,ATYALLOC,ORDRPNTQTY,ORDRUPTOLVL,u.BASEUOFM,i.UOMSCHDL,ud.QTYBSUOM,rtrim(ud.UOFMLONGDESC) UOFMLONGDESC
		from IV00101 i
		join nzbDirectedTransferOrderFormItems d on rtrim(d.ITEMNMBR)=rtrim(i.ITEMNMBR) and rtrim(d.ORDER_ID)=rtrim(@orderFormId)
		join IV00102 q on q.ITEMNMBR=i.ITEMNMBR and rtrim(q.LOCNCODE)=rtrim(@pouSiteId)
		join IV40201 u on u.UOMSCHDL=i.UOMSCHDL
		join IV40202 ud on u.UOMSCHDL=ud.UOMSCHDL and u.BASEUOFM=ud.UOFM
		where rtrim(q.LOCNCODE)=rtrim(@pouSiteId) and q.ITEMNMBR=@itemNumber ),
pickFromSiteQty as (
		select ITEMNMBR,LOCNCODE, isnull(QTYONHND-ATYALLOC,0) 'QtyAvailable' from IV00102 where LOCNCODE=@pickFromSiteId and RCRDTYPE=2 and ITEMNMBR=@itemNumber),
panaPickingPICKQty as (
		select d.ItemCode,isnull(sum(d.QtyOrder*d.QtyBaseUnitOfMeasure),0) PanaPickingPICKQty,h.SourceLocCode
		from 
		PanatrackerGP7_DirectedTransOrder h
		join PanatrackerGP7_DirectedTransOrderUnit d on h.TransOrderCode=d.TransOrderCode
		where h.OrderStatus != 4 and d.ItemCode=@itemNumber 
		group by d.ItemCode,h.SourceLocCode),
panaPickingPOUQty as (
		select d.ItemCode,isnull(sum(d.QtyOrder*d.QtyBaseUnitOfMeasure),0) PanaPickingPOUQty,h.DestinationLocCode
		from 
		PanatrackerGP7_DirectedTransOrder h
		join PanatrackerGP7_DirectedTransOrderUnit d on h.TransOrderCode=d.TransOrderCode
		where h.OrderStatus !=4 and d.ItemCode=@itemNumber 
		group by d.ItemCode,h.DestinationLocCode)

--select * from panaPickingQty
--select * from pickFromSiteQty

select r.ITEMNMBR,r.ITEMDESC,r.POUSiteID,isnull(p.QtyAvailable,0)-isnull(pickt.PanaPickingPICKQty,0) 'QtyAvailable',@pickFromSiteId 'PickFromSite',
			r.BASEUOFM,isnull(pickt.PanaPickingPICKQty,0) PanaPickingPICKQty,isnull(pout.PanaPickingPOUQty,0) PanaPickingPOUQty, r.ORDRUPTOLVL,r.UOMSCHDL,r.QTYBSUOM,r.UOFMLONGDESC,r.ZONE
from resourcePlanning r
left join pickFromSiteQty p on r.ITEMNMBR=p.ITEMNMBR
left join panaPickingPICKQty pickt on r.ITEMNMBR=pickt.ItemCode and pickt.SourceLocCode=@pickFromSiteId
left join panaPickingPOUQty pout on r.ITEMNMBR=pout.ItemCode and pout.DestinationLocCode=@pouSiteId
order by r.ZONE,r.ITEMNMBR
