-- DSR Requests
create table if not exists dsr_requests(
  id serial primary key,
  user_id varchar(64) not null,
  action varchar(16) not null,
  status varchar(16) not null default 'queued',
  created_at timestamptz not null default now()
);

-- KPI per session (simplified)
create table if not exists kpi_session(
  id serial primary key,
  session_id varchar(64) not null,
  child_id varchar(64) not null,
  overall_score numeric(4,1),
  emotion_label varchar(32),
  created_at timestamptz not null default now()
);