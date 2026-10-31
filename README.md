# doc-qa

dotnet run --project src/DocQA.API
npm run dev

## Run with Docker

```bash
cp .env.example .env        # then edit the values
docker compose up --build -d
docker compose ps           # wait until all services are healthy
```

- Web app: http://localhost:3000
- API health: http://localhost:8080/health
- Stop: `docker compose down` (add `-v` to also delete data volumes)
