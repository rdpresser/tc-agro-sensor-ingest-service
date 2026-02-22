# SignalR - Sensor Ingest Service

Guia de uso do WebSocket (SignalR) para recebimento de leituras de sensores e mudancas de status em tempo real.

## Visao Geral

O `SensorHub` permite que clientes recebam dados de sensores em tempo real via SignalR. Os dados sao agrupados por **Plot (Talhao)** — ao entrar em um grupo de plot, o cliente recebe todas as leituras dos sensores vinculados aquele talhao.

**Endpoint:** `/sensorHub`
**Porta:** `5003` (local)
**Autorizacao:** JWT Bearer — roles `Admin` ou `Producer`

---

## Autenticacao

A conexao requer um token JWT valido. O token e passado via query string automaticamente pelo cliente SignalR.

### Obtendo o Token

```bash
curl -s -X POST http://localhost:5001/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@tcagro.com","password":"Admin@123"}' | jq -r '.token'
```

Usuarios disponíveis (seed):
| Email | Senha | Role |
|---|---|---|
| `admin@tcagro.com` | `Admin@123` | Admin |
| `producer@tcagro.com` | `Producer@123` | Producer |

---

## Conexao (JavaScript)

```javascript
const token = "seu-jwt-token-aqui";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/sensorHub", {
        accessTokenFactory: () => token
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

await connection.start();
```

### CDN do cliente SignalR

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
```

### NPM

```bash
npm install @microsoft/signalr
```

---

## Metodos do Hub (Client → Server)

### `JoinPlotGroup(plotId: string)`

Entra no grupo de um talhao para receber leituras em tempo real. Ao entrar, o hub envia automaticamente as **ultimas 10 leituras** daquele talhao.

```javascript
await connection.invoke("JoinPlotGroup", "dec770c5-d45d-4192-a2fe-8d0ea5de8484");
```

**Parametro:** GUID valido do plot (nao pode ser vazio).
**Erro:** Lanca `HubException` se o plotId for invalido.

### `LeavePlotGroup(plotId: string)`

Sai do grupo de um talhao, parando de receber leituras.

```javascript
await connection.invoke("LeavePlotGroup", "dec770c5-d45d-4192-a2fe-8d0ea5de8484");
```

---

## Eventos (Server → Client)

### `sensorReading`

Recebido quando uma nova leitura de sensor e gerada (a cada 2 minutos pelo job Quartz, ou via API).

```javascript
connection.on("sensorReading", (data) => {
    console.log("Leitura:", data);
});
```

**Payload:**

```json
{
    "sensorId": "10c41afc-92f3-43c2-a12e-fc0254935423",
    "temperature": 28.5,
    "humidity": 65.2,
    "soilMoisture": 42.1,
    "timestamp": "2026-02-21T14:30:00+00:00"
}
```

| Campo | Tipo | Descricao |
|---|---|---|
| `sensorId` | `string (GUID)` | ID do sensor |
| `temperature` | `number?` | Temperatura em Celsius |
| `humidity` | `number?` | Umidade relativa (%) |
| `soilMoisture` | `number?` | Umidade do solo (%) |
| `timestamp` | `string (ISO 8601)` | Data/hora da leitura |

> **Importante:** Os nomes dos eventos usam **camelCase** (padrao do SignalR). No C# o metodo e `SensorReading`, mas no JS o listener deve ser `sensorReading`.

### `sensorStatusChanged`

Recebido quando o status operacional de um sensor muda.

```javascript
connection.on("sensorStatusChanged", (data) => {
    console.log("Status mudou:", data);
});
```

**Payload:**

```json
{
    "sensorId": "10c41afc-92f3-43c2-a12e-fc0254935423",
    "status": "Inactive"
}
```

| Campo | Tipo | Descricao |
|---|---|---|
| `sensorId` | `string (GUID)` | ID do sensor |
| `status` | `string` | Novo status (`Active`, `Inactive`, `Maintenance`, `Faulty`) |

---

## Grupos

Os clientes sao organizados em grupos por talhao com o formato `plot:{plotId}`. Quando uma leitura e gerada para um sensor, o sistema resolve o `PlotId` internamente a partir da tabela `sensor_snapshots` e envia para todos os clientes do grupo correspondente.

```
Sensor → SensorSnapshot (PlotId) → Grupo "plot:{plotId}" → Clientes conectados
```

---

## Exemplo Completo

```html
<!DOCTYPE html>
<html>
<head>
    <title>Sensor Monitor</title>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
