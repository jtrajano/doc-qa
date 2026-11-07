# AI-Powered Document Q&A: Product Design Document

**Author:** Jeff\
**Status:** Draft v1\
**Last updated:** November 7, 2026\
**Tracking:** Linear project "AI-powered document Q&A" (JEF tickets)

> **How to read this document.** Sections marked *(from Linear)* reflect tickets that exist today. Sections marked *(proposed)* are design decisions I filled in to complete the picture; they are not yet ticketed and should be reviewed before work starts.

---

## 1. Overview

AI-Powered Document Q&A lets a user upload PDF documents and ask natural-language questions about them. The system extracts text, splits it into chunks, converts the chunks to vector embeddings, and stores them in a vector database. At question time it retrieves the most relevant chunks and uses them to produce an answer that cites the source document and page.

The project is also a deliberate learning vehicle for full-stack .NET, React and DevOps, so the design favors clear abstractions, tests, containers and CI over minimal code.

## 2. Problem and Goals

### Problem

Finding a specific answer inside long PDFs (manuals, contracts, reports) is slow. Keyword search misses paraphrases, and reading everything does not scale.

### Goals

- Upload PDFs and track their processing status.
- Index documents asynchronously so uploads stay fast.
- Answer questions grounded in the uploaded content, with page-level citations.
- Run the entire stack locally with one command (`docker compose up`).
- Keep every external dependency (embedding provider, vector store, blob storage) behind an interface so it can be swapped or faked in tests.

### Non-goals (v1)

- Multi-tenant accounts, billing or sharing between users.
- File types other than PDF; OCR for scanned PDFs.
- Real-time collaborative features.
- Fine-tuning models.

## 3. Users and Key Scenarios *(proposed)*

**Primary user:** an individual who has a handful to a few hundred PDFs and wants fast, trustworthy answers.

| Scenario | Outcome |
| --- | --- |
| Upload a PDF | The file appears in the list as *Uploaded*, then moves to *Processing* and *Ready*. |
| Upload a bad file | The user sees a clear error (wrong type, too large, corrupt, password-protected). |
| Ask a question | The user gets an answer plus the page(s) it came from. |
| Delete a document | The file, its database record and its vectors are all removed. |
| Reprocess a document | Old vectors are replaced, with no duplicates. |

## 4. Scope and Features

### 4.1 Document management *(from Linear: JEF-21, JEF-23)*

- `POST /documents`: multipart PDF upload. Validates content type, extension, magic bytes and a configurable max size. Returns 400 or 413 with a problem-details body.
- `GET /documents` and `GET /documents/{id}`: metadata and status.
- `DELETE /documents/{id}`: removes the row and the blob.
- UI: drag-and-drop and file picker restricted to PDFs, client-side size check, upload progress and error states, document list (name, size, uploaded date, status), polling while anything is processing, delete with confirmation.

### 4.2 Ingestion pipeline *(from Linear: JEF-17, JEF-18, JEF-19, JEF-20, JEF-22)*

1. **Extract**: page-by-page text with page numbers (PdfPig). Handles empty, corrupt and password-protected PDFs with clear errors. *(Done)*
2. **Chunk**: overlapping chunks with configurable size and overlap, split on paragraph or sentence boundaries where possible. Each chunk keeps document id, page number(s) and chunk index.
3. **Embed**: provider-agnostic `IEmbeddingGenerator`, one real provider (Azure OpenAI or OpenAI), batching with retry and backoff, and a fake generator for tests.
4. **Index**: `IVectorStore` with upsert, delete by document id and top-K similarity search. Each vector carries document id, pages, chunk index and chunk text.
5. **Orchestrate**: upload enqueues a message on an Azure Storage Queue; a hosted background worker runs the steps above. Status moves *Uploaded, Processing, then Ready or Failed* (with the error message stored). Reprocessing is idempotent. Poison messages go to a poison queue after bounded retries.

### 4.3 Question answering *(proposed, not yet ticketed)*

- `POST /questions` takes a question and an optional document filter.
- The question is embedded, then the top-K chunks are retrieved from the vector store.
- A chat model receives the question plus the retrieved chunks with an instruction to answer only from the provided context and to say so when the answer is not present.
- The response contains the answer and a list of citations (document name, page number(s), chunk snippet).
- UI: a chat panel next to the document list, showing the answer with clickable citations.

