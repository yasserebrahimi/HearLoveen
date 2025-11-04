alter table "AudioSubmissions" enable row level security;
alter table "FeedbackReports" enable row level security;
create or replace function app_child_scope_contains(child uuid) returns boolean language sql immutable as $$
  select $1::text = ANY (regexp_split_to_array(current_setting('app.child_scope', true), ','));
$$;
do $$ begin
  create policy therapist_scope_audio on "AudioSubmissions"
    using ( coalesce(current_setting('role', true),'') = 'Admin' OR app_child_scope_contains("ChildId") );
exception when duplicate_object then null; end $$;
do $$ begin
  create policy therapist_scope_feedback on "FeedbackReports"
    using ( coalesce(current_setting('role', true),'') = 'Admin' OR app_child_scope_contains(
      (select "ChildId" from "AudioSubmissions" s where s."Id" = "SubmissionId")
    ));
exception when duplicate_object then null; end $$;
