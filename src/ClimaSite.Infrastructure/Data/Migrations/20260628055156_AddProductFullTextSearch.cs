using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace ClimaSite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProductFullTextSearch : Migration
    {
        // SEARCH-01-fts. Adds Postgres full-text search to products: a trigger-maintained, denormalised
        // `search_vector` (base fields + tags + ALL translations in one doc), a custom `climasite_search`
        // config (simple + unaccent — one config for EN/BG/DE; query config must equal vector config), GIN +
        // pg_trgm indexes. The vector is a TRIGGER-maintained plain column (NOT a generated column: it spans
        // product_translations and uses array_to_string(tags), which is STABLE not IMMUTABLE). v1 builds the
        // index in-transaction (OPS-08: no live data); the future live-DB story is a separate CONCURRENTLY
        // migration. All DDL is inlined literally here (frozen migration history — never reference a mutable
        // shared constant).

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Extensions: unaccent (diacritic folding) + pg_trgm (ILIKE substring fallback). Emits
            // CREATE EXTENSION IF NOT EXISTS …
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "search_vector",
                table: "products",
                type: "tsvector",
                nullable: true);

            // 1) The text-search configuration (idempotent). MUST exist before the SQL helper function below,
            //    which references it and is parsed at creation time.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = 'climasite_search') THEN
        CREATE TEXT SEARCH CONFIGURATION climasite_search (COPY = simple);
        ALTER TEXT SEARCH CONFIGURATION climasite_search
            ALTER MAPPING FOR asciiword, asciihword, hword_asciipart, word, hword, hword_part, numword, numhword
            WITH unaccent, simple;
    END IF;
END $$;");

            // 2) Helper that builds a product's denormalised search document. STABLE (array_to_string is
            //    STABLE) — fine for trigger use. Weights: A=name(+translation names), B=brand/model,
            //    C=tags, D=descriptions(+translation descriptions). unaccent is baked into the config mapping
            //    (NEVER unaccent(col) here — that is STABLE and would break immutability if ever inlined).
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_product_search_document(
    p_name text, p_brand text, p_model text, p_tags text[],
    p_short text, p_desc text, p_tr_names text, p_tr_descs text)
RETURNS tsvector LANGUAGE sql STABLE AS $$
    SELECT setweight(to_tsvector('climasite_search', coalesce(p_name, '')), 'A')
        || setweight(to_tsvector('climasite_search', coalesce(p_tr_names, '')), 'A')
        || setweight(to_tsvector('climasite_search', coalesce(p_brand, '') || ' ' || coalesce(p_model, '')), 'B')
        || setweight(to_tsvector('climasite_search', array_to_string(coalesce(p_tags, '{}'::text[]), ' ')), 'C')
        || setweight(to_tsvector('climasite_search',
             coalesce(p_short, '') || ' ' || coalesce(p_desc, '') || ' ' || coalesce(p_tr_descs, '')), 'D');
$$;");

            // 3a) products trigger — recompute the vector from NEW + its translations on insert / relevant
            //     update. Fires only on the source text columns (NOT search_vector), so 3b's UPDATE can't
            //     re-fire it (no recursion). On INSERT, translations don't exist yet → 3b fills them in.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_products_search_vector_trg() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    NEW.search_vector := fn_product_search_document(
        NEW.name, NEW.brand, NEW.model, NEW.tags, NEW.short_description, NEW.description,
        (SELECT string_agg(t.name, ' ') FROM product_translations t WHERE t.product_id = NEW.id),
        (SELECT string_agg(coalesce(t.short_description, '') || ' ' || coalesce(t.description, ''), ' ')
           FROM product_translations t WHERE t.product_id = NEW.id));
    RETURN NEW;
END $$;

DROP TRIGGER IF EXISTS trg_products_search_vector ON products;
CREATE TRIGGER trg_products_search_vector
    BEFORE INSERT OR UPDATE OF name, brand, model, tags, short_description, description
    ON products FOR EACH ROW EXECUTE FUNCTION fn_products_search_vector_trg();");

            // 3b) translations trigger — when a translation is inserted/updated/deleted, recompute its parent
            //     product's vector (reads the committed product row + all its translations).
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION fn_product_translations_search_vector_trg() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_id uuid;
BEGIN
    v_id := COALESCE(NEW.product_id, OLD.product_id);
    UPDATE products p SET search_vector = fn_product_search_document(
        p.name, p.brand, p.model, p.tags, p.short_description, p.description,
        (SELECT string_agg(t.name, ' ') FROM product_translations t WHERE t.product_id = v_id),
        (SELECT string_agg(coalesce(t.short_description, '') || ' ' || coalesce(t.description, ''), ' ')
           FROM product_translations t WHERE t.product_id = v_id))
    WHERE p.id = v_id;
    RETURN NULL;
END $$;

DROP TRIGGER IF EXISTS trg_product_translations_search_vector ON product_translations;
CREATE TRIGGER trg_product_translations_search_vector
    AFTER INSERT OR UPDATE OR DELETE ON product_translations
    FOR EACH ROW EXECUTE FUNCTION fn_product_translations_search_vector_trg();");

            // 4) Backfill any pre-existing rows (no-op on a fresh DB; the UPDATE touches only search_vector so
            //    the 3a trigger does not fire and the explicit value stands).
            migrationBuilder.Sql(@"
UPDATE products p SET search_vector = fn_product_search_document(
    p.name, p.brand, p.model, p.tags, p.short_description, p.description,
    (SELECT string_agg(t.name, ' ') FROM product_translations t WHERE t.product_id = p.id),
    (SELECT string_agg(coalesce(t.short_description, '') || ' ' || coalesce(t.description, ''), ' ')
       FROM product_translations t WHERE t.product_id = p.id));");

            // 5) Indexes (in-transaction, non-concurrent — safe with no live data). GIN on the vector + pg_trgm
            //    GIN on the ILIKE-fallback columns.
            migrationBuilder.CreateIndex(
                name: "ix_products_brand_trgm",
                table: "products",
                column: "brand")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_model_trgm",
                table: "products",
                column: "model")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_products_name_trgm",
                table: "products",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_products_search_vector",
                table: "products",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_products_sku_trgm",
                table: "products",
                column: "sku")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Load-bearing teardown order: triggers → trigger functions → the SQL helper (depends on the
            // config) → indexes → column → config (after the function that depends on it is gone) → extensions.
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_product_translations_search_vector ON product_translations;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_products_search_vector ON products;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_product_translations_search_vector_trg();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_products_search_vector_trg();");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS fn_product_search_document(text, text, text, text[], text, text, text, text);");

            migrationBuilder.DropIndex(
                name: "ix_products_brand_trgm",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_model_trgm",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_name_trgm",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_search_vector",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_sku_trgm",
                table: "products");

            migrationBuilder.DropColumn(
                name: "search_vector",
                table: "products");

            migrationBuilder.Sql("DROP TEXT SEARCH CONFIGURATION IF EXISTS climasite_search;");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:unaccent", ",,");
        }
    }
}