## 5. User Experience *(proposed)*

**Main screen:** two areas. The left holds the upload zone and the document list with status badges. The right holds the question box and answer area.

**Status badges:** Uploaded (grey), Processing (amber with spinner), Ready (green), Failed (red, with the error on hover).

**Empty and error states:** an empty list invites the first upload; a question against zero *Ready* documents explains that nothing is searchable yet; a failed question shows a retry action.

**Answer view:** answer text first, then a "Sources" list. Selecting a source shows the chunk text and page number.

## 6. System Architecture

```
React + TypeScript (Vite, served by nginx)
        |  HTTPS / JSON
        v
ASP.NET Core API (.NET 10)  ----> SQL Server (EF Core: Documents)
   |            |
   |            +-------------> Blob storage (Azurite locally)
   |            |
   |            +-------------> Azure Storage Queue (Azurite locally)
   |                                   |
   |                                   v
   |                      Background worker (hosted service)
   |                      extract -> chunk -> embed -> upsert
   |                                   |
   +---- similarity search <-----------+----> Vector DB (pgvector or Qdrant)
   |
   +---- embeddings / chat ------------> Azure OpenAI or OpenAI
```

### Technology choices

| Area | Choice |
| --- | --- |
| Backend | ASP.NET Core on .NET 10, xUnit, Scalar for API docs |
| Frontend | React, TypeScript, Vite, Vitest, ESLint, Prettier |
| Relational data | SQL Server via EF Core |
| Files | Azure Blob Storage (Azurite locally) |
| Async work | Azure Storage Queue (Azurite locally) plus a hosted background service |
| Vectors | pgvector or Qdrant (decision pending, JEF-20) |
| PDF parsing | PdfPig |
| AI provider | Azure OpenAI or OpenAI behind `IEmbeddingGenerator` |
| Containers | Docker, multi-stage builds, docker-compose |
| CI | GitHub Actions, separate backend and frontend workflows |

### Design principles

- **Everything external sits behind an interface** (`IBlobStorage`, `ITextExtractor`, `IChunker`, `IEmbeddingGenerator`, `IVectorStore`).
- **Secrets never live in source:** configuration, user-secrets and `.env` (with a committed `.env.example`).
- **Upload is fast, processing is async:** the API never does embedding work in the request path.
- **Idempotent processing:** safe to retry and reprocess.

## 7. Data Model

### Document (SQL Server) *(from Linear: JEF-15)*

| Field | Notes |
| --- | --- |
| Id | Primary key |
| FileName | Original name |
| ContentType | Expected `application/pdf` |
| Size | Bytes |
| BlobPath | Location in blob storage |
| Status | `DocumentStatus` enum: Uploaded, Processing, Ready, Failed |
| Error | Message stored on failure *(per JEF-22)* |
| CreatedAt / UpdatedAt | Timestamps |

### Chunk vector record (vector store) *(from Linear: JEF-20)*

Document id, page number(s), chunk index, chunk text, and the embedding vector. Vector dimensions come from configuration and must match the embedding model, because the schema depends on them.

## 8. API Surface

| Method | Route | Purpose | Status |
| --- | --- | --- | --- |
| GET | /health | Liveness plus blob storage connectivity | Done |
| POST | /documents | Upload a PDF | Planned (JEF-21) |
| GET | /documents | List documents | Planned (JEF-21) |
| GET | /documents/{id} | Document detail and status | Planned (JEF-21) |
| DELETE | /documents/{id} | Remove document, blob and vectors | Planned (JEF-21) |
| POST | /questions | Ask a question, get cited answer | Proposed |

Errors use RFC 7807 problem details. All routes are documented in Scalar.

## 9. Non-Functional Requirements

