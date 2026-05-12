USE [NZBS]
GO

/****** Object:  View [dbo].[nzbSiteOptions]    Script Date: 23/10/2024 2:04:06 pm ******/
DROP VIEW [dbo].[nzbSiteOptions]
GO

/****** Object:  View [dbo].[nzbSiteOptions]    Script Date: 23/10/2024 2:04:06 pm ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER OFF
GO

create view [dbo].[nzbSiteOptions]
 as select 
A1.SITE_OPTIONS_Key1 AS 'LocationCode',
A1.SITE_OPTIONS_142_eKANBANPickfromSite AS 'eKanBanPickFromSite',
A1.SITE_OPTIONS_149_SiteTransferEmailAddress AS 'SiteTransferEmailAddress' from 
(select 
EXT01100.Extender_Key_Values_1 as SITE_OPTIONS_Key1,
SITE_OPTIONS_142_eKANBANPickfromSite,
SITE_OPTIONS_149_SiteTransferEmailAddress
 from 
EXT01100 
 left join 
(select Extender_Record_ID, STRGA255 as SITE_OPTIONS_142_eKANBANPickfromSite
 from EXT01101 where Field_ID = 142) B142
 on EXT01100.Extender_Record_ID = B142.Extender_Record_ID 
 left join 
(select Extender_Record_ID, STRGA255 as SITE_OPTIONS_149_SiteTransferEmailAddress
 from EXT01101 where Field_ID = 149) B149
 on EXT01100.Extender_Record_ID = B149.Extender_Record_ID  where EXT01100.Extender_Window_ID = 'SITE_OPTIONS') A1
GO


