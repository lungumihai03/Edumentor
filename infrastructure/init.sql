CREATE EXTENSION IF NOT EXISTS vector;
CREATE TABLE IF NOT EXISTS practices (
 id UUID PRIMARY KEY, title TEXT NOT NULL, description TEXT NOT NULL DEFAULT '',
 problem TEXT NOT NULL DEFAULT '', method TEXT NOT NULL DEFAULT '', subject TEXT NOT NULL DEFAULT '',
 education_level TEXT NOT NULL DEFAULT '', language TEXT NOT NULL DEFAULT 'ro',
 context TEXT NOT NULL DEFAULT '', status TEXT NOT NULL DEFAULT 'Published',
 author_name TEXT NOT NULL DEFAULT 'Profesor demo', created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE TABLE IF NOT EXISTS practice_chunks (
 id BIGSERIAL PRIMARY KEY, practice_id UUID NOT NULL REFERENCES practices(id) ON DELETE CASCADE,
 chunk_text TEXT NOT NULL, embedding vector(3072)
);
INSERT INTO practices(id,title,description,problem,method,subject,education_level,language,context,author_name,status)
VALUES
('11111111-1111-1111-1111-111111111111','Geometria prin modele tridimensionale','Elevii construiesc corpuri geometrice din materiale accesibile.','Dificultăți de vizualizare spațială.','Învățare practică în echipe.','Matematică','Gimnaziu','ro','Materiale reciclabile','Profesor demo','Published'),
('22222222-2222-2222-2222-222222222222','Învățare colaborativă prin proiecte locale','Elevii investighează o problemă din comunitate.','Implicare redusă.','Învățare prin proiecte.','Interdisciplinar','Gimnaziu','ro','Comunitate locală','Profesor demo','Published'),
('33333333-3333-3333-3333-333333333333','Формативное оценивание на уроке','Короткая обратная связь во время урока.','Недостаток своевременной обратной связи.','Формативное оценивание.','Общее','Средняя школа','ru','Обычный класс','Демо-учитель','Published')
ON CONFLICT DO NOTHING;