- **Security:** validate magic bytes (not just the extension), enforce a configurable size limit, run containers as non-root, keep keys out of source. *Proposed:* authentication before any shared deployment.
- **Reliability:** bounded retries, poison queue, idempotent reprocessing, health checks on every compose service.
- **Performance:** upload returns immediately after storing the file; embedding calls are batched. *Proposed target:* a typical 20-page PDF reaches *Ready* in under a minute and a question is answered in a few seconds.
- **Cost control:** embeddings are generated once per chunk; the fake generator keeps tests free of API spend.
- **Observability** *(proposed)*: structured logging with document id as a correlation field, and a status endpoint for queue depth.
- **Maintainability:** chunk size, overlap and embedding dimensions documented in `docs/` with the reasoning behind each default.

## 10. Testing Strategy *(from Linear)*

- **Unit tests (xUnit, Vitest):** extractor, chunker (short, long, overlap, empty input), validators, React components.
- **Integration tests:** Azurite blob round trip; vector store upsert, search and delete against the compose database.
- **End-to-end test:** upload a sample PDF, wait for *Ready*, confirm similarity search returns a relevant chunk, using fake embeddings.
- **CI:** every push and PR runs restore, build and test on the backend and install, lint, test and build on the frontend, with path filters and caching.

## 11. Delivery Plan and Status

### Sprint 1: Foundations *(complete)*

| Ticket | Work | Status |
| --- | --- | --- |
| JEF-7 | Repo, folder structure, branch protection | Done |
| JEF-8 | ASP.NET Core API scaffold and /health | Done |
| JEF-9 | React + TypeScript + Vite scaffold | Done |
| JEF-10, JEF-11 | Dockerfiles for API and frontend | Done |
| JEF-12 | docker-compose for the full local stack | Done |
| JEF-13, JEF-14 | CI workflows (backend, frontend) | Done |

### Sprint 2: Backend core *(in progress)*

| Ticket | Work | Status |
| --- | --- | --- |
| JEF-15 | EF Core + SQL Server, Documents schema | Done |
| JEF-16 | Blob storage service (Azurite) | Done |
| JEF-17 | PDF text extraction service | Done |
| JEF-18 | Text chunking service | Todo |
| JEF-19 | Embedding generation service | Todo |
| JEF-20 | Vector store integration | Todo |
| JEF-21 | Upload and management endpoints | Todo |
| JEF-22 | Async ingestion pipeline | Todo |
| JEF-23 | Upload and list UI | Todo |

### Suggested dependency order

1. JEF-18 (chunking) and JEF-19 (embeddings) can proceed in parallel; both are unblocked.
2. JEF-20 (vector store) after JEF-19.
3. JEF-21 (endpoints) is unblocked now and can run alongside the above; JEF-23 (UI) follows it.
4. JEF-22 (pipeline) last on the backend, since it needs all of the above.

### Next milestone *(proposed)*

Add tickets for the question-answering endpoint, the chat UI with citations, and a deployment target (for example Azure Container Apps) once ingestion works end to end.

## 12. Risks and Mitigations

| Risk | Mitigation |
| --- | --- |
| Poor retrieval quality from bad chunking | Make size and overlap configurable; test with real documents; document the defaults |
| Embedding dimension mismatch with the vector schema | Read dimensions from configuration; create the collection on startup from that value |
| Provider rate limits or outages | Batching, retry with backoff, fake generator for tests |
| Scanned PDFs return no text | Detect empty extraction and mark *Failed* with a clear message; OCR is a future option |
| Hallucinated answers | Answer only from retrieved context, require citations, and say "not found" when context is insufficient |
| Secrets committed by accident | User-secrets, `.env` files in `.gitignore`, committed `.env.example` |
| Scope creep | Hold the non-goals list above until v1 ships |

## 13. Open Questions

1. **pgvector or Qdrant?** The decision and reasoning go in `docs/` as part of JEF-20.
2. **Azure OpenAI or OpenAI** for the first provider implementation?
3. **Which chat model** answers questions, and what top-K and context budget to use?
4. **Authentication:** needed for v1, or single-user local only?
5. **Streaming answers** (server-sent events) or a single response?
6. **Deployment target** and whether the ingestion worker stays in-process or becomes a separate container.

## 14. Success Criteria

- A new developer can clone the repo and reach a working app with `docker compose up`.
- A sample PDF uploaded through the UI reaches *Ready* and a question about its content returns a correct, cited answer.
- CI is green on both workflows and the automated tests never call a real AI provider.
- Every Sprint 2 ticket's acceptance criteria are checked off.
