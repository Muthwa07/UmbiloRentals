/* =========================================================
   UMBILO RENTALS - MIGRATION: Reviews + Roommate Compatibility
   =========================================================
   Run this against your BuildingManagementDB database
   (SSMS, or Visual Studio's SQL Server Object Explorer).

   Safe to run once. Re-running will error on the CREATE TABLE
   and duplicate ALTER COLUMN statements - that's expected,
   it means it already ran.
========================================================= */

-- ---------------------------------------------------------
-- 1. REVIEWS
--    One review per completed stay. Visible to other
--    applicants browsing a room.
-- ---------------------------------------------------------

CREATE TABLE Reviews (
    ReviewID     INT IDENTITY(1,1) PRIMARY KEY,
    RoomID       INT NOT NULL,
    UserID       INT NOT NULL,
    Rating       INT NOT NULL,
    Comment      NVARCHAR(1000) NULL,
    DatePosted   DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Reviews_Rooms FOREIGN KEY (RoomID)
        REFERENCES Rooms(RoomID),

    CONSTRAINT FK_Reviews_Users FOREIGN KEY (UserID)
        REFERENCES Users(UserID),

    CONSTRAINT CK_Reviews_Rating
        CHECK (Rating BETWEEN 1 AND 5)
);

-- One review per user per room (they can only review a room they actually stayed in, once)
ALTER TABLE Reviews
    ADD CONSTRAINT UQ_Reviews_User_Room UNIQUE (UserID, RoomID);


-- ---------------------------------------------------------
-- 2. ROOMMATE COMPATIBILITY PROFILE
--    Added to Users so it's collected once on the profile
--    page and reused anywhere compatibility is shown.
--    All nullable - existing users won't break, and the
--    profile form can prompt people to fill these in.
-- ---------------------------------------------------------

ALTER TABLE Users ADD SleepSchedule NVARCHAR(20) NULL;
    -- Expected values: 'Early Bird', 'Night Owl', 'Flexible'

ALTER TABLE Users ADD CleanlinessLevel NVARCHAR(20) NULL;
    -- Expected values: 'Very Tidy', 'Average', 'Relaxed'

ALTER TABLE Users ADD NoiseTolerance NVARCHAR(20) NULL;
    -- Expected values: 'Quiet', 'Moderate', 'Social'

ALTER TABLE Users ADD IsSmoker BIT NULL;

ALTER TABLE Users ADD StudyHabit NVARCHAR(20) NULL;
    -- Expected values: 'Studies at Home', 'Studies Elsewhere'

ALTER TABLE Users ADD GuestsPreference NVARCHAR(20) NULL;
    -- Expected values: 'Rarely', 'Sometimes', 'Often'


-- ---------------------------------------------------------
-- Done. Next step: in Visual Studio, right-click
-- Models/Model1.edmx -> "Update Model from Database" ->
-- Add tab -> tick Reviews -> Finish. Then open the Users
-- table under Refresh tab and refresh it to pick up the
-- new columns.
-- ---------------------------------------------------------