</head>
<body>
    <div id="readings"></div>

    <script>
        const TOKEN = "seu-jwt-token";
        const PLOT_ID = "dec770c5-d45d-4192-a2fe-8d0ea5de8484";

        async function start() {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("/sensorHub", {
                    accessTokenFactory: () => TOKEN
                })
                .withAutomaticReconnect()
                .build();

            // Escuta leituras
            connection.on("sensorReading", (data) => {
                const div = document.getElementById("readings");
                div.innerHTML += `
                    <p>Sensor ${data.sensorId}:
                       Temp=${data.temperature}C
                       Hum=${data.humidity}%
                       Solo=${data.soilMoisture}%
                    </p>`;
            });

            // Escuta mudancas de status
            connection.on("sensorStatusChanged", (data) => {
                console.log(`Sensor ${data.sensorId} -> ${data.status}`);
            });

            // Reconexao automatica
            connection.onreconnected(async () => {
                await connection.invoke("JoinPlotGroup", PLOT_ID);
            });

            // Conecta e entra no grupo
            await connection.start();
            await connection.invoke("JoinPlotGroup", PLOT_ID);
        }

        start();
    </script>
</body>
</html>
```

---

## Pagina de Teste

Uma pagina de teste ja esta disponivel em:

```
http://localhost:5003/signalr-test.html
```

Funcionalidades:
- Input para JWT token
- Conectar/Desconectar do hub
- Entrar/Sair de grupos por Plot ID
- Log de todos os eventos recebidos em tempo real

---

## Fluxo de Dados

```
┌──────────────┐     ┌──────────────────┐     ┌─────────────┐
│  Quartz Job  │────>│ SensorHubNotifier │────>│  SensorHub  │
│  (2 min)     │     │                  │     │  (SignalR)  │
└──────────────┘     └──────────────────┘     └──────┬──────┘
                              │                       │
                     Resolve PlotId via          Broadcast to
                     SensorSnapshot table        plot:{plotId}
                                                      │
                                                      v
                                              ┌──────────────┐
                                              │   Clientes   │
                                              │  conectados  │
                                              └──────────────┘
```

1. O **SimulatedSensorReadingsJob** (Quartz) roda a cada 2 minutos
2. Gera leituras com dados simulados (Bogus) para todos os sensores ativos
3. Persiste no banco (TimescaleDB)
4. Publica evento de integracao (RabbitMQ → analytics-worker)
5. Chama `SensorHubNotifier.NotifySensorReadingAsync(sensorId, ...)`
6. O notifier busca o `PlotId` na tabela `sensor_snapshots`
7. Envia para o grupo `plot:{plotId}` via SignalR

---

## Troubleshooting

| Problema | Causa | Solucao |
|---|---|---|
| 401 na conexao | Token expirado ou invalido | Gerar novo token via `/api/identity/login` |
| 403 na conexao | Role incorreta | Usar usuario com role `Admin` ou `Producer` |
| Conectou mas nao recebe dados | Nao entrou no grupo | Chamar `JoinPlotGroup` com o GUID do plot |
| Entrou no grupo mas nao recebe | Nenhum sensor ativo no plot | Verificar `sensor_snapshots` no banco |
| Eventos nao aparecem no JS | Nome do evento em PascalCase | Usar `sensorReading` (camelCase) no listener |
| Dados antigos nao aparecem | Normal | Ao entrar no grupo, as ultimas 10 leituras sao enviadas automaticamente |
