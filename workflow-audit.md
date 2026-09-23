# Umbilo Rentals — System-Wide Workflow Audit

Generated during a full sweep of the application, tracing actual user journeys
end-to-end rather than just checking individual pages in isolation. Split into
**bugs fixed during this sweep**, and **findings that need a decision from you**
(not fixed yet — need your input on intended behaviour, or represent bigger
scope than a quick fix).

---

## Fixed during this sweep

### 1. Payment verification was silently broken
`AdminController.VerifyPayment()` set `payment.Status = "Processed"` — but
*every other place* in the system (the dashboard's "rent paid" check, the
PayFast webhook, the admin payment totals) checks for `Status == "Paid"`.
A manually-verified payment would never register as paid anywhere else in
the app. **Fixed**: now sets `"Paid"`, consistent with everywhere else.

### 2. Manual payment verification was completely unreachable
Beyond the status bug above, there was **no button or link anywhere** to
actually trigger `VerifyPayment` — the action existed, but nothing in
`Admin/Payments.cshtml` called it. This means payments were entirely
dependent on PayFast's automatic webhook, with **no fallback** if it failed,
was delayed, or a tenant paid by another method (EFT, cash). The `Payment`
model already has `ProofOfPayment` and `VerifiedBy` fields, strongly
suggesting this manual-verification path was intended but never finished.
**Fixed**: added a "Verify Payment" button on pending payments in the admin
view.

### 3. Security feature notifications were inconsistent
When I built the Security role feature, `ResolveComplaint` notified the
tenant but `CheckInVisitor` and `ResolveLostFoundItem` didn't. **Fixed**: a
tenant is now notified when their scheduled visitor checks in, and when a
lost/found report they submitted gets resolved.

### 4. Room Details page — real bugs, not just clutter
- Two separate amenity lists ("Room Features" and "Amenities") repeated
  almost identical information
- The gallery's first image was missing the `~/Content/RoomImages/` path
  prefix every other image reference uses — likely rendering broken
- Room number/rent/status were repeated three times across the page
- The gallery's "Shared Bathroom"/"Shared Kitchen" images were generic
  stock-style photos shown identically on *every* room, not actual photos
  of that room

All consolidated into one clean structure — hero photo, header (number +
price + status shown once), description, one features list, reviews, and a
slim sticky sidebar for the actual call-to-action.

---

## Findings — need a decision, not fixed yet

### A. No email verification on registration
Anyone can register with any email address — there's no confirmation link
sent, no `EmailConfirmed` flag. For a platform that later collects payments
and ID documents, this is worth considering, though it's a genuinely bigger
feature (needs a verification token flow similar to password reset).

### B. Staff roles get zero proactive notifications of new work
Admin isn't notified when a new application comes in. Security isn't
notified when a new visitor is scheduled or a new complaint is filed.
Maintenance isn't notified of new requests (they see them on the kanban,
but only if they check it). The notification system only supports 1:1
notifications to a specific known user — there's no "notify all staff with
role X" mechanism. Right now, every staff role has to periodically check
their own dashboard rather than being alerted. Worth considering for
anything time-sensitive (a visitor arriving, a security issue).

### C. Hardcoded room features/amenities
Every room shows the exact same "Wi-Fi, Shared Bathroom, Parking..." list
regardless of what's actually true for that room, because the `Room` model
has no amenities data at all. I softened the language ("General building
features — confirm specifics before applying") rather than removing it
outright, since removing it entirely leaves the page thin. A proper fix
would mean a real per-room amenities system — new schema, an admin UI to
set them per room. Bigger scope, flagging rather than building blind.

### D. Rejected/Withdrawn applications don't prompt a next step
When an application is rejected or auto-withdrawn, the timeline shows the
status clearly, but there's no explicit "Browse other rooms" call-to-action
right there to guide the person forward. Small, but an easy win for keeping
people engaged instead of just showing them a dead end.

### E. RoleID 4 = Security — worth double-checking
I assigned RoleID 4 to the new Security role, following the existing
1=Applicant/2=Admin/3=Maintenance sequence. There's a `Role` lookup table
in your database (`RoleID`, `RoleName`) that I can't query directly from my
side — worth a quick check that RoleID 4 isn't already claimed by something
in that table from the original abandoned design, to avoid a silent
collision.

### F. No way to cancel/reschedule a visitor once scheduled
A tenant can schedule a visitor but there's currently no way to cancel a
visit if plans change — it just sits as "Expected" until either checked in
or the date passes. Small gap, easy to add if wanted.

---

## Recap: previously found and already fixed (for continuity)

These were caught in earlier sweeps this session, not part of today's pass,
listed here so this document is a single reference point for the project's
overall health:

- 10+ orphaned dead-code files (unreachable controllers/views with no
  backing routes) found and removed across two separate sweeps
- Plaintext password storage → now hashed (PBKDF2), with automatic silent
  migration for existing accounts on next login
- `.csproj` file-inclusion gaps — 25 real files (including 2 full
  controllers) were never registered in the classic MSBuild project file,
  meaning Legal pages and Reviews may have been silently 404ing
- Secrets moved out of `Web.config` into a gitignored external file after
  a real API key nearly got committed to a public repo
- Several broken action-name mismatches (`Profile` vs `MyProfile`, wrong
  redirect targets) causing dead links
- Maintenance kanban's "In Progress"/"Completed" columns were structurally
  guaranteed to always be empty due to a query bug
