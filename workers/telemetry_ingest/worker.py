import os, json, time
import pika, psycopg2

RABBIT_HOST = os.getenv("RABBIT_HOST","rabbitmq")
PG_CONN = os.getenv("PG_CONN","host=postgres user=hlv password=hlvpass dbname=hearloveen")

def ensure_table():
    conn = psycopg2.connect(PG_CONN); cur = conn.cursor()
    cur.execute("""create table if not exists kpi_session(
        id serial primary key,
        session_id varchar(64) not null,
        child_id varchar(64) not null,
        overall_score numeric(4,1),
        emotion_label varchar(32),
        created_at timestamptz not null default now()
    );""")
    conn.commit(); cur.close(); conn.close()

def main():
    ensure_table()
    conn = psycopg2.connect(PG_CONN); conn.autocommit=True
    cur = conn.cursor()
    params = pika.ConnectionParameters(host=RABBIT_HOST)
    mq = pika.BlockingConnection(params)
    ch = mq.channel()
    ch.queue_declare(queue="telemetry", durable=True)

    def cb(chx, method, props, body):
        try:
            data = json.loads(body.decode())
            cur.execute("insert into kpi_session(session_id,child_id,overall_score,emotion_label) values(%s,%s,%s,%s)",
                (data.get("session_id","n/a"), data.get("child_id","n/a"),
                 data.get("overall", None), data.get("emotion","unknown")))
        except Exception as e:
            print("ERR:", e)
        chx.basic_ack(delivery_tag=method.delivery_tag)

    ch.basic_qos(prefetch_count=10)
    ch.basic_consume(queue="telemetry", on_message_callback=cb)
    print("telemetry_ingest running...")
    ch.start_consuming()

if __name__ == "__main__":
    main()