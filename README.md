# FCG-PaymentsAPI

Microservice responsible for consuming purchase orders (`OrderPlacedEvent`), simulating payment processing, and publishing the result (`PaymentProcessedEvent`) back to RabbitMQ.

Part of **FIAP Cloud Games (FCG)** — Tech Challenge Phase 2.

## Tech Stack

- .NET 10 / ASP.NET Core
- MassTransit + RabbitMQ
- Swagger / OpenAPI

## Endpoints

| Method | Route | Description | Auth |
|--------|-------|-------------|------|
| `GET` | `/health` | Health check | No |

> PaymentsAPI is primarily event-driven — it has no user-facing purchase endpoints. It reacts to events published by CatalogAPI.

## Events

| Direction | Event | Trigger |
|-----------|-------|---------|
| Consumes | `OrderPlacedEvent` | Triggered by CatalogAPI on purchase request |
| Publishes | `PaymentProcessedEvent` | After simulating approval/rejection |

## Payment Simulation

By default, all payments are **approved**. To force rejection for testing:

```json
// appsettings.json or environment variable
"Payments": {
  "ForceRejected": true
}
```

## Event-Driven Flow

```
CatalogAPI publishes OrderPlacedEvent
  → PaymentsAPI consumes and simulates processing
    → PaymentsAPI publishes PaymentProcessedEvent (Approved | Rejected)
      → CatalogAPI updates user library (if approved)
      → NotificationsAPI sends confirmation email (if approved)
```

## Environment Variables

| Variable | Description |
|----------|-------------|
| `RabbitMq__Host` | RabbitMQ hostname |
| `RabbitMq__Username` | RabbitMQ username |
| `RabbitMq__Password` | RabbitMQ password |
| `RabbitMq__OrderPlacedQueue` | Queue name for incoming orders |
| `Payments__ForceRejected` | Set to `true` to simulate payment rejection |

## Running Locally

### Docker Compose (via FCG-Orchestration)

```bash
cd FCG-Orchestration
docker compose up --build
```

Watch payment processing logs:

```bash
docker compose logs -f payments_api
```

### Kubernetes

```bash
# Build the image first
cd FCG-PaymentsAPI
docker build -t fcg-payments-api:latest -f services/PaymentsAPI/Dockerfile .

# Apply manifests
cd k8s
kubectl apply -f .

# Verify
kubectl get pods
kubectl logs -f deployment/payments-api
```

## Solution Structure

```
FCG-PaymentsAPI/
├── FCG-PaymentsAPI.sln
├── contracts/
│   └── FCG.Contracts/        # Shared event contracts
├── services/
│   └── PaymentsAPI/          # Main service project
└── k8s/                      # Kubernetes manifests
    ├── deployment.yaml
    ├── service.yaml
    ├── configmap.yaml
    └── secret.yaml
```

## Related Repositories

- [FCG-Orchestration](https://github.com/posgraduacaofiapnet/FCG-Orchestration) — Docker Compose + global K8s infra
- [FCG-UsersAPI](https://github.com/posgraduacaofiapnet/FCG-UsersAPI)
- [FCG-CatalogAPI](https://github.com/posgraduacaofiapnet/FCG-CatalogAPI)
- [FCG-NotificationsAPI](https://github.com/posgraduacaofiapnet/FCG-NotificationsAPI)
