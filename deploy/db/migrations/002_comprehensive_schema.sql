-- ========================================
-- HearLoveen Comprehensive Database Schema
-- Version: 2.0
-- Date: 2025-01-07
-- ========================================

-- Users and Authentication
CREATE TABLE IF NOT EXISTS users (
    id VARCHAR(64) PRIMARY KEY,
    email VARCHAR(255) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    role VARCHAR(32) NOT NULL, -- 'parent', 'therapist', 'admin'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_login TIMESTAMPTZ,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);

-- Children/Patients
CREATE TABLE IF NOT EXISTS children (
    id VARCHAR(64) PRIMARY KEY,
    parent_id VARCHAR(64) NOT NULL REFERENCES users(id),
    therapist_id VARCHAR(64) REFERENCES users(id),
    name VARCHAR(255) NOT NULL,
    age INTEGER NOT NULL,
    diagnosis TEXT,
    hearing_loss_level VARCHAR(32), -- 'mild', 'moderate', 'severe', 'profound'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_children_parent ON children(parent_id);
CREATE INDEX idx_children_therapist ON children(therapist_id);

-- Sessions
CREATE TABLE IF NOT EXISTS sessions (
    id VARCHAR(64) PRIMARY KEY,
    child_id VARCHAR(64) NOT NULL REFERENCES children(id),
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    ended_at TIMESTAMPTZ,
    duration_seconds INTEGER,
    overall_score NUMERIC(4,1),
    status VARCHAR(32) NOT NULL DEFAULT 'in_progress', -- 'in_progress', 'completed', 'cancelled'
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_sessions_child ON sessions(child_id);
CREATE INDEX idx_sessions_started ON sessions(started_at);

-- Audio Recordings
CREATE TABLE IF NOT EXISTS audio_recordings (
    id VARCHAR(64) PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL REFERENCES sessions(id),
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    file_url TEXT NOT NULL,
    file_size_bytes BIGINT,
    duration_seconds INTEGER,
    sample_rate INTEGER,
    format VARCHAR(16), -- 'wav', 'mp3', 'm4a'
    transcription TEXT,
    anonymized_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audio_session ON audio_recordings(session_id);
CREATE INDEX idx_audio_user ON audio_recordings(user_id);
CREATE INDEX idx_audio_anonymized ON audio_recordings(anonymized_at);

-- Analysis Results
CREATE TABLE IF NOT EXISTS analysis_results (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL REFERENCES sessions(id),
    audio_recording_id VARCHAR(64) REFERENCES audio_recordings(id),
    analysis_type VARCHAR(32) NOT NULL, -- 'pronunciation', 'emotion', 'speech_quality'
    score NUMERIC(4,1),
    confidence NUMERIC(4,3),
    details JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_analysis_session ON analysis_results(session_id);
CREATE INDEX idx_analysis_type ON analysis_results(analysis_type);

-- Emotion Analysis
CREATE TABLE IF NOT EXISTS emotion_analysis (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL REFERENCES sessions(id),
    timestamp_ms INTEGER NOT NULL,
    primary_emotion VARCHAR(32) NOT NULL,
    confidence NUMERIC(4,3) NOT NULL,
    valence NUMERIC(4,3), -- -1 to +1
    arousal NUMERIC(4,3), -- -1 to +1
    dominance NUMERIC(4,3), -- -1 to +1
    details JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_emotion_session ON emotion_analysis(session_id);

-- Pronunciation Feedback
CREATE TABLE IF NOT EXISTS pronunciation_feedback (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL REFERENCES sessions(id),
    word VARCHAR(255) NOT NULL,
    phoneme VARCHAR(32) NOT NULL,
    accuracy NUMERIC(4,3) NOT NULL,
    expected_pronunciation TEXT,
    actual_pronunciation TEXT,
    feedback TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_pronunciation_session ON pronunciation_feedback(session_id);

-- XAI Explanations
CREATE TABLE IF NOT EXISTS xai_explanations (
    id SERIAL PRIMARY KEY,
    session_id VARCHAR(64) NOT NULL REFERENCES sessions(id),
    analysis_type VARCHAR(32) NOT NULL,
    confidence NUMERIC(4,3) NOT NULL,
    factors JSONB NOT NULL, -- Array of contributing factors
    recommendations JSONB NOT NULL, -- Array of recommendations
    model_version VARCHAR(32),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_xai_session ON xai_explanations(session_id);

-- Audit Log (7-year retention for MDR compliance)
CREATE TABLE IF NOT EXISTS audit_log (
    id SERIAL PRIMARY KEY,
    event_type VARCHAR(64) NOT NULL,
    user_id VARCHAR(64),
    description TEXT NOT NULL,
    ip_address INET,
    user_agent TEXT,
    metadata JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_event ON audit_log(event_type);
CREATE INDEX idx_audit_user ON audit_log(user_id);
CREATE INDEX idx_audit_created ON audit_log(created_at);

-- Subscriptions and Billing
CREATE TABLE IF NOT EXISTS subscriptions (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    plan_type VARCHAR(32) NOT NULL, -- 'free', 'basic', 'premium', 'enterprise'
    status VARCHAR(32) NOT NULL, -- 'active', 'cancelled', 'expired', 'trial'
    started_at TIMESTAMPTZ NOT NULL,
    expires_at TIMESTAMPTZ,
    monthly_price NUMERIC(10,2),
    currency VARCHAR(3) DEFAULT 'EUR',
    stripe_subscription_id VARCHAR(255),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_subscriptions_user ON subscriptions(user_id);
CREATE INDEX idx_subscriptions_status ON subscriptions(status);

-- User Events (for analytics)
CREATE TABLE IF NOT EXISTS user_events (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    event_type VARCHAR(64) NOT NULL, -- 'signup', 'login', 'session_completed', 'subscription_created', etc.
    event_data JSONB,
    revenue_eur NUMERIC(10,2), -- For revenue events
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_events_user ON user_events(user_id);
CREATE INDEX idx_events_type ON user_events(event_type);
CREATE INDEX idx_events_created ON user_events(created_at);

-- IoT Devices (Hearing Aids)
CREATE TABLE IF NOT EXISTS iot_devices (
    id VARCHAR(64) PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    device_type VARCHAR(32) NOT NULL, -- 'hearing_aid', 'smartphone'
    manufacturer VARCHAR(255),
    model VARCHAR(255),
    firmware_version VARCHAR(32),
    protocol VARCHAR(16), -- 'ASHA', 'MFi', 'BLE'
    last_connected TIMESTAMPTZ,
    battery_level INTEGER,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    pairing_data JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_devices_user ON iot_devices(user_id);
CREATE INDEX idx_devices_protocol ON iot_devices(protocol);

-- Notifications
CREATE TABLE IF NOT EXISTS notifications (
    id SERIAL PRIMARY KEY,
    user_id VARCHAR(64) NOT NULL REFERENCES users(id),
    type VARCHAR(32) NOT NULL, -- 'session_reminder', 'progress_update', 'system_alert'
    title VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    sent_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_notifications_user ON notifications(user_id);
CREATE INDEX idx_notifications_read ON notifications(is_read);

-- Progress Tracking
CREATE TABLE IF NOT EXISTS progress_metrics (
    id SERIAL PRIMARY KEY,
    child_id VARCHAR(64) NOT NULL REFERENCES children(id),
    metric_type VARCHAR(32) NOT NULL, -- 'pronunciation', 'vocabulary', 'emotion_stability'
    score NUMERIC(4,1) NOT NULL,
    measured_at DATE NOT NULL,
    details JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_progress_child ON progress_metrics(child_id);
CREATE INDEX idx_progress_date ON progress_metrics(measured_at);

-- ========================================
-- Functions and Triggers
-- ========================================

-- Update updated_at timestamp automatically
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_children_updated_at BEFORE UPDATE ON children
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_subscriptions_updated_at BEFORE UPDATE ON subscriptions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- ========================================
-- Views for Analytics
-- ========================================

-- Monthly Active Users
CREATE OR REPLACE VIEW monthly_active_users AS
SELECT
    DATE_TRUNC('month', created_at) AS month,
    COUNT(DISTINCT user_id) AS active_users
FROM user_events
WHERE created_at >= NOW() - INTERVAL '12 months'
GROUP BY DATE_TRUNC('month', created_at)
ORDER BY month DESC;

-- Session Completion Rate
CREATE OR REPLACE VIEW session_completion_rate AS
SELECT
    DATE_TRUNC('week', started_at) AS week,
    COUNT(*) AS total_sessions,
    COUNT(*) FILTER (WHERE status = 'completed') AS completed_sessions,
    ROUND(100.0 * COUNT(*) FILTER (WHERE status = 'completed') / COUNT(*), 2) AS completion_rate
FROM sessions
WHERE started_at >= NOW() - INTERVAL '3 months'
GROUP BY DATE_TRUNC('week', started_at)
ORDER BY week DESC;

-- Average Progress by Child
CREATE OR REPLACE VIEW child_progress_summary AS
SELECT
    c.id,
    c.name,
    c.age,
    c.diagnosis,
    AVG(s.overall_score) AS avg_score,
    COUNT(s.id) AS total_sessions,
    MAX(s.started_at) AS last_session_date
FROM children c
LEFT JOIN sessions s ON c.id = s.child_id
GROUP BY c.id, c.name, c.age, c.diagnosis;

-- ========================================
-- Grant Permissions (adjust as needed)
-- ========================================

-- Grant read/write to application role
-- CREATE ROLE hearloveen_app WITH LOGIN PASSWORD 'your_secure_password';
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO hearloveen_app;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO hearloveen_app;

-- ========================================
-- Data Retention Policies
-- ========================================

-- Anonymize audio recordings older than 60 days (GDPR)
CREATE OR REPLACE FUNCTION anonymize_old_recordings()
RETURNS void AS $$
BEGIN
    UPDATE audio_recordings
    SET
        user_id = 'ANONYMIZED',
        anonymized_at = NOW()
    WHERE
        created_at < NOW() - INTERVAL '60 days'
        AND anonymized_at IS NULL;
END;
$$ LANGUAGE plpgsql;

-- Schedule this function to run daily via pg_cron or external scheduler

COMMENT ON TABLE users IS 'User accounts (parents, therapists, admins)';
COMMENT ON TABLE children IS 'Children/patients receiving therapy';
COMMENT ON TABLE sessions IS 'Therapy sessions';
COMMENT ON TABLE audio_recordings IS 'Audio files from sessions';
COMMENT ON TABLE analysis_results IS 'AI analysis results';
COMMENT ON TABLE audit_log IS '7-year retention for MDR compliance';
COMMENT ON TABLE subscriptions IS 'User subscription and billing information';
COMMENT ON TABLE user_events IS 'User activity events for LTV/CAC analytics';
