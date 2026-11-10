# PaymentsAPI

Microsservico responsavel por consumir pedidos, simular pagamento e publicar o resultado.

## Endpoints

- `GET /health`
- `GET /metrics`

## Eventos

- Consome: `OrderPlacedEvent`
- Publica: `PaymentProcessedEvent`

## Variaveis

- `RabbitMq__Host`
- `RabbitMq__Username`
- `RabbitMq__Password`
- `RabbitMq__OrderPlacedQueue`
- `Payments__ForceRejected`

Este projeto já pertence ao repositório separado `FCG-PaymentsAPI`. Consultar o
[README principal](../../README.md) para execução, contratos, Kubernetes e papel na Fase 4.
O serviço não tem banco próprio; publica o resultado no RabbitMQ. O CatalogAPI registra a
notificação na outbox SQL para publicação posterior na SQS e consumo pela Lambda.
