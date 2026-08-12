# VS-31 — Native Navigation Alignment Design

## Status

Approved under the Product Owner's standing authorization on 2026-08-12. This design resolves the navigation debt explicitly deferred by DEC-02, DEC-03 and DEC-09.

## Goal

Align Atlas's persistent mobile navigation with `ATLAS-DESIGN-001` v1.2 without removing any existing Business, Context or Settings capability and without changing domain, API, persistence, authentication or recommendation behavior.

## Design authority

`ATLAS-DESIGN-001` defines four persistent destinations:

1. Today
2. History
3. Goals
4. Profile

VS-28 intentionally preserved the inherited five-tab shell only for that native-polish slice. VS-31 is the dedicated governed alignment slice promised by DEC-09.

## Chosen information architecture

### Persistent native tabs

Atlas will expose exactly four `NativeTabs` triggers:

- `index` → **Today**
- `history` → **History**
- `goals` → **Goals**
- `profile` → **Profile**

The underlying Today route remains `/`. Goals remains unchanged. The Profile tab continues to render the existing Business Hub rather than introducing a second profile architecture.

### Profile as the owner-management hub

The existing Business Hub remains the Profile root because it already presents the Business Atlas understands. It gains only the minimum navigation affordances needed to reach:

- Business Context → `/context`
- Settings / preferences → `/settings`
- existing Business details editor → `/edit-business`

Business intelligence, media/menu summaries and edit behavior remain unchanged.

### Context and Settings

Context and Settings remain fully available but stop competing for permanent tab-bar space. They become pushed Profile-detail routes:

- `apps/mobile/app/context.tsx`
- `apps/mobile/app/settings.tsx`

Both screens provide a visible Back-to-Profile affordance and use the root Stack's native push/pop behavior. Their APIs, data models, reset behavior, notification link and Business Memory behavior remain unchanged.

### History

History becomes a true tab root at `apps/mobile/app/(tabs)/history.tsx`.

Expo Router route groups are URL-transparent, so the public/deep-link path remains `/history` while the file becomes part of the tab group.

Because History is now a primary tab root:

- it renders with tab-bar-aware safe spacing;
- the old generic `Back` action is removed;
- Weekly Review remains available as its secondary action;
- history data, filters, Opportunity routes and empty/error states remain unchanged.

## Native tab presentation

Continue using the VS-28 Expo Router `NativeTabs` implementation.

### iOS symbols

- Today: `house` / `house.fill`
- History: `clock` / `clock.fill`
- Goals: `flag` / `flag.fill`
- Profile: `person.crop.circle` / `person.crop.circle.fill`

### Android fallback

Continue using `AtlasIcon`. Add one bounded `history` icon fallback. Existing Home/Goals/Business icon code is otherwise retained. The Profile tab may reuse the existing Atlas Business icon on Android because the Profile root is the Business Hub; no new brand system is required.

## Route migration

Move only route entry files; do not duplicate screens:

- `apps/mobile/app/history.tsx` → `apps/mobile/app/(tabs)/history.tsx`
- `apps/mobile/app/(tabs)/context.tsx` → `apps/mobile/app/context.tsx`
- `apps/mobile/app/(tabs)/settings.tsx` → `apps/mobile/app/settings.tsx`

Delete the superseded entry files after their replacements exist.

## Accessibility and native behavior

- Four tab labels must remain visible and unambiguous.
- Context/Settings detail screens need a minimum 44-point accessible Back control.
- Dynamic Type, safe-area, Reduce Motion and VS-28 native behavior remain inherited from the existing Atlas shell.
- No content may become unreachable by removing the old tab triggers.
- Root-level deep links `/history`, `/context` and `/settings` remain valid.
- Tab roots must not show a misleading Back action.

## Non-goals

- no API/backend/database changes;
- no migration;
- no authentication/authorization changes;
- no Today’s Focus or recommendation logic changes;
- no Business Hub redesign;
- no Context workflow redesign;
- no Settings/reset redesign;
- no new tab framework or custom tab-bar implementation;
- no release/deployment/EAS/OTA/production enablement.

## Acceptance

1. Exactly four `NativeTabs.Trigger` destinations exist: `index`, `history`, `goals`, `profile`.
2. Labels are exactly Today, History, Goals, Profile.
3. Context and Settings are not tab triggers and remain reachable from Profile.
4. `/history`, `/context`, `/settings` remain routable.
5. History is tab-bar-aware and no longer renders a Back action.
6. Context and Settings render as pushed details with an accessible Back-to-Profile fallback.
7. Existing Settings notifications, Business Memory and Development-only Expo reset remain intact.
8. Existing Context save/load/confirmation behavior remains intact.
9. Existing Business Hub/media/menu/edit behavior remains intact.
10. Existing native tabs, safe-area, accessibility and motion regressions remain green.
11. No API/database/migration/deployment files change.
12. CI, Security baseline and Product Intake pass on the exact certified implementation SHA.
