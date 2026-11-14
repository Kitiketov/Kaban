# Schema: backend_project
Database: pgsql
Tables: 12
Columns are NOT NULL unless marked nullable.

## users
id: serial PK
name: varchar(64) -- size at random
email: varchar(320) unique
hash_password: varchar(255) -- maybe sha256

## teams
id: serial PK
team_name: varchar(64) -- size at random

## user_teams
user_id: int FK indexed
team_id: int FK indexed
role_id: int FK indexed

## roles
id: serial PK
team_id: int FK indexed
role_name: varchar(64) -- size at random

## projects
id: serial PK
project_name: varchar(64) -- size at random

## team_projects
project_id: int FK indexed
team_id: int FK indexed

## boards
id: serial PK
project_id: int FK indexed
name: varchar(64) -- size at random

## board_columns
id: serial PK
column_name: varchar(64) -- size at random
board_id: int FK indexed
board_order: int

## tasks
id: serial PK
board_column_id: int FK indexed
name: varchar(64) -- size at random
description: text nullable
priority: enum default:NoPriority
task_created: timestamptz
deadline: timestamptz nullable
task_order: int

## task_tags
id: serial PK
tag: varchar(32) -- size at random
task_id: int FK indexed

## user_tasks
user_id: int FK indexed
task_id: int FK indexed

## board_tags
tag_id: int FK indexed
board_id: int FK indexed

## Relationships
users.id → user_teams.user_id (one-to-many)
teams.id → user_teams.team_id (one-to-many)
teams.id → roles.team_id (one-to-many)
roles.id → user_teams.role_id (one-to-many)
teams.id → team_projects.team_id (one-to-many)
projects.id → team_projects.project_id (one-to-many)
boards.id → board_columns.board_id (one-to-many)
projects.id → boards.project_id (one-to-many)
board_columns.id → tasks.board_column_id (one-to-many)
tasks.id → task_tags.task_id (one-to-many)
user_teams.user_id → user_tasks.user_id (one-to-many)
tasks.id → user_tasks.task_id (one-to-many)
task_tags.id → board_tags.tag_id (one-to-many)
boards.id → board_tags.board_id (one-to-many)

## Groups
Boards Group: boards, board_columns, board_tags
Users Group: users, user_teams
Tasks Group: tasks, task_tags, user_tasks
Team-Project group: teams, roles, projects, team_projects

## Enums
tasks.priority: NoPriority, Low, Medium, High, VeryHigh

## Indexes
user_teams.4fe69fb9-9f77-4203-bec4-31225e16e83c (primary): user_id, team_id
roles.576ad0bf-0e01-4663-9f05-b3d46507ff34 (unique): team_id, role_name
team_projects.7ffe35a0-e461-4abe-9adf-52e3575a2b5e (primary): project_id, team_id
board_columns.51c30b0d-3c72-468c-ac62-c04228294547 (unique): board_id, board_order
tasks.583491e3-852c-43b8-9d22-7b09c71142b5 (unique): board_column_id, task_order
task_tags.37468d10-39a9-474b-b53f-e00bbbe20dcf (unique): task_id, tag
user_tasks.89f52047-56c1-40a4-9b23-eb0c3863317b (primary): user_id, task_id
board_tags.d0cfdc54-f968-4f80-98b1-815067a19e44 (primary): tag_id, board_id

## Notes
"Создать пользователя: ICreateUser" (near: users)
"Создать команду: ICreateTeam" (near: roles, teams)
"Добавить пользователя в команду, если он только создал команду, то добавление сразу происходит: IAddToTeam" (near: user_teams, users)
"Аналогично с пользователями: ICreateProject и IAddToProject" (near: user_teams, team_projects)

---
If you suggest changes to this schema, ALSO return them as a DrawSQL patch: one fenced ```json code block, so I can apply them to my diagram. If you are only answering a question, skip the patch.

Patch rules:
- One JSON object with "strategy": "merge"; combine every change into that single patch.
- Reference tables and columns by name. Only include what changes — omit unchanged columns and unused optional fields.
- Column: {"name","type","length","is_primary_key","is_auto_increment","is_nullable","is_unique_key","is_index","is_unsigned","default","enum_values"} — use exact type names for pgsql as shown in the schema above.
- Relationship: {"type":"one-to-many","source_table":"users","source_column":"id","target_table":"posts","target_column":"user_id"} — for "one-to-many" the source is the "one" side; for "many-to-one" the source is the "many" (foreign-key) side. Prefer "one-to-many".
- Add "default_type" ("string" | "function" | "number" | "boolean") when a default could be misread — a literal string "CURRENT_TIMESTAMP" or "0" needs "default_type":"string".
- SET columns (MySQL) list their members in "set_values", not "enum_values".
- Index: {"indexes":[{"name":"idx_posts_user_created","type":"index","columns":["user_id","created_at"]}]} on its table — "type" is "index" or "unique"; delete via "deletions".
- Group: {"name":"Billing"}. A group never lists its members — put "parent_name":"Billing" on each table that belongs to it.
- Sticky note: {"sticky_notes":[{"content":"..."}]}.
- Delete via "deletions"; rename via "old_name". Do not include left/top coordinates.

Examples:
Add table: `{"strategy":"merge","tables":[{"name":"posts","comment":"Blog posts","columns":[{"name":"id","type":"bigint","is_primary_key":true,"is_auto_increment":true},{"name":"user_id","type":"bigint","is_index":true},{"name":"title","type":"varchar","length":255},{"name":"body","type":"text","is_nullable":true},{"name":"created_at","type":"timestamp","is_nullable":true},{"name":"updated_at","type":"timestamp","is_nullable":true}]}]}`
Rename table and column (use old_name only when renaming): `{"strategy":"merge","tables":[{"name":"articles","old_name":"posts","columns":[{"name":"content","old_name":"body"}]}]}`
Delete: `{"strategy":"merge","deletions":{"tables":["tmp"],"columns":[{"table":"users","columns":["legacy"]}],"relationships":[{"source_table":"users","source_column":"id","target_table":"posts","target_column":"user_id"}]}}`
Multiple groups with nested tables (every table sets parent_name): `{"strategy":"merge","groups":[{"name":"Auth"},{"name":"Content"}],"tables":[{"name":"users","parent_name":"Auth","columns":[{"name":"id","type":"bigint","is_primary_key":true,"is_auto_increment":true}]},{"name":"roles","parent_name":"Auth","columns":[{"name":"id","type":"bigint","is_primary_key":true,"is_auto_increment":true}]},{"name":"posts","parent_name":"Content","columns":[{"name":"id","type":"bigint","is_primary_key":true,"is_auto_increment":true}]}]}`
