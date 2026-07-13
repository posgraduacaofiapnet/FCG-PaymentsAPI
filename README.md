# FCG-PaymentsAPI

Microserviço responsável por consumir pedidos de compra (`OrderPlacedEvent`), simular o processamento do pagamento e publicar o resultado (`PaymentProcessedEvent`) de volta ao RabbitMQ.

Parte do **FIAP Cloud Games (FCG)** — Tech Challenge Fase 2.

---

## Tecnologias

- .NET 10 / ASP.NET Core
- MassTransit + RabbitMQ
- Swagger / OpenAPI
- Serilog (logs estruturados em JSON)

---

## Endpoints

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `GET` | `/health` | Health check | Não |

> A PaymentsAPI é totalmente orientada a eventos — não possui endpoints de compra expostos ao usuário. Ela reage exclusivamente a eventos publicados pela CatalogAPI.

---

## Eventos

| Direção | Evento | Gatilho |
|---------|--------|---------|
| Consome | `OrderPlacedEvent` | Disparado pela CatalogAPI ao receber uma solicitação de compra |
| Publica | `PaymentProcessedEvent` | Após simular a aprovação ou rejeição do pagamento |

---

## Fluxo Event-Driven

```
CatalogAPI publica OrderPlacedEvent
  → PaymentsAPI consome e simula o processamento
    → PaymentsAPI publica PaymentProcessedEvent (Aprovado | Rejeitado)
      → CatalogAPI atualiza a biblioteca do usuário (se Aprovado)
      → NotificationsAPI envia e-mail de confirmação (se Aprovado)
```

---

## Simulação de Pagamento

Por padrão, **todos os pagamentos são aprovados**. Para forçar rejeição em testes:

```json
// appsettings.json ou variável de ambiente
"Payments": {
  "ForceRejected": true
}
```

Ou via variável de ambiente:

```bash
Payments__ForceRejected=true
```

O evento `PaymentProcessedEvent` carrega `OrderId`, `UserId`, `GameId`, `Status` (Approved | Rejected) e `CorrelationId`.

---

## Variáveis de Ambiente

| Variável | Descrição |
|----------|-----------|
| `RabbitMq__Host` | Hostname do RabbitMQ |
| `RabbitMq__Username` | Usuário do RabbitMQ |
| `RabbitMq__Password` | Senha do RabbitMQ |
| `RabbitMq__OrderPlacedQueue` | Nome da fila para pedidos recebidos |
| `Payments__ForceRejected` | Defina `true` para simular rejeição de pagamentos |

---

## Executando Localmente

### Docker Compose (via FCG-Orchestration)

```bash
cd FCG-Orchestration
docker compose up --build
```

Acompanhar logs do processamento de pagamentos:

```bash
docker compose logs -f payments-api
```

Swagger disponível em: http://localhost:5103/swagger

### Kubernetes

```bash
# 1. Build da imagem local
cd FCG-PaymentsAPI
docker build -t fcg-payments-api:latest -f services/PaymentsAPI/Dockerfile .

# 2. Aplique a infra (RabbitMQ) primeiro
cd ../FCG-Orchestration/k8s
kubectl apply -f .

# 3. Aplique os manifestos da PaymentsAPI
cd ../../FCG-PaymentsAPI/k8s
kubectl apply -f .

# 4. Verifique os pods
kubectl get pods
kubectl get services

# 5. Acompanhe os logs
kubectl logs -f deployment/payments-api
```

#### Manifestos Kubernetes

| Arquivo | Tipo | Descrição |
|---------|------|-----------|
| `deployment.yaml` | Deployment | Define o Pod com 1 réplica, imagem, probes e referência a ConfigMap/Secret |
| `service.yaml` | Service | Expõe a API internamente no cluster na porta 80 |
| `configmap.yaml` | ConfigMap | Configurações não-sensíveis (RabbitMQ host/username, nome da fila, ForceRejected) |
| `secret.yaml` | Secret | Dados sensíveis em base64 (RabbitMQ password) |

As **readinessProbe** e **livenessProbe** do Deployment apontam para `/health` — o pod só recebe tráfego após o healthcheck passar.

---

## Testes Unitários

```bash
cd FCG-PaymentsAPI
dotnet test FCG-PaymentsAPI.sln
```

Os testes utilizam **xUnit** e **Bogus** para geração de dados fictícios.

---

## Estrutura da Solution

```
FCG-PaymentsAPI/
├── FCG-PaymentsAPI.sln
├── contracts/
│   └── FCG.Contracts/        # Contratos de eventos compartilhados
├── services/
│   └── PaymentsAPI/          # Projeto principal do serviço
├── tests/
│   └── PaymentsAPI.Tests/    # Testes unitários (xUnit)
└── k8s/                      # Manifestos Kubernetes
    ├── deployment.yaml
    ├── service.yaml
    ├── configmap.yaml
    └── secret.yaml
```

---

## Repositórios Relacionados

- [FCG-Orchestration](https://github.com/posgraduacaofiapnet/FCG-Orchestration) — Docker Compose + infraestrutura K8s global
- [FCG-UsersAPI](https://github.com/posgraduacaofiapnet/FCG-UsersAPI)
- [FCG-CatalogAPI](https://github.com/posgraduacaofiapnet/FCG-CatalogAPI)
- [FCG-NotificationsAPI](https://github.com/posgraduacaofiapnet/FCG-NotificationsAPI)
