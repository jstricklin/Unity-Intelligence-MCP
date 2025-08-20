-- Source registry for different documentation types
CREATE TABLE doc_sources (
    id INTEGER PRIMARY KEY,
    source_type TEXT NOT NULL, -- 'scripting_api', 'editor_manual', 'tutorial', 'package_docs', etc.
    source_name TEXT NOT NULL, -- 'Unity Scripting API', 'Unity Manual', etc.
    version TEXT,
    base_url TEXT,
    schema_version TEXT, -- For handling different extraction models
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(source_type, version)
);

-- Universal document container
CREATE TABLE unity_docs (
    id INTEGER PRIMARY KEY,
    source_id INTEGER NOT NULL,
    
    -- Universal identifiers
    doc_key TEXT NOT NULL, -- Source-specific unique identifier (file_path, url slug, etc.)
    title TEXT NOT NULL,
    url TEXT, -- Full or relative URL
    
    -- Content organization
    doc_type TEXT, -- 'class', 'manual_page', 'tutorial_step', 'package_overview', etc.
    category TEXT, -- 'Components', 'Scripting', 'Animation', 'Audio', etc.
    subcategory TEXT,
    
    -- Hierarchy and relationships
    parent_doc_id INTEGER, -- For hierarchical content (manual sections, tutorial steps)
    sort_order INTEGER, -- Ordering within parent or category
    
    -- Version and lifecycle
    unity_version TEXT,
    content_hash TEXT,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    
    -- Universal embeddings
    title_embedding BLOB,
    summary_embedding BLOB, -- Short description/summary
    full_content_embedding BLOB, -- Combined content for broad searches
    
    FOREIGN KEY (source_id) REFERENCES doc_sources (id),
    FOREIGN KEY (parent_doc_id) REFERENCES unity_docs (id),
    
    UNIQUE(source_id, doc_key)
);

-- Source-specific structured data (JSON for flexibility)
CREATE TABLE doc_metadata (
    id INTEGER PRIMARY KEY,
    doc_id INTEGER NOT NULL,
    metadata_type TEXT NOT NULL, -- 'scripting_api', 'manual_structure', 'tutorial_info', etc.
    metadata_json TEXT NOT NULL, -- JSON blob for source-specific data
    
    FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
    UNIQUE(doc_id, metadata_type)
);

-- Flexible content elements (replaces rigid doc_elements)
CREATE TABLE content_elements (
    id INTEGER PRIMARY KEY,
    doc_id INTEGER NOT NULL,
    
    -- Element identification
    element_key TEXT, -- Source-specific identifier
    element_type TEXT NOT NULL, -- 'api_method', 'manual_section', 'code_example', 'image', 'note', etc.
    title TEXT,
    
    -- Hierarchy within document
    parent_element_id INTEGER,
    sort_order INTEGER,
    
    -- Content and context
    content TEXT,
    content_type TEXT DEFAULT 'text', -- 'text', 'code', 'markdown', 'html'
    
    -- Source-specific attributes (JSON for flexibility)
    attributes_json TEXT, -- {"is_inherited": true, "return_type": "void", "difficulty": "beginner", etc.}
    
    -- Semantic embeddings
    element_embedding BLOB,
    
    FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
    FOREIGN KEY (parent_element_id) REFERENCES content_elements (id) ON DELETE CASCADE
);

