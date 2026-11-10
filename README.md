# FCG-PaymentsAPI

Microserviço responsável por consumir pedidos de compra (`OrderPlacedEvent`), simular o processamento do pagamento e publicar o resultado (`PaymentProcessedEvent`) de volta ao RabbitMQ.

Parte do **FIAP Cloud Games (FCG)**, mantendo o fluxo de pagamentos da Fase 2 na evolução para a Fase 4.

## Papel na Fase 4

O serviço continua stateless e orientado a eventos. MongoDB, Redis e busca pertencem ao CatalogAPI;
não é necessário adicioná-los ao PaymentsAPI para cumprir o desenho atual. O escopo mínimo de
CI/CD do enunciado é UsersAPI e CatalogAPI, embora este repositório também tenha
[workflow de testes e publicação](.github/workflows/publish-container.yml) no GHCR com tag `latest`.
O workflow não faz deploy no cluster.

A [matriz da Fase 4](https://github.com/posgraduacaofiapnet/FCG-Orchestration/blob/master/docs/FASE4.md)
e a [arquitetura](https://github.com/posgraduacaofiapnet/FCG-Orchestration/blob/master/docs/ARQUITETURA.md)
centralizam requisitos, evidências e pendências. A NotificationsAPI antiga não participa do
Compose atual: notificações seguem por outbox SQL, SQS e Lambda.

---

## Tecnologias

- .NET 10 / ASP.NET Core
- MassTransit + RabbitMQ
- Swagger / OpenAPI
- Serilog (logs estruturados em JSON)
- Prometheus-net (Métricas de aplicação)

---

## Endpoints

| Método | Rota | Descrição | Auth |
|--------|------|-----------|------|
| `GET` | `/health` | Health check | Não |
| `GET` | `/metrics` | Métricas no padrão Prometheus | Não |

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
      → CatalogAPI persiste PaymentProcessed no outbox para a Lambda de notificações
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

O evento `PaymentProcessedEvent` carrega `OrderId`, `UserId`, `GameId`, `GameTitle`, `Price`,
`Status` (Approved | Rejected) e `ProcessedAt`. O `CorrelationId` é propagado pelo header da
mensagem, não por um campo desse contrato. Os contratos de pagamento estão em
`contracts/FCG.Contracts/Events.cs` e devem permanecer compatíveis com o CatalogAPI.

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
powershell -NoProfile -ExecutionPolicy Bypass -File ./start-local.ps1
```

O comando usa PowerShell. O Orchestration exige UsersAPI, CatalogAPI, PaymentsAPI,
`FCG-Outbox-Processor` e `FCG-Notifications-Lambda` como pastas irmãs. O script empacota a Lambda
antes de iniciar a stack local; o Compose sozinho não gera esse artefato na primeira execução.

Acompanhar logs do processamento de pagamentos:

```bash
docker compose logs -f payments-api
```

Swagger disponível em: http://localhost:5103/swagger

### Kubernetes

```bash
# A partir da pasta que contém os repositórios, em contexto já configurado
kubectl config current-context
kubectl apply -k FCG-Orchestration/k8s
kubectl wait --for=condition=complete job/fcg-database-init --timeout=180s
kubectl apply -f FCG-PaymentsAPI/k8s

# Verifique o serviço e acompanhe os logs
kubectl get pods
kubectl get services
kubectl logs -f deployment/payments-api
```

Os manifests usam imagem publicada no GHCR; construir imagem com nome local diferente não
altera a imagem do Deployment. Os comandos acima não provisionam cluster cloud e não sobem
APIs, workers, gateway e monitoramento completos. Configurar registry/acesso e segredos antes
do uso no ambiente final. A [validação central](https://github.com/posgraduacaofiapnet/FCG-Orchestration/blob/master/docs/VALIDACAO.md)
descreve os critérios de evidência para a Fase 4.

#### Manifestos Kubernetes

| Arquivo | Tipo | Descrição |
|---------|------|-----------|
| `deployment.yaml` | Deployment | Define o Pod com 1 réplica, imagem, probes e referência a ConfigMap/Secret |
| `service.yaml` | Service | Expõe a API internamente no cluster na porta 80 |
| `configmap.yaml` | ConfigMap | Configurações não-sensíveis (RabbitMQ host/username, nome da fila, ForceRejected) |
| `secret.yaml` | Secret | Configuração sensível do RabbitMQ; há valores fixos versionados, ainda pendentes de adequação à Fase 4 |

As **readinessProbe** e **livenessProbe** do Deployment apontam para `/health`. A resposta HTTP
não verifica todas as dependências e não comprova, sozinha, rollout sem indisponibilidade.

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
│   └── PaymentsAPI.UnitTests/ # Testes unitários (xUnit)
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
- [FCG-Notifications-Lambda](https://github.com/posgraduacaofiapnet/FCG-Notifications-Lambda) — consumidor serverless dos eventos de notificação
- [FCG-Outbox-Processor](https://github.com/posgraduacaofiapnet/FCG-Outbox-Processor) — publicação confiável dos eventos SQL na SQS
