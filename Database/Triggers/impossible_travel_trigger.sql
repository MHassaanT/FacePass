-- Function to detect impossible travel
CREATE OR REPLACE FUNCTION detect_impossible_travel()
RETURNS TRIGGER AS $$
DECLARE
    last_classroom_id UUID;
    last_timestamp TIMESTAMPTZ;
BEGIN
    -- Get the last attendance log for this student (excluding the current one)
    SELECT classroom_id, timestamp INTO last_classroom_id, last_timestamp
    FROM attendance_logs
    WHERE student_id = NEW.student_id
      AND id != NEW.id
    ORDER BY timestamp DESC
    LIMIT 1;

    -- If a previous log exists
    IF FOUND THEN
        -- If classroom is different AND time difference is less than 60 seconds
        IF last_classroom_id != NEW.classroom_id AND (NEW.timestamp - last_timestamp) < INTERVAL '60 seconds' THEN
            NEW.status := 'suspicious';
            NEW.flagged_reason := 'Impossible Travel Detected (Different building within 60s)';
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Trigger to run before every INSERT on attendance_logs
CREATE TRIGGER trg_impossible_travel
BEFORE INSERT ON attendance_logs
FOR EACH ROW
EXECUTE FUNCTION detect_impossible_travel();