-- Granular content chunks for detailed semantic search
CREATE TABLE content_chunks (
    id INTEGER PRIMARY KEY,
    doc_id INTEGER NOT NULL,
    element_id INTEGER, -- NULL for document-level chunks
    
    -- Chunk metadata
    chunk_type TEXT, -- 'description', 'code_example', 'step_instruction', 'warning', etc.
    chunk_index INTEGER,
    content TEXT NOT NULL,
    token_count INTEGER,
    
    -- Context preservation
    preceding_context TEXT, -- Brief context from previous chunks
    following_context TEXT, -- Brief context from next chunks
    
    -- Embeddings
    embedding BLOB,
    contextual_embedding BLOB, -- Includes surrounding context
    
    FOREIGN KEY (doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
    FOREIGN KEY (element_id) REFERENCES content_elements (id) ON DELETE CASCADE
);

-- Cross-document relationships and references
CREATE TABLE doc_relationships (
    id INTEGER PRIMARY KEY,
    source_doc_id INTEGER NOT NULL,
    target_doc_id INTEGER NOT NULL,
    relationship_type TEXT NOT NULL, -- 'references', 'prerequisite', 'related', 'implements', etc.
    relationship_strength REAL DEFAULT 1.0, -- For weighted relationships
    
    FOREIGN KEY (source_doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
    FOREIGN KEY (target_doc_id) REFERENCES unity_docs (id) ON DELETE CASCADE,
    
    UNIQUE(source_doc_id, target_doc_id, relationship_type)
);

-- Vector similarity indices
CREATE VIRTUAL TABLE vec_docs_index USING vss0(
    embedding(768)
);

CREATE VIRTUAL TABLE vec_elements_index USING vss0(
    embedding(768)
);

CREATE VIRTUAL TABLE vec_chunks_index USING vss0(
    embedding(768)
);

-- Performance indices
CREATE INDEX idx_docs_source_type ON unity_docs(source_id, doc_type);
CREATE INDEX idx_docs_category ON unity_docs(category, subcategory);
CREATE INDEX idx_docs_version ON unity_docs(unity_version);
CREATE INDEX idx_elements_type ON content_elements(element_type);
CREATE INDEX idx_elements_doc ON content_elements(doc_id, element_type);
CREATE INDEX idx_chunks_type ON content_chunks(chunk_type);
CREATE INDEX idx_relationships_type ON doc_relationships(relationship_type);

-- Source-specific views for common queries

-- Scripting API view (preserves existing query patterns)
CREATE VIEW scripting_api_docs AS
SELECT 
    d.id,
    d.title,
    d.doc_key as file_path,
    json_extract(dm.metadata_json, '$.description') as description,
    json_extract(dm.metadata_json, '$.class_name') as class_name,
    json_extract(dm.metadata_json, '$.namespace') as namespace,
    d.title_embedding,
    d.summary_embedding,
    d.full_content_embedding
FROM unity_docs d
JOIN doc_sources s ON d.source_id = s.id  
LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type = 'scripting_api'
WHERE s.source_type = 'scripting_api';

-- Manual/Tutorial hierarchical view
CREATE VIEW manual_hierarchy AS
SELECT 
    d.id,
    d.title,
    d.doc_key,
    d.category,
    d.subcategory,
    d.parent_doc_id,
    d.sort_order,
    json_extract(dm.metadata_json, '$.section_type') as section_type,
    json_extract(dm.metadata_json, '$.difficulty_level') as difficulty_level,
    d.title_embedding,
    d.summary_embedding
FROM unity_docs d
JOIN doc_sources s ON d.source_id = s.id
LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type IN ('manual_structure', 'tutorial_info')
WHERE s.source_type IN ('editor_manual', 'tutorial');

-- API elements view (maintains compatibility with existing queries)
CREATE VIEW api_elements AS
SELECT 
    ce.id,
    ce.title,
    ce.content as description,
    ce.element_type,
    json_extract(ce.attributes_json, '$.is_inherited') as is_inherited,
    json_extract(ce.attributes_json, '$.inheritance_source') as inheritance_source,
    json_extract(ce.attributes_json, '$.return_type') as return_type,
    d.title as class_name,
    json_extract(dm.metadata_json, '$.namespace') as namespace,
    ce.element_embedding
FROM content_elements ce
JOIN unity_docs d ON ce.doc_id = d.id
JOIN doc_sources s ON d.source_id = s.id
LEFT JOIN doc_metadata dm ON d.id = dm.doc_id AND dm.metadata_type = 'scripting_api'
WHERE s.source_type = 'scripting_api'
AND ce.element_type IN ('property', 'public_method', 'static_method', 'message');

-- Full-text search across all sources
CREATE VIRTUAL TABLE universal_fts USING fts5(
    title,
    content,
    source_type,
    doc_type,
    category,
    content='unity_docs'
);

-- Example source registration
INSERT INTO doc_sources (source_type, source_name, version, schema_version) VALUES
('scripting_api', 'Unity Scripting API', '2023.3', '1.0'),
('editor_manual', 'Unity User Manual', '2023.3', '1.0'),
('tutorial', 'Unity Learn Tutorials', 'current', '1.0'),
('package_docs', 'Unity Package Documentation', 'current', '1.0');

-- Example queries for different source types

-- Cross-source semantic search
-- QUERY: Find content about "physics" across all sources
/*
SELECT d.title, s.source_name, d.doc_type, d.category
FROM unity_docs d
JOIN doc_sources s ON d.source_id = s.id
JOIN vec_docs_index v ON d.rowid = v.rowid
WHERE vss_search(v.embedding, @physics_embedding) > 0.7
ORDER BY vss_search(v.embedding, @physics_embedding) DESC;
*/

-- Source-specific detailed search
-- QUERY: Find scripting API methods related to "collision"
/*
SELECT ae.title, ae.class_name, ae.description, ae.element_type
FROM api_elements ae
JOIN vec_elements_index v ON ae.rowid = v.rowid  
WHERE vss_search(v.embedding, @collision_embedding) > 0.75
AND ae.element_type IN ('public_method', 'static_method')
ORDER BY vss_search(v.embedding, @collision_embedding) DESC;
*/

-- Hierarchical manual navigation
-- QUERY: Get manual section hierarchy for "Animation"
/*
WITH RECURSIVE manual_tree AS (
  SELECT id, title, parent_doc_id, 0 as level, title as path
  FROM manual_hierarchy 
  WHERE parent_doc_id IS NULL AND category = 'Animation'
  
  UNION ALL
  
  SELECT m.id, m.title, m.parent_doc_id, mt.level + 1,
         mt.path || ' > ' || m.title
  FROM manual_hierarchy m
  JOIN manual_tree mt ON m.parent_doc_id = mt.id
)
SELECT * FROM manual_tree ORDER BY level, sort_order;
*/
