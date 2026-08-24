# Session Logging Rules

Every session must be documented to maintain continuity and provide a clear history of what was accomplished throughout each day.

## Rules & Structure

1. **File Location**: All active session logs must reside in the `agent/sessions/` directory.
2. **File Naming**: Name active session files by date using the format `DD-MM-YYYY.md` (e.g., `26-05-2026.md`).
3. **Timeline Logs**:
   - Write chronological logs of actions taken throughout the day.
   - Prefix every log entry with the local time in 24-hour format: `[HH:mm:ss]`.
   - Each entry must list the files touched, specific logic changed, and verification status.
4. **Active Updates**: Update the active session log whenever a major step is completed (e.g., creating a new command, refactoring, fixing a bug, running tests). Do not wait until the very end of the day to write everything.
5. **No Implementation Secrets**: Keep the logs focused on *what* was done and *why*, linking to the files modified so they can be referenced easily.
6. **Weekly Archiving & Summaries (Sunday Rule)**:
   - At the start of a new week (every Sunday), wrap all daily session log files from the previous week (which runs Sunday to Saturday) into a dedicated archive subdirectory under `agent/sessions/`.
   - The archive subdirectory name must specify the start and end dates of that week in the format `DD-MM-YYYY_to_DD-MM-YYYY` (where the start date is Sunday and the end date is Saturday, e.g., `28-06-2026_to_04-07-2026`).
   - Create a weekly summary markdown file inside that archive directory named `weekly_summary.md` compiling and summarizing the week's accomplishments and roadmap progression.
   - Start logging new active sessions for the current week directly inside the `agent/sessions/` directory until the following Sunday.
