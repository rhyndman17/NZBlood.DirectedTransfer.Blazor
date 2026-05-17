USE [NZBS]
GO
/****** Object:  StoredProcedure [dbo].[nzbCalculateDirectedTransferWI]    Script Date: 17/05/2026 9:30:34 pm ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER proc [dbo].[nzbCalculateDirectedTransferWI] @pickFromSiteId varchar(20),@pouSiteId varchar(15) as

with resourcePlanning as (
	select q.ITEMNMBR,i.ITEMDESC,q.LOCNCODE 'POUSiteID',PLANNERID,QTYONHND,ATYALLOC,ORDRPNTQTY,ORDRUPTOLVL,u.BASEUOFM,i.UOMSCHDL,ud.QTYBSUOM,isnull(rtrim(ito.[UOM Description]),'') UOFMLONGDESC,
			case when d.DirectedTransferPriority is null or d.DirectedTransferPriority=11 then 0 else cast(d.DirectedTransferPriority as int) end as 'DirectedTransferPriority',
			case when d.DirectedTransferPriority is null or d.DirectedTransferPriority=11 then 2147483647 else cast(d.DirectedTransferPriority as int) end as 'DirectedTransferPrioritySort'
		from IV00101 i
		join nzbDirectedTransferItems d on d.ITEMNMBR=i.ITEMNMBR
		join IV00102 q on q.ITEMNMBR=i.ITEMNMBR and q.LOCNCODE=d.LOCNCODE
		join IV40201 u on u.UOMSCHDL=i.UOMSCHDL
		join IV40202 ud on u.UOMSCHDL=ud.UOMSCHDL and u.BASEUOFM=ud.UOFM
		left join nzbItemOptions ito on i.ITEMNMBR=ito.[Item Number]
		where q.LOCNCODE=@pouSiteId
		and d.DirectedTransferItem=1 ),
pickFromSiteQty as (
		select ITEMNMBR,LOCNCODE, isnull(QTYONHND-ATYALLOC,0) 'QtyAvailable' from IV00102 where LOCNCODE=@pickFromSiteId and RCRDTYPE=2),
panaPickingPICKQty as (
		select d.ItemCode,isnull(sum(d.QtyOrder*isnull(d.QtyBaseUnitOfMeasure,1)),0) PanaPickingPICKQty,h.SourceLocCode
		from 
		PanatrackerGP7_DirectedTransOrder h
		join PanatrackerGP7_DirectedTransOrderUnit d on h.TransOrderCode=d.TransOrderCode
		where h.OrderStatus != 4
		group by d.ItemCode,h.SourceLocCode),
panaPickingPOUQty as (
		select d.ItemCode,isnull(sum(d.QtyOrder*isnull(d.QtyBaseUnitOfMeasure,1)),0) PanaPickingPOUQty,h.DestinationLocCode
		from 
		PanatrackerGP7_DirectedTransOrder h
		join PanatrackerGP7_DirectedTransOrderUnit d on h.TransOrderCode=d.TransOrderCode
		where h.OrderStatus !=4
		group by d.ItemCode,h.DestinationLocCode)

--select * from panaPickingQty
--select * from pickFromSiteQty

select r.ITEMNMBR,r.ITEMDESC,r.POUSiteID,isnull(p.QtyAvailable,0)-isnull(pickt.PanaPickingPICKQty,0) 'QtyAvailable',@pickFromSiteId 'PickFromSite',
			r.BASEUOFM,isnull(pickt.PanaPickingPICKQty,0) PanaPickingPICKQty,isnull(pout.PanaPickingPOUQty,0) PanaPickingPOUQty, r.ORDRUPTOLVL,r.UOMSCHDL,r.QTYBSUOM,r.UOFMLONGDESC,
			r.DirectedTransferPriority
from resourcePlanning r
left join pickFromSiteQty p on r.ITEMNMBR=p.ITEMNMBR
left join panaPickingPICKQty pickt on r.ITEMNMBR=pickt.ItemCode and pickt.SourceLocCode=@pickFromSiteId
left join panaPickingPOUQty pout on r.ITEMNMBR=pout.ItemCode and pout.DestinationLocCode=@pouSiteId
order by r.DirectedTransferPrioritySort,r.ITEMNMBR
