USE [NZBS]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER VIEW [dbo].[nzbDirectedTransferOrderForms] AS
SELECT A1.DT_ORDER_FORMSID_OrderFormID AS OrderFormID,
       A1.DT_ORDER_FORMSDesc_Description AS Description,
       A1.DT_ORDER_FORMS_188_PointofUseSite AS PointOfUseSite,
       A1.DT_ORDER_FORMS_189_PickfromSite AS PickFromSite,
       ISNULL(A1.DT_ORDER_FORMS_193_Inactive, 0) AS Inactive
FROM (
    SELECT f.UD_Form_Field_ID AS DT_ORDER_FORMSID_OrderFormID,
           f.UD_Form_Field_Desc AS DT_ORDER_FORMSDesc_Description,
           B188.DT_ORDER_FORMS_188_PointofUseSite,
           B189.DT_ORDER_FORMS_189_PickfromSite,
           B193.DT_ORDER_FORMS_193_Inactive
    FROM EXT01200 f
    LEFT JOIN (SELECT Extender_Record_ID, STRGA255 AS DT_ORDER_FORMS_188_PointofUseSite FROM EXT01201 WHERE Field_ID=188) B188 ON f.Extender_Record_ID=B188.Extender_Record_ID
    LEFT JOIN (SELECT Extender_Record_ID, STRGA255 AS DT_ORDER_FORMS_189_PickfromSite FROM EXT01201 WHERE Field_ID=189) B189 ON f.Extender_Record_ID=B189.Extender_Record_ID
    LEFT JOIN (SELECT Extender_Record_ID, TOTAL AS DT_ORDER_FORMS_193_Inactive FROM EXT01203 WHERE Field_ID=193) B193 ON f.Extender_Record_ID=B193.Extender_Record_ID
    WHERE f.Extender_Form_ID='DT_ORDER_FORMS'
) A1
GO
