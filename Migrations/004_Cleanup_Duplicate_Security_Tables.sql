/* =========================================================
   UMBILO RENTALS - CLEANUP: Remove duplicate Security tables
   =========================================================
   Sorry about this one - turns out your database already had
   VisitorRequest, Complaint, and LostFound tables from an
   earlier abandoned feature attempt (same pattern as the
   Dashboard/Tenant controllers found earlier). They're
   actually better structured than what I just had you
   create. This drops the duplicates so there's only one
   set of tables for this feature, not two.

   Run this against your BuildingManagementDB database.
========================================================= */

DROP TABLE Visitors;
DROP TABLE SecurityIssues;
DROP TABLE LostFoundItems;

-- ---------------------------------------------------------
-- No EDMX update needed for this one - VisitorRequest,
-- Complaint and LostFound were already part of your model
-- from the start (visible in Model1.Context.cs already).
-- After running this, right-click Models/Model1.edmx ->
-- "Update Model from Database" -> Refresh tab, just to
-- clear out the three dropped tables from the designer.
-- ---------------------------------------------------------
